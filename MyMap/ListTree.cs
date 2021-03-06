using System;
using System.Collections.Generic;

namespace MyMap
{
    /// <summary>
    /// Just like the RBTree, but with a list in every node. That way
    /// multiple things can be stored with the same index. Either null or
    /// a list containing every item with the given id will be returned.
    /// </summary>
    public class ListTree<T> : RBTree<List<T>>
    {
        public override List<T> Get(long id)
        {
            List<T> t = base.Get(id);
            if(t != null)
                return t;
            return new List<T>();
        }

        public void Insert(long identifier, T item)
        {
            List<T> target;
            if((target = GetNode(identifier).Content) == null)
                Insert(identifier, target = new List<T>());
            ((List<T>)target).Add(item);
        }
        
        public new IEnumerator<T> GetEnumerator() {
            if (root != null)
            {
                foreach (RBNode<List<T>> node in root)
                {
                    if (node != null)
                    {
                        foreach (T t in node.Content)
                            yield return t;
                    }
                }
            }
        }
    }
}

