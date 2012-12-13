using System;

namespace MyMap
{
    public class Edge
    {
        private Node start;
        private Node end;
        private double[] times;

        public Edge(Node start, Node end)
        {
            this.start = start;
            this.end = end;

            int numOfVehicles = Enum.GetNames(typeof(Vehicle)).Length;
            this.times = new double[numOfVehicles];
        }

        public double GetTime(Vehicle vehicle)
        {
            return times[(int)vehicle];
        }

        public void SetTime(double value, Vehicle vehicle)
        {
            times[(int)vehicle] = value;
        }

        public Node Start
        {
            get { return start; }
            set { start = value; }
        }

        public Node End
        {
            get { return end; }
            set { end = value; }
        }
    }
}
