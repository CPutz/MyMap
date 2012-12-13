using System;
using System.Collections.Generic;

namespace MyMap
{
    public class RouteFinder
    {
        private Graph gr;

        public RouteFinder(Graph graph)
        {
            //save the graph
            this.gr = graph;
        }


        public Node[] SubArray(Node[] array, int start, int end)
        {
            Node[] res = null;

            if (end - start > 0 && start > 0 && end < array.Length)
            {
                res = new Node[end - start];
                Array.Copy(array, start, res, 0, end - start);
            }

            return res;
        }


        /// <summary>
        /// Returns the route through points "nodes" using Vehicles "vehicles" and without using any myVehicles
        /// </summary>
        public Route CalcRoute(Node[] nodes, Vehicle[] vehicles)
        {
            return CalcRoute(nodes, vehicles, new MyVehicle[0]);
        }

        /// <summary>
        /// Returns the route through points "nodes" using Vehicles "vehicles" and using MyVehicles "myVehicles"
        /// </summary>
        public Route CalcRoute(Node[] nodes, Vehicle[] vehicles, MyVehicle[] myVehicles)
        {
            Route res = null;
            Route r = null;
            double min = double.PositiveInfinity;

            foreach (MyVehicle v in myVehicles)
            {
                // Calc route to the MyVehicle
                Route toVehicle = RouteThrough(nodes[0], v.Location, vehicles);
                Route fromVehicle = null;
                
                v.Route = toVehicle;

                if (!Double.IsPositiveInfinity(toVehicle.Length))
                {
                    // Calc route from MyVehicle through the given points
                    fromVehicle = RouteThrough(SubArray(nodes, 1, nodes.Length), v.VehicleType);

                    // Route from source to destination using MyVehicle is
                    r = toVehicle + fromVehicle;
                }

                if (r != null && r.Length < min)
                {
                    res = r;
                    min = r.Length;
                }
            }


            r = RouteThrough(nodes, vehicles);
            if (r != null && r.Length < min)
                res = r;

            return res;
        }


        /// <summary>
        /// Returns the route through two points "n1" and "n2" using Vehicles "vehicles"
        /// </summary>
        private Route RouteThrough(Node n1, Node n2, Vehicle[] vehicles)
        {
            return RouteThrough(new Node[] { n1, n2 }, vehicles);
        }

        /// <summary>
        /// Returns the route through points "nodes" using Vehicle "vehicle"
        /// </summary>
        private Route RouteThrough(Node[] nodes, Vehicle vehicle)
        {
            return RouteThrough(nodes, new Vehicle[] { vehicle });
        }

        /// <summary>
        /// Returns the route through points "nodes" using Vehicles "vehicles"
        /// </summary>
        private Route RouteThrough(Node[] nodes, Vehicle[] vehicles)
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

                if (r != null && r.Length < min)
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
        private Route Dijkstra(Node source, Node destination, Vehicle v)
        {
            Route result = null;

            if (source == null || destination == null || gr == null)
                return result;


            //set all nodeDistances on PositiveInfinity
            gr.ResetNodeDistance();


            //the source is the start so it has no previous node
            source.Prev = null;

            //distance of the source-node is 0
            source.TentativeDist = 0;

            //all nodes that are completely solved
            SortedList<Node, long> solved = new SortedList<Node, long>(new NodeComparer());

            //nodes that are encountered but not completely solved
            SortedList<Node, double> unsolved = new SortedList<Node, double>(new NodeComparer());
            

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

                foreach (Edge e in gr.GetEdgesFromNode(current))
                {


                    //zeer tijdelijk!!!
                    e.SetTime(Math.Sqrt((e.Start.Longitude - e.End.Longitude) * (e.Start.Longitude - e.End.Longitude) + (e.Start.Latitude - e.End.Latitude) * (e.Start.Latitude - e.End.Latitude)), v);


                    double dist = current.TentativeDist + e.GetTime(v);

                    if (!solved.ContainsKey(e.End) && current != e.End)
                    {
                        if (e.End.TentativeDist > dist)
                        {
                            e.End.TentativeDist = dist;
                            e.End.Prev = current;

                            if (!unsolved.ContainsKey(e.End))
                                unsolved.Add(e.End, e.End.TentativeDist);
                        }
                    }
                    else if (!solved.ContainsKey(e.Start) && current != e.Start)
                    {
                        if (e.Start.TentativeDist > dist)
                        {
                            e.Start.TentativeDist = dist;
                            e.Start.Prev = current;

                            if (!unsolved.ContainsKey(e.Start))
                                unsolved.Add(e.Start, e.Start.TentativeDist);
                        }
                    }
                }
                solved.Add(current, current.ID);

                if (unsolved.Count > 0)
                {
                    current = unsolved.Keys[0];
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
                    nodes.Add(n);
                    n = n.Prev;
                } while (n != null);

                result = new Route(nodes.ToArray());
                result.Length = destination.TentativeDist;
            }
            
            return result;
        }
    }

    public class NodeComparer : IComparer<Node>
    {
        public int Compare(Node A, Node B)
        {
            return (int)(A.ID - B.ID);
        }
    }
}

