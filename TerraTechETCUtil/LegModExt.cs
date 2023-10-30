using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using UnityEngine;
using HarmonyLib;
using FMOD;
using FMODUnity;

namespace TerraTechETCUtil
{
    public static class LegModExt
    {
        internal static Harmony harmonyInstance = new Harmony("legionite.ttmodextensions");

        private static bool patched = false;
        public static bool BypassSetPieceChecks = false;

        // Make FMOD spill beans
        public static void GetSFX()
        {
            // Make FMOD spill beans
            if (Singleton.playerTank)
            {
                List<FMODEvent> batched = new List<FMODEvent>();
                TechAudio.TechAudioEventSimple[] simp = (TechAudio.TechAudioEventSimple[])typeof(TechAudio).GetField("m_SimpleEvents", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Singleton.playerTank.TechAudio);
                foreach (var item in simp)
                {
                    batched.Add(item.m_Event);
                }
                Type sekret = typeof(TechAudio).GetNestedType("TechAudioEvent",
                    BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.CreateInstance);
                FieldInfo sekret2 = sekret.GetField("m_Event",
                    BindingFlags.Public | BindingFlags.Instance);
                Array sek = (Array)typeof(TechAudio).GetField("m_Events", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Singleton.playerTank.TechAudio);
                for (int step = 0; step < sek.Length; step++)
                {
                    batched.Add((FMODEvent)sekret2.GetValue(sek.GetValue(new int[1] { step })));
                }
                Debug_TTExt.Log("Collected " + batched.Count + " entries: ");
                foreach (var item in batched)
                {
                    Debug_TTExt.Log(" - " + item.EventPath);
                }
                return;
            }
            InvokeHelper.Invoke(GetSFX, 0.5f);
        }

        private static int actionCount = 0;
        private static int actionCountCached = 0;
        internal static bool PreventDuplicates()
        {
            actionCount++;
            if (actionCountCached < actionCount)
            {
                actionCountCached++;
                return true;
            }
            return false;
        }

        private static string exportDir = Path.Combine(new DirectoryInfo(Application.dataPath).Parent.ToString(), "DataExtracts");
        internal static void TryExportTexture(Sprite tex)
        {
            if (PreventDuplicates())
            {
                if (tex)
                {
                    if (tex.texture)
                    {
                        if (!Directory.Exists(exportDir))
                            Directory.CreateDirectory(exportDir);
                        FileUtils.SaveTexture(tex.texture, exportDir + tex.texture.name + ".png");
                    }
                    else
                        Debug_TTExt.Log("Failed to export sprite - texture null");
                }
                else
                    Debug_TTExt.Log("Failed to export sprite - sprite null");
            }
        }
        internal static void TryExportTexture(Texture2D tex)
        {
            if (PreventDuplicates())
            {
                if (tex)
                {
                    if (!Directory.Exists(exportDir))
                        Directory.CreateDirectory(exportDir);
                    FileUtils.SaveTexture(tex, exportDir + tex.name + ".png");
                }
                else
                    Debug_TTExt.Log("Failed to export texture - texture null");
            }
        }


        private static FieldInfo FIDT = typeof(SpriteFetcher).GetField("m_DamageTypeIcons", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo FIDAT = typeof(SpriteFetcher).GetField("m_DamageableTypeIcons", BindingFlags.NonPublic | BindingFlags.Instance);
        private static void ExtractOnce()
        {
            actionCount = 0;
            try
            {
                /*
                if (PreventDuplicates())
                    InvokeHelper.Invoke(GetSFX, 5);
                */
                Sprite[] batcher = (Sprite[])FIDT.GetValue(ManUI.inst.m_SpriteFetcher);
                foreach (var item in batcher)
                {
                    TryExportTexture(item);
                }
                batcher = (Sprite[])FIDAT.GetValue(ManUI.inst.m_SpriteFetcher);
                foreach (var item in batcher)
                {
                    TryExportTexture(item);
                }
                Debug_TTExt.Log("ExtractOnce succeeded");
            }
            catch (Exception e)
            {
                Debug_TTExt.Log("ExtractOnce failed, trying again in 2 ingame seconds - " + e);
                InvokeHelper.Invoke(ExtractOnce, 2f);
            }
        }
        
        private static void DoUIAbilityTestCall()
        {
            UIHelpersExt.BigF5broningBanner("Ability - Test", false);
            ManSFX.inst.PlayUISFX(ManSFX.UISfxType.Craft);
        }
        public static void InsurePatches()
        {
            if (patched)
                return;
            try
            {
                WorldDeformer.Init();
                InvokeHelper.Invoke(ExtractOnce, 3f);
                UIHelpersExt.InsureNetHooks();

                //new ManAbilities.AbilityButton("DebugPower", ManIngameWiki.BlocksSprite, DoUIAbilityTestCall, 1.5f);
                //ManAbilities.InitAbilityBar();
                try
                {
                    harmonyInstance.MassPatchAllWithin(typeof(AllProjectilePatches), "TerraTechModExt", true);
                    Debug_TTExt.Info("TerraTechETCUtil: Mass patched");
                }
                catch (Exception e)
                {
                    throw new Exception("TerraTechETCUtil failed to perform mass patching", e);
                }
                try
                {
                    harmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
                    Debug_TTExt.Info("TerraTechETCUtil: Patched precise");
                }
                catch (Exception e)
                {
                    throw new Exception("TerraTechETCUtil failed to perform finer patches", e);
                }
                UIHelpersExt.Init();
                ManIngameWiki.InitWiki();
                patched = true;
            }
            catch (Exception e)
            {
                ModStatusChecker.EpicFail();
                throw new Exception("TerraTechETCUtil failed to boot ENTIRELY", e);
            }
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
        /*
         //Terrain Amplifier - BORKED
        [HarmonyPatch(typeof(TileManager))]
        [HarmonyPatch("CreateTile")]// Setup new WorldTile
        internal static class ExpandWorld
        {
            private static void Postfix(TileManager __instance, ref WorldTile tile)
            {
                if (tile != null)
                {
                    TerrainOperations.AmplifyTerrain(tile.Terrain);
                    if (WorldDeformer.inst && 
                        WorldDeformer.inst.TerrainModsActive.TryGetValue(tile.Coord, out var posSet))
                    {
                        posSet.Flush();
                    }
                }
            }
        }
        [HarmonyPatch(typeof(MapGenerator.Operation))]
        [HarmonyPatch("Evaluate", new Type[2] { typeof(float), typeof(MapGenerator.Operation.ParamBuffer) })]// Setup new WorldTile
        internal static class ExpandWorld2
        {
            private static void Postfix(ref float __result)
            {
                __result = __result * TerrainOperations.RescaleFactor;
            }
        }
        [HarmonyPatch(typeof(MapGenerator.Operation))]
        [HarmonyPatch("Evaluate", new Type[2] { typeof(float), typeof(float) })]// Setup new WorldTile
        internal static class ExpandWorld3
        {
            private static void Postfix(ref float __result)
            {
                __result = __result / TerrainOperations.RescaleFactor;
            }
        }
        */

        /*
        [HarmonyPatch(typeof(ManSaveGame.StoredVisible))]
        [HarmonyPatch("GetBackwardsCompatiblePosition")]// Setup new WorldTile
        internal static class ExpandWorld2
        {
            private static void Postfix(ManSaveGame.StoredVisible __instance, ref float __result)
            {
                __result += TerrainOperations.DownwardsOffset;
            }
        }
        [HarmonyPatch(typeof(ManSaveGame.StoredTile))]
        [HarmonyPatch("GetBackwardsCompatiblePosition")]// Setup new WorldTile
        internal static class ExpandWorld3
        {
            private static void Prefix(ManSaveGame.StoredTile __instance, ref ManSaveGame.StoredVisible storedVisible)
            {
                Vector3 offset = Vector3.down * TerrainOperations.DownwardsOffset;
                storedVisible.m_Position = storedVisible.m_Position + offset;
                storedVisible.m_WorldPosition = WorldPosition.FromGameWorldPosition(
                    storedVisible.m_WorldPosition.GameWorldPosition + offset);
            }
        }*/



        [HarmonyPatch(typeof(ManWorld))]
        [HarmonyPatch("IsTileUsableForNewSetPiece")]//
        private class BypassSetPieceChecks
        {
            private static bool Prefix(ManWorld __instance, ref bool __result)
            {
                if (LegModExt.BypassSetPieceChecks)
                {
                    __result = true;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(Localisation))]
        [HarmonyPatch("GetLocalisedString", new Type[3] { typeof(string), typeof(string), typeof(Localisation.GlyphInfo[]) })]//
        private class ShoehornText
        {
            private static bool Prefix(Localisation __instance, ref string bank, ref string id, ref string __result)
            {
                if (id == "MOD")
                {
                    __result = bank;
                    return false;
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(Localisation))]
        [HarmonyPatch("GetLocalisedString", new Type[3] { typeof(LocalisationEnums.StringBanks), typeof(int), typeof(Localisation.GlyphInfo[]) })]//
        private class ShoehornText2
        {
            private static bool Prefix(Localisation __instance, ref LocalisationEnums.StringBanks bankName, ref int stringID, ref string __result)
            {
                if (bankName == LocalisationExt.LocalisationExtID)
                {
                    throw new NotImplementedException("TerraTechETCUtil.LocalizationExt is still not complete and may not be used");
                    if (LocalisationExt.LOCExt.Count > stringID && stringID >= 0)
                    {
                        __result = LocalisationExt.LOCExt[stringID];
                    }
                    else
                        __result = "ERROR - LocalizedStringExt of ID " + stringID + " could not be found!!!";
                    return false;
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(UILoadingScreenHints))]
        [HarmonyPatch("GetNextHint")]//
        private class TryLeverNextHint
        {
            private static bool Prefix(UILoadingScreenHints __instance, ref string __result)
            {
                if (LoadingHintsExt.MandatoryHints.Any())
                {
                    __result = LoadingHintsExt.MandatoryHints.FirstOrDefault();
                    LoadingHintsExt.MandatoryHints.RemoveAt(0);
                    return false;
                }
                else
                {
                    int count = LoadingHintsExt.ExternalHints.Count + Localisation.inst.GetLocalisedStringBank(LocalisationEnums.StringBanks.LoadingHints).Length;
                    if (UnityEngine.Random.Range(0, 100f) <= 35f || UnityEngine.Random.Range(0, count) <= LoadingHintsExt.ExternalHints.Count)
                    {
                        __result = LoadingHintsExt.ExternalHints.GetRandomEntry();
                        return false;
                    }
                    else
                        return true;
                }
            }
        }
    }
}
