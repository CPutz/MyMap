using System;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace MyMap
{
    public class MapDragButton : Button
    {
        private Point mousePos;
        private bool iconPlaced = false;
        private Image icon;
        private MapIcon mapIcon;

        public MapDragButton(MapDisplay map, Bitmap icon, ButtonMode mode, MainForm parent, bool removeIcon) {
            this.MouseDown += (object o, MouseEventArgs mea) => {
                if (!iconPlaced)
                {
                    mousePos = mea.Location;
                    map.BMode = mode;
                    this.PerformClick();
                    if (removeIcon)
                        this.BackgroundImage = null;
                    parent.ChangeCursor(icon);
                }
            };

            this.MouseUp += (object o, MouseEventArgs mea) => {
                if (!iconPlaced)
                {
                    ((MainForm)Parent).ChangeCursorBack();
                    this.Invalidate();
                    if (!map.OnClick(o, new MouseMapDragEventArgs(this,
                                                                     mea.Button,
                                                                     mea.Clicks,
                                                                     mea.X + this.Location.X - map.Location.X,
                                                                     mea.Y + this.Location.Y - map.Location.Y,
                                                                     mea.Delta)))
                    {
                        if (removeIcon)
                        {
                            this.BackgroundImage = icon;
                        }
                    }
                    else
                    {
                        iconPlaced = true;
                    }

                    map.BMode = ButtonMode.None;
                }
            };

            map.MapIconPlaced += (object o, MapDragEventArgs ea) => { 
                if (ea.Button == this && removeIcon) 
                { 
                    this.BackgroundImage = null;
                    iconPlaced = true;
                } 
            };

            map.MapIconRemoved += (object o, MapDragEventArgs ea) => { 
                if (ea.Button == this) 
                { 
                    this.BackgroundImage = icon;
                    iconPlaced = false;
                } 
            };

            this.icon = icon;
        }


        public MapIcon MapIcon
        {
            set { mapIcon = value; }
        }
    }


    class StreetSelectBox : TextBox
    {
        private MapDisplay map;
        private LoadingThread graphThread;
        private IconType type;
        private MapDragButton button;


        public StreetSelectBox(MapDisplay map, LoadingThread thr, IconType type, MapDragButton button)
        {
            this.map = map;
            this.graphThread = thr;
            this.type = type;
            this.button = button;

            this.TextChanged += OnTextChanged;

            this.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            this.AutoCompleteSource = AutoCompleteSource.CustomSource;
            this.AutoCompleteCustomSource = new AutoCompleteStringCollection();
        }


        private void OnTextChanged(object o, EventArgs ea)
        {
            if (graphThread.Graph != null && this.Text != "")
            {
                // Make first character always uppercase
                if (this.Text.First().ToString() != this.Text.First().ToString().ToUpper())
                    this.Text = this.Text.First().ToString().ToUpper() + String.Join("", this.Text.Skip(1));
                this.SelectionStart = this.SelectionStart + this.SelectionLength + 1;

                Graph g = graphThread.Graph;

                List<Curve> curves = g.GetCurvesByName(this.Text);
                string[] names = new string[curves.Count];
                for (int i = 0; i < names.Length; i++)
                {
                    names[i] = curves[i].Name;
                }

                names = names.Distinct().ToArray();

                foreach (string name in names)
                {
                    // We shouldn't add a name twice to the customsource
                    if (!this.AutoCompleteCustomSource.Contains(name))
                        this.AutoCompleteCustomSource.AddRange(names);
                }
            }
        }


        private void SelectStreet(string name)
        {
            Graph graph = graphThread.Graph;
            List<Curve> curves = graph.GetCurvesByName(name);
            bool found = false;

            /*if (curves.Count > 0)
            {

                Node n = graph.GetNode(curves[0][0]);
                map.FocusOn(n.Longitude, n.Latitude);
                map.SetMapIcon(type, n, button);
            }*/


            foreach (Curve c in curves)
            {
                if (CurveTypeExtentions.FootAllowed(c.Type))
                {
                    Node n = graph.GetNode(c[c.AmountOfNodes / 2]);
                    map.FocusOn(n.Longitude, n.Latitude);
                    map.SetMapIcon(type, n, button);
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                Node n = graph.GetNode(curves[curves.Count / 2][0]);
                Node location = graph.GetNodeByPos(n.Longitude, n.Latitude, Vehicle.Foot);
                map.FocusOn(location.Longitude, location.Latitude);
                map.SetMapIcon(type, location, button);
            }
        }


        protected override void  OnKeyUp(KeyEventArgs e)
        {
            if (e.KeyValue == (int)Keys.Return)
            {
                SelectStreet(this.Text);
            }

            base.OnKeyUp(e);
        }
    }
}