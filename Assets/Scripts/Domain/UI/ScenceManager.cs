using UnityEngine;
using UnityEngine.SceneManagement;

public class ScenceManager : MonoBehaviour
{
    public void NextSence()
    {
        SceneManager.LoadScene(1);
    }
}
