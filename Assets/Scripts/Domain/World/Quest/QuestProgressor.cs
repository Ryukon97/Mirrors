using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class QuestProgressor : MonoBehaviour
{
    private QuestData pinnedQuestData;
    private List<QuestData> questDatas;
    private Dictionary<string, List<QuestProgressData>> progressDict;
    QuestConditionCheckService conditionCheckService;
    QuestProgressService questProgressService;

    private void Awake()
    {
        conditionCheckService = new QuestConditionCheckService();
        questProgressService = new QuestProgressService();
    }

    private void OnEnable()
    {
        // OnInventoryChanged += HandleInventoryChanged;
        // OnEnemyKilled += HandleQuestProgressed;
        // ... subscribe what u need. un sub in OnDisabled
    }

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
            var current = origin.GetNewProgress();
            QuestProgressData progress = new QuestProgressData(qid, origin, current);
            if (progressDict.ContainsKey(qid) == false)
            {
                progressDict.Add(qid, new List<QuestProgressData>());
            }
            progressDict[qid].Add(progress);
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
        progressDict.Remove(qid);
    }
    public bool IsCompleted(string qid)
    {
        if (progressDict.TryGetValue(qid, out List<QuestProgressData> progresses))
        {
            for (int i = 0; i < progresses.Count; i++)
            {
                var progress = progresses[i];
                if (progress.completion == false)
                    return false;
            }
            return true;
        }
        Debug.LogWarning($"Quest {qid} is not exist but trying to Check Completion.");
        return false;
    }


    // ------------------- private----------------


    // Here's To add Progress handler:
    //
    // eg. Inventory Changed Event ?
    // void HandleInventoryChanged(InventoryChangedArgs args)
    // => args.amount > 0 ?
    //      UpdateProgresses(new QuestProgressedArgs(EQuestConditionType.Obtain, args.amount));
    //      : UpdateProgresses(new QuestProgressedArgs(EQuestConditionType.Discard, args.amount));
    // 
    // eg. Enemy Killed Event?
    // void HandleEnemyKilled(EnemyKilledArgs args)
    // => UpdateProgresses(new QuestProgressedArgs(EQuestConditionType.Killed, args.amount));
    //
    // And Add QuestProgressService a discrete Handler. 
    // And Add Subscription snippets in Awake()


    void UpdateProgresses(QuestProgressedArgs args)
    {
        foreach (var progresses in progressDict.Values)
        {
            foreach (var progress in progresses)
            {
                if (progress.origin.type != args.type) return;
                if (progress.completion) return;

                questProgressService.Do(args, progress);

                if (conditionCheckService.IsMet(progress))
                    progress.Complete();
            }
        }
    }

    // ---------------------- Utils -----------------

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
}
