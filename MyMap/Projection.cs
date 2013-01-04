using System;
using System.Drawing;

namespace MyMap
{
    /// <summary>
    /// Calculates points on the map using Mercator projection.
    /// </summary>
    public class Projection
    {
        private const double TO_RADIANS = Math.PI / 180;
        private const double TO_DEGREE = 180 / Math.PI;
        private const double PI_OVER_TWO = Math.PI / 2;

        private double zoom;
        private Coordinate cCorner;
        private Point pCorner;


        /// <summary>
        /// Calculates the zoom level for the projection.
        /// The corner is the upper left corner of the map.
        /// </summary>
        public Projection(double orgWidth, int projWidth, Coordinate corner)
        {
            //this.zoom = projWidth / (2 * Math.PI * orgWidth);
            this.zoom = projWidth / DegToRad(orgWidth);
            //this.cCorner = corner;
            //this.pCorner = CoordToPoint(cCorner);
        }

        /// <summary>
        /// Returns a Point from coordinate c using the Mercator Projection.
        /// Documentation: http://en.wikipedia.org/wiki/Mercator_projection
        /// </summary>
        public Point CoordToPoint(Coordinate c)
        {
            int x = (int)(zoom * DegToRad(c.Longitude));
            int y = (int)(zoom * Math.Log(Math.Tan((DegToRad(c.Latitude) / 2) + PI_OVER_TWO / 2)));
            return new Point(x - pCorner.X, y - pCorner.Y);
        }

        /// <summary>
        /// Returns a coordinate from a Point p on the map using the Mercator Projection.
        /// Documentation: http://en.wikipedia.org/wiki/Mercator_projection
        /// </summary>
        public Coordinate PointToCoord(Point p)
        {
            p.Offset(pCorner);
            double longitude = RadToDeg(p.X / zoom);
            double latitude = RadToDeg(2 * Math.Atan(Math.Exp(p.Y / zoom)) - PI_OVER_TWO);
            return new Coordinate(longitude, latitude);
        }

        // converts an angle from radians to degrees.
        private double RadToDeg(double rad) {
            return rad * TO_DEGREE;
        }
        // converts an angle from degrees to radians.
        private double DegToRad(double deg) {
            return deg * TO_RADIANS;
        }


        public Coordinate Corner {
            set { cCorner = value;
                  pCorner = CoordToPoint(cCorner); }
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
