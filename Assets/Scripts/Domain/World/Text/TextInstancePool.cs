using UnityEngine;

public class TextInstancePool : MonoBehaviour
{
    public static TextInstancePool Instance { get; private set; }

    [Header("Canvas Ref")]
    [SerializeField] Transform worldCanvasTransform;
    [SerializeField] Transform screenCanvasTransform;

    [Header("Dialogue Text")]
    [SerializeField] DialogueTextInstance dialogueTextInstance;

    [Header("Balloon Text")]
    [SerializeField] GameObject balloonTextPrefab;
    BalloonTextInstance[] balloonTextInstances = new BalloonTextInstance[MAX_BALLOON_CNT];
    const int MAX_BALLOON_CNT = 20;

    private void Awake()
    {
        if(Instance != null)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;

        for (int i = 0; i < MAX_BALLOON_CNT; i++)
        {
            balloonTextInstances[i] = Instantiate(balloonTextPrefab, worldCanvasTransform).GetComponent<BalloonTextInstance>();
            balloonTextInstances[i].gameObject.SetActive(false);
        }
    }
    public DialogueTextInstance GetDialogueTextInstance() => dialogueTextInstance;
    public BalloonTextInstance GetBalloonTextInstance()
    {
        for(int i = 0; i < MAX_BALLOON_CNT; i++)
        {
            if (balloonTextInstances[i].gameObject.activeSelf == false)
                return balloonTextInstances[i];
        }
        Debug.LogWarning("[TextInstancePool] MAX_BALLOON_CNT Reached. idx==0 returned. ");
        return balloonTextInstances[0];
    }
}
