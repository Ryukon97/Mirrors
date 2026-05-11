
using System;

[Serializable]
public struct TextNodeFinishedArgs
{
    public ETextNodeFinishedEventType type;
    public string arg1;

    public TextNodeFinishedArgs(ETextNodeFinishedEventType type, string arg1)
    {
        this.type = type;
        this.arg1 = arg1;
    }
}
