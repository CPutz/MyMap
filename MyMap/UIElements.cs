using System;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace MyMap
{
    /// <summary>
    /// A button that can be used to drag icons on the map.
    /// It sends it's click event to the MapDisplays through.
    /// If removeIcon is true, the icon on the button will
    /// be removed when the icon is placed.
    /// </summary>
    public class MapDragButton : Button
    {
        private Point mousePos;
        private bool iconPlaced = false;
        private Image icon;
        private MapIcon mapIcon;


        public MapDragButton(MapDisplay map, Bitmap icon, ButtonMode mode, MainForm parent, bool removeIcon) {
            this.MouseDown += (object o, MouseEventArgs mea) => {
                if (!iconPlaced || !removeIcon)
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
                if (!iconPlaced || !removeIcon)
                {
                    parent.ChangeCursorBack();
                    this.Invalidate();
                    if (!map.OnClick(o, new MouseMapDragEventArgs(this,
                                                                     mea.Button,
                                                                     mea.Clicks,
                                                                     mea.X + this.Location.X - map.Location.X,
                                                                     mea.Y + this.Location.Y - map.Location.Y,
                                                                     mea.Delta)))
                    {
                        // If map.OnClick returns false it means that the icon isn't placed
                        // so if the backgroundimage is removed, it should be placed back.
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

            map.MapIconPlaced += (object o, MapDragEventArgs mdea) => {
                // If the MapIcon that links to this button is placed, the icon
                // on this button should be removed.
                if (mdea.Button == this)
                {
                    if (removeIcon)
                        this.BackgroundImage = null;
                    iconPlaced = true;
                }
            };

            map.MapIconRemoved += (object o, MapDragEventArgs mdea) => { 
                // If the MapIcon that links to this button is removed, the icon
                // on this button should be visible.
                if (mdea.Button == this)
                {
                    if (removeIcon)
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


    /// <summary>
    /// A TextBox that is used to search for streets on the map.
    /// It is linked to a MapDragButton. When a street is selected,
    /// the StreetSelectBox will placed the item that the MapDragButton
    /// places on that street.
    /// </summary>
    class StreetSelectBox : TextBox
    {
        private MapDisplay map;
        private LoadingThread graphThread;
        private IconType type;
        private MapDragButton button;


        public StreetSelectBox(MapDisplay map, LoadingThread thr, IconType type, MapDragButton button, MainForm parent)
        {
            this.map = map;
            this.graphThread = thr;
            this.type = type;
            this.button = button;
            this.Enabled = false;

            this.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            this.AutoCompleteSource = AutoCompleteSource.CustomSource;
            this.AutoCompleteCustomSource = new AutoCompleteStringCollection();

            loadNames();

            // If this control was created before the graph was fully loaded,
            // The names aren't initialized so initialize all names.
            parent.GraphLoaded += (object o, EventArgs ea) => { loadNames(); };
        }


        protected override void  OnTextChanged(EventArgs e)
        {
            if (this.Text != "")
            {
                // Make first character always uppercase because the names are all uppercase
                // and the autocompletion is case-sensitive.
                if (this.Text.First().ToString() != this.Text.First().ToString().ToUpper())
                    this.Text = this.Text.First().ToString().ToUpper() + String.Join("", this.Text.Skip(1));
                this.SelectionStart = this.SelectionStart + this.SelectionLength + 1;
            }

            base.OnTextChanged(e);
        }


        /// <summary>
        /// Loads all names from the graph if needed.
        /// </summary>
        private void loadNames()
        {
            if (graphThread.Graph != null)
            {
                Graph g = graphThread.Graph;

                // Find all curves which name starts with "".
                // (So all curves with a name that isn't null)
                List<Curve> curves = g.GetCurvesByName("");

                string[] names = new string[curves.Count];
                for (int i = 0; i < names.Length; i++)
                {
                    if (curves[i].Type.IsStreet())
                        names[i] = curves[i].Name;
                }

                // Remove duplicates from names.
                names = names.Distinct().ToArray();

                this.AutoCompleteCustomSource.AddRange(names);

                // On default, this control is disabled.
                // On this point, the names are fully loaded so the
                // textBox can be enabled.
                this.Enabled = true;
            }
        }


        private void SelectStreet(string name)
        {
            Graph graph = graphThread.Graph;
            List<Curve> curves = graph.GetCurvesByName(name);

            // Remove duplicates from curves.
            curves = curves.Distinct().ToList();

            bool found = false;

            if (curves.Count > 0)
            {
                foreach (Curve c in curves)
                {
                    // Find a curve in the list that you can walk on.
                    if (c.Type.FootAllowed())
                    {
                        Node n = graph.GetNode(c[c.AmountOfNodes / 2]);

                        // Focus map on location
                        map.FocusOn(n.Longitude, n.Latitude);

                        // Place the icon placed by MapDragButton button on the map.
                        map.SetMapIcon(type, n, button);

                        found = true;
                        break;
                    }
                }

                // If you can't walk on any of the curves, search
                // for the nearest curve that you can walk on.
                if (!found)
                {
                    Node n = graph.GetNode(curves[curves.Count / 2][0]);
                    Node location = graph.GetNodeByPos(n.Longitude, n.Latitude, Vehicle.Foot);

                    // Focus map on location
                    map.FocusOn(location.Longitude, location.Latitude);

                    // Place the icon placed by MapDragButton button on the map.
                    map.SetMapIcon(type, location, button);
                }

                // Set the selected street on the current curves.
                map.SetStreetSelection(curves);
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