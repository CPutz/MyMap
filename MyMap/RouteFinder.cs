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
        public double Dijkstra(Node source, Node destination, Vehicle v)
        {
            if(source == null || destination == null || gr == null)
                return double.NaN;

            //TODO: op volgorde inserten in unsolvedNieghbours en zo efficientie verbeteren

            double result = 0;
            List<Node> solvedNodes = new List<Node>();
            List<Node> unsolvedNeighbours = new List<Node>();
 
            Node current = source;
            current.tentativeDist = 0;
            while (!solvedNodes.Contains(destination))
            {
                Node newcurrent = current;
                foreach (Edge e in gr.GetEdgesFromNode(current))
                {
                    if (!solvedNodes.Contains(e.End) && current != e.End && !unsolvedNeighbours.Contains(e.End))
                    {
                        unsolvedNeighbours.Add(e.End);
                        unsolvedNeighbours[unsolvedNeighbours.IndexOf(e.End)].tentativeDist = e.Start.tentativeDist + e.GetTime(v);
                    }
                    else if (!solvedNodes.Contains(e.Start) && current != e.Start && !unsolvedNeighbours.Contains(e.Start))
                    {
                        unsolvedNeighbours.Add(e.Start);
                        unsolvedNeighbours[unsolvedNeighbours.IndexOf(e.Start)].tentativeDist = e.End.tentativeDist + e.GetTime(v);
                    }
                }
                solvedNodes.Add(current);

                double smallest = double.PositiveInfinity;
                foreach (Node n in unsolvedNeighbours)
                {
                    if (n.tentativeDist < smallest)
                    {
                        current = n;
                        smallest = n.tentativeDist;
                    }
                }

                if (unsolvedNeighbours.Contains(current))
                    unsolvedNeighbours.Remove(current);

            }

            result = solvedNodes[solvedNodes.IndexOf(destination)].tentativeDist;
            return result;
        }
    }
}

