using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// WIP, INCOMPLETE
/// Controls all that lives, breathes and is ANGRY non-Tech
/// </summary>
public class ManEvilFloraFauna
{
    internal static HashSet<FloraFauna> creatures = new HashSet<FloraFauna>();
    internal static void RegisterCreature(FloraFauna FF)
    {
        creatures.Add(FF);
    }
    internal static void UnregisterCreature(FloraFauna FF)
    {
        creatures.Remove(FF);
    }
    internal static void StaticUpdate()
    {
        foreach (var f in creatures)
        {

        }
    }

    internal static Bitfield<ObjectTypes> filter = new Bitfield<ObjectTypes>(new ObjectTypes[] { ObjectTypes.Vehicle });
}
public class FloraFauna : MonoBehaviour
{
    public Visible visible;
    public Transform trans;
    public int Team = -1;
    public Tank target;
    public float DetectionRange = 50;
    private float DetectionRangeSqr;
    public float LastSlowUpdateTime;
    public static FloraFauna Insure(ResourceDispenser RD)
    {
        FloraFauna comp = RD.GetComponent<FloraFauna>();
        if (!comp)
        {
            comp = RD.gameObject.AddComponent<FloraFauna>();
            comp.trans = RD.transform;
            comp.visible = RD.visible;
            RD.visible.RecycledEvent.Subscribe(comp.OnRecycle);
            comp.DetectionRangeSqr = comp.DetectionRange * comp.DetectionRange;
            ManEvilFloraFauna.RegisterCreature(comp);
        }
        return comp;
    }
    public void OnRecycle(Visible vis)
    {
        if (vis == visible)
        { 
            ManEvilFloraFauna.UnregisterCreature(this);
        }
    }
    public void Update()
    {
        if (LastSlowUpdateTime < Time.time)
        {
            target = null;
            LastSlowUpdateTime = Time.time + 1.45f;
            GetTarget();
        }
        if (target)
        {
            if (!target.visible.isActive || (target.boundsCentreWorldNoCheck - trans.position).sqrMagnitude > DetectionRangeSqr)
                target = null;
            else if (target == Singleton.playerTank)
            {
                ManMusic.inst.SetDanger(ManMusic.DangerContext.Circumstance.Enemy, target, target);
            }
        }
    }
    private void GetTarget()
    {
        target = null;
        foreach (var item in ManVisible.inst.VisiblesTouchingRadius(trans.position, DetectionRange, ManEvilFloraFauna.filter))
        {
            if (Tank.IsEnemy(Team, item.tank.Team))
            {
                target = item.tank;
                return;
            }
        }
    }
}
