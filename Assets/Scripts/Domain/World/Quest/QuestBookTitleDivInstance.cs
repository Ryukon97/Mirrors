using System;
using TMPro;
using UnityEngine;

public class QuestBookTitleDivInstance : MonoBehaviour
{
    [NonSerialized] public string qid;
    public TextMeshProUGUI titleTmpro;
    public Action OnToggled;

    public void OnDetailRequested()
    {
        if(OnToggled != null)
            OnToggled();
    }
}
