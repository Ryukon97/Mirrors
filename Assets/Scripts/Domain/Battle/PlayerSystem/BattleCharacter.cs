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

    // ---------------- [추가: 플레이어 피격 컬러 변경용 변수] ----------------
    private List<Renderer> playerRenderers = new List<Renderer>();
    private List<Color[]> originalColors = new List<Color[]>();
    private Coroutine hitFlashCoroutine;
    // ----------------------------------------------------------------------

    private void Awake()
    {
        currentHp = maxHp;
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();

        originalPosition = transform.position;
        originalRotation = transform.rotation;

        // ---------------- [추가: 플레이어 본체 및 자식들의 모든 렌더러와 원래 색상 백업] ----------------
        var skinned = GetComponentsInChildren<SkinnedMeshRenderer>();
        var mesh = GetComponentsInChildren<MeshRenderer>();

        playerRenderers.AddRange(skinned);
        playerRenderers.AddRange(mesh);

        foreach (var renderer in playerRenderers)
        {
            Color[] colors = new Color[renderer.materials.Length];
            for (int i = 0; i < renderer.materials.Length; i++)
            {
                colors[i] = renderer.materials[i].color;
            }
            originalColors.Add(colors);
        }
        // --------------------------------------------------------------------------------------------
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
                Quaternion normalRot = Quaternion.Euler(90f, 0f, 0f);
                GameObject effect = Instantiate(hitEffectPrefab, target.transform.position + Vector3.up * 0.5f, normalRot);
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

    // ---------------- [수정: 피격 연출 코루틴 작동 처리] ----------------
    public void TakeDamage(int damage)
    {
        currentHp -= damage;
        currentHp = Mathf.Max(currentHp, 0);
        Debug.Log($"<color=red>[플레이어 피격]</color> 현재 체력: {currentHp}");
        UpdateHpUI();
        PlayerHittedEvent?.Invoke();

        // 연속으로 맞았을 때 색상이 붉은 상태로 굳는 버그 방지용 예외 처리
        if (hitFlashCoroutine != null) StopCoroutine(hitFlashCoroutine);
        hitFlashCoroutine = StartCoroutine(HitFlashSequence());

        if (currentHp <= 0) Debug.Log("<color=black>플레이어 사망</color>");
    }

    // ---------------- [추가: 피격 시 마테리얼 깜빡임 코루틴] ----------------
    private IEnumerator HitFlashSequence()
    {
        // 1. 플레이어 몸 전체 메테리얼을 붉은색으로 물들임
        for (int r = 0; r < playerRenderers.Count; r++)
        {
            if (playerRenderers[r] == null) continue;
            for (int m = 0; m < playerRenderers[r].materials.Length; m++)
            {
                playerRenderers[r].materials[m].color = new Color(1f, 0.15f, 0.15f);
            }
        }

        // 0.2초 동안 유지 (대미지를 입었다는 시각적 피드백 제공)
        yield return new WaitForSeconds(0.2f);

        // 2. 백업해 뒀던 순수한 고유 원본 색상으로 완벽 롤백
        for (int r = 0; r < playerRenderers.Count; r++)
        {
            if (playerRenderers[r] == null) continue;
            for (int m = 0; m < playerRenderers[r].materials.Length; m++)
            {
                playerRenderers[r].materials[m].color = originalColors[r][m];
            }
        }
    }
    // ----------------------------------------------------------------------

    public void UpdateHpUI()
    {
        if (hpBarImage != null) hpBarImage.fillAmount = (float)currentHp / maxHp;
    }
}