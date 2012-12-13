using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyMap
{
    public class Route
    {
        private Node[] route;
        private double length;

        //other properties like what vehicles are used


        public Route(Node[] nodes)
        {
            this.route = nodes;
        }

        public Node this[int index] {
            get
            {
                if (index >= 0 && index < route.Length)
                    return route[index];
                else
                    return null;
                }
        }

        public Node[] Points
        {
            get { return route; }
        }

        public double Length
        {
            get { return length; }
            set { length = value; }
        }

        public int Count
        {
            get { return route.Length; }
        }


        public static Route operator +(Route A, Route B)
        {
            if (A == null)
                return B;
            else if (B == null)
                return A;
            else
            {
                Node[] nodes = new Node[A.Count + B.Count];
                A.Points.CopyTo(nodes, 0);
                B.Points.CopyTo(nodes, A.Count);
                return new Route(nodes);
            }
        }
    }
}
