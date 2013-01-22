using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyMap
{
    public enum RB { Black, Red };

    /// <summary>
    /// Implementation of a Red-Black Tree
    /// Documentation: http://en.wikipedia.org/wiki/Red%E2%80%93black_tree
    /// </summary>
    public class RBTree<T>
    {
        protected RBNode<T> root;
        private long size = 0;

        object writeLock = new object();

        public RBTree()
        {
            this.root = null;
        }

        public virtual T Get(long id)
        {
            return GetNode(id).Content;
        }

        public RBNode<T> GetNode(long id)
        {
            return Search(root, id);
        }

        public long Count
        {
            get { return size; }
        }

        /// <summary>
        /// Returns the node with the given id, in the tree beneath
        /// the given node.
        /// Returns a node with default(T) if not found, with the
        /// parent that it would have had if it had existed.
        /// </summary>
        private RBNode<T> Search(RBNode<T> node, long identifier)
        {
            if(node == null)
                return new RBNode<T>(identifier, default(T), null);

            if (node.ID > identifier)
            {
                if(node.Left == null)
                    return new RBNode<T>(identifier, default(T), node);
                return Search(node.Left, identifier);
            } else if (node.ID < identifier)
            {
                if(node.Right == null)
                    return new RBNode<T>(identifier, default(T), node);
                return Search(node.Right, identifier);
            } else
                return node;
        }


        //Inserts a RBNode holding a Node item
        public void Insert(long identifier, T item)
        {
            lock(writeLock)
            {
                root = RBInsert(identifier, root, item);
                root.Color = RB.Black;
                size++;
            }
        }


        public IEnumerator<T> GetEnumerator() {
            if(root == null)
                yield break;
            foreach(RBNode<T> node in root)
            {
                if(node != null)
                {
                    yield return node.Content;
                }
            }
        }


        /// <summary>
        /// Insert Algortihm
        /// </summary>
        private RBNode<T> RBInsert(long identifier, RBNode<T> n, T item)
        {          
            if (n == null)
                return new RBNode<T>(identifier, item, null);

            //flip
            if (n.Left != null && n.Right != null && 
                n.Left.Color == RB.Red && n.Right.Color == RB.Red)
            {
                Flip(n);
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
                if (n.Left != null && n.Left.Color == RB.Red &&
                    n.Left.Left != null && n.Left.Left.Color == RB.Red)
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
                if (n.Right != null && n.Right.Color == RB.Red &&
                    n.Right.Right != null && n.Right.Right.Color == RB.Red)
                {
                    n = RotateRight(n);
                    n.Color = RB.Black;
                    n.Left.Color = RB.Red;
                }
            }

            return n;
        }

        //Right rotation around node r
        private RBNode<T> RotateRight(RBNode<T> r)
        {
            RBNode<T> n = r.Right;

            n.Parent = r.Parent;
            r.Right = n.Left;

            if (r.Right != null)
                r.Right.Parent = r;

            r.Parent = n;
            n.Left = r;

            return n;
        }
        //Left rotation around node r
        private RBNode<T> RotateLeft(RBNode<T> r)
        {
            RBNode<T> n = r.Left;

            n.Parent = r.Parent;
            r.Left = n.Right;

            if (r.Left != null)
                r.Left.Parent = r;

            r.Parent = n;
            n.Right = r;

            return n;
        }

        //flips node n and it's childs
        private void Flip(RBNode<T> n)
        {
            n.Color = RB.Red;
            n.Left.Color = RB.Black;
            n.Right.Color = RB.Black;
        }
    }

    
    /// <summary>
    /// A Node in the RBTree.
    /// Holds a Content item of type T.
    /// It always is a Red or a Black node.
    /// </summary>
    public class RBNode<T>
    {
        private RBNode<T> m_left;
        private RBNode<T> m_right;
        private RBNode<T> m_parent;
        public T Content;
        private RB m_color;
        private long id;

        public RBNode(long identifier, T item, RBNode<T> parent)
        {
            id = identifier;
            this.m_parent = parent;
            this.Content = item;
            this.Color = RB.Red;
        }

        public IEnumerator<RBNode<T>> GetEnumerator() {
            if (Left != null)
            {
                foreach (RBNode<T> node in Left)
                {
                    if (node != null)
                    {
                        yield return node;
                    }
                }
            }
            yield return this;
            if (Right != null)
            {
                foreach (RBNode<T> node in Right)
                {
                    if (node != null)
                    {
                        yield return node;
                    }
                }
            }
        }

        #region properties

        public long ID
        {
            get { return id; }
            set { id = value; } //nodig bij delete
        }

        public RB Color
        {
            get { return m_color; }
            set { m_color = value; }
        }

        public RBNode<T> Left
        {
            get { return m_left; }
            set { m_left = value; }
        }

        public RBNode<T> Right
        {
            get { return m_right; }
            set { m_right = value; }
        }

        public RBNode<T> Parent
        {
            get { return m_parent; }
            set { m_parent = value; }
        }
        #endregion
    }
}
