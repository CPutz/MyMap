using System;
using System.Threading;

namespace MyMap
{
    /// <summary>
    /// Loads the graph by running it's constructor in a
    /// different thread than the main-thread.
    /// And can send the graph object to other classes if needed.
    /// </summary>
    public class LoadingThread
    {
        private Thread t;
        private Graph g;

        public LoadingThread(string path)
        {
            t = new Thread(new ThreadStart(() => {
                g = new Graph(path);
                Console.WriteLine("Setup done");
                t = null;
            }));
            t.Start();
        }

        public Graph Graph
        {
            get { return g; }
        }

        public void Abort() {
            if(t != null)
                t.Abort();
        }
    }
}
