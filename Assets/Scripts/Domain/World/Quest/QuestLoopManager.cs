using UnityEngine;

public class QuestLoopManager : MonoBehaviour, IQuestLoopManager
{
    static QuestLoopManager instance;
    public static IQuestLoopManager Instance { get => instance; }

    [SerializeField] QuestDataContainer questDataContainer;
    [SerializeField] QuestProgressor progressor;
    QuestConditionCheckService conditionCheckService;

    private void Awake()
    {
        if(instance != null)
        {
            Destroy(this.gameObject);
            return;
        }
        instance = this;
        TextControllerManager.OnTextNodeFinished += HandleTextNodeFinished;
        conditionCheckService = new QuestConditionCheckService();
    }

    // ------------------ public -----------------------

    public void AcceptQuest(string qid)
    {
        if(questDataContainer.Datas.ContainsKey(qid))
        {
            progressor.AddQuest(qid);
            // add quest on quest book
        }
        else
        {
            Debug.LogWarning($"Quest {qid} is not exist but trying to Accept Quest.");
        }
    }
    public bool CheckQuestCompletion(string qid)
    {
        if(questDataContainer.Datas.TryGetValue(qid, out QuestData quest))
        {
            return progressor.IsCompleted(qid);
        }
        Debug.LogWarning($"Quest {qid} is not exist but trying to Check Completion.");
        return false;
    }
    public void CompleteQuest(string qid)
    {
        if (questDataContainer.Datas.ContainsKey(qid))
        {
            progressor.RemoveQuest(qid);
            // rmv quest from quest book
        }
        else
        {
            Debug.LogWarning($"Quest {qid} is not exist but trying to Complete Quest.");
        }
    }

    // ------------- private ----------------- 

    void HandleTextNodeFinished(TextNodeFinishedArgs args)
    {
        if (args.type != ETextNodeFinishedEventType.StartQuest)
            return;

        string qid = args.arg1;

        AcceptQuest(qid);
    }
}
