using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;


public class PriorityQueue<o> {
    public int size;
    SortedDictionary<int, Queue<o>> queues;

    
    public PriorityQueue()
    {
        this.queues = new SortedDictionary<int, Queue<o>>();
        this.size = 0;
    }

    /* Check the Queues are not empty */
    public void checkNotEmpty()
    {
        if (size == 0)
        {
            throw new IndexOutOfRangeException("The Priority Queue is empty!");
        }
    }

    /* Pop the object with the lowest priority value */
    public o pop()
    {
        // Confirm the Queue is not empty.
        checkNotEmpty();

        // Remove the first item in the priority queue.
        foreach (System.Collections.Generic.Queue<o> q in queues.Values)
        {
            if (q.Count != 0)
            {
                size--;
                return q.Dequeue();
            }
        }
        throw new IndexOutOfRangeException("The Priority Queue is empty!");
    }

    /* Pop the object with a given priority value */
    public o pop(int priority)
    {
        // Confirm the Queue is not empty.
        checkNotEmpty();
        // Pop an item with given priority
        return queues[priority].Dequeue();
    }

    /* Peek into the queue with the lowest priority value */
    public o peek()
    {
        // Confirm the Queue is not empty.
        checkNotEmpty();
        // Find the first item in the queue.
        foreach (System.Collections.Generic.Queue<o> q in queues.Values)
        {
            if (q.Count != 0)
            {
                return q.Peek();
            }
        }
        throw new IndexOutOfRangeException("The Priority Queue is empty!");
    }

    /* Add a new object to the queue */
    public void push(o o, int priority)
    {
        if (!queues.ContainsKey(priority))
        {
            queues.Add(priority, new Queue<o>());
        }
        queues[priority].Enqueue(o);
        size++;
    }
}
