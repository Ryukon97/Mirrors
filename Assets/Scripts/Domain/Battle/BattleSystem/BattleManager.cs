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
    public Transform timelineContainer; // Vertical Layout Group이 붙은 패널
    public GameObject timelineIconPrefab;

    private List<BattleUnitOrder> turnTimeline = new List<BattleUnitOrder>();
    private List<GameObject> activeTimelineIcons = new List<GameObject>();

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
        // 플레이어 턴일 때만 버튼 작동
        if (CurrentState != EBattleState.PlayerTurn)
        {
            return;
        }

        StartCoroutine(PlayerTurnSequence());
    }

    public void UpdateTimeline()
    {
        turnTimeline.Clear();

        // 임의의 속도 계산 (Player: 100, Enemy: 80)
        float playerAV = 10000f / 100f;
        float enemyAV = 10000f / 80f;

        turnTimeline.Add(new BattleUnitOrder { unitType = ECharacterType.Player, unitName = "Player", actionValue = playerAV });
        turnTimeline.Add(new BattleUnitOrder { unitType = ECharacterType.Enemy, unitName = "Enemy", actionValue = enemyAV });

        turnTimeline = turnTimeline.OrderBy(unit => unit.actionValue).ToList();

        UpdateTimelineUI();
    }

    // --------------- private ------------------

    private void DetermineNextTurn()
    {
        if (turnTimeline.Count == 0) return;

        BattleUnitOrder nextUnit = turnTimeline[0];

        if (nextUnit.unitType == ECharacterType.Player)
        {
            CurrentState = EBattleState.PlayerTurn;
            Debug.Log("<color=green>[Turn]</color> 플레이어의 턴입니다. 버튼을 누르세요.");
        }
        else
        {
            CurrentState = EBattleState.EnemyTurn;
            Debug.Log("<color=red>[Turn]</color> 적의 턴입니다. 자동으로 공격합니다.");
            StartCoroutine(EnemyTurnSequence());
        }
    }

    private void UpdateTimelineUI()
    {
        foreach (GameObject icon in activeTimelineIcons)
        {
            Destroy(icon);
        }
        activeTimelineIcons.Clear();

        foreach (BattleUnitOrder unit in turnTimeline)
        {
            if (timelineIconPrefab != null)
            {
                GameObject iconObj = Instantiate(timelineIconPrefab, timelineContainer);
                activeTimelineIcons.Add(iconObj);
            }
        }
    }

    private IEnumerator PlayerTurnSequence()
    {
        CurrentState = EBattleState.Busy;

        // 플레이어가 적에게 다가가서 공격하고 복귀
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