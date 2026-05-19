using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryItemMatrix
{
    private List<InventoryItemCell> _items;

    public Vector2Int Size { get; private set; }
    public IReadOnlyList<IReadOnlyItemCell> Items => _items;

    public event Action<InventoryItemCell> OnItemAdded;
    public event Action<int, InventoryItemCell> OnItemAmountChanged;
    public event Action<int, InventoryItemCell> OnItemRemoved;

    public InventoryItemMatrix(Vector2Int size)
    {
        this.Size = size;
        _items = new List<InventoryItemCell>();
    }
    public void AddItem(InventoryItemCell elem)
    {
        _items.Add(elem);
        OnItemAdded?.Invoke(elem);
    }
    public void ChangeItemAmount(int index, int count)
    {
        _items[index].ItemCnt = count;
        OnItemAmountChanged?.Invoke(count, _items[index]);
    }

    public void CleanZeroCntItem()
    {
        //Same As But Invoke each elem's removals: _items.RemoveAll(x => x.ItemCnt == 0);
        for (int i = _items.Count; i >= 0; i--)
        {
            if (_items[i].ItemCnt == 0)
            {
                var removed = _items[i];
                _items.RemoveAt(i);
                OnItemRemoved?.Invoke(i, removed);
            }
        }
    }
}

