#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

/// <summary>
/// [에디터 전용] 청크를 절차적으로 생성하고 프리팹으로 저장한 뒤
/// Addressables에 "Chunk_X_Z" 키로 등록합니다.
///
/// 사용법:
///   1. 씬에 이 컴포넌트를 가진 GameObject를 배치합니다.
///   2. 인스펙터에서 Config와 설정을 채웁니다.
///   3. 인스펙터 우클릭 → "Generate All Chunks" 실행.
/// </summary>
public class ChunkGeneratorTool : MonoBehaviour
{
    // ---------------------------------------------------------------
    // 공용 Config
    // ---------------------------------------------------------------

    [Header("Config")]
    [SerializeField] private ChunkMapConfig mapConfig;
    [SerializeField] private ChunkDataConfig dataConfig;

    // ---------------------------------------------------------------
    // 툴 전용 설정
    // ---------------------------------------------------------------

    [Header("Generation Settings")]
    [Tooltip("전체 맵 생성에 사용할 랜덤 시드.")]
    [SerializeField] private int masterSeed = 0;

    [Header("Save Settings")]
    [Tooltip("프리팹이 저장될 경로. Assets/ 로 시작해야 합니다.")]
    [SerializeField] private string savePath = "Assets/Prefabs/World/GeneratedChunks";

    [Tooltip("어드레서블 그룹 이름. 없으면 신규 생성됩니다.")]
    [SerializeField] private string addressableGroup = "Chunks";

    // ---------------------------------------------------------------
    // 에디터 진입점
    // ---------------------------------------------------------------

    [ContextMenu("Generate All Chunks")]
    private void GenerateAllChunks()
    {
        if (!ValidateInputs()) return;

        EnsureDirectory(savePath);

        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Debug.LogError("[ChunkGeneratorTool] Addressable Asset Settings를 찾을 수 없습니다. " +
                           "Window > Asset Management > Addressables > Groups 에서 초기화하세요.");
            return;
        }

        var group = GetOrCreateGroup(settings, addressableGroup);
        var rng   = new System.Random(masterSeed);

        int total     = mapConfig.gridWidth * mapConfig.gridHeight;
        int generated = 0;

        ChunkPreset[,] chunkPresets = new ChunkPreset[mapConfig.gridHeight, mapConfig.gridWidth];

        ChunkSelector chunkSelector = new ChunkSelector(mapConfig, chunkPresets);
        try
        {
            for (int z = 0; z < mapConfig.gridHeight; z++)
            {
                for (int x = 0; x < mapConfig.gridWidth; x++)
                {
                    string key = ChunkMapConfig.ChunkKey(x, z);
                    EditorUtility.DisplayProgressBar(
                        "청크 생성 중",
                        $"{key} ({generated + 1}/{total})",
                        (float)generated / total);
                    var preset = chunkSelector.SelectChunk(dataConfig, rng, x, z);
                    if (preset == null)
                    {
                        Debug.LogWarning("Preset Null");
                        continue;
                    }
                    int chunkSeed   = rng.Next();
                    var worldOrigin = mapConfig.ChunkCoordToWorld(new UnityEngine.Vector2Int(x, z));

                    chunkPresets[z, x] = preset;

                    GenerateAndSaveChunk(preset, worldOrigin, chunkSeed, key, settings, group);
                    generated++;
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[ChunkGeneratorTool] 청크 {generated}개 생성 완료.");
    }

    // ---------------------------------------------------------------
    // 핵심 로직
    // ---------------------------------------------------------------

    private void GenerateAndSaveChunk(
        ChunkPreset preset,
        UnityEngine.Vector3 worldOrigin,
        int seed,
        string key,
        AddressableAssetSettings settings,
        AddressableAssetGroup group)
    {
        // 1. 청크 GameObject 생성
        var chunkGo = ChunkSpawner.BuildChunk(preset, worldOrigin, seed, dataConfig);

        // 2. 프리팹으로 저장 (이미 존재하면 덮어씀)
        string prefabPath = Path.Combine(savePath, $"{key}.prefab").Replace("\\", "/");
        bool   existed    = File.Exists(prefabPath);

        PrefabUtility.SaveAsPrefabAsset(chunkGo, prefabPath, out bool success);
        DestroyImmediate(chunkGo);

        if (!success)
        {
            Debug.LogError($"[ChunkGeneratorTool] 프리팹 저장 실패: {prefabPath}");
            return;
        }

        // 3. 어드레서블 등록 (이미 존재하면 덮어씀)
        string guid = AssetDatabase.AssetPathToGUID(prefabPath);

        var existingEntry = settings.FindAssetEntry(guid);
        if (existingEntry != null)
            settings.RemoveAssetEntry(guid);

        var entry = settings.CreateOrMoveEntry(guid, group, readOnly: false, postEvent: false);
        entry.address = key;

        string action = existed ? "덮어씀" : "신규 등록";
        Debug.Log($"[ChunkGeneratorTool] {action}: {key} → {prefabPath}");
    }

    // ---------------------------------------------------------------
    // 유틸리티
    // ---------------------------------------------------------------

    private bool ValidateInputs()
    {
        if (mapConfig == null)
        {
            Debug.LogError("[ChunkGeneratorTool] ChunkMapConfig를 연결해주세요.");
            return false;
        }
        if (mapConfig.gridWidth <= 0 || mapConfig.gridHeight <= 0)
        {
            Debug.LogError("[ChunkGeneratorTool] Grid 크기는 1 이상이어야 합니다.");
            return false;
        }
        if (dataConfig == null)
        {
            Debug.LogError("[ChunkGeneratorTool] ChunkDataConfig를 연결해주세요.");
            return false;
        }
        return dataConfig.Validate("ChunkGeneratorTool");
    }

    private static void EnsureDirectory(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        string[] parts   = path.Split('/');
        string   current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    private static AddressableAssetGroup GetOrCreateGroup(
        AddressableAssetSettings settings, string groupName)
    {
        var group = settings.FindGroup(groupName);
        if (group != null) return group;

        group = settings.CreateGroup(
            groupName,
            setAsDefaultGroup: false,
            readOnly: false,
            postEvent: false,
            schemasToCopy: null);

        Debug.Log($"[ChunkGeneratorTool] 어드레서블 그룹 '{groupName}' 생성.");
        return group;
    }
}
#endif
