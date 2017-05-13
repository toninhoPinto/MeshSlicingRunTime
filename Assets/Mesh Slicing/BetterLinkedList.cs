using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BetterLinkedList<T>  {

    int count;
    public Node<T> start;
    public Node<T> end;

	public BetterLinkedList()
    {
        count = 0;
    }

    public void Add(T newElement)
    {
        Node<T> newNode = new Node<T>(newElement);

        if (count == 0)
        {
            start = newNode;
        }

        end.SetNext(newNode);
        end = newNode;
        count++;
    }

    public void Merge(BetterLinkedList<T> list)
    {
        end.SetNext(list.start);
        count += list.count;
    }

}


public class Node<T>{

    Node<T> nextNode;
    T value;

    public Node(T value)
    {
        this.value = value;
    }

    public void SetNext(Node<T> nextNode)
    {
        this.nextNode = nextNode;
    }

}