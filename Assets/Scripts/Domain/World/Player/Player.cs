using UnityEngine;

public class Player : MonoBehaviour
{
    public Transform camTarget;
    public Transform forwardCamTarget;
    public Transform playerBody;
    public CharacterController controller;

    public float mouseSenceX;
    public float mouseSenceY;

    public PlayerStateMachine FSM { get; private set; }

    private void Awake()
    {

        FSM = new PlayerStateMachine(this);
    }
    private void Update()
    {
        FSM.UpdateState();
    }
}
