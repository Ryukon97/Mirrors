using UnityEngine;

[RequireComponent(typeof(TextController))]
public class NPC_Interactable : NPC, IInteractable
{
    private bool isReady = true;
    public bool IsReady => isReady;

    [SerializeField] TextNode rawTextNode;

    [SerializeField] TextController textController;

    public void Interact()
    {
        if (IsReady == false) return;
        textController.SetAndPlayNode(rawTextNode);
    }
}
