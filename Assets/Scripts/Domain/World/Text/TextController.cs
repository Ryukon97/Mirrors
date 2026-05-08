using System;
using System.Collections.Generic;
using UnityEngine;

public class TextController : MonoBehaviour
{
    [SerializeField] private TextView textView;
    private TextNode textNode;
    private TextModel textModel;
    public bool IsSelection { get => textNode != null && textNode.node.Count > 1; }

    private void Awake()
    {
        textModel = new TextModel(); // 
        textModel.OnViewRequested += HandleViewRequested;
        textModel.ChangeText(new string[] {""});
        textView.InitView();
    }

    // ---------------- public ---------------- 
    public void ChangeText(string[] texts) => textModel.ChangeText(texts);
    public void RequestView() => textModel.RequestView();
    public void HideView() => textView.HideView();
    public void SetAndPlayNode(TextNode textNode)
    {
        PlayerSubState_PlayNextNode.TextController = this;
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
        if(textNode.isSelection)
        {
            string[] texts = new string[textNode.node.Count];
            for(int i = 0; i < texts.Length; i++) 
                texts[i] = textNode.node[i].text;
            textModel.ChangeText(texts);
        }
        else
        {
            textModel.ChangeText(new string[] { textNode.node[0].text });
        }
        textModel.RequestView();
        return true;
    }
    public bool PlayNextNode(int selection = 0)
    {
        if (textNode == null)
        {
            return false;
        }
        textNode = textNode.node[selection].nextNode;
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
    private void HandleViewRequested(string[] texts) => textView.UpdateView(texts);
    private void OnDestroy()
    {
        textModel.OnViewRequested -= HandleViewRequested;
    }

}
