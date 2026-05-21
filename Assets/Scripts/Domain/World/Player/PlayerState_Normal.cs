
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerState_Normal : PlayerState_Base
{

    public PlayerState_Normal(PlayerStateMachine machine, Player player
		, PlayerSubState_Base[] subStates) : base(machine, player, subStates)
	{
    }
	public override void OnEnter()
    {
        PlayerInputReciever.Instance.AttackAction.performed += AttackAction_performed;
        foreach (var state in subStates)
			state.OnEnter(this, player);
	}
    public override void OnUpdate()
	{
		foreach (var state in subStates)
			state.OnUpdate(this, player);
	}
	public override void OnExit()
    {
        PlayerInputReciever.Instance.AttackAction.performed -= AttackAction_performed;
        foreach (var state in subStates)
			state.OnExit(this, player);
    }

    private void AttackAction_performed(InputAction.CallbackContext obj)
    {
        machine.ChangeState(machine.attackState);
    }

}
