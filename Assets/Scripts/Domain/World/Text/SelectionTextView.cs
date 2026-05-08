using System.Collections.Generic;
using UnityEngine;

public class SelectionTextView : TextView
{
    SelectionTextInstance selectionTextInstance;
    public List<string> texts;
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

        // set text[0..n] to selectionTextInstance.tmpros[0..n]
    }
}
