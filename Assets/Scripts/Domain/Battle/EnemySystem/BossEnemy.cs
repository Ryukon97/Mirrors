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

    // CS0115 에러 해결: 부모의 Awake가 virtual이어야 합니다.
    protected override void Awake()
    {
        base.Awake();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) originalColor = spriteRenderer.color;
    }

    // CS0111 에러 해결: ShouldTriggerEvent가 아래쪽에 한 번 더 있는지 확인하고 하나만 남기세요.
    public bool ShouldTriggerEvent()
    {
        // CS0122 에러 해결: Enemy.maxHp가 protected여야 합니다.
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

    public void EndCounterMode()
    {
        isCounterMode = false;
        if (spriteRenderer != null) spriteRenderer.color = originalColor;
        Debug.Log("보스의 반격 자세가 풀렸습니다.");
    }

    public void ExecuteCounter(BattleCharacter player)
    {
        // 반격 데미지를 기존 20에서 더 강력하게(예: 40) 상향하여 2배의 압박을 줍니다.
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

        Time.timeScale = 0.2f;
        bool qteFinished = false;
        bool isEvaded = false;
        requestQTE((result) => { isEvaded = result; qteFinished = true; });
        yield return new WaitUntil(() => qteFinished);

        Time.timeScale = 1.0f;
        Time.fixedDeltaTime = 0.02f;

        int finalDamage = isEvaded ? specialAttackDamage / 2 : specialAttackDamage;
        player.TakeDamage(finalDamage);
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