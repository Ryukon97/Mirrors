using System;
using System.Collections.Generic;
using UnityEngine;

public class QuestProgressService
{

    private Dictionary<EQuestConditionType, Action<QuestProgressedArgs, QuestProgressData>> progressExecuters
        = new Dictionary<EQuestConditionType, Action<QuestProgressedArgs, QuestProgressData>>
    {
            {EQuestConditionType.AutoComplete, (args, progress) => { } }
            // add handler here for each type.
    };

    public void Do(QuestProgressedArgs args, QuestProgressData progress)
    {
        progressExecuters[args.type](args, progress);
    }
}
