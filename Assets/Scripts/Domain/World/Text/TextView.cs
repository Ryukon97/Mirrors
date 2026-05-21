using UnityEngine;

public abstract class TextView : MonoBehaviour
{
    public abstract void InitView();
    public abstract void UpdateViewOwner(TextController ownerController);
    public abstract void UpdateView(TextController.TextModel model);
    public abstract void HideView();
}
