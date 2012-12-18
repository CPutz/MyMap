using System;
using System.Drawing;

namespace MyMap
{
    /// <summary>
    /// Calculates points on the map using Mercator projection.
    /// </summary>
    public class Projection
    {
        private double zoom;

        public Projection(double orgWidth, int projWidth)
        {
            this.zoom = projWidth / orgWidth;
        }

        /// <summary>
        /// Returns a Point from coordinate c using the Mercator Projection.
        /// Documentation: http://en.wikipedia.org/wiki/Mercator_projection
        /// </summary>
        public Point CoordToPoint(Coordinate c)
        {
            int x = (int)(zoom * c.Longitude);
            int y = (int)(zoom * Math.Log(Math.Tan((c.Latitude / 2) + (Math.PI / 4))));
            return new Point(x, y);
        }


        public Coordinate PointToCoord(Point p)
        {
            double longitude = p.X / zoom;
            double latitude = 2 * Math.Atan(Math.Pow(Math.E, p.Y / zoom)) - Math.PI / 2;
            return new Coordinate(longitude, latitude);
        }
    }

    public class Coordinate
    {
        private double longitude;
        private double latitude;

        public Coordinate(double longitude, double latitude)
        {
            this.longitude = longitude;
            this.latitude = latitude;
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
    }
}
