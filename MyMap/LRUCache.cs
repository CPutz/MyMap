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

        object writeLock = new object();

        LRUCacheNode<T> first, last;

        public LRUCache(int capacity)
        {
            this.capacity = capacity;
        }

        public T Get(long id)
        {
            if(first == null)
                return default(T);

            LRUCacheNode<T> node = first;

            if(node.Id == id)
                return node.Value;

            // Find node
            while(node.Id != id)
            {
                if(node.Next == null)
                    return default(T);
                node = node.Next;
            }
            
            lock(writeLock) {
                // Move node to front
                if(node != first)
                {
                    Remove(node);
                    Add(node);
                }
            }

            return node.Value;
        }

        // DOES NOT LOCK
        private void Remove(LRUCacheNode<T> node)
        {
            LRUCacheNode<T> next = node.Next;
            LRUCacheNode<T> prev = node.Previous;

            if(prev != null)
                prev.Next = next;
            else first = next;

            if(next != null)
                next.Previous = prev;
            else last = prev;

            node.Next = null;
            node.Previous = null;
        }

        public void Add(long id, T value)
        {
            lock(writeLock) {
                Add(new LRUCacheNode<T>(id, value));
            }
        }

        // DOES NOT LOCK
        private void Add(LRUCacheNode<T> node)
        {
            size++;

            if(first == null)
            {
                first = node;
                last = node;
                return;
            }

            node.Next = first;
            first.Previous = node;
            first = node;
            first.Previous = null;

            while(size > capacity)
            {
                last = last.Previous;
                last.Next = null;
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
                    LRUCacheNode<T> node = first;
                    for(int i = 0; i < capacity; i++)
                    {
                        node = node.Next;
                    }
                    node.Previous.Next = null;
                    last = node.Previous;
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
