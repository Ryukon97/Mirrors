using System;
using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] private int skillDamage = 40;        // 스킬 데미지 상향 (기존 30 -> 60)
    [SerializeField] private int ultimateDamage = 25;     // 궁극기 타당 데미지

    [Header("UI")]
    public Image hpBarImage;

    [Header("Effects")]
    public GameObject hitEffectPrefab;      // 일반 타격 이펙트
    public GameObject skillCastEffectPrefab; // [추가] 스킬 시전 시 캐릭터에게 나올 이펙트
    public GameObject skillHitEffectPrefab;  // [추가] 스킬 타격 시 적들에게 터질 이펙트

    private Vector3 originalPosition;
    private const float ATTACK_DISTANCE = 1.5f;
    private const float MOVE_SPEED = 15.0f;

    private bool isAttacking = false;
    public int MaxHp => maxHp;
    public int CurrentHp => currentHp;
    public int SkillDamage => skillDamage;

    private void Awake()
    {
        currentHp = maxHp;
    }

    private void Start()
    {
        currentHp = maxHp;
        originalPosition = transform.position;
        UpdateHpUI();
    }
    public void Heal(int amount)
    {
        currentHp += amount;

        // 최대 체력을 넘지 않도록 제한
        if (currentHp > maxHp) currentHp = maxHp;

        Debug.Log($"<color=green>[Heal]</color> {amount} 회복! (현재: {currentHp}/{maxHp})");

        // HP UI 업데이트 (기존 메서드 호출)
        UpdateHpUI();
    }
    // ---------------------------------------------------------
    // [핵심 수정] 공격 시퀀스: 어떤 타입의 공격인지 인자를 추가로 받음
    // ---------------------------------------------------------
    public IEnumerator AttackSequence(Enemy target, string attackType = "Normal")
    {
        if (isAttacking) yield break;
        isAttacking = true;

        // --- [광역 스킬(Skill) 처리] ---
        if (attackType == "Skill")
        {
            Debug.Log("<color=cyan>[광역 스킬 시전]</color>");

            // 1. 캐릭터 제자리 시전 연출 (여기서는 데미지 안 들어감)
            if (skillCastEffectPrefab != null)
            {
                GameObject castEffect = Instantiate(skillCastEffectPrefab, transform.position, Quaternion.identity);
                Destroy(castEffect, 2.0f);
            }

            // 시전 연출을 위한 대기 시간 (이 시간이 지난 후 타격)
            yield return new WaitForSeconds(0.6f);

            // 2. 적 타격 연출 및 실제 데미지 판정
            List<Enemy> allEnemies = BattleManager.Instance.GetEnemies();
            if (allEnemies != null)
            {
                foreach (var enemy in allEnemies)
                {
                    if (enemy == null || enemy.CurrentHp <= 0) continue;

                    // [타격 이펙트 생성]
                    if (skillHitEffectPrefab != null)
                    {
                        GameObject hitEffect = Instantiate(skillHitEffectPrefab, enemy.transform.position + Vector3.up * 0.5f, Quaternion.identity);
                        Destroy(hitEffect, 1.5f);
                    }

                    // [데미지 적용] 타격 이펙트가 생성되는 이 시점에 데미지를 줍니다.
                    BossEnemy boss = enemy as BossEnemy;
                    if (boss != null && boss.isCounterMode)
                    {
                        boss.ExecuteCounter(this);
                    }
                }
            }

            yield return new WaitForSeconds(0.4f);
            isAttacking = false;
            yield break;
        }

        // --- [일반 공격 및 궁극기 처리] ---
        // (이동 후 타격 이펙트 생성 시점에 TakeDamage가 호출되도록 기존 로직 유지)
        Vector3 targetPos = target.transform.position + (transform.position - target.transform.position).normalized * ATTACK_DISTANCE;

        while (Vector3.Distance(transform.position, targetPos) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, MOVE_SPEED * Time.deltaTime);
            yield return null;
        }

        // 적에게 닿았을 때 이펙트 출력 및 데미지
        if (hitEffectPrefab != null)
        {
            GameObject effect = Instantiate(hitEffectPrefab, target.transform.position + Vector3.up * 0.5f, Quaternion.identity);
            Destroy(effect, 1.5f);
        }

        BossEnemy singleBoss = target as BossEnemy;
        if (singleBoss != null && singleBoss.isCounterMode) singleBoss.ExecuteCounter(this);
        else target.TakeDamage((attackType == "Ultimate") ? ultimateDamage : normalAttackDamage);

        yield return new WaitForSeconds(0.3f);

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