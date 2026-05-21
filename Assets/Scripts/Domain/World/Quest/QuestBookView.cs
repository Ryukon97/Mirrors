using System.Collections.Generic;
using UnityEngine;

public class QuestBookView : MonoBehaviour
{
    [Header("Quest Book Ref")]
    [SerializeField] GameObject questBook;
    [SerializeField] GameObject questBookContent;

    [Header("Quest Book Elememts Ref")]
    [SerializeField] GameObject titleDivPrefab;
    [SerializeField] GameObject detailDivPrefab;

    List<QuestBookTitleDivInstance> titleDivs;
    List<QuestBookDetailDivInstance> detailDivs;

    private void Awake()
    {
        titleDivs = new List<QuestBookTitleDivInstance>();
        detailDivs = new List<QuestBookDetailDivInstance>();
        questBook.SetActive(false);
    }

    public void AddQuest(QuestData quest)
    {
        QuestBookTitleDivInstance titleDiv;
        QuestBookDetailDivInstance detailDiv;

        titleDiv = Instantiate(titleDivPrefab, questBookContent.transform)
            .GetComponent<QuestBookTitleDivInstance>();
        titleDivs.Add(titleDiv);


        detailDiv = Instantiate(detailDivPrefab, questBookContent.transform)
            .GetComponent<QuestBookDetailDivInstance>();
        detailDivs.Add(detailDiv);

        titleDiv.titleTmpro.text = quest.title;
        detailDiv.detailTmpro.text = quest.description;
        titleDiv.qid = quest.qid;
        detailDiv.qid = quest.qid;
        titleDiv.gameObject.SetActive(true);
        detailDiv.gameObject.SetActive(false);
        titleDiv.OnToggled = () =>
        {
            if (detailDiv.gameObject.activeSelf)
                detailDiv.gameObject.SetActive(false);
            else
                detailDiv.gameObject.SetActive(true);
        };
    }

    public void RemoveQuest(QuestData quest)
    {
        for(int i = 0; i < titleDivs.Count; i++)
        {
            if (titleDivs[i].qid == quest.qid)
            {
                Destroy(titleDivs[i].gameObject);
                Destroy(detailDivs[i].gameObject);
                titleDivs.RemoveAt(i);
                detailDivs.RemoveAt(i);
                return;
            }
        }
    }

    public void ToggleView()
    {
        if (questBook.activeSelf)
            HideView();
        else
            ShowView();
    }
    public void ShowView()
    {
        questBook.SetActive(true);
        PlayerSubState_Look.locks++;
        PlayerSubState_Move.locks++;
    }
    public void HideView()
    {
        detailDivs.ForEach(div => div.gameObject.SetActive(false));
        questBook.SetActive(false);
        PlayerSubState_Look.locks--;
        PlayerSubState_Move.locks--;
    }
}
