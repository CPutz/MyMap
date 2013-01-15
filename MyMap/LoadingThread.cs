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
