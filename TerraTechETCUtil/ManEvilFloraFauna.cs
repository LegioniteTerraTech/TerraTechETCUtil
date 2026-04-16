using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// <b>WIP, INCOMPLETE</b>
/// <para>Controls all that lives, breathes and is ANGRY non-Tech</para>
/// <para>The TerraTech OG equiv to TerraTech Worlds hostile flora</para>
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
/// <summary>
/// <b>WIP, INCOMPLETE</b>
/// <para>A hostile flora or fauna visible that can attack Techs that come too close</para>
/// </summary>
public class FloraFauna : MonoBehaviour
{
    /// <summary>
    /// Every interactable has a Visible
    /// </summary>
    public Visible visible;
    /// <summary>
    /// Main transform
    /// </summary>
    public Transform trans;
    /// <summary>
    /// Team this is assigned to - <i>yes <see cref="FloraFauna"/> can be assigned teams, because they can be tamed</i>
    /// </summary>
    public int Team = -1;
    /// <summary>
    /// Current target
    /// </summary>
    public Visible target;
    /// <summary>
    /// How far the <see cref="FloraFauna"/> can see
    /// </summary>
    public float DetectionRange = 50;
    private float DetectionRangeSqr;
    /// <summary>
    /// last time this was slow updated
    /// </summary>
    public float LastSlowUpdateTime;
    /// <summary>
    /// Adds this to a <see cref="ResourceDispenser"/>
    /// </summary>
    /// <param name="RD"></param>
    /// <returns></returns>
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
    internal void OnRecycle(Visible vis)
    {
        if (vis == visible)
        { 
            ManEvilFloraFauna.UnregisterCreature(this);
        }
    }
    internal void Update()
    {
        if (LastSlowUpdateTime < Time.time)
        {
            target = null;
            LastSlowUpdateTime = Time.time + 1.45f;
            GetTarget();
        }
        if (target)
        {
            if (!target.isActive || (target.centrePosition - trans.position).sqrMagnitude > DetectionRangeSqr)
                target = null;
            else if (target == Singleton.playerTank)
            {
                ManMusic.inst.SetDanger(ManMusic.DangerContext.Circumstance.Enemy, Singleton.playerTank, Singleton.playerTank);
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
                target = item.tank.visible;
                return;
            }
        }
    }
}
