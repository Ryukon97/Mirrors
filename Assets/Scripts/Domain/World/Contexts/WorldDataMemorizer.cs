using UnityEngine;

public class WorldDataMemorizer : MonoBehaviour
{
    private static WorldDataMemorizer instance;
    public static WorldDataMemorizer Instance { get => instance; }
    public WorldData data;

    private void Awake()
    {
        if(instance != null)
        {
            Destroy(this.gameObject);
            return;
        }
        instance = this;
    }
}
