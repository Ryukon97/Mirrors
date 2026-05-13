
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerState_Normal : PlayerState_Base
{
	const float ATTACK_RANGE = 3f;
	
	public PlayerState_Normal(PlayerStateMachine machine, Player player
		, PlayerSubState_Base[] subStates) : base(machine, player, subStates)
	{

    }
	public override void OnEnter()
    {
        PlayerInputReciever.Instance.AttackAction.performed += DoAttack;
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
        PlayerInputReciever.Instance.AttackAction.performed -= DoAttack;
        foreach (var state in subStates)
			state.OnExit(this, player);
	}
	void DoAttack(InputAction.CallbackContext ctx)
	{
		var vfxTransform = player.AttackEffect.gameObject.transform;
        vfxTransform.position = player.playerBody.transform.position + player.playerBody.transform.forward* ATTACK_RANGE;
		vfxTransform.rotation = Quaternion.Euler(0, 0, Random.Range(-50f, 50f));
		player.AttackEffect.Play();
		machine.ChangeState(machine.attackState);
    }
}
