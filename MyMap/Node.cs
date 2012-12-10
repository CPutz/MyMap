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

        public Node Prev
        {
            get { return prev; }
            set { prev = value; }
        }

        #endregion

    }
}
