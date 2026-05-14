using UnityEngine;
using UnityEngine.SceneManagement;

public class WorldToBattleMonster : MonoBehaviour, IDestroyable
{
    [SerializeField] string battleSceneName;
    public bool IsReady { get; private set; }
    private void Awake()
    {
        IsReady = true;
    }
    public void TryDestroy()
    {
        GoBattleScene(earlyAttacked: true);
    }

    void GoBattleScene(bool earlyAttacked)
    {
        IsReady = false;

        W2BContextDeliver.Instance.Context.EarlyStriked = earlyAttacked;

        if (FadeInOutManager.Instance != null)
            _ = FadeInOutManager.Instance.DoSomethingBtwFadingAsync(() => SceneManager.LoadScene(battleSceneName));
        else
            SceneManager.LoadScene(battleSceneName);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GoBattleScene(earlyAttacked: false);
        }
    }

}
