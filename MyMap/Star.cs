using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;
using System.Threading;
using System.Drawing.Drawing2D;


namespace MyMap
{
    class Star
    {
        Point[] starPoints;
        string name;
        public double Radius
        {
            get
            {
                return radius;
            }
            set
            {
                radius = value;
                starPoints = getStarPoints();
            }
        }
        double radius;
        public Star(string name)
        {
            Radius = 100;
            this.name = name;
        }
        public void Draw(Graphics gr, Point center, double textSizePercentage, bool showName)
        {
            gr.TranslateTransform(center.X, center.Y);
            // center of logo is now considered (0,0)
            gr.FillPolygon(Brushes.Yellow, new Point[] { starPoints[0], starPoints[2], starPoints[4], starPoints[1], starPoints[3] }, FillMode.Winding);
            int r = (int)radius;
            //gr.DrawEllipse(Pens.Black,- r, - r, 2 * r, 2 * r);
            if (showName)
            {
                Font font = new Font("Tahoma", (float) (textSizePercentage * Radius / 500));
                SizeF sizeF = gr.MeasureString(name, font);
                gr.DrawString(name, font, Brushes.Black, new PointF(-sizeF.Width / 2, -sizeF.Height / 2));
            }
            gr.TranslateTransform(-center.X, -center.Y);

        }
        Point[] getStarPoints()
        {
            Point[] starPoints = new Point[5];
            for (int i = 0; i < starPoints.Count(); i++)
            {
                double x, y;
                x = (radius * Math.Cos(-Math.PI / 2 - i * 2 * Math.PI / starPoints.Count()));
                y = (radius * Math.Sin(-Math.PI / 2 - i * 2 * Math.PI / starPoints.Count()));
                starPoints[i] = new Point((int)x, (int)y);
            }
            return starPoints;
        }
    }
}
