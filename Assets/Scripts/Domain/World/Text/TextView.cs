using UnityEngine;

public abstract class TextView : MonoBehaviour
{
    public abstract void InitView();
    public abstract void UpdateView(string[] text);
    public abstract void HideView();
}
