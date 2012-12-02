using System;
using System.Windows.Forms;

namespace MyMap
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    public enum Vehicle { Car, Bicycle, Foot, Bus, Metro, Train };

    public enum CurveType { Street, Building, Land }; //hier kan nog meer bij komen...
}
