using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyMap
{
    /// <summary>
    /// BoundingBox object that holds 4 doubles for the upperLeft and bottomRight corners.
    /// </summary>
    public class BBox
    {
        private double xMin, yMin, xMax, yMax;

        public BBox(double x1, double y1, double x2, double y2)
        {
            this.xMin = Math.Min(x1, x2);
            this.xMax = Math.Max(x1, x2);
            this.yMin = Math.Min(y1, y2);
            this.yMax = Math.Max(y1, y2);
        }

        #region properties

        public double XMin
        {
            get { return xMin; }
            set
            {
                if (value > xMax)
                {
                    xMin = xMax;
                    xMax = value;
                }
                else
                {
                    xMin = value;
                }
            }
                    
        }
        public double YMin
        {
            get { return yMin; }
            set
            {
                if (value > yMax)
                {
                    yMin = yMax;
                    yMax = value;
                }
                else
                {
                    yMin = value;
                }
            }
        }
        public double XMax
        {
            get { return xMax; }
            set
            {
                if (value < xMin)
                {
                    xMax = xMin;
                    xMin = value;
                }
                else
                {
                    xMax = value;
                }
            }
        }
        public double YMax
        {
            get { return yMax; }
            set
            {
                if (value < yMin)
                {
                    yMax = yMin;
                    yMin = value;
                }
                else
                {
                    yMax = value;
                }
            }
        }

        public double Width
        {
            get { return xMax - xMin; }
        }

        public double Height
        {
            get { return yMax - yMin; }
        }

        #endregion

        /// <summary>
        /// Returns true if the point (x,y) is in the BoundingBox.
        /// </summary>
        public bool Contains(double x, double y)
        {
            if (x >= xMin && y >= yMin && x <= xMax && y <= yMax)
                return true;
            return false;
        }

        /// <summary>
        /// Return true if the boundingbox intersects with "box".
        /// </summary>
        public bool IntersectWith(BBox box)
        {
            return box.Contains(xMin, yMin) || box.Contains(xMin, yMax) || 
                   box.Contains(xMax, yMin) || box.Contains(yMax, yMax) ||
                   this.Contains(box.XMin, box.YMin) || this.Contains(box.XMin, box.YMax) || 
                   this.Contains(box.XMax, box.YMin) || this.Contains(box.XMax, box.YMax);
        }

        /// <summary>
        /// Moves the boundingbox with dx in X direction and dy in Y direction
        /// and then returns itself.
        /// </summary>
        public BBox Offset(double dx, double dy)
        {
            this.xMin += dx;
            this.xMax += dx;
            this.yMin += dy;
            this.yMax += dy;
            return this;
        }


        public double XFraction(double x)
        {
            return (x - xMin) / Width;
        }

        public double YFraction(double y)
        {
            return (y - yMin) / Height;
        }


        // Checks if two boundingboxes are exactly the same.
        public static bool operator ==(BBox A, BBox B)
        {
            return A.XMin == B.XMin && A.YMin == B.YMin && A.XMax == B.XMax && A.YMax == B.YMax;
        }
        public static bool operator !=(BBox A, BBox B)
        {
            return A.XMin != B.XMin || A.YMin != B.YMin || A.XMax != B.XMax || A.YMax != B.YMax;
        }


        /// <summary>
        /// Resizes a BBox by a factor so that the middle stays
        /// at the same location.
        /// </summary>
        public static BBox getResizedBBox(BBox box, double factor)
        {
            BBox resizedBBox = new BBox(box.XMin, box.YMin, box.XMax, box.YMax);
            resizedBBox.XMin -= (factor - 1) * box.Width / 2;
            resizedBBox.XMax += (factor - 1) * box.Width / 2;
            resizedBBox.YMin -= (factor - 1) * box.Height / 2;
            resizedBBox.YMax += (factor - 1) * box.Height / 2;
            return resizedBBox;
        }
    }
}
