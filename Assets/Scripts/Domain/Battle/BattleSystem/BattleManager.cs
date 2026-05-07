using UnityEngine;
using System.Collections;

public class BattleManager : MonoBehaviour
{
    // --------------- 변수 영역 --------------
    public BattleCharacter player;
    public Enemy enemy;

    public EBattleState CurrentState { get; private set; }

    // --------------- Unity Life Cycle --------------
    private void Start()
    {
        CurrentState = EBattleState.PlayerTurn;
    }

    // --------------- public APIs --------------
    public void OnAttackButtonClick()
    {
        if (CurrentState != EBattleState.PlayerTurn)
        {
            return;
        }

        StartCoroutine(PlayerTurnSequence());
    }

    // --------------- private ------------------
    private IEnumerator PlayerTurnSequence()
    {
        CurrentState = EBattleState.Busy;

        // 플레이어 이동 및 공격
        yield return StartCoroutine(player.AttackSequence());

        if (enemy.CurrentHp > 0)
        {
            CurrentState = EBattleState.EnemyTurn;
            yield return new WaitForSeconds(1.0f);
            StartCoroutine(EnemyTurnSequence());
        }
    }

    private IEnumerator EnemyTurnSequence()
    {
        // 적 또한 플레이어처럼 움직이며 공격 시퀀스 실행
        yield return StartCoroutine(enemy.AttackSequence(player.transform));

        yield return new WaitForSeconds(1.0f);

        CurrentState = EBattleState.PlayerTurn;
    }
}