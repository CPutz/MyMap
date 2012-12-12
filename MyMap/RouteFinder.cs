using System;
using System.Collections.Generic;

namespace MyMap
{
    public class RouteFinder
    {
        private Graph gr;

        public RouteFinder(Graph graph)
        {
            this.gr = graph;
        }

        /// <summary>
        /// Dijkstra in graph gr, from source to destination, using vehicle v.
        /// Returns the distance on success, or NaN on invalid arguments
        /// </summary>
        public Route Dijkstra(Node source, Node destination, Vehicle v)
        {
            Route result = null;

            source.Prev = null;
            source.TentativeDist = 0;

            if (source == null || destination == null || gr == null)
                return result;

            //TODO: op volgorde inserten in unsolvedNieghbours en zo efficientie verbeteren
            
            List<Node> solvedNodes = new List<Node>();

            //nodes that are encountered but not solved
            List<Node> unsolved = new List<Node>();
 
            Node current = source;
            Node prev = null;

            bool found = false;

            while (current != prev)
            {
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

                    if (!solvedNodes.Contains(e.End) && current != e.End)
                    {
                        if (e.End.TentativeDist > dist)
                        {
                            e.End.TentativeDist = dist;
                            e.End.Prev = current;

                            if (!unsolved.Contains(e.End))
                                //unsolvedNeighbours.Insert((long)(e.End.TentativeDist * 100000000), e.End);
                                unsolved.Add(e.End);
                        }
                    }
                    else if (!solvedNodes.Contains(e.Start) && current != e.Start)
                    {
                        if (e.Start.TentativeDist > dist)
                        {
                            e.Start.TentativeDist = dist;
                            e.Start.Prev = current;

                            if (!unsolved.Contains(e.Start))
                                //unsolvedNeighbours.Insert((long)(e.Start.TentativeDist * 100000000), e.Start);
                                unsolved.Add(e.Start);
                        }
                    }
                }
                solvedNodes.Add(current);

                prev = current;

                double smallest = double.PositiveInfinity;
                foreach (Node n in unsolved)
                {
                    if (n.TentativeDist <= smallest)
                    {
                        current = n;
                        smallest = n.TentativeDist;
                    }
                }

                //current = unsolvedNeighbours.GetSmallest();

                if (current != null)
                    //unsolvedNeighbours.Remove(current, (long)(current.TentativeDist * 100000000));
                    unsolved.Remove(current);
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
}

