using System.Collections.Generic;
using UnityEngine;

public class Inventory: MonoBehaviour
{
    InventoryItemMatrix matrix;
    InventoryItemPoper poper;
    InventoryItemStacker stacker;
    [SerializeField] Vector2Int InventorySize;
    [SerializeField] InventoryView view;

    private void Awake()
    {
        matrix = new InventoryItemMatrix(InventorySize);
        poper = new InventoryItemPoper();
        stacker = new InventoryItemStacker();
    }
    private void OnEnable()
    {
        view.BindInventoryChangedEvents(matrix);
    }
    private void OnDisable()
    {
        view.UnBindInventoryChangedEvents();
    }

    public bool TryAddItem(InventoryItemCell cell) => stacker.AddItem(matrix, cell);
    public bool TryRmvItem(string id, int count) => poper.RmvItem(matrix, id, count);
}
