using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyMap
{
    class Curve : Edge
    {
        //hier komt iig ook nog het type van zo'n curve
        //zoals snelweg, voetpad, etc.

        private Node[] nodes;

        public Curve(Node[] nodes, string name) : base(nodes[0], nodes[nodes.Length - 1], name)
        {
            this.nodes = nodes;
        }


        public Node this[int index]
        {
            get {
                if (index >= 0 && index < nodes.Length)
                    return nodes[index];
                else
                    return null;
            }
            set {
                if (index >= 0 && index < nodes.Length)
                    nodes[index] = value;
            }
        }
    }
}
