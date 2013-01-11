using System;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace MyMap
{
    class MapDragButton : Button
    {
        private Point mousePos;
        private bool mouseDown = false;
        private Image icon;

        public MapDragButton(MapDisplay map, Bitmap icon) {
            this.MouseDown += (object o, MouseEventArgs mea) => { mouseDown = true; mousePos = mea.Location; this.PerformClick(); };
            this.MouseMove += (object o, MouseEventArgs mea) =>
            {
                if (mouseDown)
                {
                    mousePos = mea.Location;
                    ((MainForm)Parent).ChangeCursor(icon);
                }
            };
            this.MouseUp += (object o, MouseEventArgs mea) => {
                mouseDown = false;
                ((MainForm)Parent).ChangeCursorBack();
                this.Invalidate();
                map.OnClick(o, new MouseEventArgs(mea.Button,
                                                  mea.Clicks,
                                                  mea.X + this.Location.X - map.Location.X,
                                                  mea.Y + this.Location.Y - map.Location.Y,
                                                  mea.Delta));
                /*map.BMode = ButtonMode.None;*/ };
            
            this.icon = icon;
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

            //this.triggers.Add(new ShortCutTrigger(Keys.Tab, TriggerState.Select));

            //this.AllowDrop = true;
        }


        private void OnTextChanged(object o, EventArgs ea)
        {
            if (graphThread.Graph != null && this.Text != "")
            {
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