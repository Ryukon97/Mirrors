using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI; // UI 사용을 위해 추가

public class BattleCharacter : MonoBehaviour
{
    // --------------- 변수 영역 --------------
    public event Action PlayerHittedEvent;

    [Header("Status")]
    [SerializeField] private int maxHp = 100;
    private int currentHp;

    [Header("UI")]
    public Image hpBarImage; // 유니티 인스펙터에서 Filled 타입 이미지를 연결하세요.

    private Vector3 originalPosition;
    private const float ATTACK_DISTANCE = 1.5f;
    private const float MOVE_SPEED = 15.0f; // 기존 10.0f에서 공격 시퀀스 속도에 맞춰 조정

    public int CurrentHp => currentHp;

    // --------------- Unity Life Cycle --------------
    private void Awake()
    {
        // Start보다 Awake에서 초기화하는 것이 안전합니다.
        currentHp = maxHp;
    }

    private void Start()
    {
        originalPosition = transform.position;
        UpdateHpUI();
    }

    // --------------- public APIs --------------

    // [통합] 공격 시퀀스: 매니저에서 받은 타겟을 정확히 타격
    public IEnumerator AttackSequence(Enemy target)
    {
        if (target == null) yield break;

        // 타겟의 위치로 이동 (공격 거리 유지)
        Vector3 targetPos = target.transform.position + (transform.position - target.transform.position).normalized * ATTACK_DISTANCE;

        // 1. 이동
        while (Vector3.Distance(transform.position, targetPos) > 0.1f)
        {
            if (target == null) { transform.position = originalPosition; yield break; }
            transform.position = Vector3.MoveTowards(transform.position, targetPos, MOVE_SPEED * Time.deltaTime);
            yield return null;
        }

        // 2. 타격
        Debug.Log($"{target.name}을(를) 공격합니다!");
        target.TakeDamage(25);
        yield return new WaitForSeconds(0.2f);

        // 3. 복귀
        while (Vector3.Distance(transform.position, originalPosition) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, originalPosition, MOVE_SPEED * Time.deltaTime);
            yield return null;
        }
        transform.position = originalPosition;
    }

    // [통합] 피격 로직: 대미지를 입고 UI와 이벤트를 갱신
    public void TakeDamage(int damage)
    {
        currentHp -= damage;
        currentHp = Mathf.Max(currentHp, 0); // 체력이 0 미만으로 내려가지 않게 방지

        Debug.Log($"<color=red>[플레이어 피격]</color> 현재 체력: {currentHp}");

        // UI 갱신
        UpdateHpUI();

        // 이벤트 호출 (사운드나 이펙트용)
        PlayerHittedEvent?.Invoke();

        if (currentHp <= 0)
        {
            Debug.Log("<color=black>플레이어 사망</color>");
        }
    }

    // [추가] 체력바 UI 업데이트
    public void UpdateHpUI()
    {
        if (hpBarImage != null)
        {
            // 현재 체력 비율 계산 (0.0 ~ 1.0)
            hpBarImage.fillAmount = (float)currentHp / maxHp;
        }
    }
}