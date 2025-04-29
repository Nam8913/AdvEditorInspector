using UnityEngine;
using System;
using System.Collections.Generic;

public class MainThreadDispatcher : MonoBehaviour
{
    private static readonly Queue<Action> executeQueue = new Queue<Action>();

    public static void RunOnMainThread(Action action)
    {
        lock (executeQueue)
        {
            executeQueue.Enqueue(action);
        }
    }

    void Update()
    {
        lock (executeQueue)
        {
            while (executeQueue.Count > 0)
            {
                executeQueue.Dequeue()?.Invoke();
            }
        }
    }
}
