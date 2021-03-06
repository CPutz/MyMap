﻿using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Reflection;
using System.Resources;
using System.Threading;
using System.Threading.Tasks;

namespace MyMap
{

    /// <summary>
    /// Control that Displays the map on the screen.
    /// It also does the tileManagement so it checks wheter tiles need to be rendered.
    /// </summary>
    public class MapDisplay : Panel
    {
        ButtonMode buttonMode = ButtonMode.None;
        RouteMode routeMode = RouteMode.Fastest;
        Graph graph;
        BBox bounds;

        List<List<Bitmap>> tiles;
        List<List<Point>> tileCorners;
        List<SortedList<int, SortedList<int, int>>> tileIndexes;

        int tileIndex;
        List<double> zoomWidth;
        List<double> zoomHeight;

        RouteFinder rf;
        bool isCalculatingRoute = false;
        Renderer render;
        int bmpWidth = 128;
        int bmpHeight = 128;

        List<MyVehicle> myVehicles;
        Route route;
        
        List<MapIcon> icons;

        List<Curve> streetSelection;

        Label statLabel;

        MapIcon dragIcon;
        bool isDraggingIcon = false;

        //Pens for drawing routes
        Pen footPen = new Pen(Color.FromArgb(155, 12, 95, 233), 5);
        Pen bikePen = new Pen(Color.FromArgb(155, 60, 157, 77), 5);
        Pen carPen = new Pen(Color.FromArgb(155, 234, 0, 0), 5);
        Pen busPen = new Pen(Color.FromArgb(155, 123, 49, 185), 5);
        Pen otherPen = new Pen(Color.FromArgb(155, 234, 222, 0), 7.5f);


        bool mouseDown = false;
        bool lockZoom = false;
        bool forceUpdate = false;
        Point mousePos;

        // Loading the graph and updating the tiles.
        delegate void UpdateStatusDelegate();
        delegate void UpdateRouteStatsDelegate();
        delegate void StartLogoDelegate();
        delegate void StopLogoDelegate();
        UpdateStatusDelegate updateStatusDelegate = null;
        UpdateRouteStatsDelegate updateRouteStatsDelegate = null;
        StartLogoDelegate startLogoDelegate = null;
        StopLogoDelegate stopLogoDelegate = null;
        Thread updateThread;
        bool restartUpdateThread = false;
        bool stopUpdateThread = false;
        LoadingThread loadingThread;
        System.Windows.Forms.Timer loadingTimer;
        
        // Logo for waiting
        AllstarsLogo logo;

        public event EventHandler<MapDragEventArgs> MapIconPlaced;
        public event EventHandler<MapDragEventArgs> MapIconRemoved;


        /// <summary>
        /// Control that draws the map and updates the tiles.
        /// </summary>
        public MapDisplay(int x, int y, int width, int height, LoadingThread thr)
        {
            this.Location = new Point(x, y);
            this.Width = width;
            this.Height = height;
            this.bounds = new BBox(5.16130, 52.06070, 5.19430, 52.09410);
            this.DoubleBuffered = true;

            this.updateStatusDelegate = new UpdateStatusDelegate(UpdateStatus);
            this.updateRouteStatsDelegate = new UpdateRouteStatsDelegate(UpdateRouteStats);
            this.startLogoDelegate = new StartLogoDelegate(StartLoadingLogo);
            this.stopLogoDelegate = new StopLogoDelegate(StopLoadingLogo);

            this.updateThread = new Thread(new ThreadStart(this.UpdateTiles));
            this.MouseClick += (object o, MouseEventArgs mea) => { OnClick(o, new MouseMapDragEventArgs(null, mea.Button, mea.Clicks, 
                                                                                                        mea.X, mea.Y, mea.Delta)); };
            this.MouseDoubleClick += OnDoubleClick;
            this.Paint += OnPaint;
            this.Resize += OnResize;
            this.MouseDown += OnMouseDown;
            this.MouseUp += OnMouseUp;
            this.MouseMove += OnMouseMove;
            this.Disposed += (object o, EventArgs ea) => { updateThread.Abort(); };

            // Thread that loads the graph.
            loadingThread = thr;

            // Checks whether the graph is loaded so the mapdisplay can start loading tiles.
            loadingTimer = new System.Windows.Forms.Timer();
            loadingTimer.Interval = 100;
            loadingTimer.Tick += (object o, EventArgs ea) => { DoUpdate(); };
            loadingTimer.Start();


            logo = new AllstarsLogo(true);
            logo.Location = Point.Empty;
            logo.Width = this.Width;
            logo.Height = this.Height;
            this.Controls.Add(logo);
            logo.Start();


            LinkLabel creditLabel = new LinkLabel();
            creditLabel.Text = "© OpenStreetMap contributors";
            creditLabel.LinkArea = new LinkArea(2, 13);
            creditLabel.LinkClicked += (object o, LinkLabelLinkClickedEventArgs ea) => { System.Diagnostics.Process.Start("http://www.openstreetmap.org/copyright/en"); };
            creditLabel.Anchor = (AnchorStyles.Right | AnchorStyles.Bottom);
            creditLabel.Size = creditLabel.PreferredSize;
            creditLabel.Location = new Point(this.Width - creditLabel.Width - 1, this.Height - creditLabel.Height - 1);
            this.Controls.Add(creditLabel);

            statLabel = new Label();
            statLabel.AutoSize = true;
            statLabel.Resize += (object o, EventArgs ea) => { statLabel.Location = new Point(this.Width - 1 - statLabel.Size.Width, 1); };
            statLabel.Anchor = (AnchorStyles.Right | AnchorStyles.Top);
            statLabel.Font = new Font("Microsoft Sans Serif", 11);
            this.Controls.Add(statLabel);


            myVehicles = new List<MyVehicle>();
            icons = new List<MapIcon>();
            streetSelection = new List<Curve>();

            tiles = new List<List<Bitmap>>();
            tileCorners = new List<List<Point>>();
            tiles.Add(new List<Bitmap>());
            tileCorners.Add(new List<Point>());
            tileIndex = 0;

            tileIndexes = new List<SortedList<int, SortedList<int, int>>>();
            tileIndexes.Add(new SortedList<int, SortedList<int, int>>());

            zoomWidth = new List<double>();
            zoomHeight = new List<double>();


            this.Disposed += (sender, e) =>
            {
                logo.StillLoading = false;
            };


            // Set linecaps for the pens.
            footPen.SetLineCap(LineCap.Round, LineCap.Round, DashCap.Round);
            bikePen.SetLineCap(LineCap.Round, LineCap.Round, DashCap.Round);
            carPen.SetLineCap(LineCap.Round, LineCap.Round, DashCap.Round);
            busPen.SetLineCap(LineCap.Round, LineCap.Round, DashCap.Round);
            otherPen.SetLineCap(LineCap.Round, LineCap.Round, DashCap.Round);
        }

        #region Properties

        public List<MyVehicle> MyVehicles
        {
            get { return myVehicles; }
        }

        public ButtonMode BMode
        {
            set { buttonMode = value; }
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

        #endregion


        /// <summary>
        /// Creates a RouteFinder and a Renderer when needed.
        /// Starts and stops the UpdateThread if needed.
        /// </summary>
        private void DoUpdate()
        {
            if (graph == null)
            {
                graph = loadingThread.Graph;

                if (graph != null)
                {
                    // Set bounds to filebounds.
                    BBox fileBounds = graph.FileBounds;

                    Point p1 = CoordToPoint(fileBounds.XMax, fileBounds.YMax);
                    Point p2 = CoordToPoint(fileBounds.XMin, fileBounds.YMin);

                    int w = Math.Abs(p1.X - p2.X);
                    int h = Math.Abs(p1.Y - p2.Y);

                    if ((float)h / w > (float)this.Height / this.Width)
                    {
                        this.bounds = new BBox(fileBounds.XMin, fileBounds.YMax, fileBounds.XMin + LonFromX(h), fileBounds.YMin);
                    }
                    else
                    {
                        this.bounds = new BBox(fileBounds.XMin, fileBounds.YMax, fileBounds.XMax,
                                               fileBounds.YMax - LatFromY(LonToX(fileBounds.YMin) + h));
                    }

                    zoomWidth.Add(bounds.Width);
                    zoomHeight.Add(bounds.Height);
                }
            }
            else
            {
                if (rf == null || render == null)
                {
                    rf = new RouteFinder(graph);
                    render = new Renderer(graph);
                    loadingTimer.Stop();
                    logo.Stop();
                    this.Controls.Remove(logo);

                    updateThread.Start();
                }

                // If the updateThread is running and this method is called, 
                // just let the thread restart when it's finished the current tile.
                if (forceUpdate && updateThread.ThreadState == ThreadState.Running)
                {
                    restartUpdateThread = true;
                    forceUpdate = false;
                }

                // If the updateThread is stopped and this method is called,
                // then just start the thread.
                if (updateThread.ThreadState == ThreadState.Stopped)
                {
                    updateThread = new Thread(new ThreadStart(this.UpdateTiles));
                    updateThread.Start();
                }

                this.Invalidate();
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

                while (((n - 3) * this.bmpWidth < this.Width || (n - 3) * this.bmpHeight < this.Height))
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

                    if (restartUpdateThread)
                    {
                        stopUpdateThread = false;
                        break;
                    }
                }

                restartUpdateThread = false;
            }

            stopUpdateThread = false;
        }


        /// <summary>
        /// Calculates the route and displays it.
        /// </summary>
        public void UpdateRoute()
        {
            CalcRoute();
            this.Invalidate();
        }


        /// <summary>
        /// Renders the tile at position (x,y) (projection position) if needed, adds it to the list
        /// and invalidates the Mapdisplay so the tile is shown.
        /// </summary>
        private void AddTile(int x, int y)
        {
            bool found = false;

            for (int i = 0; i < tileCorners[tileIndex].Count; i++)
            {
                Point tile = tileCorners[tileIndex][i];
                if (tile.X == x && tile.Y == y)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                Coordinate c = PointToCoord(x, y);

                Coordinate c1 = PointToCoord(x + bmpWidth, y);
                Coordinate c2 = PointToCoord(x, y - bmpHeight);

                double tileWidth = c1.Longitude - c2.Longitude;
                double tileHeight = c1.Latitude - c2.Latitude;

                Bitmap tile = render.GetTile(c.Longitude, c.Latitude, c.Longitude + tileWidth, c.Latitude + tileHeight, bmpWidth, bmpHeight);

                tiles[tileIndex].Add(tile);
                tileCorners[tileIndex].Add(new Point(x, y));

                if (!tileIndexes[tileIndex].ContainsKey(x))
                {
                    tileIndexes[tileIndex].Add(x, new SortedList<int, int>());
                }

                if (!tileIndexes[tileIndex][x].ContainsKey(y))
                    tileIndexes[tileIndex][x].Add(y, tiles[tileIndex].Count - 1);

                // Invalidates the Form so tiles will appear on the screen while calculating other tiles.
                this.Invoke(this.updateStatusDelegate);
            }
        }

        // Used to invalidate the form from the UpdateThread.
        private void UpdateStatus()
        {
            this.Invalidate();
        }


        /// <summary>
        /// Makes the middle of the map the position (longitude, latitude).
        /// </summary>
        public void FocusOn(double longitude, double latitude)
        {
            Point upLeft = CoordToPoint(bounds.XMin, bounds.YMax);

            Point p = CoordToPoint(longitude, latitude);

            int dx = p.X - upLeft.X - this.Width / 2;
            int dy = -p.Y + upLeft.Y - this.Height / 2;

            Coordinate newCoord = PointToCoord(upLeft.X + dx, upLeft.Y + dy);

            bounds.Offset(newCoord.Longitude - bounds.XMin, bounds.YMax - newCoord.Latitude);

            forceUpdate = true;
            this.DoUpdate();
        }


        /// <summary>
        /// Adds a mapIcon to the map at the node 'location'.
        /// </summary>
        public void SetMapIcon(IconType type, Node location, MapDragButton button)
        {
            MapIcon newIcon;

            switch (type)
            {
                case IconType.Start:
                    if (!isCalculatingRoute)
                    {
                        MapIcon start = GetMapIcon(IconType.Start);
                        if (start != null)
                            icons.Remove(start);
                        newIcon = new MapIcon(IconType.Start, this, button);
                        newIcon.Location = location;
                        icons.Add(newIcon);
                        CalcRoute();
                        MapIconPlaced(this, new MapDragEventArgs(button));
                    }
                    break;
                case IconType.End:
                    if (!isCalculatingRoute)
                    {
                        MapIcon end = GetMapIcon(IconType.End);
                        if (end != null)
                            icons.Remove(end);
                        newIcon = new MapIcon(IconType.End, this, button);
                        newIcon.Location = location;
                        icons.Add(newIcon);
                        CalcRoute();
                        MapIconPlaced(this, new MapDragEventArgs(button));
                    }
                    break;
                case IconType.Via:
                    newIcon = new MapIcon(IconType.Via, this, button);
                    newIcon.Location = location;
                    icons.Add(newIcon);
                    CalcRoute();
                    MapIconPlaced(this, new MapDragEventArgs(button));
                    break;
            }
        }


        /// <summary>
        /// Adds a myVehicle of type v to the map.
        /// </summary>
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
                    CalcRoute();
                    break;
                case Vehicle.Bicycle:
                    newIcon = new MapIcon(IconType.Bike, this, null, v);
                    newIcon.Location = v.Location;
                    icons.Add(newIcon);
                    CalcRoute();
                    break;
            }
        }


        /// <summary>
        /// Returns the position of a coordinate point on the screen. 
        /// (so if it's outside of the screen it might be a negative position)
        /// </summary>
        public Point GetPixelPos(double longitude, double latitude)
        {
            Point corner = CoordToPoint(bounds.XMin, bounds.YMax);
            Point pos = CoordToPoint(longitude, latitude);
            return new Point(pos.X - corner.X, corner.Y - pos.Y);
        }

        
        /// <summary>
        /// Makes a click on the display.
        /// Returns true when a mapicon is placed.
        /// </summary>
        public bool OnClick(object o, MouseMapDragEventArgs mmdea)
        {
            bool placedIcon = false;

            if (ClientRectangle.Contains(mmdea.Location) && graph != null && !isCalculatingRoute)
            {
                if (mmdea.Button == MouseButtons.Left)
                {
                    Point corner = CoordToPoint(bounds.XMin, bounds.YMax);
                    Coordinate c = PointToCoord(corner.X + mmdea.X, corner.Y - mmdea.Y);

                    Node location = null;

                    switch (buttonMode)
                    {
                        case ButtonMode.From:
                            location = graph.GetNodeByPos(c.Longitude, c.Latitude, Vehicle.Foot);
                            if (location != null)
                            {
                                SetMapIcon(IconType.Start, location, mmdea.MapButton);
                                placedIcon = true;
                            }
                            break;
                        case ButtonMode.To:
                            location = graph.GetNodeByPos(c.Longitude, c.Latitude, Vehicle.Foot);
                            if (location != null && mmdea.MapButton != null)
                            {
                                SetMapIcon(IconType.End, location, mmdea.MapButton);
                                placedIcon = true;
                            }
                            break;
                        case ButtonMode.Via:
                            //location = graph.GetNodeByPos(lon, lat, Vehicle.All);
                            // Not used Vehicle.All because then are situations where you can't go to
                            // the location, and the RouteFinder doesn't support this.
                            location = graph.GetNodeByPos(c.Longitude, c.Latitude, Vehicle.Foot);
                            if (location != null && mmdea.MapButton != null)
                            {
                                SetMapIcon(IconType.Via, location, mmdea.MapButton);
                                placedIcon = true;
                            }
                            break;
                        case ButtonMode.NewBike:
                            // You can place a Bycicle at a location where you can walk.
                            location = graph.GetNodeByPos(c.Longitude, c.Latitude, new Vehicle[] { Vehicle.Bicycle, Vehicle.Foot });  
                            if (location != null && mmdea.MapButton != null)
                            {
                                AddVehicle(new MyVehicle(Vehicle.Bicycle, location));
                                placedIcon = true;
                            }
                            break;
                        case ButtonMode.NewCar:
                            location = graph.GetNodeByPos(c.Longitude, c.Latitude, Vehicle.Car);
                            if (location != null && mmdea.MapButton != null)
                            {
                                AddVehicle(new MyVehicle(Vehicle.Car, location));
                                placedIcon = true;
                            }
                            break;
                    }

                    buttonMode = ButtonMode.None;
                }
                else if (mmdea.Button == MouseButtons.Right)
                {
                    foreach (MapIcon icon in icons)
                    {
                        if (icon.IntersectWith(mmdea.Location))
                        {
                            mouseDown = false;
                            lockZoom = true;

                            icons.Remove(icon);
                            myVehicles.Remove(icon.Vehicle);
                            MapIconRemoved(this, new MapDragEventArgs(icon.Button));
                            CalcRoute();
                            break;
                        }
                    }
                }

                // TODO: lelijke code
                ((MainForm)Parent).Save();

                this.Invalidate();
            }           

            return placedIcon;
        }


        private void OnMouseDown(object o, MouseEventArgs mea)
        {
            mouseDown = true;
            mousePos = mea.Location;

            if (mea.Button == MouseButtons.Left && !isCalculatingRoute)
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
                Point corner = CoordToPoint(bounds.XMin, bounds.YMax);

                Coordinate c1 = PointToCoord(corner.X + mousePos.X, corner.Y - mousePos.Y);
                Coordinate c2 = PointToCoord(corner.X + mea.X, corner.Y - mea.Y);

                double dx = c1.Longitude - c2.Longitude;
                double dy = c1.Latitude - c2.Latitude;

                if (isDraggingIcon && !isCalculatingRoute)
                {
                    dragIcon.Longitude -= dx;
                    dragIcon.Latitude -= dy;
                }
                else
                {
                    bounds.Offset(dx, dy);
                }

                lockZoom = true;
                forceUpdate = true;
                this.DoUpdate();
            }

            mousePos = mea.Location;
        }

        private void OnMouseUp(object o, MouseEventArgs mea)
        {
            if (isDraggingIcon && !isCalculatingRoute)
            {
                if (ClientRectangle.Contains(mea.Location))
                {
                    Node location = null;
                    if (dragIcon.Vehicle.VehicleType != Vehicle.Bicycle)
                        location = graph.GetNodeByPos(dragIcon.Longitude, dragIcon.Latitude, dragIcon.Vehicle.VehicleType);
                    else
                        // You can place a Bycicle at a location where you can walk.
                        location = graph.GetNodeByPos(dragIcon.Longitude, dragIcon.Latitude, new Vehicle[] { Vehicle.Bicycle, Vehicle.Foot });

                    if (location != null)
                    {
                        dragIcon.Location = location;
                    }
                }
                else
                {
                    icons.Remove(dragIcon);
                    myVehicles.Remove(dragIcon.Vehicle);
                    MapIconRemoved(this, new MapDragEventArgs(dragIcon.Button));
                }

                isDraggingIcon = false;

                CalcRoute();
            }

            mouseDown = false;
            lockZoom = false;
        }


        private void OnDoubleClick(object o, MouseEventArgs mea)
        {
            if (mea.Button == MouseButtons.Right)
                this.Zoom(mea.X, mea.Y, 0.5f);
            else
                this.Zoom(mea.X, mea.Y, 2.0f);
        }

        public void OnMouseScroll(object o, MouseEventArgs mea)
        {
            if (this.ClientRectangle.Contains(mea.Location))
            {
                if (mea.Delta < 0)
                    this.Zoom(mea.X, mea.Y, 0.5f);
                else
                    this.Zoom(mea.X, mea.Y, 2.0f);
            }
        }


        /// <summary>
        /// Let a CalcRouteThread calculate a route from start to end 
        /// through all via nodes if start and end exists.
        /// </summary>
        private void CalcRoute()
        {
            if (!isCalculatingRoute)
            {
                route = null;
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

                    Thread CalcRouteThread = new Thread(new ThreadStart(() => { CalcRoute(nodes); }));
                    this.Disposed += (object o, EventArgs ea) => { CalcRouteThread.Abort(); };
                    CalcRouteThread.Start();
                }
            }
        }

        /// <summary>
        /// Calculates a new route through all nodes 'nodes' and saves it.
        /// Also updates the mainform stats.
        /// </summary>
        private void CalcRoute(List<long> nodes)
        {
            // Determine all the forbidden vehicles.
            List<Vehicle> forbiddenVehicles = new List<Vehicle>();
            foreach (Vehicle v in Enum.GetValues(typeof(Vehicle)))
            {
                if (v != Vehicle.All)
                    if (!((MainForm)this.Parent).VehicleAllowed(v))
                        forbiddenVehicles.Add(v);
            }

            this.Invoke(startLogoDelegate);

            isCalculatingRoute = true;
            route = rf.CalcRoute(nodes.ToArray(), new List<Vehicle>() { Vehicle.Foot }, forbiddenVehicles, myVehicles, routeMode);

            if (this.InvokeRequired)
                this.Invoke(stopLogoDelegate);

            // Updates the stats on the form.
            if (this.InvokeRequired)
                this.Invoke(this.updateRouteStatsDelegate);

            isCalculatingRoute = false;
        }

        /// <summary>
        /// Creates a new AllstartsLogo and starts it.
        /// </summary>
        private void StartLoadingLogo()
        {
            logo = new AllstarsLogo(true);
            int w = Math.Min(this.Width, this.Height) / 10;
            logo.Size = new Size(w, w);
            logo.Location = new Point(1, 1);
            this.Controls.Add(logo);
            logo.Start();
        }
        /// <summary>
        /// Stops and removes the AllstarsLogo created in StartLoadingLogo.
        /// </summary>
        private void StopLoadingLogo()
        {
            if (logo != null)
            {
                logo.Stop();
                this.Controls.Remove(logo);
            }
        }


        /// <summary>
        /// Updates the distance and time stats on the mainform and invalidates.
        /// </summary>
        private void UpdateRouteStats()
        {
            // Update stats on mainform.
            if (route != null)
                ChangeStats(route.Length, route.Time);
            else
                ChangeStats(double.PositiveInfinity, double.PositiveInfinity);

            if (route.NumOfNodes <= 0)
            {
                route = null;
            }

            this.Invalidate();
        }

        /// <summary>
        /// Sets the text of the route-statistics label.
        /// </summary>
        public void ChangeStats(double distance, double time)
        {
            string distUnit = "m";
            string timeUnit = "s";

            if (distance > 1000)
            {
                distance /= 1000;
                distUnit = "km";
                distance = Math.Round(distance, 1);
            }
            else
            {
                distance = Math.Round(distance, 0);
            }

            if (time > 60)
            {
                time /= 60;
                timeUnit = "min";
            }
            if (time > 60)
            {
                time /= 60;
                timeUnit = "h";
            }



            time = Math.Round(time, 0);

            statLabel.Text = "Distance: " + distance.ToString() + " " + distUnit + '\n' +
                             "Time: " + time.ToString() + " " + timeUnit;
        }


        public void SetStreetSelection(List<Curve> street)
        {
            streetSelection = street;
            this.Invalidate();
        }


        /// <summary>
        /// Returns a MapIcon of IconType type if it exists.
        /// If there are more than 1 MapIcons of that type, the first encountered will be returned.
        /// </summary>
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


        /// <summary>
        /// Zooms in on point (x,y) on the screen with a factor of 'factor'.
        /// If factor > 1, zooms in.
        /// If factor < 1, zooms out.
        /// </summary>
        private void Zoom(int x, int y, float factor)
        {
            if (!lockZoom)
            {
                //return if you already have zoomed in to the max
                if ((factor > 1) && (Renderer.GetZoomLevel(LonFromX(0), LonFromX(bmpWidth), bmpWidth) < 1))
                    return;

                Point upLeft = CoordToPoint(bounds.XMin, bounds.YMax);

                double fracX = (double)x / this.Width;
                double fracY = (double)y / this.Height;

                double w = (int)(this.Width / factor);
                double h = (int)(this.Height / factor);

                int xMin = (int)(x - fracX * w);
                int yMin = (int)(y - fracY * h);
                int xMax = (int)(xMin + w);
                int yMax = (int)(yMin + h);

                Coordinate cUpLeft = PointToCoord(xMin + upLeft.X, upLeft.Y - yMin);
                Coordinate cDownRight = PointToCoord(xMax + upLeft.X, upLeft.Y - yMax);


                // When the user zooms in/out, the tiles on that zoomlevel are saved
                // and there will be created a new list where new tiles will be added.
                // When the user goes back to the old zoomLevel, the old tiles can be used again.
                if (factor > 1) {
                    if (tileIndex - 1 >= 0) {
                        cDownRight = new Coordinate(cUpLeft.Longitude + zoomWidth[tileIndex - 1], cUpLeft.Latitude - zoomHeight[tileIndex - 1]);
                    }
                    else {
                        zoomWidth.Insert(0, Math.Abs(cUpLeft.Longitude - cDownRight.Longitude));
                        zoomHeight.Insert(0, Math.Abs(cUpLeft.Latitude - cDownRight.Latitude));
                    }

                    if (tileIndex > 0) {
                        tileIndex--;
                    }
                    else {
                        tiles.Insert(0, new List<Bitmap>());
                        tileCorners.Insert(0, new List<Point>());
                        tileIndexes.Insert(0, new SortedList<int, SortedList<int, int>>());
                    }
                }
                else {
                    tileIndex++;

                    if (tileIndex < zoomWidth.Count) {
                        cDownRight = new Coordinate(cUpLeft.Longitude + zoomWidth[tileIndex], cUpLeft.Latitude - zoomHeight[tileIndex]);
                    }
                    else {
                        zoomWidth.Insert(tileIndex, Math.Abs(cUpLeft.Longitude - cDownRight.Longitude));
                        zoomHeight.Insert(tileIndex, Math.Abs(cUpLeft.Latitude - cDownRight.Latitude));
                    }

                    if (tileIndex >= tiles.Count) {
                        tiles.Insert(tileIndex, new List<Bitmap>());
                        tileCorners.Insert(tileIndex, new List<Point>());
                        tileIndexes.Insert(tileIndex, new SortedList<int,SortedList<int,int>>());
                    }
                }


                bounds = new BBox(cUpLeft.Longitude, cUpLeft.Latitude, cDownRight.Longitude, cDownRight.Latitude);

                forceUpdate = true;
                this.DoUpdate();
            }
        }


        /// <summary>
        /// Clears all old tiles when the window is resized and calls for an update.
        /// </summary>
        private void OnResize(object o, EventArgs ea)
        {
            for (int i = 0; i < tiles.Count; i++) 
            {
                tiles[i] = new List<Bitmap>();
                tileCorners[i] = new List<Point>();
                tileIndexes[i] = new SortedList<int, SortedList<int, int>>();
            }

            while ((Renderer.GetZoomLevel(LonFromX(0), LonFromX(bmpWidth), bmpWidth) < 1))
            {
                this.Zoom(this.Width / 2, this.Height / 2, 0.5f);
            }

            forceUpdate = true;
            this.DoUpdate();
        }


        private void OnPaint(object o, PaintEventArgs pea)
        {
            Graphics gr = pea.Graphics;
            gr.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            Point corner = CoordToPoint(bounds.XMin, bounds.YMax);

            // Checks what tiles should be drawn and if available, draws them.
            for (int x = corner.X - corner.X % bmpWidth; x < corner.X + bmpWidth + this.Width; x += 128) 
            {
                for (int y = corner.Y - corner.Y % bmpWidth; y > corner.Y - bmpHeight - this.Height; y -= 128) 
                {
                    if (tileIndexes[tileIndex].ContainsKey(x) && tileIndexes[tileIndex][x].ContainsKey(y))
                    {
                        int index = tileIndexes[tileIndex][x][y];
                        gr.DrawImage(tiles[tileIndex][index], -corner.X + x, corner.Y - y - bmpHeight, bmpWidth, bmpHeight);
                    }
                    else
                    {
                        // Update becaues tile is missing.
                        if (updateThread.ThreadState == ThreadState.Stopped)
                            this.DoUpdate();
                    }
                }
            }


            if (streetSelection != null)
            {
                foreach (Curve c in streetSelection)
                {
                    Node n = graph.GetNode(c[0]);
                    Point cur = CoordToPoint(n.Longitude, n.Latitude);
                    cur = new Point(cur.X - corner.X, -cur.Y + corner.Y);
                    List<Point> points = new List<Point>();
                    points.Add(cur);

                    for (int i = 1; i < c.AmountOfNodes; i++)
                    {
                        n = graph.GetNode(c[i]);
                        cur = CoordToPoint(n.Longitude, n.Latitude);
                        cur = new Point(cur.X - corner.X, -cur.Y + corner.Y);
                        //gr.DrawLine(otherPen, prev, cur);
                        points.Add(cur);
                    }

                    gr.DrawLines(otherPen, points.ToArray());
                }
            }


            foreach (MapIcon icon in icons)
            {
                icon.DrawIcon(gr);
            }


            //drawing the route
            if (route != null)
            {
                List<Point> points = new List<Point>();
                List<int> changeVehiclePoints = new List<int>();

                int num = route.NumOfNodes;
                int x, y;


                for (int i = 0; i < num - 1; i++)
                {
                    Point pos = CoordToPoint(route[i].Longitude, route[i].Latitude);
                    x = pos.X - corner.X;
                    y = corner.Y - pos.Y;

                    points.Add(new Point(x, y));

                    if (route.GetVehicle(i) != route.GetVehicle(i + 1))
                    {
                        pos = CoordToPoint(route[i + 1].Longitude, route[i + 1].Latitude);
                        points.Add(new Point(pos.X - corner.X, corner.Y - pos.Y));

                        gr.DrawLines(GetPen(route, i), points.ToArray());

                        changeVehiclePoints.Add(i);

                        points = new List<Point>();
                    }
                }

                Point p = CoordToPoint(route[num - 1].Longitude, route[num - 1].Latitude);

                points.Add(new Point(p.X - corner.X, corner.Y - p.Y));
                if (points.Count > 1)
                    gr.DrawLines(GetPen(route, num - 1), points.ToArray());


                foreach (int index in changeVehiclePoints)
                {
                    p = CoordToPoint(route[index + 1].Longitude, route[index + 1].Latitude);
                    Point changePoint = new Point(p.X - corner.X, corner.Y - p.Y);
                    DrawChangeVehicleIcon(gr, changePoint, route.GetVehicle(index + 1));
                }
            }


            // Black borders of the mapDisplay.
            gr.DrawLine(Pens.Black, 0, 0, this.Width - 1, 0);
            gr.DrawLine(Pens.Black, 0, 0, 0, this.Height - 1);
            gr.DrawLine(Pens.Black, this.Width - 1, 0, this.Width - 1, this.Height - 1);
            gr.DrawLine(Pens.Black, 0, this.Height - 1, this.Width - 1, this.Height - 1);
        }


        /// <summary>
        /// Draws the vehicle change icon at the location for vehicle v.
        /// </summary>
        private void DrawChangeVehicleIcon(Graphics gr, Point location, Vehicle v)
        {
            ResourceManager resourcemanager
               = new ResourceManager("MyMap.Properties.Resources"
                                    , Assembly.GetExecutingAssembly());
            Bitmap icon;

            switch (v)
            {
                case Vehicle.Bus:
                    icon = new Bitmap((Image)resourcemanager.GetObject("bus_small"), 24, 24);
                    break;
                case Vehicle.Bicycle:
                    icon = new Bitmap((Image)resourcemanager.GetObject("bike_small"), 24, 24);
                    break;
                case Vehicle.Foot:
                    icon = new Bitmap((Image)resourcemanager.GetObject("walk_small"), 24, 24);
                    break;
                case Vehicle.Car:
                    icon = new Bitmap((Image)resourcemanager.GetObject("car_small"), 24, 24);
                    break;
                default:
                    icon = new Bitmap((Image)resourcemanager.GetObject("walk_small"), 24, 24);
                    break;
            }


            gr.FillEllipse(Brushes.White, location.X - 5, location.Y - 5, 10, 10);
            gr.DrawEllipse(new Pen(Color.Black, 2), location.X - 5, location.Y - 5, 10, 10);
            gr.DrawImage(icon, location.X - icon.Width / 2, location.Y - icon.Width / 2 - 16);
        }


        /// <summary>
        /// Returns the right pen for the vehicle at index i of the route.
        /// So if at index i the vehicle is Car, the carPen will be returned.
        /// </summary>
        private Pen GetPen(Route r, int i)
        {
            switch (r.GetVehicle(i))
            {
                case Vehicle.Foot:
                    return footPen;
                case Vehicle.Bicycle:
                    return bikePen;
                case Vehicle.Car:
                    return carPen;
                case Vehicle.Bus:
                    return busPen;
                default:
                    return otherPen;
            }
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


        /// <summary>
        /// Returns true if the tileCorner with index 'id' is in the screen.
        /// </summary>
        private bool IsInScreen(int id)
        {
            if (id >= tileCorners[tileIndex].Count)
                return false;

            Coordinate c1 = PointToCoord(tileCorners[tileIndex][id].X, tileCorners[tileIndex][id].Y);
            Coordinate c2 = PointToCoord(tileCorners[tileIndex][id].X + bmpWidth, tileCorners[tileIndex][id].Y + bmpHeight);
            BBox box = new BBox(c1.Longitude, c1.Latitude, c2.Longitude, c2.Latitude);

            return this.bounds.IntersectWith(box);           
        }
    }



    /// <summary>
    /// EventArgs that can pass a MapDragButton.
    /// </summary>
    public class MapDragEventArgs : EventArgs
    {
        private MapDragButton button;

        public MapDragEventArgs(MapDragButton button) : base()
        {
            this.button = button;
        }

        public MapDragButton Button
        {
            get { return button; }
        }
    }

    /// <summary>
    /// MouseEventArgs that can pass a MapDragButton.
    /// </summary>
    public class MouseMapDragEventArgs : MouseEventArgs
    {
        private MapDragButton button;

        public MouseMapDragEventArgs(MapDragButton mapButton, MouseButtons button, int clicks, int x, int y, int delta) 
            : base(button, clicks, x, y, delta)
        {
            this.button = mapButton;
        }

        public MapDragButton MapButton
        {
            get { return button; }
        }
    }




    public enum IconType { Start, End, Via, Bike, Car };

    /// <summary>
    /// An icon on the map that can be moved.
    /// Start, end and via points are mapicons and bycicles and cars are mapicons.
    /// MapIcons can be placed by a MapDragButton.
    /// </summary>
    public class MapIcon
    {
        private MapDisplay parent;
        private MapDragButton button;
        private double lon;
        private double lat;
        private Bitmap icon;
        private Color col;
        private float radius;
        private Node location;
        private IconType type;
        private MyVehicle vehicle;


        public MapIcon(IconType type, MapDisplay parent, MapDragButton button)
        {
            this.col = Color.Blue;
            this.radius = 5.0f;
            this.parent = parent;
            this.button = button;
            this.type = type;
            
            ResourceManager resourcemanager
            = new ResourceManager("MyMap.Properties.Resources"
                                 , Assembly.GetExecutingAssembly());

            switch (type)
            {
                case IconType.Start:
                    this.icon = new Bitmap((Image)resourcemanager.GetObject("start"));
                    break;
                case IconType.End:
                    this.icon = new Bitmap((Image)resourcemanager.GetObject("end"));
                    break;
                case IconType.Via:
                    this.icon = new Bitmap((Image)resourcemanager.GetObject("via"));
                    break;
                case IconType.Bike:
                    this.icon = new Bitmap((Image)resourcemanager.GetObject("bike"), 24, 24);
                    break;
                case IconType.Car:
                    this.icon = new Bitmap((Image)resourcemanager.GetObject("car"), 24, 24);
                    break;
            }

            // If no vehicle is set make it foot.
            if (vehicle == null)
                vehicle = new MyVehicle(MyMap.Vehicle.Foot, new Node(0, 0, 0));

            if (button != null)
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
            gr.FillEllipse(Brushes.Black, location.X - radius, location.Y - radius, 2 * radius, 2 * radius);
            if (type == IconType.Start || type == IconType.Via || type == IconType.End)
                gr.DrawImage(icon, location.X - icon.Width / 2 + 1.5f, location.Y - icon.Height);
            else
                gr.DrawImage(icon, location.X - icon.Width / 2, location.Y - icon.Width / 2 - 16);
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

        public MapDragButton Button
        {
            get { return button; }
        }

        #endregion
    }
}
