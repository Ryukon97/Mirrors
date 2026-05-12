
using System;

[Serializable]
public struct QuestConditionData
{
    public EQuestConditionType type;
    public int valueInt;

    public QuestConditionData GetNewProgress()
    {
        QuestConditionData progress = this;
        progress.valueInt = 0;

        return progress;
    }
}
