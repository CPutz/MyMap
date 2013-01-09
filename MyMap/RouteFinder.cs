using System;
using System.Collections.Generic;

namespace MyMap
{
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
        /// Returns the route through points "nodes" using Vehicles "vehicles" and without using any myVehicles
        /// </summary>
        public Route CalcRoute(long[] nodes, Vehicle[] vehicles)
        {
            return CalcRoute(nodes, vehicles, new MyVehicle[0], 1);
        }

        /// <summary>
        /// Returns the route through points "nodes" using Vehicles "vehicles" and using MyVehicles "myVehicles"
        /// </summary>
        public Route CalcRoute(long[] nodes, Vehicle[] vehicles, MyVehicle[] myVehicles, int iterations)
        {
            Route res = null;
            Route r = null;
            double min = double.PositiveInfinity;

            foreach (MyVehicle v in myVehicles)
            {
                // Calc route to the MyVehicle
                Route toVehicle = RouteThrough(nodes[0], v.Location.ID, vehicles);
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
                        fromVehicle = RouteThrough(through, v.VehicleType);

                        //fromVehicle = CalcRoute(through, vehicles, myVehicles, iterations - 1);

                        /*fromVehicle = RouteThrough(AddArray(new long[] { v.Location.ID },
                        SubArray(nodes, 1, nodes.Length)), v.VehicleType);*/

                        // Route from source to destination using MyVehicle is
                        r = toVehicle + fromVehicle;
                    }
                }

                if (r != null && r.Time < min)
                {
                    res = r;
                    min = r.Time;
                }
            }


            r = RouteThrough(nodes, vehicles);
            if (r != null && r.Time < min)
                res = r;

            return res;
        }


        /// <summary>
        /// Returns the route through two points "n1" and "n2" using Vehicles "vehicles"
        /// </summary>
        private Route RouteThrough(long n1, long n2, Vehicle[] vehicles)
        {
            return RouteThrough(new long[] { n1, n2 }, vehicles);
        }

        /// <summary>
        /// Returns the route through points "nodes" using Vehicle "vehicle"
        /// </summary>
        private Route RouteThrough(long[] nodes, Vehicle vehicle)
        {
            return RouteThrough(nodes, new Vehicle[] { vehicle });
        }

        /// <summary>
        /// Returns the route through points "nodes" using Vehicles "vehicles"
        /// </summary>
        private Route RouteThrough(long[] nodes, Vehicle[] vehicles)
        {
            Route res = null;
            double min = double.PositiveInfinity;

            foreach (Vehicle v in vehicles)
            {
                Route r = null;

                for (int i = 0; i < nodes.Length - 1; i++)
                {
                   r += Dijkstra(nodes[i], nodes[i + 1], v);
                }

                if (r != null && r.Time < min)
                {
                    res = r;
                    min = r.Length;
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
        private Route Dijkstra(long from, long to, Vehicle v)
        {
            Route result = null;

            if (from == 0 || to == 0 || graph == null)
                return result;


            //set all nodeDistances on PositiveInfinity
            graph.ResetNodeDistance();

            Node source = graph.GetNode(from);
            Node destination = graph.GetNode(to);

            //the source is the start so it has no previous node
            source.Prev = null;

            //distance of the source-node is 0
            source.TentativeDist = 0;
            source.TrueDist = 0;

            //all nodes that are completely solved
            //SortedList<Node, long> solved = new SortedList<Node, long>(new NodeIDComparer());
            SortedList<long, Node> solved = new SortedList<long, Node>();


            //nodes that are encountered but not completely solved
            //SortedList<Node, double> unsolved = new SortedList<Node, double>(new NodeDistanceComparer());
            SortedList<double, Node> unsolved = new SortedList<double, Node>();

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

                        double distance = NodeCalcExtensions.Distance(start, end);
                        double speed = GetSpeed(v);
                        e.SetTime(distance / speed, v);

                        double time = current.TentativeDist + e.GetTime(v);
                        double trueDist = current.TrueDist + distance;
                        
                        if (!solved.ContainsValue(end) && current != end)
                        {
                            if (end.Latitude != 0 && end.Longitude != 0)
                            {
                                if (end.TentativeDist > time)
                                {
                                    end.TentativeDist = time;
                                    end.TrueDist = trueDist;
                                    end.Prev = current;

                                    if (!unsolved.ContainsValue(end))
                                        unsolved.Add(end.TentativeDist, end);
                                }
                            }
                        }
                        else if (!solved.ContainsValue(start) && current != start)
                        {
                            if (start.Latitude != 0 && start.Longitude != 0)
                            {
                                if (start.TentativeDist > time)
                                {
                                    start.TentativeDist = time;
                                    start.TrueDist = trueDist;
                                    start.Prev = current;

                                    if (!unsolved.ContainsValue(start))
                                        unsolved.Add(start.TentativeDist, start);
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
                Node n = destination;
                do
                {
                    nodes.Insert(0, n);
                    n = n.Prev;
                } while (n != null);

                result = new Route(nodes.ToArray(), v);
                result.Time = destination.TentativeDist;
                result.Length = destination.TrueDist;
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
                default:
                    return false;
            }
        }

        // geen goede benadering voor auto's!!!!
        /// <summary>
        /// Retuns the speed using vehicle v in metre/second.
        /// </summary>
        private double GetSpeed(Vehicle v)
        {
            switch (v)
            {
                case Vehicle.Car:
                case Vehicle.Bus:
                    return 22; // By assuming cars have a average speed of 80km/h.
                case Vehicle.Bicycle:
                    return 5.3; // Using Google Maps: 37,8km in 2h => 5,3m/s.
                case Vehicle.Foot:
                    return 1.4; // Documentation: http://ageing.oxfordjournals.org/content/26/1/15.full.pdf.
                default:
                    return 1.4;
            }
        }
    }
}

