using System;
using System.Collections.Generic;
using UnityEditor;
using static UnityEngine.Rendering.GPUSort;

public class QuestProgressService
{
    private Dictionary<EQuestConditionType, Action<QuestProgressedArgs, QuestProgressData>> progressExecuters;
    public QuestProgressService()
    {
        progressExecuters = new Dictionary<EQuestConditionType, Action<QuestProgressedArgs, QuestProgressData>>
        {
                {EQuestConditionType.AutoComplete, DoAutoComplete},
                {EQuestConditionType.Talk, DoTalk}

                // add handler here for each type.
        };
    }

    public void Do(QuestProgressedArgs args, QuestProgressData progress)
    {
        progressExecuters[args.type](args, progress);
    }

    private void DoAutoComplete(QuestProgressedArgs args, QuestProgressData progress)
    {
        progress.completion = true;
    }
    private void DoTalk(QuestProgressedArgs args, QuestProgressData progress)
    {
        progress.completion |= args.valueString == progress.origin.valueString;
    }
}
