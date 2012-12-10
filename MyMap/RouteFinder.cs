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

            if (source == null || destination == null || gr == null)
                //return double.NaN;
                return result;

            //TODO: op volgorde inserten in unsolvedNieghbours en zo efficientie verbeteren

            
            List<Node> solvedNodes = new List<Node>();
            List<Node> unsolvedNeighbours = new List<Node>();
 
            Node current = source;

            //moet voor elke node gedaan worden...
            //current.TentativeDist = 0;

            while (!solvedNodes.Contains(destination))
            {
                Node newcurrent = current;
                foreach (Edge e in gr.GetEdgesFromNode(current))
                {
                    if (!solvedNodes.Contains(e.End) && current != e.End && !unsolvedNeighbours.Contains(e.End))
                    {
                        if (e.End.TentativeDist < current.TentativeDist + e.GetTime(v))
                        {
                            unsolvedNeighbours.Add(e.End);
                           // unsolvedNeighbours[unsolvedNeighbours.IndexOf(e.End)].TentativeDist = e.Start.TentativeDist + e.GetTime(v);
                            e.End.TentativeDist = e.Start.TentativeDist + e.GetTime(v);
                            e.End.Prev = e.Start;
                        }
                    }
                    else if (!solvedNodes.Contains(e.Start) && current != e.Start && !unsolvedNeighbours.Contains(e.Start))
                    {
                        if (e.Start.TentativeDist < current.TentativeDist + e.GetTime(v))
                        {
                            unsolvedNeighbours.Add(e.Start);
                            //unsolvedNeighbours[unsolvedNeighbours.IndexOf(e.Start)].TentativeDist = e.End.TentativeDist + e.GetTime(v);
                            e.Start.TentativeDist = e.End.TentativeDist + e.GetTime(v);
                            e.Start.Prev = e.End;
                        }
                    }
                }
                solvedNodes.Add(current);

                double smallest = double.PositiveInfinity;
                foreach (Node n in unsolvedNeighbours)
                {
                    if (n.TentativeDist < smallest)
                    {
                        current = n;
                        smallest = n.TentativeDist;
                    }
                }

                if (unsolvedNeighbours.Contains(current))
                    unsolvedNeighbours.Remove(current);

            }

           // result = solvedNodes[solvedNodes.IndexOf(destination)].TentativeDist;

            List<Node> nodes = new List<Node>();
            Node prev = destination;
            do
            {
                nodes.Add(prev);
                prev = prev.Prev;
            } while (prev != null);

            result = new Route(nodes.ToArray());
            result.Length = destination.TentativeDist;

            return result;
        }
    }
}

