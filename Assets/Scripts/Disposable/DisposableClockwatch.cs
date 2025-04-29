using System;
using UnityEngine;

public class DisposableClockwatch : IDisposable
{
    System.Diagnostics.Stopwatch watch;
    public IAsyncInitCall obj;
    public void Dispose()
    {
        watch.Stop();
        var elapsed = watch.Elapsed;
        
        if(this.obj != null)
        {
            Debug.Log(this.obj.LabelEnd + "-:Elapsed time: " + elapsed);
        }else
        {
            Debug.Log("Elapsed time: " + elapsed);
        }
    }

    public DisposableClockwatch(GameObject obj = null)
    {
        if(obj != null)
        {
            this.obj = obj.GetComponent<IAsyncInitCall>();
        }
        if(this.obj != null)
        {
            Debug.Log("Start async init for" + obj.name + " : " + this.obj.LabelInit);
        }else
        {
            Debug.Log("Watcher");
        }
        watch = new System.Diagnostics.Stopwatch();
        watch.Start();
    }
}
