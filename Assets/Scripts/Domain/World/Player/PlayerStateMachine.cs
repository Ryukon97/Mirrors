using UnityEngine;

public class PlayerStateMachine
{
    PlayerState_Base curState;

	public PlayerState_Watch interactingState;
	public PlayerState_Normal normalState;

	public PlayerStateMachine(Player player)
	{
		var move = new PlayerSubState_Move(speed: 10f, rotSpeed: 10f);
		var look = new PlayerSubState_Look();
		var interact = new PlayerSubState_Interact(LayerMask.GetMask("Interactable"));
        var playNextNode = new PlayerSubState_PlayNextNode();

        normalState = new PlayerState_Normal(this, player, new PlayerSubState_Base[]{ move, look, interact});
        interactingState = new PlayerState_Watch(this, player, new PlayerSubState_Base[] { playNextNode });

		curState = normalState;
		curState.OnEnter();
	}
	public bool IsState<T>() where T :PlayerState_Base => curState is T;
	public void ChangeState(PlayerState_Base state)
	{
		if (curState == state) return;
		curState.OnExit();
		curState = state;
		curState.OnEnter();
	}
	public void UpdateState()
	{
		curState.OnUpdate();
	}
}
