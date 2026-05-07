using UnityEngine;

[RequireComponent(typeof(TextController))]
public class NPC_Interactable : NPC, IInteractable
{
    private bool isReady;
    public bool IsReady => isReady;

    [SerializeField] TextController textController;

    public void Interact()
    {
        textController.ChangeText("Hello");
        textController.RequestView();
    }
}
