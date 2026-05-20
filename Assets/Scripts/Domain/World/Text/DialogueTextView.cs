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

    public override void UpdateView(TextController.TextModel model)
    {
        if (dialogueTextInstance.gameObject.activeSelf == false)
            dialogueTextInstance.gameObject.SetActive(true);
        dialogueTextInstance.illust.sprite = model.illust;
        dialogueTextInstance.contentTmpro.text = model.texts[0];
    }

    public override void UpdateViewOwner(TextController ownerController)
    {
        dialogueTextInstance.speakerTmpro.text = ownerController.ownerName;
    }
}
