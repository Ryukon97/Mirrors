using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSubState_PlayNextNode : PlayerSubState_Base
{
    InputAction interactAction = PlayerInputReciever.Instance.InteractAction;
    TextController textController;

    public override void OnEnter(PlayerState_Base baseState, Player player)
    {
        interactAction.performed += OnInteracted;
    }
    public override void OnExit(PlayerState_Base baseState, Player player)
    {
        interactAction.performed -= OnInteracted;
    }

    public override void OnUpdate(PlayerState_Base baseState, Player player)
    {
        throw new System.NotImplementedException();
    }

    private void OnInteracted(InputAction.CallbackContext obj)
    {
        textController.PlayNextNode();
    }

}
