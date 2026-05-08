
public abstract class PlayerSubState_Base
{
	public abstract void OnEnter(PlayerState_Base baseState, Player player);
	public abstract void OnExit(PlayerState_Base baseState, Player player);
	public abstract void OnUpdate(PlayerState_Base baseState, Player player);
}
