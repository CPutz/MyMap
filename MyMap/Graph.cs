using System;
using System.Collections.Generic;

namespace MyMap
{
    public class Graph
    {
        Edge[] edges;
        Node[] nodes;

        public Graph()
        {
            CreateFakeEdges();
        }


        //temporary function for testing
        //sets up a network of random edges
        private void CreateFakeEdges()
        {
            Random rand = new Random();
            int width = 500, height = 500;
            int id = 0;
            int numOfPoints = 3;
            int d = (int)((Math.Min(width, height)) / (numOfPoints - 1));

            List<Node> nodeList = new List<Node>();
            List<Edge> edgeList = new List<Edge>();

            for (int x = 0; x < d * numOfPoints; x += d)
            {
                for (int y = 0; y < d * numOfPoints; y += d)
                {
                    nodeList.Add(new Node(x + rand.Next(-d / 2, d / 2), y + rand.Next(-d / 2, d / 2), id));
                    id++;
                }
            }

            for (int i = 0; i < nodeList.Count; i++)
            {
                if (i < nodeList.Count - numOfPoints)
                {
                    edgeList.Add(new Edge(nodes[i], nodes[i + numOfPoints], ""));
                }
                if (i % numOfPoints != numOfPoints - 1)
                {
                    edgeList.Add(new Edge(nodes[i], nodes[i + 1], ""));
                }
            }

            nodes = nodeList.ToArray();
            edges = edgeList.ToArray();
        }


        public Edge[] GetEdgesFromNode(Node node)
        {
            List<Edge> edgeList = new List<Edge>();

            //zeker niet definitieve versie
            //moet slimmer kunnen :P
            foreach (Edge edge in edges)
            {
                if (edge.Start == node || edge.End == node)
                    edgeList.Add(edge);
            }

            return edgeList.ToArray();
        }
    }
}

