using UnityEngine;

public class DestroyableBox : MonoBehaviour, IDestroyable
{
    public int maxHP;
    int curHP;
    private void Awake()
    {
        curHP = maxHP;
    }
    public bool IsReady => curHP > 0;

    public void TryDestroy()
    {
        curHP--;

        if (curHP <= 0)
            Debug.Log("Destroyed");
    }
}
