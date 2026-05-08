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