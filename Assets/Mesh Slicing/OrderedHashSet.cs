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

/*
 * 
 * struct FastEnumIntEqualityComparer<TEnum> : IEqualityComparer<TEnum>
        where TEnum : struct
{
    static class BoxAvoidance
    {
        static readonly Func<TEnum, int> _wrapper;

        public static int ToInt(TEnum enu)
        {
            return _wrapper(enu);
        }

        static BoxAvoidance()
        {
            var p = Expression.Parameter(typeof(TEnum), null);
            var c = Expression.ConvertChecked(p, typeof(int));

            _wrapper = Expression.Lambda<Func<TEnum, int>>(c, p).Compile();
        }
    }

    public bool Equals(TEnum firstEnum, TEnum secondEnum)
    {
        return BoxAvoidance.ToInt(firstEnum) ==
            BoxAvoidance.ToInt(secondEnum);
    }

    public int GetHashCode(TEnum firstEnum)
    {
        return BoxAvoidance.ToInt(firstEnum);
    }
}
*/