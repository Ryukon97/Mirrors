using System.Collections.Generic;
using UnityEngine;

public class InventoryItemMatrix
{
    public Vector2Int size { get; private set; }
    public List<InventoryItemCell> items;

    public InventoryItemMatrix(Vector2Int size)
    {
        this.size = size;
        items = new List<InventoryItemCell>();
    }

    public void CleanZeroCntItem()
    {
        items.RemoveAll((x) => x.itemCnt == 0);
    }
}

