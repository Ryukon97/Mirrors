using System;
using System.Collections.Generic;
using UnityEngine;

public class TextController : MonoBehaviour
{
    public string ownerName;
    private TextView curTextView;
    private TextNode textNode;
    public ETextControllerType controllerType;
    public string[] Texts { get; private set; }
    public bool IsSelection { get => textNode != null && textNode.viewType == ETextViewType.Selection; }
    public event Action<TextNodeFinishedArgs> OnTextNodeFinished;
    public event Action OnNodeChainFinished;
    private void Awake()
    {
        Texts = new string[] {""};
    }

    // ---------------- public ---------------- 
    public void ChangeText(string[] texts) => Texts = texts;
    public void RequestView()
    {
        if (curTextView != null)
            curTextView.UpdateView(Texts);
    }
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

        RequestView();
        return true;
    }
    public void SetAndPlayNode(TextNode textNode)
    {
        TextControllerManager.Instance.Controller = this;
        this.textNode = textNode;
        PlayCurrentNode();
    }
    public bool PlayNextNode(int selection = 0)
    {
        if (textNode == null)
        {
            return false;
        }
        if(textNode.finishedEvent.type != ETextNodeFinishedEventType.None)
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
        OnNodeChainFinished?.Invoke();
    }
    private void SetModelByViewType(ETextViewType type)
    {
        if (textNode == null) return;
        switch (type)
        {
            case ETextViewType.Balloon:
            case ETextViewType.Dialogue:
                ChangeText(new string[] { textNode.node[0].text });
                break;
            case ETextViewType.Selection:
                string[] texts = new string[textNode.node.Count];
                for (int i = 0; i < texts.Length; i++)
                    texts[i] = textNode.node[i].text;
                ChangeText(texts);
                break;
        }
    }
    private void SetViewByViewType(ETextViewType type)
    {
        curTextView = TextControllerManager.Instance.GetView(type);
        curTextView.UpdateViewOwner(this);
    }

}
