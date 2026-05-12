using UnityEngine;

public class QuestView : MonoBehaviour
{
    [SerializeField] PinnedQuestView pinnedQuestView;
    [SerializeField] QuestBookView questBookView;

    // -------- public
    public void AddQuest(QuestData quest)
    {
        pinnedQuestView.AddQuest(quest);
        questBookView.AddQuest(quest);
    }

    public void RemoveQuest(QuestData quest)
    {
        pinnedQuestView.RemoveQuest(quest);
        questBookView.RemoveQuest(quest);
    }
}
