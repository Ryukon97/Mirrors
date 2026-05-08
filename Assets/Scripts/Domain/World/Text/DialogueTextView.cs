using UnityEngine;

public class DialogueTextView : TextView
{
    DialogueTextInstance dialogueTextInstance;
    [SerializeField] string speakerName;


    public override void InitView()
    {
        dialogueTextInstance = TextInstancePool.Instance.GetDialogueTextInstance();
        dialogueTextInstance.gameObject.SetActive(false);
        dialogueTextInstance.speakerTmpro.text = speakerName;
    }
    public override void HideView()
    {
        dialogueTextInstance.gameObject.SetActive(false);
    }

    public override void UpdateView(string[] text)
    {
        if (dialogueTextInstance.gameObject.activeSelf == false)
            dialogueTextInstance.gameObject.SetActive(true);
        dialogueTextInstance.contentTmpro.text = text[0];
    }
}
