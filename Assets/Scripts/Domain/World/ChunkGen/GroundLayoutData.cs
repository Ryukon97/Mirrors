using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 청크 내 로컬 좌표 한 슬롯에 대한 배치 후보.
/// 여러 후보가 있을 때 spawnChance 합계가 1을 넘어도 되며,
/// ChunkSpawner가 순서대로 확률 판정해 첫 번째 성공한 후보를 사용합니다.
/// </summary>
[System.Serializable]
public class ElementCandidate
{
    public ElementType elementType;
    [Range(0f, 1f)] public float spawnChance = 0.5f;
}

/// <summary>
/// 청크 로컬 좌표 한 슬롯에 배치될 수 있는 후보 목록.
/// </summary>
[System.Serializable]
public class SlotRule
{
    /// <summary>청크 기준 로컬 위치 (월드 단위).</summary>
    public Vector3 localPosition;

    /// <summary>
    /// 스폰될 오브젝트의 회전값 (오일러각).
    /// randomizeYaw가 true이면 Y축은 런타임에 랜덤 적용됩니다.
    /// </summary>
    public Vector3 rotation;

    /// <summary>Y축 회전을 런타임에 랜덤화할지 여부.</summary>
    public bool randomizeYaw = false;

    /// <summary>
    /// 이 슬롯에 스폰될 수 있는 후보들.
    /// 순서대로 확률 판정 — 첫 번째 성공한 ElementType이 스폰됩니다.
    /// </summary>
    public List<ElementCandidate> candidates = new();
}

/// <summary>
/// 특정 GroundType 위에서 적용될 요소 배치 규칙 전체를 담는 ScriptableObject.
/// 여러 SlotRule의 집합으로 구성됩니다.
/// </summary>
[CreateAssetMenu(fileName = "GroundLayoutData", menuName = "World/Ground Layout Data")]
public class GroundLayoutData : ScriptableObject
{
    public GroundType groundType;

    [Tooltip("청크 내 각 슬롯의 배치 규칙 목록.")]
    public List<SlotRule> slotRules = new();
}
