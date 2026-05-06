using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// GroundType별로 땅 프리팹을 보유하는 데이터 컨테이너 ScriptableObject.
/// 동일 GroundType에 여러 프리팹을 등록하면 랜덤으로 선택됩니다.
/// </summary>
[CreateAssetMenu(fileName = "GroundPool", menuName = "World/Ground Pool")]
public class GroundPool : ScriptableObject
{
    [System.Serializable]
    public class GroundEntry
    {
        public GroundType groundType;
        public List<GameObject> prefabs = new();
    }

    [SerializeField] private List<GroundEntry> entries = new();

    // 에디터에서 직접 접근 / 디버깅용
    public IReadOnlyList<GroundEntry> Entries => entries;

    private Dictionary<GroundType, List<GameObject>> _cache;

    private void BuildCache()
    {
        _cache = new Dictionary<GroundType, List<GameObject>>();
        foreach (var e in entries)
        {
            if (e == null || e.prefabs == null || e.prefabs.Count == 0) continue;
            if (_cache.ContainsKey(e.groundType))
            {
                Debug.LogWarning($"[GroundPool] GroundType '{e.groundType}' 중복 등록 — 첫 번째 항목만 사용합니다.");
                continue;
            }
            _cache[e.groundType] = e.prefabs;
        }
    }

    /// <summary>
    /// 해당 GroundType에 등록된 프리팹 중 하나를 랜덤 반환합니다.
    /// </summary>
    public GameObject GetRandomPrefab(GroundType type)
    {
        if (_cache == null) BuildCache();

        if (!_cache.TryGetValue(type, out var list) || list.Count == 0)
        {
            Debug.LogWarning($"[GroundPool] '{type}'에 등록된 프리팹이 없습니다.");
            return null;
        }

        return list[Random.Range(0, list.Count)];
    }
}
