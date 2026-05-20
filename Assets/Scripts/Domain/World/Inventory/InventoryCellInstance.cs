using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryCellInstance : MonoBehaviour
{
    [NonSerialized] public string id;
    [NonSerialized] public int count;
    public Image image;
    public TextMeshProUGUI tmpro;
    public void UpdateData(InventoryItemCell data, ItemDB itemDB)
    {
        id = data.ItemID;
        count = data.ItemCnt;
        tmpro.text = data.ItemCnt.ToString();
        image.sprite = itemDB.items.Find(x => x.id == id).sprite;
    }
}
