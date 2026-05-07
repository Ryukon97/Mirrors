using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class BattleManager : MonoBehaviour
{
    // --------------- 변수 영역 --------------
    [Header("Characters")]
    public BattleCharacter player;
    public Enemy enemy;

    [Header("UI References")]
    public Transform timelineContainer; // Vertical Layout Group 적용 패널
    public GameObject playerIconPrefab;
    public GameObject enemyIconPrefab;

    [Header("Ultimate System")]
    public Button ultimateButton;      // 필살기 버튼
    public Image ultimateGaugeImage;   // 게이지 바 (Filled 타입)
    private float currentGauge = 0f;
    private const float MAX_GAUGE = 100f;
    private const float GAUGE_PER_ATTACK = 25f; // 4번 공격 시 필살기 가능

    private List<BattleUnitOrder> turnTimeline = new List<BattleUnitOrder>();
    private List<GameObject> activeTimelineIcons = new List<GameObject>();

    public EBattleState CurrentState { get; private set; }

    // --------------- Unity Life Cycle --------------
    private void Start()
    {
        CurrentState = EBattleState.Start;

        UpdateUltimateUI();
        InitializeTimeline(); // 초기 [P, P, E] 데이터 생성
        DetermineNextTurn();  // 첫 번째 턴 결정
    }

    // --------------- public APIs --------------

    // 일반 공격 버튼 (UI 연결)
    public void OnAttackButtonClick()
    {
        if (CurrentState != EBattleState.PlayerTurn) return;
        StartCoroutine(PlayerTurnSequence());
    }

    // 필살기 버튼 (UI 연결)
    public void OnUltimateButtonClick()
    {
        if (CurrentState != EBattleState.PlayerTurn || currentGauge < MAX_GAUGE) return;
        StartCoroutine(UltimateSequence());
    }

    // --------------- private ------------------

    // 1. 초기 타임라인 설정 (P:P:E 비율 유지용 최소 데이터)
    private void InitializeTimeline()
    {
        turnTimeline.Clear();

        // 플레이어 2개, 적 1개 데이터를 기본 세트로 가집니다.
        turnTimeline.Add(new BattleUnitOrder { unitType = ECharacterType.Player, unitName = "Player", actionValue = 100f });
        turnTimeline.Add(new BattleUnitOrder { unitType = ECharacterType.Player, unitName = "Player", actionValue = 105f });
        turnTimeline.Add(new BattleUnitOrder { unitType = ECharacterType.Enemy, unitName = "Enemy", actionValue = 120f });

        UpdateTimelineUI();
    }

    // 2. 다음 턴 결정 (에러 발생했던 함수)
    private void DetermineNextTurn()
    {
        if (turnTimeline.Count == 0) return;

        // 리스트의 첫 번째 유닛 확인
        BattleUnitOrder nextUnit = turnTimeline[0];

        if (nextUnit.unitType == ECharacterType.Player)
        {
            CurrentState = EBattleState.PlayerTurn;
            Debug.Log("<color=green>[Turn]</color> 플레이어 차례");
        }
        else
        {
            CurrentState = EBattleState.EnemyTurn;
            Debug.Log("<color=red>[Turn]</color> 적 차례 (자동 공격)");
            StartCoroutine(EnemyTurnSequence());
        }
    }

    // 3. UI 갱신 (8개 이상 보이도록 순환)
    private void UpdateTimelineUI()
    {
        foreach (GameObject icon in activeTimelineIcons) Destroy(icon);
        activeTimelineIcons.Clear();

        if (turnTimeline.Count == 0) return;

        List<BattleUnitOrder> displayList = new List<BattleUnitOrder>();
        int currentIndex = 0;

        // 데이터가 3개라도 UI에는 순환시켜서 8개를 채움
        while (displayList.Count < 8)
        {
            displayList.Add(turnTimeline[currentIndex % turnTimeline.Count]);
            currentIndex++;
        }

        foreach (BattleUnitOrder unit in displayList)
        {
            GameObject prefab = unit.unitType == ECharacterType.Player ? playerIconPrefab : enemyIconPrefab;
            if (prefab != null)
            {
                GameObject iconObj = Instantiate(prefab, timelineContainer);
                activeTimelineIcons.Add(iconObj);
            }
        }
    }

    // 4. 플레이어 공격 시퀀스 (게이지 충전 포함)
    private IEnumerator PlayerTurnSequence()
    {
        CurrentState = EBattleState.Busy;

        yield return StartCoroutine(player.AttackSequence());

        // 게이지 증가 로직
        currentGauge = Mathf.Min(currentGauge + GAUGE_PER_ATTACK, MAX_GAUGE);
        UpdateUltimateUI();

        if (enemy.CurrentHp > 0)
        {
            CycleFinishedUnit(); // 턴 넘기기
            yield return new WaitForSeconds(1.0f);
            DetermineNextTurn();
        }
        else
        {
            CurrentState = EBattleState.Won;
        }
    }

    // 5. 필살기 시퀀스 (게이지 소모 및 강력한 공격)
    private IEnumerator UltimateSequence()
    {
        CurrentState = EBattleState.Busy;

        currentGauge = 0f;
        UpdateUltimateUI();

        // 필살기 연출: 3번 연속 공격
        for (int i = 0; i < 3; i++)
        {
            yield return StartCoroutine(player.AttackSequence());
        }

        if (enemy.CurrentHp > 0)
        {
            CycleFinishedUnit();
            yield return new WaitForSeconds(1.0f);
            DetermineNextTurn();
        }
        else
        {
            CurrentState = EBattleState.Won;
        }
    }

    // 6. 적 공격 시퀀스 (자동)
    private IEnumerator EnemyTurnSequence()
    {
        CurrentState = EBattleState.Busy;

        // 적이 플레이어를 공격
        yield return StartCoroutine(enemy.AttackSequence(player.transform));

        if (player.CurrentHp > 0)
        {
            CycleFinishedUnit();
            yield return new WaitForSeconds(1.0f);
            DetermineNextTurn();
        }
        else
        {
            CurrentState = EBattleState.Lost;
        }
    }

    // 7. 유닛 턴 순환 (맨 앞을 맨 뒤로)
    private void CycleFinishedUnit()
    {
        if (turnTimeline.Count > 0)
        {
            BattleUnitOrder finishedUnit = turnTimeline[0];
            turnTimeline.RemoveAt(0);
            turnTimeline.Add(finishedUnit);
            UpdateTimelineUI();
        }
    }

    // 8. 필살기 UI 업데이트
    private void UpdateUltimateUI()
    {
        if (ultimateGaugeImage != null)
            ultimateGaugeImage.fillAmount = currentGauge / MAX_GAUGE;

        if (ultimateButton != null)
            ultimateButton.interactable = (currentGauge >= MAX_GAUGE);
    }
}