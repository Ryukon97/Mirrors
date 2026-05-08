using UnityEngine;

public class TextController : MonoBehaviour
{

    [SerializeField] private TextView textView;
    private TextNode textNode;
    private TextModel textModel;

    private void Awake()
    {
        textModel = new TextModel(); // 
        textModel.OnViewRequested += HandleViewRequested;
        textModel.ChangeText("");
        textView.InitView();
    }

    // ---------------- public ---------------- 
    public void ChangeText(string newText) => textModel.ChangeText(newText);
    public void RequestView() => textModel.RequestView();
    public void SetAndPlayNode(TextNode textNode)
    {
        this.textNode = textNode;
        PlayCurrentNode();
    }
    public void PlayCurrentNode()
    {
        // Todo: When Node Ends?
        if (textNode == null) return;
        textModel.ChangeText(textNode.text);
        textModel.RequestView();
    }
    public void PlayNextNode()
    {
        if (textNode == null) return;
        textNode = textNode.nextNode;
        PlayCurrentNode();
    }
    public void PlayPrevNode()
    {
        if (textNode == null) return;
        textNode = textNode.prevNode;
        PlayCurrentNode();
    }

    // ----------------  private ---------------- 
    private void HandleViewRequested(string text) => textView.UpdateView(text);
    private void OnDestroy()
    {
        textModel.OnViewRequested -= HandleViewRequested;
    }

}
