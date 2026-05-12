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

    int curPoolCnt;

    private void Awake()
    {
        titleDivs = new List<QuestBookTitleDivInstance>();
        detailDivs = new List<QuestBookDetailDivInstance>();
        questBook.SetActive(false);
        curPoolCnt = 0;
    }

    public void AddQuest(QuestData quest)
    {
        QuestBookTitleDivInstance titleDiv;
        QuestBookDetailDivInstance detailDiv;

        if(curPoolCnt == titleDivs.Count)
        {

            titleDiv = Instantiate(titleDivPrefab, questBookContent.transform)
                .GetComponent<QuestBookTitleDivInstance>();
            titleDivs.Add(titleDiv);

            detailDiv = Instantiate(detailDivPrefab, questBookContent.transform)
                .GetComponent<QuestBookDetailDivInstance>();
            detailDivs.Add(detailDiv);

        }
        else
        {
            titleDiv = titleDivs[curPoolCnt];
            detailDiv = detailDivs[curPoolCnt];
        }


        titleDiv.titleTmpro.text = quest.title;
        detailDiv.detailTmpro.text = quest.description;
        titleDiv.gameObject.SetActive(true);
        detailDiv.gameObject.SetActive(false);

        curPoolCnt++;
    }

    public void RemoveQuest(QuestData quest)
    {
        titleDivs.Find(div => quest.qid == div.qid).gameObject.SetActive(false);
        detailDivs.Find(div => quest.qid == div.qid).gameObject.SetActive(false);

        curPoolCnt--;
    }

    public void ShowView()
    {
        questBook.SetActive(true);
    }
    public void HideView()
    {
        detailDivs.ForEach(div => div.gameObject.SetActive(false));
        questBook.SetActive(false);
    }
}
