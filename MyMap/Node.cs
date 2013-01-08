using System;

namespace MyMap
{
    public class Node
    {
        private double longitude;
        private double latitude;
        private long id;

        //used by Dijkstra Algorithm
        private double tentativeDist;
        private double trueDist;
        private Node prev;


        public Node(double longitude, double latitude, long id)
        {
            this.longitude = longitude;
            this.latitude = latitude;
            this.id = id;
        }

        #region properties

        public double Longitude
        {
            get { return longitude; }
            set { longitude = value; }
        }

        public double Latitude
        {
            get { return latitude; }
            set { latitude = value; }
        }

        public long ID
        {
            get { return id; }
            set { id = value; }
        }

        public double TentativeDist
        {
            get { return tentativeDist; }
            set { tentativeDist = value; }
        }

        public double TrueDist
        {
            get { return trueDist; }
            set { trueDist = value; }
        }

        public Node Prev
        {
            get { return prev; }
            set { prev = value; }
        }

        #endregion
    }


    public class NodeCalcExtensions
    {
        private const double EARTH_RADIUS = 6371009; //Earth radius in metre.
        private const double TO_RADIANS = Math.PI / 180;


        /// <summary>
        /// Returns the distance between two nodes A and B in kilometre using the great-circle distance.
        /// Documentation: http://en.wikipedia.org/wiki/Great-circle_distance
        /// </summary>
        public static double Distance(Node A, Node B)
        {
            double labda1 = A.Longitude * TO_RADIANS;
            double phi1 = A.Latitude * TO_RADIANS;
            double labda2 = B.Longitude * TO_RADIANS;
            double phi2 = B.Latitude * TO_RADIANS;

            return EARTH_RADIUS * Math.Acos(Math.Sin(phi1) * Math.Sin(phi2) + Math.Cos(phi1) * Math.Cos(phi2) * Math.Cos(labda2 - labda1));
        }
    }
}
