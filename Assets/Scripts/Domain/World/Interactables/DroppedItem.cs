using UnityEngine;

public class DroppedItem : MonoBehaviour
{
    static Inventory inventory;
    public string id;
    public int cnt;
    public void GiveItem()
    {
        FindInventory();

        if (inventory != null)
        {
            inventory.TryAddItem(new InventoryItemCell(id, cnt));
            Destroy(this.gameObject);
        }
    }

    private void FindInventory()
    {
        if (inventory == null)
        {
            var inventorys = FindObjectsByType<Inventory>();
            if (inventorys.Length > 0)
                inventory = inventorys[0];
        }
    }
}
