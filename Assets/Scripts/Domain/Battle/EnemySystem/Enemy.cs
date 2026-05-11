using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Enemy : MonoBehaviour
{
    // --------------- 변수 영역 --------------
    [Header("Stats")]
    [SerializeField] private int maxHp = 200;
    private int currentHp;
    private Vector3 originalPosition;

    private const float ATTACK_DISTANCE = 1.5f;
    private const float MOVE_SPEED = 10.0f;

    [Header("UI & Indicators")]
    public GameObject hpCanvas;           // 머리 위 HP 캔버스
    public Image hpBarImage;              // HP 게이지 이미지
    public GameObject selectionUI;        // [변경] 선택 시 보여줄 월드 스페이스 UI 오브젝트

    public int CurrentHp => currentHp;

    // --------------- Unity Life Cycle --------------
    private void Awake()
    {
        currentHp = maxHp;

        // 시작 시 선택 UI는 꺼둡니다.
        if (selectionUI != null)
        {
            selectionUI.SetActive(false);
        }
    }

    private void Start()
    {
        originalPosition = transform.position;
        UpdateHpUI();
    }

    private void LateUpdate()
    {
        // 체력바와 선택 UI가 항상 카메라를 정면으로 바라보게 함 (빌보드)
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
                // [수정] 모델과 겹치지 않게 위치 조정
                // y는 1.0f(몸통 높이), z는 -0.5f(카메라 쪽으로 살짝 앞)로 설정
                selectionUI.transform.localPosition = new Vector3(0, 1.0f, -0.5f);
            }
        }
    }

    // [공격 시퀀스] 턴제 적 공격 로직
    public IEnumerator AttackSequence(Transform target, Action<Action<bool>> requestQTE)
    {
        // 1. 플레이어 앞으로 이동
        Vector3 targetPos = target.position + (transform.position - target.position).normalized * ATTACK_DISTANCE;

        while (Vector3.Distance(transform.position, targetPos) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, MOVE_SPEED * Time.deltaTime);
            yield return null;
        }

        // 2. 공격 직전 슬로우 모션 및 QTE 발생
        Time.timeScale = 0.2f;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        bool isEvaded = false;
        bool qteFinished = false;

        requestQTE((result) => {
            isEvaded = result;
            qteFinished = true;
        });

        yield return new WaitUntil(() => qteFinished);

        // 3. 시간 정상화
        Time.timeScale = 1.0f;
        Time.fixedDeltaTime = 0.02f;

        // 4. 대미지 판정
        if (isEvaded)
        {
            Debug.Log($"<color=cyan>[회피 성공]</color>");
        }
        else
        {
            BattleCharacter player = target.GetComponent<BattleCharacter>();
            if (player != null) player.TakeDamage(15);
        }

        yield return new WaitForSeconds(0.3f);

        // 5. 원래 위치로 복귀
        while (Vector3.Distance(transform.position, originalPosition) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, originalPosition, MOVE_SPEED * Time.deltaTime);
            yield return null;
        }
        transform.position = originalPosition;
    }

    public void TakeDamage(int damage)
    {
        currentHp -= damage;
        currentHp = Mathf.Max(currentHp, 0);
        UpdateHpUI();

        if (currentHp <= 0)
        {
            if (BattleManager.Instance != null)
                BattleManager.Instance.RemoveEnemy(this);

            Destroy(gameObject);
        }
    }

    private void UpdateHpUI()
    {
        if (hpBarImage != null)
            hpBarImage.fillAmount = (float)currentHp / maxHp;
    }
}