using System;
using System.Threading;
using UnityEngine;

[RequireComponent(typeof(TextController))]
public class NPC_Interactable : NPC, IInteractable
{
    private bool isReady = true;
    public bool IsReady => isReady;

    [SerializeField] TextNode rawTextNode;

    [SerializeField] TextController textController;
    public bool forceToInteractingState;
    public bool destroyOnInteractEnds;
    CancellationToken token;

    private void Awake()
    {
        token = destroyCancellationToken;
        textController.OnNodeChainFinished += DestroySelf;
    }
    private void DestroySelf() { if (destroyOnInteractEnds && gameObject != null) Destroy(gameObject); }
    private void OnDestroy()
    {

        textController.OnNodeChainFinished -= DestroySelf;
    }
    public void Interact()
    {
        if (IsReady == false) return;
        if(textController.controllerType == ETextControllerType.Auto)
        {
            _ = PlayNextNodeAuto();//Unity 6.5 ~ Available: .LogExceptionAndForget();
            isReady = false;
        }
        textController.SetAndPlayNode(rawTextNode);
        if (forceToInteractingState) Player.Instance.FSM.ChangeState(Player.Instance.FSM.interactingState);
    }

    async Awaitable PlayNextNodeAuto()
    {
        try
        {
            do
            {
                await Awaitable.WaitForSecondsAsync(2f);    // Todo: ref config SO
            } while (token.IsCancellationRequested == false && textController.PlayNextNode());
            isReady = true;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }
}
