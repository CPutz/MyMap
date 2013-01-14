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
        //private bool mouseDown = false;
        private bool iconPlaced = false;
        private Image icon;
        private MapIcon mapIcon;

        public MapDragButton(MapDisplay map, Bitmap icon, bool removeIcon) {
            this.MouseDown += (object o, MouseEventArgs mea) => { 
                //mouseDown = true; 
                mousePos = mea.Location; 
                this.PerformClick(); 
                if (removeIcon)
                    this.BackgroundImage = null;
                ((MainForm)Parent).ChangeCursor(icon);
            };

            this.MouseUp += (object o, MouseEventArgs mea) => {
                //mouseDown = false;
                ((MainForm)Parent).ChangeCursorBack();
                this.Invalidate();
                if (!map.OnClick(this, new MouseEventArgs(mea.Button,
                                                  mea.Clicks,
                                                  mea.X + this.Location.X - map.Location.X,
                                                  mea.Y + this.Location.Y - map.Location.Y,
                                                  mea.Delta)))
                {
                    if (removeIcon)
                        this.BackgroundImage = icon;
                }
                
                //if (!iconPlaced)
                //    this.BackgroundImage = icon;
                /*map.BMode = ButtonMode.None;*/ };

            //map.MapIconPlaced += (object o, EventArgs ea) => { iconPlaced = true; };
            map.MapIconRemoved += (object o, EventArgs ea) => { this.BackgroundImage = icon; };

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
        private ButtonMode buttonMode;


        public StreetSelectBox(MapDisplay map, LoadingThread thr, ButtonMode buttonMode)
        {
            this.map = map;
            this.graphThread = thr;
            this.buttonMode = buttonMode;

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

            if (curves.Count > 0)
            {
                Node n = graph.GetNode(curves[0][0]);
                map.FocusOn(n.Longitude, n.Latitude);
                map.SetMapIcon(IconType.Start, n);
            }
        }


        /*protected override void OnClick(EventArgs e)
        {
            SelectStreet(this.Text);
            base.OnClick(e);
        }*/

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