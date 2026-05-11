using UnityEngine;

public struct QuestProgressedArgs
{
    public EQuestConditionType type;
    public int valueInt;

    public QuestProgressedArgs(EQuestConditionType type, int valueInt)
    {
        this.type = type;
        this.valueInt = valueInt;
    }
}
