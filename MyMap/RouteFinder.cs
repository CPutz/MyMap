using System;
using System.Collections.Generic;

namespace MyMap
{
    public class RouteFinder
    {
        public RouteFinder()
        {

        }

        public double Dijkstra(Graph gr, Node source, Node destination, Vehicle v)
        {
            double result = 0;
            List<Node> tempNodes = new List<Node>();
            tempNodes.AddRange(gr.nodes);
            List<Node> unvisited = new List<Node>();
            unvisited.AddRange(tempNodes);
            foreach (Node n in tempNodes)
            {
                n.tentativeDist = 10000000000000;
            }
            Node current = tempNodes[tempNodes.IndexOf(source)];
            current.tentativeDist = 0;
            while (unvisited.Contains(destination))
            {
                List<Edge> unvisitedNeighbors = new List<Edge>();
                foreach (Edge e in gr.GetEdgesFromNode(current))
                {
                    if (unvisited.Contains(e.End))
                    {
                        unvisited[unvisited.IndexOf(e.End)].tentativeDist = e.GetTime(v) + current.tentativeDist;
                    }
                    else if (unvisited.Contains(e.Start))
                    {
                        unvisited[unvisited.IndexOf(e.Start)].tentativeDist = e.GetTime(v) + current.tentativeDist;
                    }
                }
                result = unvisited[unvisited.IndexOf(destination)].tentativeDist;
                unvisited.Remove(current);
                double smallest = 10000000000000000000;
                foreach (Node n in unvisited)
                {
                    if (n.tentativeDist < smallest)
                    {
                        smallest = n.tentativeDist;
                        current = unvisited[unvisited.IndexOf(n)];
                    }
                }
                
            }

            return result;



        }
    }
}

