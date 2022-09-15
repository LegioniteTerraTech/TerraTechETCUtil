using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using HarmonyLib;

namespace TerraTechETCUtil
{
    public static class LegModExt
    {
        internal static Harmony harmonyInstance = new Harmony("legionite.ttmodextensions");

        private static bool patched = false;
        public static void InsurePatches()
        {
            if (patched)
                return;
            harmonyInstance.MassPatchAllWithin(typeof(AllProjectilePatches), "TerraTechModExt");
            patched = true;
        }
        public static void RemovePatches()
        {
            if (!patched)
                return;
            harmonyInstance.MassUnPatchAllWithin(typeof(AllProjectilePatches), "TerraTechModExt");
            patched = false;
        }
    }

    internal class AllProjectilePatches
    {
        internal static class ProjectilePatches
        {
            internal static Type target = typeof(Projectile);
            static FieldInfo death = typeof(Projectile).GetField("m_LifeTime", BindingFlags.NonPublic | BindingFlags.Instance);
            // Custom Projectiles

            //Make sure that WeightedProjectile is checked for and add changes
            /// <summary>
            /// PatchProjectile
            /// </summary>
            private static void OnPool_Postfix(Projectile __instance)
            {
                //DebugRandAddi.Log("RandomAdditions: Patched Projectile OnPool(WeightedProjectile)");
                if (ProjBase.PrePoolTryApplyThis(__instance))
                {
                    var ModuleCheck = __instance.gameObject.GetComponent<ProjBase>();
                    ModuleCheck.Pool(__instance);
                }
            }

            /// <summary>
            /// PatchProjectileRemove
            /// </summary>
            private static void OnRecycle_Prefix(Projectile __instance)
            {
                var ModuleCheck = __instance.GetComponent<ProjBase>();
                if (ModuleCheck)
                    ModuleCheck.OnWorldRemoval();
            }

            /// <summary>
            /// PatchProjectileCollision
            /// </summary>
            private static void HandleCollision_Prefix(Projectile __instance, ref Damageable damageable, ref Vector3 hitPoint, ref Collider otherCollider, ref bool ForceDestroy)//
            {
                //DebugRandAddi.Log("RandomAdditions: Patched Projectile HandleCollision(KeepSeekingProjectile & OHKOProjectile)");
                var ModuleCheckR = __instance.GetComponent<ProjBase>();
                if (ModuleCheckR != null)
                {
                    ModuleCheckR.OnImpact(otherCollider, damageable, hitPoint, ref ForceDestroy);
                }
            }

            /// <summary>
            /// PatchProjectileFire
            /// </summary>
            private static void Fire_Postfix(Projectile __instance, ref FireData fireData, ref ModuleWeapon weapon, ref Tank shooter)
            {
                ProjBase.Insure(__instance).Fire(fireData, shooter, weapon);
            }

        }
    }
}
