using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Test : MonoBehaviour
{
    [ShowOnly]
    public float testFloat = 0.0f;
    private readonly List<IAsyncInitCall> systems = new();

    public Color testColor1;
    public CustomColor testColor = new CustomColor(Color.white);
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        RegisterSystems();
    }
    async void Start()
    {
        // foreach (var item in systems)
        // {
        //     using (new DisposableClockwatch(item.getGameObj))
        //     {
        //         await item.InitAsync();
        //     }
        //     MainThreadDispatcher.RunOnMainThread(() => {
        //         Debug.Log("Test2!");
        //     });
        // }
        //using var clock = new DisposableClockwatch();
        List<Task> tasks = systems.Select(f => f.InitAsync()).ToList();
        await Task.WhenAll(tasks);
    }
    void RegisterSystems()
    {
        foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
        {
            if (typeof(IAsyncInitCall).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
            {
                // Nếu là MonoBehaviour thì tìm instance trong scene
                if (typeof(MonoBehaviour).IsAssignableFrom(type))
                {
                    var found = FindObjectsByType(type,FindObjectsSortMode.None);
                    if (found != null)
                    foreach (var item in found)
                    {
                        systems.Add((IAsyncInitCall)item);
                    }
                       
                }
                else
                {
                    // Nếu là class thường => new
                    var instance = Activator.CreateInstance(type) as IAsyncInitCall;
                    if (instance != null)
                        systems.Add(instance);
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        Custom.Log("Update!!!!");
    }
}
public static class Custom
{
    private static Queue<LogQueue> logQueue = new Queue<LogQueue>();
    public static void Log (string message,string channel = "Gameplay", UnityEngine.Object context = null)
    {
        string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        string formattedMessage = $"({timestamp})-[{channel}]-:{message}";
        System.Diagnostics.StackTrace t = new System.Diagnostics.StackTrace(true);
        LogQueue log = new LogQueue(formattedMessage,t.ToString(),context,"Info");
        logQueue.Enqueue(log);

        //Debug.Log(formattedMessage, context);
    }
    public static Queue<LogQueue> GetLogs
    {
        get
        {
            return logQueue;
        }
    }
    public static void ClearLogs()
    {
        logQueue.Clear();
    }
    public struct LogQueue
    {
        public string msg, stackTrace;
        public UnityEngine.Object cxt;
        public string Type;
        public int Count;

        public LogQueue(string msg , string stackTrace, UnityEngine.Object cxt, string type)
        {
            this.msg = msg;
            this.stackTrace = stackTrace;
            this.cxt = cxt;
            this.Type = type;
            this.Count = 0;
        }

        public UnityEngine.LogType GetLogType()
        {
            switch(this.Type)
            {
                case "Error":
                    return UnityEngine.LogType.Error;
                case "Warning":
                    return UnityEngine.LogType.Warning;
                case "Info":
                    return UnityEngine.LogType.Log;
                case "Assert":
                    return UnityEngine.LogType.Assert;
                case "Exception":
                    return UnityEngine.LogType.Exception;
                default:
                    return UnityEngine.LogType.Log;
            }
        }
    }
}