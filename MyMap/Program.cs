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
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            LoadingThread thread = new LoadingThread("input.osm.pbf");
            StartForm startForm = new StartForm();
            Application.Run(startForm);
            if(startForm.Gebruiker > -1)
            {
                MainForm mainForm = new MainForm(startForm.UserData,
                                                 startForm.Gebruiker,
                                                 thread);
                // BUG Poging tot het lezen of schrijven van beveiligd geheugen. Dit duidt er vaak op dat ander geheugen is beschadigd. regel hieronder. triggert bij invullen textbox.
                Application.Run(mainForm);
            }
            thread.Abort();
        }
    }
}
