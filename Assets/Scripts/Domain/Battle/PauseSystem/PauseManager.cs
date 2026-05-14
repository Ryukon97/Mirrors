using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem; // [추가] 신형 입력 시스템 네임스페이스

public class PauseManager : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject pausePanel;

    [Header("Buttons")]
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button quitButton;

    private bool isPaused = false;

    private void Awake()
    {
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
        // SceneManager.LoadScene("MainScene");
    }
}