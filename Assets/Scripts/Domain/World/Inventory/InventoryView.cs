using System.Collections.Generic;
using UnityEngine;

public class InventoryView: MonoBehaviour
{
    // Toggle this activation for toggling view.
    [SerializeField] GameObject inventoryRootUIObj;
    // Gonna enumerate cells in here. auto-aligned by layout component
    [SerializeField] GameObject inventoryMatrixLayoutObj;
    // each cells prefab
    [SerializeField] GameObject inventoryCellInstancePrefab;
    [SerializeField] ItemDB itemDB;

    private List<InventoryCellInstance> instances;
    private InventoryItemMatrix bindedMatrix;

    private void Awake()
    {
        instances = new List<InventoryCellInstance>();
    }
    private void Start()
    {
        inventoryRootUIObj.SetActive(false);
    }
    private void OnDestroy()
    {
        UnBindInventoryChangedEvents(); // for fail-safe
    }
    // ----------- publics 
    public void BindInventoryChangedEvents(InventoryItemMatrix matrix)
    {
        bindedMatrix = matrix;
        bindedMatrix.OnItemAdded += Matrix_OnItemAdded;
        bindedMatrix.OnItemAmountChanged += Matrix_OnItemAmountChanged;
        bindedMatrix.OnItemRemoved += Matrix_OnItemRemoved;
    }
    public void UnBindInventoryChangedEvents()
    {
        if (bindedMatrix == null) return;
        bindedMatrix.OnItemAdded -= Matrix_OnItemAdded;
        bindedMatrix.OnItemAmountChanged -= Matrix_OnItemAmountChanged;
        bindedMatrix.OnItemRemoved -= Matrix_OnItemRemoved;
    }
    public void ToggleView()
    {
        if (inventoryRootUIObj.activeSelf)
            CloseView();
        else
            OpenView();
    }

    // ----------- privates
    private void OpenView()
    {
        inventoryRootUIObj.SetActive(true);
    }
    private void CloseView()
    {
        inventoryRootUIObj.SetActive(false);
    }

    // ------------ Event Listeners
    /*
     * Now Instantiate and Destroy.
     * [Todo] Object Pool which max size is cell count of matrix.Size;
    */ 

    private void Matrix_OnItemAdded(InventoryItemCell data)
    {
        var obj = Instantiate(inventoryCellInstancePrefab, inventoryMatrixLayoutObj.transform).GetComponent<InventoryCellInstance>();
        obj.UpdateData(data, itemDB);
        instances.Add(obj);
    }
    private void Matrix_OnItemAmountChanged(int index, InventoryItemCell data)
    {
        instances[index].UpdateData(data, itemDB);
    }
    private void Matrix_OnItemRemoved(int index, InventoryItemCell data)
    {
        Destroy(instances[index].gameObject);
        instances.RemoveAt(index);
        //Debug.Log($"Item{data.ItemID} has removed from UI");
    }
}
