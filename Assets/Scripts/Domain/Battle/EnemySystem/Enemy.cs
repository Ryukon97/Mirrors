using UnityEngine;
using System.Collections;

public class Enemy : MonoBehaviour
{
    // --------------- 변수 영역 --------------
    private const float ATTACK_DISTANCE = 1.5f;
    private const float MOVE_SPEED = 10.0f;

    private int currentHp = 200;
    private Vector3 originalPosition;

    public int CurrentHp => currentHp;

    // --------------- Unity Life Cycle --------------
    private void Start()
    {
        originalPosition = transform.position;
    }

    // --------------- public APIs --------------

    public IEnumerator AttackSequence(Transform target)
    {
        // 1. 타겟(플레이어) 방향으로 이동
        Vector3 targetPos = target.position + (transform.position - target.position).normalized * ATTACK_DISTANCE;
        while (Vector3.Distance(transform.position, targetPos) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, MOVE_SPEED * Time.deltaTime);
            yield return null;
        }

        // 2. 공격 수행 및 데미지 전달
        Debug.Log("<color=blue>[적 공격]</color> 적이 플레이어를 타격했습니다!");
        BattleCharacter player = target.GetComponent<BattleCharacter>();
        if (player != null)
        {
            player.TakeDamage(15);
        }
        yield return new WaitForSeconds(0.3f);

        // 3. 원래 위치로 복귀
        while (Vector3.Distance(transform.position, originalPosition) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, originalPosition, MOVE_SPEED * Time.deltaTime);
            yield return null;
        }

        transform.position = originalPosition;
    }

    public void TakeDamage(int damage)
    {
        currentHp -= damage;
        Debug.Log($"적 HP 감소: {currentHp}");
    }
}