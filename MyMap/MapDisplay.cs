﻿using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Reflection;
using System.Resources;
using System.Threading;
using System.Threading.Tasks;

namespace MyMap
{
    public class MapDisplay : Panel
    {
        private ButtonMode buttonMode = ButtonMode.None;
        private RouteMode routeMode = RouteMode.Fastest;
        private Graph graph;
        private BBox bounds;
        private List<Bitmap> tiles;
        private List<Point> tileCorners;
        private RouteFinder rf;
        private Renderer render;
        private int bmpWidth = 128;
        private int bmpHeight = 128;

        private List<MyVehicle> myVehicles;
        private Route route;
        
        private List<MapIcon> icons;

        private MapIcon dragIcon;
        private bool isDraggingIcon = false;

        //tijdelijk
        Pen footPen = new Pen(Brushes.Blue, 3);
        Pen bikePen = new Pen(Brushes.Green, 3);
        Pen carPen = new Pen(Brushes.Red, 3);
        Pen otherPen = new Pen(Brushes.Yellow, 3);
        Pen busPen = new Pen(Brushes.Purple, 3);


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

        public event EventHandler MapIconRemoved;


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
            this.MouseClick += (object o, MouseEventArgs mea) => { OnClick(o, mea); };
            this.Paint += OnPaint;
            this.Resize += OnResize;
            this.MouseDown += OnMouseDown;
            this.MouseUp += OnMouseUp;
            this.MouseMove += OnMouseMove;
            this.MouseWheel += OnMouseScroll;
            this.Disposed += (object o, EventArgs ea) => { UpdateThread.Abort(); };

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


            tiles = new List<Bitmap>();
            tileCorners = new List<Point>();
            myVehicles = new List<MyVehicle>();
            icons = new List<MapIcon>();
            
        }


        public List<MyVehicle> MyVehicles
        {
            get { return myVehicles; }
        }

        public void AddVehicle(MyVehicle v)
        {
            myVehicles.Add(v);
            MapIcon newIcon;
            switch (v.VehicleType)
            {
                case Vehicle.Car:
                    newIcon = new MapIcon(IconType.Car, this, null, v);
                    newIcon.Location = v.Location;
                    icons.Add(newIcon);
                    break;
                case Vehicle.Bicycle:
                    newIcon = new MapIcon(IconType.Bike, this, null, v);
                    newIcon.Location = v.Location;
                    icons.Add(newIcon);
                    break;
            }
        }


        public ButtonMode BMode
        {
            set { buttonMode = value; }
            get { return buttonMode; }
        }

        public RouteMode RouteMode
        {
            set{
                if (routeMode != value){
                    routeMode = value;
                    CalcRoute();
                }
            }
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
                
                Point upLeft = CoordToPoint(bounds.XMin, bounds.YMax);
                Point mid = new Point(upLeft.X + this.Width / 2, upLeft.Y - this.Height / 2);

                int n = 1;
                int m = 1;
                int x = mid.X - mid.X % bmpWidth;
                int y = mid.Y - mid.Y % bmpHeight;

                while ((n - 2) * this.bmpWidth < this.Width || (n - 2) * this.bmpHeight < this.Height)
                {
                    for (int i = 1; i < n + 1; i++)
                    {
                        AddTile(x, y);

                        y -= m * bmpHeight;                       
                    }

                    for (int i = 1; i < n + 1; i++)
                    {
                        AddTile(x, y);

                        x += m * bmpWidth;
                    }
                    
                    n++;
                    if (n % 2 == 1)
                        m = 1;
                    else
                        m = -1;
                }
            }

            UpdateThread.Abort();
        }


        private void AddTile(int x, int y)
        {
            bool found = false;

            foreach (Point tile in tileCorners)
            {
                if (tile.X == x && tile.Y == y)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                double lon = LonFromX(x);
                double lat = LatFromY(y);
                double tileWidth = LonFromX(x + bmpWidth) - LonFromX(x);
                double tileHeight = LatFromY(y) - LatFromY(y - bmpHeight);

                Bitmap tile = render.GetTile(lon, lat, lon + tileWidth, lat + tileHeight, bmpWidth, bmpHeight);

                tiles.Add(tile);
                tileCorners.Add(new Point(x, y));

                // Invalidates the Form so tiles will appear on the screen while calculating other tiles.
                //if (this.InvokeRequired)
                this.Invoke(this.updateStatusDelegate);
                //else
                //    this.UpdateStatus();
            }
        }


        // Used to invalidate the form from the UpdateThread.
        private void UpdateStatus()
        {
            this.Invalidate();
        }


        public void FocusOn(double longitude, double latitude)
        {
            Point upLeft = CoordToPoint(bounds.XMin, bounds.YMax);

            int dx = LonToX(longitude) - upLeft.X - this.Width / 2;
            int dy = -LatToY(latitude) + upLeft.Y - this.Height / 2;

            double newLon = LonFromX(upLeft.X + dx);
            double newLat = LatFromY(upLeft.Y + dy);

            bounds.Offset(newLon - bounds.XMin, bounds.YMax - newLat);
            this.Update();
        }

        public void SetMapIcon(IconType type, Node location)
        {
            MapIcon newIcon;

            switch (type)
            {
                case IconType.Start:
                    MapIcon start = GetMapIcon(IconType.Start);
                    if (start != null)
                        icons.Remove(start);
                    newIcon = new MapIcon(IconType.Start, this, null);
                    newIcon.Location = location;
                    icons.Add(newIcon);
                    CalcRoute();
                    break;
                case IconType.End:
                    MapIcon end = GetMapIcon(IconType.End);
                    if (end != null)
                        icons.Remove(end);
                    newIcon = new MapIcon(IconType.End, this, null);
                    newIcon.Location = location;
                    icons.Add(newIcon);
                    CalcRoute();
                    break;
                case IconType.Via:
                    MapIcon via = GetMapIcon(IconType.Via);
                    if (via != null)
                        icons.Remove(via);
                    newIcon = new MapIcon(IconType.Via, this, null);
                    newIcon.Location = location;
                    icons.Add(newIcon);
                    CalcRoute();
                    break;
            }
        }


        /// <summary>
        /// Returns the position of the upperleft-corner of the map in comparison with the projection.
        /// </summary>
        public Point GetPixelPos(double longitude, double latitude)
        {
            Point corner = CoordToPoint(bounds.XMin, bounds.YMax);
            Point pos = CoordToPoint(longitude, latitude);
            return new Point(pos.X - corner.X, corner.Y - pos.Y);
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
        
        /// <summary>
        /// Simulates a click on the mapdisplay.
        /// Returns true when there is a mapIcon placed.
        /// </summary>
        public bool OnClick(object o, MouseEventArgs mea)
        {
            bool placedIcon = false;

            if(ClientRectangle.Contains(mea.Location) && graph != null)
            {
                if (mea.Button == MouseButtons.Left)
                {
                    Point corner = CoordToPoint(bounds.XMin, bounds.YMax);
                    double lon = LonFromX(corner.X + mea.X);
                    double lat = LatFromY(corner.Y - mea.Y);

                    Node location = null;
                    MapIcon newIcon = null;

                    switch (buttonMode)
                    {
                        case ButtonMode.From:
                            location = graph.GetNodeByPos(lon, lat, Vehicle.Foot);
                            if (location != null)
                            {
                                MapIcon start = GetMapIcon(IconType.Start);
                                if (start != null)
                                    icons.Remove(start);
                                newIcon = new MapIcon(IconType.Start, this, (MapDragButton)o);
                                newIcon.Location = location;
                                icons.Add(newIcon);
                                CalcRoute();
                                placedIcon = true;
                            }
                            break;
                        case ButtonMode.To:
                            location = graph.GetNodeByPos(lon, lat, Vehicle.Foot);
                            if (location != null)
                            {
                                MapIcon end = GetMapIcon(IconType.End);
                                if (end != null)
                                    icons.Remove(end);
                                newIcon = new MapIcon(IconType.End, this, (MapDragButton)o);
                                newIcon.Location = location;
                                icons.Add(newIcon);
                                CalcRoute();
                                placedIcon = true;
                            }
                            break;
                        case ButtonMode.Via:
                            location = graph.GetNodeByPos(lon, lat, Vehicle.All);
                            if (location != null)
                                newIcon = new MapIcon(IconType.Via, this, (MapDragButton)o);
                            newIcon.Location = location;
                            icons.Add(newIcon);
                            CalcRoute();
                            placedIcon = true;
                            break;
                        case ButtonMode.NewBike:
                            location = graph.GetNodeByPos(lon, lat, Vehicle.Bicycle);
                            if (location != null)
                            {
                                MyVehicle v = new MyVehicle(Vehicle.Bicycle, location);
                                myVehicles.Add(v);
                                newIcon = new MapIcon(IconType.Bike, this, (MapDragButton)o, v);
                                newIcon.Location = location;
                                icons.Add(newIcon);
                                CalcRoute();
                            }
                            break;
                        case ButtonMode.NewCar:
                            location = graph.GetNodeByPos(lon, lat, Vehicle.Car);
                            if (location != null)
                            {
                                MyVehicle v = new MyVehicle(Vehicle.Car, location);
                                myVehicles.Add(v);
                                newIcon = new MapIcon(IconType.Car, this, (MapDragButton)o, v);
                                newIcon.Location = location;
                                icons.Add(newIcon);
                                CalcRoute();
                            }
                            break;
                    }

                    buttonMode = ButtonMode.None;
                }
                else if (mea.Button == MouseButtons.Right)
                {
                    foreach (MapIcon icon in icons)
                    {
                        if (icon.IntersectWith(mea.Location))
                        {
                            mouseDown = false;
                            lockZoom = true;

                            icons.Remove(icon);
                            myVehicles.Remove(icon.Vehicle);
                            MapIconRemoved(icon, new EventArgs());
                            CalcRoute();
                            break;
                        }
                    }
                }

                this.Invalidate();
            }

            return placedIcon;
        }


        private void OnMouseDown(object o, MouseEventArgs mea)
        {
            mouseDown = true;
            mousePos = mea.Location;

            if (mea.Button == MouseButtons.Left)
            {
                foreach (MapIcon icon in icons)
                {
                    if (icon.IntersectWith(mea.Location))
                    {
                        dragIcon = icon;
                        isDraggingIcon = true;
                    }
                }
            }
        }

        private void OnMouseMove(object o, MouseEventArgs mea)
        {
            if (mouseDown)
            {
                int startX = LonToX(bounds.XMin);
                int startY = LatToY(bounds.YMax);

                double dx = LonFromX(startX + mousePos.X) - LonFromX(startX + mea.X);
                double dy = LatFromY(startY - mousePos.Y) - LatFromY(startY - mea.Y);

                if (isDraggingIcon)
                {
                    dragIcon.Longitude -= dx;
                    dragIcon.Latitude -= dy;
                }
                else
                {
                    bounds.Offset(dx, dy);
                }

                lockZoom = true;
                this.Update();
            }

            mousePos = mea.Location;
        }

        private void OnMouseUp(object o, MouseEventArgs mea)
        {
            if (isDraggingIcon)
            {
                Node location = graph.GetNodeByPos(dragIcon.Longitude, dragIcon.Latitude, dragIcon.Vehicle.VehicleType);
                if (location != null)
                {
                    dragIcon.Location = location;
                }

                isDraggingIcon = false;

                CalcRoute();
            }

            mouseDown = false;
            lockZoom = false;
        }


        public void OnMouseScroll(object o, MouseEventArgs mea)
        {
            if (mea.Delta > 0)
                this.Zoom(mea.X, mea.Y, 3f/2f);
            else
                this.Zoom(mea.X, mea.Y, 2f/3f);
        }


        private void CalcRoute()
        {
            MapIcon start = GetMapIcon(IconType.Start);
            MapIcon end = GetMapIcon(IconType.End);

            if (start != null && end != null)
            {
                List<long> nodes = new List<long>();
                nodes.Add(start.Location.ID);

                foreach (MapIcon icon in icons)
                {
                    if (icon.Type == IconType.Via)
                        nodes.Add(icon.Location.ID);
                }

                nodes.Add(end.Location.ID);

                route = rf.CalcRoute(nodes.ToArray(), new Vehicle[] { Vehicle.Foot }, myVehicles.ToArray(), myVehicles.Count, routeMode);

                // Update stats on mainform.
                if (route != null)
                    ((MainForm)this.Parent).ChangeStats(route.Length, route.Time);
                else
                    ((MainForm)this.Parent).ChangeStats(double.PositiveInfinity, double.PositiveInfinity);

                this.Invalidate();
            }
            else
            {
                route = null;
            }
        }


        private MapIcon GetMapIcon(IconType type)
        {
            MapIcon res = null;

            foreach (MapIcon icon in icons)
            {
                if (icon.Type == type)
                    res = icon;
            }

            return res;
        }


        private void Zoom(int x, int y, float factor)
        {
            if (!lockZoom)
            {
                Point upLeft = CoordToPoint(bounds.XMin, bounds.YMax);

                float fracX = (float)x / this.Width;
                float fracY = (float)y / this.Height;

                int w = (int)(this.Width / factor);
                int h = (int)(this.Height / factor);

                int xMin = (int)(x - fracX * w);
                int yMin = (int)(y - fracY * h);
                int xMax = (int)(xMin + w);
                int yMax = (int)(yMin + h);

                Coordinate cUpLeft = PointToCoord(xMin + upLeft.X, upLeft.Y - yMin);
                Coordinate cDownRight = PointToCoord(xMax + upLeft.X, upLeft.Y - yMax);

                bounds = new BBox(cUpLeft.Longitude, cUpLeft.Latitude, cDownRight.Longitude, cDownRight.Latitude);

                Point upLeft2 = CoordToPoint(bounds.XMin, bounds.YMax);
                
                
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
                    int y = startY - tileCorners[i].Y - bmpHeight;
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
                int x1 = LonToX(route[0].Longitude) - startX;
                int y1 = startY - LatToY(route[0].Latitude);

                for (int i = 0; i < num - 1; i++)
                {
                    int x2 = LonToX(route[i + 1].Longitude) - startX;
                    int y2 = startY - LatToY(route[i + 1].Latitude);

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
                        case Vehicle.Bus:
                            gr.DrawLine(busPen, x1, y1, x2, y2);
                            break;
                        default:
                            gr.DrawLine(otherPen, x1, y1, x2, y2);
                            break;
                    }


                    x1 = x2;
                    y1 = y2;
                }
            }


            foreach (MapIcon icon in icons)
            {
                icon.DrawIcon(gr);
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

        private Coordinate PointToCoord(int x, int y)
        {
            Projection p = new Projection(bounds.Width, this.Width, new Coordinate(bounds.XMin, bounds.YMax));
            return p.PointToCoord(new Point(x, y));
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
            if(id >= tileCorners.Count)
                return false;

            BBox box = new BBox(LonFromX(tileCorners[id].X), LatFromY(tileCorners[id].Y), LonFromX(tileCorners[id].X + 128), LatFromY(tileCorners[id].Y + 128));
            return this.bounds.IntersectWith(box);           
        }
    }




    public enum IconType { Start, End, Via, Bike, Car };

    public class MapIcon
    {
        private MapDisplay parent;
        private MapDragButton button;
        private double lon;
        private double lat;
        private Image icon;
        private Color col;
        private int radius;
        private Node location;
        private IconType type;
        private MyVehicle vehicle;


        public MapIcon(IconType type, MapDisplay parent, MapDragButton button)
        {
            this.col = Color.Blue;
            this.radius = 5;
            this.parent = parent;
            this.button = button;
            this.type = type;
            
            ResourceManager resourcemanager
            = new ResourceManager("MyMap.Properties.Resources"
                                 , Assembly.GetExecutingAssembly());

            switch (type)
            {
                case IconType.Start:
                    this.icon = (Image)resourcemanager.GetObject("start");
                    break;
                case IconType.End:
                    this.icon = (Image)resourcemanager.GetObject("end");
                    break;
                case IconType.Via:
                    this.icon = (Image)resourcemanager.GetObject("via");
                    break;
                case IconType.Bike:
                    this.icon = (Image)resourcemanager.GetObject("bike");
                    break;
                case IconType.Car:
                    this.icon = (Image)resourcemanager.GetObject("car");
                    break;
            }

            // If no vehicle is set make it foot.
            if (vehicle == null)
            {
                vehicle = new MyVehicle(MyMap.Vehicle.Foot, new Node(0, 0, 0));
            }

            button.MapIcon = this;
        }

        public MapIcon(IconType type, MapDisplay parent, MapDragButton button, MyVehicle myVehicle)
            : this(type, parent, button)
        {
            this.vehicle = myVehicle;
        }


        public void DrawIcon(Graphics gr)
        {
            Point location = parent.GetPixelPos(lon, lat);
            gr.FillEllipse(Brushes.Blue, location.X - radius, location.Y - radius, 2 * radius, 2 * radius);
            gr.DrawImage(icon, location.X- icon.Width / 2 - 3.5f, location.Y - icon.Height - 10);
        }

        public bool IntersectWith(Point p)
        {
            Bitmap bmp = new Bitmap(parent.Width, parent.Height);
            Graphics gr = Graphics.FromImage(bmp);
            
            // Draws itself on the bitmap.
            this.DrawIcon(gr);

            return bmp.GetPixel(p.X, p.Y) != Color.FromArgb(0, 0, 0, 0);
        }


        #region Properties

        public double Longitude
        {
            set { lon = value; }
            get { return lon; }
        }

        public double Latitude
        {
            set { lat = value; }
            get { return lat; }
        }

        public Node Location
        {
            set { 
                location = value;
                lon = value.Longitude;
                lat = value.Latitude;
                vehicle.Location = value;
            }
            get { return location; }
        }

        public IconType Type
        {
            get { return type; }
        }

        public MyVehicle Vehicle
        {
            get { return vehicle; }
        }

        #endregion
    }
}
