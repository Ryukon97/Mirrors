using UnityEngine;


[RequireComponent(typeof(ChunkStateTracker))]
public class WorldStreamingSystem : MonoBehaviour
{
    private static WorldStreamingSystem instance;
    public static WorldStreamingSystem Instance { get => instance; }
    public bool IsChunkLoaded { get; private set; }

    // 거리순 정렬
    ChunkLoadPriorityQueue chunkLoadPriorityQueue;

    // ChunkData SO 로드
    ChunkAddressableLoader chunkAddressableLoader;

    private async void Awake()
    {
        if(instance != null)
        {
            Destroy(instance.gameObject);
            return;
        }
        IsChunkLoaded = false;
        instance = this;

        chunkLoadPriorityQueue = new ChunkLoadPriorityQueue();
        chunkAddressableLoader = new ChunkAddressableLoader();

        await LoadAndInitChunks();

        IsChunkLoaded = true;
    }
    async Awaitable LoadAndInitChunks()
    {
        await chunkAddressableLoader.GetLocations();
        await chunkAddressableLoader.LoadAllChunkData();
    }
}
