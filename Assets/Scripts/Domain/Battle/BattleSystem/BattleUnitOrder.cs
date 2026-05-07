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
    // 멤버 변수는 lowerCamelCase 적용
    public ECharacterType unitType;
    public string unitName;
    public float actionValue; // 턴이 돌아오기까지 남은 값
}