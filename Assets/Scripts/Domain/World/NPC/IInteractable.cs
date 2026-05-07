using UnityEngine;

public interface IInteractable
{
    bool IsReady { get; }
    void Interact();
}
