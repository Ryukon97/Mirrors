using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    // 다른 스크립트에서 접근할 수 있도록 싱글톤 또는 정적 프로퍼티 추가
    public static PauseManager Instance { get; private set; }

    [Header("UI Panels")]
    [SerializeField] private GameObject pausePanel;

    [Header("Buttons")]
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button quitButton;

    private bool isPaused = false;
    // QTE나 플레이어 스크립트에서 확인할 수 있는 일시정지 상태 변수
    public bool IsPaused => isPaused;

    private void Awake()
    {
        // 싱글톤 초기화
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (pausePanel != null) pausePanel.SetActive(false);

        if (pauseButton != null) pauseButton.onClick.AddListener(TogglePause);
        if (resumeButton != null) resumeButton.onClick.AddListener(TogglePause);
        if (quitButton != null) quitButton.onClick.AddListener(QuitToMain);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        isPaused = !isPaused;

        if (isPaused)
        {
            // [강제 고정] 버튼을 누른 즉시 타임스케일을 완전히 0으로 만들어 전역 시스템을 정지시킵니다.
            Time.timeScale = 0f;

            if (pausePanel != null) pausePanel.SetActive(true);
            Debug.Log("<color=yellow>게임 일시정지 (Time.timeScale = 0)</color>");
        }
        else
        {
            Time.timeScale = 1f;
            if (pausePanel != null) pausePanel.SetActive(false);
            Debug.Log("<color=green>게임 재개 (Time.timeScale = 1)</color>");
        }
    }

    // 외부(QTE 등)에서 강제로 타임스케일을 조절하려 할 때 퍼즈 상태라면 강제 차단하기 위한 예방 대책 함수
    public void ForcePauseScale()
    {
        if (isPaused)
        {
            Time.timeScale = 0f;
        }
    }

    public void QuitToMain()
    {
        Debug.Log("메인 메뉴로 이동");
        Time.timeScale = 1f;
        if (FadeInOutManager.Instance != null)
            _ = FadeInOutManager.Instance.DoSomethingBtwFadingAsync(() => SceneManager.LoadScene("World"));
        else
            SceneManager.LoadScene("World");
    }
}