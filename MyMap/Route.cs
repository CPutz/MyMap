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

        //is deze nodig?
        public IEnumerator<Node> GetEnumerator() {
            foreach (Node node in route)
                yield return node;
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

        public double Length
        {
            get { return length; }
            set { length = value; }
        }

        public int NumOfNodes
        {
            get { return route.Length; }
        }
    }
}
