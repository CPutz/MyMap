using System;
using System.Collections.Generic;

namespace MyMap
{
    public enum RouteMode { Fastest, Shortest };

    public class RouteFinder
    {
        private Graph graph;

        public RouteFinder(Graph graph)
        {
            //save the graph
            this.graph = graph;
        }


        public long[] SubArray(long[] array, int start, int end)
        {
            long[] res = null;

            if (end - start > 0 && start > 0 && end <= array.Length)
            {
                res = new long[end - start];
                Array.Copy(array, start, res, 0, end - start);
            }

            return res;
        }
        public Node[] AddArray(Node[] array1, Node[] array2)
        {
            Node[] res = null;

            if (array1 != null && array2 != null)
            {
                res = new Node[array1.Length + array2.Length];
                Array.Copy(array1, res, array1.Length);
                Array.Copy(array2, 0, res, array1.Length, array2.Length);
            }

            return res;
        }

        public long[] AddArray(long[] array1, long[] array2)
        {
            long[] res = null;

            if (array1 != null && array2 != null)
            {
                res = new long[array1.Length + array2.Length];
                Array.Copy(array1, res, array1.Length);
                Array.Copy(array2, 0, res, array1.Length, array2.Length);
            }

            return res;
        }


        /// <summary>
        /// Returns the route through points "nodes" starting with Vehicles "vehicles",
        /// without using any vehicletype from "forbiddenVehicles" and using myVehicles "myVehicles".
        /// </summary>
        public Route CalcRoute(long[] nodes, List<Vehicle> vehicles, List<Vehicle> forbiddenVehicles, List<MyVehicle> myVehicles, RouteMode mode)
        {
            Route res = new Route(new Node[0], Vehicle.Foot);
            res.Length = double.PositiveInfinity;
            res.Time = double.PositiveInfinity;

            Route r = null;
            double min = double.PositiveInfinity;

            for (int i = 0; i < myVehicles.Count; i++ )
            {
                MyVehicle v = myVehicles[i];

                if (!forbiddenVehicles.Contains(v.VehicleType))
                {
                    // Calc route to the MyVehicle
                    List<MyVehicle> newMyVehicles = new List<MyVehicle>();
                    foreach (MyVehicle vNew in myVehicles)
                        if (vNew != v)
                            newMyVehicles.Add(vNew);

                    Route toVehicle = CalcRoute(new long[] { nodes[0], v.Location.ID }, vehicles, forbiddenVehicles, newMyVehicles, mode);

                    Route fromVehicle = null;

                    v.Route = toVehicle;

                    if (toVehicle != null && !Double.IsPositiveInfinity(toVehicle.Length))
                    {
                        // Calc route from MyVehicle through the given points
                        long[] through = new long[nodes.Length];
                        Array.Copy(nodes, through, nodes.Length);

                        through[0] = v.Location.ID;
                        fromVehicle = CalcRoute(through, new List<Vehicle>() { v.VehicleType, Vehicle.Foot }, forbiddenVehicles, newMyVehicles, mode);


                        // Route from source to destination using MyVehicle is
                        r = toVehicle + fromVehicle;
                    }

                    if (r != null && (r.Time < min && mode == RouteMode.Fastest || r.Length < min && mode == RouteMode.Shortest))
                    {
                        res = r;
                        switch (mode)
                        {
                            case RouteMode.Fastest:
                                min = r.Time;
                                break;
                            case RouteMode.Shortest:
                                min = r.Length;
                                break;
                        }
                    }
                }
            }


            if (nodes.Length >= 2)
            {
                r = Dijkstra(nodes[0], nodes[1], vehicles.ToArray(), mode, !forbiddenVehicles.Contains(Vehicle.Bus));
                
                    if (nodes.Length > 2)
                    {
                        r += CalcRoute(SubArray(nodes, 1, nodes.Length), vehicles, forbiddenVehicles, myVehicles, mode);
                    }
            }

            if (r != null && (r.Time < min && mode == RouteMode.Fastest || r.Length < min && mode == RouteMode.Shortest))
                res = r;

            return res;
        }


        /// <summary>
        /// Dijkstra in graph gr, from source to destination, using vehicle v.
        /// </summary>
        /// <param name="source"> the startpoint </param>
        /// <param name="destination"> the destination </param>
        /// <param name="v"> vehicle that is used </param>
        /// <returns></returns>
        public Route Dijkstra(long from, long to, Vehicle[] vehicles, RouteMode mode, bool useBus)
        {
            Route result = null;

            if (from == 0 || to == 0 || graph == null)
                return result;


            Node source = graph.GetNode(from);
            Node destination = graph.GetNode(to);

            //all nodes that are completely solved
            SortedList<long, Node> solved = new SortedList<long, Node>();


            //nodes that are encountered but not completely solved
            SortedList<double, Node> unsolved = new SortedList<double, Node>();

            RBTree<Node> prevs = new RBTree<Node>();
            RBTree<double> times = new RBTree<double>();
            RBTree<double> distances = new RBTree<double>();
            RBTree<Vehicle> vehicleUse = new RBTree<Vehicle>();
            List<Edge> abstractBusses = graph.GetAbstractBusEdges();

            Node current = source;
            bool found = false;

            //if there's no new current node it means the algorithm should stop
            while (current != null)
            {
                //if we encounter the destination it means we found the shortest route so we break
                if (current == destination)
                {
                    found = true;
                    break;
                }


                List<Edge> edges = new List<Edge>(graph.GetEdgesFromNode(current.ID));
                foreach (Edge busEdge in abstractBusses)
                {
                    if (busEdge.End == current.ID || busEdge.Start == current.ID)
                        edges.Add(busEdge);
                }

                foreach (Edge e in edges)
                {
                    if (IsAllowed(e, vehicles, useBus))
                    {
                        Node start = graph.GetNode(e.Start);
                        Node end = graph.GetNode(e.End);
                        double distance = double.PositiveInfinity;
                        double time = double.PositiveInfinity;
                        Vehicle v = vehicles[0];


                        if (e.Type != CurveType.AbstractBusRoute)
                        {
                            double speed = 0;
                            foreach (Vehicle vehicle in vehicles)
                            {
                                double vSpeed = GetSpeed(vehicle, e);
                                if (vSpeed > speed && IsAllowed(e, vehicle, useBus))
                                {
                                    speed = vSpeed;
                                    v = vehicle;
                                }
                            }

                            distance = NodeCalcExtensions.Distance(start, end);
                            time = distance / speed;

                            if (e.Route != null)
                            {
                                // Take busroute if better
                                if (mode == RouteMode.Shortest && distance > e.Route.Length)
                                {
                                    distance = e.Route.Length;
                                    v = Vehicle.Foot;
                                }
                                else if (mode == RouteMode.Fastest && time > e.Route.Time)
                                {
                                    time = e.Route.Time;
                                    v = Vehicle.Foot;
                                }
                            }
                        }
                        else
                        {
                            Node n1 = null, n2 = null;
                            if (start.Longitude != 0 || start.Latitude != 0)
                                n1 = graph.GetNodeByPos(start.Longitude, start.Latitude, Vehicle.Bus);
                            if (end.Longitude != 0 || end.Latitude != 0)
                                n2 = graph.GetNodeByPos(end.Longitude, end.Latitude, Vehicle.Bus);

                            if (n1 != default(Node) && n2 != default(Node))
                            {
                                Curve curve = new Curve(new long[] { start.ID, end.ID }, e.name);
                                //curve = new BusCurve(new long[] { street1.ID, street2.ID }, name);

                                Route r = this.Dijkstra(n1.ID, n2.ID, new Vehicle[] { Vehicle.Bus }, RouteMode.Fastest, false);
                                r = new Route(new Node[] { start }, Vehicle.Bus) + r + new Route(new Node[] { end }, Vehicle.Bus);

                                curve.Type = CurveType.Bus;
                                curve.Route = r;

                                // We calculate with 30 seconds of waiting time for the bus
                                r.Time += 30;

                                graph.AddWay(start.ID, curve);
                                graph.AddWay(end.ID, curve);

                                e.Route = r;

                                // Take busroute if better
                                if (mode == RouteMode.Shortest && distance > e.Route.Length)
                                {
                                    distance = e.Route.Length;
                                    v = Vehicle.Foot;
                                }
                                else if (mode == RouteMode.Fastest && time > e.Route.Time)
                                {
                                    time = e.Route.Time;
                                    v = Vehicle.Foot;
                                }
                            }

                            graph.RemoveAbstractBus(e);
                            abstractBusses.Remove(e);
                        }


                        time += times.Get(current.ID);
                        double trueDist = distances.Get(current.ID) + distance;
                        
                        if (!solved.ContainsValue(end) && current != end)
                        {
                            if (end.Latitude != 0 && end.Longitude != 0)
                            {
                                if (times.Get(end.ID) == 0 || distances.Get(end.ID) == 0)
                                {
                                    times.Insert(end.ID, double.PositiveInfinity);
                                    distances.Insert(end.ID, double.PositiveInfinity);
                                    vehicleUse.Insert(end.ID, v);
                                }

                                if ((mode == RouteMode.Fastest &&
                                    times.Get(end.ID) > time) ||
                                    (mode == RouteMode.Shortest &&
                                    distances.Get(end.ID) > trueDist))
                                {
                                    times.GetNode(end.ID).Content = time;
                                    distances.GetNode(end.ID).Content = trueDist;
                                    vehicleUse.GetNode(end.ID).Content = v;
                                    
                                    if (prevs.GetNode(end.ID).Content == null)
                                        prevs.Insert(end.ID, current);
                                    else
                                        prevs.GetNode(end.ID).Content = current;

                                    if (!unsolved.ContainsValue(end))
                                    {
                                        if (mode == RouteMode.Fastest)
                                        {
                                            // Very bad solution but I couldn't think of a simple better one.
                                            while (unsolved.ContainsKey(times.Get(end.ID)))
                                            {
                                                times.GetNode(end.ID).Content += 0.0000000001;
                                            }

                                            unsolved.Add(times.Get(end.ID), end);
                                        }
                                        else if (mode == RouteMode.Shortest)
                                        {
                                            // Very bad solution but I couldn't think of a simple better one.
                                            while (unsolved.ContainsKey(distances.Get(end.ID)))
                                            {
                                                distances.GetNode(end.ID).Content += 0.0000000001;
                                            }

                                            unsolved.Add(distances.Get(end.ID), end);
                                        }
                                    }
                                }
                            }
                        }
                        else if (!solved.ContainsValue(start) && current != start)
                        {
                            if (start.Latitude != 0 && start.Longitude != 0)
                            {
                                if (times.Get(start.ID) == 0 || distances.Get(start.ID) == 0)
                                {
                                    times.Insert(start.ID, double.PositiveInfinity);
                                    distances.Insert(start.ID, double.PositiveInfinity);
                                    vehicleUse.Insert(start.ID, v);
                                }

                                if ((mode == RouteMode.Fastest &&
                                    times.Get(start.ID) > time) ||
                                    (mode == RouteMode.Shortest &&
                                    distances.Get(start.ID) > trueDist))
                                {
                                    times.GetNode(start.ID).Content = time;
                                    distances.GetNode(start.ID).Content = trueDist;
                                    vehicleUse.GetNode(start.ID).Content = v;

                                    if (prevs.GetNode(start.ID).Content == null)
                                        prevs.Insert(start.ID, current);
                                    else
                                        prevs.GetNode(start.ID).Content = current;

                                    if (!unsolved.ContainsValue(start))
                                    {
                                        if (mode == RouteMode.Fastest)
                                        {
                                            // Very bad solution but I couldn't think of a simple better one.
                                            while (unsolved.ContainsKey(times.Get(start.ID)))
                                            {
                                                times.GetNode(start.ID).Content += 0.0000000001;
                                            }

                                            unsolved.Add(times.Get(start.ID), start);
                                        }
                                        else if (mode == RouteMode.Shortest)
                                        {
                                            // Very bad solution but I couldn't think of a simple better one.
                                            while (unsolved.ContainsKey(distances.Get(start.ID)))
                                            {
                                                distances.GetNode(start.ID).Content += 0.0000000001;
                                            }

                                            unsolved.Add(distances.Get(start.ID), start);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                //dit zou niet voor moeten komen maar toch gebeurt het...
                if (!solved.ContainsKey(current.ID))
                    solved.Add(current.ID, current);

                if (unsolved.Count > 0)
                {
                    current = unsolved.Values[0];
                    unsolved.RemoveAt(0);
                }
                else
                {
                    current = null;
                }
            }


            if (found)
            {
                List<Node> nodes = new List<Node>();
                List<long> extras = graph.GetExtras();
                Node n = destination;

                List<long> busStartStop = new List<long>();

                do
                {
                    bool foundRoute = false;

                    if (n.ID == 643040516 || n.ID == 643040521)
                    {
                        int test = 33;
                        test *= 200;
                    }

                    if (extras.Contains(n.ID))
                    {
                        // Change straigt buslines in for the actual route.
                        foreach (Edge e in graph.GetEdgesFromNode(n.ID))
                        {
                            if (n.ID == e.Start &&
                                prevs.Get(n.ID) != null &&
                                prevs.Get(n.ID).ID == e.End &&
                                e.Route != null)
                            {
                                Node[] busNodes = e.Route.Points;

                                if (busNodes[0].ID == e.Start)
                                    Array.Reverse(busNodes);

                                busStartStop.Add(busNodes[0].ID);
                                busStartStop.Add(busNodes[busNodes.Length - 1].ID);
                                nodes.InsertRange(0, busNodes);

                                //n = prevs.Get(n.ID);
                                n = prevs.Get(n.ID);

                                foundRoute = true;
                                break;
                            }
                            else if (n.ID == e.End &&
                                     prevs.Get(n.ID).ID == e.Start &&
                                     e.Route != null)
                            {

                                Node[] busNodes = e.Route.Points;

                                if (busNodes[0].ID == e.End)
                                    Array.Reverse(busNodes);

                                busStartStop.Add(busNodes[0].ID);
                                busStartStop.Add(busNodes[busNodes.Length - 1].ID);
                                nodes.InsertRange(0, busNodes);
                                
                                //n = prevs.Get(n.ID);
                                n = prevs.Get(n.ID);

                                foundRoute = true;
                                break;
                            }
                        }
                    }

                    if (!foundRoute)
                    {
                        nodes.Insert(0, n);
                        n = prevs.Get(n.ID);
                    }

                } while (n != null);


                result = new Route(nodes.ToArray(), vehicles[0]);
                result.Time = times.Get(destination.ID);
                result.Length = distances.Get(destination.ID);


                // Set bus as vehicle
                if (busStartStop.Count > 0)
                {
                    int i = 0;
                    Node[] routePoints = result.Points;

                    for (int j = 0; j < routePoints.Length; j++)
                    {
                        if (routePoints[j].ID == busStartStop[i])
                        {
                            if (i % 2 == 1)
                            {
                                result.SetVehicle(Vehicle.Foot, j);
                                i++;
                            }
                            else
                            {
                                result.SetVehicle(Vehicle.Bus, j);
                                i++;
                            }
                        }

                        if (i >= busStartStop.Count)
                            break;
                    }
                }

                Vehicle cur = result.GetVehicle(0), prev;
                for (int i = 2; i < result.NumOfNodes; i++)
                {
                    if (result.GetVehicle(i) != Vehicle.Bus)
                    {
                        prev = vehicleUse.Get(result[i].ID);

                        if (prev != cur && i > 1)
                            result.SetVehicle(prev, i - 1);

                        cur = prev;
                    }
                }

            }
            else
            {
                result = new Route(new Node[] { source }, vehicles[0]);
                result.Time = double.PositiveInfinity;
                result.Length = double.PositiveInfinity;
            }

            return result;
        }


        private bool IsAllowed(Edge e, Vehicle[] vehicles, bool useBus)
        {
            foreach (Vehicle vehicle in vehicles)
            {
                if (IsAllowed(e, vehicle, useBus))
                    return true;
            }

            return false;
        }

        private bool IsAllowed(Edge e, Vehicle v, bool useBus)
        {
            switch (v)
            {
                case Vehicle.Car:
                case Vehicle.Bus:
                    return CurveTypeExtentions.CarsAllowed(e.Type);
                case Vehicle.Bicycle:
                    return CurveTypeExtentions.BicyclesAllowed(e.Type);
                case Vehicle.Foot:
                    if (useBus || !useBus && e.Type != CurveType.Bus && e.Type != CurveType.AbstractBusRoute)
                        return CurveTypeExtentions.FootAllowed(e.Type);
                    else
                        return false;
                case Vehicle.All:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Retuns the speed using vehicle v in metre/second.
        /// </summary>
        private double GetSpeed(Vehicle v, Edge e)
        {
            switch (v)
            {
                case Vehicle.Car:
                case Vehicle.Bus:
                    if (e.MaxSpeed > 0)
                        return e.MaxSpeed / 3.6;
                    else
                        return 14; // Assuming cars have a average speed of 50km/h.
                case Vehicle.Bicycle:
                    return 5.3; // Using Google Maps: 37,8km in 2h => 5,3m/s.
                case Vehicle.Foot:
                    if (e.Type != CurveType.Bus)
                        return 1.4; // Documentation: http://ageing.oxfordjournals.org/content/26/1/15.full.pdf.
                    else
                        return 22; // By assuming busses have a average speed of 80km/h.
                default:
                    return 1.4;
            }
        }
    }
}

