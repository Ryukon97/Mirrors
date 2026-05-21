using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BattleCharacter : MonoBehaviour
{
    public event Action PlayerHittedEvent;
    private Animator anim;
    private Rigidbody rb;

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
    private Quaternion originalRotation;
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
        rb = GetComponent<Rigidbody>();

        // Awake 시점에 최초 배치된 위치와 회전값을 정확히 기억합니다.
        originalPosition = transform.position;
        originalRotation = transform.rotation;
    }

    private void Start()
    {
        currentHp = maxHp;

        // Start 시점에도 애니메이션 초기화 등으로 밀리는 것을 방지하기 위해 재고정
        transform.position = originalPosition;
        transform.rotation = originalRotation;

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

        // 공격 시작 전 적을 향해 회전
        if (target != null)
        {
            Vector3 targetDirection = new Vector3(target.transform.position.x, transform.position.y, target.transform.position.z);
            transform.LookAt(targetDirection);
        }

        if (attackType == "Skill")
        {
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
        else
        {
            if (anim != null) anim.SetBool("Attack", true);

            // 앞으로 날아가서 타격하는 타이밍까지 대기 (1.6초)
            yield return new WaitForSeconds(1.6f);

            if (hitEffectPrefab != null && target != null)
            {
                GameObject effect = Instantiate(hitEffectPrefab, target.transform.position + Vector3.up * 0.5f, Quaternion.identity);
                Destroy(effect, 1.5f);
            }

            // 타격 후 후속 모션 대기 (0.3초)
            yield return new WaitForSeconds(0.3f);

            // 애니메이션 종료 요청 (Idle 복귀 시작)
            if (anim != null) anim.SetBool("Attack", false);

            // 앞으로 날아갔던 애니메이션 연산이 끝날 수 있도록 한 프레임 대기 후 다음 로직 수행
            yield return null;
        }

        // --- 밀림 및 방향 틀어짐 방지 핵심 로직 (보강됨) ---

        // 1. 애니메이션 이동으로 쌓인 물리 속도 완전히 리셋
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // 2. 처음 시작할 때의 절대 좌표와 정면 회전값으로 덮어쓰기
        transform.position = originalPosition;
        transform.rotation = originalRotation;

        // 3. 애니메이션 상태가 완벽히 Idle로 전환될 때까지 확실하게 좌표를 고정 (0.2초간 매 프레임 고정)
        float elapsed = 0f;
        while (elapsed < 0.2f)
        {
            transform.position = originalPosition;
            transform.rotation = originalRotation;
            if (rb != null) rb.linearVelocity = Vector3.zero;

            elapsed += Time.deltaTime;
            yield return null;
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