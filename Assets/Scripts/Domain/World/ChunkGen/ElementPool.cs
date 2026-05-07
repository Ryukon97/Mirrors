using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ElementType별로 오브젝트 프리팹들을 보유하는 데이터 컨테이너 ScriptableObject.
/// 동일 ElementType에 여러 프리팹을 등록하면 랜덤으로 선택됩니다.
/// </summary>
[CreateAssetMenu(fileName = "ElementPool", menuName = "World/Element Pool")]
public class ElementPool : ScriptableObject
{
    [System.Serializable]
    public class ElementEntry
    {
        public ElementType elementType;
        public List<GameObject> prefabs = new();
    }

    [SerializeField] private List<ElementEntry> entries = new();

    public IReadOnlyList<ElementEntry> Entries => entries;

    private Dictionary<ElementType, List<GameObject>> _cache;

    private void BuildCache()
    {
        _cache = new Dictionary<ElementType, List<GameObject>>();
        foreach (var e in entries)
        {
            if (e == null || e.prefabs == null || e.prefabs.Count == 0) continue;
            if (_cache.ContainsKey(e.elementType))
            {
                Debug.LogWarning($"[ElementPool] ElementType '{e.elementType}' 중복 등록 — 첫 번째 항목만 사용합니다.");
                continue;
            }
            _cache[e.elementType] = e.prefabs;
        }
    }

    /// <summary>
    /// 해당 ElementType에 등록된 프리팹 중 하나를 랜덤 반환합니다.
    /// </summary>
    public GameObject GetRandomPrefab(ElementType type)
    {
        if (_cache == null) BuildCache();

        if (!_cache.TryGetValue(type, out var list) || list.Count == 0)
        {
            Debug.LogWarning($"[ElementPool] '{type}'에 등록된 프리팹이 없습니다.");
            return null;
        }

        return list[Random.Range(0, list.Count)];
    }
}
