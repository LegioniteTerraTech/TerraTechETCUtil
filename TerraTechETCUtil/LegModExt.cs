using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using FMOD.Studio;
using UnityEngine;
#if !EDITOR
using HarmonyLib;
#endif

#if !EDITOR
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
                        FileUtils.SaveTexture(tex.texture, Path.Combine(exportDir, tex.texture.name) + ".png");
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
                    FileUtils.SaveTexture(tex, Path.Combine(exportDir, tex.name) + ".png");
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
                //InvokeHelper.Invoke(ExtractOnce, 3f);
                UIHelpersExt.InsureNetHooks();

                ResourcesHelper.ModsPostLoadEvent.Subscribe(ManAudioExt.RegisterAllSounds);

                //new ManAbilities.AbilityButton("DebugPower", ManIngameWiki.BlocksSprite, DoUIAbilityTestCall, 1.5f);
                //ManAbilities.InitAbilityBar();
                try
                {
                    harmonyInstance.MassPatchAllWithin(typeof(AllProjectilePatches), "TerraTechModExt", true);
                    Debug_TTExt.Log("TerraTechETCUtil: Mass patched");
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
                ResourcesHelper.ModsPreLoadEvent.Send();
                ResourcesHelper.ModsPreLoadEvent.Subscribe(WikiPageDamageStats.ResetAllCustomDamageables);

                ResourcesHelper.ModsPostLoadEvent.Subscribe(ManIngameWiki.InitWiki);
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
            ResourcesHelper.ModsPostLoadEvent.Unsubscribe(ManIngameWiki.InitWiki);
            WorldTerraformer.DeInit();

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
                //Debug_TTExt.Log("TTExtUtil: Patched Projectile OnPool(WeightedProjectile)");
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
                //Debug_TTExt.Log("TTExtUtil: Patched Projectile HandleCollision(KeepSeekingProjectile & OHKOProjectile)");
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
        [HarmonyPatch(typeof(ModuleBlockAttributes))]
        [HarmonyPatch("InitBlockAttributes")]//
        internal static class InsureModdedIsRight
        {
            internal static FieldInfo generator = typeof(ModuleEnergy).GetField(
                "m_OutputConditions", BindingFlags.NonPublic | BindingFlags.Instance);
            internal static FieldInfo generatorValue = typeof(ModuleEnergy).GetField(
                "m_OutputPerSecond", BindingFlags.NonPublic | BindingFlags.Instance);
            internal static FieldInfo anchorRequired = typeof(ModuleItemConsume).GetField(
                "m_NeedsToBeAnchored", BindingFlags.NonPublic | BindingFlags.Instance);

            private static void Prefix(ref ModuleBlockAttributes __instance, Visible visible)
            {
                int errorcode = 0;
                try
                {
                    try
                    {
                        errorcode = 1;
                        if (visible.ItemType >= Enum.GetValues(typeof(BlockTypes)).Length)
                        {
                            errorcode = 2;
                            int hash = visible.m_ItemType.GetHashCode();
                            BlockAttributes blockAttributeFlags = (BlockAttributes)ManSpawn.inst.VisibleTypeInfo.GetDescriptorFlags<BlockAttributes>(hash);
                            errorcode = 3;
                            var booster = __instance.GetComponent<ModuleBooster>();
                            if (booster)
                            {
                                if (booster.transform.GetComponentInChildren<BoosterJet>(true))
                                    blockAttributeFlags.SetFlagsBitShift(true, BlockAttributes.FuelConsumer);
                            }
                            errorcode = 4;
                            var energy = __instance.GetComponent<ModuleEnergy>();
                            if (energy)
                            {
                                try
                                {
                                    if (energy.UpdateConsumeEvent.HasSubscribers())
                                        blockAttributeFlags.SetFlags(BlockAttributes.PowerConsumer, true);
                                }
                                catch { }
                                if ((float)generatorValue.GetValue(energy) > 0f)
                                {
                                    blockAttributeFlags.SetFlagsBitShift(true, BlockAttributes.PowerProducer);
                                    ModuleEnergy.OutputConditionFlags flags = (ModuleEnergy.OutputConditionFlags)generator.GetValue(energy);
                                    if ((flags & ModuleEnergy.OutputConditionFlags.Thermal) != 0)
                                        blockAttributeFlags.SetFlagsBitShift(true, BlockAttributes.Steam);
                                    if ((flags & ModuleEnergy.OutputConditionFlags.Anchored) != 0)
                                        blockAttributeFlags.SetFlagsBitShift(true, BlockAttributes.Anchored);
                                }
                            }
                            errorcode = 5;

                            var consume = __instance.GetComponent<ModuleItemConsume>();
                            if (consume)
                            {
                                blockAttributeFlags.SetFlags(BlockAttributes.ResourceBased, true);
                                if ((bool)anchorRequired.GetValue(consume))
                                    blockAttributeFlags.SetFlags(BlockAttributes.Anchored, true);
                            }
                            errorcode = 6;
                            if (__instance.GetComponent<ModuleAIBot>())
                                blockAttributeFlags.SetFlags(BlockAttributes.AI, true);
                            else if (__instance.GetComponent<ModuleTechController>() &&
                                __instance.GetComponent<ModuleTechController>().m_PlayerInput)
                                blockAttributeFlags.SetFlags(BlockAttributes.PlayerCab, true);
                            errorcode = 7;
                            if (__instance.GetComponent<ModuleItemProducer>())
                                blockAttributeFlags.SetFlags(BlockAttributes.Mining, true);
                            var circuits = __instance.GetComponent<ModuleCircuitNode>();
                            if (circuits && (circuits.Dispensor || circuits.IsChargeCarrier))
                                blockAttributeFlags.SetFlags(BlockAttributes.CircuitsEnabled, true);
                            errorcode = 9;
                            if (__instance.GetComponent<ModuleEnergyStore>())
                                blockAttributeFlags.SetFlags(BlockAttributes.PowerStorage, true);
                            errorcode = 10;
                            if (__instance.GetComponent<ModuleHeart>())
                                blockAttributeFlags.SetFlags(BlockAttributes.BlockStorage, true);
                            errorcode = 11;
                            if (__instance.GetComponent<ModuleAnchor>() &&
                                __instance.GetComponent<ModuleAnchor>().AllowsRotation)
                                blockAttributeFlags.SetFlags(BlockAttributes.AnchoredMobile, true);
                            errorcode = 12;
                            ManSpawn.inst.VisibleTypeInfo.SetDescriptorFlags<BlockAttributes>(hash, (int)blockAttributeFlags);
                        }
                    }
                    catch (Exception e)
                    {
                        if (!__instance)
                            throw new NullReferenceException("__instance is null");
                        if (!visible)
                            throw new NullReferenceException("visible is null");
                        if (ManSpawn.inst?.VisibleTypeInfo == null)
                            throw new NullReferenceException("ManSpawn.inst.VisibleTypeInfo is null");
                        if (generator == null)
                            throw new NullReferenceException("generator is null");
                        if (generatorValue == null)
                            throw new NullReferenceException("generatorValue is null");
                        if (anchorRequired == null)
                            throw new NullReferenceException("anchorRequired is null");
                        throw e;
                    }
                }
                catch (Exception e) 
                { 
                    Debug_TTExt.Log("Failure on InsureModdedIsRight(InitBlockAttributes) " +
                        "while properly indexing modded block for rapid database lookup with errorCode(" + errorcode + ") - " + e);
                }
            }
        }



        [HarmonyPatch(typeof(ManPointer))]
        [HarmonyPatch("UpdateMouseEvents")]//
        internal static class LockMouseWhenOverSubMenu
        {
            private static bool Prefix(ref ManPointer __instance)
            {
                return __instance.DraggingItem != null || !ManModGUI.IsMouseOverModGUI;
            }
        }
        [HarmonyPatch(typeof(FMODEventInstance))]
        [HarmonyPatch("start")]//
        internal static class GetDatas
        {
            private static void Prefix(ref FMODEventInstance __instance)
            {
                if (SFXHelpers.FetchSound && __instance.m_EventInstance.isValid() && !__instance.m_EventPath.NullOrEmpty())
                {
                    SFXHelpers.FetchSound = false;
                    Debug_TTExt.Log("FMODEventInstance - " + __instance.m_EventPath);
                    __instance.m_EventInstance.getParameterCount(out int num);
                    for (int i = 0; i < num; i++)
                    {
                        ParameterInstance parameterInstance;
                        __instance.m_EventInstance.getParameterByIndex(i, out parameterInstance);
                        PARAMETER_DESCRIPTION parameter_DESCRIPTION;
                        parameterInstance.getDescription(out parameter_DESCRIPTION);
                        parameterInstance.getValue(out float value);
                        Debug_TTExt.Log(" - " + parameter_DESCRIPTION.name + ", [" + i + "] = " + value.ToString("F"));
                    }
                }
            }
        }


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
                if (bankName < (LocalisationEnums.StringBanks)0)
                {
                    //throw new NotImplementedException("TerraTechETCUtil.LocalizationExt is still not complete and may not be used");
                    if (!LocalisationExt.TryGetFrom(bankName,stringID, ref __result))
                        __result = "ERROR - LocalizedStringExt of ID " + stringID + " could not be found!!!";
                    return false;
                }
                return !LocalisationExt.TryGetFrom(bankName, stringID, ref __result);
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
        [HarmonyPatch(typeof(ManProfile.Profile))]
        [HarmonyPatch("SetHintSeen")]//
        private class DontSaveModdedHint
        {
            private static bool Prefix(ref GameHints.HintID hintId)
            {
                return (int)hintId < ExtUsageHint.HintsSeenETCIndexStart;
            }
        }

        [HarmonyPatch(typeof(ManWorld), "TryProjectToGround",
            new Type[] { typeof(Vector3), typeof(Vector3), typeof(bool) },
            new ArgumentType[] { ArgumentType.Ref, ArgumentType.Out, ArgumentType.Normal, })]//
        private static class InsureGroundHit
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> collection)
            {
                int Ldc_R4Count = 0;
                foreach (var item in collection)
                {
                    if (item.opcode == OpCodes.Ldc_R4)
                    {
                        if (item.operand is float floatC && floatC == 250f)
                        {
                            Ldc_R4Count++;
                            Debug_TTExt.Log("Adjusted ground raycasting(" + Ldc_R4Count + ")");
                            item.operand = 550f;
                        }
                    }
                    yield return item;
                }
            }
        }

        [HarmonyPatch(typeof(ManTimeOfDay), "UpdateBiomeColours")]
        private static class MaintainEffects
        {
            internal static void Postfix(ref DayNightColours dayColours, ref DayNightColours nightColours)
            {
                ManTimeOfDayExt.ReinforceStateActive(ref dayColours, ref nightColours);
            }
        }
    }
}
#endif