using UnityEngine;

public class Inventory : MonoBehaviour
{
    InventoryItemMatrix matrix;
    InventoryItemPoper poper;
    InventoryItemStacker stacker;

    private void Awake()
    {
        matrix = new InventoryItemMatrix(new Vector2Int(6, 6));
        poper = new InventoryItemPoper();
        stacker = new InventoryItemStacker();
    }

    public bool TryAddItem(InventoryItemCell cell) => stacker.AddItem(matrix, cell);
    public bool TryRmvItem(string id, int count) => poper.RmvItem(matrix, id, count);
}
