using System;
using System.Drawing;
using System.Diagnostics;
using System.Resources;
using System.Reflection;

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
            BBox box = new BBox(x1, y1, x2, y2);
            Curve[] curves = graph.GetCurvesInBbox(box);
            for (int i = 0; i < curves.Length; i++)
            {
                drawCurve(box, tile, curves[i]);
            }
            return tile;
        }
        // determine what to draw, then draw it
        protected void drawCurve(BBox box, Bitmap tile, Curve curve)
        {
            Brush brushForLanduses = null;
            Pen penForStreets = null;
            switch (curve.Type)
            {
                case CurveType.Motorway_link:
                case CurveType.Primary_link:
                case CurveType.Secondary_link:
                case CurveType.Tertiary_link:
                case CurveType.Trunk_link:
                case CurveType.Motorway:
                case CurveType.Primary:
                case CurveType.Secondary:
                case CurveType.Tertiary:
                case CurveType.Trunk:
                case CurveType.Unclassified:
                case CurveType.Living_street:
                case CurveType.Residential_street:
                case CurveType.Service:
                case CurveType.Track:
                case CurveType.Raceway:
                case CurveType.Bus_guideway:
                case CurveType.Cycleway:
                case CurveType.Construction_street:
                case CurveType.Path:
                case CurveType.Footway:
                    penForStreets = Pens.Black;
                    break;
                case CurveType.Road:
                    penForStreets =  Pens.Black;
                    break;
                case CurveType.Grass:
                    brushForLanduses = Brushes.LightGreen;
                    break;
                case CurveType.Forest:
                    brushForLanduses = Brushes.Green;
                    break;
                case CurveType.Building:
                    brushForLanduses = Brushes.Gray;
                    break;
                case CurveType.Water:
                    brushForLanduses = Brushes.LightBlue;
                    break;
                case CurveType.Cemetery:
                    fillCurve(box, tile, curve, new TextureBrush((Image) resourcemanager.GetObject("Cemetery")));
                    break;
                default:
                    Debug.WriteLine("Unknown curvetype " + curve.Name);
                    break;
            }
            if (brushForLanduses != null)
            {
                fillCurve(box, tile, curve, brushForLanduses);
            }
            if (penForStreets != null)
            {
                drawStreet(box, tile, curve, penForStreets);
            }
        }
        // draw line between nodes from streetcurve
        protected void drawStreet(BBox box, Bitmap tile, Curve curve, Pen pen)
        {
            // it doesn't matter if pt2 is null at start when
            Point pt1, pt2 = nodeToTilePoint(box, tile, graph.GetNode(curve[0]));
            for (int i = 1; i < curve.AmountOfNodes; i++)
            {
                pt1 = pt2;
                pt2 = nodeToTilePoint(box, tile, graph.GetNode(curve[i]));
                Graphics.FromImage(tile).DrawLine(pen, pt1, pt2);
            }
        }
        // fills area with brush.
        protected void fillCurve(BBox box, Bitmap tile, Curve curve, Brush brush)
        {
            Point[] polygonPoints = new Point[curve.AmountOfNodes];
            for (int i = 0; i < curve.AmountOfNodes; i++)
            {
                polygonPoints[i] = nodeToTilePoint(box, tile, graph.GetNode(curve[i]));
            }
            Graphics.FromImage(tile).FillPolygon(brush, polygonPoints);
        }
        // determine location of node on the tile
        protected Point nodeToTilePoint(BBox box, Bitmap tile, Node node)
        {
            int x = (int)(tile.Width * (node.Longitude - box.XMin) / (box.XMax - box.XMin));
            int y = (int)(tile.Height * (node.Latitude - box.YMin) / (box.YMax - box.YMin));
            return new Point(x, y);
        }
    }
}

