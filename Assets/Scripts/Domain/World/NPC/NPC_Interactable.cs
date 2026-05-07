using UnityEngine;

[RequireComponent(typeof(TextController))]
public class NPC_Interactable : NPC, IInteractable
{
    private bool isReady;
    public bool IsReady => isReady;

    TextController textController;

    private void Awake()
    {
        textController = GetComponent<TextController>();
    }
    public void Interact()
    {
        textController.ChangeText("");
    }
}
