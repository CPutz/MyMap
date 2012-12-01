using System;

namespace MyMap
{
    public class Node
    {
        private double longitude;
        private double latitude;
        private long id;

        public Node(double longitude, double latitude, long id)
        {
            this.longitude = longitude;
            this.latitude = latitude;
            this.id = id;
        }

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

        public double tentativeDist;


    }
}
