using UnityEngine;

/// <summary>
/// We need UI Update When Inventory Elems Changed.
/// I Chose to Invoke Events On Add, Changed, Removed. 
/// Because, to de-couple logics and UI(View). 
/// So Every Changes of Inventory Elems must invoke event.
/// Therefore, Making interface for readonly fields, Ensure every changes be done from specific Method which invokes events. 
/// </summary>
public interface IReadOnlyItemCell
{
    public string ItemID { get; }
    public int ItemCnt { get; }
}
public class InventoryItemCell: IReadOnlyItemCell
{
    public string ItemID { get; set; }
    public int ItemCnt { get; set; }

    public InventoryItemCell(string itemID, int itemCnt)
    {
        this.ItemID = itemID;
        this.ItemCnt = itemCnt;
    }
    //public float expireTime;
}
