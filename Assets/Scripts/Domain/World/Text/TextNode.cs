using UnityEngine;

[CreateAssetMenu(fileName = "TextNode", menuName = "World/TextNode")]
public class TextNode : ScriptableObject
{
    public TextNode prevNode;
    public string text;
    public TextNode nextNode;
}
