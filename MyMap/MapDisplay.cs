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
        private double distance;

        public MapDisplay(int x, int y, int width, int height)
        {
            this.Location = new Point(x, y);
            this.Width = width;
            this.Height = height;
            this.bounds = new BBox(0, 0, 1, 1);

            this.MouseClick += OnClick;
            this.Paint += OnPaint;

            graph = new Graph();
            rf = new RouteFinder(graph);
            render = new Renderer(graph);
            tiles = new List<Bitmap>();

            UpdateTiles();
        }


        private void UpdateTiles()
        {
            //Bitmap tile = render.GetTile(this.Width, this.Height, bounds.XMin, bounds.YMin, bounds.XMax, bounds.YMax);
            //tiles.Add(tile);
        }


        private void OnClick(object o, MouseEventArgs mea)
        {
            double lon = LonFromX(mea.X);
            double lat = LatFromY(mea.Y);

            if (first != null)
            {
                first = graph.GetNodeByPos(lon, lat);
            }
            else
            {
                second = graph.GetNodeByPos(lon, lat);

                distance = rf.Dijkstra(first, second, Vehicle.Foot);
            }
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

            gr.DrawString(distance.ToString(), new Font("Arial", 40), Brushes.Black, new PointF(10, 10));
        }

        // houdt nog geen rekening met de projectie!
        private double LonFromX(int x)
        {
            return bounds.Width * ((double)x / this.Width);
        }

        // houdt nog geen rekening met de projectie!
        private double LatFromY(int y)
        {
            return bounds.Height * ((double)y / this.Height);
        }

        // houdt nog geen rekening met de projectie!
        private int LonToX(double lon)
        {
            return (int)(this.Width * ((bounds.XMax - lon) / bounds.Width));
        }

        // houdt nog geen rekening met de projectie!
        private int LatToY(double lat)
        {
            return (int)(this.Height * ((bounds.YMax - lat) / bounds.Height));
        }


        private bool IsInScreen(int id)
        {
            return this.bounds.IntersectWith(tileBoxes[id]);           
        }
    }
}
