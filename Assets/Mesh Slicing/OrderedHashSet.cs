using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Profiling;
using System.Linq;

public class OrderedHashSet<T> : KeyedCollection<T, T>
{
    protected override T GetKeyForItem(T item)
    {
        return item;
    }

    public OrderedHashSet() : base()
    {

    }

    public OrderedHashSet(IEqualityComparer<T> thing) : base(thing)
    {

    }

    public OrderedHashSet<T> ConcatIt(OrderedHashSet<T> dest)
    {
        for (int i = 0; i < dest.Count; i++)
        {
            if (!Contains(dest[i]))
                Add(dest[i]);
        }

        return this;
    }
}


struct VectorEqualityComparer : IEqualityComparer<Vector3>
{
    public bool Equals(Vector3 firstV, Vector3 secondV)
    {
        return firstV.x == secondV.x && firstV.y == secondV.y && firstV.z == secondV.z;
    }

    public int GetHashCode(Vector3 firstV)
    {
        return firstV.GetHashCode();
    }

}
