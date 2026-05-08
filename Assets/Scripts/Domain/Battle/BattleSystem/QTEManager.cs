using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.InputSystem; // [추가] New Input System 네임스페이스

public class QTEManager : MonoBehaviour
{
    public GameObject qtePanel;
    public RectTransform needle;
    public RectTransform successZone;

    private bool isActive = false;
    private float rotationSpeed = 30f;
    private Action<bool> onQTEFinished;

    void Update()
    {
        // 1. isActive가 false면 아래 로직을 아예 타지 않으므로 바늘도 멈춥니다.
        if (!isActive) return;

        // 바늘 회전
        needle.Rotate(Vector3.back * rotationSpeed * Time.deltaTime);

        // 2. New Input System 방식으로 스페이스바 입력 체크
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            CheckSuccess();
        }
    }

    public void StartQTE(Action<bool> callback)
    {
        onQTEFinished = callback;
        qtePanel.SetActive(true);
        isActive = true; // 여기서부터 Update의 로직이 돌아갑니다.

        // 성공 영역 랜덤 설정
        float randomZ = UnityEngine.Random.Range(0f, 360f);
        successZone.localRotation = Quaternion.Euler(0, 0, randomZ);

        // 바늘 초기화
        needle.localRotation = Quaternion.identity;
    }

    private void CheckSuccess()
    {
        // 3. 입력을 감지하자마자 isActive를 false로 만들어 바늘을 즉시 멈춤


        isActive = false;
        float angleDiff = Mathf.Abs(Mathf.DeltaAngle(needle.localEulerAngles.z, successZone.localEulerAngles.z));

        if (angleDiff < 15f) // 정중앙 근처 (대성공)
        {
            Debug.Log("PERFECT! 데미지 0");
            onQTEFinished?.Invoke(true);
        }
        else if (angleDiff < 45f) // 근처 (성공)
        {
            Debug.Log("GOOD! 데미지 50% 감소");
            // 이 경우 BattleManager에서 데미지를 절반만 입히는 로직을 추가할 수 있습니다.
            onQTEFinished?.Invoke(true);
        }
        else // (실패)
        {
            Debug.Log("FAIL! 데미지 100%");
            onQTEFinished?.Invoke(false);
        }

        qtePanel.SetActive(false);
    }
}