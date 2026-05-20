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

    // ---------------- [복구 및 유지: 우측 상단 토탈 데미지 UI 변수] ----------------
    [Header("Damage UI Settings")]
    [Tooltip("이번 턴에 가한 총 누적 데미지를 표기할 우측 상단 텍스트 컴포넌트")]
    public TextMeshProUGUI totalDamageText;

    private int totalDamage = 0;
    // ----------------------------------------------------------------------------

    // ---------------- [유지: 플로팅 데미지 텍스트 프리랩 등록] ----------------
    [Header("Damage Floating UI Settings")]
    [Tooltip("적 머리 위에 띄울 3D TextMeshPro 기반의 DamageText 프리랩")]
    public GameObject damageTextPrefab;
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
        UpdateDamageUI(); // 시작할 때 토탈 데미지 텍스트 초기 정렬
        InitializeTimeline();
        DetermineNextTurn();
    }

    // --------------- public APIs (Buttons) --------------

    public void OnAttackButtonClick()
    {
        if (Time.timeScale == 0f) return;
        if (CurrentState != EBattleState.PlayerTurn) return;
        ResetTurnDamage(); // 다음 공격 시작 시 이전 턴의 토탈 데미지 리셋
        StartCoroutine(PlayerTurnSequence());
    }

    public void OnSkillButtonClick()
    {
        if (Time.timeScale == 0f) return;
        if (CurrentState != EBattleState.PlayerTurn || currentMana < SKILL_COST) return;
        ResetTurnDamage(); // 다음 공격 시작 시 이전 턴의 토탈 데미지 리셋
        StartCoroutine(ExecuteFullAOESkill());
    }

    public void OnUltimateButtonClick()
    {
        if (Time.timeScale == 0f) return;
        if (CurrentState != EBattleState.PlayerTurn || currentGauge < MAX_GAUGE) return;
        ResetTurnDamage(); // 다음 공격 시작 시 이전 턴의 토탈 데미지 리셋
        StartCoroutine(UltimateThreeHitSequence());
    }

    public void OnWaitButtonClicked()
    {
        if (Time.timeScale == 0f) return;
        if (CurrentState != EBattleState.PlayerTurn) return;
        ResetTurnDamage(); // 대기 버튼 시에도 리셋
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
                yield return StartCoroutine(BossEnemy.SpawnBossSetup(bossPrefab, bossSpawnPoint, this));
                isVictoryLocked = false;
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

    // ---------------- [수정 영역: 토탈 연산 및 플로팅 스폰 통합 처리] ----------------
    /// <summary>
    /// 외부(Enemy.cs)에서 데미지를 입었을 때 호출하여 토탈 누적량을 올리고 적 위에 숫자를 띄웁니다.
    /// </summary>
    public void SpawnDamageText(Vector3 worldPosition, int damageAmount)
    {
        // 1. 이번 턴에 가한 데미지를 실시간 누적시키고 우측 상단 UI 텍스트 새로고침
        totalDamage += damageAmount;
        UpdateDamageUI();

        // 2. 적 머리 위에 플로팅 팝업 텍스트 프리랩 생성
        if (damageTextPrefab == null) return;
        Vector3 spawnPosition = worldPosition + Vector3.up * 2.0f;
        GameObject dmgObj = Instantiate(damageTextPrefab, spawnPosition, Quaternion.identity);

        if (Camera.main != null)
        {
            dmgObj.transform.forward = Camera.main.transform.forward;
        }

        DamageText dmgTextScript = dmgObj.GetComponent<DamageText>();
        if (dmgTextScript != null)
        {
            dmgTextScript.Setup(damageAmount);
        }
    }

    /// <summary>
    /// 이번 턴의 총 누적 데미지를 우측 상단 UI 텍스트 컴포넌트에 반영
    /// </summary>
    private void UpdateDamageUI()
    {
        if (totalDamageText != null)
        {
            totalDamageText.text = $"TURN DAMAGE: <color=#FFCC00>{totalDamage}</color>";
        }
    }

    /// <summary>
    /// 플레이어 공격 행동 시작 시 턴 누적 수치를 0으로 리셋해 주는 함수
    /// </summary>
    private void ResetTurnDamage()
    {
        totalDamage = 0;
        UpdateDamageUI();
    }
    // ----------------------------------------------------------------------------------
}