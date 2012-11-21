using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using OSMPBF;

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
            if(args.Length == 1) {
                analyzeFile(args[0]);
                return;
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }

        static void analyzeFile(string s) {
            FileStream f = new FileStream(s, FileMode.Open);

            while(true) {
                // Getting length of BlobHeader
                int length = 0;
                for(int i = 0; i < 4; i++)
                    length = length*255 + f.ReadByte();

                // Reading BlobHeader
                byte[] buf = new byte[length];
                f.Read(buf, 0, length);
                BlobHeader head = BlobHeader.ParseFrom(buf);
                Console.WriteLine("Blob of type " + head.Type + " and size " + head.Datasize);
                //Console.WriteLine("datasize:" + head.Datasize);
                //Console.WriteLine("type:" + head.Type);

                f.Seek(head.Datasize, SeekOrigin.Current);
                //f.Read(buf, 0, head.Datasize);
                //Blob b = Blob.ParseFrom(buf);

            }
        }
    }
}
