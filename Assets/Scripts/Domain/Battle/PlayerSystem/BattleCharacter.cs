using System;
using System.Collections;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

public class BattleCharacter : MonoBehaviour
{
    // --------------- 변수 영역 --------------
    // 이벤트는 PascalCase 및 과거 분사형 접미사
    public event Action PlayerHittedEvent;

    public Transform enemyTransform;

    // 상수는 SCREAMING_SNAKE_CASE
    private const float ATTACK_DISTANCE = 1.5f;
    private const float MOVE_SPEED = 10.0f;

    // 멤버 변수는 lowerCamelCase
    private int currentHp = 100;
    private Vector3 originalPosition;
    private bool isAttacking = false;

    // 프로퍼티 => 연산자 사용
    public int CurrentHp => currentHp;

    // --------------- Unity Life Cycle --------------
    private void Start()
    {
        originalPosition = transform.position;
    }

    // --------------- public APIs --------------
    public IEnumerator AttackSequence(Enemy target) // Enemy 인자를 받도록 수정
    {
        if (target == null) yield break;

        Vector3 originalPos = transform.position;
        // 타겟의 위치로 이동 (공격 거리 유지)
        Vector3 targetPos = target.transform.position + (transform.position - target.transform.position).normalized * 1.5f;

        // 1. 이동
        while (Vector3.Distance(transform.position, targetPos) > 0.1f)
        {
            // 이동 도중 타겟이 파괴되었는지 체크 (에러 방지)
            if (target == null) { transform.position = originalPos; yield break; }

            transform.position = Vector3.MoveTowards(transform.position, targetPos, 15f * Time.deltaTime);
            yield return null;
        }

        // 2. 타격 (중요: 여기서 target에게만 대미지를 줌)
        Debug.Log($"{target.name}을(를) 공격합니다!");
        target.TakeDamage(25);
        yield return new WaitForSeconds(0.2f);

        // 3. 복귀
        while (Vector3.Distance(transform.position, originalPos) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, originalPos, 15f * Time.deltaTime);
            yield return null;
        }
        transform.position = originalPos;
    }

    public void TakeDamage(int damage)
    {
        currentHp -= damage;
        // 이벤트 호출
        PlayerHittedEvent?.Invoke();
    }
}