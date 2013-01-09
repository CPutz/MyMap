using System;
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

                Point upLeft = CoordToPoint(bounds.XMin, bounds.YMax);
                Point downRight = CoordToPoint(bounds.XMax, bounds.YMin);

                for (int x = upLeft.X - upLeft.X % bmpWidth; x < downRight.X - downRight.X % bmpWidth + bmpWidth; x += bmpWidth)
                {
                    for (int y = upLeft.Y - upLeft.Y % bmpHeight + bmpHeight; y > downRight.Y - downRight.Y % bmpHeight - bmpHeight; y -= bmpHeight)
                    {
                        Rectangle pixelBounds = new Rectangle(upLeft.X, upLeft.Y, downRight.X, downRight.Y);
                        Rectangle box = new Rectangle(x, y, x + bmpWidth, y - bmpHeight);

                        if (pixelBounds.IntersectsWith(box))
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
                                if (this.InvokeRequired)
                                    this.Invoke(this.updateStatusDelegate);
                                else
                                    this.UpdateStatus();
                            }
                        }
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


        public void OnClick(object o, MouseEventArgs mea)
        {
            if(ClientRectangle.Contains(mea.Location) && graph != null)
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
                            newIcon = new MapIcon(IconType.Start, this);
                        }
                        break;
                    case ButtonMode.To:
                        location = graph.GetNodeByPos(lon, lat, Vehicle.Foot);
                        if (location != null)
                        {
                            MapIcon end = GetMapIcon(IconType.End);
                            if (end != null)
                                icons.Remove(end);
                            newIcon = new MapIcon(IconType.End, this);
                        }
                        break;
                    case ButtonMode.Via:
                        location = graph.GetNodeByPos(lon, lat, Vehicle.Foot); //eigenlijk hangt dit dan weer af van het voertuig...
                        if (location != null)
                            newIcon = new MapIcon(IconType.Via, this);
                        break; 
                    case ButtonMode.NewBike:
                        location = graph.GetNodeByPos(lon, lat, Vehicle.Bicycle);
                        if (location != null)
                        {
                            MyVehicle v = new MyVehicle(Vehicle.Bicycle, location);
                            myVehicles.Add(v);
                            newIcon = new MapIcon(IconType.Bike, this, v);
                        }
                        break;
                    case ButtonMode.NewCar:
                        location = graph.GetNodeByPos(lon, lat, Vehicle.Car);
                        if (location != null)
                        {
                            MyVehicle v = new MyVehicle(Vehicle.Car, location);
                            myVehicles.Add(v);
                            newIcon = new MapIcon(IconType.Car, this, v);
                        }
                        break;
                    case ButtonMode.None:
                        if (mea.Button == MouseButtons.Left)
                            this.Zoom(mea.X, mea.Y, 2);
                        else
                            this.Zoom(mea.X, mea.Y, 0.5f);
                        break;
                }

                if (newIcon != null)
                {
                    newIcon.Location = location;
                    icons.Add(newIcon);
                }

                CalcRoute();

                this.Invalidate();
            }
        }


        private void OnMouseDown(object o, MouseEventArgs mea)
        {
            mouseDown = true;
            mousePos = mea.Location;

            foreach (MapIcon icon in icons)
            {
                if (icon.IntersectWith(mea.Location))
                {
                    if (mea.Button == MouseButtons.Right)
                    {
                        mouseDown = false;                        
                        lockZoom = true;

                        icons.Remove(icon);
                        myVehicles.Remove(icon.Vehicle);
                        break;
                    }
                    else
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
                dragIcon.Location = location;

                isDraggingIcon = false;

                CalcRoute();

                this.Update();
            }

            mouseDown = false;
            lockZoom = false;
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

                route = rf.CalcRoute(nodes.ToArray(), new Vehicle[] { Vehicle.Foot }, myVehicles.ToArray(), myVehicles.Count);

                // Update stats on mainform.
                if (route != null)
                    ((MainForm)this.Parent).ChangeStats(route.Length, route.Time);
                else
                    ((MainForm)this.Parent).ChangeStats(double.PositiveInfinity, double.PositiveInfinity);
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
        private double lon;
        private double lat;
        private Image icon;
        private Color col;
        private int radius;
        private Node location;
        private IconType type;
        private MyVehicle vehicle;


        public MapIcon(IconType type, MapDisplay parent)
        {
            this.col = Color.Blue;
            this.radius = 5;
            this.parent = parent;
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
        }

        public MapIcon(IconType type, MapDisplay parent, MyVehicle myVehicle) : this(type, parent)
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
