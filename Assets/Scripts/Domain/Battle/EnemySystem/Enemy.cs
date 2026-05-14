using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Enemy : MonoBehaviour
{
    // --------------- 변수 영역 --------------
    [Header("Stats")]
    // [수정] private -> protected: 자식인 BossEnemy가 현재 체력을 읽고 쓸 수 있어야 합니다.
    protected int currentHp;
    private Vector3 originalPosition;

    private const float ATTACK_DISTANCE = 1.5f;
    private const float MOVE_SPEED = 10.0f;

    [Header("UI & Indicators")]
    public GameObject hpCanvas;
    public Image hpBarImage;
    public GameObject selectionUI;

    [Header("Base Status")]
    // [수정] protected: 자식인 BossEnemy가 최대 체력을 읽어 기믹을 발동할 수 있습니다.
    [SerializeField] protected int maxHp = 100;
    public int CurrentHp { get; protected set; }

    // --------------- Unity Life Cycle --------------

    // [수정] private -> protected virtual: 보스가 이 기능을 '재정의(override)' 할 수 있게 합니다.
    protected virtual void Awake()
    {
        currentHp = maxHp; // 초기 체력 설정
        CurrentHp = maxHp;

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

    public IEnumerator AttackSequence(Transform target, Action<Action<bool>> requestQTE)
    {
        Vector3 targetPos = target.position + (transform.position - target.position).normalized * ATTACK_DISTANCE;

        while (Vector3.Distance(transform.position, targetPos) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, MOVE_SPEED * Time.deltaTime);
            yield return null;
        }

        Time.timeScale = 0.2f;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        bool isEvaded = false;
        bool qteFinished = false;

        requestQTE((result) => {
            isEvaded = result;
            qteFinished = true;
        });

        yield return new WaitUntil(() => qteFinished);

        Time.timeScale = 1.0f;
        Time.fixedDeltaTime = 0.02f;

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

        // 상위 시스템에서 참조하는 CurrentHp 속성도 동기화
        CurrentHp = currentHp;

        UpdateHpUI();

        if (currentHp <= 0)
        {
            if (BattleManager.Instance != null)
                BattleManager.Instance.RemoveEnemy(this);

            Destroy(gameObject);
        }
    }

    // [수정] private -> protected: 자식이 데미지를 입힌 후 UI를 스스로 갱신할 수 있게 합니다.
    protected void UpdateHpUI()
    {
        if (hpBarImage != null)
            hpBarImage.fillAmount = (float)currentHp / maxHp;
    }
}