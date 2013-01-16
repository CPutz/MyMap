using System;

namespace MyMap
{
    public class Edge
    {
        private long start;
        private long end;
        private int maxSpeed;
        private Route route;

        public CurveType Type;

        //tijdelijk voor testen...
        public string name;


        public Edge(long start, long end)
        {
            this.start = start;
            this.end = end;

            int numOfVehicles = Enum.GetNames(typeof(Vehicle)).Length;
            //this.times = new double[numOfVehicles];
        }

       /* public double GetTime(Vehicle vehicle)
        {
            return times[(int)vehicle];
        }

        public void SetTime(double value, Vehicle vehicle)
        {
            times[(int)vehicle] = value;
        }*/


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

        /*public double Time
        {
            get { return time; }
            set { time = value; }
        }*/

        #endregion
    }
}
