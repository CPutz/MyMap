using System;
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
        private List<BBox> tileBoxes;
        private RouteFinder rf;
        private Renderer render;

        private Node start, end;
        private Route route;

        private List<MyVehicle> myVehicles;
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

        private delegate void UpdateStatusDelegate();
        private UpdateStatusDelegate updateStatusDelegate = null;
        private Thread UpdateThread;
        private bool stopUpdateThread = false;
        private LoadingThread loadingThread;
        private System.Windows.Forms.Timer loadingTimer;
        private AllstarsLogo logo;



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

            //events
            this.MouseClick+= OnClick;
            this.Paint += OnPaint;
            this.Resize += OnResize;
            this.MouseDown += OnMouseDown;
            this.MouseUp += OnMouseUp;
            this.MouseMove += OnMouseMove;

            //graph = new Graph("input.osm.pbf");

            ResourceManager resourcemanager
            = new ResourceManager("MyMap.Properties.Resources"
                                 , Assembly.GetExecutingAssembly());
            startImg = (Image)resourcemanager.GetObject("start");
            endImg = (Image)resourcemanager.GetObject("end");
            bikeImg = (Image)resourcemanager.GetObject("bike");
            carImg = (Image)resourcemanager.GetObject("car");

            //while (thr.Graph == null) { Thread.Sleep(10); };
            //this.graph = thr.Graph;
            loadingThread = thr;

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
            
            tiles = new List<Bitmap>();
            tileBoxes = new List<BBox>();
            myVehicles = new List<MyVehicle>();

            this.Update();
        }

        public ButtonMode BMode
        {
            set { buttonMode = value; }
            get { return buttonMode; }
        }

        /// <summary>
        /// Updates all the tiles, this methode is always called from a different Thread than the Main Thread.
        /// </summary>
        private void UpdateTiles()
        {
            while (!stopUpdateThread)
            {
                //thread shuts itself of after one cycle if stopUpdateThread isn't set to false
                stopUpdateThread = true;

                int bmpWidth = 128;
                int bmpHeight = 128;
                
                double tileWidth = LonFromX(bmpWidth);

                for (double x = bounds.XMin - bounds.XMin % tileWidth; x < bounds.XMax + tileWidth; x += tileWidth)
                {

                    int start = LatToY(bounds.YMax);
                    double tileHeight = bounds.YMax - LatFromY(start - 128);

                    int first = start - start % bmpHeight + bmpHeight;

                    for (double y = LatFromY(first); y > bounds.YMin + tileHeight; y -= tileHeight)
                    //for (double y = bounds.YMin; y < bounds.YMax; y += tileHeight)
                    {
                        BBox box = new BBox(x, y, x + tileWidth, y + tileHeight);

                        if (bounds.IntersectWith(box))
                        {
                            bool found = false;
                            foreach (BBox tile in tileBoxes)
                            {
                                if (tile == box)
                                {
                                    found = true;
                                    break;
                                }
                            }

                            if (!found)
                            {
                                Bitmap tile = render.GetTile(x, y, x + tileWidth, y + tileHeight, bmpWidth, bmpHeight);
                                tiles.Add(tile);
                                tileBoxes.Add(box);

                                // Invalidates the Form so tiles will appear on the screen while calculating other tiles.
                                //if (this.InvokeRequired)
                                    this.Invoke(this.updateStatusDelegate);
                                //else
                                //    this.UpdateStatus();
                            }
                        }

                        start -= 128;
                        tileHeight = LatFromY(start) - LatFromY(start - 128);
                    }
                }
            }

            UpdateThread.Abort();
        }


        private void UpdateStatus()
        {
            this.Invalidate();
        }


        private void Update()
        {
            if (graph == null)
            {
                graph = loadingThread.Graph;
                //logo
            }

            if (graph != null)
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
                    this.tileBoxes = new List<BBox>();
                    forceUpdate = false;
                }

                if (UpdateThread.ThreadState != ThreadState.Running)
                {
                    UpdateThread = new Thread(new ThreadStart(UpdateTiles));

                    try
                    {
                        stopUpdateThread = false;
                        UpdateThread.Start();
                    }
                    catch
                    {
                    }
                }
                else
                {
                    stopUpdateThread = false;
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
                //double fw = bounds.Width / this.Width;
                //double fh = bounds.Height / this.Height;
                //double dx = (mousePos.X - mea.X) * fw;
                //double dy = (mousePos.Y - mea.Y) * fh;

                double dx = LonFromX(mousePos.X) - LonFromX(mea.X);
                double dy = -LatFromY(mousePos.Y) + LatFromY(mea.Y);

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
                    int x = -startX + LonToX(tileBoxes[i].XMin);
                    int y = startY - LatToY(tileBoxes[i].YMax);

                    //int test1 = LatToY(tileBoxes[i].YMax);
                    //int test2 = LatToY(tileBoxes[i].YMin);

                    //int w = LonToX(tileBoxes[i].XMax) - x;
                    //int h = LatToY(tileBoxes[i].YMax) - y;
                    int w = 128;
                    int h = 128;
                    gr.DrawImage(tiles[i], x, y, w, h);
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
            if (start != null) {
                gr.FillEllipse(Brushes.Blue, LonToX(start.Longitude) - r, LatToY(start.Latitude) - r, 2 * r, 2 * r);
                gr.DrawImage(startImg, LonToX(start.Longitude) - startImg.Width / 2 - 3.5f, LatToY(start.Latitude) - startImg.Height - 10);
            }
            if (end != null) {
                gr.FillEllipse(Brushes.Blue, LonToX(end.Longitude) - r, LatToY(end.Latitude) - r, 2 * r, 2 * r);
                gr.DrawImage(endImg, LonToX(end.Longitude) - endImg.Width / 2 - 3.5f, LatToY(end.Latitude) - endImg.Height - 10);
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
            return this.bounds.IntersectWith(tileBoxes[id]);           
        }
    }
}
