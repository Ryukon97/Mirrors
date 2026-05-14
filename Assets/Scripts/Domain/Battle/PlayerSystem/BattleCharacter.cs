using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BattleCharacter : MonoBehaviour
{
    public event Action PlayerHittedEvent;
    private Animator anim;

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
        anim = GetComponent<Animator>();
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

        // 공격 시작 전 적을 향해 회전 (모든 공격 공통)
        if (target != null)
        {
            Vector3 targetDirection = new Vector3(target.transform.position.x, transform.position.y, target.transform.position.z);
            transform.LookAt(targetDirection);
        }

        // --- [광역 스킬(Skill) 처리] ---
        if (attackType == "Skill")
        {
            // ※ 스킬 애니메이션이 없으므로 SetBool("Attack", true)를 호출하지 않습니다.

            if (skillCastEffectPrefab != null)
            {
                GameObject castEffect = Instantiate(skillCastEffectPrefab, transform.position, Quaternion.identity);
                Destroy(castEffect, 2.0f);
            }

            yield return new WaitForSeconds(0.6f);

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
            // 1. 일반 공격/궁극기일 때만 애니메이션 시작
            if (anim != null) anim.SetBool("Attack", true);

            // 애니메이션 상에서 타격이 이루어지는 타이밍까지 대기
            yield return new WaitForSeconds(1.6f);

            if (hitEffectPrefab != null && target != null)
            {
                GameObject effect = Instantiate(hitEffectPrefab, target.transform.position + Vector3.up * 0.5f, Quaternion.identity);
                Destroy(effect, 1.5f);
            }

            yield return new WaitForSeconds(0.3f);

            // 2. 애니메이션 종료 (Idle로 복귀)
            if (anim != null) anim.SetBool("Attack", false);
        }

        // 애니메이션 내의 루트 모션으로 인해 좌표가 변했다면 원래 자리로 초기화
        transform.position = originalPosition;

        yield return new WaitForSeconds(0.2f);
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