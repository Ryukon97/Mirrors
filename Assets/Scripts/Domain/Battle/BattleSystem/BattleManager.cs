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
            AutoTargetNext();
        }
        UpdateTimelineUI();
    }

    private void AutoTargetNext()
    {
        currentTarget = enemies.Find(e => e != null && e.CurrentHp > 0);
        if (currentTarget != null) currentTarget.SetSelection(true);
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
        if (actingEnemy != null) yield return StartCoroutine(actingEnemy.AttackSequence(player.transform, qteManager.StartQTE));
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
}