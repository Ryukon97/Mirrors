using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerState_Attack : PlayerState_Base
{
    const int MAX_ATTACK_TARGET = 3;
    const float ATTACK_RANGE = 2f;
    const float ATTACK_RADIUS = 2.2f;
    Collider[] overlapedTargetedColliders = new Collider[MAX_ATTACK_TARGET];

    LayerMask destroyableLayer;

    float enteredT;
    // = anim-frame-length / sample
    const float attackDuration = 50 / 60f;

    // = attack-occur-frame / sample
    const float attackOccurTime = 26 / 60f;
    bool attacked;

    public PlayerState_Attack(PlayerStateMachine machine, Player player
        , PlayerSubState_Base[] subStates) : base(machine, player, subStates)
    {
        destroyableLayer = 1 << LayerMask.NameToLayer("Destroyable");
    }
    public override void OnEnter()
    {
        foreach (var state in subStates)
            state.OnEnter(this, player);
        enteredT = Time.time;
        attacked = false;
        player.anim.SetTrigger("OnAttacked");
    }
    public override void OnUpdate()
    {
        foreach (var state in subStates)
            state.OnUpdate(this, player);
        if(attacked == false)
        {
            if(Time.time - enteredT > attackOccurTime)
            {
                SpawnAttack();
                attacked = true;
            }
        }
        else if(Time.time - enteredT > attackDuration)
        {
            machine.ChangeState(machine.normalState);
        }
    }
    public override void OnExit()
    {
        foreach (var state in subStates)
            state.OnExit(this, player);
    }
    void SpawnAttack()
    {
        var vfxTransform = player.AttackEffect.gameObject.transform;
        vfxTransform.position = player.playerBody.transform.position + player.playerBody.transform.forward * ATTACK_RANGE;
        vfxTransform.rotation = Quaternion.Euler(0, 0, Random.Range(-10f, 10f));
        player.AttackEffect.Play();
        TryDestroyTargets();
    }

    void TryDestroyTargets()
    {
        IDestroyable[] destroyables = new IDestroyable[MAX_ATTACK_TARGET];
        int n = Physics.OverlapSphereNonAlloc(player.AttackEffect.transform.position, ATTACK_RADIUS, overlapedTargetedColliders, destroyableLayer);
        for (int i = 0; i < n; i++)
        {
            destroyables[i] = overlapedTargetedColliders[i].gameObject.GetComponent<IDestroyable>();

            if (destroyables[i].IsReady)
                destroyables[i].TryDestroy();
        }
    }
}
