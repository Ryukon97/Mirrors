using UnityEngine.InputSystem;

public class PlayerInputReciever
{
	static PlayerInputReciever instance;
	public static PlayerInputReciever Instance
	{
		get
		{
			if (instance != null)
			{
				return instance;
			}
			instance = new PlayerInputReciever();
			return instance;
		}
	}

	// Player Actions
	public InputAction InteractAction { get; private set; }
	public InputAction LookAction { get; private set; }
	public InputAction MoveAction { get; private set; }
	public InputAction SprintAction { get; private set; }
	
	PlayerInputReciever()
	{
		InteractAction = InputSystem.actions.FindAction("Interact");
		LookAction = InputSystem.actions.FindAction("Look");
		MoveAction = InputSystem.actions.FindAction("Move");
        SprintAction = InputSystem.actions.FindAction("Sprint");
	}
}
