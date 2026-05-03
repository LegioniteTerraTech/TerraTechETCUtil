using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TerraTechETCUtil
{
    internal class AllProjectilePatches
    {
        internal static class ProjectilePatches
        {
            internal static Type target = typeof(Projectile);
            // Custom Projectiles

            //Make sure that WeightedProjectile is checked for and add changes
            /// <summary>
            /// PatchProjectile
            /// </summary>
            private static void OnPool_Postfix(Projectile __instance)
            {
                //Debug_TTExt.Log("TTExtUtil: Patched Projectile OnPool(WeightedProjectile)");
                if (ProjBase.PrePoolTryApplyThis(__instance))
                    __instance.gameObject.GetComponent<ProjBase>().Pool(__instance);
            }

            /// <summary>
            /// PatchProjectileRemove
            /// </summary>
            private static void OnRecycle_Prefix(Projectile __instance)
            {
                __instance.GetComponent<ProjBase>()?.OnWorldRemoval();
            }

            /// <summary>
            /// PatchProjectileCollision
            /// </summary>
            private static void HandleCollision_Prefix(Projectile __instance, ref Damageable damageable, ref Vector3 hitPoint, ref Collider otherCollider, ref bool ForceDestroy)//
            {
                //Debug_TTExt.Log("TTExtUtil: Patched Projectile HandleCollision(KeepSeekingProjectile & OHKOProjectile)");
                __instance.GetComponent<ProjBase>()?.OnImpact(otherCollider, damageable, hitPoint, ref ForceDestroy);
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
