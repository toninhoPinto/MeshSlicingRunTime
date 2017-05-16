using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class BetterLinkedList<T>  {

    public int Count;
    public Node<T> start;
    public Node<T> end;
    public Node<T> current;

	public BetterLinkedList()
    {
        Count = 0;
    }

    public BetterLinkedList(T newElement1, T newElement2, T newElement3)
    {
        Count = 0;
        Add(newElement1);
        Add(newElement2);
        Add(newElement3);
    }

    public void Add(T newElement)
    {
        Node<T> newNode = new Node<T>(newElement);
        if (Count == 0)
        {
            start = newNode;
            end = newNode;
            current = newNode;
            Count++;
            return;
        }

        if (current != null && current == end && Count > 1)
        {
            end.SetNext(newNode);
            end = newNode;
            current = newNode;
            Count++;
        }
        else
        {
            start.value = newElement;
            current = start.nextNode;
        }

    }

    public void AddRange(BetterLinkedList<T> list)
    {
        end.SetNext(list.start);
        end = list.end;
        current = list.end;
        Count += list.Count;
    }

    public void AddRange(IOrderedEnumerable<T> list)
    {
        int size = list.Count<T>();
        for (int i = 0; i < size ; i++)
        {
            Add(list.ElementAt(i));
        }
        Count += size;
    }

    public void Clear()
    {
        current = null;
    }

    public List<T> ToList()
    {
        List<T> returnList = new List<T>();
        Node<T> head = start;
        returnList.Add(head.value);
        while (head != current)
        {
            returnList.Add(head.value);
            head = head.nextNode;
        }
        return returnList;
    }

}


public class Node<T>{

    public Node<T> nextNode;
    public T value;

    public Node(T value)
    {
        this.value = value;
    }

    public void SetNext(Node<T> nextNode)
    {
        this.nextNode = nextNode;
    }

}