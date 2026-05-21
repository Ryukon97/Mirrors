using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic; // List 사용을 위해 추가

public class BossEnemy : Enemy
{
    [Header("Boss Gimmick Settings")]
    public bool isCounterMode = false;

    [SerializeField] private float attackDistance = 2.0f;
    [SerializeField] private float counterDistance = 1.5f;

    // 부모(Enemy)의 오버라이딩을 방지하고 보스 고유의 애니메이터 제어를 위한 변수
    private Animator bossAnim;

    // 축 뒤틀림 현상을 완벽하게 방지하기 위해 보스용 정면 회전값 저장
    private Quaternion bossOriginalRot;

    // ---------------- [추가: 3D 보스 반격 컬러 변경용 변수] ----------------
    private List<Renderer> bossRenderers = new List<Renderer>();
    private List<Color[]> originalColors = new List<Color[]>(); // 원래 메테리얼들의 고유 색상 백업용
    // ----------------------------------------------------------------------

    protected new void Awake()
    {
        base.Awake();

        bossAnim = GetComponent<Animator>();
        if (bossAnim == null)
        {
            bossAnim = GetComponentInChildren<Animator>();
        }

        // ---------------- [추가: 보스 본체 및 자식 오브젝트의 모든 렌더러 캐싱] ----------------
        // SkinnedMeshRenderer(애니메이션 뼈대용)와 일반 MeshRenderer 모두 긁어옵니다.
        var skinned = GetComponentsInChildren<SkinnedMeshRenderer>();
        var mesh = GetComponentsInChildren<MeshRenderer>();

        bossRenderers.AddRange(skinned);
        bossRenderers.AddRange(mesh);

        // 최초의 순수한 고유 색상들을 전부 백업합니다 (멀티 메테리얼 대응)
        foreach (var renderer in bossRenderers)
        {
            Color[] colors = new Color[renderer.materials.Length];
            for (int i = 0; i < renderer.materials.Length; i++)
            {
                colors[i] = renderer.materials[i].color;
            }
            originalColors.Add(colors);
        }
        // ------------------------------------------------------------------------------------
    }

    private void Start()
    {
        bossOriginalRot = transform.rotation;
    }

    public void SyncBossAnimatorWithBase()
    {
        if (bossAnim == null)
        {
            bossAnim = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
        }

        System.Reflection.FieldInfo field = typeof(Enemy).GetField("anim", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(this, bossAnim);
        }
    }

    public static IEnumerator SpawnBossSetup(GameObject bossPrefab, Transform spawnPoint, BattleManager bm)
    {
        Debug.Log("<color=orange>[System] 모든 적 처치! 보스전 전용 턴제로 전환합니다.</color>");

        if (RenderSettings.skybox != null)
        {
            float duration = 2.0f;
            float elapsedTime = 0f;
            float startBlend = RenderSettings.skybox.GetFloat("_Blend");

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float blendValue = Mathf.Lerp(startBlend, 1f, elapsedTime / duration);
                RenderSettings.skybox.SetFloat("_Blend", blendValue);

                yield return null;
            }
            RenderSettings.skybox.SetFloat("_Blend", 1f);
        }

        yield return new WaitForSeconds(1.0f);

        if (bossPrefab != null && spawnPoint != null)
        {
            GameObject bossObj = Instantiate(bossPrefab, spawnPoint.position, spawnPoint.rotation);
            BossEnemy boss = bossObj.GetComponent<BossEnemy>();

            if (boss != null)
            {
                boss.SyncBossAnimatorWithBase();
                boss.bossOriginalRot = spawnPoint.rotation;

                if (bm != null)
                {
                    bm.enemies.Clear();
                    bm.enemies.Add(boss);
                    bm.SetupBossTimeline(boss);
                    bm.SetTarget(boss);
                }
            }
        }

        yield return new WaitForSeconds(1.0f);
    }

    public IEnumerator ExecuteMeleeAttackSequence(Transform playerTransform, QTEManager qteManager)
    {
        Vector3 startPos = transform.position;
        Vector3 targetPos = playerTransform.position + playerTransform.forward * attackDistance;
        targetPos.y = transform.position.y;

        transform.LookAt(new Vector3(playerTransform.position.x, transform.position.y, playerTransform.position.z));

        if (bossAnim != null)
        {
            bossAnim.SetTrigger("Attack");
            Debug.Log("<color=cyan>[Animation] 돌진과 동시에 보스 공격 애니메이션 트리거 선발동</color>");
        }

        float elapsed = 0f;
        while (elapsed < 0.25f)
        {
            transform.position = Vector3.Lerp(startPos, targetPos, elapsed / 0.25f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = targetPos;

        yield return new WaitForSeconds(0.12f);

        if (bossAnim != null) bossAnim.updateMode = AnimatorUpdateMode.UnscaledTime;

        Time.timeScale = 0.15f;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        bool isQteSuccess = false;
        bool isQteFinished = false;

        if (qteManager != null)
        {
            qteManager.StartQTE((result) =>
            {
                isQteSuccess = result;
                isQteFinished = true;
            });

            while (!isQteFinished)
            {
                yield return null;
            }
        }
        else
        {
            yield return new WaitForSecondsRealtime(1.0f);
            isQteSuccess = false;
        }

        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        if (bossAnim != null) bossAnim.updateMode = AnimatorUpdateMode.Normal;

        BattleCharacter player = playerTransform.GetComponent<BattleCharacter>();
        if (isQteSuccess)
        {
            Debug.Log("<color=green>[QTE 성공] 플레이어가 보스의 내려치기를 회피했습니다.</color>");
        }
        else
        {
            Debug.Log("<color=red>[QTE 실패] 보스의 공격이 명중했습니다.</color>");
            if (player != null) player.TakeDamage(15);
        }

        yield return new WaitForSeconds(0.4f);

        transform.LookAt(new Vector3(startPos.x, transform.position.y, startPos.z));

        elapsed = 0f;
        while (elapsed < 0.25f)
        {
            transform.position = Vector3.Lerp(targetPos, startPos, elapsed / 0.25f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = startPos;

        transform.rotation = bossOriginalRot;
    }

    public IEnumerator ExecuteCounterSequence(Transform playerTransform)
    {
        Vector3 startPos = transform.position;
        Vector3 targetPos = playerTransform.position + playerTransform.forward * counterDistance;
        targetPos.y = transform.position.y;

        transform.LookAt(new Vector3(playerTransform.position.x, transform.position.y, playerTransform.position.z));

        if (bossAnim != null)
        {
            bossAnim.SetTrigger("Counter");
            Debug.Log("<color=cyan>[Animation] 보스 반격 공격 발동!</color>");
        }

        float elapsed = 0f;
        while (elapsed < 0.3f)
        {
            transform.position = Vector3.Lerp(startPos, targetPos, elapsed / 0.3f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = targetPos;

        yield return new WaitForSeconds(2.2f);

        BattleCharacter player = playerTransform.GetComponent<BattleCharacter>();
        if (player != null)
        {
            ExecuteCounter(player);
        }

        yield return new WaitForSeconds(0.5f);

        transform.LookAt(new Vector3(startPos.x, transform.position.y, startPos.z));

        elapsed = 0f;
        while (elapsed < 0.3f)
        {
            transform.position = Vector3.Lerp(targetPos, startPos, elapsed / 0.3f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = startPos;

        transform.rotation = bossOriginalRot;

        DisableCounterMode(); // 반격이 끝났으므로 컬러 복구 및 해제
    }

    public bool ShouldTriggerEvent() => false;
    public IEnumerator DeceptiveQTESequence(BattleCharacter p, Action<Action<bool>> q) { yield return null; }

    // ---------------- [수정: 보스 반격 태세 돌입 시 몸을 붉게 변경] ----------------
    public IEnumerator CounterStanceSequence()
    {
        isCounterMode = true;

        if (bossAnim != null)
        {
            bossAnim.SetTrigger("CounterStance");
        }

        // 보스의 온몸을 새빨갛게 물들입니다. (위험 신호 직관성 제공)
        for (int r = 0; r < bossRenderers.Count; r++)
        {
            if (bossRenderers[r] == null) continue;
            for (int m = 0; m < bossRenderers[r].materials.Length; m++)
            {
                // 약간 투명하거나 은은하게 붉은 광을 내고 싶다면 Color(1f, 0.3f, 0.3f) 등으로 커스텀 가능
                bossRenderers[r].materials[m].color = new Color(1f, 0.2f, 0.2f);
            }
        }
        Debug.Log("<color=red>[Gimmick] 보스가 온몸을 붉히며 반격 태세를 갖췄습니다!</color>");

        yield return new WaitForSeconds(0.5f);
    }

    // ---------------- [수정: 반격 해제 시 원래 고유 색상으로 복구] ----------------
    public void DisableCounterMode()
    {
        isCounterMode = false;

        // 백업해 뒀던 원래 메테리얼들의 색상으로 완벽하게 되돌립니다.
        for (int r = 0; r < bossRenderers.Count; r++)
        {
            if (bossRenderers[r] == null) continue;
            for (int m = 0; m < bossRenderers[r].materials.Length; m++)
            {
                bossRenderers[r].materials[m].color = originalColors[r][m];
            }
        }
        Debug.Log("<color=green>[Gimmick] 보스의 반격 태세가 해제되어 원래 색상으로 돌아왔습니다.</color>");
    }

    public void ExecuteCounter(BattleCharacter player) { player.TakeDamage(20); }
}