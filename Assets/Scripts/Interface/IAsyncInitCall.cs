using System.Threading.Tasks;
using UnityEngine;

public interface IAsyncInitCall
{
    GameObject getGameObj { get; }
    Task InitAsync();
    string LabelInit { get; }
    string LabelEnd { get; }
}
