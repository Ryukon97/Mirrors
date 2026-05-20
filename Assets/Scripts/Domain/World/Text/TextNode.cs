using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TextNode", menuName = "World/TextNode")]
public class TextNode : ScriptableObject
{
    public string id;
    public ETextViewType viewType;
    public TextNode prevNode;
    public bool isSelection;
    public List<SingleTextNode> node;
    public TextNodeFinishedArgs finishedEvent;
    public Sprite illust;
}

[Serializable]
public class SingleTextNode
{
    public string text;
    public TextNode nextNode;
}

