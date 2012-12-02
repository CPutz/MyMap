using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyMap
{
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
        /// returns true if the point (x,y) is in the BoundingBox
        /// </summary>
        public bool Contains(double x, double y)
        {
            if (x >= xMin && y >= yMin && x <= xMax && y <= yMax)
                return true;
            return false;
        }
    }
}
