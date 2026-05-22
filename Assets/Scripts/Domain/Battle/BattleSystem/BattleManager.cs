using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;
using TMPro;

[RequireComponent(typeof(AudioSource))]
public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }

    [Header("Characters")]
    public BattleCharacter player;
    // 일반 적들도 스타트 시점에 이 리스트에 미리 채워져 있다고 가정합니다.
    public List<Enemy> enemies = new List<Enemy>();

    [Header("UI References")]
    public Transform timelineContainer;
    public GameObject playerIconPrefab;
    public GameObject enemyIconPrefab;

    [Header("Damage UI Settings")]
    public TextMeshProUGUI totalDamageText;
    private int totalDamage = 0;

    [Header("Damage Floating UI Settings")]
    public GameObject damageTextPrefab;

    [Header("Ultimate System")]
    public Button ultimateButton;
    public Image ultimateGaugeImage;
    private float currentGauge = 0f;
    private const float MAX_GAUGE = 100f;
    private const float GAUGE_PER_ATTACK = 25f;

    [Header("Audio Settings (SFX)")]
    public AudioClip normalAttackSFX;
    public AudioClip skillAttackSFX;
    public AudioClip ultimateAttackSFX;
    private AudioSource audioSource;

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

    // 겉으로는 읽기 전용이지만 내부에서는 자유롭게 수정 가능하도록 private set 유지
    public EBattleState CurrentState { get; private set; }
    private Enemy currentTarget;

    public bool CanFinishBattle => isBossSpawned && enemies.Count == 0 && !isVictoryLocked;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        CurrentState = EBattleState.Start;

        // 씬 시작 시 enemies 리스트가 비어있다면 방어 코드로 채워줌 (최초 1회만)
        if (enemies == null || enemies.Count == 0)
        {
            enemies = new List<Enemy>(FindObjectsByType<Enemy>(FindObjectsInactive.Exclude));
        }

        UpdateUltimateUI();
        UpdateManaUI();
        UpdateDamageUI();
        InitializeTimeline();
        DetermineNextTurn();
    }

    // --------------- public APIs (Buttons) --------------

    public void OnAttackButtonClick()
    {
        if (Time.timeScale == 0f || CurrentState != EBattleState.PlayerTurn) return;
        ResetTurnDamage();
        StartCoroutine(PlayerTurnSequence());
    }

    public void OnSkillButtonClick()
    {
        if (Time.timeScale == 0f || CurrentState != EBattleState.PlayerTurn || currentMana < SKILL_COST) return;
        ResetTurnDamage();
        StartCoroutine(ExecuteFullAOESkill());
    }

    public void OnUltimateButtonClick()
    {
        if (Time.timeScale == 0f || CurrentState != EBattleState.PlayerTurn || currentGauge < MAX_GAUGE) return;
        ResetTurnDamage();
        StartCoroutine(UltimateThreeHitSequence());
    }

    public void OnWaitButtonClicked()
    {
        if (Time.timeScale == 0f || CurrentState != EBattleState.PlayerTurn) return;
        ResetTurnDamage();
        StartCoroutine(WaitTurnSequence());
    }

    // --------------- 핵심 전투 흐름 제어 ------------------

    // 최적화: 매번 Find하지 않고 관리 중인 리스트를 안전하게 복사하여 반환
    public List<Enemy> GetEnemies()
    {
        return new List<Enemy>(enemies);
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
                if (currentTarget is BossEnemy boss && boss.isCounterMode)
                {
                    yield return StartCoroutine(boss.ExecuteCounterSequence(player.transform));
                }
                else
                {
                    PlaySFX(normalAttackSFX);
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

        PlaySFX(skillAttackSFX);

        foreach (var e in targets)
        {
            if (e == null || e.CurrentHp <= 0) continue;

            if (e is BossEnemy boss && boss.isCounterMode)
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

        // 1. 6.6초 동안 컷씬 연출 대기
        yield return new WaitForSeconds(6.6f);

        // 2. 대기 종료 후 타겟 확보 및 공격
        List<Enemy> targets = GetEnemies();

        if (targets.Count > 0 && player != null)
        {
            PlaySFX(ultimateAttackSFX);
            int pureUltimateDamage = player.UltimateDamage;

            foreach (var enemy in targets)
            {
                if (enemy == null || enemy.CurrentHp <= 0) continue;

                if (enemy is BossEnemy boss && boss.isCounterMode)
                {
                    yield return StartCoroutine(boss.ExecuteCounterSequence(player.transform));
                }
                else
                {
                    enemy.TakeDamage(pureUltimateDamage);
                }
            }
        }

        yield return new WaitForSeconds(0.5f);
        yield return StartCoroutine(FinishPlayerTurn());
    }

    private IEnumerator WaitTurnSequence()
    {
        CurrentState = EBattleState.Busy;
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
        if (currentTarget == null || currentTarget.CurrentHp <= 0)
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
        currentTarget = enemies.Find(e => e != null && e.CurrentHp > 0);
        if (currentTarget != null) currentTarget.SetSelection(true);
    }

    private void DetermineNextTurn()
    {
        if (turnTimeline.Count == 0 || CurrentState == EBattleState.Won || CurrentState == EBattleState.Lost) return;

        BattleUnitOrder nextUnit = turnTimeline[0];
        if (nextUnit.unitType == ECharacterType.Player)
        {
            CurrentState = EBattleState.PlayerTurn;
        }
        else
        {
            if (nextUnit.enemyReference == null || nextUnit.enemyReference.CurrentHp <= 0)
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

        if (actingEnemy != null && actingEnemy.CurrentHp > 0)
        {
            if (actingEnemy is BossEnemy boss)
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

        // 최적화: 리플렉션 제거하고 직관적으로 대입
        CurrentState = newState;
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
        // TODO: 장기적으로는 성능 향상을 위해 UI 풀링(Pooling) 방식을 적용하는 것을 권장합니다.
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

    public void SpawnDamageText(Vector3 worldPosition, int damageAmount)
    {
        totalDamage += damageAmount;
        UpdateDamageUI();

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

    private void UpdateDamageUI()
    {
        if (totalDamageText != null)
        {
            totalDamageText.text = $"TURN DAMAGE: <color=#FFCC00>{totalDamage}</color>";
        }
    }

    private void ResetTurnDamage()
    {
        totalDamage = 0;
        UpdateDamageUI();
    }

    private void PlaySFX(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}