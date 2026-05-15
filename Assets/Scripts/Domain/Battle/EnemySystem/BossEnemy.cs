using UnityEngine;
using System.Collections;
using System;

public class BossEnemy : Enemy
{
    [Header("Boss Gimmick Settings")]
    public bool isCounterMode = false;

    [SerializeField] private float attackDistance = 2.0f;
    [SerializeField] private float counterDistance = 1.5f;

    private Animator bossAnim;

    protected override void Awake()
    {
        base.Awake();
        bossAnim = GetComponent<Animator>();
        if (bossAnim == null)
        {
            bossAnim = GetComponentInChildren<Animator>();
        }
    }

    public static IEnumerator SpawnBossSetup(GameObject bossPrefab, Transform spawnPoint, BattleManager bm)
    {
        Debug.Log("<color=orange>[System] 모든 적 처치! 보스전 전용 턴제로 전환합니다.</color>");
        yield return new WaitForSeconds(1.5f);

        if (bossPrefab != null && spawnPoint != null)
        {
            GameObject bossObj = Instantiate(bossPrefab, spawnPoint.position, spawnPoint.rotation);
            BossEnemy boss = bossObj.GetComponent<BossEnemy>();

            if (boss != null && boss.bossAnim == null)
            {
                boss.bossAnim = bossObj.GetComponent<Animator>() ?? bossObj.GetComponentInChildren<Animator>();
            }

            if (boss != null && bm != null)
            {
                bm.enemies.Clear();
                bm.enemies.Add(boss);
                bm.SetupBossTimeline(boss);
                bm.SetTarget(boss);
            }
        }

        yield return new WaitForSeconds(1.0f);
    }

    /// <summary>
    /// 보스 일반 평타 기믹 함수 (출발 타이밍 및 슬로우모션 싱크 버그 수정)
    /// </summary>
    public IEnumerator ExecuteMeleeAttackSequence(Transform playerTransform, QTEManager qteManager)
    {
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        Vector3 targetPos = playerTransform.position + playerTransform.forward * attackDistance;
        targetPos.y = transform.position.y;

        transform.LookAt(new Vector3(playerTransform.position.x, transform.position.y, playerTransform.position.z));

        // [수정 포인트 1] 돌진하기 직전(혹은 동시에)에 애니메이션 트리거를 미리 발동시킵니다.
        // 유니티 믹싱/전환 지연 시간(Transition) 동안 돌진을 수행하여 타이밍을 기가 막히게 맞춥니다.
        if (bossAnim != null)
        {
            bossAnim.SetTrigger("Attack");
            Debug.Log("<color=cyan>[Animation] 돌진과 동시에 공격 애니메이션 트리거 선발동</color>");
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

        // [수정 포인트 2] 돌진 완료 후, 애니메이션 상태가 'Attack'으로 완벽히 전전(Transition)할 수 있도록 
        // 타임스케일이 1인 정상 상태에서 아주 잠깐 프레임을 대기해 줍니다. (0.1초~0.15초)
        yield return new WaitForSeconds(0.12f);

        // [수정 포인트 3] 극적인 극소 슬로우 모션 및 QTE UI 개방
        // 이때 보스 애니메이션이 아예 굳어버리는 것을 막기 위해 UnscaledTime(현실 시간 기준 재생)으로 잠시 변경합니다.
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

            // QTE 판단 동안 대기 (Unscaled 상태이므로 플레이어 반응 대기 가능)
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
        elapsed = 0f;
        while (elapsed < 0.25f)
        {
            transform.position = Vector3.Lerp(targetPos, startPos, elapsed / 0.25f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = startPos;
        transform.rotation = startRot;
    }

    /// <summary>
    /// 보스 반격기 기믹 함수 (타이밍 동기화)
    /// </summary>
    public IEnumerator ExecuteCounterSequence(Transform playerTransform)
    {
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        Vector3 targetPos = playerTransform.position + playerTransform.forward * counterDistance;
        targetPos.y = transform.position.y;

        transform.LookAt(new Vector3(playerTransform.position.x, transform.position.y, playerTransform.position.z));

        if (bossAnim != null)
        {
            bossAnim.SetTrigger("Counter");
            Debug.Log("<color=cyan>[Animation] 보스 반격 공격 발동!</color>");
        }

        float elapsed = 0f;
        while (elapsed < 0.3f) // 반격 돌진은 좀 더 빠르게 수정
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

        elapsed = 0f;
        while (elapsed < 0.3f)
        {
            transform.position = Vector3.Lerp(targetPos, startPos, elapsed / 0.3f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = startPos;
        transform.rotation = startRot;

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