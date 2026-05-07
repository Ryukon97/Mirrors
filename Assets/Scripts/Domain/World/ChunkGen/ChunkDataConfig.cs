using UnityEngine;

/// <summary>
/// 청크 생성에 필요한 데이터 레퍼런스를 한곳에서 관리하는 ScriptableObject.
/// ChunkGeneratorTool(에디터)과 ChunkSpawner(런타임) 양쪽에서 참조합니다.
/// </summary>
[CreateAssetMenu(fileName = "ChunkDataConfig", menuName = "World/Chunk Data Config")]
public class ChunkDataConfig : ScriptableObject
{
    [Header("Pools")]
    public GroundPool groundPool;
    public ElementPool elementPool;

    [Header("Layout")]
    public GroundLayoutLibrary layoutLibrary;

    [Header("Presets")]
    [Tooltip("맵 생성 시 각 셀에서 랜덤 선택될 ChunkPreset 목록.")]
    public ChunkPreset[] chunkPresets;

    // ---------------------------------------------------------------
    // 유효성 검사
    // ---------------------------------------------------------------

    /// <summary>
    /// 필수 레퍼런스가 모두 채워졌는지 확인합니다.
    /// 문제가 있으면 로그를 출력하고 false를 반환합니다.
    /// </summary>
    public bool Validate(string callerName)
    {
        if (groundPool == null || elementPool == null || layoutLibrary == null)
        {
            Debug.LogError($"[{callerName}] ChunkDataConfig: GroundPool / ElementPool / LayoutLibrary를 모두 연결해주세요.");
            return false;
        }
        if (chunkPresets == null || chunkPresets.Length == 0)
        {
            Debug.LogError($"[{callerName}] ChunkDataConfig: ChunkPreset이 하나 이상 필요합니다.");
            return false;
        }
        return true;
    }
}
