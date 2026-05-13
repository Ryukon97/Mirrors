using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSubState_Look : PlayerSubState_Base
{
	float xRotation = 0f;
	float yRotation = 0f;
	InputAction lookAction = PlayerInputReciever.Instance.LookAction;
	public PlayerSubState_Look() : base()
	{

	}
	public override void OnEnter(PlayerState_Base baseState, Player player)
	{

	}

	public override void OnUpdate(PlayerState_Base baseState, Player player)
	{
		Vector2 v = lookAction.ReadValue<Vector2>();
		if (v.magnitude < 0.1f) return;

		// 1. Calculate values
		xRotation -= v.y * player.mouseSenceY * Time.deltaTime;
		xRotation = Mathf.Clamp(xRotation, -60f, 60f);

		// Instead of using .Rotate(), track Y rotation manually
		yRotation += v.x * player.mouseSenceX * Time.deltaTime;

		// 2. Apply once using a single Quaternion
		player.camTarget.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);
	}

	public override void OnExit(PlayerState_Base baseState, Player player)
	{

	}
}
