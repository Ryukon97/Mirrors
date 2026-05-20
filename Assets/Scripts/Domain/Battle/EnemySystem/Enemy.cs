using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Enemy : MonoBehaviour
{
    // --------------- 변수 영역 --------------
    [Header("Stats")]
    protected int currentHp;
    private Vector3 originalPosition;
    // [축 뒤틀림 해결] 스폰 시점의 완벽한 순수 회전값을 기억할 변수
    private Quaternion originalRotation;

    private const float ATTACK_DISTANCE = 1.5f;
    private const float MOVE_SPEED = 10.0f;

    [Header("UI & Indicators")]
    public GameObject hpCanvas;
    public Image hpBarImage;
    public GameObject selectionUI;

    [Header("Base Status")]
    [SerializeField] protected int maxHp = 100;
    public int CurrentHp { get; protected set; }

    private Animator anim;

    // --------------- Unity Life Cycle --------------

    protected virtual void Awake()
    {
        currentHp = maxHp;
        CurrentHp = maxHp;

        anim = GetComponent<Animator>();

        if (selectionUI != null)
        {
            selectionUI.SetActive(false);
        }
    }

    private void Start()
    {
        // 배치된 시점의 올바른 정면 좌표와 회전값을 정확하게 백업합니다.
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        UpdateHpUI();
    }

    private void LateUpdate()
    {
        if (Camera.main != null)
        {
            if (hpCanvas != null)
            {
                hpCanvas.transform.LookAt(hpCanvas.transform.position + Camera.main.transform.forward);
            }

            if (selectionUI != null && selectionUI.activeSelf)
            {
                selectionUI.transform.LookAt(selectionUI.transform.position + Camera.main.transform.forward);
            }
        }
    }

    // --------------- public APIs --------------

    private void OnMouseDown()
    {
        Debug.Log($"<color=orange>{gameObject.name} 클릭됨!</color>");
        if (BattleManager.Instance != null)
            BattleManager.Instance.SetTarget(this);
    }

    public void SetSelection(bool isSelected)
    {
        if (selectionUI != null)
        {
            selectionUI.SetActive(isSelected);

            if (isSelected)
            {
                selectionUI.transform.localPosition = new Vector3(1f, 0.7f, 0.2f);
            }
        }
    }

    /// <summary>
    /// 일반 몬스터 공격 루틴 (돌아왔을 때 뼈대 뒤틀림 현상 완벽 수정)
    /// </summary>
    public IEnumerator AttackSequence(Transform target, Action<Action<bool>> requestQTE)
    {
        Vector3 targetPos = target.position + (transform.position - target.position).normalized * ATTACK_DISTANCE;

        // 1. 타겟을 바라보고 돌진
        transform.LookAt(new Vector3(target.position.x, transform.position.y, target.position.z));

        if (anim != null) anim.SetTrigger("Attack");

        while (Vector3.Distance(transform.position, targetPos) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, MOVE_SPEED * Time.deltaTime);
            yield return null;
        }
        transform.position = targetPos;

        // 애니메이션이 Attack 상태로 진입하도록 대기
        yield return new WaitForSeconds(0.12f);

        // 2. QTE 및 슬로우 모션 제어
        if (anim != null) anim.updateMode = AnimatorUpdateMode.UnscaledTime;
        Time.timeScale = 0.2f;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        bool isEvaded = false;
        bool qteFinished = false;

        if (requestQTE != null)
        {
            requestQTE((result) => {
                isEvaded = result;
                qteFinished = true;
            });

            while (!qteFinished)
            {
                yield return null;
            }
        }
        else
        {
            yield return new WaitForSecondsRealtime(1.0f);
            isEvaded = false;
        }

        // 타임스케일 및 애니메이터 복원
        Time.timeScale = 1.0f;
        Time.fixedDeltaTime = 0.02f;
        if (anim != null) anim.updateMode = AnimatorUpdateMode.Normal;

        // 3. 피격 연산
        if (isEvaded)
        {
            Debug.Log($"<color=cyan>[QTE 회피 성공]</color>");
        }
        else
        {
            BattleCharacter player = target.GetComponent<BattleCharacter>();
            if (player != null) player.TakeDamage(15);
        }

        // 공격 후딜레이 대기
        yield return new WaitForSeconds(0.5f);

        // 4. 본래 자리로 퇴각 복귀 기동
        // 돌아갈 때는 복귀 지점을 바라보게 만들어 시각적 어색함을 지웁니다.
        transform.LookAt(new Vector3(originalPosition.x, transform.position.y, originalPosition.z));

        while (Vector3.Distance(transform.position, originalPosition) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, originalPosition, MOVE_SPEED * Time.deltaTime);
            yield return null;
        }

        // [핵심 버그 수정 포인트] 원래 위치에 소수점 오차 없이 딱 붙입니다.
        transform.position = originalPosition;

        // [핵심 버그 수정 포인트] 자리에 '완전히 도착한 순간' 저장해 뒀던 원래 회전축 값으로 강제 고정합니다.
        // 애니메이션 회전 잔여값이나 LookAt 오차로 인해 축이 꺾여있던 현상이 완벽하게 청소됩니다.
        transform.rotation = originalRotation;
    }

    public void TakeDamage(int damage)
    {
        if (currentHp <= 0) return;

        currentHp -= damage;
        currentHp = Mathf.Max(currentHp, 0);
        CurrentHp = currentHp;

        UpdateHpUI();

        // [핵심 변경] 우측 상단 로그 대신, 내 위치(transform.position) 위에 대미지 팝업!
        if (BattleManager.Instance != null)
        {
            // 내 3D 좌표(transform.position)를 넘겨주어 머리 위에 스폰하도록 유도
            BattleManager.Instance.SpawnDamageText(transform.position, damage);
        }

        if (currentHp <= 0)
        {
            StartCoroutine(DieSequence());
        }
    }

    /// <summary>
    /// 적 캐릭터 사망 연출 및 데이터 정리 코루틴
    /// </summary>
    private IEnumerator DieSequence()
    {
        Debug.Log($"<color=red>[사망] {gameObject.name}의 체력이 0이 되어 사망 애니메이션을 재생합니다.</color>");

        // 1. 배틀 매니저의 타임라인 및 적 리스트에서 먼저 제외하여 턴이 꼬이지 않게 합니다.
        if (BattleManager.Instance != null)
        {
            BattleManager.Instance.RemoveEnemy(this);
        }

        // 2. 조준선 UI가 켜져 있었다면 즉시 꺼줍니다.
        if (selectionUI != null)
        {
            selectionUI.SetActive(false);
        }

        // 3. 사망 애니메이션 트리거 작동
        if (anim != null)
        {
            // 유니티 애니메이터에 'Die' 트리거가 세팅되어 있어야 합니다.
            anim.SetTrigger("Die");

            // 애니메이션이 'Die' 상태로 완전히 진입할 수 있도록 미세 대기
            yield return new WaitForSeconds(0.1f);

            // 4. 사망 애니메이션의 재생 시간만큼 대기합니다.
            // 대략적인 사망 모션 시간(예: 1.5초)을 주거나, 애니메이터 상태를 체크할 수 있습니다.
            // 여기서는 1.5초 동안 쓰러지는 모습을 보여줍니다. (프로젝트 모션 길이에 맞춰 조절 가능)
            yield return new WaitForSeconds(3f);
        }
        else
        {
            // 애니메이터가 없을 경우를 대비한 예외 처리 대기
            yield return new WaitForSeconds(0.5f);
        }

        // 5. 모든 연출이 끝난 후 게임 오브젝트를 최종 삭제합니다.
        Destroy(gameObject);
    }

    protected void UpdateHpUI()
    {
        if (hpBarImage != null)
            hpBarImage.fillAmount = (float)currentHp / maxHp;
    }
}