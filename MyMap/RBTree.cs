using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyMap
{
    enum RB { Black, Red };

    /// <summary>
    /// Implementation of a Red-Black Tree
    /// </summary>
    class RBTree
    {
        private RBNode root;
        private long size = 0;

        public RBTree()
        {
            this.root = null;
        }

        public object GetNode(int id)
        {
            return Search(root, id);
        }

        public int Count
        {
            get { return size; }
        }

        private object Search(RBNode node, long id)
        {
            if (node == null)
                return null;
            else if (node.ID > id)
                return Search(node.Left, id);
            else if (node.ID < id)
                return Search(node.Right, id);
            else
                return node.Content;
        }

        //Inserts a RBNode holding a Node item
        public void Insert(long identifier, object item)
        {
            root = RBInsert(identifier, root, item);
            root.Color = RB.Black;
            size++;
        }

        /// <summary>
        /// Insert Algortihm
        /// </summary>
        private RBNode RBInsert(long identifier, RBNode n, object item)
        {          
            if (n == null)
                return new RBNode(identifier, item, null);

            //flip
            if ((n.Left != null && n.Right != null) && (n.Left.Color == RB.Red && n.Right.Color == RB.Red))
            {
                n.Color = RB.Red;
                n.Left.Color = RB.Black;
                n.Right.Color = RB.Black;
            }

            //go left branch
            if (identifier < n.ID)
            {
                n.Left = RBInsert(identifier, n.Left, item);
                n.Left.Parent = n;
                if (n.Color == RB.Red && n.Left.Color == RB.Red && n.Parent.Right == n)
                {
                    n = RotateLeft(n);
                }
                if (n.Left.Color == RB.Red && n.Left.Left != null && n.Left.Left.Color == RB.Red)
                {
                    n = RotateLeft(n);
                    n.Color = RB.Black;
                    n.Right.Color = RB.Red;
                }
            }

            //go right branch
            else
            {
                n.Right = RBInsert(identifier, n.Right, item);
                n.Right.Parent = n;
                if (n.Color == RB.Red && n.Right.Color == RB.Red && n.Parent.Left == n)
                {
                    n = RotateRight(n);
                }
                if (n.Right.Color == RB.Red && n.Right.Right != null && n.Right.Right.Color == RB.Red)
                {
                    n = RotateRight(n);
                    n.Color = RB.Black;
                    n.Left.Color = RB.Red;
                }
            }

            return n;
        }

        //Right rotation around node r
        private RBNode RotateRight(RBNode r)
        {
            RBNode n = r.Right;

            n.Parent = r.Parent;
            r.Right = n.Left;
            r.Parent = n;
            n.Left = r;

            return n;
        }
        //Left rotation around node r
        private RBNode RotateLeft(RBNode r)
        {
            RBNode n = r.Left;

            n.Parent = r.Parent;
            r.Left = n.Right;
            r.Parent = n;
            n.Right = r;

            return n;
        }
    }

    

    class RBNode
    {
        private RBNode m_left;
        private RBNode m_right;
        private RBNode m_parent;
        private object content;
        private RB m_color;
        private long id;

        public RBNode(long identifier, object item, RBNode parent)
        {
            id = identifier;
            this.m_parent = parent;
            this.content = item;
            this.Color = RB.Red;
        }

        #region properties
        public object Content
        {
            get { return content; }
            //set { node = value; }
        }

        public int ID
        {
            get { return id; }
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
