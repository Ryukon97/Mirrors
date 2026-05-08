using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSubState_Move : PlayerSubState_Base
{
	float speed;
	float rotSpeed;

	InputAction moveAction = PlayerInputReciever.Instance.MoveAction;
	InputAction sprintAction = PlayerInputReciever.Instance.SprintAction;
	public PlayerSubState_Move(float speed, float rotSpeed) : base()
	{
		this.speed = speed;
		this.rotSpeed = rotSpeed;
	}
	public override void OnEnter(PlayerState_Base baseState, Player player)
	{
	}

	public override void OnExit(PlayerState_Base baseState, Player player)
	{
	}
	public override void OnUpdate(PlayerState_Base baseState, Player player)
	{
		Vector2 v = moveAction.ReadValue<Vector2>();

		Vector3 dir = player.camTarget.forward * v.y + player.camTarget.right * v.x;
		dir.y = 0;

		if (dir != Vector3.zero)
		{
			Quaternion targetRot = Quaternion.LookRotation(dir);
			player.playerBody.localRotation = Quaternion.Slerp(player.playerBody.localRotation, targetRot, rotSpeed * Time.deltaTime);
		}
		var speedMult = sprintAction.IsPressed() ? 1.5f : 1f;
		player.controller.Move(dir.normalized * speed * speedMult * Time.deltaTime);

		//var param
		//	= moveAction.IsInProgress()
		//	? sprintAction.IsInProgress()
		//	? 5f : 1f : 0f;

		//player.animator.SetFloat("MoveSpeed", param);
	}
}
