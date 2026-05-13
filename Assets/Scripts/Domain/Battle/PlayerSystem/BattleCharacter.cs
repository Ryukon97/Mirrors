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
    public int NormalAttackDamage => normalAttackDamage;    
    public int SkillDamage => skillDamage;
    public int UltimateDamage => ultimateDamage;

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
            if (skillCastEffectPrefab != null)
            {
                GameObject castEffect = Instantiate(skillCastEffectPrefab, transform.position, Quaternion.identity);
                Destroy(castEffect, 2.0f);
            }

            // 0.6초 동안 시전 애니메이션/이펙트 연출 (데미지는 BattleManager가 이 시간에 맞춰서 줄 것임)
            yield return new WaitForSeconds(0.6f);

            // 타격 이펙트만 생성
            List<Enemy> allEnemies = BattleManager.Instance.GetEnemies();
            foreach (var enemy in allEnemies)
            {
                if (enemy == null || enemy.CurrentHp <= 0) continue;
                if (skillHitEffectPrefab != null)
                {
                    GameObject hitEffect = Instantiate(skillHitEffectPrefab, enemy.transform.position + Vector3.up * 0.5f, Quaternion.identity);
                    Destroy(hitEffect, 1.5f);
                }
            }
            yield return new WaitForSeconds(0.4f);
        }
        // --- [일반 공격 및 궁극기 처리] ---
        else
        {
            Vector3 targetPos = target.transform.position + (transform.position - target.transform.position).normalized * ATTACK_DISTANCE;

            // 적에게 이동
            while (Vector3.Distance(transform.position, targetPos) > 0.1f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPos, MOVE_SPEED * Time.deltaTime);
                yield return null;
            }

            // 적에게 닿았을 때 이펙트만 출력 (여기서 TakeDamage를 삭제!)
            if (hitEffectPrefab != null)
            {
                GameObject effect = Instantiate(hitEffectPrefab, target.transform.position + Vector3.up * 0.5f, Quaternion.identity);
                Destroy(effect, 1.5f);
            }

            yield return new WaitForSeconds(0.3f);

            // 원래 위치로 복귀
            while (Vector3.Distance(transform.position, originalPosition) > 0.1f)
            {
                transform.position = Vector3.MoveTowards(transform.position, originalPosition, MOVE_SPEED * Time.deltaTime);
                yield return null;
            }
            transform.position = originalPosition;
        }

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