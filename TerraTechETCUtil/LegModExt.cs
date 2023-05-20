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
            try
            {
                harmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
                Debug_TTExt.Log("TerraTechETCUtil: Patched batch");
            }
            catch (Exception e)
            {
                Debug_TTExt.Log("TerraTechETCUtil failed to boot: " + e);
            }
            UIHelpersExt.Init();
            patched = true;
        }
        public static void RemovePatches()
        {
            if (!patched)
                return;
            harmonyInstance.MassUnPatchAllWithin(typeof(AllProjectilePatches), "TerraTechModExt");
            try
            {
                harmonyInstance.UnpatchAll(harmonyInstance.Id);
                Debug_TTExt.Log("TerraTechETCUtil: Unpatched batch");
            }
            catch (Exception e)
            {
                Debug_TTExt.Log("TerraTechETCUtil failed to boot: " + e);
            }
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
                //Debug_TTExt.Log("RandomAdditions: Patched Projectile OnPool(WeightedProjectile)");
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
                //Debug_TTExt.Log("RandomAdditions: Patched Projectile HandleCollision(KeepSeekingProjectile & OHKOProjectile)");
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
    public class Patches
    {
        [HarmonyPatch(typeof(Localisation))]
        [HarmonyPatch("GetLocalisedString", new Type[3] { typeof(string), typeof(string), typeof(Localisation.GlyphInfo[]) })]//
        private class ShoehornText
        {
            private static bool Prefix(UIScreenMultiplayerTechSelect __instance, ref string bank, ref string id, ref string __result)
            {
                if (id == "MOD")
                {
                    __result = bank;
                    return false;
                }
                return true;
            }
        }
    }
}
