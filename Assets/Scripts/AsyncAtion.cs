using System.Threading.Tasks;
using UnityEngine;

public class AsyncAtion : MonoBehaviour,IAsyncInitCall
{
    public GameObject getGameObj => this.gameObject;
    public string LabelInit => "Start AsyncAtion";

    public string LabelEnd => "End AsyncAtion";

    public async Task InitAsync()
    {
        using var clock = new DisposableClockwatch();
        Debug.Log($"{LabelInit}");
        await Task.Delay(4000);
        Debug.Log($"{LabelEnd}");
    }

}
