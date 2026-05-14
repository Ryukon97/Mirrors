using UnityEngine;
using UnityEngine.VFX;

public class Player : MonoBehaviour
{
    public Transform camTarget;
    public Transform playerBody;
    public CharacterController controller;

    public VisualEffect AttackEffect;

    public float mouseSenceX;
    public float mouseSenceY;

    public PlayerStateMachine FSM { get; private set; }

    private void Awake()
    {
        FSM = new PlayerStateMachine(this);
    }
    private void Start()
    {
        if (WorldDataMemorizer.Instance != null)
            transform.position = WorldDataMemorizer.Instance.data.lastPlayerSpawnPoint;
    }
    private void Update()
    {
        FSM.UpdateState();
    }
    private void OnDestroy()
    {
        WorldDataMemorizer.Instance.data.lastPlayerSpawnPoint = originToChunkSpawnPoint(transform.position);
        FSM.OnDestroy();
    }
    Vector3 originToChunkSpawnPoint(Vector3 origin)
    {
        origin += new Vector3(32, 0, 32);
        Vector3 newPos = new Vector3(
            Mathf.FloorToInt(origin.x / 64) * 64,
            6,
            Mathf.FloorToInt(origin.z / 64) * 64);

        return newPos;
    }
}
