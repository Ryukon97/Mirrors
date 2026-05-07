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
    public IEnumerator AttackSequence()
    {
        isAttacking = true;

        // 1. 이동
        Vector3 targetPos = enemyTransform.position + (transform.position - enemyTransform.position).normalized * ATTACK_DISTANCE;
        while (Vector3.Distance(transform.position, targetPos) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, MOVE_SPEED * Time.deltaTime);
            yield return null;
        }

        // 2. 공격 수행
        Debug.Log("<color=red>[공격]</color> 적에게 피해를 입혔습니다!");
        Enemy targetEnemy = enemyTransform.GetComponent<Enemy>();
        if (targetEnemy != null)
        {
            targetEnemy.TakeDamage(20);
        }

        yield return new WaitForSeconds(0.2f);

        // 3. 복귀
        while (Vector3.Distance(transform.position, originalPosition) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, originalPosition, MOVE_SPEED * Time.deltaTime);
            yield return null;
        }

        transform.position = originalPosition;
        isAttacking = false;
    }

    public void TakeDamage(int damage)
    {
        currentHp -= damage;
        // 이벤트 호출
        PlayerHittedEvent?.Invoke();
    }
}