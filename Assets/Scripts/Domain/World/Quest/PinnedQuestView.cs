using System;
using System.Collections.Generic;
using UnityEngine;

public class PinnedQuestView : MonoBehaviour
{
    [SerializeField] PinnedQuestInstance[] pinnedQuests;
    const int MAX_PIN = 3;
    List<QuestData> pinnedQuestDatas;


    private void Awake()
    {
        pinnedQuestDatas = new List<QuestData>();
        for (int i = 0; i < MAX_PIN; i++)
            pinnedQuests[i].gameObject.SetActive(false);
    }
    private void OnValidate()
    {
        if (pinnedQuests.Length != MAX_PIN)
        {
            Debug.LogWarning($"[QuestView] pinnedQuests Size is fixed to {MAX_PIN}!");
            Array.Resize(ref pinnedQuests, MAX_PIN);
        }
    }

    // -------- public
    public void AddQuest(QuestData quest)
    {
        pinnedQuestDatas.Add(quest);
        if (pinnedQuestDatas.Count > MAX_PIN)
            pinnedQuestDatas.RemoveAt(MAX_PIN);

        UpdateProgressView();
    }

    public void RemoveQuest(QuestData quest)
    {
        bool removed = pinnedQuestDatas.Remove(quest);
        if (removed == false)
        {
            Debug.LogWarning($"[QuestViwe.RemoveQuest] {quest.qid} Not Exist But Trying to Remove!!");
        }
        else UpdateProgressView();
    }

    // -------- private
    void UpdateProgressView()
    {
        for (int i = 0; i < MAX_PIN; i++)
        {
            if (i < pinnedQuestDatas.Count)
            {
                pinnedQuests[i].gameObject.SetActive(true);
                pinnedQuests[i].titleTmpro.text = pinnedQuestDatas[i].title;
            }
            else
            {
                pinnedQuests[i].gameObject.SetActive(false);
            }
        }
    }
}
