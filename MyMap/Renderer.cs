using System;
using System.Drawing;
using System.Diagnostics;
using System.Resources;
using System.Reflection;
using System.Collections.Generic;
using System.Drawing.Drawing2D;

namespace MyMap
{
    /// <summary>
    /// Renders all tiles. Gets data from Graph class and then draws it on tiles.
    /// </summary>
    public class Renderer
    {
        private Graph graph;
        ResourceManager resourcemanager
            = new ResourceManager("MyMap.Properties.Resources"
                                 , Assembly.GetExecutingAssembly()
                                 );

        // Render takes graph to get it's data.
        public Renderer(Graph graph)
        {
            this.graph = graph;
        }


        /// <summary>
        /// Creates a Bitmap image with size width,height.
        /// From the area (x1,y2) to (x2,y2) where x1,y1,x2,y2 are
        /// geological coordinates.
        /// </summary>
        public Bitmap GetTile(double x1, double y1, double x2, double y2, int width, int height)
        {
            Bitmap tile = new Bitmap(width, height);
            int zoomLevel = GetZoomLevel(x1, x2, width);
            BBox box = new BBox(x1, y1, x2, y2);

            // Make searchbox wider dependent on zoomlevel.
            // Explanation at summary of getSearchBBox.
            BBox searchBox = getSearchBBox(box, zoomLevel);

            // Clears image with grey backgroundcolor.
            Graphics.FromImage(tile).Clear(Color.FromArgb(230, 230, 230));

            // Draww all seperate elements in following order:
            // Land -> Buildings -> Streets -> extra locations (Busstations, parking signs)
            drawLandCurves(box, tile, graph.GetLandsInBBox(searchBox));
            drawBuildingCurves(box, tile, graph.GetBuildingsInBBox(searchBox));
            drawStreetCurves(box, tile, graph.GetWaysInBBox(searchBox), zoomLevel);
            drawExtras(box, tile, graph.GetExtrasInBBox(BBox.getResizedBBox(box, 2)), zoomLevel);
            //drawAdditionalCurves(box, tile, graph.GetExtrasinBBOX(searchBox), zoomLevel);

            //used for debugging
            //Graphics.FromImage(tile).DrawLines(Pens.LightGray, new Point[] { Point.Empty, new Point(0, height), new Point(width, height), new Point(width, 0), Point.Empty });
            return tile;
        }

        /// <summary>
        /// Draws all landpieces in "landCurves" on Bitmap tile with box
        /// containing the start and end point of the tile in geological coordinates.
        /// </summary>
        private void drawLandCurves(BBox box, Bitmap tile, Curve[] landCurves)
        {
            foreach (Curve landCurve in landCurves)
            {
                Brush brush = getBrushFromCurveType(landCurve.Type);
                if (brush != null)
                {
                    drawLanduse(box, tile, landCurve, brush);
                }
            }
        }
        /// <summary>
        /// Draws all buildings in "buildingCurves" on Bitmap tile with box
        /// containing the start and end point of the tile in geological coordinates.
        /// </summary>
        private void drawBuildingCurves(BBox box, Bitmap tile, Curve[] buildingCurves)
        {
            foreach (Curve buildingCurve in buildingCurves)
            {
                Brush brush = getBrushFromCurveType(buildingCurve.Type);
                if (brush != null)
                {
                    drawLanduse(box, tile, buildingCurve, brush);
                }
            }
        }
        /// <summary>
        /// Draws all streets in "streetCurves" on Bitmap tile with box
        /// containing the start and end point of the tile in geological coordinates.
        /// Streets will only be drawn in certain zoomlevels.
        /// </summary>
        private void drawStreetCurves(BBox box, Bitmap tile, Curve[] streetCurves, int zoomLevel)
        {
            foreach (Curve streetCurve in streetCurves)
            {
                Pen pen = getPenFromCurveType(streetCurve.Type, zoomLevel);
                if (pen != null)
                {
                    drawStreet(box, tile, streetCurve, getPenFromCurveType(streetCurve.Type, zoomLevel));
                }
            }
        }
        /// <summary>
        /// Draws all locations in "extraLocations" on Bitmap tile with box
        /// containing the start and end point of the tile in geological coordinates.
        /// Locations will only be drawn in certain zoomlevels.
        /// </summary>
        private void drawExtras(BBox box, Bitmap tile, Location[] extraLocations, int zoomLevel)
        {
            foreach (Location extraLocation in extraLocations)
            {
                Image icon = getIconFromLocationType(extraLocation.Type, zoomLevel);
                if (icon != null)
                {
                    drawExtra(box, tile, extraLocation, icon);
                }
            }
        }

        /// <summary>
        /// Resizes the BBox box if zoomlevel is very low.
        /// This is because when curves pass through a tile but no nodes
        /// of that curve are in that tile the curve won't be drawn in that tile.
        /// So on very low zoomlevels, we need to search wider to draw all
        /// curves in a tile.
        /// </summary>
        private BBox getSearchBBox(BBox box, int zoomLevel)
        {
            if (zoomLevel == 1)
                return BBox.getResizedBBox(box, 3);
            if (zoomLevel == 0)
                return BBox.getResizedBBox(box, 7);
            return box;
        }
        
        /// <summary>
        /// returns a integer between -1 and 5 that tells
        /// how far we are zoomed in. A zoomlevel of -1 or 0 means
        /// that we are zoomed to far.
        /// We are not allowed to zoom farther than zoomlevel 1.
        /// </summary>
        public static int GetZoomLevel(double x1, double x2, int width)
        {
            double realLifeDistance = Math.Abs(x2 - x1);
            double scale = realLifeDistance / width;
            if (scale < 0.0000025)
                return -1;
            if (scale < 0.000005)
                return 0;
            //from here on it is an acceptable zoomlevel.
            if (scale < 0.00002)
                return 1;
            if (scale < 0.00008)
                return 2;
            if (scale < 0.00035)
                return 3;
            if (scale < 0.0015)
                return 4;
            return 5;
        }


        /// <summary>
        /// Returns the brush that is used to draw a
        /// piece of land or a building of CurveType "curveType".
        /// Returns null when the curveType isn't recognized.
        /// </summary>
        private Brush getBrushFromCurveType(CurveType curveType)
        {
            Brush brushForLanduses;
            switch (curveType)
            {
                case CurveType.Grass:
                    brushForLanduses = Brushes.LightGreen;
                    break;
                case CurveType.Forest:
                    brushForLanduses = Brushes.Green;
                    break;
                case CurveType.Building:
                    brushForLanduses = Brushes.LightGray;
                    break;
                case CurveType.Canal:
                case CurveType.Basin:
                case CurveType.Salt_pond:
                case CurveType.Water:
                    brushForLanduses = Brushes.LightBlue;
                    break;
                case CurveType.Cemetery:
                    brushForLanduses = new TextureBrush((Image)resourcemanager.GetObject("cemetry"));
                    break;
                case CurveType.Recreation_ground:
                    brushForLanduses = Brushes.PaleGreen;
                    break;
                case CurveType.Construction_land:
                    brushForLanduses = Brushes.LightGray;
                    break;
                case CurveType.Farm:
                    brushForLanduses = new HatchBrush(HatchStyle.ForwardDiagonal, Color.Blue, Color.LawnGreen);
                    break;
                case CurveType.Orchard:
                    brushForLanduses = new TextureBrush((Image)resourcemanager.GetObject("apple"));
                    break;
                case CurveType.Allotments:
                    brushForLanduses = new TextureBrush((Image)resourcemanager.GetObject("allotment"));
                    break;
                case CurveType.Military:
                    brushForLanduses = Brushes.DarkGreen;
                    break;
                case CurveType.Parking:
                    brushForLanduses = new HatchBrush(HatchStyle.LargeGrid, Color.White, Color.LightGray);
                    break;
                case CurveType.Power:
                    brushForLanduses = Brushes.Yellow;
                    break;
                default:
                    //Debug.WriteLine("Unknown brush curvetype " + curveType.ToString());
                    brushForLanduses = null;
                    break;
            }
            return brushForLanduses;
        }


        /// <summary>
        /// Returns the pen that is used to draw a street of CurveType "curveType".
        /// The width of the pen is dependent on the zoomLevel.
        /// Returns null when the curveType isn't recognized.
        /// </summary>
        private Pen getPenFromCurveType(CurveType curveType, int zoomLevel)
        {
            Pen penForStreets;
            float penSizePercentage = (float)(14 - 4 * zoomLevel) / 100;
            switch (curveType)
            {
                case CurveType.Motorway:
                case CurveType.Motorway_link:
                case CurveType.Trunk:
                case CurveType.Trunk_link:
                case CurveType.Primary:
                case CurveType.Primary_link:
                    penForStreets = new Pen(Brushes.Orange, 100 * penSizePercentage);
                    break;
                case CurveType.Secondary:
                case CurveType.Secondary_link:
                case CurveType.Tertiary_link:
                case CurveType.Tertiary:
                    penForStreets = new Pen(Brushes.LightGoldenrodYellow, 90 * penSizePercentage);
                    break;
                case CurveType.Living_street:
                case CurveType.Residential_street:
                    penForStreets = new Pen(Brushes.PaleVioletRed, 60 * penSizePercentage);
                    break;
                case CurveType.Unclassified:
                    penForStreets = new Pen(Brushes.Gray, 20 * penSizePercentage);
                    break;
                case CurveType.Bus_guideway:
                    // niet zichtbaar op uithof
                    penForStreets = new Pen(Brushes.Red, 50 * penSizePercentage);
                    break;
                case CurveType.Service:
                    penForStreets = new Pen(Brushes.Red, 20 * penSizePercentage);
                    penForStreets.DashStyle = DashStyle.Dot;
                    break;
                case CurveType.Track:
                    // nauwelijks zichtbaar op uithof
                    penForStreets = new Pen(Brushes.Red, 50 * penSizePercentage);
                    break;
                case CurveType.Raceway:
                    // niet zichtbaar op uithof
                    penForStreets = new Pen(Brushes.Red, 50 * penSizePercentage);
                    break;
                case CurveType.Cycleway:
                    penForStreets = new Pen(Brushes.Pink, 60 * penSizePercentage);
                    break;
                case CurveType.CarBicycleFoot:
                case CurveType.CarBicycleNoFoot:
                case CurveType.NoCarBicycleNoFoot:
                    penForStreets = new Pen(Brushes.LightGoldenrodYellow, 60 * penSizePercentage);
                    break;
                case CurveType.Construction_street:
                    penForStreets = new Pen(Brushes.Yellow, 50 * penSizePercentage);
                    penForStreets.DashStyle = DashStyle.Dot;
                    break;
                case CurveType.Path:
                case CurveType.Footway:
                case CurveType.Pedestrian:
                    penForStreets = new Pen(Brushes.Brown, 30 * penSizePercentage);
                    break;
                case CurveType.UnTested:
                    penForStreets = null;
                    //penForStreets = new Pen(Brushes.Black);
                    break;
                case CurveType.Road:
                    penForStreets = new Pen(Brushes.Black);
                    break;
                case CurveType.Steps:
                    penForStreets = new Pen(Brushes.LightSlateGray, 50 * penSizePercentage);
                    break;
                case CurveType.Bus:
                    penForStreets = null;
                    //penForStreets = new Pen(Brushes.Red, 70 * penSizePercentage);
                    break;
                case CurveType.PublicServiceVehicles:
                case CurveType.BusBicycle:
                case CurveType.BusFoot:
                case CurveType.BusFootBicycle:
                case CurveType.PublicTransportPlatform:
                    penForStreets = new Pen(Brushes.Purple, 50 * penSizePercentage);
                    penForStreets.DashStyle = DashStyle.Dot;
                    break;
                case CurveType.Waterway:
                    penForStreets = new Pen(Brushes.LightBlue, 20 * penSizePercentage);
                    break;
                default:                     
                //Debug.WriteLine("Unknown pen curvetype " + curveType.ToString());
                    penForStreets = null;
                    break;
            }

            if (penForStreets != null)
            {
                if (penForStreets.DashStyle == DashStyle.Solid)
                {
                    penForStreets.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                    penForStreets.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                }
                else
                {
                    penForStreets.DashCap = DashCap.Round;
                }
            }

            return penForStreets;
        }


        /// <summary>
        /// Returns an icon that is used to draw a location
        /// of LocationType "locationType" on the map.
        /// Returns null if the LocationType isn't recognized or
        /// when the icon should not be drawn because of the zoomLevel.
        /// </summary>
        private Image getIconFromLocationType(LocationType locationType, int zoomLevel)
        {
            Image icon = null;
            switch (locationType)
            {
                case LocationType.BusStation:
                    if (zoomLevel < 2)
                    {
                        icon = new Bitmap((Image)resourcemanager.GetObject("busstop"), 12, 12);
                    }
                    break;
                case LocationType.Parking:
                    if (zoomLevel < 2)
                    {
                        icon = new Bitmap((Image)resourcemanager.GetObject("park"), 16, 16);
                    }
                    break;
                default:
                    break;
            }
            return icon;
        }


        /// <summary>
        /// Draws the Curve "curve" with Pen "pen" on the Bitmap "tile",
        /// where box represents the position and size
        /// of the tile in geological coordinates.
        /// </summary>
        private void drawStreet(BBox box, Bitmap tile, Curve curve, Pen pen)
        {
            Point start = nodeToTilePoint(box, tile, new Node(box.XMin, box.YMax, 0));
            Graphics gr = Graphics.FromImage(tile);
            gr.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            Node startNode = graph.GetNode(curve[0]);
            int index = 1;
            while (startNode.Longitude == 0 && startNode.Latitude == 0)
            {
                startNode = graph.GetNode(curve[index]);
                index++;
            }

            // it doesn't matter if pt2 is null at start
            Point pt1 = nodeToTilePoint(box, tile, startNode), pt2;
            for (int i = index; i < curve.AmountOfNodes; i++)
            {
                Node node = graph.GetNode(curve[i]);
                if (node.Longitude != 0 && node.Latitude != 0)
                {
                    pt2 = nodeToTilePoint(box, tile, node);
                    gr.DrawLine(pen, pt1.X - start.X, -pt1.Y + start.Y, pt2.X - start.X, -pt2.Y + start.Y);
                    pt1 = pt2;
                }
            }
        }


        /// <summary>
        /// Draws a piece of land or building using Brush "brush" 
        /// on the Bitmap "tile", where box represents the position
        /// and size of the tile in geological coordinates.
        /// </summary>
        private void drawLanduse(BBox box, Bitmap tile, Curve curve, Brush brush)
        {
            List<Point> polygonPoints = new List<Point>();
            Point start = nodeToTilePoint(box, tile, new Node(box.XMin, box.YMax, 0));

            Graphics gr = Graphics.FromImage(tile);
            gr.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            for (int i = 0; i < curve.AmountOfNodes; i++)
            {
                Node node = graph.GetNode(curve[i]);
                if (node.Longitude != 0 && node.Latitude != 0)
                {
                    Point p = nodeToTilePoint(box, tile, node);
                    polygonPoints.Add(new Point(p.X - start.X, -p.Y + start.Y));
                }
            }

            gr.FillPolygon(brush, polygonPoints.ToArray());
        }


        /// <summary>
        /// Draws a location on the map with Image "icon" on 
        /// the Bitmap "tile", where box represents the position 
        /// and size of the tile in geological coordinates.
        /// </summary>
        private void drawExtra(BBox box, Bitmap tile, Location location, Image icon)
        {
            Graphics gr = Graphics.FromImage(tile);
            gr.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            Point start = nodeToTilePoint(box, tile, new Node(box.XMin, box.YMax, 0));
            Point p = nodeToTilePoint(box, tile, location);

            gr.DrawImage(icon, new Point(p.X - start.X, -p.Y + start.Y));
        }


        /// <summary>
        /// Returns a Point in pixel-coordinates from a Node with
        /// geological coordinates. The Point is in coordinates for the
        /// entire map so it should be converted to coordinates for the
        /// specific tile that is drawn.
        /// </summary>
        private Point nodeToTilePoint(BBox box, Bitmap tile, Node node)
        {
            Coordinate c = new Coordinate(node.Longitude, node.Latitude);
            Projection p = new Projection(box.Width, tile.Width, new Coordinate(box.XMin, box.YMax));
            return p.CoordToPoint(c);
        }
    }
}
