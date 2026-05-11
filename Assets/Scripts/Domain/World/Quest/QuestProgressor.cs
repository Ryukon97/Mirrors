using System.Collections.Generic;
using UnityEngine;

public class QuestProgressor : MonoBehaviour
{
    private struct Progress
    {
        public QuestProgressData data;
        public bool isCompleted;

        public Progress(QuestProgressData data, bool isCompleted)
        {
            this.data = data;
            this.isCompleted = isCompleted;
        }
    }
    private QuestData pinnedQuestData;
    private List<QuestData> questDatas;
    private Dictionary<string, List<Progress>> progressDict;
    QuestConditionCheckService conditionCheckService;

    // ------------------- public ----------------

    public void AddQuest(string qid)
    {
        if (TryGetData(qid, out QuestData quest)) return;

        questDatas.Add(quest);
        pinnedQuestData = quest;

        // Todo Here: Set Viewer: the new pinned quest

        for(int i = 0; i < quest.conditions.Length; i++)
        {
            var origin = quest.conditions[i];
            var current = GetNewProgress(origin);
            QuestProgressData progress = new QuestProgressData(qid, origin, current);
            BindQuestTracker(qid, progress);
        }
    }
    public void RemoveQuest(string qid)
    {
        if (TryGetData(qid, out QuestData quest) == false) return;
        
        questDatas.Remove(quest);
        if(pinnedQuestData == null)
        {
            // Todo Here: Set Viewer: hide pinnedQuest
        }
        UnbindQuestTracker(qid);
    }
    public bool IsCompleted(string qid)
    {
        if (progressDict.TryGetValue(qid, out List<Progress> progresses))
        {
            for (int i = 0; i < progresses.Count; i++)
            {
                var progress = progresses[i];
                if (progress.isCompleted == false)
                    return false;
            }
            return true;
        }
        Debug.LogWarning($"Quest {qid} is not exist but trying to Check Completion.");
        return false;
    }

    // ------------------- private----------------
    void BindQuestTracker(string qid, QuestProgressData progress)
    {
        if(progressDict.ContainsKey(qid) == false)
        {
            progressDict.Add(qid, new List<Progress>());
        }
        // Todo Here: subscribe what progress need to listen.
        // implement in another class since it'll become too huge codes

        progressDict[qid].Add(new Progress(progress, false));
    }
    void UnbindQuestTracker(string qid)
    {
        if (progressDict.ContainsKey(qid) == false) return;

        // Todo Here: unsub qid's progress subscriptions. 
        // implement in another class since it'll become too huge codes(same as binding)
    }

    bool TryGetData(string qid, out QuestData data)
    {
        foreach(var quest in questDatas)
        {
            if (quest.qid == qid)
            {
                data = quest;
                return true;
            }
        }
        data = null;
        return false;
    }
    // Only use to start condition tracking progress
    // Get new conditionData that all field zero-init. 
    // To complete quest: this progress values > origin values
    QuestConditionData GetNewProgress(QuestConditionData origin)
    {
        QuestConditionData progress = origin;
        progress.valueInt = 0;

        return progress;
    }
}
