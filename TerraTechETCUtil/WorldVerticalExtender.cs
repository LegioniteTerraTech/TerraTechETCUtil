using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Cryptography;
using System.Text;
using HarmonyLib;
using TerraTechETCUtil;
using UnityEngine;
using UnityEngine.UI;

namespace TerraTechETCUtil
{
    internal class WorldVerticalExtender
    {
        internal static class TerrainSetPiecePatches
        {
            internal static Type target = typeof(TerrainSetPiece);
            // InsureSetPieceTerrainRescaled - Setup new WorldTile
            internal static IEnumerable<CodeInstruction> ApplyHeightMap_Transpiler(IEnumerable<CodeInstruction> collection)
            {
                int Stloc_SCount = 0;
                foreach (var item in collection)
                {
                    if (item.opcode == OpCodes.Stloc_S)
                    {
                        Stloc_SCount++;
                        if (Stloc_SCount == 10)
                        {
                            yield return new CodeInstruction(OpCodes.Ldc_R4, TerrainOperations.RescaleFactorInv);
                            yield return new CodeInstruction(OpCodes.Mul, null);
                            Debug_TTExt.Log("Tweaked SetPieces to load in at correct scale");
                        }
                    }
                    yield return item;
                }
                if (Stloc_SCount < 10)
                    Debug_TTExt.FatalError("Failed to set SetPieces to load in at correct scale");
            }
        }

        internal static class BiomeMapPatches
        {

            internal static Type target = typeof(BiomeMap);
            //InsureScenerySpawnsNormally
            internal static void EstimateSteepness_Postfix(ref float __result)
            {
                __result = __result * TerrainOperations.RescaleFactor;
            }
            //InsureScenerySpawnsNormally2
            internal static IEnumerable<CodeInstruction> GenerateSceneryCells_Transpiler(IEnumerable<CodeInstruction> collection)
            {
                MethodInfo MI = AccessTools.Method(typeof(MapGenerator),
                    "GeneratePoint", new Type[] { typeof(MapGenerator.GenerationContext) ,
                    typeof(Vector2)});
                int CallvirtCount = 0;
                bool did = false;
                foreach (var item in collection)
                {
                    if (item.opcode == OpCodes.Callvirt && item.operand is MethodInfo Method &&
                        Method.Name == MI.Name)
                    {
                        CallvirtCount++;
                        yield return item;
                        yield return new CodeInstruction(OpCodes.Ldc_R4, TerrainOperations.RescaleFactor);
                        yield return new CodeInstruction(OpCodes.Mul, null);
                        Debug_TTExt.Log("Fixed terrain height detection(" + (CallvirtCount + 1) + ")");
                        did = true;
                    }
                    else
                        yield return item;
                }
                if (!did)
                    Debug_TTExt.FatalError("Failed to fix terrain height detection");
            }
        }
        internal static class TileManagerPatches
        {
            internal static Type target = typeof(TileManager);

            internal static void GenerateTerrainData_Prefix(MapGenerator __instance)
            {
                if (ManWorld.inst.CurrentBiomeMap == null)
                {
                    Exception e = new NullReferenceException("GenerateTerrainData - CurrentBiomeMap is NULL");
                    DebugUtil.inst.ReRaiseException = e;
                    throw e;
                }
            }
            //CorrectHeightMath
            internal static IEnumerable<CodeInstruction> GetTerrainHeightAtPosition_Transpiler(IEnumerable<CodeInstruction> collection)
            {
                bool did = false;
                int Ldc_R4Count = 0;
                foreach (var item in collection)
                {
                    if (item.opcode == OpCodes.Ldc_R4)
                    {
                        Ldc_R4Count++;
                        if (item.operand is float floatC && floatC == TerrainOperations.TileHeightDefault)
                        {
                            item.operand = TerrainOperations.TileHeightRescaled;
                            Debug_TTExt.Log("Fixed terrain height detection[2]");
                            did = true;
                        }
                    }
                    yield return item;
                }
                if (!did)
                    Debug_TTExt.FatalError("Failed to fix terrain height detection[2]");
            }

            //ExpandWorld
            internal static IEnumerable<CodeInstruction> CreateTile_Transpiler(IEnumerable<CodeInstruction> collection)
            {
                bool did = false;
                int Ldc_R4Count = 0;
                foreach (var item in collection)
                {
                    if (item.opcode == OpCodes.Ldc_R4)
                    {
                        Ldc_R4Count++;
                        if (item.operand is float floatC && floatC == TerrainOperations.TileHeightDefault)
                        {
                            Debug_TTExt.Log("Amplified terrain generation");
                            item.operand = TerrainOperations.TileHeightRescaled;
                            did = true;
                        }
                    }
                    yield return item;
                }
                if (!did)
                    Debug_TTExt.FatalError("Failed to amplify terrain generation");
            }

            //LowerTerrain1
            internal static IEnumerable<CodeInstruction> CalcTileOrigin_Transpiler(IEnumerable<CodeInstruction> collection)
            {
                bool did = false;
                int Ldc_R4Count = 0;
                foreach (var item in collection)
                {
                    if (item.opcode == OpCodes.Ldc_R4)
                    {
                        Ldc_R4Count++;
                        if (item.operand is float floatC && floatC == TerrainOperations.TileYOffsetDefault)
                        {
                            Debug_TTExt.Log("Lowered terrain");
                            item.operand = TerrainOperations.TileYOffsetRescaled;
                            did = true;
                        }
                    }
                    yield return item;
                }
                if (!did)
                    Debug_TTExt.FatalError("Failed to lower terrain");
            }
            //LowerTerrain2
            internal static IEnumerable<CodeInstruction> CalcTileCentre_Transpiler(IEnumerable<CodeInstruction> collection)
            {
                bool did = false;
                int Ldc_R4Count = 0;
                foreach (var item in collection)
                {
                    if (item.opcode == OpCodes.Ldc_R4)
                    {
                        Ldc_R4Count++;
                        if (item.operand is float floatC && floatC == TerrainOperations.TileYOffsetDefault)
                        {
                            Debug_TTExt.Log("Lowered terrain(2)");
                            item.operand = TerrainOperations.TileYOffsetRescaled;
                            did = true;
                        }
                    }
                    yield return item;
                }
                if (!did)
                    Debug_TTExt.FatalError("Failed to lower terrain(2)");
            }
            //SaveCamAtDefaultHeight
            internal static void StoreAllLoadedTileData_Prefix()
            {
                ManSaveGame.CameraPosition camPos = ManSaveGame.inst.CurrentState.m_CameraPos;
                Vector3 pos = camPos.m_WorldPosition.TileRelativePos;
                camPos.m_WorldPosition = new WorldPosition(camPos.m_WorldPosition.TileCoord,
                    new Vector3(pos.x, TerrainOperations.LerpToDefault(pos.y), pos.z));
            }
        }

        internal static class MapGeneratorPatches
        {
            internal static Type target = typeof(MapGenerator);
            // RemoveTerrainLimitations
            [HarmonyPriority(Priority.HigherThanNormal)]
            internal static IEnumerable<CodeInstruction> GeneratePoint_Transpiler(IEnumerable<CodeInstruction> collection)
            {
                bool did = false;
                int bltCount = 0;
                foreach (var item in collection)
                {
                    if (item.opcode == OpCodes.Blt)
                    {
                        bltCount++;
                        if (bltCount == 1)
                        {
                            yield return item;
                            yield return new CodeInstruction(OpCodes.Ldloc_0, null);
                            yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(
                                typeof(TerrainOperations), "TerraGenRescaled", new Type[] { typeof(float) }));
                            yield return new CodeInstruction(OpCodes.Stloc_0, null);
                            /*;
                            yield return new CodeInstruction(OpCodes.Ldloc_0, null);
                            yield return new CodeInstruction(OpCodes.Ldc_R4, TerrainOperations.RescaleFactorInv);
                            yield return new CodeInstruction(OpCodes.Mul, null);
                            yield return new CodeInstruction(OpCodes.Stloc_0, null);

                            yield return new CodeInstruction(OpCodes.Ldloc_0, null);
                            yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(
                                typeof(Debug_TTExt), "Log", new Type[] { typeof(float) }));
                            */
                            Debug_TTExt.Log("Rescaled terrain generation to permit higher limits");
                            did = true;
                            continue;
                        }
                    }
                    yield return item;
                }
                if (!did)
                    Debug_TTExt.FatalError("Failed to rescale terrain generation to permit higher limits");
            }
            [HarmonyPriority(Priority.HigherThanNormal)]
            internal static void GeneratePoint_Postfix(MapGenerator __instance, ref float __result)
            {
                //*
                if (ManWorldGeneratorExt.LowerTerrainHeightClamped.TryGetValue(__instance, out float val) &&
                    __result < val)
                    __result = val;
                // */ /* */

            }
            //RemoveTerrainLimitations2
            [HarmonyPriority(Priority.HigherThanNormal)]
            internal static IEnumerable<CodeInstruction> GeneratePointLegacy_Transpiler(IEnumerable<CodeInstruction> collection)
            {
                bool did = false;
                int bltCount = 0;
                foreach (var item in collection)
                {
                    if (item.opcode == OpCodes.Blt)
                    {
                        bltCount++;
                        if (bltCount == 1)
                        {
                            yield return item;
                            yield return new CodeInstruction(OpCodes.Ldloc_0, null);
                            yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(
                                typeof(TerrainOperations), "TerraGenRescaled", new Type[] { typeof(float) }));
                            yield return new CodeInstruction(OpCodes.Stloc_0, null);
                            Debug_TTExt.Log("Rescaled terrain generation to permit higher limits(2)");
                            did = true;
                            continue;
                        }
                    }
                    yield return item;
                }
                if (!did)
                    Debug_TTExt.FatalError("Failed to rescale terrain generation to permit higher limits(2)");
            }
            [HarmonyPriority(Priority.HigherThanNormal)]
            internal static void GeneratePointLegacy_Postfix(MapGenerator __instance, ref float __result)
            {
                //*
                if (ManWorldGeneratorExt.LowerTerrainHeightClamped.TryGetValue(__instance, out float val) &&
                    __result < val)
                    __result = val;
                // */ /* */
            }
        }

        /*
        [HarmonyPatch(typeof(MapGenerator.Operation))]
        [HarmonyPatch("Evaluate", new Type[2] { typeof(float), typeof(MapGenerator.Operation.ParamBuffer) })]// Setup new WorldTile
        internal static class ExpandWorld2
        {
            private static void Postfix(ref float __result)
            {
                __result = (__result * TerrainOperations.RescaleFactor) + TerrainOperations.DownOffsetScaled;
            }
        }
        [HarmonyPatch(typeof(MapGenerator.Operation))]
        [HarmonyPatch("Evaluate", new Type[2] { typeof(float), typeof(float) })]// Setup new WorldTile
        internal static class ExpandWorld3
        {
            private static void Postfix(ref float __result)
            {
                __result = (__result * TerrainOperations.RescaleFactorInv) - TerrainOperations.DownOffsetScaled;
            }
        }
        */



        internal static class VisiblePatches
        {
            internal static Type target = typeof(Visible);
            //SaveVisiblesAtDefaultHeight
            internal static void SaveForStorage_Postfix(ManSaveGame.StoredVisible sv)
            {
                Vector3 pos = sv.m_WorldPosition.TileRelativePos;
                sv.m_WorldPosition = new WorldPosition(sv.m_WorldPosition.TileCoord,
                    new Vector3(pos.x, TerrainOperations.LerpToDefault(pos.y), pos.z));
            }
        }

        internal static class ManSaveGame_StoredVisiblePatches
        {
            internal static Type target = typeof(ManSaveGame.StoredVisible);
            //LoadVisiblesAtResizedHeight
            internal static void GetBackwardsCompatiblePosition_Postfix(ManSaveGame.StoredVisible __instance, ref Vector3 __result)
            {
                __result = __instance.m_Position;
                if (__instance.m_WorldPosition != default && __instance.m_Position == Vector3.zero)
                {
                    __result = __instance.m_WorldPosition.ScenePosition;
                }
                __result = __result.SetY(TerrainOperations.LerpToRescaled(__result.y));
            }
        }
        internal static class ManSaveGame_CameraPositionPatches
        {
            internal static Type target = typeof(ManSaveGame.CameraPosition);
            //LoadCamAtResizedHeight
            internal static bool GetBackwardsCompatiblePosition_Prefix(ManSaveGame.CameraPosition __instance, ref Vector3 __result)
            {
                __result = __instance.m_Position;
                if (__instance.m_WorldPosition != default && __instance.m_Position == Vector3.zero)
                {
                    __result = __instance.m_WorldPosition.ScenePosition;
                }
                __result = __result.SetY(TerrainOperations.LerpToRescaled(__result.y));
                return false;
            }
        }


    }
}
