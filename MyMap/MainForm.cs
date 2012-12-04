using System;
using System.Windows.Forms;

namespace MyMap
{
    class MainForm : Form
    {
        public MainForm()
        {
            RBTree tree = new RBTree();
            tree.Insert(new Node(0, 0, 10));
            tree.Insert(new Node(0, 0, 11));
            tree.Insert(new Node(0, 0, 2));
            tree.Insert(new Node(0, 0, 1));
            tree.Insert(new Node(0, 0, 5));
            tree.Insert(new Node(0, 0, 4));
            tree.Insert(new Node(0, 0, 6));
            tree.Insert(new Node(0, 0, 3));
            tree.Insert(new Node(0, 0, 20));
        }
    }
}
