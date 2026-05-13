using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSubState_Interact : PlayerSubState_Base
{
	LayerMask interactableLayerMask;
	bool interactHovering;
	IInteractable target;

	InputAction interactAction = PlayerInputReciever.Instance.InteractAction;

	Collider[] overlapedColliders = new Collider[1];	// max interact target always 1.

	public PlayerSubState_Interact(LayerMask interactableLayerMask)
	{
		this.interactableLayerMask = interactableLayerMask;
	}
	public override void OnEnter(PlayerState_Base baseState, Player player)
	{
		interactHovering = false;
    }

    public override void OnExit(PlayerState_Base baseState, Player player)
    {
        // turn off interact hover ui
    }

    public override void OnUpdate(PlayerState_Base baseState, Player player)
	{
		Interact(baseState.machine, player);
	}
	private void Interact(PlayerStateMachine machine, Player player)
	{
		if (interactHovering && interactAction.WasPerformedThisFrame())
		{
			if (IsInteractReady(interactableLayerMask, player))
			{
				target.Interact();
				machine.ChangeState(machine.interactingState);
			}
		}
		else
		{
			if (IsInteractReady(interactableLayerMask, player))
			{
				if (interactHovering == false)
				{
					// turn on interact hover ui
				}
				interactHovering = true;
			}
			else
			{
				if (interactHovering == true)
                {
                    // turn off interact hover ui
                }
				interactHovering = false;
			}
		}
	}

	bool IsInteractReady(LayerMask layerMask, Player player)
	{
		int n = Physics.OverlapBoxNonAlloc(player.forwardCamTarget.position, new Vector3(2f, 2f, 2f), overlapedColliders, player.forwardCamTarget.rotation, layerMask);
		for(int i = 0; i < n; i++)
		{
			target = overlapedColliders[i].gameObject.GetComponent<IInteractable>();
		}
		if (n == 0)
			target = null;
		return target != null && target.IsReady;
	}
}
