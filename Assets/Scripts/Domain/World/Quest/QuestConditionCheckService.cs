
using System.Collections.Generic;

public class QuestConditionCheckService
{
    Dictionary<EQuestConditionType, QuestConditionChecker> checkers = new Dictionary<EQuestConditionType, QuestConditionChecker>
    {
        {EQuestConditionType.AutoComplete, new QuestCondChecker_AutoComplete()}
    };

    public bool IsMet(QuestProgressData progress) => checkers[progress.origin.type].IsMet();
}
