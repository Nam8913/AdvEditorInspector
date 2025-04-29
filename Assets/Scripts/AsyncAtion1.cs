using System.Threading.Tasks;
using UnityEngine;

public class AsyncAtion1 : MonoBehaviour,IAsyncInitCall
{
    public GameObject getGameObj => this.gameObject;
    public string LabelInit => "Start AsyncAtion1";

    public string LabelEnd => "End AsyncAtion1";

    public async Task InitAsync()
    {
        using var clock = new DisposableClockwatch();
        Debug.Log($"{LabelInit}");
        await Task.Delay(8000);
        Debug.Log($"{LabelEnd}");
        
    }

}
