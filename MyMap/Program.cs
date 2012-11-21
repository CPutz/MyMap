using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.IO;

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
            FileStream f = new FileStream("/home/sophie/Projects/Introductie/MyMap/netherlands.osm.pbf",
                                          FileMode.Open);
            //OSMPBF.BlobHeader.ParseFrom(f);
            Console.WriteLine("" + f.Length);
            return;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
