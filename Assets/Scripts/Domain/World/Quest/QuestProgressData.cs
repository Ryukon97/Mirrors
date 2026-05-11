using UnityEngine;

public struct QuestProgressData
{
    public string qid;
    public QuestConditionData origin;
    public QuestConditionData current;

    public QuestProgressData(string qid, QuestConditionData origin, QuestConditionData current)
    {
        this.qid = qid;
        this.origin = origin;
        this.current = current;
    }
}
