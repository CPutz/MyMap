using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace logo
{
    class Program
    {
        static void Main()
        {
            Application.Run(new Hoofdscherm());
        }
    }
    class Hoofdscherm : Form
    {
        AllstarsLogo testlogo;
        public Hoofdscherm()
        {
            this.ClientSize = new Size(700, 700);
            this.BackColor = Color.Red;
            testlogo = new AllstarsLogo(true);
            testlogo.Location = new Point(50, 50);
            testlogo.Size = new Size(600, 600);
            this.Controls.Add(testlogo);
            testlogo.MouseClick += loading;
        }
        void loading(object o, MouseEventArgs mea)
        {
            if (testlogo.StillLoading)
            {
                testlogo.Stop();
            }
            else
            {
                testlogo.Start();
            }
        }
    }
}
