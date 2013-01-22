using System;

namespace MyMap
{
    /// <summary>
    /// A connection between two nodes.
    /// Edges are used by the RouteFinder class because they
    /// are easier to handle than curves because curves hold more
    /// than two nodes.
    /// It contains the name and maxSpeed of the Curve if available.
    /// </summary>
    public class Edge
    {
        private long start;
        private long end;
        private CurveType type;
        private string name;
        private int maxSpeed;
        private Route route;


        public Edge(long start, long end)
        {
            this.start = start;
            this.end = end;
        }


        #region Properties

        public long Start
        {
            get { return start; }
            set { start = value; }
        }

        public long End
        {
            get { return end; }
            set { end = value; }
        }

        public Route Route
        {
            get { return route; }
            set { route = value; }
        }

        public int MaxSpeed
        {
            get { return maxSpeed; }
            set { maxSpeed = value; }
        }

        public CurveType Type
        {
            get { return type; }
            set { type = value; }
        }

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        #endregion
    }
}
