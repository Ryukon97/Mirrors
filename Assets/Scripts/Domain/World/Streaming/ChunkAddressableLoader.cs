using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

public class ChunkAddressableLoader
{
    public AssetLabelReference assetLabel;
    public ChunkDataRegistry DataRegistry { get; private set; }
    public ChunkAssetRegistry AssetRegistry { get; private set; }
    IList<IResourceLocation> locations;

    public ChunkAddressableLoader()
    {
        DataRegistry = new ChunkDataRegistry();
        AssetRegistry = new ChunkAssetRegistry();
    }

    public async Awaitable GetLocations()
    {
        Debug.Log("[ChunkAddressableLoader] GetLocations...");
        var handle = Addressables.LoadResourceLocationsAsync(assetLabel.labelString);
        await handle.Task;
        locations = handle.Result;
        Debug.Log("[ChunkAddressableLoader] GetLocations...Complete");
    }
    public async Awaitable LoadAllChunkData()
    {
        float t = Time.time;
        Debug.Log("[ChunkAddressableLoader] LoadAllChunkData... Started: " + t);
        if (locations == null)
        {
            Debug.Log("Locations are not loaded");
            return;
        }
        List<AsyncOperationHandle<ChunkData>> handles = new List<AsyncOperationHandle<ChunkData>>();
        for(int i = 0; i < locations.Count; i++)
        {
            var handle = Addressables.LoadAssetAsync<ChunkData>(locations[i]);
            handle.Completed += (h) =>
            {
                DataRegistry.Register(handle.Result.chunkCoord, handle);
            };
            handles.Add(handle);
        }
        foreach (var handle in handles)
        {
            await handle.Task;
            Debug.Log($"[ChunkAddressableLoader] Chunk {handle.Result.chunkCoord} Loaded");
        }
        Debug.Log($"[ChunkAddressableLoader] LoadAllChunkData...Complete. Elapsed {Time.time - t}");
    }
    public async Awaitable LoadChunkAsset(Vector2Int coord)
    {
        float t = Time.time;
        Debug.Log($"[ChunkAddressableLoader] LoadChunkAsset... Chunk {coord} Started On {t}");
        ChunkDataInstance instance = DataRegistry.ChunkDatas[coord];
        if (instance.status != EChunkStatus.Unloaded)
        {
            Debug.LogWarning($"[ChunkAddressableLoader] LoadChunkAsset... Chunk {coord} Status is not Unloaded");
            return;
        }
        instance.status = EChunkStatus.Loading;

        foreach (var asset in instance.data.assets)
        {
            var handle = Addressables.LoadAssetAsync<GameObject>(asset.addressableKey);
            handle.Completed += (h) =>
            {
                AssetRegistry.Register(coord, h.Result, h);
            };
        }

        foreach (var asset in AssetRegistry.ChunkAssets[coord])
        {
            await asset.handle.Task;
            Debug.Log($"[ChunkAddressableLoader] Asset {asset.handle.Result.name} Loaded");
        }
        instance.status = EChunkStatus.Loaded;
        Debug.Log($"[ChunkAddressableLoader] LoadChunkAsset...Completed. Elapsed {Time.time - t}");
    }

    public void UnloadChunkAsset(Vector2Int coord)
    {
        if (DataRegistry.ChunkDatas.ContainsKey(coord) == false) return;

        var dataInstance = DataRegistry.ChunkDatas[coord];
        dataInstance.status = EChunkStatus.Unloading;

        AssetRegistry.UnRegister(coord); // Destroy + Release Æ÷ÇÔ

        dataInstance.status = EChunkStatus.Unloaded;
    }
}
