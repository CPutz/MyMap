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
            if(First == null)
                return default(T);

            LRUCacheNode<T> node = First;

            // Find node
            while(node.Id != id)
            {
                if(node.Next == null)
                    return default(T);
                node = node.Next;
            }

            // Move node to front
            if(node != First)
            {
                Remove(node);
                Add(node);
            }

            return node.Value;
        }

        public void Remove(LRUCacheNode<T> node)
        {
            LRUCacheNode<T> next = node.Next;
            LRUCacheNode<T> prev = node.Previous;

            if(prev != null)
                prev.Next = next;
            else
                First = next;

            if(next != null)
                next.Previous = prev;
            else
                Last = prev;

            node.Next = null;
            node.Previous = null;
        }

        public void Add(long id, T value)
        {
            Add(new LRUCacheNode<T>(id, value));
        }

        public void Add(LRUCacheNode<T> node)
        {
            size++;

            if(First == null)
            {
                First = node;
                Last = node;
                return;
            }

            node.Next = First;
            First.Previous = node;
            First = node;
            First.Previous = null;

            while(size > capacity)
            {
                Last = Last.Previous;
                Last.Next = null;
                size--;
            }
        }

        public long Size
        {
            get {
                return size;
            }
        }

        public long Capacity {
            get {
                return capacity;
            }
            set {
                this.capacity = value;
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
