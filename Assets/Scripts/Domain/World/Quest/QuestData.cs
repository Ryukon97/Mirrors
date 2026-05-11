using System;
using UnityEngine;

[CreateAssetMenu(fileName = "QuestData", menuName = "World/QuestData")]
public class QuestData : ScriptableObject
{
    public string qid;
    public string title;
    public string description;
    public QuestConditionData[] conditions;
    public QuestRewardData[] rewards;
}