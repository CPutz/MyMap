using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;
using System.Threading;

namespace logo
{
    class AllstarsLogo : Control
    {
        public bool Transparant, StillLoading;
        Thread loadingThread;
        public Point Center
        {
            get
            {
                if (Height > Width)
                    return new Point(Width / 2, Width / 2);
                else
                    return new Point(Height / 2, Height / 2);
            }
        }
        public int Radius
        {
            get
            {
                if (Height > Width)
                    return Width / 2 - 1;
                else
                    return Height / 2 - 1;
            }
        }
        double degrees = 0;
        List<Star> stars = new List<Star>();
        double centerStarToCenterLogo
        {
            get
            {
                return (double) (2 * Radius) / 3;
            }
        }
        public AllstarsLogo(bool Transparant)
        {
            this.Transparant = Transparant;
            this.Paint += Draw;
            this.DoubleBuffered = true;
            this.Resize += resize;
            createStars();            
        }
        void createStars()
        {
            stars.Clear();
            stars.Add(new Star("Angelo"));
            stars.Add(new Star("Casper"));
            stars.Add(new Star("Chiel"));
            stars.Add(new Star("Daan"));
            stars.Add(new Star("Sophie"));
            createRadiusStars();
        }
        void createRadiusStars()
        {
            double starRadius = (double)Radius / 3;
            foreach (Star star in stars)
            {
                star.Radius = starRadius;
            }
        }
        void resize(object o, EventArgs ea)
        {
            createRadiusStars();
            bool b = true;
        }
        public void Start()
        {
            loadingThread = new Thread(loading);
            StillLoading = true;
            loadingThread.Start();
        }
        void loading()
        {
            while (StillLoading)
            {
                Rotate();
                this.Invalidate();
                Thread.Sleep(50);
            }
        }
        void Rotate()
        {
            degrees = (degrees - Math.PI / 40) % (2 * Math.PI);
        }
        public void Stop()
        {
            StillLoading = false;
            this.Invalidate();
        }
        public void Draw(object o, PaintEventArgs pea)
        {
            Graphics gr = pea.Graphics;
            if (!Transparant)
            {
                gr.FillRectangle(Brushes.White, 0, 0, Width, Height);
            }
            //gr.DrawEllipse(Pens.Black, Center.X - Radius, Center.Y - Radius, 2 * Radius, 2 * Radius);
            for(int i = 0; i < stars.Count(); i++)
            {
                double dx, dy;
                dx = centerStarToCenterLogo * Math.Sin(degrees + i * 2 * Math.PI / stars.Count());
                dy = centerStarToCenterLogo * Math.Cos(degrees + i * 2 * Math.PI / stars.Count());
                stars[i].Draw(gr, new Point((int) (Center.X + dx), (int) (Center.Y + dy)), 100, !StillLoading);
            }
            if (!StillLoading)
            {
                string name = "Allstars";
                Font font = new Font("Tahoma", Radius / 8);
                SizeF sizeF = gr.MeasureString(name, font);
                gr.DrawString(name, font, Brushes.Black, new PointF(Center.X - sizeF.Width / 2, Center.Y - sizeF.Height / 2));
            }
            Star centerStar = new Star("AllStars");
            centerStar.Radius = (double)Radius / 3;
            //centerStar.Draw(gr, Center, 150, true);
        }
    }
}
