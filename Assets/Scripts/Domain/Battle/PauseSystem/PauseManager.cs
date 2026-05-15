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
            Time.timeScale = 0f;
            pausePanel.SetActive(true);
            Debug.Log("<color=yellow>게임 일시정지</color>");
        }
        else
        {
            Time.timeScale = 1f;
            pausePanel.SetActive(false);
            Debug.Log("<color=green>게임 재개</color>");
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