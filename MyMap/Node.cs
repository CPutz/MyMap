using System;

namespace MyMap
{
    /// <summary>
    /// A point on the map with position (longitude, latitude) and an id.
    /// All streets and pieces of land are formed out of nodes.
    /// </summary>
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

        #endregion
    }


    public enum LocationType { BusStation, Parking };

    /// <summary>
    /// A Node with a specific type.
    /// </summary>
    public class Location : Node
    {
        private LocationType type;
        
        public Location(Node n, LocationType type) : base(n.Longitude, n.Latitude, n.ID)
        {
            this.type = type;
        }

        public LocationType Type
        {
            get { return type; }
        }
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
