using System;
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
    public void HideView() => textView.HideView();
    public void SetAndPlayNode(TextNode textNode)
    {
        this.textNode = textNode;
        PlayCurrentNode();
    }
    public bool PlayCurrentNode()
    {
        if (textNode == null)
        {
            EndNode();
            return false;
        }
        textModel.ChangeText(textNode.text);
        textModel.RequestView();
        return true;
    }
    public bool PlayNextNode()
    {
        if (textNode == null)
        {
            return false;
        }
        textNode = textNode.nextNode;
        return PlayCurrentNode();
    }
    public void PlayPrevNode()
    {
        if (textNode == null) return;
        textNode = textNode.prevNode;
        PlayCurrentNode();
    }
    // ----------------  private ---------------- 
    void EndNode()
    {
        HideView();
    }
    private void HandleViewRequested(string text) => textView.UpdateView(text);
    private void OnDestroy()
    {
        textModel.OnViewRequested -= HandleViewRequested;
    }

}
