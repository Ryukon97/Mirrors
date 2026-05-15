using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;

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
        InitializeTimeline();
        DetermineNextTurn();
    }

    // --------------- public APIs (Buttons) --------------

    public void OnAttackButtonClick()
    {
        if (Time.timeScale == 0f) return;
        if (CurrentState != EBattleState.PlayerTurn) return;
        StartCoroutine(PlayerTurnSequence());
    }

    public void OnSkillButtonClick()
    {
        if (Time.timeScale == 0f) return;
        if (CurrentState != EBattleState.PlayerTurn || currentMana < SKILL_COST) return;
        StartCoroutine(ExecuteFullAOESkill());
    }

    public void OnUltimateButtonClick()
    {
        if (Time.timeScale == 0f) return;
        if (CurrentState != EBattleState.PlayerTurn || currentGauge < MAX_GAUGE) return;
        StartCoroutine(UltimateThreeHitSequence());
    }

    public void OnWaitButtonClicked()
    {
        if (Time.timeScale == 0f) return;
        if (CurrentState != EBattleState.PlayerTurn) return;
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

    /// <summary>
    /// [수정 핵심] 보스 전용 타임라인을 1:1 교대 구조로 길게 나열하여 턴 스킵/연속 공격 버그를 완전히 방지합니다.
    /// </summary>
    public void SetupBossTimeline(BossEnemy boss)
    {
        turnTimeline.Clear();

        // 플레이어 -> 보스 -> 플레이어 -> 보스 형태로 순서가 무한 순환되도록 넉넉하게 적재합니다.
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
        // 기본 잡몹 단계 타임라인 초기화
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

        // 타임라인 상단 UI 배치 개수 통제 (최대 8개까지 노출)
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
}