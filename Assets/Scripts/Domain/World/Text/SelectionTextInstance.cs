using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SelectionTextInstance : MonoBehaviour
{
    public List<GameObject> buttons { get; private set; }
    public List<TextMeshProUGUI> tmpros;
    public Image illust;

    private void Awake()
    {
        buttons = new List<GameObject>();
        for(int i = 0; i < tmpros.Count; i++)
        {
            buttons.Add(tmpros[i].GetComponentInParent<Button>().gameObject);
        }
    }
}
