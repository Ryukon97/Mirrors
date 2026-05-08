using System;
using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    // --------------- 변수 영역 --------------
    private const float ATTACK_DISTANCE = 1.5f;
    private const float MOVE_SPEED = 10.0f;

    private int currentHp = 200;
    private Vector3 originalPosition;

    public int CurrentHp => currentHp;
    public GameObject selectionIndicator;

    // --------------- Unity Life Cycle --------------
    private void Start()
    {
        originalPosition = transform.position;
    }

    // --------------- public APIs --------------

     // 선택되었을 때 보여줄 이펙트나 이미지 (선택 사항)

    private void OnMouseDown()
    {
        // 클릭 시 매니저에게 나를 타겟으로 설정하라고 알림
        FindObjectOfType<BattleManager>().SetTarget(this);
    }

    public void SetSelection(bool isSelected)
    {
        if (selectionIndicator != null)
            selectionIndicator.SetActive(isSelected);
    }
    // Enemy.cs

    // BattleManager에서 QTE 결과(isEvaded)를 받아서 실행하도록 매개변수 추가
    public IEnumerator AttackSequence(Transform target, Action<Action<bool>> requestQTE)
    {
        // 1. 타겟(플레이어) 방향으로 이동
        Vector3 targetPos = target.position + (transform.position - target.position).normalized * ATTACK_DISTANCE;

        while (Vector3.Distance(transform.position, targetPos) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, MOVE_SPEED * Time.deltaTime);
            yield return null;
        }

        // [추가] 2. 공격 직전 슬로우 모션 및 QTE 발생
        Time.timeScale = 0.2f; // 시간을 5배 느리게 설정
        Time.fixedDeltaTime = 0.02f * Time.timeScale; // 물리 연산 주기도 함께 조절 (부드러운 연출용)

        bool isEvaded = false;
        bool qteFinished = false;

        // 매니저를 통해 QTE 시작 (BattleManager에서 이 로직을 넘겨줌)
        requestQTE((result) => {
            isEvaded = result;
            qteFinished = true;
        });

        // QTE가 끝날 때까지 대기 (실시간 시간 기준으로 대기해야 함)
        yield return new WaitUntil(() => qteFinished);

        // 3. 시간 정상화
        Time.timeScale = 1.0f;
        Time.fixedDeltaTime = 0.02f;

        // 4. 대미지 판정
        if (isEvaded)
        {
            Debug.Log("<color=cyan>회피 성공!</color>");
        }
        else
        {
            BattleCharacter player = target.GetComponent<BattleCharacter>();
            if (player != null) player.TakeDamage(15);
        }

        yield return new WaitForSeconds(0.3f);

        // 5. 원래 위치로 복귀
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
        Debug.Log($"{gameObject.name} HP 감소: {currentHp}");

        if (currentHp <= 0)
        {
            Debug.Log($"{gameObject.name} 사망!");
            // BattleManager의 리스트에서도 제거하기 위해 아래 함수 호출
            FindObjectOfType<BattleManager>().RemoveEnemy(this);
            Destroy(gameObject);
        }
    }
}