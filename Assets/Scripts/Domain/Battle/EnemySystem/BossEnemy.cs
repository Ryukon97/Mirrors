using System;
using System.Collections;
using UnityEngine;

public class BossEnemy : Enemy
{
    [Header("Boss Gimmick Settings")]
    [SerializeField] private int specialAttackDamage = 40;
    private bool eventTriggered = false;

    // 보스 전용: QTE를 기만하는 특수 공격
    public IEnumerator DeceptiveQTESequence(BattleCharacter player, Action<Action<bool>> requestQTE)
    {
        // 1. 시작 위치 확실히 저장
        Vector3 startPosition = transform.position;
        Debug.Log($"공격 시작 위치: {startPosition}");

        // 2. 플레이어 앞으로 돌진 (기존 동일)
        Vector3 targetPos = player.transform.position + (transform.position - player.transform.position).normalized * 1.8f;
        while (Vector3.Distance(transform.position, targetPos) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, 20f * Time.deltaTime);
            yield return null;
        }

        // 3. QTE 로직 (기존 동일)
        Time.timeScale = 0.2f;
        bool qteFinished = false;
        bool isEvaded = false;
        requestQTE((result) => { isEvaded = result; qteFinished = true; });
        yield return new WaitUntil(() => qteFinished);

        Time.timeScale = 1.0f;
        Time.fixedDeltaTime = 0.02f;

        // 4. 데미지 입히기
        int finalDamage = isEvaded ? specialAttackDamage / 2 : specialAttackDamage;
        player.TakeDamage(finalDamage);

        Debug.Log($"<color=red>보스의 강제 타격! {finalDamage}의 데미지!</color>");

        // 타격 후 잠깐 멈춰서 임팩트를 줌
        yield return new WaitForSeconds(0.5f);

        // 5. 복귀 로직 (Lerp 방식)
        Debug.Log("보스 복귀 시작");
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
        Debug.Log("보스 복귀 완료");

        yield return new WaitForSeconds(0.5f);
    }

    public bool ShouldTriggerEvent()
    {
        // 체력이 50% 이하일 때 딱 한 번만 발동
        if (CurrentHp <= 100 && !eventTriggered)
        {
            eventTriggered = true;
            return true;
        }
        return false;
    }
}