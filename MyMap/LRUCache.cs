using System;
using System.Collections.Generic;

namespace MyMap
{
    /*
     * LRU stands for Least Recently Used. It remembers the last
     * things that have been requested.
     * 
     * Because we can't see into the future, LRU is probably
     * the best caching method for us mortals
     */
    public class LRUCache<T>
    {
        long capacity;
        long size = 0;

        public LRUCacheNode<T> First, Last;

        public LRUCache(int capacity)
        {
            this.capacity = capacity;
        }

        public T Get(long id)
        {
            LRUCacheNode<T> node = First;

            // Find node
            while(node.Id != id)
            {
                if(node.Next == null)
                    return default(T);
                node = node.Next;
            }

            // If there was only 1 node to start with
            if(node.Previous == null)
                return default(T);

            // Move node to the from of the row
            node.Previous.Next = node.Next;
            node.Next.Previous = node.Previous;
            node.Next = First;

            if(Last == node && First != node)
                Last = node.Previous;
            
            First = node;
            node.Previous = null;

            return node.Value;
        }

        public void Add(long id, T value)
        {
            First = new LRUCacheNode<T>(id, value);
            size++;

            if(Last == null)
                Last = First;

            if(Last.Previous != null)
            {
                while(size > capacity)
                {
                    Last = Last.Previous;
                    Last.Next = null;
                }
            }
        }

        public long Size
        {
            set {
                this.capacity = capacity;
                if(size > capacity)
                {
                    LRUCacheNode<T> node = First;
                    for(int i = 0; i < capacity; i++)
                    {
                        node = node.Next;
                    }
                    node.Previous.Next = null;
                    Last = node.Previous;
                }
            }
            get {
                return size;
            }
        }

        public long Capacity {
            get {
                return capacity;
            }
        }
    }

    public class LRUCacheNode<T>
    {
        public LRUCacheNode<T> Next, Previous;
        public T Value;
        public long Id;

        public LRUCacheNode(long id, T value){
            Value = value;
            Id = id;
        }
    }
}
