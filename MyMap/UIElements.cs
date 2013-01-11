using System;
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


    class StreetSelectBox : ComboBox
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
            this.SelectedIndexChanged += OnSelectedChanged;
        }


        private void OnTextChanged(object o, EventArgs ea)
        {
            if (graphThread.Graph != null)
            {
                Graph g = graphThread.Graph;

                List<Curve> curves = g.GetCurvesByName(this.Text);
                string[] names = new string[curves.Count];
                for (int i = 0; i < names.Length; i++)
                    names[i] = curves[i].Name;
                Array.Sort(names);
                List<string> nameList = new List<string>(names);

                // Remove duplicates
                string current = "";
                int n = nameList.Count;
                for (int i = 0; i < n; i++)
                {
                    if (current == nameList[i])
                    {
                        nameList.RemoveAt(i);
                        i--;
                        n--;
                    }
                    current = nameList[i];
                }

                // Clear all items
                while (this.Items.Count > 0) {
                    this.Items.RemoveAt(0);
                }

                foreach (string name in nameList)
                    this.Items.Add(name);

                this.DroppedDown = true;
                
                //this.Focus();
                //this.Parent.Focus();

            }
        }

        private void OnSelectedChanged(object o, EventArgs ea)
        {
            if (graphThread.Graph != null)
            {
                Graph g = graphThread.Graph;
                List<Curve> curves = g.GetCurvesByName(this.Text);

                if (curves.Count > 0)
                {

                }
            }
        }


        /*protected override void  OnGotFocus(EventArgs e)
        {
            // do nothing
        }*/
    }
}