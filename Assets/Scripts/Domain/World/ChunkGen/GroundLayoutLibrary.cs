using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// GroundType별 GroundLayoutData를 한곳에서 관리하는 ScriptableObject.
/// ChunkSpawner가 이 라이브러리를 통해 배치 규칙을 조회합니다.
/// </summary>
[CreateAssetMenu(fileName = "GroundLayoutLibrary", menuName = "World/Ground Layout Library")]
public class GroundLayoutLibrary : ScriptableObject
{
    [SerializeField] private List<GroundLayoutData> layouts = new();

    private Dictionary<GroundType, GroundLayoutData> _cache;

    private void BuildCache()
    {
        _cache = new Dictionary<GroundType, GroundLayoutData>();
        foreach (var l in layouts)
        {
            if (l == null) continue;
            if (_cache.ContainsKey(l.groundType))
            {
                Debug.LogWarning($"[GroundLayoutLibrary] GroundType '{l.groundType}' 중복 등록 — 첫 번째 항목만 사용합니다.");
                continue;
            }
            _cache[l.groundType] = l;
        }
    }

    public bool TryGetLayout(GroundType type, out GroundLayoutData layout)
    {
        if (_cache == null) BuildCache();
        return _cache.TryGetValue(type, out layout);
    }
}
