using System;
using System.Drawing;
using System.Diagnostics;

namespace MyMap
{
    public class Renderer
    {
        private Graph graph;

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
            switch (curve.Type)
            {
                case CurveType.Road:
                    drawStreet(box, tile, curve);
                    break;
                case CurveType.Grass:
                    fillCurve(box, tile, curve, Brushes.Green);
                    break;
                case CurveType.Building:
                    fillCurve(box, tile, curve, Brushes.Gray);
                    break;
                default:
                    Debug.WriteLine("Unknown curvetype " + curve.Name);
                    break;
            }
        }
        // draw line between nodes from streetcurve
        protected void drawStreet(BBox box, Bitmap tile, Curve curve)
        {
            // it doesn't matter if pt2 is null at start when
            Point pt1, pt2 = nodeToTilePoint(box, tile, graph.GetNode(curve[0]));
            for (int i = 1; i < curve.AmountOfNodes; i++)
            {
                pt1 = pt2;
                pt2 = nodeToTilePoint(box, tile, graph.GetNode(curve[i]));
                Graphics.FromImage(tile).DrawLine(Pens.Black, pt1, pt2);
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

