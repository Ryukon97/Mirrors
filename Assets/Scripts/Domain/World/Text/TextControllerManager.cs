using System;
using UnityEngine;

public class TextControllerManager : MonoBehaviour
{
    public static event Action<TextNodeFinishedArgs> OnTextNodeFinished;

    static TextController controller;
    public static TextController Controller
    { 
        get
        {
            return controller;
        }
        
        set
        {
            if(controller != null)
                controller.OnTextNodeFinished -= HandleTextNodeFinished;
            controller = value;
            controller.OnTextNodeFinished += HandleTextNodeFinished;
        }
    }

    static void HandleTextNodeFinished(TextNodeFinishedArgs args)
    {
        OnTextNodeFinished?.Invoke(args);
    }
    // On Select Button UGUI Selected
    public void OnSelectionSelected(int idx)
    {
        controller.PlayNextNode(idx);
    }
}
