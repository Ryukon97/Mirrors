using System;
using System.Collections.Generic;
using UnityEngine;

public class TextController : MonoBehaviour
{
    public class TextModel
    {
        public string[] texts;
        public Sprite illust;
        public TextModel()
        {
            texts = new string[] { "" };
            illust = null;
        }
    }
    public string ownerName;
    private TextView curTextView;
    private TextNode textNode;
    public TextModel model { get; private set; }
    public ETextControllerType controllerType;
    public bool IsSelection { get => textNode != null && textNode.viewType == ETextViewType.Selection; }
    public event Action<TextNodeFinishedArgs> OnTextNodeFinished;
    public event Action<TextNodeStartedArgs> OnTextNodeStarted;
    public event Action OnNodeChainFinished;
    private void Awake()
    {
        model = new TextModel();
    }

    // ---------------- public ---------------- 
    public void ChangeText(string[] texts) => model.texts = texts;
    public void RequestView()
    {
        if (curTextView != null)
            curTextView.UpdateView(model);
    }
    public void HideView()
    {
        if(curTextView != null)
            curTextView.HideView();
    }
    public bool PlayCurrentNode()
    {
        Debug.Log("PlayerCurrentNode_1");
        HideView();
        Debug.Log("PlayerCurrentNode_2");
        if (textNode == null)
        {
            EndNodeChain();
            return false;
        }
        Debug.Log("PlayerCurrentNode_3");
        OnTextNodeStarted?.Invoke(new TextNodeStartedArgs(textNode.id));
        Debug.Log("PlayerCurrentNode_4");
        SetModelByViewType(textNode.viewType);
        Debug.Log("PlayerCurrentNode_5");
        SetViewByViewType(textNode.viewType);
        Debug.Log("PlayerCurrentNode_6");
        model.illust = textNode.illust;
        Debug.Log("PlayerCurrentNode_7");

        RequestView();
        Debug.Log("PlayerCurrentNode_8");
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
