using System;
using System.Collections.Generic;

namespace MyMap
{
    /// <summary>
    /// Just like the RBTree, but with a list in every node. That way
    /// multiple things can be stored with the same index. Either null or
    /// a list containing every item with the given id will be returned.
    /// </summary>
    public class ListTree : RBTree
    {
        public override void Insert(long identifier, object item)
        {
            object target;
            if((target = GetNode(identifier)) == null)
                Insert(identifier, target = new List<object>());
            ((List<object>)target).Add(item);
        }
    }
}

