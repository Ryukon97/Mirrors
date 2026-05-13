using UnityEngine;

public interface IDestroyable
{
    bool IsReady { get; }
    void TryDestroy();
    
}
