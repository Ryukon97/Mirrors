public interface IQuestLoopManager
{
    void AcceptQuest(string qid);
    bool CheckQuestCompletion(string qid);
    void CompleteQuest(string qid);
}
