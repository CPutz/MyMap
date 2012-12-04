﻿using System;
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
            root = RBInsert(root, item, false);
            root.Color = RB.Black;
        }

        private RBNode RotateRight(RBNode n)
        {
            RBNode r = n.Parent;

            n.Right.Parent = r;
            r.Left = n.Right;

            n.Right = r;
            r.Parent = n;

            root = n;
            n.Parent = null;

            return n;
        }
        private RBNode RotateLeft(RBNode n)
        {
            RBNode r = n.Parent;

            n.Left.Parent = r;
            r.Right = n.Left;

            n.Left = r;
            r.Parent = n;

            root = n;
            n.Parent = null;

            return n;
        }

        private RBNode RBInsert(RBNode n, Node item, bool sw)
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
                n.Left = RBInsert(n.Left, item, false);
                n.Left.Parent = n;
                if (n.Color == RB.Red && n.Left.Color == RB.Red && sw)
                {
                    n = RotateRight(n.Left);
                }
                if (n.Left.Color == RB.Red && n.Left.Left != null && n.Left.Left.Color == RB.Red)
                {
                    n = RotateRight(n.Left);
                    n.Color = RB.Black;
                    n.Right.Color = RB.Red;
                }
            }
            else
            {
                n.Right = RBInsert(n.Right, item, true);
                n.Right.Parent = n;
                if (n.Color == RB.Red && n.Right.Color == RB.Red && sw)
                {
                    n = RotateLeft(n.Right);
                }
                if (n.Right.Color == RB.Red && n.Right.Right != null && n.Right.Right.Color == RB.Red)
                {
                    n = RotateLeft(n.Right);
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
            //this.m_left = leaf;
            //this.m_right = leaf;
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

        public RBNode GrandParent()
        {
            return this.m_parent.Parent;
        }

        public RBNode Uncle()
        {
            RBNode g = this.GrandParent();
            if (g == null)
                return null;
            else if (this.m_parent == g.Left)
                return g.Right;
            else
                return g.Left;
        }
    }
}
