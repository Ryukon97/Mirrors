using System.Collections.Generic;
using UnityEngine;

public class Inventory
{
    InventoryItemMatrix matrix;
    InventoryItemPoper poper;
    InventoryItemStacker stacker;

    public InventoryItemMatrix Matrix { get => matrix; }
    public Inventory(
        InventoryItemMatrix matrix,
        InventoryItemPoper poper,
        InventoryItemStacker stacker
        )
    {
        this.matrix = matrix;
        this.poper = poper;
        this.stacker = stacker;
    }

    public bool TryAddItem(InventoryItemCell cell) => stacker.AddItem(matrix, cell);
    public bool TryRmvItem(string id, int count) => poper.RmvItem(matrix, id, count);
}
