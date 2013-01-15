using System;
using System.Drawing;
using System.Diagnostics;
using System.Resources;
using System.Reflection;
using System.Collections.Generic;
using System.Drawing.Drawing2D;

namespace MyMap
{
    public class Renderer
    {
        private Graph graph;
        ResourceManager resourcemanager
            = new ResourceManager("MyMap.Properties.Resources"
                                 , Assembly.GetExecutingAssembly()
                                 );

        // you only need to give the renderer its graph from the start
        public Renderer(Graph graph)
        {
            this.graph = graph;
        }

        // create bitmap and draw a piece of the map on it
        public Bitmap GetTile(double x1, double y1, double x2, double y2, int width, int height)
        {
            Bitmap tile = new Bitmap(width, height);
            int zoomLevel = GetZoomLevel(x1, x2, width);
            BBox box = new BBox(x1, y1, x2, y2);
            BBox searchBox = getSearchBBox(box, zoomLevel);
            Graphics.FromImage(tile).Clear(Color.FromArgb(230, 230, 230));
            drawLandCurves(box, tile, graph.GetLandsInBBox(searchBox));
            drawBuildingCurves(box, tile, graph.GetBuildingsInBBox(searchBox));
            drawStreetCurves(box, tile, graph.GetWaysInBBox(searchBox), zoomLevel);
            drawExtras(box, tile, graph.GetExtrasInBBox(BBox.getResizedBBox(box, 2)), zoomLevel);
            //drawAdditionalCurves(box, tile, graph.GetExtrasinBBOX(searchBox), zoomLevel);
            //used for debugging
            //Graphics.FromImage(tile).DrawLines(Pens.LightGray, new Point[] { Point.Empty, new Point(0, height), new Point(width, height), new Point(width, 0), Point.Empty });
            return tile;
        }
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
        private void drawExtras(BBox box, Bitmap tile, Location[] extraLocations, int zoomLevel)
        {
            foreach (Location extraLocation in extraLocations)
            {
                Image icon = getIconFromLocationType(extraLocation.Type, zoomLevel);
                if (icon != null)
                {
                    drawExtra(box, tile, extraLocation, icon, zoomLevel);
                }
            }
        }
        private BBox getSearchBBox(BBox box, int zoomLevel)
        {
            if (zoomLevel == 1)
                return BBox.getResizedBBox(box, 3);
            if (zoomLevel == 0)
                return BBox.getResizedBBox(box, 7);
            return box;
        }
        
        public static int GetZoomLevel(double x1, double x2, int width)
        {
            double realLifeDistance = Math.Abs(x1 - x2);
            double scale = realLifeDistance / width;
            if (scale < 0.0000025)
                return -1;
            if (scale < 0.000005)
                return 0;
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
        protected Brush getBrushFromCurveType(CurveType curveType)
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
                case CurveType.Basin:
                case CurveType.Salt_pond:
                case CurveType.Water:
                    brushForLanduses = Brushes.LightBlue;
                    break;
                case CurveType.Cemetery:
                    brushForLanduses = new TextureBrush((Image)resourcemanager.GetObject("gravestone"));
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
                    brushForLanduses = new TextureBrush((Image)resourcemanager.GetObject("volkstuin"));
                    break;
                case CurveType.Military:
                    brushForLanduses = Brushes.DarkGreen;
                    break;
                default:
                    Debug.WriteLine("Unknown brush curvetype " + curveType.ToString());
                    brushForLanduses = null;
                    break;
            }
            return brushForLanduses;
        }
        protected Pen getPenFromCurveType(CurveType curveType, int zoomLevel)
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
                    penForStreets = new Pen(Brushes.Red, 500 * penSizePercentage);
                    break;
                case CurveType.Service:
                    penForStreets = null;
                    //penForStreets = new Pen(Brushes.Red, 50 * penSizePercentage);
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
                case CurveType.Construction_street:
                    penForStreets = new Pen(Brushes.Purple, 50 * penSizePercentage);
                    penForStreets.DashStyle = DashStyle.Dot;
                    break;
                case CurveType.Path:
                case CurveType.Footway:
                case CurveType.Pedestrian:
                    penForStreets = new Pen(Brushes.Brown, 30 * penSizePercentage);
                    break;
                case CurveType.UnTested:
                    penForStreets = new Pen(Brushes.Black);
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
                default:
                    Debug.WriteLine("Unknown pen curvetype " + curveType.ToString());
                    penForStreets = null;
                    break;
            }
            if (penForStreets != null)
            {
                penForStreets.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                penForStreets.StartCap = System.Drawing.Drawing2D.LineCap.Round;
            }
            return penForStreets;
        }
        protected Image getIconFromLocationType(LocationType locationType, int zoomLevel)
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
                default:
                    break;
            }
            return icon;
        }
        // draw line between nodes from streetcurve
        protected void drawStreet(BBox box, Bitmap tile, Curve curve, Pen pen)
        {
            Point start = nodeToTilePoint(box, tile, new Node(box.XMin, box.YMax, 0));
            Graphics gr = Graphics.FromImage(tile);
            gr.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // it doesn't matter if pt2 is null at start
            Point pt1, pt2 = nodeToTilePoint(box, tile, graph.GetNode(curve[0]));
            for (int i = 1; i < curve.AmountOfNodes; i++)
            {
                pt1 = pt2;
                Node node = graph.GetNode(curve[i]);
                if (node.Longitude != 0 && node.Latitude != 0)
                    pt2 = nodeToTilePoint(box, tile, node);

                gr.DrawLine(pen, pt1.X - start.X, -pt1.Y + start.Y, pt2.X - start.X, -pt2.Y + start.Y);

                //Graphics.FromImage(tile).DrawLine(pen, pt1.X - start.X, -pt1.Y + start.Y, pt2.X - start.X, -pt2.Y + start.Y);
            }
        }
        // fills area with brush.
        protected void drawLanduse(BBox box, Bitmap tile, Curve curve, Brush brush)
        {
            Point[] polygonPoints = new Point[curve.AmountOfNodes];
            Point start = nodeToTilePoint(box, tile, new Node(box.XMin, box.YMax, 0));

            Graphics gr = Graphics.FromImage(tile);
            gr.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            for (int i = 0; i < curve.AmountOfNodes; i++)
            {
                Node node = graph.GetNode(curve[i]);
                if (node.Longitude != 0 || node.Latitude != 0)
                {
                    Point p = nodeToTilePoint(box, tile, node);
                    polygonPoints[i] = new Point(p.X - start.X, -p.Y + start.Y);
                }
                else
                {
                    if (i != 0)
                        polygonPoints[i] = polygonPoints[i - 1];
                    else
                    {
                        int j = 1;
                        Node test = graph.GetNode(curve[j]);
                        while (test.Latitude == 0 && test.Longitude == 0)
                        {
                            j++;
                            test = graph.GetNode(curve[j]);
                        }
                        polygonPoints[0] = nodeToTilePoint(box, tile, test);
                    }
                }

            }

            gr.FillPolygon(brush, polygonPoints);

            //Graphics.FromImage(tile).FillPolygon(brush, polygonPoints);
        }
        protected void drawExtra(BBox box, Bitmap tile, Location location, Image icon, int zoomLevel)
        {
            Graphics gr = Graphics.FromImage(tile);
            gr.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            Point start = nodeToTilePoint(box, tile, new Node(box.XMin, box.YMax, 0));
            Point p = nodeToTilePoint(box, tile, location);
            gr.DrawImage(icon, new Point(p.X - start.X, -p.Y + start.Y));
        }
        // determine location of node on the tile
        protected Point nodeToTilePoint(BBox box, Bitmap tile, Node node)
        {
            //int x = (int)(tile.Width * (node.Longitude - box.XMin) / (box.XMax - box.XMin));
            //int y = (int)(tile.Height * (node.Latitude - box.YMin) / (box.YMax - box.YMin));
            //return new Point(x, y);

            Coordinate c = new Coordinate(node.Longitude, node.Latitude);
            Projection p = new Projection(box.Width, tile.Width, new Coordinate(box.XMin, box.YMax));
            return p.CoordToPoint(c);
        }
    }
}
