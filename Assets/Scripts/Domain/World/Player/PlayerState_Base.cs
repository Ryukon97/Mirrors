
public abstract class PlayerState_Base
{
	public PlayerStateMachine machine;
    protected PlayerSubState_Base[] subStates;
    protected Player player;
	public PlayerState_Base(PlayerStateMachine machine, Player player, PlayerSubState_Base[] subStates)
	{
		this.subStates = subStates;
        this.machine = machine;
		this.player = player;
	}
	public abstract void OnEnter();
	public abstract void OnUpdate();
	public abstract void OnExit();
}
