﻿using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Reflection;
using System.Resources;
using System.Threading;

namespace MyMap
{
    class MapDisplay : Panel
    {
        private ButtonMode buttonMode = ButtonMode.None;
        private Graph graph;
        private BBox bounds;
        private List<Bitmap> tiles;
        private List<Point> tileCorners;
        private RouteFinder rf;
        private Renderer render;
        private int bmpWidth = 128;
        private int bmpHeight = 128;

        private List<MyVehicle> myVehicles;
        private Node start, end;
        private Route route;
        
        private Image startImg;
        private Image endImg;
        private Image bikeImg;
        private Image carImg;


        //tijdelijk
        Pen footPen = new Pen(Brushes.Blue, 3);
        Pen bikePen = new Pen(Brushes.Green, 3);
        Pen carPen = new Pen(Brushes.Red, 3);
        Pen otherPen = new Pen(Brushes.Yellow, 3);


        private bool mouseDown = false;
        private bool lockZoom = false;
        private bool forceUpdate = false;
        private Point mousePos;

        // Loading the graph and updating the tiles.
        private delegate void UpdateStatusDelegate();
        private UpdateStatusDelegate updateStatusDelegate = null;
        private Thread UpdateThread;
        private bool stopUpdateThread = false;
        private LoadingThread loadingThread;
        private System.Windows.Forms.Timer loadingTimer;
        
        // Logo for waiting
        private AllstarsLogo logo;

        /// <summary>
        /// Control that draws the map and updates the tiles.
        /// </summary>
        public MapDisplay(int x, int y, int width, int height, LoadingThread thr)
        {
            this.Location = new Point(x, y);
            this.Width = width;
            this.Height = height;
            //this.bounds = new BBox(5.1625, 52.0925, 5.17, 52.085);
            this.bounds = new BBox(5.16130, 52.06070, 5.19430, 52.09410);
            //this.bounds = new BBox(5.15130, 52.07070, 5.20430, 52.10410);
            this.DoubleBuffered = true;
            this.updateStatusDelegate = new UpdateStatusDelegate(UpdateStatus);
            this.UpdateThread = new Thread(new ThreadStart(this.UpdateTiles));

            // Events
            this.MouseClick+= OnClick;
            this.Paint += OnPaint;
            this.Resize += OnResize;
            this.MouseDown += OnMouseDown;
            this.MouseUp += OnMouseUp;
            this.MouseMove += OnMouseMove;

            // Loading the images
            ResourceManager resourcemanager
            = new ResourceManager("MyMap.Properties.Resources"
                                 , Assembly.GetExecutingAssembly());
            startImg = (Image)resourcemanager.GetObject("start");
            endImg = (Image)resourcemanager.GetObject("end");
            bikeImg = (Image)resourcemanager.GetObject("bike");
            carImg = (Image)resourcemanager.GetObject("car");

            // Thread that loads the graph.
            loadingThread = thr;

            // Checks whether the graph is loaded so the mapdisplay can start loading tiles.
            loadingTimer = new System.Windows.Forms.Timer();
            loadingTimer.Interval = 100;
            loadingTimer.Tick += (object o, EventArgs ea) => { Update(); };
            loadingTimer.Start();

            logo = new AllstarsLogo(true);
            logo.Location = Point.Empty;
            logo.Width = this.Width;
            logo.Height = this.Height;
            this.Controls.Add(logo);
            logo.Start();
            

            // Initialize all lists.
            tiles = new List<Bitmap>();
            tileCorners = new List<Point>();
            myVehicles = new List<MyVehicle>();
        }


        public ButtonMode BMode
        {
            set { buttonMode = value; }
            get { return buttonMode; }
        }


        /// <summary>
        /// Updates all the tiles, checks whether a tile isn't rendered yet and then renders it.
        /// This methode should be always called from a different Thread than the Main Thread.
        /// </summary>
        private void UpdateTiles()
        {
            while (!stopUpdateThread)
            {
                //thread shuts itself of after one cycle if stopUpdateThread isn't set to false
                stopUpdateThread = true;

                double tileWidth = LonFromX(bmpWidth);

                for (double x = bounds.XMin - bounds.XMin % tileWidth; x < bounds.XMax + tileWidth; x += tileWidth)
                {
                    int startX = LonToX(x);

                    int startY = LatToY(bounds.YMax);
                    double tileHeight = bounds.YMax - LatFromY(startY - 128);

                    int corner = startY - startY % bmpHeight + bmpHeight;

                    for (double y = LatFromY(corner); y > bounds.YMin + tileHeight; y -= tileHeight)
                    {
                        BBox box = new BBox(x, y, x + tileWidth, y + tileHeight);
                        startY = LatToY(y);

                        if (bounds.IntersectWith(box))
                        {
                            bool found = false;

                            foreach (Point tile in tileCorners)
                            {
                                if (tile.X == startX && tile.Y == startY)
                                {
                                    found = true;
                                    break;
                                }
                            }

                            // If the tile already exists, it shouldn't be rendered again.
                            if (!found)
                            {
                                Bitmap tile = render.GetTile(x, y, x + tileWidth, y + tileHeight, bmpWidth, bmpHeight);
                                tiles.Add(tile);
                                tileCorners.Add(new Point(startX, startY));

                                // Invalidates the Form so tiles will appear on the screen while calculating other tiles.
                                //if (this.InvokeRequired)
                                    this.Invoke(this.updateStatusDelegate);
                                //else
                                //    this.UpdateStatus();
                            }
                        }

                        startY -= 128;
                        tileHeight = LatFromY(startY) - LatFromY(startY - 128);
                    }
                }
            }

            UpdateThread.Abort();
        }

        // Used to invalidate the form from the UpdateThread.
        private void UpdateStatus()
        {
            this.Invalidate();
        }


        /// <summary>
        /// Creates a RouteFinder and a Renderer when needed.
        /// Starts and stops the UpdateThread.
        /// </summary>
        private void Update()
        {
            if (graph == null)
            {
                graph = loadingThread.Graph;
                //logo
            }
            else
            {
                if (rf == null)
                {
                    rf = new RouteFinder(graph);
                    loadingTimer.Stop();
                    logo.Stop();
                    this.Controls.Remove(logo);
                }
                if (render == null)
                {
                    render = new Renderer(graph);
                    loadingTimer.Stop();
                    logo.Stop();
                    this.Controls.Remove(logo);
                }

                if (forceUpdate)
                {
                    UpdateThread.Abort();
                    this.tiles = new List<Bitmap>();
                    this.tileCorners = new List<Point>();
                    forceUpdate = false;
                }


                stopUpdateThread = false;

                if (UpdateThread.ThreadState != ThreadState.Running)
                {
                    UpdateThread = new Thread(new ThreadStart(UpdateTiles));

                    try
                    {
                        UpdateThread.Start();
                    }
                    catch
                    {
                    }
                }

                this.Invalidate();
            }
        }


        private void OnResize(object o, EventArgs ea)
        {
            forceUpdate = true;
            this.Update();
        }


        public void OnClick(object o, MouseEventArgs mea)
        {
            if (ClientRectangle.IntersectsWith(new Rectangle(mea.Location, Size.Empty)))
            {
                Point corner = CoordToPoint(bounds.XMin, bounds.YMax);
                Point test1 = CoordToPoint(bounds.XMax, bounds.YMin);
                double lon = LonFromX(corner.X + mea.X);
                double lat = LatFromY(corner.Y - mea.Y);
                Node location = graph.GetNodeByPos(lon, lat);

                switch (buttonMode)
                {
                    case ButtonMode.From:
                        start = location;
                        break;
                    case ButtonMode.To:
                        end = location;
                        break;
                    case ButtonMode.NewBike:
                        myVehicles.Add(new MyVehicle(Vehicle.Bicycle, location));
                        break;
                    case ButtonMode.NewCar:
                        myVehicles.Add(new MyVehicle(Vehicle.Car, location));
                        break;
                    case ButtonMode.None:
                        if (mea.Button == MouseButtons.Left)
                            this.Zoom(lon, lat, 2);
                        else
                            this.Zoom(lon, lat, 0.5f);
                        break;
                }

                CalcRoute();

                this.Invalidate();
            }
        }


        private void OnMouseDown(object o, MouseEventArgs mea)
        {
            mouseDown = true;
            mousePos = mea.Location;
        }

        private void OnMouseMove(object o, MouseEventArgs mea)
        {
            if (mouseDown)
            {
                int startX = LonToX(bounds.XMin);
                int startY = LatToY(bounds.YMax);

                double dx = LonFromX(startX + mousePos.X) - LonFromX(startX + mea.X);
                double dy = LatFromY(startY - mousePos.Y) - LatFromY(startY - mea.Y);

                bounds.Offset(dx, dy);
                lockZoom = true;
                this.Update();
            }

            mousePos = mea.Location;
        }

        private void OnMouseUp(object o, MouseEventArgs mea)
        {
            mouseDown = false;
            lockZoom = false;
        }


        private void CalcRoute()
        {
            if (start != null && end != null)
            {
                route = rf.CalcRoute(new long[] { start.ID, end.ID }, new Vehicle[] { Vehicle.Foot }, myVehicles.ToArray());
            }
        }


        private void Zoom(double x, double y, float factor)
        {
            if (!lockZoom)
            {
                float fracX = (float)((x - bounds.XMin) / bounds.Width);
                float fracY = (float)((y - bounds.YMin) / bounds.Height);

                double w = bounds.Width / factor;
                double h = bounds.Height / factor;

                double xMin = x - fracX * w;
                double yMin = y - fracY * h;
                double xMax = xMin + w;
                double yMax = yMin + h;

                bounds = new BBox(xMin, yMin, xMax, yMax);
                forceUpdate = true;

                this.Update();
            }
        }


        private void OnPaint(object o, PaintEventArgs pea)
        {
            Graphics gr = pea.Graphics;

            int startX = LonToX(bounds.XMin);
            int startY = LatToY(bounds.YMax);

            //drawing the tiles
            for (int i = 0; i < tiles.Count; i++)
            {
                if (IsInScreen(i))
                {
                    int x = -startX + tileCorners[i].X;
                    int y = startY - tileCorners[i].Y;
                    gr.DrawImage(tiles[i], x, y, bmpWidth, bmpHeight);
                }
            }


            //drawing the distance text and drawing the route
            string s = "";
            if (route != null)
            {
                //s = route.Length.ToString();
                //gr.DrawString(s, new Font("Arial", 40), Brushes.Black, new PointF(10, 10));

                int num = route.NumOfNodes;
                int x1 = LonToX(route[0].Longitude);
                int y1 = LatToY(route[0].Latitude);

                for (int i = 0; i < num - 1; i++)
                {
                    int x2 = LonToX(route[i + 1].Longitude);
                    int y2 = LatToY(route[i + 1].Latitude);

                    switch (route.GetVehicle(i))
                    {
                        case Vehicle.Foot:
                            gr.DrawLine(footPen, x1, y1, x2, y2);
                            break;
                        case Vehicle.Bicycle:
                            gr.DrawLine(bikePen, x1, y1, x2, y2);
                            break;
                        case Vehicle.Car:
                            gr.DrawLine(carPen, x1, y1, x2, y2);
                            break;
                        default:
                            gr.DrawLine(otherPen, x1, y1, x2, y2);
                            break;
                    }


                    x1 = x2;
                    y1 = y2;
                }
            }


            //drawing the start- and endpositions
            float r = 5;
            if (start != null)
            {
                gr.FillEllipse(Brushes.Blue, LonToX(start.Longitude) - startX - r, -LatToY(start.Latitude) + startY + r, 2 * r, 2 * r);
                gr.DrawImage(startImg, LonToX(start.Longitude) - startX - startImg.Width / 2 - 3.5f, -LatToY(start.Latitude) + startY - startImg.Height - 10);
            }
            if (end != null)
            {
                gr.FillEllipse(Brushes.Blue, LonToX(end.Longitude) - startX - r, -LatToY(end.Latitude) + startY - r, 2 * r, 2 * r);
                gr.DrawImage(endImg, LonToX(end.Longitude) - startX - endImg.Width / 2 - 3.5f, -LatToY(end.Latitude) + startY - endImg.Height - 10);
            }

            foreach (MyVehicle v in myVehicles)
            {
                Point location = new Point(LonToX(v.Location.Longitude), LatToY(v.Location.Latitude));
                switch (v.VehicleType)
                {
                    case Vehicle.Bicycle:
                        gr.FillEllipse(Brushes.Green, location.X - r, location.Y - r, 2 * r, 2 * r);
                        gr.DrawImage(bikeImg, location.X - bikeImg.Width / 2 - 3.5f, location.Y - bikeImg.Height - 10);
                        break;
                    case Vehicle.Car:
                        gr.FillEllipse(Brushes.Red, location.X - r, location.Y - r, 2 * r, 2 * r);
                        gr.DrawImage(carImg, location.X - carImg.Width / 2 - 3.5f, location.Y - carImg.Height - 10);
                        break;
                    default:
                        gr.FillEllipse(Brushes.Gray, location.X - r, location.Y - r, 2 * r, 2 * r);
                        break;
                }

            }


            //draw the borders
            gr.DrawLine(Pens.Black, 0, 0, this.Width - 1, 0);
            gr.DrawLine(Pens.Black, 0, 0, 0, this.Height - 1);
            gr.DrawLine(Pens.Black, this.Width - 1, 0, this.Width - 1, this.Height - 1);
            gr.DrawLine(Pens.Black, 0, this.Height - 1, this.Width - 1, this.Height - 1);
        }

        private Point CoordToPoint(double lon, double lat)
        {
            Projection p = new Projection(bounds.Width, this.Width, new Coordinate(bounds.XMin, bounds.YMax));
            return p.CoordToPoint(new Coordinate(lon, lat));
        }

        private double LonFromX(int x)
        {
            Projection p = new Projection(bounds.Width, this.Width, new Coordinate(bounds.XMin, bounds.YMax));

            Coordinate c = p.PointToCoord(new Point(x, 0));
            return c.Longitude;

            //return bounds.XMin + bounds.Width * ((double)x / this.Width);
        }

        private double LatFromY(int y)
        {
            //Projection p = new Projection(bounds.Height, this.Height);
            Projection p = new Projection(bounds.Width, this.Width, new Coordinate(bounds.XMin, bounds.YMax));

            Coordinate c = p.PointToCoord(new Point(0, y));
            return c.Latitude;

            //return bounds.YMin + bounds.Height * ((double)y / this.Height);
        }

        private int LonToX(double lon)
        {
            Projection p = new Projection(bounds.Width, this.Width, new Coordinate(bounds.XMin, bounds.YMax));

            Point point = p.CoordToPoint(new Coordinate(lon, 0));
            return point.X;

            //return (int)(this.Width * (lon - bounds.XMin) / bounds.Width);
        }

        private int LatToY(double lat)
        {
            //Projection p = new Projection(bounds.Height, this.Height);
            Projection p = new Projection(bounds.Width, this.Width, new Coordinate(bounds.XMin, bounds.YMax));

            Point point = p.CoordToPoint(new Coordinate(0, lat));
            return point.Y;

            //return (int)(this.Height * (lat - bounds.YMin) / bounds.Height);
        }

        private bool IsInScreen(int id)
        {
            BBox box = new BBox(LonFromX(tileCorners[id].X), LatFromY(tileCorners[id].Y), LonFromX(tileCorners[id].X + 128), LatFromY(tileCorners[id].Y - 128));
            return this.bounds.IntersectWith(box);           
        }
    }
}
