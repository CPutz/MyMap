using System;
using System.Collections.Generic;

namespace MyMap
{
    public class RouteFinder
    {
        public RouteFinder()
        {

        }

        public void Dijkstra(Graph gr, Node source, Node destination, Vehicle v)
        {
            List<Node> tempNodes = new List<Node>();
            tempNodes.AddRange(gr.nodes);
            List<Node> unvisited = new List<Node>();
            unvisited.AddRange(tempNodes);
            unvisited.Remove(source);
            foreach (Node n in tempNodes)
            {
                n.tentativeDist = 999999;
            }
            Node current = tempNodes[tempNodes.IndexOf(source)];

            while (unvisited.Contains(destination))
            {
                List<Edge> unvisitedNeighbors = new List<Edge>();
                foreach (Edge e in gr.GetEdgesFromNode(current))
                {
                    if (unvisited.Contains(e.End))
                    {
                        unvisited[unvisited.IndexOf(e.Start)].tentativeDist = e.GetTime(v) + current.tentativeDist;
                    }
                    else if (unvisited.Contains(e.Start))
                    {
                        unvisited[unvisited.IndexOf(e.Start)].tentativeDist = e.GetTime(v) + current.tentativeDist;
                    }
                }

                unvisited.Remove(current);
                double smallest = 999999;
                foreach (Node n in unvisited)
                {
                    if (n.tentativeDist < smallest)
                    {
                        smallest = n.tentativeDist;
                        current = n;
                    }
                }
            }



        }
    }
}

