using System.Collections.Generic;
using UnityEngine;

public class QuestDataContainer: MonoBehaviour
{
    [SerializeField] List<QuestData> datas;

    public Dictionary<string, QuestData> Datas { get; private set; }  
    private void Awake()
    {
        Datas = new Dictionary<string, QuestData>();
        foreach(var data in datas)
        {
            Datas.Add(data.qid, data);
        }
    }
}
