using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyMap
{
    enum RB { Black, Red };

    class RBTree
    {
        private RBNode root;

        public RBTree()
        {
            this.root = null;
        }

        public void Insert(Node item)
        {
            root = RBInsert(root, item);
            root.Color = RB.Black;
        }

        public Node GetNode(int id)
        {
            return Search(root, id);
        }

        private Node Search(RBNode node, int id)
        {
            if (node == null)
                return null;
            else if (node.Node.ID > id)
                return Search(node.Left, id);
            else if (node.Node.ID < id)
                return Search(node.Right, id);
            else
                return node.Node;
        }

        private void RotateRight(RBNode r)
        {
            //RBNode r = n.Parent;
            RBNode n = r.Left;

            n.Right.Parent = r;
            r.Left = n.Right;
            n.Right = r;
            r.Parent = n;
            root = n;
            n.Parent = null;
        }
        private void RotateLeft(RBNode r)
        {
            //RBNode r = n.Parent;
            RBNode n = r.Right;

            n.Left.Parent = r;
            r.Right = n.Left;
            n.Left = r;
            r.Parent = n;
            root = n;
            n.Parent = null;
        }

        private RBNode RBInsert(RBNode n, Node item)
        {          
            if (n == null)
                return new RBNode(item, null);

            //Flip
            if ((n.Left != null && n.Right != null) && (n.Left.Color == RB.Red && n.Right.Color == RB.Red))
            {
                n.Color = RB.Red;
                n.Left.Color = RB.Black;
                n.Right.Color = RB.Black;
            }


            if (item.ID < n.Node.ID)
            {
                n.Left = RBInsert(n.Left, item);
                n.Left.Parent = n;
                if (n.Color == RB.Red && n.Left.Color == RB.Red && n.Parent.Right == n)
                {
                    RotateLeft(n.Parent);
                }
                if (n.Left.Color == RB.Red && n.Left.Left != null && n.Left.Left.Color == RB.Red)
                {
                    RotateRight(n);
                    n.Color = RB.Black;
                    n.Right.Color = RB.Red;
                }
            }
            else
            {
                n.Right = RBInsert(n.Right, item);
                n.Right.Parent = n;
                if (n.Color == RB.Red && n.Right.Color == RB.Red && n.Parent.Left == n)
                {
                    RotateRight(n.Parent);
                }
                if (n.Right.Color == RB.Red && n.Right.Right != null && n.Right.Right.Color == RB.Red)
                {
                    RotateLeft(n);
                    n.Color = RB.Black;
                    n.Left.Color = RB.Red;
                }
            }

            return n;
        }
    }

    

    class RBNode
    {
        private RBNode m_left;
        private RBNode m_right;
        private RBNode m_parent;
        private Node node;
        private RB m_color;

        public RBNode(Node item, RBNode parent)
        {
            this.m_parent = parent;
            this.node = item;
            this.Color = RB.Red;
        }

        #region properties
        public Node Node
        {
            get { return node; }
            //set { node = value; }
        }

        public RB Color
        {
            get { return m_color; }
            set { m_color = value; }
        }

        public RBNode Left
        {
            get { return m_left; }
            set { m_left = value; }
        }

        public RBNode Right
        {
            get { return m_right; }
            set { m_right = value; }
        }

        public RBNode Parent
        {
            get { return m_parent; }
            set { m_parent = value; }
        }
        #endregion
    }
}
