using System;
using System.Collections;
using UnityEngine;

public class BossEnemy : Enemy
{
    [Header("Boss Gimmick Settings")]
    [SerializeField] private int specialAttackDamage = 40;
    [SerializeField] private int counterDamage = 20;
    private bool eventTriggered = false;

    [Header("Counter Gimmick")]
    public bool isCounterMode = false;
    private Color originalColor;
    private SpriteRenderer spriteRenderer;

    protected override void Awake()
    {
        base.Awake();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) originalColor = spriteRenderer.color;
    }

    public bool ShouldTriggerEvent()
    {
        // maxHp는 부모 클래스(Enemy)에서 protected로 선언되어 있어야 합니다.
        if (CurrentHp <= (maxHp / 2) && !eventTriggered)
        {
            eventTriggered = true;
            return true;
        }
        return false;
    }

    // --- 반격 로직 수정 및 추가 ---

    public IEnumerator CounterStanceSequence()
    {
        isCounterMode = true;
        Debug.Log("<color=yellow>보스가 반격 자세를 취합니다!</color>");

        // 시각적 피드백: 노란색으로 변경
        if (spriteRenderer != null) spriteRenderer.color = Color.yellow;

        yield return new WaitForSeconds(1.5f);
    }

    /// <summary>
    /// BattleManager의 WaitTurnSequence 등 외부에서 호출하여 반격을 강제로 해제하는 함수
    /// </summary>
    public void DisableCounterMode()
    {
        isCounterMode = false;

        // 시각적 피드백: 원래 색상으로 복구
        if (spriteRenderer != null) spriteRenderer.color = originalColor;

        Debug.Log($"<color=white>{gameObject.name}의 반격 자세가 강제로 해제되었습니다.</color>");
    }

    public void EndCounterMode()
    {
        // 내부 로직 일관성을 위해 DisableCounterMode 호출
        DisableCounterMode();
    }

    public void ExecuteCounter(BattleCharacter player)
    {
        // 반격 데미지를 기존 20에서 2배인 40으로 적용
        int doubleDamage = counterDamage * 2;
        Debug.Log($"<color=orange>반격 발동! 플레이어에게 {doubleDamage}의 대미지!</color>");
        player.TakeDamage(doubleDamage);

        // 반격 성공 후 자세 해제
        EndCounterMode();
    }

    // --- QTE 및 기타 로직 ---

    public IEnumerator DeceptiveQTESequence(BattleCharacter player, Action<Action<bool>> requestQTE)
    {
        Vector3 startPosition = transform.position;
        Vector3 targetPos = player.transform.position + (transform.position - player.transform.position).normalized * 1.8f;

        // 접근 연출
        while (Vector3.Distance(transform.position, targetPos) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, 20f * Time.deltaTime);
            yield return null;
        }

        // QTE 시작 (시간 느려짐)
        Time.timeScale = 0.2f;
        bool qteFinished = false;
        bool isEvaded = false;
        requestQTE((result) => { isEvaded = result; qteFinished = true; });
        yield return new WaitUntil(() => qteFinished);

        Time.timeScale = 1.0f;
        Time.fixedDeltaTime = 0.02f;

        // 결과 반영
        int finalDamage = isEvaded ? specialAttackDamage / 2 : specialAttackDamage;
        player.TakeDamage(finalDamage);
        yield return new WaitForSeconds(0.5f);

        // 복귀 연출
        float journeyTime = 0.5f;
        float elapsed = 0f;
        Vector3 currentPos = transform.position;
        while (elapsed < journeyTime)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(currentPos, startPosition, elapsed / journeyTime);
            yield return null;
        }
        transform.position = startPosition;
        yield return new WaitForSeconds(0.5f);
    }
}