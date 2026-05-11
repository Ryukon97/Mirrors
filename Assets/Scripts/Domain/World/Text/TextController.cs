using System;
using System.Collections.Generic;
using UnityEngine;

public class TextController : MonoBehaviour
{
    [SerializeField] private DialogueTextView dialoguetextView;
    [SerializeField] private BalloonTextView balloonTextView;
    [SerializeField] private SelectionTextView selectionTextView;
    private TextView curTextView;
    private TextNode textNode;
    private TextModel textModel;
    public bool IsSelection { get => textNode != null && textNode.node.Count > 1; }
    public event Action<TextNodeFinishedArgs> OnTextNodeFinished;
    private void Awake()
    {
        textModel = new TextModel(); // 
        textModel.OnViewRequested += HandleViewRequested;
        textModel.ChangeText(new string[] {""});

        dialoguetextView.InitView();
        balloonTextView.InitView();
        selectionTextView.InitView();
    }

    // ---------------- public ---------------- 
    public void ChangeText(string[] texts) => textModel.ChangeText(texts);
    public void RequestView() => textModel.RequestView();
    public void HideView()
    {
        if(curTextView != null)
            curTextView.HideView();
    }
    public bool PlayCurrentNode()
    {
        HideView();
        if (textNode == null)
        {
            EndNodeChain();
            return false;
        }
        SetModelByViewType(textNode.viewType);
        SetViewByViewType(textNode.viewType);

        textModel.RequestView();
        return true;
    }
    public void SetAndPlayNode(TextNode textNode)
    {
        TextControllerManager.controller = this;
        this.textNode = textNode;
        PlayCurrentNode();
    }
    public bool PlayNextNode(int selection = 0)
    {
        if (textNode == null)
        {
            return false;
        }
        OnTextNodeFinished?.Invoke(textNode.finishedEvent);
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
    private void EndNodeChain()
    {
        HideView();
    }
    private void SetModelByViewType(ETextViewType type)
    {
        if (textNode == null) return;
        switch (type)
        {
            case ETextViewType.Balloon:
            case ETextViewType.Dialogue:
                textModel.ChangeText(new string[] { textNode.node[0].text });
                break;
            case ETextViewType.Selection:
                string[] texts = new string[textNode.node.Count];
                for (int i = 0; i < texts.Length; i++)
                    texts[i] = textNode.node[i].text;
                textModel.ChangeText(texts);
                break;
        }
    }
    private void SetViewByViewType(ETextViewType type)
    {
        switch(type)
        {
            case ETextViewType.Balloon:
                curTextView = balloonTextView;
                break;
            case ETextViewType.Dialogue:
                curTextView = dialoguetextView;
                break;
            case ETextViewType.Selection:
                curTextView = selectionTextView;
                break;
        }
    }
    private void HandleViewRequested(string[] texts)
    {
        if(curTextView != null)
            curTextView.UpdateView(texts);
    }
    private void OnDestroy()
    {
        textModel.OnViewRequested -= HandleViewRequested;
    }

}
