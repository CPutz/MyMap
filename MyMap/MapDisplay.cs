using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Reflection;
using System.Resources;

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
        private Point mousePos;


        public MapDisplay(int x, int y, int width, int height)
        {
            this.Location = new Point(x, y);
            this.Width = width;
            this.Height = height;
            //this.bounds = new BBox(5.1625, 52.0925, 5.17, 52.085);
            this.bounds = new BBox(5.16130, 52.08070, 5.19430, 52.09410);
            this.DoubleBuffered = true;

            //events
            this.MouseClick+= OnClick;
            this.Paint += OnPaint;
            this.Resize += OnResize;

            graph = new Graph("input.osm.pbf");

            ResourceManager resourcemanager
            = new ResourceManager("MyMap.Properties.Resources"
                                 , Assembly.GetExecutingAssembly());
            startImg = (Image)resourcemanager.GetObject("start");
            endImg = (Image)resourcemanager.GetObject("end");
            bikeImg = (Image)resourcemanager.GetObject("bike");
            carImg = (Image)resourcemanager.GetObject("car");


            rf = new RouteFinder(graph);
            render = new Renderer(graph);
            tiles = new List<Bitmap>();
            tileBoxes = new List<BBox>();
            myVehicles = new List<MyVehicle>();

            Update();
        }

        public ButtonMode BMode
        {
            set { buttonMode = value; }
            get { return buttonMode; }
        }


        private void UpdateTiles()
        {
            tiles = new List<Bitmap>();
            tileBoxes = new List<BBox>();
            Bitmap tile = render.GetTile(bounds.XMin, bounds.YMin, bounds.XMax, bounds.YMax, this.Width, this.Height);
            tiles.Add(tile);
            tileBoxes.Add(bounds);
        }


        private void Update()
        {
            UpdateTiles();
            this.Invalidate();
        }


        private void OnResize(object o, EventArgs ea)
        {
            Update();
        }


        public void OnClick(object o, MouseEventArgs mea)
        {
            if (ClientRectangle.IntersectsWith(new Rectangle(mea.Location, Size.Empty)))
            {
                double lon = LonFromX(mea.X);
                double lat = LatFromY(mea.Y);
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
                double fw = bounds.Width / this.Width;
                double fh = bounds.Height / this.Height;
                double dx = (mea.X - mousePos.X) * fw;
                double dy = (mea.Y - mousePos.Y) * fh;

                bounds.Offset(dx, dy);
                this.Update();
            }
        }


        private void CalcRoute()
        {
            if (start != null && end != null)
            {
                route = rf.CalcRoute(new Node[] { start, end }, new Vehicle[] { Vehicle.Foot }, myVehicles.ToArray());
            }
        }


        private void Zoom(double x, double y, float factor)
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
            UpdateTiles();
            Invalidate();
        }


        private void OnPaint(object o, PaintEventArgs pea)
        {
            Graphics gr = pea.Graphics;

            //drawing the tiles
            for (int i = 0; i < tiles.Count; i++)
            {
                if (IsInScreen(i))
                {
                    int x = LonToX(tileBoxes[i].XMin);
                    int y = LatToY(tileBoxes[i].YMin);
                    int w = LonToX(tileBoxes[i].XMax) - x;
                    int h = LatToY(tileBoxes[i].YMax) - y;
                    gr.DrawImage(tiles[i], x, y, w, h);
                }
            }


            //drawing the distance text and drawing the route
            string s = "";
            if (route != null)
            {
                s = route.Length.ToString();
                gr.DrawString(s, new Font("Arial", 40), Brushes.Black, new PointF(10, 10));

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

        // houdt nog geen rekening met de projectie!
        private double LonFromX(int x)
        {
            return bounds.XMin + bounds.Width * ((double)x / this.Width);
        }

        // houdt nog geen rekening met de projectie!
        private double LatFromY(int y)
        {
            return bounds.YMin + bounds.Height * ((double)y / this.Height);
        }

        // houdt nog geen rekening met de projectie!
        private int LonToX(double lon)
        {
            return (int)(this.Width * (lon - bounds.XMin) / bounds.Width);
        }

        // houdt nog geen rekening met de projectie!
        private int LatToY(double lat)
        {
            return (int)(this.Height * (lat - bounds.YMin) / bounds.Height);
        }

        private bool IsInScreen(int id)
        {
            return this.bounds.IntersectWith(tileBoxes[id]);           
        }
    }
}
