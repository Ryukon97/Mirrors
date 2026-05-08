using UnityEngine;

public class TextControllerManager : MonoBehaviour
{
    public static TextController controller { get; set; }
    public void OnSelectionSelected(int idx)
    {
        controller.PlayNextNode(idx);
    }
}
