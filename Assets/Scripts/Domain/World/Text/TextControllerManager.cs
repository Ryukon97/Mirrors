using System;
using UnityEngine;

[RequireComponent(typeof(DialogueTextView), typeof(BalloonTextView), typeof(SelectionTextView))]
public class TextControllerManager : MonoBehaviour
{
    private static TextControllerManager instance;
    public static TextControllerManager Instance { get => instance; }
    public DialogueTextView DialogueView { get; private set; }
    public BalloonTextView BalloonView { get; private set; }
    public SelectionTextView SelectionView { get; private set; }
    public event Action<TextNodeFinishedArgs> OnTextNodeFinished;
    public event Action<TextNodeStartedArgs> OnTextNodeStarted;

    TextController controller;
    public TextController Controller
    { 
        get
        {
            return controller;
        }
        
        set
        {
            if (controller != null)
            {
                controller.OnTextNodeFinished -= HandleTextNodeFinished;
                controller.OnTextNodeStarted -= HandleTextNodeStarted;
            }
            controller = value;
            controller.OnTextNodeFinished += HandleTextNodeFinished;
            controller.OnTextNodeStarted += HandleTextNodeStarted;
        }
    }
    private void Awake()
    {
        if(instance != null)
        {
            Destroy(this.gameObject);
            return;
        }
        instance = this;
        DialogueView = GetComponent<DialogueTextView>();
        BalloonView = GetComponent<BalloonTextView>();
        SelectionView = GetComponent<SelectionTextView>();
    }
    private void Start()
    {
        DialogueView.InitView();
        BalloonView.InitView();
        SelectionView.InitView();
    }
    public TextView GetView(ETextViewType type)
    {
        switch (type)
        {
            case ETextViewType.Balloon:
                return BalloonView;
            case ETextViewType.Dialogue:
                return DialogueView;
            case ETextViewType.Selection:
                return SelectionView;
        }
        return null;
    }
    void HandleTextNodeFinished(TextNodeFinishedArgs args)
    {
        OnTextNodeFinished?.Invoke(args);
    }
    void HandleTextNodeStarted(TextNodeStartedArgs args)
    {
        OnTextNodeStarted?.Invoke(args);
    }
    public bool TryPlayNextNode(ETextControllerType type, out bool wasLast)
    {
        wasLast = false;
        if (controller == null || controller.controllerType != type) return false;
        if (controller.IsSelection) return false;
        wasLast = !controller.PlayNextNode();
        return true;
    }
    // On Select Button UGUI Selected
    public void OnSelectionSelected(int idx)
    {
        controller.PlayNextNode(idx);
    }
}

public enum ETextControllerType
{
    PlayerInput,
    Auto,
}