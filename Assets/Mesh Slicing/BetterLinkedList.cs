using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BetterLinkedList<T>  {

    public int Count;
    public Node<T> start;
    public Node<T> end;

	public BetterLinkedList()
    {
        Count = 0;
    }

    public void Add(T newElement)
    {
        Node<T> newNode = new Node<T>(newElement);

        if (Count == 0)
        {
            start = newNode;
        }

        end.SetNext(newNode);
        end = newNode;
        Count++;
    }

    public void Merge(BetterLinkedList<T> list)
    {
        end.SetNext(list.start);
        Count += list.Count;
    }

    public void Clear()
    {

    }

    public List<T> ToList()
    {
        List<T> returnList = new List<T>();

        Node<T> head = start;
        while(head != end)
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