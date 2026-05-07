using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 청크 하나의 구성 정보를 정의하는 ScriptableObject.
/// GroundType과 해당 청크에 등장할 수 있는 ElementType 목록을 보유합니다.
/// </summary>
[CreateAssetMenu(fileName = "ChunkPreset", menuName = "World/Chunk Preset")]
public class ChunkPreset : ScriptableObject
{
    [Header("Ground")]
    public GroundType groundType;
    public Vector3 rotation;

    [Header("Allowed Elements")]
    public List<ElementType> elementTypes = new();
}
