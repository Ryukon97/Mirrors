using System;

public class TextModel
{
    public event Action<string> OnViewRequested;
    public string Text { get; private set; }
    public void RequestView()
    {
        OnViewRequested?.Invoke(Text);
    }
    public void ChangeText(string newText) { Text = newText; }
}
