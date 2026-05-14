using UnityEngine;

public class W2BContextDeliver : MonoBehaviour
{
    static W2BContextDeliver instance;
    public static W2BContextDeliver Instance { get => instance; }

    public WorldToBattleContext Context;
    private void Awake()
    {
        if (instance != null)
        {
            Destroy(instance.gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(this.gameObject); 
    }
}
