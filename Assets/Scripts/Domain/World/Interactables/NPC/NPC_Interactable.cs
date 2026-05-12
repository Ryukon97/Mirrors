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
        if(textController.controllerType == ETextControllerType.Auto)
        {
            _ = PlayNextNodeAuto();//Unity 6.5 ~ Available: .LogExceptionAndForget();
            isReady = false;
        }
        textController.SetAndPlayNode(rawTextNode);
    }

    async Awaitable PlayNextNodeAuto()
    {
        do
        {
            await Awaitable.WaitForSecondsAsync(2f);    // Todo: ref config SO
        } while (textController.PlayNextNode());
        isReady = true;
    }
}
