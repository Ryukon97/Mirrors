using UnityEngine;

public struct QuestProgressData
{
    public string qid;
    public QuestConditionData origin;
    public QuestConditionData current;
    public bool completion;
    public QuestProgressData(string qid, QuestConditionData origin, QuestConditionData current)
    {
        this.qid = qid;
        this.origin = origin;
        this.current = current;
        this.completion = false;
    }
    public void Complete() => completion = true;
}
