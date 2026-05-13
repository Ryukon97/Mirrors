using UnityEngine;

public class InteractHoverPopupView : MonoBehaviour
{
    [SerializeField] GameObject panel;
    static GameObject _panel;
    private void Awake()
    {
        _panel = panel;
        _panel.SetActive(false);
    }
    public static void Show()
    {
        if (_panel != null)
            _panel.SetActive(true);
    }
    public static void Hide()
    {
        if (_panel != null)
            _panel.SetActive(false);
    }

}
