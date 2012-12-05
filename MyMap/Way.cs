using System;

namespace MyMap
{
    public class Way
    {
        public Node Start;
        public Node End;
        public Way (Node n1, Node n2)
        {
            Start = n1;
            End = n2;
        }
    }
}

