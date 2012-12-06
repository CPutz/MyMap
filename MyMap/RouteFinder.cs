using System;
using System.Collections.Generic;

namespace MyMap
{
    public class RouteFinder
    {
        public RouteFinder()
        {

        }

        /// <summary>
        /// Dijkstra in graph gr, from source to destination, using vehicle v.
        /// Returns the distance on success, or NaN on invalid arguments
        /// </summary>
        public double Dijkstra(Graph gr, Node source, Node destination, Vehicle v)
        {
            if(source == null || destination == null || gr == null)
                return double.NaN;

            double result = 0;
            List<Node> solvedNodes = new List<Node>();
 
            Node current = source;
            current.tentativeDist = 0;
            while (!solvedNodes.Contains(destination))
            {
                List<Node> unsolvedNeighbors = new List<Node>();

                Node newcurrent = current;
                foreach (Edge e in gr.GetEdgesFromNode(current))
                {
                    if (!solvedNodes.Contains(e.End) && current != e.End)
                    {
                        unsolvedNeighbors.Add(e.End);
                        unsolvedNeighbors[unsolvedNeighbors.IndexOf(e.End)].tentativeDist = e.Start.tentativeDist + e.GetTime(v);
                    }
                    else if (!solvedNodes.Contains(e.Start) && current != e.Start)
                    {
                        unsolvedNeighbors.Add(e.Start);
                        unsolvedNeighbors[unsolvedNeighbors.IndexOf(e.Start)].tentativeDist = e.End.tentativeDist + e.GetTime(v);
                    }
                }
                solvedNodes.Add(current);
                double smallest = double.PositiveInfinity;
                foreach (Node n in unsolvedNeighbors)
                {
                    if (n.tentativeDist < smallest)
                    {
                        current = n;
                        smallest = n.tentativeDist;
                    }
                }
                
              
                
            }

            result = solvedNodes[solvedNodes.IndexOf(destination)].tentativeDist;
            return result;



        }
    }
}

