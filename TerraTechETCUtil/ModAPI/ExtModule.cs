using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TerraTechETCUtil
{
    /// <summary>
    /// Interface for grabbable <see cref="ExtModule"/>s that are called by <see cref="ExtModule.OnItemGrabbed"/> whern grabbed
    /// </summary>
    public interface IInvokeGrabbable
    {
        /// <summary>
        /// Called when block is grabbed by local player
        /// </summary>
        void OnGrabbed();
    }

    // Do not use any of these alone. They will do nothing useful.
    /// <summary>
    /// <para>Used for modded modules equivalent to <see cref="Module"/></para>
    /// </summary>
    public abstract class ExtModule : MonoBehaviour, IInvokeGrabbable
    {
        private static bool notInitStatic = true;
        private static TankBlock lastBlock = null;
        /// <summary>
        /// The last block that was grabbed
        /// </summary>
        public static TankBlock LastBlock => lastBlock;
        internal static void OnItemGrabbed(Visible vis, ManPointer.DragAction act, Vector3 pos)
        {
            if (act == ManPointer.DragAction.Grab && vis?.block)
            {
                //Debug_TTExt.Info("OnItemGrabbed - " + vis.name);
                lastBlock = vis.block;
                try
                {
                    foreach (var item in lastBlock.GetComponentsInChildren<IInvokeGrabbable>(true))
                    {
                        //Debug_TTExt.Info("IInvokeGrabbable - " + item.GetType());
                        try
                        {
                            item.OnGrabbed();
                        }
                        catch (Exception e)
                        {
                            Debug_TTExt.Log("Error on " + item.GetType().FullName + ".OnGrabbed() - " + e);
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug_TTExt.Log("Error on OnItemGrabbed() - " + e);
                }
            }
        }

        /// <summary>
        /// The cached data fast lookup for the block
        /// </summary>
        public virtual BlockDetails.Flags BlockDetailFlags => BlockDetails.Flags.None;
        /// <summary>
        /// <see cref="TankBlock"/> this is attached to
        /// </summary>
        public TankBlock block { get; private set; }
        /// <summary>
        /// <see cref="Tank"/> this is attached to. Not always present so check for this!
        /// </summary>
        public Tank tank => block.tank;
        /// <summary>
        /// <see cref="ModuleDamage"/> this is attached to
        /// </summary>
        public ModuleDamage dmg { get; private set; }

        /// <summary>
        /// Always fires first before the module.
        /// <para><b>DO NOT CALL THIS EXTERNALLY</b></para>
        /// </summary>
        public void OnPool()
        {
            if (notInitStatic)
            {
                notInitStatic = false;
                ManPointer.inst.DragEvent.Subscribe(OnItemGrabbed);
                Debug_TTExt.Log("ExtModule - Init Static");
            }
            if (!block)
            {
                block = gameObject.GetComponent<TankBlock>();
                if (!block)
                {
                    BlockDebug.ThrowWarning(true, "TerraTechModExt: Modules must be in the lowest JSONBLOCK/Deserializer GameObject layer!\nThis operation cannot be handled automatically.\nCause of error - Block " + gameObject.name);
                    enabled = false;
                    return;
                }
                dmg = gameObject.GetComponent<ModuleDamage>();
                try
                {
                    block.SubToBlockAttachConnected(OnAttach, OnDetach);
                }
                catch
                {
                    Debug_TTExt.LogError("TerraTechModExt: ExtModule - TankBlock is null");
                    enabled = false;
                    return;
                }
                enabled = true;
                Pool();
            }
        }
        /// <summary>
        /// Custom override when this module is pooled
        /// </summary>
        protected virtual void Pool() { }
        /// <inheritdoc/>
        public virtual void OnGrabbed() { }
        /// <summary>
        /// Called when the block is attached, equivalent to <see cref="TankBlock.AttachedEvent"/>
        /// </summary>
        public virtual void OnAttach() { }
        /// <summary>
        /// Called when the block is detached, equivalent to <see cref="TankBlock.DetachingEvent"/>
        /// </summary>
        public virtual void OnDetach() { }

        /// <summary>
        /// Iterate all attached AP neigboors on the block this is attached to
        /// </summary>
        /// <returns></returns>
        protected IEnumerable<TankBlock> IterateAllAttachedAPNeighboors()
        {
            for (int step = 0; step < block.attachPoints.Length; step++)
            {
                if (block.ConnectedBlocksByAP[step].IsNotNull())
                    yield return block.ConnectedBlocksByAP[step];
            }
        }
    }
    /// <summary>
    /// An advanced <see cref="ExtModule"/> type that can be placed on child <see cref="GameObject"/> off of the main <see cref="GameObject"/>.
    /// <para><b>Do not use these on the main <see cref="GameObject"/>!</b></para>
    /// <para>Used for modded modules equivalent to <see cref="Module"/> that also need to operate away from the base block GameObject position.</para>
    /// </summary>
    public abstract class ChildModule : MonoBehaviour
    {
        /// <summary>
        /// Forces this <see cref="ChildModule"/> to only be loadable on <see cref="TankBlock"/>s
        /// </summary>
        public virtual bool ForceTechOnly => true;
        /// <summary>
        /// The main block <see cref="Visible"/>
        /// </summary>
        public Visible visible { get; internal set; }
        /// <summary>
        /// The main <see cref="TankBlock"/>
        /// </summary>
        public TankBlock block { get; internal set; }
        /// <summary>
        /// The main <see cref="ResourceDispenser"/> if this is placed on one
        /// </summary>
        public ResourceDispenser resdisp => visible.resdisp;
        /// <summary>
        /// <see cref="Tank"/> this is attached to. Not always present so check for this!
        /// </summary>
        public Tank tank => block.tank;
        /// <summary>
        /// <see cref="ModuleDamage"/> this is attached to
        /// </summary>
        public ModuleDamage dmg { get; private set; }
        /// <summary>
        /// <see cref="Damageable"/> this is attached to
        /// </summary>
        public Damageable dmgMain { get; private set; }

        /// <summary>
        /// Always fires first before the module
        /// Call to hook up to the block or Projectile
        /// </summary>
        public void OnPool()
        {
            if (!visible)
            {
                visible = gameObject.GetComponentInParents<Visible>();
                if (resdisp)
                {
                    dmg = visible.GetComponent<ModuleDamage>();
                    dmgMain = visible.GetComponent<Damageable>();
                    Pool();
                    OnPostPool();
                }
                else if (gameObject.GetComponent<TankBlock>())
                {
                    BlockDebug.ThrowWarning(true, "TerraTechModExt: ChildModule must NOT be in the lowest layer of JSONBLOCK/Deserializer GameObject layer!\nThis operation cannot be handled automatically.\nCause of error - Block " + gameObject.name);
                    enabled = false;
                }
                else
                {
                    block = gameObject.GetComponentInParents<TankBlock>();
                    if (block)
                    {
                        try
                        {
                            dmg = visible.GetComponent<ModuleDamage>();
                            dmgMain = visible.GetComponent<Damageable>();
                            block.SubToBlockAttachConnected(OnAttach, OnDetach);
                        }
                        catch
                        {
                            Debug_TTExt.LogError("TerraTechModExt: ChildModule - Visible is null");
                            enabled = false;
                            return;
                        }
                        enabled = true;
                        Pool();
                        OnPostPool();
                    }
                    else
                    {
                        var proj = gameObject.GetComponentInParent<ChildProjectile>();
                        if (proj && proj.Register(this))
                        {
                            enabled = false; // We don't enable UNLESS WE HAVE A VALID BLOCK LINK
                            Pool();
                        }
                        else
                        {
                            BlockDebug.ThrowWarning(true, "TerraTechModExt: ChildModules must be in a valid Block or in a Projectile below a declared ChildProjectile!\nThis operation cannot be handled automatically.\nCause of error - Block " + transform.root.name);
                            enabled = false;
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Call to init hooks to modules WITHIN the block itself or the firing block
        /// </summary>
        public void OnPostPool()
        {
            if (visible && resdisp)
                PostPoolScenery();
            else
                PostPoolBlock();
        }

        /// <summary>
        /// Custom override when this module is pooled
        /// </summary>
        protected virtual void Pool() { }
        /// <summary>
        /// Custom override when this module is pooled for <see cref="TankBlock"/>, after <see cref="Pool"/>
        /// </summary>
        protected virtual void PostPoolBlock() { }
        /// <summary>
        /// Custom override when this module is pooled for <see cref="ResourceDispenser"/>, after <see cref="Pool"/>
        /// </summary>
        protected virtual void PostPoolScenery() { }
        /// <summary>
        /// Called when the block is attached, equivalent to <see cref="TankBlock.AttachedEvent"/>
        /// </summary>
        public virtual void OnAttach() { }
        /// <summary>
        /// Called when the block is detached, equivalent to <see cref="TankBlock.DetachingEvent"/>
        /// </summary>
        public virtual void OnDetach() { }

    }
}
