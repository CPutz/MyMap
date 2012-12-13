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

        /// <summary>
        /// Dijkstra in graph gr, from source to destination, using vehicle v.
        /// </summary>
        /// <param name="source"> the startpoint </param>
        /// <param name="destination"> the destination </param>
        /// <param name="v"> vehicle that is used </param>
        /// <returns></returns>
        public Route Dijkstra(Node source, Node destination, Vehicle v)
        {
            Route result = null;

            if (source == null || destination == null || gr == null)
                return result;


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

