/// <summary>
/// 땅 유형. GroundPool과 GroundLayoutData의 키로 사용됩니다.
/// </summary>
public enum GroundType
{
    Grassland,
    Roadway_0way,
    Roadway_1way,
    Roadway_2way_Straight,
    Roadway_2way_Curved,
    Roadway_3way,
    Roadway_4way,
}

/// <summary>
/// 요소 유형. ElementPool과 GroundLayoutData 배치 규칙의 키로 사용됩니다.
/// </summary>
public enum ElementType
{
    S_Building,
    M_Building,
    H_Building,
    Tree_BesideRoad,
}
