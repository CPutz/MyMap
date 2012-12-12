using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyMap
{
    public enum RB { Black, Red };

    /// <summary>
    /// Implementation of a Red-Black Tree
    /// </summary>
    public class RBTree<T>
    {
        protected RBNode<T> root;
        private long size = 0;

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

        public T GetSmallest()
        {
            RBNode<T> n;
            for (n = root; n.Left != null; n = n.Left) { }
            return n.Content;
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



        public bool Contains(RBNode<T> node)
        {
            return RBContains(root, node.ID);
        }

        private bool RBContains(RBNode<T> node, long identifier)
        {
            if (node == null)
                return false;

            if (node.ID > identifier)
                return RBContains(node.Left, identifier);
            else if (node.ID < identifier)
                return RBContains(node.Right, identifier);
            else
                return true;
        }



        //Inserts a RBNode holding a Node item
        public void Insert(long identifier, T item)
        {
            root = RBInsert(identifier, root, item);
            root.Color = RB.Black;
            size++;
        }


        public IEnumerator<T> GetEnumerator() {
            foreach(RBNode<T> node in root)
            {
                if(node != null)
                {
                    yield return node.Content;
                }
            }
        }


        public void Remove(RBNode<T> n)
        {
            RemoveAt(n.ID);
        }

        public void RemoveAt(long identifier)
        {
            RBRemove(root, identifier);
            root.Color = RB.Black;
            size--;
        }


        /// <summary>
        /// Remove Algorithm
        /// </summary>
        private RBNode<T> RBRemove(RBNode<T> n, long identifier)
        {
            if (n != null)
            {
                if (n == root && n.Left != null && n.Right != null && 
                    n.Left.Color == RB.Black && n.Right.Color == RB.Black)
                    n.Color = RB.Red;

                //node found
                if (n.ID == identifier)
                {
                    RBNode<T> x;

                    if (n.Left == null || n.Right == null)
                    {
                        if (n.Left != null)
                            x = n.Left;
                        else
                            x = n.Right;

                        if (x != null)
                            x.Parent = n.Parent;

                        if (n.Parent == null)
                            return null;
                        else if (n == n.Parent.Left)
                            n.Parent.Left = x;
                        else
                            n.Parent.Right = x;

                        if (x != null &&  n.Color == RB.Black)
                            x.Color = RB.Black;


                        //dispose n


                        return x;
                    }
                    else
                    {
                        for (x = n.Right; x.Left != null; x = x.Left) { }
                        n.ID = x.ID;
                        identifier = x.ID;
                    }
                }


                if (n.ID <= identifier)
                {
                    if (n.Right.Left != null && n.Right.Right != null && 
                        n.Right.Color == RB.Black && n.Right.Left.Color == RB.Black && n.Right.Right.Color == RB.Black)
                    {
                        if (n.Color == RB.Black)
                        {
                            n = RotateRight(n);
                            ChangeColor(n);
                            ChangeColor(n.Right);
                        }
                        else
                        {
                            if (n.Left.Left != null && n.Left.Right != null && 
                                n.Left.Color == RB.Black && n.Left.Left.Color == RB.Black && n.Left.Right.Color == RB.Black)
                            {
                                Flip(n);
                            }
                            else
                            {
                                if (n.Left.Right.Color == RB.Black)
                                {
                                    n = RotateRight(n);
                                    ChangeColor(n);
                                    ChangeColor(n.Left);
                                    ChangeColor(n.Right);
                                    ChangeColor(n.Right.Right);
                                }
                                else
                                {
                                    n.Left = RotateLeft(n.Left);
                                    n = RotateRight(n);
                                    ChangeColor(n.Right);
                                    ChangeColor(n.Right.Right);
                                }
                            }
                        }
                    }

                    n.Right = RBRemove(n.Right, identifier);
                }
                else if (n.ID > identifier)
                {
                    if (n.Left.Left != null && n.Left.Right != null && 
                        n.Left.Color == RB.Black && n.Left.Left.Color == RB.Black && n.Left.Right.Color == RB.Black)
                    {
                        if (n.Color == RB.Black)
                        {
                            n = RotateLeft(n);
                            ChangeColor(n);
                            ChangeColor(n.Left);
                        }
                        else
                        {
                            if (n.Right.Left != null && n.Right.Right != null && 
                                n.Right.Color == RB.Black && n.Right.Left.Color == RB.Black && n.Right.Right.Color == RB.Black)
                            {
                                Flip(n);
                            }
                            else
                            {
                                if (n.Right.Left.Color == RB.Black)
                                {
                                    n = RotateLeft(n);
                                    ChangeColor(n);
                                    ChangeColor(n.Right);
                                    ChangeColor(n.Left);
                                    ChangeColor(n.Left.Left);
                                }
                                else
                                {
                                    n.Right = RotateRight(n.Right);
                                    n = RotateLeft(n);
                                    ChangeColor(n.Left);
                                    ChangeColor(n.Left.Left);
                                }
                            }
                        }
                    }

                    n.Left = RBRemove(n.Left, identifier);
                }

                return n;
            }

            return null;
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


        private void ChangeColor(RBNode<T> n)
        {
            if (n.Color == RB.Black)
                n.Color = RB.Red;
            else
                n.Color = RB.Black;
        }
    }

    

    public class RBNode<T>
    {
        private RBNode<T> m_left;
        private RBNode<T> m_right;
        private RBNode<T> m_parent;
        private T content;
        private RB m_color;
        private long id;

        public RBNode(long identifier, T item, RBNode<T> parent)
        {
            id = identifier;
            this.m_parent = parent;
            this.content = item;
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
        public T Content
        {
            get { return content; }
        }

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
