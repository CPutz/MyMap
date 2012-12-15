﻿using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

namespace MyMap
{
    class MapDisplay : Panel
    {
        private ButtonMode buttonMode = ButtonMode.From;
        private Graph graph;
        private BBox bounds;
        private List<Bitmap> tiles;
        private List<BBox> tileBoxes;
        private RouteFinder rf;
        private Renderer render;

        private Node start, end;
        private Route route;

        private List<MyVehicle> myVehicles;


        //tijdelijk
        Pen footPen = new Pen(Brushes.Blue, 3);
        Pen bikePen = new Pen(Brushes.Green, 3);
        Pen carPen = new Pen(Brushes.Red, 3);
        Pen otherPen = new Pen(Brushes.Yellow, 3);

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
            //graph = new Graph(@"D:\GitProjects\utrecht.osm.pbf");
            //graph = new Graph("input.osm.pbf");
            //graph = new Graph("/home/sophie/Projects/Introductie/utrecht.osm.pbf");

            rf = new RouteFinder(graph);
            render = new Renderer(graph);
            tiles = new List<Bitmap>();
            tileBoxes = new List<BBox>();
            myVehicles = new List<MyVehicle>();

            UpdateTiles();
        }

        public ButtonMode BMode
        {
            set { buttonMode = value; }
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
            Node location = graph.GetNodeByPos(lon, lat);

            switch (buttonMode)
            {
                case ButtonMode.From:
                    start = location;
                    break;
                case ButtonMode.To:
                    end = location;
                    break;
                case ButtonMode.NewBike:
                    myVehicles.Add(new MyVehicle(Vehicle.Bicycle, location));
                    break;
                case ButtonMode.NewCar:
                    myVehicles.Add(new MyVehicle(Vehicle.Car, location));
                    break;
            }

            CalcRoute();

            this.Invalidate();
        }

        private void CalcRoute()
        {
            if (start != null && end != null)
            {
                route = rf.CalcRoute(new Node[] { start, end }, new Vehicle[] { Vehicle.Foot }, myVehicles.ToArray());
            }
        }


        private void OnPaint(object o, PaintEventArgs pea)
        {
            Graphics gr = pea.Graphics;

            //drawing the tiles
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


            //drawing the distance text and drawing the route
            string s = "";
            if (route != null)
            {
                s = route.Length.ToString();
                gr.DrawString(s, new Font("Arial", 40), Brushes.Black, new PointF(10, 10));

                int num = route.NumOfNodes;
                int x1 = LonToX(route[0].Longitude);
                int y1 = LatToY(route[0].Latitude);
                
                for (int i = 0; i < num - 1; i++)
                {
                    int x2 = LonToX(route[i + 1].Longitude);
                    int y2 = LatToY(route[i + 1].Latitude);

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


            //drawing the start- and endpositions
            float r = 5;
            if (start != null)
                gr.FillEllipse(Brushes.Blue, LonToX(start.Longitude) - r, LatToY(start.Latitude) - r, 2 * r, 2 * r);
            if (end != null)
                gr.FillEllipse(Brushes.Blue, LonToX(end.Longitude) - r, LatToY(end.Latitude) - r, 2 * r, 2 * r);

            foreach (MyVehicle v in myVehicles)
            {
                switch (v.VehicleType)
                {
                    case Vehicle.Bicycle:
                        gr.FillEllipse(Brushes.Green, LonToX(v.Location.Longitude) - r, LatToY(v.Location.Latitude) - r, 2 * r, 2 * r);
                        break;
                    case Vehicle.Car:
                        gr.FillEllipse(Brushes.Red, LonToX(v.Location.Longitude) - r, LatToY(v.Location.Latitude) - r, 2 * r, 2 * r);
                        break;
                    default:
                        gr.FillEllipse(Brushes.Gray, LonToX(v.Location.Longitude) - r, LatToY(v.Location.Latitude) - r, 2 * r, 2 * r);
                        break;
                }
                
            }


            //drawing the borders
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
