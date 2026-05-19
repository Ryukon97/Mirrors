using UnityEngine;

public class InventoryItemStacker
{
    /// <summary>
    /// Simple Item Stacker.
    /// if it has empty cell to apply, then Add new Item cell. 
    /// </summary>
    /// <param name="matrix"></param>
    /// <param name="cell"></param>
    /// <returns></returns>
    public bool AddItem(InventoryItemMatrix matrix, InventoryItemCell cell)
    {
        if (cell.ItemCnt == 0) return false;
        int len = matrix.Size.x * matrix.Size.y;
        if (matrix.Items.Count < len)
        {
            matrix.AddItem(cell);
            return true;
        }
        return false;
    }
}
