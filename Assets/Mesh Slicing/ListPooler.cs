using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ListPooler {

    List<List<Vector3>> poolingList;
    List<List<Vector4>> poolingListVector4;
    List<List<Vector2>> poolingListVector2;

    List<BetterLinkedList<Vector3>> poolingLinkedList;
    List<BetterLinkedList<Vector4>> poolingLinkedListVector4;
    List<BetterLinkedList<Vector2>> poolingLinkedListVector2;

    List<List<int>> poolingListIndexes;
    List<OrderedHashSet<Vector3>> poolingHashSets;
    VectorEqualityComparer comparer;


    // Use this for initialization
    public ListPooler () {
        poolingList = new List<List<Vector3>>();
        poolingListVector4 = new List<List<Vector4>>();
        poolingListVector2 = new List<List<Vector2>>();

        poolingLinkedList = new List<BetterLinkedList<Vector3>>();
        poolingLinkedListVector4 = new List<BetterLinkedList<Vector4>>();
        poolingLinkedListVector2 = new List<BetterLinkedList<Vector2>>();

        poolingListIndexes = new List<List<int>>();
        poolingHashSets = new List<OrderedHashSet<Vector3>>();
        comparer = new VectorEqualityComparer();
    }

    public List<Vector3> GetPooledListVector3()
    {
        int size = poolingList.Count;

        if (size == 0)
            return new List<Vector3>();

        List<Vector3> toBeReturned = poolingList[size - 1];
        poolingList.RemoveAt(size - 1);

        return toBeReturned;
    }

    public List<Vector3> GetPooledListVector3(Vector3 a, Vector3 b, Vector3 c)
    {
        int size = poolingList.Count;

        if (size == 0)
            return new List<Vector3>() { a, b, c };

        List<Vector3> toBeReturned = poolingList[size - 1];
        poolingList.RemoveAt(size - 1);

        toBeReturned.Add(a);
        toBeReturned.Add(b);
        toBeReturned.Add(c);

        return toBeReturned;
    }

    public List<Vector2> GetPooledListVector2()
    {
        int size = poolingListVector2.Count;

        if (size == 0)
            return new List<Vector2>() { };

        List<Vector2> toBeReturned = poolingListVector2[size - 1];
        poolingListVector2.RemoveAt(size - 1);
        return toBeReturned;
    }

    public List<Vector2> GetPooledListVector2(Vector2 a, Vector2 b, Vector2 c)
    {
        int size = poolingListVector2.Count;

        if (size == 0)
            return new List<Vector2>() { a, b, c };

        List<Vector2> toBeReturned = poolingListVector2[size - 1];
        poolingListVector2.RemoveAt(size - 1);

        toBeReturned.Add(a);
        toBeReturned.Add(b);
        toBeReturned.Add(c);

        return toBeReturned;
    }

    public List<Vector4> GetPooledListVector4()
    {
        int size = poolingListVector4.Count;

        if (size == 0)
            return new List<Vector4>();

        List<Vector4> toBeReturned = poolingListVector4[size - 1];
        poolingListVector4.RemoveAt(size - 1);

        return toBeReturned;
    }

    public List<Vector4> GetPooledListVector4(Vector4 a, Vector4 b, Vector4 c)
    {
        int size = poolingListVector4.Count;

        if (size == 0)
            return new List<Vector4>() { a, b, c };

        List<Vector4> toBeReturned = poolingListVector4[size - 1];
        poolingListVector4.RemoveAt(size - 1);

        toBeReturned.Add(a);
        toBeReturned.Add(b);
        toBeReturned.Add(c);

        return toBeReturned;
    }


    public BetterLinkedList<Vector3> GetPooledLinkedListVector3(Vector3 a, Vector3 b, Vector3 c)
    {
        int size = poolingLinkedList.Count;

        BetterLinkedList<Vector3> toBeReturned;

        if (size == 0)
            toBeReturned = new BetterLinkedList<Vector3>();
        else
        {
            toBeReturned = poolingLinkedList[size - 1];
            poolingLinkedList.RemoveAt(size - 1);
        }

        toBeReturned.Add(a);
        toBeReturned.Add(b);
        toBeReturned.Add(c);

        return toBeReturned;
    }

    public BetterLinkedList<Vector2> GetPooledLinkedListVector2(Vector2 a, Vector2 b, Vector2 c)
    {
        int size = poolingLinkedListVector2.Count;

        BetterLinkedList<Vector2> toBeReturned;

        if (size == 0)
            toBeReturned = new BetterLinkedList<Vector2>();
        else
        {
            toBeReturned = poolingLinkedListVector2[size - 1];
            poolingLinkedListVector2.RemoveAt(size - 1);
        }

        toBeReturned.Add(a);
        toBeReturned.Add(b);
        toBeReturned.Add(c);

        return toBeReturned;
    }

    public BetterLinkedList<Vector4> GetPooledLinkedListVector4(Vector4 a, Vector4 b, Vector4 c)
    {
        int size = poolingLinkedListVector4.Count;

        BetterLinkedList<Vector4> toBeReturned;

        if (size == 0)
            toBeReturned = new BetterLinkedList<Vector4>();
        else
        {
            toBeReturned = poolingLinkedListVector4[size - 1];
            poolingLinkedListVector4.RemoveAt(size - 1);
        }

        toBeReturned.Add(a);
        toBeReturned.Add(b);
        toBeReturned.Add(c);

        return toBeReturned;
    }



    public List<int> GetPooledList()
    {
        int size = poolingListIndexes.Count;

        if (size == 0)
            return new List<int>();

        List<int> toBeReturned = poolingListIndexes[size - 1];
        poolingListIndexes.RemoveAt(size - 1);

        return toBeReturned;
    }

    public OrderedHashSet<Vector3> GetPooledHashSet(Vector3 a, Vector3 b, Vector3 c)
    {
        int size = poolingHashSets.Count;

        if (size == 0)
            return new OrderedHashSet<Vector3>(comparer) { a, b, c };

        OrderedHashSet<Vector3> toBeReturned = poolingHashSets[size - 1];
        poolingHashSets.RemoveAt(size - 1);

        toBeReturned.Add(a);
        toBeReturned.Add(b);
        toBeReturned.Add(c);

        return toBeReturned;
    }

    public void PoolList(List<Vector3> dedList)
    {
        dedList.Clear();
        poolingList.Add(dedList);
    }

    public void PoolList(List<Vector4> dedList)
    {
        dedList.Clear();
        poolingListVector4.Add(dedList);
    }

    public void PoolList(List<Vector2> dedList)
    {
        dedList.Clear();
        poolingListVector2.Add(dedList);
    }

    public void PoolList(List<int> dedList)
    {
        dedList.Clear();
        poolingListIndexes.Add(dedList);
    }

    public void PoolList(BetterLinkedList<Vector3> dedList)
    {
        dedList.Clear();
        poolingLinkedList.Add(dedList);
    }

    public void PoolList(BetterLinkedList<Vector2> dedList)
    {
        dedList.Clear();
        poolingLinkedListVector2.Add(dedList);
    }

    public void PoolList(BetterLinkedList<Vector4> dedList)
    {
        dedList.Clear();
        poolingLinkedListVector4.Add(dedList);
    }

    public void PoolHashSet(OrderedHashSet<Vector3> dedHashSet)
    {
        dedHashSet.Clear();
        poolingHashSets.Add(dedHashSet);
    }


}
