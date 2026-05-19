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
        int len = matrix.size.x * matrix.size.y;
        if (matrix.items.Count < len)
        {
            matrix.items.Add(cell);
            return true;
        }
        return false;
    }
}
