using UnityEngine;

/// <summary>
/// 맵 전체 구조에 관한 공용 설정 ScriptableObject.
/// ChunkGeneratorTool(에디터)과 ChunkLoader(런타임) 양쪽에서 참조합니다.
/// </summary>
[CreateAssetMenu(fileName = "ChunkMapConfig", menuName = "World/Chunk Map Config")]
public class ChunkMapConfig : ScriptableObject
{
    [Tooltip("청크 하나의 월드 크기 (m).")]
    public float chunkSize = 50f;

    [Tooltip("X축 방향 전체 청크 수.")]
    public int gridWidth = 10;

    [Tooltip("Z축 방향 전체 청크 수.")]
    public int gridHeight = 10;

    // ---------------------------------------------------------------
    // 유틸리티
    // ---------------------------------------------------------------

    /// <summary>그리드 좌표 → 월드 원점 변환.</summary>
    public Vector3 ChunkCoordToWorld(Vector2Int coord)
        => new Vector3(coord.x * chunkSize, 0f, coord.y * chunkSize);

    /// <summary>월드 좌표 → 그리드 좌표 변환.</summary>
    public Vector2Int WorldToChunkCoord(Vector3 worldPos)
        => new Vector2Int(
            Mathf.FloorToInt(worldPos.x / chunkSize),
            Mathf.FloorToInt(worldPos.z / chunkSize));

    /// <summary>그리드 범위 안에 있는 좌표인지 검사.</summary>
    public bool IsInBounds(Vector2Int coord)
        => coord.x >= 0 && coord.x < gridWidth &&
           coord.y >= 0 && coord.y < gridHeight;

    /// <summary>"Chunk_X_Z" 형식의 어드레서블 키 반환.</summary>
    public static string ChunkKey(int x, int z) => $"Chunk_{x}_{z}";

    /// <summary>"Chunk_X_Z" 형식의 어드레서블 키 반환 (Vector2Int 오버로드).</summary>
    public static string ChunkKey(Vector2Int coord) => $"Chunk_{coord.x}_{coord.y}";
}
