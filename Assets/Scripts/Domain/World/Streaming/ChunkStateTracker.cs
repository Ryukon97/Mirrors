using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 주변 N칸 반경을 계산해서 로드해야 할 청크, 
/// 언로드해야 할 청크 목록을 만들어 WorldStreamingSystem에 알립니다. 
/// 청크의 현재 상태(Unloaded / Loading / Loaded / Unloading)를 enum으로 관리합니다.
/// </summary>
/// 

[RequireComponent(typeof(WorldStreamingSystem))]
public class ChunkStateTracker: MonoBehaviour
{
    const int CHUNK_SIZE = 64;
    
    [SerializeField] ChunkAddressableLoader loader;

    public void OnPlayerMoved(Vector3 pos)
    {
        Vector2Int coord = new Vector2Int(
                Mathf.FloorToInt(pos.x / CHUNK_SIZE),
                Mathf.FloorToInt(pos.y / CHUNK_SIZE)
            );
        if(IsValidCoord(coord) == false)
        {
            Debug.LogWarning($"[ChunkStateTracker] OnPlayerMoved: InValid Coord: {coord}");
        }

        if (loader.DataRegistry.ChunkDatas[coord].status == EChunkStatus.Unloaded)
        {
            loader.LoadChunkAsset(coord);
        }
    }

    bool IsValidCoord(Vector2Int coord) => loader.DataRegistry.ChunkDatas.ContainsKey(coord);
}
