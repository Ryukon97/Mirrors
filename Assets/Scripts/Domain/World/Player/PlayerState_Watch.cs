using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerState_Watch : PlayerState_Base
{
	public PlayerState_Watch(PlayerStateMachine machine, Player player
        , PlayerSubState_Base[] subStates) : base(machine, player, subStates)
    {

    }
    public override void OnEnter()
    {
        player.anim.SetFloat("Speed", 0);
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
        foreach (var state in subStates)
            state.OnExit(this, player);
    }
}
