using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemDB", menuName = "Scriptable Objects/ItemDB")]
public class ItemDB : ScriptableObject
{
    public List<ItemData> items;
}
[Serializable]
public class ItemData
{
    public string id;
    public Sprite sprite;
}
