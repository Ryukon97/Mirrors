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

    [Header("Damage Settings")]
    [SerializeField] private int normalAttackDamage = 25;
    [SerializeField] private int skillDamage = 40;
    [SerializeField] private int ultimateDamage = 120;     // 궁극기 기본 대미지 (120)

    [Header("UI")]
    public Image hpBarImage;

    [Header("Effects")]
    public GameObject hitEffectPrefab;
    public GameObject skillCastEffectPrefab;
    public GameObject skillHitEffectPrefab;

    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private const float ATTACK_DISTANCE = 1.5f;
    private const float MOVE_SPEED = 15.0f;

    private bool isAttacking = false;
    public int MaxHp => maxHp;
    public int CurrentHp => currentHp;
    public int NormalAttackDamage => normalAttackDamage;
    public int SkillDamage => skillDamage;
    public int UltimateDamage => ultimateDamage; // BattleManager에서 이 값을 가져갑니다.

    private void Awake()
    {
        currentHp = maxHp;
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();

        originalPosition = transform.position;
        originalRotation = transform.rotation;
    }

    private void Start()
    {
        currentHp = maxHp;
        transform.position = originalPosition;
        transform.rotation = originalRotation;
        UpdateHpUI();
    }

    public void Heal(int amount)
    {
        currentHp += amount;
        if (currentHp > maxHp) currentHp = maxHp;
        Debug.Log($"<color=green>[Heal]</color> {amount} 회복! (현재: {currentHp}/{maxHp})");
        UpdateHpUI();
    }

    public IEnumerator AttackSequence(Enemy target, string attackType = "Normal")
    {
        if (isAttacking) yield break;
        isAttacking = true;

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
            yield return new WaitForSeconds(1.6f);

            if (hitEffectPrefab != null && target != null)
            {
                GameObject effect = Instantiate(hitEffectPrefab, target.transform.position + Vector3.up * 0.5f, Quaternion.identity);
                Destroy(effect, 1.5f);
            }

            yield return new WaitForSeconds(0.3f);
            if (anim != null) anim.SetBool("Attack", false);
            yield return null;
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        transform.position = originalPosition;
        transform.rotation = originalRotation;

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