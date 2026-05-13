using System.Collections;
using UnityEngine;

public class PlayerState_Attack : PlayerState_Base
{
    float enteredT;
    const float attackDuration = .4f;    // 
    public PlayerState_Attack(PlayerStateMachine machine, Player player
        , PlayerSubState_Base[] subStates) : base(machine, player, subStates)
    {
        
    }
    public override void OnEnter()
    {
        foreach (var state in subStates)
            state.OnEnter(this, player);
        enteredT = Time.time;
    }
    public override void OnUpdate()
    {
        foreach (var state in subStates)
            state.OnUpdate(this, player);
        if(Time.time - enteredT > attackDuration)
        {
            machine.ChangeState(machine.normalState);
        }
    }
    public override void OnExit()
    {
        foreach (var state in subStates)
            state.OnExit(this, player);
    }
}
