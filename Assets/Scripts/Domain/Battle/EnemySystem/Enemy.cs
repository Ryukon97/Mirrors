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
                selectionUI.transform.localPosition = new Vector3(1f, 0f, 0.2f);
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
        yield return new WaitForSeconds(0.3f);

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
        currentHp -= damage;
        currentHp = Mathf.Max(currentHp, 0);

        CurrentHp = currentHp;

        UpdateHpUI();

        if (currentHp <= 0)
        {
            if (BattleManager.Instance != null)
                BattleManager.Instance.RemoveEnemy(this);

            Destroy(gameObject);
        }
    }

    protected void UpdateHpUI()
    {
        if (hpBarImage != null)
            hpBarImage.fillAmount = (float)currentHp / maxHp;
    }
}