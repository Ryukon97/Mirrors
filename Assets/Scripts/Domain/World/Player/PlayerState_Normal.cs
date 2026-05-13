
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerState_Normal : PlayerState_Base
{
    const int MAX_ATTACK_TARGET = 3;   // max target per attack
    const float ATTACK_RANGE = 3f;
    const float ATTACK_RADIUS = 2.2f;
    Collider[] overlapedTargetedColliders = new Collider[MAX_ATTACK_TARGET];    // max interact target always 1.

    LayerMask destroyableLayer;

    public PlayerState_Normal(PlayerStateMachine machine, Player player
		, PlayerSubState_Base[] subStates) : base(machine, player, subStates)
	{
        destroyableLayer = 1 << LayerMask.NameToLayer("Destroyable");
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
        TryDestroyTargets();

        machine.ChangeState(machine.attackState);
    }

    void TryDestroyTargets()
    {
        IDestroyable[] destroyables = new IDestroyable[MAX_ATTACK_TARGET];
        int n = Physics.OverlapSphereNonAlloc(player.AttackEffect.transform.position, ATTACK_RADIUS, overlapedTargetedColliders, destroyableLayer);
        for (int i = 0; i < n; i++)
        {
            destroyables[i] = overlapedTargetedColliders[i].gameObject.GetComponent<IDestroyable>();

            if (destroyables[i].IsReady)
                destroyables[i].TryDestroy();
        }
    }
}
