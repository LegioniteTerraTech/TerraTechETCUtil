﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System.Reflection;

namespace TerraTechETCUtil
{
    public class ManExtProj : MonoBehaviour
    {
        private static ManExtProj inst;
        public static readonly List<ProjBase> projPool = new List<ProjBase>();

        private const float SlowUpdateTime = 0.6f;
        private float SlowUpdate = 0;
        public static void InsureInit()
        {
            if (inst)
                return;
            inst = new GameObject("ManExtProj").AddComponent<ManExtProj>();
            Debug_TTExt.Log("TerraTechModExt: Created ManExtProj.");
        }
        public void Update()
        {
            if (SlowUpdate < Time.time)
            {
                SlowUpdate = Time.time + SlowUpdateTime;
                UpdateSlow();
            }
        }

        internal static void UpdateSlow()
        {
            string errorBreak = null;
            foreach (var item in projPool)
            {
                try
                {
                    item.SlowUpdate();
                }
                catch
                {
                    try
                    {
                        errorBreak = item.name;
                    }
                    catch
                    {
                        errorBreak = "ITEM NAME WAS NULL";
                    }
                    break;
                }
            }
            Debug_TTExt.Assert(errorBreak != null, "A projectile errored out - " + errorBreak);
        }

    }


    // Please let me know if you want to use any of the method calls to the derived projectile modules.  
    //  I will make it public if needed.


    /// <summary>
    /// Does nothing. DO NOT USE ALONE.
    /// </summary>
    public class ExtProj : MonoBehaviour
    {
        private ProjBase _PB;
        public ProjBase PB
        {
            get
            {
                if (_PB == null)
                {
                    _PB = GetComponent<ProjBase>();
                    if (_PB == null)
                        _PB = gameObject.AddComponent<ProjBase>().PoolEmergency(GetComponent<Projectile>());
                }
                return _PB;
            }
            set
            {
                _PB = value;
            }
        }

        public void Recycle()
        {
            if (PB?.project)
            {
                PB.project.Recycle(false);
            }
        }

        public virtual void PrePool(Projectile proj) { }
        /// <summary>
        /// Use PB (ProjBase) to access the main projectile from now on.
        /// </summary>
        public virtual void Pool() { }
        public virtual void Fire(FireData fireData) { }



        public virtual void WorldRemoval() { }
        public virtual void Impact(Collider other, Damageable damageable, Vector3 hitPoint, ref bool ForceDestroy) { }
        public virtual void ImpactOther(Collider other, Vector3 hitPoint, ref bool ForceDestroy) { }
        public virtual void ImpactDamageable(Collider other, Damageable damageable, Vector3 hitPoint, ref bool ForceDestroy) { }

        public virtual void SlowUpdate() { }
    }

    /// <summary>
    /// Does nothing. DO NOT USE ALONE.
    /// </summary>
    public class ProjBase : MonoBehaviour
    {
        public Projectile project { get; internal set; }
        public Rigidbody rbody { get; internal set; }
        public ModuleWeapon launcher { get; internal set; }
        public Tank shooter { get; internal set; }
        protected ExtProj[] projTypes;

        /// <summary>
        /// PrePool should NOT BE USED to set reference links!  Only to set up variables to copy!
        /// </summary>
        /// <param name="inst"></param>
        public static bool PrePoolTryApplyThis(Projectile inst)
        {
            ExtProj[] projTemp = inst.GetComponents<ExtProj>();
            if (projTemp != null)
            {
                var PB = inst.GetComponent<ProjBase>();
                if (!PB)
                {
                    PB = inst.gameObject.AddComponent<ProjBase>();
                    var proj = PB.GetComponent<Projectile>();
                    if (!proj)
                    {
                        BlockDebug.ThrowWarning(true, "ProjBase was called in a non-projectile. This module should not be called in any JSON.");
                    }
                    foreach (var item in projTemp)
                    {
                        item.PrePool(proj);
                    }
                }
                return true;
            }
            return false;
        }

        public void Pool(Projectile inst)
        {
            ExtProj[] projTemp = inst.GetComponents<ExtProj>();
            if (projTemp != null)
            {
                project = inst;
                rbody = GetComponent<Rigidbody>();
                projTypes = projTemp;
                foreach (var item in projTypes)
                {
                    item.PB = this;
                    item.Pool();
                }
            }
        }

        public static ProjBase Insure(Projectile inst)
        {
            var ModuleCheck = inst.GetComponent<ProjBase>();
            if (ModuleCheck == null)
            {
                ModuleCheck = inst.gameObject.AddComponent<ProjBase>();
                return ModuleCheck.PoolEmergency(inst);
            }
            return ModuleCheck;
        }
        public ProjBase PoolEmergency(Projectile inst)
        {
            PrePoolTryApplyThis(inst);
            Pool(inst);
            return this;
        }


        internal void Fire(FireData fireData, Tank shooter, ModuleWeapon firingPiece)
        {
            ManExtProj.InsureInit();
            launcher = firingPiece;
            this.shooter = shooter;
            Debug_TTExt.Assert(!shooter, "TerraTechModExt: ProjBase was given NO SHOOTER, this may cause issues!");
            //Debug_TTExt.Log("Projectile " + gameObject.name + " fired with");
            foreach (var item in projTypes)
            {
                item.Fire(fireData);
            }
            ManExtProj.projPool.Add(this);
        }

        internal void OnWorldRemoval()
        {
            foreach (var item in projTypes)
            {
                item.WorldRemoval();
            }
            ManExtProj.projPool.Remove(this);
        }
        internal void OnImpact(Collider other, Damageable damageable, Vector3 hitPoint, ref bool ForceDestroy)
        {
            foreach (var item in projTypes)
            {
                item.Impact(other, damageable, hitPoint, ref ForceDestroy);
            }
            if (damageable)
            {
                foreach (var item in projTypes)
                {
                    item.ImpactDamageable(other, damageable, hitPoint, ref ForceDestroy);
                }
            }
            else
            {
                foreach (var item in projTypes)
                {
                    item.ImpactOther(other, hitPoint, ref ForceDestroy);
                }
            }
        }

        internal void SlowUpdate()
        {
            foreach (var item in projTypes)
            {
                item.SlowUpdate();
            }
        }



        internal static FieldInfo explode = typeof(Projectile).GetField("m_Explosion", BindingFlags.NonPublic | BindingFlags.Instance);
        public void ExplodeNoRecycle()
        {
            Transform explodo = (Transform)explode.GetValue(project);
            if ((bool)explodo)
            {
                var boom = explodo.GetComponent<Explosion>();
                if ((bool)boom)
                {
                    Explosion boom2 = explodo.Spawn(null, project.trans.position, Quaternion.identity).GetComponent<Explosion>();
                    if (boom2 != null)
                    {
                        boom2.SetDamageSource(shooter);
                        boom2.SetDirectHitTarget(null);
                        boom2.gameObject.SetActive(true);
                    }
                }
                else
                {
                    Transform transCase = explodo.Spawn(null, project.trans.position, Quaternion.identity);
                    transCase.gameObject.SetActive(true);
                }
            }
        }
        public static void ExplodeNoDamage(Projectile inst)
        {
            Transform explodo = (Transform)explode.GetValue(inst);
            if ((bool)explodo)
            {
                var boom = explodo.GetComponent<Explosion>();
                if ((bool)boom)
                {
                    Explosion boom2 = explodo.UnpooledSpawnWithLocalTransform(null, inst.trans.position, Quaternion.identity).GetComponent<Explosion>();
                    if ((bool)boom2)
                    {
                        boom2.SetDamageSource(inst.Shooter);
                        boom2.SetDirectHitTarget(null);
                        boom2.gameObject.SetActive(true);
                        boom2.DoDamage = false;
                    }
                }
                else
                {
                    Transform transCase = explodo.UnpooledSpawnWithLocalTransform(null, inst.trans.position, Quaternion.identity);
                    transCase.gameObject.SetActive(true);
                }
            }
        }
    }
}