using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// 플레이어 위치를 기준으로 주변 청크를 Addressables로 로드/언로드합니다.
/// 키 규칙: "Chunk_X_Z" (ChunkMapConfig.ChunkKey 와 동일)
/// </summary>
public class ChunkLoader : MonoBehaviour
{
    // ---------------------------------------------------------------
    // 설정
    // ---------------------------------------------------------------

    [Header("Config")]
    [SerializeField] private ChunkMapConfig mapConfig;

    [Header("Target")]
    [Tooltip("기준이 되는 플레이어 Transform.")]
    [SerializeField] private Transform player;

    [Header("Load Settings")]
    [Tooltip("플레이어 기준으로 로드할 청크 반경 (청크 단위). " +
             "예: 2이면 플레이어 주변 5×5 범위를 로드.")]
    [SerializeField] private int loadRadius = 2;

    // ---------------------------------------------------------------
    // 내부 상태
    // ---------------------------------------------------------------

    // 현재 로드된 청크: 그리드 좌표 → (인스턴스 GameObject, AsyncOperationHandle)
    private readonly Dictionary<Vector2Int, (GameObject instance, AsyncOperationHandle<GameObject> handle)>
        _loadedChunks = new();

    // 로드 중인 청크 (중복 요청 방지)
    private readonly HashSet<Vector2Int> _pending = new();

    private Vector2Int _lastPlayerChunk = new(int.MinValue, int.MinValue);

    // ---------------------------------------------------------------
    // Unity 생명주기
    // ---------------------------------------------------------------

    private void Update()
    {
        if (player == null) return;
        var currentChunk = mapConfig.WorldToChunkCoord(player.position);
        if (currentChunk == _lastPlayerChunk) return;

        _lastPlayerChunk = currentChunk;
        UpdateChunks(currentChunk);
    }

    private void OnDestroy()
    {
        UnloadAll();
    }

    // ---------------------------------------------------------------
    // 로드/언로드 관리
    // ---------------------------------------------------------------

    private void UpdateChunks(Vector2Int center)
    {
        var desired = new HashSet<Vector2Int>();

        for (int dx = -loadRadius; dx <= loadRadius; dx++)
        {
            for (int dz = -loadRadius; dz <= loadRadius; dz++)
            {
                var coord = new Vector2Int(center.x + dx, center.y + dz);

                if (!mapConfig.IsInBounds(coord)) continue;

                desired.Add(coord);
            }
        }

        // 범위 밖 청크 언로드
        var toUnload = new List<Vector2Int>();
        foreach (var coord in _loadedChunks.Keys)
        {
            if (!desired.Contains(coord))
                toUnload.Add(coord);
        }
        foreach (var coord in toUnload)
            UnloadChunk(coord);

        // 범위 내 미로드 청크 로드
        foreach (var coord in desired)
        {
            if (!_loadedChunks.ContainsKey(coord) && !_pending.Contains(coord))
                _ = LoadChunkAsync(coord);
        }
    }

    private async Task LoadChunkAsync(Vector2Int coord)
    {
        _pending.Add(coord);

        string key      = ChunkMapConfig.ChunkKey(coord);
        var    worldPos = mapConfig.ChunkCoordToWorld(coord);

        var handle = Addressables.InstantiateAsync(key, worldPos, Quaternion.identity);
        try
        {
            await handle.Task;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[ChunkLoader] 청크 로드 예외: {key} — {e.Message}");
            _pending.Remove(coord);
            return;
        }

        // await 이후 이 MonoBehaviour가 Destroy됐을 수 있음
        if (this == null)
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
                Addressables.ReleaseInstance(handle);
            return;
        }

        // await 이후 언로드 요청이 들어왔을 수 있으므로 상태 재확인
        if (!_pending.Contains(coord))
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
                Addressables.ReleaseInstance(handle);
            return;
        }

        _pending.Remove(coord);

        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogWarning($"[ChunkLoader] 청크 로드 실패: {key}");
            return;
        }

        _loadedChunks[coord] = (handle.Result, handle);
    }

    private void UnloadChunk(Vector2Int coord)
    {
        _pending.Remove(coord);

        if (!_loadedChunks.TryGetValue(coord, out var entry)) return;

        Addressables.ReleaseInstance(entry.handle);
        _loadedChunks.Remove(coord);
    }

    private void UnloadAll()
    {
        _pending.Clear();

        foreach (var entry in _loadedChunks.Values)
            Addressables.ReleaseInstance(entry.handle);

        _loadedChunks.Clear();
    }
}
