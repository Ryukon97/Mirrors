using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TextNode", menuName = "World/TextNode")]
public class TextNode : ScriptableObject
{
    public ETextViewType viewType;
    public TextNode prevNode;
    public bool isSelection;
    public List<SingleTextNode> node;
    public TextNodeFinishedEvent finishedEvent;
}

[Serializable]
public class SingleTextNode
{
    public string text;
    public TextNode nextNode;
}

