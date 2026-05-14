using UnityEngine;

public class PlayerControllerGravityApplier : MonoBehaviour
{
	[SerializeField]
	CharacterController controller;

    Vector3 velocity = Vector3.zero;
	float gravity = Physics.gravity.y;

	void Update()
    {
		if(controller.isGrounded && velocity.y < 0)
		{
			velocity.y = -1f;
		}

		// Apply gravity
		velocity.y += gravity * Time.deltaTime;

		// Move
		controller.Move(velocity * Time.deltaTime);
	}
}
