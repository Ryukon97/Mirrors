using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSubState_PlayNextNode : PlayerSubState_Base
{
    InputAction interactAction = PlayerInputReciever.Instance.InteractAction;
    TextController textController;
    PlayerStateMachine fsm;
    public override void OnEnter(PlayerState_Base baseState, Player player)
    {
        if (fsm == null) fsm = player.FSM;
        interactAction.performed += OnInteracted;
    }
    public override void OnExit(PlayerState_Base baseState, Player player)
    {
        interactAction.performed -= OnInteracted;
    }

    public override void OnUpdate(PlayerState_Base baseState, Player player)
    {

    }

    private void OnInteracted(InputAction.CallbackContext obj)
    {
        bool played = textController.PlayNextNode();
        if(played == false)
        {
            fsm.ChangeState(fsm.normalState);
        }
    }
    public void SetTextController(TextController textController)
    {
        this.textController = textController;
    }

}
