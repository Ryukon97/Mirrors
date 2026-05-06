using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ChunkData", menuName = "Scriptable Objects/World/ChunkData")]
public class ChunkData : ScriptableObject
{
    public Vector2Int chunkCoord;        // (3, 5)
    public List<ChunkDataEntry> assets;
}
[System.Serializable]
public class ChunkDataEntry
{
    public string addressableKey;        // "structure/3_5/building_blacksmith"
    public EAssetLayer layer;
    public ELoadPriority priority;
}

public enum EAssetLayer { Terrain, Structure, Prop, NPC }
public enum ELoadPriority { High, Normal, Low }