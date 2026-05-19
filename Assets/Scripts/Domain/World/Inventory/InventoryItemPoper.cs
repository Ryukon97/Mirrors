using System.Collections.Generic;
using UnityEngine;

public class InventoryItemPoper
{
    /// <summary>
    /// Simple Remover. 
    /// Removes Item from first to last, until enough item remove.
    /// It Doesn't remove items if matrix has not enough items. 
    /// </summary>
    /// <param name="matrix">item inventory matrix. modify item list</param>
    /// <param name="itemId">item id to remove</param>
    /// <param name="cnt">item count to remove</param>
    /// <returns>false if not enough items(amount < cnt). true if removed</returns>
    public bool RmvItem(InventoryItemMatrix matrix, string itemId, int cnt)
    {
        int sumAmount = 0;
        List<int> indexs = new List<int>();
        for(int i = 0; i< matrix.items.Count; i++)
        {
            if (matrix.items[i].itemID != itemId) continue;
            indexs.Add(i);
            sumAmount += matrix.items[i].itemCnt;
            if(sumAmount >= cnt)
            {
                for(int j = 0; j < indexs.Count - 1; j++)
                {
                    matrix.items[indexs[j]].itemCnt = 0;
                }
                matrix.items[indexs.Count - 1].itemCnt = sumAmount - cnt;
                matrix.CleanZeroCntItem();
                return true;
            }
        }
        return false;
    }
}
