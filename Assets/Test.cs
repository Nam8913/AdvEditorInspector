using System;
using UnityEngine;

public class Test : MonoBehaviour
{
    [ShowOnly]
    public float testFloat = 0.0f;

    public Color testColor = Color.white;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        MyLogger.Log("Gameplay", "Started game!");
        MyLogger.Log("Gameplay", "Test!", this);
        MyLogger.Log("Gameplay", "Test1!", this);
        MyLogger.Log("Gameplay", "Test2!", this);
        MyLogger.Log("Gameplay", "Test3!", this);
        MyLogger.Log("Gameplay", "Test4!", this);
        MyLogger.Log("Network", "Connected to server.");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
public static class MyLogger
{
    public static void Log(string channel, string message, UnityEngine.Object context = null)
    {
        string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        string formattedMessage = $"({timestamp})-[{channel}]-:{message}";
        Debug.Log(formattedMessage, context);
    }
}