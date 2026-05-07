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
    public Transform timelineContainer; // Vertical Layout Group 적용 패널

    [Header("Icon Prefabs")]
    public GameObject playerIconPrefab;
    public GameObject enemyIconPrefab;

    // 멤버 변수는 lowerCamelCase
    private List<BattleUnitOrder> turnTimeline = new List<BattleUnitOrder>();
    private List<GameObject> activeTimelineIcons = new List<GameObject>();

    public EBattleState CurrentState { get; private set; }

    // --------------- Unity Life Cycle --------------
    private void Start()
    {
        CurrentState = EBattleState.Start;
        // 게임 시작 시에만 타임라인 초기 데이터를 생성합니다.
        InitializeTimeline();
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

    // [이름 변경 및 로직 수정] 초기 데이터만 비율대로 생성합니다.
    public void InitializeTimeline()
    {
        turnTimeline.Clear();

        // 플레이어 2턴 : 적 1턴 비율의 최소 데이터만 넣습니다.
        for (int i = 0; i < 2; i++)
        {
            turnTimeline.Add(new BattleUnitOrder { unitType = ECharacterType.Player, unitName = "Player", actionValue = 100f });
        }
        turnTimeline.Add(new BattleUnitOrder { unitType = ECharacterType.Enemy, unitName = "Enemy", actionValue = 120f });

        // 초기 정렬 (AV 순서대로)
        turnTimeline = turnTimeline.OrderBy(unit => unit.actionValue).ToList();

        UpdateTimelineUI();
    }

    // --------------- private ------------------

    private void DetermineNextTurn()
    {
        if (turnTimeline.Count == 0) return;

        // 타임라인의 맨 앞(0번째) 유닛이 다음 턴입니다.
        BattleUnitOrder nextUnit = turnTimeline[0];

        if (nextUnit.unitType == ECharacterType.Player)
        {
            CurrentState = EBattleState.PlayerTurn;
            Debug.Log("<color=green>[Turn]</color> 플레이어의 턴입니다.");
        }
        else
        {
            CurrentState = EBattleState.EnemyTurn;
            Debug.Log("<color=red>[Turn]</color> 적의 턴입니다. 자동으로 공격합니다.");
            StartCoroutine(EnemyTurnSequence());
        }
    }

    // [수정] 아이콘 생성 시, 데이터가 8개 미만이라면 순환시켜서 강제로 채웁니다.
    private void UpdateTimelineUI()
    {
        foreach (GameObject icon in activeTimelineIcons)
        {
            Destroy(icon);
        }
        activeTimelineIcons.Clear();

        if (turnTimeline.Count == 0) return;

        // UI에 표시될 리스트 (8개 이상 유지하기 위해 가상으로 생성)
        List<BattleUnitOrder> displayList = new List<BattleUnitOrder>();

        int currentIndex = 0;
        while (displayList.Count < 8)
        {
            // 원본 리스트를 순환하며 8개를 채웁니다.
            int indexToUse = currentIndex % turnTimeline.Count;
            displayList.Add(turnTimeline[indexToUse]);
            currentIndex++;
        }

        // 채워진 displayList로 UI를 생성합니다.
        foreach (BattleUnitOrder unit in displayList)
        {
            GameObject prefabToSpawn = unit.unitType == ECharacterType.Player ? playerIconPrefab : enemyIconPrefab;

            if (prefabToSpawn != null)
            {
                GameObject iconObj = Instantiate(prefabToSpawn, timelineContainer);
                activeTimelineIcons.Add(iconObj);
            }
        }
    }

    private IEnumerator PlayerTurnSequence()
    {
        CurrentState = EBattleState.Busy;

        yield return StartCoroutine(player.AttackSequence());

        if (enemy.CurrentHp > 0)
        {
            // [중요] 행동 완료 후 데이터를 맨 뒤로 보냅니다.
            CycleFinishedUnit();
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
        CurrentState = EBattleState.Busy;

        yield return StartCoroutine(enemy.AttackSequence(player.transform));

        if (player.CurrentHp > 0)
        {
            // [중요] 행동 완료 후 데이터를 맨 뒤로 보냅니다.
            CycleFinishedUnit();
            yield return new WaitForSeconds(1.0f);
            DetermineNextTurn();
        }
        else
        {
            CurrentState = EBattleState.Lost;
            Debug.Log("전투 패배...");
        }
    }

    // [추가] 행동을 마친 맨 앞 데이터를 맨 뒤로 보내 턴을 순환시킵니다.
    private void CycleFinishedUnit()
    {
        if (turnTimeline.Count > 0)
        {
            BattleUnitOrder finishedUnit = turnTimeline[0];
            turnTimeline.RemoveAt(0);
            turnTimeline.Add(finishedUnit); // 맨 뒤로 이동

            UpdateTimelineUI();
        }
    }
}