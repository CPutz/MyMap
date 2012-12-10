using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

namespace MyMap
{
    class MapDisplay : Panel
    {
        private Graph graph;
        private BBox bounds;
        private List<Bitmap> tiles;
        private List<BBox> tileBoxes;
        private RouteFinder rf;
        private Renderer render;

        private Node first;
        private Node second;
        private Route route;

        public MapDisplay(int x, int y, int width, int height)
        {
            this.Location = new Point(x, y);
            this.Width = width;
            this.Height = height;
            this.bounds = new BBox(5.1625, 52.0925, 5.17, 52.085);
            this.DoubleBuffered = true;

            this.MouseClick += OnClick;
            this.Paint += OnPaint;

            graph = new Graph(@"D:\GitProjects\klein.osm.pbf");
            rf = new RouteFinder(graph);
            render = new Renderer(graph);
            tiles = new List<Bitmap>();
            tileBoxes = new List<BBox>();

            UpdateTiles();
        }


        private void UpdateTiles()
        {
            Bitmap tile = render.GetTile(bounds.XMin, bounds.YMin, bounds.XMax, bounds.YMax, this.Width, this.Height);
            tiles.Add(tile);
            tileBoxes.Add(bounds);
        }


        // tijdelijk
        private void OnClick(object o, MouseEventArgs mea)
        {
            double lon = LonFromX(mea.X);
            double lat = LatFromY(mea.Y);

            if (first == null)
            {
                first = graph.GetNodeByPos(lon, lat);
            }
            else
            {
                second = graph.GetNodeByPos(lon, lat);

                graph.ResetNodeDistance();
                route = rf.Dijkstra(first, second, Vehicle.Foot);

                first = null;
            }

            this.Invalidate();
        }


        private void OnPaint(object o, PaintEventArgs pea)
        {
            Graphics gr = pea.Graphics;

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

            string s = "";
            if (route != null)
            {
                s = route.Length.ToString();
                gr.DrawString(s, new Font("Arial", 40), Brushes.Black, new PointF(10, 10));

                int num = route.NumOfNodes;
                int x1 = LonToX(route[0].Longitude);
                int y1 = LatToY(route[0].Latitude);
                Pen pen = new Pen(Brushes.Red, 3);

                for (int i = 0; i < num - 1; i++)
                {
                    int x2 = LonToX(route[i + 1].Longitude);
                    int y2 = LatToY(route[i + 1].Latitude);

                    gr.DrawLine(pen, x1, y1, x2, y2);

                    x1 = x2;
                    y1 = y2;
                }

                pen.Dispose();
            }

            gr.DrawLine(Pens.Black, 0, 0, 0, this.Width - 1);
            gr.DrawLine(Pens.Black, 0, 0, this.Height - 1, 0);
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
            return (int)(this.Width * (1 - (bounds.XMax - lon) / bounds.Width));
        }

        // houdt nog geen rekening met de projectie!
        private int LatToY(double lat)
        {
            return (int)(this.Height * (1 - (bounds.YMax - lat) / bounds.Height));
        }


        private bool IsInScreen(int id)
        {
            return this.bounds.IntersectWith(tileBoxes[id]);           
        }
    }
}
