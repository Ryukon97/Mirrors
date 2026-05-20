using UnityEngine;
using System.Collections;
using System;

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

    protected new void Awake()
    {
        base.Awake();

        // 부모의 Awake에서 일반 anim을 가져왔듯이, 보스도 본체 및 자식 컴포넌트에서 애니메이터를 정확히 캐싱합니다.
        bossAnim = GetComponent<Animator>();
        if (bossAnim == null)
        {
            bossAnim = GetComponentInChildren<Animator>();
        }
    }

    private void Start()
    {
        // 최초 회전값을 백업하되, 스폰 셋업에서 한 번 더 정밀하게 동기화해 줍니다.
        bossOriginalRot = transform.rotation;
    }

    /// <summary>
    /// 외부(부모 클래스)에서 보스의 애니메이터 컴포넌트에 접근할 수 있도록 동기화 통로를 열어줍니다.
    /// </summary>
    public void SyncBossAnimatorWithBase()
    {
        if (bossAnim == null)
        {
            bossAnim = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
        }

        // 중요: 부모 클래스(Enemy)가 가지고 있는 protected 'anim' 변수에도 보스의 애니메이터를 꽂아줍니다.
        // 이 처리가 되어야 체력이 0이 되었을 때 부모의 DieSequence() 내에서 보스 사망 애니메이션이 정상 호출됩니다.
        System.Reflection.FieldInfo field = typeof(Enemy).GetField("anim", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(this, bossAnim);
        }
    }

    public static IEnumerator SpawnBossSetup(GameObject bossPrefab, Transform spawnPoint, BattleManager bm)
    {
        Debug.Log("<color=orange>[System] 모든 적 처치! 보스전 전용 턴제로 전환합니다.</color>");

        // ---------------- [수정 포인트: 스카이박스 서서히 블렌딩] ----------------
        if (RenderSettings.skybox != null)
        {
            float duration = 2.0f;    // 스카이박스가 서서히 바뀌는 총 시간 (원하는 초로 조절 가능)
            float elapsedTime = 0f;
            float startBlend = RenderSettings.skybox.GetFloat("_Blend"); // 현재 블렌드 값 기점 시작

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                // 시간에 따라 0에서 1까지 비율 계산
                float blendValue = Mathf.Lerp(startBlend, 1f, elapsedTime / duration);
                RenderSettings.skybox.SetFloat("_Blend", blendValue);

                yield return null; // 다음 프레임까지 대기
            }
            RenderSettings.skybox.SetFloat("_Blend", 1f); // 최종 값 고정 안전장치
        }
        // ------------------------------------------------------------------------

        // 앞서 블렌딩 연출로 2초를 소모했으므로 원래 기다리던 3초 중 남은 1초만 추가 대기합니다.
        yield return new WaitForSeconds(1.0f);

        if (bossPrefab != null && spawnPoint != null)
        {
            GameObject bossObj = Instantiate(bossPrefab, spawnPoint.position, spawnPoint.rotation);
            BossEnemy boss = bossObj.GetComponent<BossEnemy>();

            if (boss != null)
            {
                // 1. 애니메이터 캐싱 및 부모-자식 구조간의 컴포넌트 강제 동기화
                boss.SyncBossAnimatorWithBase();

                // 2. 스폰이 완벽히 끝난 시점의 회전축을 정밀하게 다시 저장 (뒤틀림 원천 차단)
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
    /// <summary>
    /// 보스 일반 평타 기믹 함수 (복귀 시 축 뒤틀림 버그 완벽 수정)
    /// </summary>
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

        // 1. 돌진 기동
        float elapsed = 0f;
        while (elapsed < 0.25f)
        {
            transform.position = Vector3.Lerp(startPos, targetPos, elapsed / 0.25f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = targetPos;

        yield return new WaitForSeconds(0.12f);

        // 2. 극적인 극소 슬로우 모션 및 QTE UI 개방
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

        // QTE 종료 즉시 정상 시간 배율 복원 및 애니메이터를 정상 상태로 롤백
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        if (bossAnim != null) bossAnim.updateMode = AnimatorUpdateMode.Normal;

        // 결과 데미지 연산 분기
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

        // 내려친 후의 묵직한 후딜레이 모션 마무리 시간
        yield return new WaitForSeconds(0.4f);

        // 3. 원래 자리로 퇴각 복귀 기동
        transform.LookAt(new Vector3(startPos.x, transform.position.y, startPos.z));

        elapsed = 0f;
        while (elapsed < 0.25f)
        {
            transform.position = Vector3.Lerp(targetPos, startPos, elapsed / 0.25f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = startPos;

        // [핵심] 자리에 도착한 직후 보스의 뼈대 회전축을 원래대로 완전히 고정합니다.
        transform.rotation = bossOriginalRot;
    }

    /// <summary>
    /// 보스 반격기 기믹 함수 (복귀 시 축 뒤틀림 버그 완벽 수정)
    /// </summary>
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

        // 퇴각할 때 시작 지점을 바라보게 설정
        transform.LookAt(new Vector3(startPos.x, transform.position.y, startPos.z));

        elapsed = 0f;
        while (elapsed < 0.3f)
        {
            transform.position = Vector3.Lerp(targetPos, startPos, elapsed / 0.3f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = startPos;

        // [핵심] 자리에 도착한 직후 보스의 뼈대 회전축을 원래대로 완전히 고정합니다.
        transform.rotation = bossOriginalRot;

        DisableCounterMode();
    }

    public bool ShouldTriggerEvent() => false;
    public IEnumerator DeceptiveQTESequence(BattleCharacter p, Action<Action<bool>> q) { yield return null; }

    public IEnumerator CounterStanceSequence()
    {
        isCounterMode = true;
        if (bossAnim != null)
        {
            bossAnim.SetTrigger("CounterStance");
        }
        yield return new WaitForSeconds(0.5f);
    }

    public void DisableCounterMode() => isCounterMode = false;
    public void ExecuteCounter(BattleCharacter player) { player.TakeDamage(20); }
}