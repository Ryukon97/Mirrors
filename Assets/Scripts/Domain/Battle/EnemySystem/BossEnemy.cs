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

    // [추가] 애니메이터 변수
    private Animator anim;

    protected override void Awake()
    {
        base.Awake();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) originalColor = spriteRenderer.color;

        // [추가] 애니메이터 초기화
        anim = GetComponent<Animator>();
    }

    public bool ShouldTriggerEvent()
    {
        if (CurrentHp <= (maxHp / 2) && !eventTriggered)
        {
            eventTriggered = true;
            return true;
        }
        return false;
    }

    public IEnumerator CounterStanceSequence()
    {
        isCounterMode = true;
        Debug.Log("<color=yellow>보스가 반격 자세를 취합니다!</color>");

        if (spriteRenderer != null) spriteRenderer.color = Color.yellow;

        yield return new WaitForSeconds(1.5f);
    }

    public void DisableCounterMode()
    {
        isCounterMode = false;
        if (spriteRenderer != null) spriteRenderer.color = originalColor;
        Debug.Log($"<color=white>{gameObject.name}의 반격 자세가 강제로 해제되었습니다.</color>");
    }

    public void EndCounterMode()
    {
        DisableCounterMode();
    }

    public void ExecuteCounter(BattleCharacter player)
    {
        // [추가] 반격 시 애니메이션 실행 (Trigger: Counter)
        if (anim != null) anim.SetTrigger("Counter");

        int doubleDamage = counterDamage * 2;
        Debug.Log($"<color=orange>반격 발동! 플레이어에게 {doubleDamage}의 대미지!</color>");
        player.TakeDamage(doubleDamage);

        EndCounterMode();
    }

    public IEnumerator DeceptiveQTESequence(BattleCharacter player, Action<Action<bool>> requestQTE)
    {
        Vector3 startPosition = transform.position;
        Vector3 targetPos = player.transform.position + (transform.position - player.transform.position).normalized * 1.8f;

        while (Vector3.Distance(transform.position, targetPos) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, 20f * Time.deltaTime);
            yield return null;
        }

        // [추가] QTE 공격 애니메이션 시작 (Bool: Attack ON)
        if (anim != null) anim.SetBool("Attack", true);

        Time.timeScale = 0.2f;
        bool qteFinished = false;
        bool isEvaded = false;
        requestQTE((result) => { isEvaded = result; qteFinished = true; });
        yield return new WaitUntil(() => qteFinished);

        Time.timeScale = 1.0f;
        Time.fixedDeltaTime = 0.02f;

        int finalDamage = isEvaded ? specialAttackDamage / 2 : specialAttackDamage;
        player.TakeDamage(finalDamage);

        // [추가] 공격 애니메이션 종료 (Bool: Attack OFF)
        if (anim != null) anim.SetBool("Attack", false);

        yield return new WaitForSeconds(0.5f);

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