// 파일명 권장: BattleUnitOrder.cs

// E 접두사 및 PascalCase 적용
public enum ECharacterType
{
    Player,
    Enemy
}

// PascalCase 적용
public struct BattleUnitOrder
{
    public ECharacterType unitType;
    public string unitName;
    public float actionValue;
    public Enemy enemyReference; // [추가] 실제 어떤 적 오브젝트인지 저장
}