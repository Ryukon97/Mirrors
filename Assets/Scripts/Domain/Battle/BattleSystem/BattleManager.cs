using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;


public class BattleManager : MonoBehaviour
{
    // --------------- 싱글톤 인스턴스 --------------
    public static BattleManager Instance { get; private set; }

    // --------------- 변수 영역 --------------
    [Header("Characters")]
    public BattleCharacter player;
    public List<Enemy> enemies; // 인스펙터에서 적 3명을 넣어주세요.

    [Header("UI References")]
    public Transform timelineContainer; // Vertical Layout Group 패널
    public GameObject playerIconPrefab;
    public GameObject enemyIconPrefab;

    [Header("Ultimate System")]
    public Button ultimateButton;      // 필살기 버튼
    public Image ultimateGaugeImage;   // Filled 타입 게이지 이미지
    private float currentGauge = 0f;
    private const float MAX_GAUGE = 100f;
    private const float GAUGE_PER_ATTACK = 25f;

    [Header("QTE System")]
    public QTEManager qteManager;

    private List<BattleUnitOrder> turnTimeline = new List<BattleUnitOrder>();
    private List<GameObject> activeTimelineIcons = new List<GameObject>();

    public EBattleState CurrentState { get; private set; }
    private Enemy currentTarget;

    // --------------- Unity Life Cycle --------------
    private void Awake()
    {
        // 싱글톤 초기화
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        CurrentState = EBattleState.Start;
        UpdateUltimateUI();
        InitializeTimeline(); // [P, P, E1, E2, E3] 구조 생성
        DetermineNextTurn();
    }

    // --------------- public APIs --------------

    public void OnAttackButtonClick() // 일반 공격 버튼 연결
    {
        if (CurrentState != EBattleState.PlayerTurn) return;
        StartCoroutine(PlayerTurnSequence());
    }

    public void OnUltimateButtonClick() // 필살기 버튼 연결
    {
        if (CurrentState != EBattleState.PlayerTurn || currentGauge < MAX_GAUGE) return;
        StartCoroutine(UltimateSequence());
    }

    // 적이 사망(Destroy)할 때 Enemy 스크립트에서 호출
    public void RemoveEnemy(Enemy deadEnemy)
    {
        if (enemies.Contains(deadEnemy)) enemies.Remove(deadEnemy);

        // 타임라인에서 해당 적 데이터 모두 삭제
        turnTimeline.RemoveAll(unit => unit.enemyReference == deadEnemy);

        // 죽은 적이 현재 타겟이었다면 타겟 초기화 및 자동 변경
        if (currentTarget == deadEnemy)
        {
            currentTarget = null;
            AutoTargetNext();
        }

        UpdateTimelineUI();

        // 모든 적 처치 시 승리 판정
        if (enemies.Count == 0)
        {
            CurrentState = EBattleState.Won;
            Debug.Log("<color=yellow>모든 적을 처치했습니다! 승리!</color>");
        }
    }

    public void SetTarget(Enemy target)
    {
        if (target == null || target.CurrentHp <= 0) return;

        // 기존 타겟 표시 해제
        if (currentTarget != null) currentTarget.SetSelection(false);

        currentTarget = target;
        currentTarget.SetSelection(true);
        Debug.Log($"<color=yellow>[타겟 변경]</color> {target.gameObject.name}을(를) 조준합니다.");
    }

    // --------------- private Logic ------------------

    private void InitializeTimeline()
    {
        turnTimeline.Clear();

        // 플레이어 2턴 추가
        for (int i = 0; i < 2; i++)
        {
            turnTimeline.Add(new BattleUnitOrder { unitType = ECharacterType.Player, unitName = "Player" });
        }

        // 적 3명 각각 추가
        for (int i = 0; i < enemies.Count; i++)
        {
            turnTimeline.Add(new BattleUnitOrder
            {
                unitType = ECharacterType.Enemy,
                unitName = "Enemy_" + i,
                enemyReference = enemies[i]
            });
        }
        UpdateTimelineUI();
        AutoTargetNext();
    }

    private void DetermineNextTurn()
    {
        if (turnTimeline.Count == 0 || CurrentState == EBattleState.Won) return;

        BattleUnitOrder nextUnit = turnTimeline[0];

        if (nextUnit.unitType == ECharacterType.Player)
        {
            CurrentState = EBattleState.PlayerTurn;
            Debug.Log("<color=green>[Turn]</color> 플레이어 차례입니다.");
        }
        else
        {
            // 적이 이미 죽어있을 경우 스킵 로직
            if (nextUnit.enemyReference == null)
            {
                CycleFinishedUnit();
                DetermineNextTurn();
                return;
            }

            CurrentState = EBattleState.EnemyTurn;
            StartCoroutine(EnemyTurnSequence());
        }
    }

    private void UpdateTimelineUI()
    {
        foreach (GameObject icon in activeTimelineIcons) Destroy(icon);
        activeTimelineIcons.Clear();

        if (turnTimeline.Count == 0) return;

        List<BattleUnitOrder> displayList = new List<BattleUnitOrder>();
        int currentIndex = 0;

        // 항상 8개의 아이콘을 보여줌 (순환 데이터)
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

    private IEnumerator PlayerTurnSequence()
    {
        CurrentState = EBattleState.Busy;

        if (currentTarget == null || currentTarget.CurrentHp <= 0)
        {
            AutoTargetNext();
        }

        if (currentTarget != null)
        {
            yield return StartCoroutine(player.AttackSequence(currentTarget));
        }

        currentGauge = Mathf.Min(currentGauge + GAUGE_PER_ATTACK, MAX_GAUGE);
        UpdateUltimateUI();

        CycleFinishedUnit();
        yield return new WaitForSeconds(0.5f);
        DetermineNextTurn();
    }

    private IEnumerator UltimateSequence()
    {
        CurrentState = EBattleState.Busy;
        currentGauge = 0f;
        UpdateUltimateUI();

        Debug.Log("<color=yellow>!!! 필살기 발동: 전체 공격 !!!</color>");

        // 공격 시점의 적 리스트 복사
        List<Enemy> targets = new List<Enemy>(enemies);

        foreach (var target in targets)
        {
            if (target != null && target.CurrentHp > 0)
            {
                yield return StartCoroutine(player.AttackSequence(target));
            }
        }

        if (enemies.Count > 0)
        {
            CycleFinishedUnit();
            yield return new WaitForSeconds(0.5f);
            DetermineNextTurn();
        }
        else
        {
            CurrentState = EBattleState.Won;
        }
    }

    private IEnumerator EnemyTurnSequence()
    {
        CurrentState = EBattleState.Busy;

        BattleUnitOrder currentUnit = turnTimeline[0];
        Enemy actingEnemy = currentUnit.enemyReference;

        if (actingEnemy != null && actingEnemy.CurrentHp > 0)
        {
            // 적의 공격 시퀀스 실행 (QTE 매니저 전달)
            yield return StartCoroutine(actingEnemy.AttackSequence(player.transform, qteManager.StartQTE));
        }

        if (player.CurrentHp <= 0)
        {
            CurrentState = EBattleState.Lost;
            Debug.Log("<color=red>패배: 플레이어 사망</color>");
        }
        else
        {
            CycleFinishedUnit();
            yield return new WaitForSeconds(0.5f);
            DetermineNextTurn();
        }
    }

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

    private void UpdateUltimateUI()
    {
        if (ultimateGaugeImage != null)
            ultimateGaugeImage.fillAmount = currentGauge / MAX_GAUGE;

        if (ultimateButton != null)
            ultimateButton.interactable = (currentGauge >= MAX_GAUGE);
    }

    private void AutoTargetNext()
    {
        currentTarget = enemies.Find(e => e != null && e.CurrentHp > 0);
        if (currentTarget != null)
        {
            currentTarget.SetSelection(true);
        }
    }
}