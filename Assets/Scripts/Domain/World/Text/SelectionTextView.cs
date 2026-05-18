using System.Collections.Generic;
using UnityEngine;

public class SelectionTextView : TextView
{
    SelectionTextInstance selectionTextInstance;
    public override void InitView()
    {
        selectionTextInstance = TextInstancePool.Instance.GetSelectionTextInstance();
        selectionTextInstance.gameObject.SetActive(false);
    }

    public override void HideView()
    {
        selectionTextInstance.gameObject.SetActive(false);
    }

    public override void UpdateView(string[] text)
    {
        if (selectionTextInstance.gameObject.activeSelf == false)
            selectionTextInstance.gameObject.SetActive(true);

        for(int i = 0; i < selectionTextInstance.tmpros.Count; i++)
        {
            if (i < text.Length)
            {
                selectionTextInstance.tmpros[i].text = text[i];
                selectionTextInstance.buttons[i].SetActive(true);
            }
            else
                selectionTextInstance.buttons[i].SetActive(false);
        }
    }

    public override void UpdateViewOwner(TextController ownerController)
    {

    }
}
