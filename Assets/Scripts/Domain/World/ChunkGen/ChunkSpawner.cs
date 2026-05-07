using UnityEngine;

/// <summary>
/// ChunkPreset을 입력받아 땅 프리팹을 배치하고,
/// GroundLayoutData의 SlotRule에 따라 요소들을 확률적으로 스폰합니다.
/// 런타임 / 에디터 모두에서 호출 가능하도록 생성 로직을 static으로 분리합니다.
/// </summary>
public class ChunkSpawner : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private ChunkDataConfig dataConfig;

    // ---------------------------------------------------------------
    // 런타임 진입점 (인스턴스 메서드 — dataConfig 사용)
    // ---------------------------------------------------------------

    /// <summary>
    /// dataConfig에 연결된 데이터로 청크를 생성합니다. (런타임 전용)
    /// </summary>
    public GameObject SpawnChunk(ChunkPreset preset, Vector3 worldOrigin, int seed = 0)
        => BuildChunk(preset, worldOrigin, seed, dataConfig);

    // ---------------------------------------------------------------
    // 에디터 / 런타임 공용 static 팩토리
    // ---------------------------------------------------------------

    /// <summary>
    /// ChunkDataConfig를 직접 주입해 청크 GameObject를 생성합니다.
    /// 에디터 툴과 런타임 모두에서 호출 가능합니다.
    /// </summary>
    public static GameObject BuildChunk(
        ChunkPreset preset,
        Vector3 worldOrigin,
        int seed,
        ChunkDataConfig dataConfig)
    {
        if (dataConfig == null)
        {
            Debug.LogError("[ChunkSpawner] ChunkDataConfig가 null입니다.");
            return null;
        }

        var chunkRoot = new GameObject($"Chunk_{preset.name}");
        chunkRoot.transform.position = worldOrigin;

        BuildGround(preset.groundType, chunkRoot.transform, dataConfig.groundPool);
        BuildElements(preset, chunkRoot.transform, seed, dataConfig);

        return chunkRoot;
    }

    // ---------------------------------------------------------------
    // Private helpers
    // ---------------------------------------------------------------

    private static void BuildGround(GroundType groundType, Transform parent, GroundPool groundPool)
    {
        var prefab = groundPool.GetRandomPrefab(groundType);
        if (prefab == null) return;

        Object.Instantiate(prefab, parent.position, Quaternion.identity, parent);
    }

    private static void BuildElements(
        ChunkPreset preset,
        Transform parent,
        int seed,
        ChunkDataConfig dataConfig)
    {
        if (!dataConfig.layoutLibrary.TryGetLayout(preset.groundType, out var layout))
        {
            Debug.LogWarning($"[ChunkSpawner] GroundType '{preset.groundType}'에 대한 LayoutData가 없습니다.");
            return;
        }

        var savedState = Random.state;
        Random.InitState(seed);

        foreach (var slot in layout.slotRules)
        {
            foreach (var candidate in slot.candidates)
            {
                if (!preset.elementTypes.Contains(candidate.elementType))
                    continue;

                if (Random.value > candidate.spawnChance)
                    continue;

                BuildElement(candidate.elementType, slot, parent, dataConfig.elementPool);
                break;
            }
        }

        Random.state = savedState;
    }

    private static void BuildElement(
        ElementType elementType,
        SlotRule slot,
        Transform parent,
        ElementPool elementPool)
    {
        var prefab = elementPool.GetRandomPrefab(elementType);
        if (prefab == null) return;

        var worldPos = parent.position + slot.localPosition;
        var eulerY   = slot.randomizeYaw ? Random.Range(0f, 360f) : slot.rotation.y;
        var rotation = Quaternion.Euler(slot.rotation.x, eulerY, slot.rotation.z);

        Object.Instantiate(prefab, worldPos, rotation, parent);
    }
}
