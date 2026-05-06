using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

public class ChunkDataRegistry
{
    public Dictionary<Vector2Int, ChunkDataInstance> ChunkDatas { get; private set; }

    public ChunkDataRegistry()
    {
        ChunkDatas = new Dictionary<Vector2Int, ChunkDataInstance>();
    }
    public void Register(Vector2Int coord, AsyncOperationHandle<ChunkData> handle)
    {
        var key = coord;
        var value = new ChunkDataInstance(handle.Result, EChunkStatus.Unloaded);
        ChunkDatas.Add(key, value);
    }
    public void UnRegister(Vector2Int key)
    {
        if (ChunkDatas.ContainsKey(key))
        {
            ChunkDatas.Remove(key);
        }
    }
}