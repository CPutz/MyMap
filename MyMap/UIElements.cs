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

        public MapDragButton(MapDisplay map, TopPanel panel, Image icon) {
            this.MouseDown += (object o, MouseEventArgs mea) => { mouseDown = true; mousePos = mea.Location; this.PerformClick(); };
            this.MouseMove += (object o, MouseEventArgs mea) =>
            {
                if (mouseDown)
                {
                    mousePos = mea.Location; panel.NeedRepaint = true;
                    panel.Invalidate();
                }
            };
            this.MouseUp += (object o, MouseEventArgs mea) => {
                mouseDown = false;
                this.Invalidate(); panel.NeedRepaint = true;
                map.OnClick(o, new MouseEventArgs(mea.Button,
                                                  mea.Clicks,
                                                  mea.X + this.Location.X - map.Location.X,
                                                  mea.Y + this.Location.Y - map.Location.Y,
                                                  mea.Delta)); };
            this.icon = icon;
        }

        /// <summary>
        /// Returns true if the function draws the icon
        /// </summary>
        //public void OnPaint(object o, PaintEventArgs pea) {
        //    if (mouseDown)
        //        pea.Graphics.DrawImage(icon, mousePos.X + this.Location.X, mousePos.Y + this.Location.Y - icon.Height);
        //}
        public void OnPaint(Graphics gr)
        {
            if (mouseDown)
                gr.DrawImage(icon, mousePos.X + this.Location.X - icon.Width / 2 - 5, mousePos.Y + this.Location.Y - icon.Height - 10);
        }
    }


    /// <summary>
    /// Panel that lies on top of all controls and sends events through
    /// so underlying controls don't stop working.
    /// The panel is used to draw on top of all controls
    /// Main documentation: http://www.bobpowell.net/transcontrols.htm
    /// </summary>
    /*class TopPanel : Panel
    {
        //timer that invalidates the parent
        private Timer timer = new Timer();
        private MapDragButton[] buttons;
        private Image img;
        private bool needRepaint = true;


        public TopPanel() {
            //this.DoubleBuffered = true;
            timer.Tick += (object o, EventArgs ea) => { this.InvalidateEx(); };
            timer.Interval = 10;
            timer.Enabled = true;

            this.Paint += OnPaint;
        }

        /// <summary>
        /// Sets the MapDragButtons that can draw on the panel
        /// </summary>
        public void SetButtons(MapDragButton[] buttons) {
            this.buttons = buttons;
        }

        private void OnPaint(object o, PaintEventArgs pea)
        {
            if (img == null || needRepaint)
            {
                img = new Bitmap(this.Width, this.Height);
                Graphics g = Graphics.FromImage(img);

                foreach (MapDragButton b in buttons)
                {
                    b.OnPaint(g);
                }

                needRepaint = false;
            }
            
            pea.Graphics.DrawImage(img, Point.Empty);
        }

        public bool NeedRepaint
        {
            set { needRepaint = value; }
        }

        protected override CreateParams CreateParams {
            get {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x00000020; // WS_EX_TRANSPARENT
                return cp;
            }
        }

        /// <summary>
        /// Invalidates the parent and it's children
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
    }*/

    class TopPanel : Panel
    {
        //timer that invalidates the parent
        private Timer timer = new Timer();
        private MapDragButton[] buttons;
        private Image img;
        private bool needRepaint = true;

        public TopPanel()
        {
            this.Paint += OnPaint;
        }

        public bool NeedRepaint
        {
            set { needRepaint = value; }
        }

        public void SetButtons(MapDragButton[] buttons)
        {
            this.buttons = buttons;
        }

        /*public void SetButtons(MapDragButton[] buttons)
        {
            foreach (MapDragButton b in buttons)
            {
                this.Paint += b.OnPaint;
            }
        }*/

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x20;
                return cp;
            }
        }

        private void OnPaint(object o, PaintEventArgs pea)
        {
            if (img == null || needRepaint)
            {
                img = new Bitmap(this.Width, this.Height);
                Graphics g = Graphics.FromImage(img);

                foreach (MapDragButton b in buttons)
                {
                    b.OnPaint(g);
                }

                needRepaint = false;
            }

            pea.Graphics.DrawImage(img, Point.Empty);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // do nothing
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_NCHITTEST = 0x0084;
            const int HTTRANSPARENT = (-1);

            if (m.Msg == WM_NCHITTEST)
            {
                m.Result = (IntPtr)HTTRANSPARENT;
            }
            else
            {
                base.WndProc(ref m);
            }
        }
    }
}