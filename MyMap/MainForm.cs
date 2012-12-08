using System;
using System.Windows.Forms;

namespace MyMap
{
    class MainForm : Form
    {
        public MainForm()
        {
            //Graph graph = new Graph("input.osm.pbf");
            //RouteFinder rf = new RouteFinder();

            // Dummy output, distance between nodes with id 1 and 2
            //Console.WriteLine(rf.Dijkstra(graph, graph.GetNode(1),
            //            graph.GetNode(2), new Vehicle()));

            MapDisplay map = new MapDisplay(10, 10, this.ClientSize.Width - 20, this.ClientSize.Height - 20);
            this.Controls.Add(map);
        }
    }
}
