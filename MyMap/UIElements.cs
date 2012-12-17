using System;
using System.Drawing;
using System.Windows.Forms;

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
                                                  mea.Delta)); };
            this.icon = icon;
        }
    }
}