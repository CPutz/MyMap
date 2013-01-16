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


        public Node[] SubArray(Node[] array, int start, int end)
        {
            Node[] res = null;

            if (end - start > 0 && start > 0 && end <= array.Length)
            {
                res = new Node[end - start];
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
        /// without using any vehicletype from "forbiddenVehicles" and without using any myVehicles.
        /// </summary>
        public Route CalcRoute(long[] nodes, Vehicle[] vehicles, List<Vehicle> forbiddenVehicles, RouteMode mode)
        {
            return CalcRoute(nodes, vehicles, forbiddenVehicles, new MyVehicle[0], 1, mode);
        }

        /// <summary>
        /// Returns the route through points "nodes" starting with Vehicles "vehicles",
        /// without using any vehicletype from "forbiddenVehicles" and using myVehicles "myVehicles".
        /// </summary>
        public Route CalcRoute(long[] nodes, Vehicle[] vehicles, List<Vehicle> forbiddenVehicles, MyVehicle[] myVehicles, int iterations, RouteMode mode)
        {
            Route res = null;
            Route r = null;
            double min = double.PositiveInfinity;

            foreach (MyVehicle v in myVehicles)
            {
                if (!forbiddenVehicles.Contains(v.VehicleType))
                {

                    // Calc route to the MyVehicle
                    Route toVehicle = RouteThrough(nodes[0], v.Location.ID, vehicles, forbiddenVehicles, mode);
                    Route fromVehicle = null;

                    v.Route = toVehicle;

                    if (toVehicle != null && !Double.IsPositiveInfinity(toVehicle.Length))
                    {
                        if (iterations > 0)
                        {
                            // Calc route from MyVehicle through the given points
                            long[] through = new long[nodes.Length];
                            Array.Copy(nodes, through, nodes.Length);

                            //through = AddArray(new long[] { v.Location.ID }, through);

                            through[0] = v.Location.ID;
                            fromVehicle = RouteThrough(through, v.VehicleType, forbiddenVehicles, mode);

                            //fromVehicle = CalcRoute(through, vehicles, myVehicles, iterations - 1);

                            /*fromVehicle = RouteThrough(AddArray(new long[] { v.Location.ID },
                            SubArray(nodes, 1, nodes.Length)), v.VehicleType);*/

                            // Route from source to destination using MyVehicle is
                            r = toVehicle + fromVehicle;
                        }
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


            r = RouteThrough(nodes, vehicles, forbiddenVehicles, mode);
            if (r != null && (r.Time < min && mode == RouteMode.Fastest || r.Length < min && mode == RouteMode.Shortest))
                res = r;

            return res;
        }


        /// <summary>
        /// Returns the route through two points "n1" and "n2" using Vehicles "vehicles"
        /// </summary>
        private Route RouteThrough(long n1, long n2, Vehicle[] vehicles, List<Vehicle> forbiddenVehicles, RouteMode mode)
        {
            return RouteThrough(new long[] { n1, n2 }, vehicles, forbiddenVehicles, mode);
        }

        /// <summary>
        /// Returns the route through points "nodes" using Vehicle "vehicle"
        /// </summary>
        private Route RouteThrough(long[] nodes, Vehicle vehicle, List<Vehicle> forbiddenVehicles, RouteMode mode)
        {
            return RouteThrough(nodes, new Vehicle[] { vehicle }, forbiddenVehicles, mode);
        }

        /// <summary>
        /// Returns the route through points "nodes" using Vehicles "vehicles"
        /// </summary>
        private Route RouteThrough(long[] nodes, Vehicle[] vehicles, List<Vehicle> forbiddenVehicles, RouteMode mode)
        {
            Route res = null;
            double min = double.PositiveInfinity;

            foreach (Vehicle v in vehicles)
            {
                if (!forbiddenVehicles.Contains(v))
                {
                    Route r = null;

                    for (int i = 0; i < nodes.Length - 1; i++)
                    {
                        r += Dijkstra(nodes[i], nodes[i + 1], v, mode);
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

            return res;
        }


        /// <summary>
        /// Dijkstra in graph gr, from source to destination, using vehicle v.
        /// </summary>
        /// <param name="source"> the startpoint </param>
        /// <param name="destination"> the destination </param>
        /// <param name="v"> vehicle that is used </param>
        /// <returns></returns>
        public Route Dijkstra(long from, long to, Vehicle v, RouteMode mode)
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

                foreach (Edge e in graph.GetEdgesFromNode(current.ID))
                {

                    if (IsAllowed(e, v))
                    {
                        Node start = graph.GetNode(e.Start);
                        Node end = graph.GetNode(e.End);
                        double distance = double.PositiveInfinity;

                        if (e.Route != null)
                        {
                            distance = e.Route.Length;
                            e.SetTime(e.Route.Time, Vehicle.Foot);
                        }
                        else
                        {
                            double speed = GetSpeed(v, e);
                            distance = NodeCalcExtensions.Distance(start, end);
                            e.SetTime(distance / speed, v);
                        }

                        double time = times.Get(current.ID) + e.GetTime(v);
                        double trueDist = distances.Get(current.ID) + distance;
                        
                        if (!solved.ContainsValue(end) && current != end)
                        {
                            if (end.Latitude != 0 && end.Longitude != 0)
                            {
                                if (mode == RouteMode.Fastest && times.Get(end.ID) == 0)
                                    times.Insert(end.ID, double.PositiveInfinity);

                                if (mode == RouteMode.Shortest && distances.Get(end.ID) == 0)
                                    distances.Insert(end.ID, double.PositiveInfinity);

                                if ((mode == RouteMode.Fastest &&
                                    times.Get(end.ID) > time) ||
                                    (mode == RouteMode.Shortest &&
                                    distances.Get(end.ID) > trueDist))
                                {
                                    times.GetNode(end.ID).Content = time;
                                    distances.GetNode(end.ID).Content = trueDist;
                                    
                                    if (prevs.GetNode(end.ID).Content == null)
                                        prevs.Insert(end.ID, current);
                                    else
                                        prevs.GetNode(end.ID).Content = current;

                                    if (!unsolved.ContainsValue(end))
                                    {
                                        // Very bad solution but I couldn't think of a simple better one.
                                        while (unsolved.ContainsKey(times.Get(end.ID))) { 
                                            times.GetNode(end.ID).Content += 0.0000000001; 
                                        }

                                        unsolved.Add(times.Get(end.ID), end);
                                    }
                                }
                            }
                        }
                        else if (!solved.ContainsValue(start) && current != start)
                        {
                            if (start.Latitude != 0 && start.Longitude != 0)
                            {
                                if (mode == RouteMode.Fastest && times.Get(start.ID) == 0)
                                    times.Insert(start.ID, double.PositiveInfinity);

                                if (mode == RouteMode.Shortest && distances.Get(start.ID) == 0)
                                    distances.Insert(start.ID, double.PositiveInfinity);

                                if ((mode == RouteMode.Fastest &&
                                    times.Get(start.ID) > time) ||
                                    (mode == RouteMode.Shortest &&
                                    distances.Get(start.ID) > trueDist))
                                {
                                    times.GetNode(start.ID).Content = time;
                                    distances.GetNode(start.ID).Content = trueDist;

                                    if (prevs.GetNode(start.ID).Content == null)
                                        prevs.Insert(start.ID, current);
                                    else
                                        prevs.GetNode(start.ID).Content = current;

                                    if (!unsolved.ContainsValue(start))
                                    {
                                        // Very bad solution but I couldn't think of a simple better one.
                                        while (unsolved.ContainsKey(times.Get(start.ID)))
                                        {
                                            times.GetNode(start.ID).Content += 0.0000000001;
                                        }

                                        unsolved.Add(times.Get(start.ID), start);
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

                                n = prevs.Get(n.ID);
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
                                
                                n = prevs.Get(n.ID);
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

                result = new Route(nodes.ToArray(), v);
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
            }
            else
            {
                result = new Route(new Node[] { source }, v);
                result.Time = double.PositiveInfinity;
                result.Length = double.PositiveInfinity;
            }

            return result;
        }


        private bool IsAllowed(Edge e, Vehicle v)
        {
            switch (v)
            {
                case Vehicle.Car:
                case Vehicle.Bus:
                    return CurveTypeExtentions.CarsAllowed(e.Type);
                case Vehicle.Bicycle:
                    return CurveTypeExtentions.BicyclesAllowed(e.Type);
                case Vehicle.Foot:
                    return CurveTypeExtentions.FootAllowed(e.Type);
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

