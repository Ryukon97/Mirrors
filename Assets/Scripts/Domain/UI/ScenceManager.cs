using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{

    public static SceneLoader Instance; //싱글톤 패턴을 위한 추가 퍼블릭, 어디서든 접근 가능한 정적변수

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    public void NextSence()
    {
        SceneManager.LoadScene("Test_KYO");
    }
}
