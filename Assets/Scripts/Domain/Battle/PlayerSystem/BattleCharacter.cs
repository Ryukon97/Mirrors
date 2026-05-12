using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BattleCharacter : MonoBehaviour
{
    public event Action PlayerHittedEvent;

    [Header("Status")]
    [SerializeField] private int maxHp = 100;
    private int currentHp;

    [Header("Damage Settings")] // 데미지 수치 통합 관리
    [SerializeField] private int normalAttackDamage = 25;
    [SerializeField] private int skillDamage = 60;        // 스킬 데미지 상향 (기존 30 -> 60)
    [SerializeField] private int ultimateDamage = 25;     // 궁극기 타당 데미지

    [Header("UI")]
    public Image hpBarImage;

    private Vector3 originalPosition;
    private const float ATTACK_DISTANCE = 1.5f;
    private const float MOVE_SPEED = 15.0f;

    public int CurrentHp => currentHp;

    private void Awake()
    {
        currentHp = maxHp;
    }

    private void Start()
    {
        originalPosition = transform.position;
        UpdateHpUI();
    }

    // ---------------------------------------------------------
    // [핵심 수정] 공격 시퀀스: 어떤 타입의 공격인지 인자를 추가로 받음
    // ---------------------------------------------------------
    public IEnumerator AttackSequence(Enemy target, string attackType = "Normal")
    {
        if (target == null) yield break;

        Vector3 targetPos = target.transform.position + (transform.position - target.transform.position).normalized * ATTACK_DISTANCE;

        // 1. 이동
        while (Vector3.Distance(transform.position, targetPos) > 0.1f)
        {
            if (target == null) { transform.position = originalPosition; yield break; }
            transform.position = Vector3.MoveTowards(transform.position, targetPos, MOVE_SPEED * Time.deltaTime);
            yield return null;
        }

        // 2. 타격 및 데미지 계산
        // BattleManager에서 하던 데미지 처리를 여기서 한 번만 수행 (중복 방지)
        int finalDamage = 0;
        switch (attackType)
        {
            case "Skill":
                finalDamage = skillDamage;
                Debug.Log($"<color=cyan>[스킬]</color> {target.name}에게 {finalDamage} 데미지!");
                break;
            case "Ultimate":
                finalDamage = ultimateDamage;
                Debug.Log($"<color=magenta>[궁극기]</color> {target.name}에게 {finalDamage} 데미지!");
                break;
            default:
                finalDamage = normalAttackDamage;
                Debug.Log($"{target.name}에게 평타 {finalDamage} 데미지!");
                break;
        }

        target.TakeDamage(finalDamage);

        // 타격 연출을 위한 짧은 대기
        yield return new WaitForSeconds(0.2f);

        // 3. 복귀
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
        currentHp = Mathf.Max(currentHp, 0);
        Debug.Log($"<color=red>[플레이어 피격]</color> 현재 체력: {currentHp}");
        UpdateHpUI();
        PlayerHittedEvent?.Invoke();

        if (currentHp <= 0) Debug.Log("<color=black>플레이어 사망</color>");
    }

    public void UpdateHpUI()
    {
        if (hpBarImage != null) hpBarImage.fillAmount = (float)currentHp / maxHp;
    }
}