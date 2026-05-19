using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;
using TMPro;
public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }

    [Header("Characters")]
    public BattleCharacter player;
    public List<Enemy> enemies = new List<Enemy>();

    [Header("UI References")]
    public Transform timelineContainer;
    public GameObject playerIconPrefab;
    public GameObject enemyIconPrefab;

    // ---------------- [추가 변수 영역: 우측 상단 데미지 로그 UI] ----------------
    [Header("Damage Log UI Settings")]
    [Tooltip("이번 턴에 가한 총 데미지를 표기할 텍스트 컴포넌트")]
    public TextMeshProUGUI totalDamageText;

    [Tooltip("우측 상단에 최근 피격/타격 로그들을 표기할 텍스트 컴포넌트")]
    public TextMeshProUGUI damageLogText;

    private int totalDamage = 0;
    private List<string> damageLogs = new List<string>();
    private const int MAX_LOG_COUNT = 5; // 화면에 최대로 띄울 실시간 로그 줄 수
    // ----------------------------------------------------------------------------

    [Header("Ultimate System")]
    public Button ultimateButton;
    public Image ultimateGaugeImage;
    private float currentGauge = 0f;
    private const float MAX_GAUGE = 100f;
    private const float GAUGE_PER_ATTACK = 25f;

    [Header("Mana System")]
    public Image manaBarImage;
    public Button skillButton;
    private int currentMana = 50;
    private const int MAX_MANA = 50;
    private const int SKILL_COST = 10;
    private const int MANA_REGAIN = 10;

    [Header("QTE System")]
    public QTEManager qteManager;

    [Header("Boss Settings")]
    public GameObject bossPrefab;
    public Transform bossSpawnPoint;
    public bool isBossSpawned = false;
    private bool isVictoryLocked = false;

    [Header("Gimmick Settings")]
    private int totalTurnCount = 0;

    private List<BattleUnitOrder> turnTimeline = new List<BattleUnitOrder>();
    private List<GameObject> activeTimelineIcons = new List<GameObject>();

    public EBattleState CurrentState { get; private set; }
    private Enemy currentTarget;

    public bool CanFinishBattle => isBossSpawned && enemies.Count == 0 && !isVictoryLocked;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        CurrentState = EBattleState.Start;
        UpdateUltimateUI();
        UpdateManaUI();
        UpdateDamageUI(); // [추가] 시작할 때 데미지 관련 텍스트 UI 초기 정렬
        InitializeTimeline();
        DetermineNextTurn();
    }

    // --------------- public APIs (Buttons) --------------

    public void OnAttackButtonClick()
    {
        if (Time.timeScale == 0f) return;
        if (CurrentState != EBattleState.PlayerTurn) return;
        ResetTurnDamage();
        StartCoroutine(PlayerTurnSequence());
    }

    public void OnSkillButtonClick()
    {
        if (Time.timeScale == 0f) return;
        if (CurrentState != EBattleState.PlayerTurn || currentMana < SKILL_COST) return;
        ResetTurnDamage();
        StartCoroutine(ExecuteFullAOESkill());
    }

    public void OnUltimateButtonClick()
    {
        if (Time.timeScale == 0f) return;
        if (CurrentState != EBattleState.PlayerTurn || currentGauge < MAX_GAUGE) return;
        ResetTurnDamage();
        StartCoroutine(UltimateThreeHitSequence());
    }

    public void OnWaitButtonClicked()
    {
        if (Time.timeScale == 0f) return;
        if (CurrentState != EBattleState.PlayerTurn) return;
        ResetTurnDamage();
        StartCoroutine(WaitTurnSequence());
    }

    // --------------- 핵심 전투 흐름 제어 ------------------

    public List<Enemy> GetEnemies()
    {
        if (isBossSpawned) return new List<Enemy>(enemies);
        return new List<Enemy>(FindObjectsByType<Enemy>(FindObjectsInactive.Exclude));
    }

    private IEnumerator PlayerTurnSequence()
    {
        CurrentState = EBattleState.Busy;
        CheckTargetHealth();

        if (player != null && currentTarget != null)
        {
            yield return StartCoroutine(player.AttackSequence(currentTarget, "Normal"));

            if (currentTarget != null)
            {
                BossEnemy boss = currentTarget as BossEnemy;
                if (boss != null && boss.isCounterMode)
                {
                    yield return StartCoroutine(boss.ExecuteCounterSequence(player.transform));
                }
                else
                {
                    currentTarget.TakeDamage(player.NormalAttackDamage);
                }
            }
            GainMana(MANA_REGAIN);
        }

        AddUltimateGauge(GAUGE_PER_ATTACK * 0.5f);
        yield return StartCoroutine(FinishPlayerTurn());
    }

    private IEnumerator ExecuteFullAOESkill()
    {
        CurrentState = EBattleState.Busy;
        currentMana -= SKILL_COST;
        UpdateManaUI();

        List<Enemy> targets = GetEnemies();
        yield return StartCoroutine(player.AttackSequence(targets.Count > 0 ? targets[0] : null, "Skill"));

        foreach (var e in targets)
        {
            if (e == null || e.gameObject == null || e.CurrentHp <= 0) continue;

            BossEnemy boss = e as BossEnemy;
            if (boss != null && boss.isCounterMode)
            {
                yield return StartCoroutine(boss.ExecuteCounterSequence(player.transform));
            }
            else
            {
                e.TakeDamage(player.SkillDamage);
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

            yield return StartCoroutine(player.AttackSequence(currentTarget, "Ultimate"));

            if (currentTarget != null)
            {
                currentTarget.TakeDamage(player.UltimateDamage);
            }

            yield return new WaitForSeconds(0.2f);
        }

        yield return StartCoroutine(FinishPlayerTurn());
    }

    private IEnumerator WaitTurnSequence()
    {
        CurrentState = EBattleState.Busy;
        Debug.Log("<color=yellow>대기: 모든 적의 반격 자세를 파훼하고 재정비합니다.</color>");

        foreach (var e in enemies)
        {
            if (e is BossEnemy boss) boss.DisableCounterMode();
        }

        int healAmount = Mathf.RoundToInt(player.MaxHp * 0.1f);
        player.Heal(healAmount);
        currentMana = Mathf.Min(MAX_MANA, currentMana + 5);
        UpdateManaUI();

        yield return new WaitForSeconds(0.5f);
        yield return StartCoroutine(FinishPlayerTurn());
    }

    private void GainMana(int amount)
    {
        currentMana = Mathf.Min(currentMana + amount, MAX_MANA);
        UpdateManaUI();
    }

    private void UpdateManaUI()
    {
        if (manaBarImage != null)
            manaBarImage.fillAmount = (float)currentMana / MAX_MANA;

        if (skillButton != null)
            skillButton.interactable = (currentMana >= SKILL_COST);
    }

    private IEnumerator FinishPlayerTurn()
    {
        totalTurnCount++;
        if (totalTurnCount % 5 == 0) yield return StartCoroutine(BombardmentSequence());

        if (enemies.Count > 0)
        {
            CycleFinishedUnit();
            yield return new WaitForSeconds(0.5f);
            DetermineNextTurn();
        }
        else
        {
            if (bossPrefab != null && !isBossSpawned)
            {
                isBossSpawned = true;
                isVictoryLocked = true;
                CurrentState = EBattleState.Busy;

                // 보스 생성 완료 시점까지 완벽하게 스레드 홀딩
                yield return StartCoroutine(BossEnemy.SpawnBossSetup(bossPrefab, bossSpawnPoint, this));

                isVictoryLocked = false;

                // 보스 턴 세팅이 완벽히 끝난 후 타임라인의 첫 주자(플레이어) 턴 개시
                DetermineNextTurn();
            }
            else if (IsBossActuallyDead())
            {
                SetBattleState(EBattleState.Won);
            }
        }
    }

    private IEnumerator BombardmentSequence()
    {
        Debug.Log("<color=red> 경고: 지원 폭격! </color>");
        yield return new WaitForSeconds(1.0f);
        player.TakeDamage(35);
        yield return new WaitForSeconds(0.5f);
    }

    private void CheckTargetHealth()
    {
        if (currentTarget == null || currentTarget.gameObject == null || currentTarget.CurrentHp <= 0)
        {
            AutoTargetNext();
        }
    }

    public void RemoveEnemy(Enemy deadEnemy)
    {
        if (enemies.Contains(deadEnemy)) enemies.Remove(deadEnemy);
        turnTimeline.RemoveAll(unit => unit.enemyReference == deadEnemy);
        if (currentTarget == deadEnemy) currentTarget = null;

        UpdateTimelineUI();

        if (enemies.Count == 0)
        {
            if (bossPrefab != null && !isBossSpawned)
            {
                isBossSpawned = true;
                isVictoryLocked = true;
                CurrentState = EBattleState.Busy;
                StartCoroutine(SpawnBossFromRemoveSetup());
            }
            else if (IsBossActuallyDead())
            {
                StopAllCoroutines();
                SetBattleState(EBattleState.Won);
            }
        }
        else
        {
            AutoTargetNext();
        }
    }

    private IEnumerator SpawnBossFromRemoveSetup()
    {
        yield return StartCoroutine(BossEnemy.SpawnBossSetup(bossPrefab, bossSpawnPoint, this));
        isVictoryLocked = false;
        DetermineNextTurn();
    }

    private void AutoTargetNext()
    {
        if (enemies == null || enemies.Count == 0) { currentTarget = null; return; }
        currentTarget = enemies.Find(e => e != null && e.gameObject != null && e.CurrentHp > 0);
        if (currentTarget != null) currentTarget.SetSelection(true);
    }

    private void DetermineNextTurn()
    {
        if (turnTimeline.Count == 0 || CurrentState == EBattleState.Won || CurrentState == EBattleState.Lost) return;

        BattleUnitOrder nextUnit = turnTimeline[0];
        if (nextUnit.unitType == ECharacterType.Player)
        {
            CurrentState = EBattleState.PlayerTurn;
            Debug.Log("<color=green>[Turn] 플레이어 차례입니다. UI 활성화 완료.</color>");
        }
        else
        {
            if (nextUnit.enemyReference == null || nextUnit.enemyReference.gameObject == null)
            {
                CycleFinishedUnit();
                DetermineNextTurn();
                return;
            }
            CurrentState = EBattleState.EnemyTurn;
            StartCoroutine(EnemyTurnSequence());
        }
    }

    private IEnumerator EnemyTurnSequence()
    {
        CurrentState = EBattleState.Busy;
        BattleUnitOrder currentUnit = turnTimeline[0];
        Enemy actingEnemy = currentUnit.enemyReference;

        if (actingEnemy != null && actingEnemy.gameObject != null && actingEnemy.CurrentHp > 0)
        {
            BossEnemy boss = actingEnemy as BossEnemy;
            if (boss != null)
            {
                if (boss.ShouldTriggerEvent())
                {
                    yield return StartCoroutine(boss.DeceptiveQTESequence(player, qteManager.StartQTE));
                }
                else if (boss.isCounterMode)
                {
                    yield return new WaitForSeconds(0.5f);
                }
                else if (UnityEngine.Random.value <= 0.3f)
                {
                    yield return StartCoroutine(boss.CounterStanceSequence());
                }
                else
                {
                    // 보스가 스스로 연출 코루틴을 작동시킵니다.
                    yield return StartCoroutine(boss.ExecuteMeleeAttackSequence(player.transform, qteManager));
                }
            }
            else
            {
                yield return StartCoroutine(actingEnemy.AttackSequence(player.transform, qteManager.StartQTE));
            }
        }

        if (player.CurrentHp <= 0)
        {
            SetBattleState(EBattleState.Lost);
        }
        else
        {
            // 보스 공격이 완전히 끝나 복귀하면 다음 순서로 턴 사이클을 넘깁니다.
            CycleFinishedUnit();
            yield return new WaitForSeconds(0.5f);
            DetermineNextTurn();
        }
    }

    public void SetupBossTimeline(BossEnemy boss)
    {
        turnTimeline.Clear();

        for (int i = 0; i < 4; i++)
        {
            turnTimeline.Add(new BattleUnitOrder { unitType = ECharacterType.Player, unitName = "Player" });
            turnTimeline.Add(new BattleUnitOrder { unitType = ECharacterType.Enemy, unitName = "BOSS", enemyReference = boss });
        }

        UpdateTimelineUI();
    }

    private void SetBattleState(EBattleState newState)
    {
        if (newState == EBattleState.Won && isVictoryLocked) return;

        var field = typeof(BattleManager).GetProperty("CurrentState");
        if (field != null) field.SetValue(this, newState);
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
        if (turnTimeline.Count > 0)
        {
            BattleUnitOrder finishedUnit = turnTimeline[0];
            turnTimeline.RemoveAt(0);
            turnTimeline.Add(finishedUnit);
            UpdateTimelineUI();
        }
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
        if (turnTimeline.Count == 0) return;

        int displayCount = Mathf.Min(8, turnTimeline.Count);
        for (int i = 0; i < displayCount; i++)
        {
            BattleUnitOrder unit = turnTimeline[i];
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

    public bool IsBossActuallyDead()
    {
        if (bossPrefab != null && !isBossSpawned) return false;
        if (enemies.Count > 0) return false;

        return true;
    }

    // ---------------- [추가 public API: 실시간 타격 데이터를 접수하고 UI 출력 갱신하는 연산 함수] ----------------
    /// <summary>
    /// 외부(Enemy.cs 내부)에서 데미지를 가했을 때 호출하여 실시간 로그를 적재하는 인터페이스
    /// </summary>
    public void LogDamage(string targetName, int damageAmount)
    {
        // 1. 가해진 순수 데미지량을 누적 데이터에 추가
        totalDamage += damageAmount;

        // 2. 새로운 출력용 로그 텍스트 라인 생성 (리치 텍스트 컬러 코드로 화려하게 세팅)
        string newLogLine = $"[{targetName}]에게 <color=#FF3B30>-{damageAmount}</color> 피해 기록!";
        damageLogs.Add(newLogLine);

        // 3. 로그가 설정해놓은 개수 한계점을 돌파하면 가장 처음 발생했던 오래된 문자열 라인 제거
        if (damageLogs.Count > MAX_LOG_COUNT)
        {
            damageLogs.RemoveAt(0);
        }

        // 4. 화면 UI 컴포넌트에 스트링 밀어넣기 새로고침
        UpdateDamageUI();
    }

    /// <summary>
    /// 축적된 실시간 데미지 가시 데이터를 텍스트 컴포넌트에 포맷팅하여 바인딩
    /// </summary>
    private void UpdateDamageUI()
    {
        if (totalDamageText != null)
        {
            // TOTAL 대신 TURN DAMAGE 또는 이번 타격 데미지라는 명칭으로 변경
            totalDamageText.text = $"TURN DAMAGE: <color=#FFCC00>{totalDamage}</color>";
        }

        if (damageLogText != null)
        {
            damageLogText.text = string.Join("\n", damageLogs);
        }
    }
    /// <summary>
    /// 플레이어가 공격 행동을 시작할 때 이번 턴 누적액을 깨끗하게 비워주는 함수
    /// </summary>
    private void ResetTurnDamage()
    {
        totalDamage = 0;
        UpdateDamageUI();
    }
    // ------------------------------------------------------------------------------------------------------------
}