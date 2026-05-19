using UnityEngine;

public class InventoryItemCell
{
    public string itemID;
    public int itemCnt;

    public InventoryItemCell(string itemID, int itemCnt)
    {
        this.itemID = itemID;
        this.itemCnt = itemCnt;
    }
    //public float expireTime;
}
