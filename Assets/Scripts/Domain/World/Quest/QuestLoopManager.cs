using UnityEngine;

public class QuestLoopManager : MonoBehaviour
{
    private void Awake()
    {
        TextControllerManager.OnTextNodeFinished += HandleTextNodeFinished;
    }

    void HandleTextNodeFinished(TextNodeFinishedArgs args)
    {
        if (args.type != ETextNodeFinishedEventType.StartQuest)
            return;

        string qid = args.arg1;
    }
}
