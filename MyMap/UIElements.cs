using System;
using System.Drawing;
using System.Windows.Forms;

namespace MyMap
{
    class MapDragButton : Button
    {
        private Point mousePos;
        private bool mouseDown = false;

        public MapDragButton(MapDisplay map, TopPanel panel) {
            this.MouseDown += (object o, MouseEventArgs mea) => { mouseDown = true; mousePos = mea.Location; this.PerformClick(); };
            this.MouseMove += (object o, MouseEventArgs mea) => { mousePos = mea.Location; panel.Invalidate(); };
            this.MouseUp += (object o, MouseEventArgs mea) => {
                mouseDown = false;
                this.Invalidate(); panel.Invalidate();
                map.OnClick(o, new MouseEventArgs(mea.Button,
                                                  mea.Clicks,
                                                  mea.X + this.Location.X - map.Location.X,
                                                  mea.Y + this.Location.Y - map.Location.Y,
                                                  mea.Delta)); };
        }

        public void OnPaint(object o, PaintEventArgs pea) {
            if (mouseDown)
                pea.Graphics.FillEllipse(Brushes.Blue, mousePos.X + this.Location.X - 5, mousePos.Y + this.Location.Y - 5, 10, 10);
        }
    }


    /// <summary>
    /// Panel that lies on top of all controls and sends events through
    /// so underlying controls don't stop working.
    /// The panel is used to draw on top of all controls
    /// Main documentation: http://www.bobpowell.net/transcontrols.htm
    /// </summary>
    class TopPanel : Panel
    {
        //timer that invalidates the parent
        private Timer timer = new Timer();


        public TopPanel() {
            //this.DoubleBuffered = true;
            timer.Tick += (object o, EventArgs ea) => { this.InvalidateEx(); };
            timer.Interval = 10;
            timer.Enabled = true;
        }

        /// <summary>
        /// Sets the MapDragButtons that can draw on the panel
        /// </summary>
        public void SetButtons(MapDragButton[] buttons) {
            foreach (MapDragButton b in buttons) {
                this.Paint += b.OnPaint;
            }
        }

        protected override CreateParams CreateParams {
            get {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x00000020; // WS_EX_TRANSPARENT
                return cp;
            }
        }

        /// <summary>
        /// Invalidates the parent
        /// </summary>
        protected void InvalidateEx() {
            if (Parent == null)
                return;

            Rectangle rc = new Rectangle(this.Location, this.Size);
            Parent.Invalidate(rc, true);
        }

        protected override void OnPaintBackground(PaintEventArgs pea) {
            //do not paint background
        }

        /// <summary>
        /// Causes the panel to send through all events to underlying controls
        /// documentation: http://stackoverflow.com/questions/547172/pass-through-mouse-events-to-parent-control
        /// </summary>
        protected override void WndProc(ref Message m)
        {
            const int WM_NCHITTEST = 0x0084;
            const int HTTRANSPARENT = (-1);

            if (m.Msg == WM_NCHITTEST) {
                m.Result = (IntPtr)HTTRANSPARENT;
            }
            else {
                base.WndProc(ref m);
            }
        }
    }
}