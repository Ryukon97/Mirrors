using UnityEngine;

public struct QuestProgressedArgs
{
    public EQuestConditionType type;
    public int valueInt;
    public string valueString;

    public QuestProgressedArgs(EQuestConditionType type)
    {
        this.type = type;
        valueInt = 0;
        valueString = "";
    }
}
