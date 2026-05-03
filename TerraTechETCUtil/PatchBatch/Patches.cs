using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using FMOD.Studio;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TerraTechETCUtil
{
    /// <summary>
    /// General patch class for <see cref="TerraTechETCUtil"/>
    /// </summary>
    public class Patches
    {
        // FOR THE MINIMAP

        [HarmonyPatch(typeof(UIHUDWorldMap))]
        [HarmonyPatch("OnPointerUpHandler")]//
        internal static class StopWaypointPlacement
        {
            internal static bool Prefix(UIHUDWorldMap __instance, PointerEventData eventData)
            {
                if (eventData.button == PointerEventData.InputButton.Right &&
                    (eventData.position - eventData.pressPosition).sqrMagnitude < 225f)
                {
                    return !ManMinimapExt.OpenedModal || ManMinimapExt.OpenedModalTime > Time.realtimeSinceStartup;
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(ManRadar))]
        [HarmonyPatch("IconTypeCount", MethodType.Getter)]//
        internal static class ExtendRadarIconsCount
        {
            internal static bool Prefix(TooltipComponent __instance, ref int __result)
            {
                if (ManMinimapExt.AddedMinimapIndexes < ManMinimapExt.VanillaMapIconCount)
                    __result = ManMinimapExt.VanillaMapIconCount;
                else
                    __result = ManMinimapExt.AddedMinimapIndexes;
                //DebugRandAddi.Log("IconTypeCount returned " + __result);
                return false;
            }
        }
        [HarmonyPatch(typeof(TooltipComponent))]
        [HarmonyPatch("OnPointerEnter")]//
        internal static class CatchHoverInMapUI
        {
            internal static void Prefix(TooltipComponent __instance)
            {
                if (__instance?.gameObject && __instance.gameObject.GetComponent<UIMiniMapElement>())
                    ManMinimapExt.LastModaledTarget = __instance.gameObject.GetComponent<UIMiniMapElement>();
            }
        }
        [HarmonyPatch(typeof(TooltipComponent))]
        [HarmonyPatch("OnPointerExit")]//
        internal static class CatchHoverInMapUI2
        {
            internal static void Prefix(TooltipComponent __instance)
            {
                if (__instance?.gameObject && __instance.gameObject.GetComponent<UIMiniMapElement>())
                    if (ManMinimapExt.LastModaledTarget == __instance.gameObject.GetComponent<UIMiniMapElement>())
                        ManMinimapExt.LastModaledTarget = null;
            }
        }
        /*
        [HarmonyPatch(typeof(UIHUDWorldMap))]
        [HarmonyPatch("TryGetWaypoint")]//
        internal static class LaunchModalOnMap
        {
            internal static void Prefix(GameObject cursorGO)
            {
                if (cursorGO != null)
                {
                    UIMiniMapElement uiminiMapElement = cursorGO.GetComponent<UIMiniMapElement>();
                    if (uiminiMapElement?.TrackedVis != null && 
                        !(uiminiMapElement.TrackedVis.ObjectType == ObjectTypes.Waypoint || 
                        uiminiMapElement.TrackedVis.RadarType == RadarTypes.MapNavTarget))
                    {
                        ManMinimapExt.BringUpMinimapModal(uiminiMapElement);
                    }
                }
            }
        }//*/

        /*
        [HarmonyPatch(typeof(ManPointer))]
        [HarmonyPatch("IsInteractionBlocked", MethodType.Getter)]//
        [HarmonyPriority(135)]
        internal static class MakeSureModUIBlocksMouse
        {
            internal static bool Prefix(ref bool __result)
            {
                if (ManModGUI.InteractionModded && ManModGUI.UIKickoffState)
                {
                    __result = true;
                    return false;
                }
                return true;
            }
        }//*/
        [HarmonyPatch(typeof(ManSpawn))]
        [HarmonyPatch("SpawnTankFromTechData")]//
        [HarmonyPriority(151)]
        internal static class MakeSureWeArentNestingOperations
        {
            internal static void Prefix()
            {
                RawTechBase.IsSpawningTech = true;
            }
            internal static void Postfix()
            {
                RawTechBase.IsSpawningTech = false;
            }
        }

        //  Block Changes
        // -------------------------------------
        //             Block Changes
        // -------------------------------------
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

            internal static void Prefix(ref ModuleBlockAttributes __instance, Visible visible)
            {
                int errorcode = 0;
                try
                {
                    try
                    {
                        errorcode = 1;
                        if (visible.ItemType >= Enum.GetValues(typeof(BlockTypes)).Length)
                        {   // Modded
                            errorcode = 2;
                            int hash = visible.m_ItemType.GetHashCode();
                            BlockAttributes blockAttributeFlags = (BlockAttributes)ManSpawn.inst.VisibleTypeInfo.GetDescriptorFlags<BlockAttributes>(hash);
                            errorcode = 3;
                            var booster = __instance.GetComponent<ModuleBooster>();
                            if (booster && booster.FuelBurnPerSecond() > 0)
                            {
                                if (booster.transform.GetComponentInChildren<BoosterJet>(true))
                                    blockAttributeFlags.SetFlagsBitShift(BlockAttributes.FuelConsumer, true);
                            }
                            errorcode = 4;
                            var energy = __instance.GetComponent<ModuleEnergy>();
                            if (energy)
                            {
                                try
                                {
                                    if (energy.UpdateConsumeEvent.HasSubscribers())
                                        blockAttributeFlags.SetFlagsBitShift(BlockAttributes.PowerConsumer, true);
                                }
                                catch { }
                                if ((float)generatorValue.GetValue(energy) > 0f)
                                {
                                    blockAttributeFlags.SetFlagsBitShift(BlockAttributes.PowerProducer, true);
                                    ModuleEnergy.OutputConditionFlags flags = (ModuleEnergy.OutputConditionFlags)generator.GetValue(energy);
                                    if ((flags & ModuleEnergy.OutputConditionFlags.Thermal) != 0)
                                        blockAttributeFlags.SetFlagsBitShift(BlockAttributes.Steam, true);
                                    if ((flags & ModuleEnergy.OutputConditionFlags.Anchored) != 0)
                                        blockAttributeFlags.SetFlagsBitShift(BlockAttributes.Anchored, true);
                                }
                            }
                            errorcode = 5;

                            var consume = __instance.GetComponent<ModuleItemConsume>();
                            if (consume)
                            {
                                blockAttributeFlags.SetFlagsBitShift(BlockAttributes.ResourceBased, true);
                                if ((bool)anchorRequired.GetValue(consume))
                                    blockAttributeFlags.SetFlagsBitShift(BlockAttributes.Anchored, true);
                            }
                            errorcode = 6;
                            if (__instance.GetComponent<ModuleAIBot>())
                                blockAttributeFlags.SetFlagsBitShift(BlockAttributes.AI, true);
                            else if (__instance.GetComponent<ModuleTechController>() &&
                                __instance.GetComponent<ModuleTechController>().m_PlayerInput)
                                blockAttributeFlags.SetFlagsBitShift(BlockAttributes.PlayerCab, true);
                            errorcode = 7;
                            if (__instance.GetComponent<ModuleItemProducer>())
                                blockAttributeFlags.SetFlagsBitShift(BlockAttributes.Mining, true);
                            var circuits = __instance.GetComponent<ModuleCircuitNode>();
                            if (circuits && (circuits.Dispensor || circuits.IsChargeCarrier))
                                blockAttributeFlags.SetFlagsBitShift(BlockAttributes.CircuitsEnabled, true);
                            errorcode = 9;
                            if (__instance.GetComponent<ModuleEnergyStore>())
                                blockAttributeFlags.SetFlagsBitShift(BlockAttributes.PowerStorage, true);
                            errorcode = 10;
                            if (__instance.GetComponent<ModuleHeart>())
                                blockAttributeFlags.SetFlagsBitShift(BlockAttributes.BlockStorage, true);
                            errorcode = 11;
                            if (__instance.GetComponent<ModuleAnchor>() &&
                                __instance.GetComponent<ModuleAnchor>().AllowsRotation)
                                blockAttributeFlags.SetFlagsBitShift(BlockAttributes.AnchoredMobile, true);
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


        //  UI Changes
        // -------------------------------------
        //               UI Changes
        // -------------------------------------

        [HarmonyPatch(typeof(Localisation))]
        [HarmonyPatch("GetLocalisedString", new Type[3] { typeof(string), typeof(string), typeof(Localisation.GlyphInfo[]) })]//
        private class ShoehornText
        {
            internal static bool Prefix(Localisation __instance, ref string bank, ref string id, ref string __result)
            {
                if (bank.StartsWith(LocalisationExt.ModTag))
                {
                    if (char.IsDigit(bank.Last()) && int.TryParse(bank.Substring(3), out int ID) &&
                        LocalisationExt.TryGetFrom(LocalisationExt.LOC_ExtGeneralID, ID, ref __result))
                    {
                        return false;
                    }
                    __result = id;
                    return false;
                }
                return true;
            }
            internal static void Postfix(Localisation __instance, ref string bank, ref string id, ref string __result)
            {
                if (DebugExtUtilities.LogAllStringLocalisationLoadEvents2)
                    Debug_TTExt.Log("GetLocalisedString(bank: " + (bank.NullOrEmpty() ? "<NULL>" : bank) +
                        ", id: " + (id.NullOrEmpty() ? "<NULL>" : id) + ") = " + (__result.NullOrEmpty() ? "<NULL>" : __result));
            }
        }

        [HarmonyPatch(typeof(Localisation))]
        [HarmonyPatch("GetLocalisedString", new Type[3] { typeof(LocalisationEnums.StringBanks), typeof(int), typeof(Localisation.GlyphInfo[]) })]//
        private class ShoehornText2
        {
            internal static bool Prefix(Localisation __instance, ref LocalisationEnums.StringBanks bankName, ref int stringID, ref string __result)
            {
                if (bankName < (LocalisationEnums.StringBanks)0)
                {
                    if (!LocalisationExt.TryGetFrom(bankName, stringID, ref __result))
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
            internal static bool Prefix(UILoadingScreenHints __instance, ref string __result)
            {
                if (LoadingHintsExt.MandatoryHints.Any())
                {
                    __result = LoadingHintsExt.MandatoryHints.FirstOrDefault()?.ToString();
                    LoadingHintsExt.MandatoryHints.RemoveAt(0);
                    return false;
                }
                else
                {
                    int count = LoadingHintsExt.ExternalHints.Count + Localisation.inst.GetLocalisedStringBank(LocalisationEnums.StringBanks.LoadingHints).Length;
                    if (UnityEngine.Random.Range(0, 100f) <= 35f || UnityEngine.Random.Range(0, count) <= LoadingHintsExt.ExternalHints.Count)
                    {
                        __result = LoadingHintsExt.ExternalHints.GetRandomEntry()?.ToString();
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


        //  Sound Tweaks
        // -------------------------------------
        //              Sound Tweaks
        // -------------------------------------
        [HarmonyPatch(typeof(FMODEventInstance))]
        [HarmonyPatch("start")]//
        internal static class GetDatas
        {
            internal static void Prefix(ref FMODEventInstance __instance)
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


        //  Gameplay Changes
        // -------------------------------------
        //            Gameplay Changes
        // -------------------------------------
        [HarmonyPatch(typeof(ManWorld))]
        [HarmonyPatch("IsTileUsableForNewSetPiece")]//
        private class BypassSetPieceChecks
        {
            internal static bool Prefix(ManWorld __instance, ref bool __result)
            {
                if (LegModExt.BypassSetPieceChecks)
                {
                    __result = true;
                    return false;
                }
                return true;
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
                        if (item.operand is float floatC)
                        {
                            if (floatC == 250f)
                            {
                                Ldc_R4Count++;
                                Debug_TTExt.Log("Adjusted ground raycasting(" + Ldc_R4Count + ")");
                                item.operand = 550f;
                            }
                            else if (floatC == 251f)
                            {
                                Ldc_R4Count++;
                                Debug_TTExt.Log("Adjusted ground raycasting(" + Ldc_R4Count + ")");
                                item.operand = 551f;
                            }
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
