using UnityEngine;

public class WorldToBattleMonster : MonoBehaviour, IDestroyable
{
    public bool IsReady { get; set; }
    
    public void TryDestroy()
    {
        GoBattleScene(earlyAttacked: true);
    }

    void GoBattleScene(bool earlyAttacked)
    {
        IsReady = false;
        Debug.Log($"World To Battle, earlyAttacked: {earlyAttacked} ");

        // load scene 
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GoBattleScene(earlyAttacked: false);
        }
    }

}
