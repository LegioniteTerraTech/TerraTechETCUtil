﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TerraTechETCUtil
{
    public interface IInvokeGrabbable
    {
        void OnGrabbed();
    }

    // Do not use any of these alone. They will do nothing useful.
    /// <summary>
    /// Used solely for this mod for modules compat with MP
    /// </summary>
    public class ExtModule : MonoBehaviour, IInvokeGrabbable
    {
        private static bool notInitStatic = true;
        private static TankBlock lastBlock = null;
        public static TankBlock LastBlock => lastBlock;
        public static void OnItemGrabbed(Visible vis, ManPointer.DragAction act, Vector3 pos)
        {
            if (act == ManPointer.DragAction.Grab && vis?.block)
            {
                lastBlock = vis.block;
                foreach (var item in lastBlock.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (item is IInvokeGrabbable IG)
                        IG.OnGrabbed();
                }
                //Debug_TTExt.Log("OnItemGrabbed - " + vis.name);
            }
        }

        public TankBlock block { get; private set; }
        public Tank tank => block.tank;
        public ModuleDamage dmg { get; private set; }

        /// <summary>
        /// Always fires first before the module
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
                    BlockDebug.ThrowWarning("TerraTechModExt: Modules must be in the lowest JSONBLOCK/Deserializer GameObject layer!\nThis operation cannot be handled automatically.\nCause of error - Block " + gameObject.name);
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
        protected virtual void Pool() { }
        public virtual void OnGrabbed() { }
        public virtual void OnAttach() { }
        public virtual void OnDetach() { }


        protected TankBlock[] GetAllAttachedAPNeighboors()
        {
            List<TankBlock> fetched = new List<TankBlock>();
            for (int step = 0; step < block.attachPoints.Length; step++)
            {
                if (block.ConnectedBlocksByAP[step].IsNotNull())
                {
                    fetched.Add(block.ConnectedBlocksByAP[step]);
                }
            }
            if (fetched.Count > 0)
                return fetched.ToArray();
            return null;
        }
    }
    /// <summary>
    /// Used for MP-compatable modules that also need to operate away from the base block GameObject position.
    /// </summary>
    public class ChildModule : MonoBehaviour
    {
        public TankBlock block { get; internal set; }
        public Tank tank => block.tank;
        public ModuleDamage modDmg { get; private set; }
        public Damageable dmg { get; private set; }

        /// <summary>
        /// Always fires first before the module
        /// Call to hook up to the block or Projectile
        /// </summary>
        public void OnPool()
        {
            if (!block)
            {
                block = gameObject.GetComponent<TankBlock>();
                if (block)
                {
                    BlockDebug.ThrowWarning("TerraTechModExt: ChildModule must NOT be in the lowest layer of JSONBLOCK/Deserializer GameObject layer!\nThis operation cannot be handled automatically.\nCause of error - Block " + gameObject.name);
                    enabled = false;
                }
                else
                {
                    block = gameObject.GetComponentInParents<TankBlock>();
                    if (block)
                    {
                        try
                        {
                            modDmg = block.GetComponent<ModuleDamage>();
                            dmg = block.GetComponent<Damageable>();
                            block.SubToBlockAttachConnected(OnAttach, OnDetach);
                        }
                        catch
                        {
                            Debug_TTExt.LogError("TerraTechModExt: ChildModule - TankBlock is null");
                            enabled = false;
                            return;
                        }
                        enabled = true;
                        Pool();
                        PostPool();
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
                            BlockDebug.ThrowWarning("TerraTechModExt: ChildModules must be in a valid Block or in a Projectile below a declared ChildProjectile!\nThis operation cannot be handled automatically.\nCause of error - Block " + transform.root.name);
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
            PostPool();
        }

        protected virtual void Pool() { }
        protected virtual void PostPool() { }
        public virtual void OnAttach() { }
        public virtual void OnDetach() { }

    }
}
