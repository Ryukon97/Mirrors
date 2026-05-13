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
    private bool isBossSpawned = false;

    [Header("Gimmick Settings")]
    private int totalTurnCount = 0;

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
        UpdateManaUI();
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
        if (CurrentState != EBattleState.PlayerTurn || currentMana < SKILL_COST) return;
        StartCoroutine(ExecuteFullAOESkill());
    }

    public void OnUltimateButtonClick()
    {
        if (CurrentState != EBattleState.PlayerTurn || currentGauge < MAX_GAUGE) return;
        StartCoroutine(UltimateThreeHitSequence());
    }

    public void OnWaitButtonClicked()
    {
        if (CurrentState != EBattleState.PlayerTurn) return;
        StartCoroutine(WaitTurnSequence());
    }

    // --------------- 핵심 전투 로직 ------------------

    public List<Enemy> GetEnemies()
    {
        return new List<Enemy>(FindObjectsByType<Enemy>(FindObjectsInactive.Exclude));
    }

    private IEnumerator PlayerTurnSequence()
    {
        CurrentState = EBattleState.Busy;
        CheckTargetHealth();

        if (player != null && currentTarget != null)
        {
            // [연출] 이동 및 애니메이션 (데미지 로직 중복 방지를 위해 캐릭터 내부 TakeDamage는 삭제 필수)
            yield return StartCoroutine(player.AttackSequence(currentTarget, "Normal"));

            if (currentTarget != null)
            {
                BossEnemy boss = currentTarget as BossEnemy;
                // 반격 모드 체크 후 데미지 처리
                if (boss != null && boss.isCounterMode)
                {
                    boss.ExecuteCounter(player);
                }
                else
                {
                    // [경고 해결] player의 변수를 참조
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

        // 1. 캐릭터 시전 연출 시작
        Coroutine skillRoutine = StartCoroutine(player.AttackSequence(targets.Count > 0 ? targets[0] : null, "Skill"));

        // 2. 캐릭터 코드의 타격 타이밍(0.6초)에 맞춰 데미지 일괄 처리
        yield return new WaitForSeconds(0.6f);

        foreach (var e in targets)
        {
            if (e == null || e.CurrentHp <= 0) continue;

            BossEnemy boss = e as BossEnemy;
            if (boss != null && boss.isCounterMode)
            {
                boss.ExecuteCounter(player);
            }
            else
            {
                // [경고 해결] player의 변수를 참조
                e.TakeDamage(player.SkillDamage);
            }
        }

        // [복구] 스킬 사용 시에도 궁극기 게이지 추가
        AddUltimateGauge(GAUGE_PER_ATTACK);

        yield return skillRoutine;
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

            // 1. 연출 실행 (BattleCharacter는 시각적 효과만 담당)
            yield return StartCoroutine(player.AttackSequence(currentTarget, "Ultimate"));

            // 2. 연출 타이밍에 맞춰 실제 데미지 1회 적용 (중복 방지)
            if (currentTarget != null)
            {
                // [경고 해결] player.UltimateDamage 참조
                currentTarget.TakeDamage(player.UltimateDamage);
            }

            yield return new WaitForSeconds(0.2f);
        }

        yield return StartCoroutine(FinishPlayerTurn());
    }

    private IEnumerator WaitTurnSequence()
    {
        CurrentState = EBattleState.Busy;
        Debug.Log("<color=yellow>대기: 반격 자세 파훼 및 재정비</color>");

        // 1. 모든 보스의 반격 자세 강제 해제 (시각적 효과 포함)
        Enemy[] allActiveEnemies = FindObjectsByType<Enemy>(FindObjectsInactive.Exclude);
        foreach (var e in allActiveEnemies)
        {
            if (e is BossEnemy boss)
            {
                // BossEnemy에 추가한 DisableCounterMode 호출 (색상 복구 포함)
                boss.DisableCounterMode();
            }
        }

        // 2. 체력 10% 회복
        int healAmount = Mathf.RoundToInt(player.MaxHp * 0.1f);
        player.Heal(healAmount);

        // 3. 마나 보너스
        currentMana = Mathf.Min(MAX_MANA, currentMana + 5);
        UpdateManaUI();

        yield return new WaitForSeconds(0.5f);

        yield return StartCoroutine(FinishPlayerTurn());
    }

    // --------------- 보조 및 관리 로직 (기존 유지) ------------------

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
            var field = typeof(BattleManager).GetProperty("CurrentState");
            field.SetValue(this, EBattleState.Won);
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
            if (bossPrefab != null && !isBossSpawned) StartCoroutine(SpawnBossSequence());
            else
            {
                StopAllCoroutines();
                var field = typeof(BattleManager).GetProperty("CurrentState");
                field.SetValue(this, EBattleState.Won);
            }
        }
        else AutoTargetNext();
    }

    private void AutoTargetNext()
    {
        if (enemies == null || enemies.Count == 0) { currentTarget = null; return; }
        currentTarget = enemies.Find(e => e != null && e.gameObject != null && e.CurrentHp > 0);
        if (currentTarget != null) currentTarget.SetSelection(true);
    }

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
            BossEnemy boss = actingEnemy as BossEnemy;
            if (boss != null)
            {
                if (boss.ShouldTriggerEvent()) yield return StartCoroutine(boss.DeceptiveQTESequence(player, qteManager.StartQTE));
                else if (UnityEngine.Random.value <= 0.3f) yield return StartCoroutine(boss.CounterStanceSequence());
                else yield return StartCoroutine(actingEnemy.AttackSequence(player.transform, qteManager.StartQTE));
            }
            else yield return StartCoroutine(actingEnemy.AttackSequence(player.transform, qteManager.StartQTE));
        }

        if (player.CurrentHp <= 0) CurrentState = EBattleState.Lost;
        else { CycleFinishedUnit(); yield return new WaitForSeconds(0.5f); DetermineNextTurn(); }
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
        if (turnTimeline.Count == 0) return;
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
        var stateField = typeof(BattleManager).GetProperty("CurrentState");
        stateField.SetValue(this, EBattleState.Busy);
        isBossSpawned = true;
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
                SetTarget(boss);
            }
        }
        yield return new WaitForSeconds(1.0f);
        stateField.SetValue(this, EBattleState.PlayerTurn);
        DetermineNextTurn();
    }
}