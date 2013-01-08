using System;
using System.Threading;

namespace MyMap
{
    public class LoadingThread
    {
        private Thread t;
        private Graph g;

        public LoadingThread(string path)
        {
            t = new Thread(new ThreadStart(() => { g = new Graph(path); }));
            t.Start();
        }

        public Graph Graph
        {
            get { return g; }
        }
    }
}
