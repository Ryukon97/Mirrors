using UnityEngine;

public class DialogueTextView : TextView
{
    DialogueTextInstance dialogueTextInstance;

    public override void InitView()
    {
        dialogueTextInstance = TextInstancePool.Instance.GetDialogueTextInstance();
        dialogueTextInstance.gameObject.SetActive(false);
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

    public override void UpdateViewOwner(TextController ownerController)
    {
        dialogueTextInstance.speakerTmpro.text = ownerController.ownerName;
    }
}
