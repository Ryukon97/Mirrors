using UnityEngine;

public class TextController : MonoBehaviour
{

    [SerializeField] private TextView textView;

    private TextModel textModel;

    private void Awake()
    {
        textModel = new TextModel(); // 
        textModel.OnViewRequested += HandleViewRequested;

        textView.InitView();
    }

    // ---------------- public ---------------- 
    public void ChangeText(string newText) => textModel.ChangeText(newText);

    // ----------------  private ---------------- 
    private void HandleViewRequested(string text) => textView.UpdateView(text);
    private void OnDestroy()
    {
        textModel.OnViewRequested -= HandleViewRequested;
    }

}
