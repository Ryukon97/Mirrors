using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class BattleManager : MonoBehaviour
{
    // --------------- 변수 영역 --------------
    public BattleCharacter player;
    public Enemy enemy;

    [Header("UI References")]
    public Transform timelineContainer; // 아이콘들이 나열될 부모 UI (Horizontal Layout Group 적용)
    public GameObject timelineIconPrefab; // TimelineIcon Prefab 할당

    // 리스트는 복수형 사용, 멤버 변수는 lowerCamelCase
    private List<BattleUnitOrder> turnTimeline = new List<BattleUnitOrder>();
    private List<GameObject> activeTimelineIcons = new List<GameObject>(); // 생성된 아이콘 관리용

    public EBattleState CurrentState { get; private set; }

    // --------------- Unity Life Cycle --------------
    private void Start()
    {
        CurrentState = EBattleState.Start;
        UpdateTimeline();
        DetermineNextTurn();
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

    public void UpdateTimeline()
    {
        turnTimeline.Clear();

        float playerAV = 10000f / 100f;
        float enemyAV = 10000f / 80f;

        turnTimeline.Add(new BattleUnitOrder { unitType = ECharacterType.Player, unitName = "Player", actionValue = playerAV });
        turnTimeline.Add(new BattleUnitOrder { unitType = ECharacterType.Enemy, unitName = "Enemy", actionValue = enemyAV });

        turnTimeline = turnTimeline.OrderBy(unit => unit.actionValue).ToList();

        // UI 갱신 함수 호출
        UpdateTimelineUI();
    }

    // --------------- private ------------------

    private void DetermineNextTurn()
    {
        if (turnTimeline.Count == 0)
        {
            return;
        }

        var nextUnit = turnTimeline[0];

        if (nextUnit.unitType == ECharacterType.Player)
        {
            CurrentState = EBattleState.PlayerTurn;
            Debug.Log("<color=green>[Turn]</color> 플레이어의 턴입니다.");
        }
        else
        {
            CurrentState = EBattleState.EnemyTurn;
            StartCoroutine(EnemyTurnSequence());
        }
    }

    // [추가된 함수] 타임라인 UI를 화면에 생성하고 업데이트하는 함수
    private void UpdateTimelineUI()
    {
        // 1. 기존 생성되어 있는 UI 아이콘 모두 제거
        foreach (var icon in activeTimelineIcons)
        {
            Destroy(icon);
        }
        activeTimelineIcons.Clear();

        // 2. 새로운 순서대로 아이콘 생성
        foreach (var unit in turnTimeline)
        {
            // 컨테이너(Horizontal Layout Group)의 자식으로 생성
            GameObject iconObj = Instantiate(timelineIconPrefab, timelineContainer);
            activeTimelineIcons.Add(iconObj);

            // TODO: 여기서 초상화 이미지(Image 컴포넌트)를 unitType에 따라 바꿔줄 수 있습니다.
            // 예: ECharacterType.Player 라면 플레이어 스프라이트로 변경
        }
    }

    private IEnumerator PlayerTurnSequence()
    {
        CurrentState = EBattleState.Busy;

        yield return StartCoroutine(player.AttackSequence());

        if (enemy.CurrentHp > 0)
        {
            UpdateTimeline();
            yield return new WaitForSeconds(1.0f);
            DetermineNextTurn();
        }
        else
        {
            CurrentState = EBattleState.Won;
            Debug.Log("전투 승리!");
        }
    }

    private IEnumerator EnemyTurnSequence()
    {
        // 중요: 적이 플레이어에게 다가가서 공격하고 복귀하는 연출 실행
        yield return StartCoroutine(enemy.AttackSequence(player.transform));

        if (player.CurrentHp > 0)
        {
            UpdateTimeline();
            yield return new WaitForSeconds(1.0f);
            DetermineNextTurn();
        }
        else
        {
            CurrentState = EBattleState.Lost;
            Debug.Log("전투 패배...");
        }
    }
}