using System;
using System.Drawing;
using System.Diagnostics;
using System.Resources;
using System.Reflection;
using System.Collections.Generic;

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
            List<Curve> streetcurves = new List<Curve>();
            for (int i = 0; i < curves.Length; i++)
            {
                if (!curves[i].Type.IsStreet())
                {
                    drawCurve(box, tile, curves[i]);
                }
                else
                {
                    streetcurves.Add(curves[i]);
                }
            }
            foreach (Curve curve in streetcurves)
            {
                drawCurve(box, tile, curve);
            }

            //Graphics.FromImage(tile).DrawLines(Pens.LightGray, new Point[] { Point.Empty, new Point(0, height), new Point(width, height), new Point(width, 0), Point.Empty });
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
                    penForStreets = Pens.Black;
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
                case CurveType.Basin:
                case CurveType.Salt_pond:
                case CurveType.Water:
                    brushForLanduses = Brushes.LightBlue;
                    break;
                case CurveType.Cemetery:
                    fillCurve(box, tile, curve, new TextureBrush((Image)resourcemanager.GetObject("Cemetery")));
                    break;
                case CurveType.Recreation_ground:
                    brushForLanduses = Brushes.Yellow;
                    break;
                case CurveType.Construction_land:
                    brushForLanduses = Brushes.LightGray;
                    break;
                case CurveType.Farm:
                    brushForLanduses = Brushes.Orange;
                    break;
                case CurveType.Orchard:
                    brushForLanduses = Brushes.Red; //appeltje
                    break;
                case CurveType.Allotments:
                    brushForLanduses = Brushes.Purple;
                    break;
                case CurveType.Military:
                    brushForLanduses = Brushes.DarkGreen;
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
            Point start = nodeToTilePoint(box, tile, new Node(box.XMin, box.YMax, 0));
            
            // it doesn't matter if pt2 is null at start
            Point pt1, pt2 = nodeToTilePoint(box, tile, graph.GetNode(curve[0]));
            for (int i = 1; i < curve.AmountOfNodes; i++)
            {
                pt1 = pt2;
                Node node = graph.GetNode(curve[i]);
                if (node.Longitude != 0 && node.Latitude != 0)
                    pt2 = nodeToTilePoint(box, tile, node);
                Graphics.FromImage(tile).DrawLine(pen, pt1.X - start.X, -pt1.Y + start.Y, pt2.X - start.X, -pt2.Y + start.Y);
            }
        }
        // fills area with brush.
        protected void fillCurve(BBox box, Bitmap tile, Curve curve, Brush brush)
        {
            Point[] polygonPoints = new Point[curve.AmountOfNodes];
            Point start = nodeToTilePoint(box, tile, new Node(box.XMin, box.YMax, 0));

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
            Graphics.FromImage(tile).FillPolygon(brush, polygonPoints);
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

