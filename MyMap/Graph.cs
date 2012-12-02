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
                    Edge newEdge = new Edge(nodeList[i], nodeList[i + numOfPoints], "");
                    double time = (newEdge.End.Longitude - newEdge.Start.Longitude) * (newEdge.End.Longitude - newEdge.Start.Longitude) +
                                  (newEdge.End.Latitude - newEdge.Start.Latitude) * (newEdge.End.Latitude - newEdge.Start.Latitude);
                    foreach (Vehicle vehicle in Enum.GetValues(typeof(Vehicle)))
                    {
                        newEdge.SetTime(time, vehicle);
                    }
                    edgeList.Add(newEdge);
                }
                if (i % numOfPoints != numOfPoints - 1)
                {
                    Edge newEdge = new Edge(nodeList[i], nodeList[i + 1], "");
                    double time = (newEdge.End.Longitude - newEdge.Start.Longitude) * (newEdge.End.Longitude - newEdge.Start.Longitude) +
                                  (newEdge.End.Latitude - newEdge.Start.Latitude) * (newEdge.End.Latitude - newEdge.Start.Latitude);
                    foreach (Vehicle vehicle in Enum.GetValues(typeof(Vehicle)))
                    {
                        newEdge.SetTime(time, vehicle);
                    }
                    edgeList.Add(newEdge);
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





        //doet nu even dit maar gaat heel anders werken later
        public Node GetNodeByName(string s)
        {
            Node res = null;

            foreach (Edge edge in edges)
            {
                if (edge.Name == s)
                    return edge.Start;
            }

            return res;
        }


        /// <summary>
        /// returns the node that is the nearest to the position (longitude, latitude)
        /// </summary>
        public Node GetNodeByPos(double longitude, double latitude)
        {
            Node res = null;
            double min = 0;

            foreach (Node node in nodes)
            {
                if (node.Latitude * node.Latitude + node.Longitude * node.Longitude < min)
                    res = node;
            }

            return res;
        }


        /// <summary>
        /// returns the index of a node item in a sorted list of nodes by it's id
        /// the used method is binary searching
        /// </summary>
        private int IndexOfId(List<Node> sortedList, int id)
        {
            int min = 0;
            int max = sortedList.Count - 1;
            int mid = (min + max) / 2;

            while (max >= min)
            {
                if (sortedList[mid].ID > id)
                    max = mid - 1;
                else if (sortedList[mid].ID < id)
                    min = mid + 1;
                else
                    return mid;

                mid = (min + max) / 2;
            }

            return -1;
        }
    }
}

