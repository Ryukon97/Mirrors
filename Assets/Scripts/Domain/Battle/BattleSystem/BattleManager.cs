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

    // --------------- private Logic ------------------

    // 1. 타임라인 초기 데이터 구성 (P:P:E 비율)
    private void InitializeTimeline()
    {
        turnTimeline.Clear();

        // 플레이어 2턴
        for (int i = 0; i < 2; i++)
        {
            turnTimeline.Add(new BattleUnitOrder { unitType = ECharacterType.Player, unitName = "Player" });
        }

        // 적 3명 각각 추가 (각자의 정보를 담음)
        for (int i = 0; i < enemies.Count; i++)
        {
            turnTimeline.Add(new BattleUnitOrder
            {
                unitType = ECharacterType.Enemy,
                unitName = "Enemy_" + i,
                enemyReference = enemies[i] // 각 적 오브젝트를 매칭
            });
        }
        UpdateTimelineUI();
        AutoTargetNext();
    }

    // 2. 다음 턴 결정 및 자동 실행
    private void DetermineNextTurn()
    {
        if (turnTimeline.Count == 0) return;

        BattleUnitOrder nextUnit = turnTimeline[0];

        if (nextUnit.unitType == ECharacterType.Player)
        {
            CurrentState = EBattleState.PlayerTurn;
            Debug.Log("<color=green>[Turn]</color> 플레이어 차례입니다.");
        }
        else
        {
            CurrentState = EBattleState.EnemyTurn;
            StartCoroutine(EnemyTurnSequence());
        }
    }

    // 3. 타임라인 UI 업데이트 (8개 순환 표시)
    private void UpdateTimelineUI()
    {
        foreach (GameObject icon in activeTimelineIcons) Destroy(icon);
        activeTimelineIcons.Clear();

        if (turnTimeline.Count == 0) return;

        List<BattleUnitOrder> displayList = new List<BattleUnitOrder>();
        int currentIndex = 0;

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

    // 4. 플레이어 일반 공격
    private IEnumerator PlayerTurnSequence()
    {
        CurrentState = EBattleState.Busy;

        // 타겟이 없거나 죽었다면 자동 타겟팅
        if (currentTarget == null || currentTarget.CurrentHp <= 0)
        {
            AutoTargetNext();
        }

        if (currentTarget != null)
        {
            // [수정] 타겟 정보를 인자로 넘겨줍니다. 
            // 이제 BattleCharacter 내부에서 직접 TakeDamage를 호출하므로 여기서 중복으로 호출하지 마세요.
            yield return StartCoroutine(player.AttackSequence(currentTarget));
        }

        currentGauge = Mathf.Min(currentGauge + GAUGE_PER_ATTACK, MAX_GAUGE);
        UpdateUltimateUI();

        CycleFinishedUnit();
        yield return new WaitForSeconds(0.5f);
        DetermineNextTurn();
    }


    public void RemoveEnemy(Enemy deadEnemy)
    {
        if (enemies.Contains(deadEnemy)) enemies.Remove(deadEnemy);
        turnTimeline.RemoveAll(unit => unit.enemyReference == deadEnemy);

        // 죽은 적이 현재 타겟이었다면 타겟 초기화 및 자동 변경
        if (currentTarget == deadEnemy)
        {
            currentTarget = null;
            AutoTargetNext();
        }

        UpdateTimelineUI();

        if (enemies.Count == 0)
        {
            CurrentState = EBattleState.Won;
            Debug.Log("승리!");
        }
    }

    // [확인] 필살기: 모든 적 전체 공격 로직
    // 5. 필살기 (모든 적 공격)
    private IEnumerator UltimateSequence()
    {
        CurrentState = EBattleState.Busy;
        currentGauge = 0f;
        UpdateUltimateUI();

        Debug.Log("<color=yellow>!!! 필살기 발동: 전체 공격 !!!</color>");

        // 공격 시점의 적 리스트를 복사하여 사용
        List<Enemy> targets = new List<Enemy>(enemies);

        foreach (var target in targets)
        {
            // 타겟이 아직 파괴되지 않았고 살아있는지 확인
            if (target != null && target.CurrentHp > 0)
            {
                // [수정된 부분] 타겟(target)을 인자로 전달합니다.
                yield return StartCoroutine(player.AttackSequence(target));

                // 주의: BattleCharacter.AttackSequence 내부에서 TakeDamage를 호출한다면 
                // 여기서 중복으로 호출하지 않도록 주의하세요.
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

    // 6. 적 자동 공격
    private IEnumerator EnemyTurnSequence()
    {
        CurrentState = EBattleState.Busy;

        BattleUnitOrder currentUnit = turnTimeline[0];
        Enemy actingEnemy = currentUnit.enemyReference;

        if (actingEnemy != null && actingEnemy.CurrentHp > 0)
        {
            // 적에게 QTE 시작 함수를 인자로 넘겨줍니다.
            yield return StartCoroutine(actingEnemy.AttackSequence(player.transform, qteManager.StartQTE));
        }

        if (player.CurrentHp <= 0)
        {
            CurrentState = EBattleState.Lost;
        }
        else
        {
            CycleFinishedUnit();
            yield return new WaitForSeconds(0.5f);
            DetermineNextTurn();
        }
    }

    // 7. 데이터 순환 로직
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

    // 8. 게이지 UI 동기화
    private void UpdateUltimateUI()
    {
        if (ultimateGaugeImage != null)
            ultimateGaugeImage.fillAmount = currentGauge / MAX_GAUGE;

        if (ultimateButton != null)
            ultimateButton.interactable = (currentGauge >= MAX_GAUGE);
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

    // 자동 타겟팅 (다음 살아있는 적 찾기)
    private void AutoTargetNext()
    {
        currentTarget = enemies.Find(e => e != null && e.CurrentHp > 0);
        if (currentTarget != null)
        {
            currentTarget.SetSelection(true);
        }
    }
}