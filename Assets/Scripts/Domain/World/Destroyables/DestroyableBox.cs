using UnityEngine;

public class DestroyableBox : MonoBehaviour, IDestroyable
{
    [SerializeField] GameObject particle;
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
            DoDestroy();
    }
    private void DoDestroy()
    {
        Instantiate(particle, transform.position, particle.transform.rotation);
        Destroy(this.gameObject);
    }
}
