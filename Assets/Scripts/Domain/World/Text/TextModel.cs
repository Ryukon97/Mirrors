using System;

public class TextModel
{
    public event Action<string[]> OnViewRequested;
    public string[] Texts { get; private set; }
    public void RequestView()
    {
        OnViewRequested?.Invoke(Texts);
    }
    public void ChangeText(string[] texts) { Texts = texts; }
}
