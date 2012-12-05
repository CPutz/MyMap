using System;
using System.Collections.Generic;

namespace MyMap
{
    public class Graph
    {
        RBTree edges = new RBTree();
        RBTree nodes = new RBTree();

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


            for (int x = 0; x < d * numOfPoints; x += d)
            {
                for (int y = 0; y < d * numOfPoints; y += d)
                {
                    edges.Insert(id, new Node(x + rand.Next(-d / 2, d / 2), y + rand.Next(-d / 2, d / 2), id));
                    id++;
                }
            }

            for (int i = 0; i < edges.Count; i++)
            {
                if (i < nodes.Count - numOfPoints)
                {
                    Edge newEdge = new Edge((Node)nodes.GetNode(i),
                                            (Node)nodes.GetNode(i + numOfPoints), "");
                    double time = (newEdge.End.Longitude - newEdge.Start.Longitude) * (newEdge.End.Longitude - newEdge.Start.Longitude) +
                                  (newEdge.End.Latitude - newEdge.Start.Latitude) * (newEdge.End.Latitude - newEdge.Start.Latitude);
                    foreach (Vehicle vehicle in Enum.GetValues(typeof(Vehicle)))
                    {
                        newEdge.SetTime(time, vehicle);
                    }
                    edges.Insert(newEdge.Start.ID, newEdge);
                    edges.Insert(newEdge.End.ID, newEdge);
                }
                if (i % numOfPoints != numOfPoints - 1)
                {
                    Edge newEdge = new Edge((Node)nodes.GetNode(i),
                                            (Node)nodes.GetNode(i + 1), "");
                    double time = (newEdge.End.Longitude - newEdge.Start.Longitude) * (newEdge.End.Longitude - newEdge.Start.Longitude) +
                                  (newEdge.End.Latitude - newEdge.Start.Latitude) * (newEdge.End.Latitude - newEdge.Start.Latitude);
                    foreach (Vehicle vehicle in Enum.GetValues(typeof(Vehicle)))
                    {
                        newEdge.SetTime(time, vehicle);
                    }
                    edges.Insert(newEdge.Start.ID, newEdge);
                    edges.Insert(newEdge.End.ID, newEdge);
                }
            }
        }


        //tijdelijk
        public Node[] GetEdgesFromNode(Node node)
        {
            return ((List<Node>)edges.GetNode(node.ID)).ToArray();
        }


        //tijdelijk
        public Curve[] GetCurvesInBBox(BBox box)
        {
            List<Curve> curves = new List<Curve>();

            foreach (Edge edge in edges)
            {
                if (box.Contains(edge.Start.Longitude, edge.Start.Latitude) || box.Contains(edge.End.Longitude, edge.End.Latitude))
                {
                    Node[] nds = { edge.Start, edge.End };
                    Curve newCurve = new Curve(nds, edge.Name);
                    newCurve.Type = CurveType.Street;
                    curves.Add(newCurve);
                }
            }

            return curves.ToArray();
        }


        /*
         * Hoezo zijn er een GetCurvesInBBox en een GetNodesInBBox? 1 van de twee moet voldoen...
         */
        public Node[] GetNodesInBBox(BBox box)
        {
            throw new NotImplementedException();
            List<Node> nds = new List<Node>();

            foreach (Node nd in nodes)
            {
                if (box.Contains(nd.Longitude, nd.Latitude))
                {
                    nds.Add(nd);
                }
            }

            return nds.ToArray();
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
        /// Returns the index of a node item in a sorted list of nodes by it's id
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

