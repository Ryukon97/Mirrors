using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;


// Instantiated GameObject Registry
public class ChunkAssetRegistry
{
    public Dictionary<Vector2Int, List<ChunkAssetInstance>> ChunkAssets { get; private set; } 
    public ChunkAssetRegistry()
    {
        ChunkAssets = new Dictionary<Vector2Int, List<ChunkAssetInstance>>();
    }

    public void Register(Vector2Int key, GameObject value, AsyncOperationHandle<GameObject> handle)
    {
        if(ChunkAssets.ContainsKey(key) == false)
        {
            ChunkAssets.Add(key, new List<ChunkAssetInstance>());
        }
        ChunkAssets[key].Add(new ChunkAssetInstance(handle, value));
    }
    public void UnRegister(Vector2Int key)
    {
        foreach (var instance in ChunkAssets[key])
        {
            if(instance.go != null)
                Object.Destroy(instance.go);
            Addressables.Release(instance.handle);
        }
        ChunkAssets.Remove(key);
    }
}