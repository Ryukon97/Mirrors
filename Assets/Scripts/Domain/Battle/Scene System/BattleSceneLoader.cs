using UnityEngine;
using UnityEngine.SceneManagement;

public class BattleSceneLoader : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private string mainSceneName = "MainScene";
    [SerializeField] private float transitionDelay = 2.0f;

    private bool isTransitioning = false;

    private void Update()
    {
        // BattleManager가 존재하고 아직 전환 중이 아닐 때만 체크
        if (BattleManager.Instance != null && !isTransitioning)
        {
            EBattleState state = BattleManager.Instance.CurrentState;

            if (state == EBattleState.Won)
            {
                // [핵심 수정] 
                // 1. BattleManager의 IsBossActuallyDead()로 보스가 죽었는지 재확인
                // 2. CanFinishBattle 프로퍼티로 소환 잠금(Lock)이 풀렸는지 확인
                if (BattleManager.Instance.IsBossActuallyDead() && BattleManager.Instance.CanFinishBattle)
                {
                    isTransitioning = true;
                    HandleBattleEnd(state);
                }
            }
            else if (state == EBattleState.Lost)
            {
                // 패배는 보스 소환 여부와 상관없으므로 즉시 처리
                isTransitioning = true;
                HandleBattleEnd(state);
            }
        }
    }

    // [정리] 기존의 불완전한 CheckFinalVictory는 BattleManager의 메서드로 대체되었으므로 삭제해도 무방합니다.

    private void HandleBattleEnd(EBattleState result)
    {
        string resultMessage = (result == EBattleState.Won) ? "승리" : "패배";
        Debug.Log($"<color=cyan>[SceneLoader]</color> 전투 종료 조건 충족: {resultMessage}");

        StartCoroutine(TransitionSequence());
    }

    private System.Collections.IEnumerator TransitionSequence()
    {
        // 설정한 딜레이만큼 대기 (승리/패배 연출용)
        yield return new WaitForSeconds(transitionDelay);

        Debug.Log($"<color=magenta><b>[Debug]</b> 최종 전투 종료 후 씬 이동 시도 (대상: {mainSceneName})</color>");

        // 실제 씬 이동 활성화 (주석을 해제하면 작동합니다)
        if (FadeInOutManager.Instance != null)
            _ = FadeInOutManager.Instance.DoSomethingBtwFadingAsync(() => SceneManager.LoadScene("World"));
        else
            SceneManager.LoadScene("World");
    }
}