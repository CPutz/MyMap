using System;
using System.Drawing;

namespace MyMap
{
    /// <summary>
    /// Calculates points on the map using Mercator projection.
    /// </summary>
    public class Projection
    {
        private const double ToRad = Math.PI / 180;
        private const double ToDeg = 180 / Math.PI;
        private const double R_MAJOR = 637813.70;
        private const double R_MINOR = 635675.23142;
        private static readonly double RATIO = R_MINOR / R_MAJOR;
        private static readonly double ECCENT = Math.Sqrt(1.0 - (RATIO * RATIO));
        private static readonly double COM = 0.5 * ECCENT;
        private static readonly double PI_2 = Math.PI / 2.0;

        private double zoom;

        public Projection(double orgWidth, int projWidth)
        {
            //this.zoom = projWidth / (2 * Math.PI * orgWidth);
            this.zoom = projWidth / DegToRad(orgWidth);
        }

        /// <summary>
        /// Returns a Point from coordinate c using the Mercator Projection.
        /// Documentation: http://en.wikipedia.org/wiki/Mercator_projection
        /// </summary>
        /*public static Point CoordToPoint(Coordinate c)
        {
            int x = (int)(R_MAJOR * DegToRad(c.Longitude));
            //int y = (int)(R_MAJOR * Math.Log(Math.Tan((Math.PI * c.Latitude / 360) + (Math.PI / 4))));
            c.Latitude = Math.Min(89.5, Math.Max(c.Latitude, -89.5));
            double phi = DegToRad(c.Latitude);
            double sinphi = Math.Sin(phi);
            double con = ECCENT * sinphi;
            con = Math.Pow(((1.0 - con) / (1.0 + con)), COM);
            double ts = Math.Tan(0.5 * ((Math.PI * 0.5) - phi)) / con;
            int y = (int)(0 - R_MAJOR * Math.Log(ts));
            return new Point(x, y);
        }*/

        public Point CoordToPoint(Coordinate c)
        {
            int x = (int)(zoom * DegToRad(c.Longitude));
            int y = (int)(zoom * Math.Log(Math.Tan((DegToRad(c.Latitude) / 2) + (Math.PI / 4))));
            return new Point(x, y);
        }


        public Coordinate PointToCoord(Point p)
        {
            double longitude = RadToDeg(p.X / zoom);
            double latitude = RadToDeg(2 * Math.Atan(Math.Exp(p.Y / zoom)) - Math.PI / 2);

            /*double ts = Math.Exp(-p.Y / R_MAJOR);
            double phi = PI_2 - 2 * Math.Atan(ts);
            double dphi = 1.0;
            int i = 0;
            while ((Math.Abs(dphi) > 0.000000001) && (i < 15))
            {
                double con = ECCENT * Math.Sin(phi);
                dphi = PI_2 - 2 * Math.Atan(ts * Math.Pow((1.0 - con) / (1.0 + con), COM)) - phi;
                phi += dphi;
                i++;
            }
            double latitude = RadToDeg(phi);*/


            return new Coordinate(longitude, latitude);
        }

        private static double RadToDeg(double rad) {
            return rad * ToDeg;
        }
        private static double DegToRad(double deg) {
            return deg * ToRad;
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
