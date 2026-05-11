public struct TextNodeFinishedEvent
{
    public ETextNodeFinishedEventType type;
    public string arg1;

    public TextNodeFinishedEvent(ETextNodeFinishedEventType type, string arg1)
    {
        this.type = type;
        this.arg1 = arg1;
    }
}
