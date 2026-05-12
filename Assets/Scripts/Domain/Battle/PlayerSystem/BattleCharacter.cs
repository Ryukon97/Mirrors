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

    [Header("Effects")]
    public GameObject hitEffectPrefab;

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

        // 공격 위치 계산
        Vector3 targetPos = target.transform.position + (transform.position - target.transform.position).normalized * ATTACK_DISTANCE;

        // 1. 적에게 이동
        while (Vector3.Distance(transform.position, targetPos) > 0.1f)
        {
            if (target == null) { transform.position = originalPosition; yield break; }
            transform.position = Vector3.MoveTowards(transform.position, targetPos, MOVE_SPEED * Time.deltaTime);
            yield return null;
        }

        // --- [2. 타격 시점: 이펙트 생성 및 데미지 계산] ---

        // [이펙트 처리] 적에게 닿은 순간 생성하고 캐릭터의 자식으로 설정하여 함께 이동하게 함
        if (hitEffectPrefab != null)
        {
            // 적의 위치에 생성
            GameObject effect = Instantiate(hitEffectPrefab, target.transform.position + Vector3.up * 0.5f, Quaternion.identity);

            // 이펙트가 캐릭터를 따라다니게 함 (복귀 시 함께 이동)
            effect.transform.SetParent(this.transform);

            // 이펙트가 무한히 남지 않도록 1.5초 뒤 삭제
            Destroy(effect, 1.5f);
        }

        // [데미지 및 반격 처리]
        BossEnemy boss = target as BossEnemy;

        // 만약 보스가 반격 모드라면?
        if (boss != null && boss.isCounterMode)
        {
            // 반격 발동 (BattleManager에서 설정한 2배 데미지 로직 실행)
            boss.ExecuteCounter(this);
        }
        else
        {
            // 반격 모드가 아닐 때만 정상 데미지 계산
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
        }

        // 타격 연출을 위한 짧은 대기 (역경직)
        yield return new WaitForSeconds(0.2f);

        // 3. 원래 위치로 복귀 (이펙트가 자식으로 설정되어 있어 함께 이동함)
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