using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.InputSystem;

public class QTEManager : MonoBehaviour
{
    public GameObject qtePanel;
    public RectTransform needle;
    public RectTransform successZone;

    private bool isActive = false;
    private float rotationSpeed = 300f;
    private Action<bool> onQTEFinished;

    // ---------------- [수정: 성공 구역 기준 제한 시간 변수들] ----------------
    private float totalRotatedAngle = 0f;
    private bool hasPassedSuccessZone = false;
    private float targetSuccessAngle = 0f;
    private float timeoutLimitAngle = 9999f; // 타임아웃 컷트라인 각도
    // ----------------------------------------------------------------------

    private void Start()
    {
        qtePanel.SetActive(false);
    }

    void Update()
    {
        if (!isActive) return;

        if (Time.timeScale == 0f)
        {
            return;
        }

        // 1. 바늘 회전 시키기
        float angleToRotate = rotationSpeed * Time.unscaledDeltaTime;
        needle.Rotate(Vector3.back * angleToRotate);

        // 2. 누적 회전 각도 계산
        totalRotatedAngle += angleToRotate;

        // 3. 바늘이 처음으로 성공 영역의 각도를 지나쳤는지 감지
        if (!hasPassedSuccessZone)
        {
            if (totalRotatedAngle >= targetSuccessAngle)
            {
                hasPassedSuccessZone = true;

                // [핵심] 성공 구역을 지나간 이 시점부터 정확히 380도만큼 더 돌 수 있게 제한 컷트라인을 설정합니다.
                // 이로 인해 첫 진입 기회 + 한 바퀴 돌아서 오는 2번째 기회(대략 총 380도 구간)까지 살려둡니다.
                timeoutLimitAngle = targetSuccessAngle + 380f;
                Debug.Log("<color=yellow>[QTE] 성공 구역 최초 통과! 2번째 기회 구간 개방.</color>");
            }
        }

        // 4. [수정 핵심] 성공 구역을 기준으로 380도 이상 돌아갔다면 기회 종료 (자동 실패)
        if (totalRotatedAngle >= timeoutLimitAngle)
        {
            Debug.Log("<color=red>TIMEOUT! 기회 2번을 모두 놓치고 제한 범위를 벗어나 실패했습니다.</color>");
            OnTimeoutFail();
            return;
        }

        // New Input System 입력 감지
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            CheckSuccess();
        }
    }

    public void StartQTE(Action<bool> callback)
    {
        onQTEFinished = callback;
        qtePanel.SetActive(true);
        isActive = true;

        // 변수들 초기화
        totalRotatedAngle = 0f;
        hasPassedSuccessZone = false;
        timeoutLimitAngle = 9999f; // 아직 성공 구역을 지나기 전이므로 임시 맥스값

        // 성공 영역 랜덤 설정 (0 ~ 360)
        float randomZ = UnityEngine.Random.Range(0f, 360f);
        successZone.localRotation = Quaternion.Euler(0, 0, randomZ);

        // 바늘이 Vector3.back(우회전)하므로 360도에서 빼주어야 정확한 도달 누적 각도가 나옵니다.
        targetSuccessAngle = 360f - randomZ;

        // 바늘 위치 초기화 (0도)
        needle.localRotation = Quaternion.identity;
    }

    private void CheckSuccess()
    {
        isActive = false;
        float angleDiff = Mathf.Abs(Mathf.DeltaAngle(needle.localEulerAngles.z, successZone.localEulerAngles.z));

        if (angleDiff < 10f) // 정중앙 근처 (대성공)
        {
            Debug.Log("PERFECT! 데미지 0");
            onQTEFinished?.Invoke(true);
        }
        else if (angleDiff < 35f) // 근처 (성공)
        {
            Debug.Log("GOOD! 데미지 50% 감소");
            onQTEFinished?.Invoke(true);
        }
        else // (실패)
        {
            Debug.Log("FAIL! 데미지 100%");
            onQTEFinished?.Invoke(false);
        }

        qtePanel.SetActive(false);
    }

    private void OnTimeoutFail()
    {
        isActive = false;
        onQTEFinished?.Invoke(false);
        qtePanel.SetActive(false);
    }
}