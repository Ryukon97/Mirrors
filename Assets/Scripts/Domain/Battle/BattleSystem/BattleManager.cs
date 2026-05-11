using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }

    [Header("Characters")]
    public BattleCharacter player;
    public List<Enemy> enemies;

    [Header("UI References")]
    public Transform timelineContainer;
    public GameObject playerIconPrefab;
    public GameObject enemyIconPrefab;

    [Header("Ultimate System")]
    public Button ultimateButton;
    public Image ultimateGaugeImage;
    private float currentGauge = 0f;
    private const float MAX_GAUGE = 100f;
    private const float GAUGE_PER_ATTACK = 25f;

    [Header("Mana System")]
    public Image manaBarImage;         // 마나 바 UI (선택사항)
    public Button skillButton;         // 스킬 버튼
    private int currentMana = 50;      // 현재 마나
    private const int MAX_MANA = 50;   // 최대 마나
    private const int SKILL_COST = 10; // 스킬 소모량
    private const int MANA_REGAIN = 10;// 평타 회복량

    [Header("QTE System")]
    public QTEManager qteManager;

    [Header("Boss Settings")]
    public GameObject bossPrefab;      // 소환할 보스 프리팹
    public Transform bossSpawnPoint;   // 보스가 나타날 위치
    private bool isBossSpawned = false; // 보스가 이미 소환되었는지 체크

    private List<BattleUnitOrder> turnTimeline = new List<BattleUnitOrder>();
    private List<GameObject> activeTimelineIcons = new List<GameObject>();

    public EBattleState CurrentState { get; private set; }
    private Enemy currentTarget;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        CurrentState = EBattleState.Start;
        UpdateUltimateUI();
        UpdateManaUI(); // 마나 UI 초기화
        InitializeTimeline();
        DetermineNextTurn();
    }

    // --------------- public APIs (Buttons) --------------

    public void OnAttackButtonClick()
    {
        if (CurrentState != EBattleState.PlayerTurn) return;
        StartCoroutine(PlayerTurnSequence());
    }

    public void OnSkillButtonClick()
    {
        // 마나가 부족하면 발동 불가
        if (CurrentState != EBattleState.PlayerTurn || currentMana < SKILL_COST) return;
        StartCoroutine(SkillSequence());
    }

    public void OnUltimateButtonClick()
    {
        if (CurrentState != EBattleState.PlayerTurn || currentGauge < MAX_GAUGE) return;
        StartCoroutine(UltimateThreeHitSequence());
    }

    // --------------- 핵심 전투 로직 ------------------

    private IEnumerator PlayerTurnSequence()
    {
        CurrentState = EBattleState.Busy;

        // 에러 방지: 타겟을 한 번 더 체크
        CheckTargetHealth();

        if (player != null && currentTarget != null)
        {
            yield return StartCoroutine(player.AttackSequence(currentTarget));
            currentTarget.TakeDamage(25);

            // 평타 시 마나 회복
            GainMana(MANA_REGAIN);
        }

        AddUltimateGauge(GAUGE_PER_ATTACK * 0.5f);
        yield return StartCoroutine(FinishPlayerTurn());
    }

    private IEnumerator SkillSequence()
    {
        CurrentState = EBattleState.Busy;

        // 마나 소모
        currentMana -= SKILL_COST;
        UpdateManaUI();

        Debug.Log($"<color=cyan>광역 스킬! 남은 마나: {currentMana}</color>");

        List<Enemy> targets = new List<Enemy>(enemies);
        foreach (var target in targets)
        {
            if (target != null && target.CurrentHp > 0)
            {
                yield return StartCoroutine(player.AttackSequence(target));
                target.TakeDamage(30);
            }
        }

        AddUltimateGauge(GAUGE_PER_ATTACK);
        yield return StartCoroutine(FinishPlayerTurn());
    }

    private IEnumerator UltimateThreeHitSequence()
    {
        CurrentState = EBattleState.Busy;
        currentGauge = 0f;
        UpdateUltimateUI();

        for (int i = 0; i < 3; i++)
        {
            CheckTargetHealth();
            if (currentTarget == null || player == null) break;

            yield return StartCoroutine(player.AttackSequence(currentTarget));
            currentTarget.TakeDamage(40);
            yield return new WaitForSeconds(0.2f);
        }

        yield return StartCoroutine(FinishPlayerTurn());
    }

    // --------------- 보조 및 관리 로직 ------------------

    private void GainMana(int amount)
    {
        currentMana = Mathf.Min(currentMana + amount, MAX_MANA);
        UpdateManaUI();
    }

    private void UpdateManaUI()
    {
        if (manaBarImage != null)
            manaBarImage.fillAmount = (float)currentMana / MAX_MANA;

        // 스킬 버튼 활성화/비활성화
        if (skillButton != null)
            skillButton.interactable = (currentMana >= SKILL_COST);
    }

    private IEnumerator FinishPlayerTurn()
    {
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

    // NullReferenceException 방지를 위한 안전 체크 함수
    private void CheckTargetHealth()
    {
        // currentTarget이 파괴되었거나(null) 체력이 없으면 다음 타겟 탐색
        if (currentTarget == null || currentTarget.gameObject == null || currentTarget.CurrentHp <= 0)
        {
            AutoTargetNext();
        }
    }

    public void RemoveEnemy(Enemy deadEnemy)
    {
        if (enemies.Contains(deadEnemy)) enemies.Remove(deadEnemy);
        turnTimeline.RemoveAll(unit => unit.enemyReference == deadEnemy);

        if (currentTarget == deadEnemy)
        {
            currentTarget = null;
        }

        UpdateTimelineUI();

        if (enemies.Count == 0)
        {
            // 보스가 아직 안 나왔다면 소환 시퀀스 실행
            if (bossPrefab != null && !isBossSpawned)
            {
                StartCoroutine(SpawnBossSequence());
            }
            // 보스까지 죽었거나, 소환할 보스가 아예 없을 때만 승리
            else if (isBossSpawned || bossPrefab == null)
            {
                // [중요] 상태 변경 전 로그 확인
                Debug.Log("<color=yellow>모든 적 처치! 승리 상태로 전환합니다.</color>");
                StopAllCoroutines();
                // CurrentState가 private set인 경우 내부에서만 변경 가능하므로 아래처럼 작성
                var field = typeof(BattleManager).GetProperty("CurrentState");
                field.SetValue(this, EBattleState.Won);
            }
        }
        else
        {
            // 적이 남아있다면 다음 타겟 자동 설정
            AutoTargetNext();
        }
    }

    private void AutoTargetNext()
    {
        if (enemies == null || enemies.Count == 0)
        {
            currentTarget = null;
            return;
        }

        // 살아있는 적 중 첫 번째 선택
        currentTarget = enemies.Find(e => e != null && e.gameObject != null && e.CurrentHp > 0);

        if (currentTarget != null)
        {
            currentTarget.SetSelection(true);
        }
    }

    // (이하 InitializeTimeline, DetermineNextTurn, CycleFinishedUnit, UpdateUltimateUI, UpdateTimelineUI는 기존과 동일)
    private void DetermineNextTurn()
    {
        if (turnTimeline.Count == 0 || CurrentState == EBattleState.Won) return;
        BattleUnitOrder nextUnit = turnTimeline[0];
        if (nextUnit.unitType == ECharacterType.Player) CurrentState = EBattleState.PlayerTurn;
        else
        {
            if (nextUnit.enemyReference == null) { CycleFinishedUnit(); DetermineNextTurn(); return; }
            CurrentState = EBattleState.EnemyTurn;
            StartCoroutine(EnemyTurnSequence());
        }
    }

    private IEnumerator EnemyTurnSequence()
    {
        CurrentState = EBattleState.Busy;

        BattleUnitOrder currentUnit = turnTimeline[0];
        Enemy actingEnemy = currentUnit.enemyReference;

        if (actingEnemy != null && actingEnemy.CurrentHp > 0)
        {
            // 현재 적이 보스인지 체크
            BossEnemy boss = actingEnemy as BossEnemy;

            if (boss != null && boss.ShouldTriggerEvent())
            {
                // 보스 전용 기믹 공격 실행
                yield return StartCoroutine(boss.DeceptiveQTESequence(player, qteManager.StartQTE));
            }
            else
            {
                // 일반 공격
                yield return StartCoroutine(actingEnemy.AttackSequence(player.transform, qteManager.StartQTE));
            }
        }

        // 이후 사망 체크 및 턴 종료 로직 (기존과 동일)
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

    private void InitializeTimeline()
    {
        turnTimeline.Clear();
        for (int i = 0; i < 2; i++) turnTimeline.Add(new BattleUnitOrder { unitType = ECharacterType.Player, unitName = "Player" });
        foreach (var e in enemies) turnTimeline.Add(new BattleUnitOrder { unitType = ECharacterType.Enemy, unitName = e.name, enemyReference = e });
        UpdateTimelineUI();
        AutoTargetNext();
    }

    private void CycleFinishedUnit()
    {
        if (turnTimeline.Count > 0) { BattleUnitOrder finishedUnit = turnTimeline[0]; turnTimeline.RemoveAt(0); turnTimeline.Add(finishedUnit); UpdateTimelineUI(); }
    }

    private void AddUltimateGauge(float amount) { currentGauge = Mathf.Min(currentGauge + amount, MAX_GAUGE); UpdateUltimateUI(); }

    private void UpdateUltimateUI()
    {
        if (ultimateGaugeImage != null) ultimateGaugeImage.fillAmount = currentGauge / MAX_GAUGE;
        if (ultimateButton != null) ultimateButton.interactable = (currentGauge >= MAX_GAUGE);
    }

    private void UpdateTimelineUI()
    {
        foreach (GameObject icon in activeTimelineIcons) Destroy(icon);
        activeTimelineIcons.Clear();
        for (int i = 0; i < 8; i++)
        {
            BattleUnitOrder unit = turnTimeline[i % turnTimeline.Count];
            GameObject prefab = unit.unitType == ECharacterType.Player ? playerIconPrefab : enemyIconPrefab;
            if (prefab != null) activeTimelineIcons.Add(Instantiate(prefab, timelineContainer));
        }
    }

    public void SetTarget(Enemy target)
    {
        if (target == null || target.CurrentHp <= 0) return;
        if (currentTarget != null) currentTarget.SetSelection(false);
        currentTarget = target;
        currentTarget.SetSelection(true);
    }
    private IEnumerator SpawnBossSequence()
    {
        // 소환 시작 시 상태를 Busy로 고정하여 승리 판정 간섭 차단
        var stateField = typeof(BattleManager).GetProperty("CurrentState");
        stateField.SetValue(this, EBattleState.Busy);

        isBossSpawned = true;

        Debug.Log("<color=red>모든 적을 처치하자 거대한 기운이 나타납니다...</color>");
        yield return new WaitForSeconds(1.5f);

        if (bossPrefab != null)
        {
            GameObject bossObj = Instantiate(bossPrefab, bossSpawnPoint.position, bossSpawnPoint.rotation);
            BossEnemy boss = bossObj.GetComponent<BossEnemy>();

            if (boss != null)
            {
                enemies.Clear();
                enemies.Add(boss);

                turnTimeline.Clear();
                turnTimeline.Add(new BattleUnitOrder { unitType = ECharacterType.Player, unitName = "Player" });
                turnTimeline.Add(new BattleUnitOrder { unitType = ECharacterType.Enemy, unitName = "BOSS", enemyReference = boss });

                UpdateTimelineUI();
                SetTarget(boss); // 여기서 currentTarget이 확실히 잡힘
            }
        }

        yield return new WaitForSeconds(1.0f);

        Debug.Log("전투 재개 - 플레이어 턴으로 강제 복귀");

        // DetermineNextTurn을 부르기 전에 상태를 확실히 PlayerTurn으로 밀어넣음
        stateField.SetValue(this, EBattleState.PlayerTurn);
        DetermineNextTurn();
    }
}
