
using System;

[Serializable]
public class QuestConditionData
{
    public EQuestConditionType type;
    public int valueInt;
    public string valueString;

    public QuestConditionData GetNewProgress()
    {
        QuestConditionData progress = new QuestConditionData();
        progress.valueInt = 0;
        progress.valueString = "";
        return progress;
    }
}
