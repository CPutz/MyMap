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

        public T GetSmallest()
        {
            List<T> t = base.GetSmallest();
            if (t.Count != 0)
                return t[0];
            return default(T);
        }

        public bool Contains(Node node)
        {
            long identifier = (long)(node.TentativeDist * 100000000);
            return Contains((T)Convert.ChangeType(node, typeof(T)), identifier);
        }

        private bool Contains(T item, long identifier)
        {
            List<T> list = Get(identifier);
            return list.Contains(item);
        }

        public void Remove(T item, long identifier)
        {
            List<T> list = Get(identifier);
            list.Remove(item);
            if (list.Count == 0)
                RemoveAt(identifier);
        }

        public void Insert(long identifier, T item)
        {
            List<T> target;
            if((target = GetNode(identifier).Content) == null)
                Insert(identifier, target = new List<T>());
            ((List<T>)target).Add(item);
        }
        
        public new IEnumerator<T> GetEnumerator() {
            foreach(RBNode<List<T>> node in root)
            {
                if(node != null)
                {
                    foreach(T t in node.Content)
                        yield return t;
                }
            }
        }
    }
}

