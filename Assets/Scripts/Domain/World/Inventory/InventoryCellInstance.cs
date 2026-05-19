using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryCellInstance : MonoBehaviour
{
    public string id;
    public int count;
    public Image image;
    public TextMeshProUGUI tmpro;
    public void UpdateData(InventoryItemCell data)
    {
        id = data.ItemID;
        count = data.ItemCnt;
        tmpro.text = data.ItemCnt.ToString();
        //image.sprite = 
    }
}
