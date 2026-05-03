using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using FMOD.Studio;


#if !EDITOR
using HarmonyLib;
#endif
using UnityEngine;

namespace TerraTechETCUtil
{
    /// <summary>
    /// Manages extension methods for various classes.
    /// </summary>
    public static class Utilities
    {

        /// <inheritdoc cref="ModStatusChecker.EncapsulateSafeInit(string, Action, Action, bool)"/>
        public static void InitWithErrorHandling(this ModBase _, string ModID, Action init, Action onFail = null, bool doChainFail = false) =>
            ModStatusChecker.EncapsulateSafeInit(ModID, init, onFail, doChainFail);

        /// <inheritdoc cref="ModStatusChecker.LookForMod(string)"/>
        public static bool LookForMod(this ModBase _, string name) =>
            ModStatusChecker.LookForMod(name);
        /// <inheritdoc cref="ModStatusChecker.LookForType(string)"/>
        public static Type LookForType(this ModBase _, string name) =>
            ModStatusChecker.LookForType(name);
        /// <inheritdoc cref="ModStatusChecker.IsModOptionsAvailable"/>
        public static bool IsModOptionsAvailable(this ModBase _) => ModStatusChecker.IsModOptionsAvailable();

        /// <summary>
        /// Tries to fix tech data from older modded Techs
        /// </summary>
        /// <param name="TD"></param>
        public static void FixupTechData(this TechData TD)
        {
            int fixCount = 0;
            var specs2 = new List<TankPreset.BlockSpec>();
            var specs = TD.m_BlockSpecs;
            for (int i = 0; i < specs.Count; i++)
            {
                TankPreset.BlockSpec spec = TD.m_BlockSpecs[i];
                BlockTypes BT = BlockIndexer.StringToBlockType(spec.block);
                if (BT != BlockTypes.GSOAIController_111)
                {
                    specs2.Add(new TankPreset.BlockSpec()
                    {
                        block = ManSpawn.inst.GetBlockPrefab(BT).name,
                        m_BlockType = BT,
                        m_SkinID = spec.m_SkinID,
                        m_VisibleID = spec.m_VisibleID,
                        orthoRotation = spec.orthoRotation,
                        position = spec.position,
                        saveState = spec.saveState,
                        textSerialData = spec.textSerialData,
                    });
                    fixCount++;
                }
            }
            specs.Clear();
            TD.m_BlockSpecs = specs2;
            if (TD.m_BlockSpecs != specs2)
                throw new InvalidOperationException("TF m_BlockSpecs reset itself!?!");
            Debug_TTExt.Log("Fixed " + fixCount + " blocks for " + TD.Name);
        }

        /// <summary>
        /// To <see cref="ColorBytes"/> type.
        /// <para><b>Lossy</b></para>
        /// </summary>
        /// <returns></returns>
        public static ColorBytes ToRGBA255(this Color toNum)
        {
            return new ColorBytes(toNum);
        }
        /// <summary>
        /// To hex color string.
        /// </summary>
        /// <returns></returns>
        public static string ToHex(this Color toHex)
        {
            return new ColorBytes(toHex).ToString();
        }

        /// <summary>
        /// Get and set a flag QUICKLY!
        /// </summary>
        /// <typeparam name="T">Enum (MUST BE INT32)</typeparam>
        /// <param name="inst">The enum to adjust</param>
        /// <param name="flagBit">The flag to adjust</param>
        /// <param name="trueState">The state to set</param>
        /// <returns>True if it changed the enum value</returns>
        public static bool GetSetFlagBitShift<T>(this ref T inst, T flagBit, bool trueState) where T : struct, Enum
        {
            int valM = (int)(object)inst;
            int valF = 1 << (int)(object)flagBit;
            bool curState = (valM & valF) != 0;
            if (curState != trueState)
            {
                inst = (T)(object)((valM & ~valF) | (trueState ? valF : 0));
                return true;
            }
            return false;
        }
        /// <summary>
        /// Get and set a flag QUICKLY!
        /// </summary>
        /// <typeparam name="T">Enum (MUST BE INT32)</typeparam>
        /// <param name="inst">The enum to adjust</param>
        /// <param name="flagBit">The flag to adjust</param>
        /// <param name="trueState">The state to set</param>
        /// <returns>True if it changed the enum value</returns>
        public static bool GetSetFlag<T>(this ref T inst, T flagBit, bool trueState) where T : struct, Enum
        {
            int valM = (int)(object)inst;
            int valF = (int)(object)flagBit;
            bool curState = (valM & valF) != 0;
            if (curState != trueState)
            {
                inst = (T)(object)((valM & ~valF) | (trueState ? valF : 0));
                return true;
            }
            return false;
        }
        /// <summary>
        /// Gets flags in enum
        /// </summary>
        /// <typeparam name="T">Enum</typeparam>
        /// <param name="inst">The enum to check</param>
        /// <param name="flag">The flag to test for</param>
        public static bool HasAnyFlag<T>(ref this T inst, T flag) where T : struct, Enum
        {
            switch (Convert.GetTypeCode(inst))
            {
                case TypeCode.SByte:
                    return ((sbyte)(object)inst & (sbyte)(object)flag) != 0;
                case TypeCode.Byte:
                    return ((byte)(object)inst & (byte)(object)flag) != 0;
                case TypeCode.Int16:
                    return ((short)(object)inst & (short)(object)flag) != 0;
                case TypeCode.UInt16:
                    return ((ushort)(object)inst & (ushort)(object)flag) != 0;
                case TypeCode.Int32:
                    return ((int)(object)inst & (int)(object)flag) != 0;
                case TypeCode.UInt32:
                    return ((uint)(object)inst & (uint)(object)flag) != 0;
                default:
                    throw new NotImplementedException(Convert.GetTypeCode(inst).ToString());
            }
        }
        /// <summary>
        /// Gets flags that aren't bit-shifted in enum
        /// </summary>
        /// <typeparam name="T">Enum</typeparam>
        /// <param name="inst">The enum to check</param>
        /// <param name="flag">The flag to test for</param>
        public static bool HasFlagBitShift<T>(ref this T inst, T flag) where T : struct, Enum
        {
            switch (Convert.GetTypeCode(inst))
            {
                case TypeCode.SByte:
                    return ((sbyte)(object)inst & (1 << (sbyte)(object)flag)) != 0;
                case TypeCode.Byte:
                    return ((byte)(object)inst & (1U << (byte)(object)flag)) != 0;
                case TypeCode.Int16:
                    return ((short)(object)inst & (1 << (short)(object)flag)) != 0;
                case TypeCode.UInt16:
                    return ((ushort)(object)inst & (1U << (ushort)(object)flag)) != 0;
                case TypeCode.Int32:
                    return ((int)(object)inst & (1 << (int)(object)flag)) != 0;
                case TypeCode.UInt32:
                    return ((uint)(object)inst & (1U << (int)(uint)(object)flag)) != 0;
                default:
                    throw new NotImplementedException(Convert.GetTypeCode(inst).ToString());
            }
        }

        /// <summary>
        /// Sets flags that are already bit-shifted in enum
        /// </summary>
        /// <typeparam name="T">Enum</typeparam>
        /// <param name="inst">The enum to set</param>
        /// <param name="flags">The flags to set</param>
        /// <param name="state">The state to set</param>
        public static void SetFlags<T>(ref this T inst, T flags, bool state) where T : struct, Enum
        {
            switch (Convert.GetTypeCode(inst))
            {
                case TypeCode.SByte:
                    inst = (T)(object)(((sbyte)(object)inst) & (~(sbyte)(object)flags) | (state ? (sbyte)(object)flags : 0));
                    break;
                case TypeCode.Byte:
                    inst = (T)(object)(((byte)(object)inst) & (~(byte)(object)flags) | (state ? (byte)(object)flags : 0));
                    break;
                case TypeCode.Int16:
                    inst = (T)(object)(((short)(object)inst) & (~(short)(object)flags) | (state ? (short)(object)flags : 0));
                    break;
                case TypeCode.UInt16:
                    inst = (T)(object)(((ushort)(object)inst) & (~(ushort)(object)flags) | (state ? (ushort)(object)flags : 0));
                    break;
                case TypeCode.Int32:
                    inst = (T)(object)(((int)(object)inst) & (~(int)(object)flags) | (state ? (int)(object)flags : 0));
                    break;
                case TypeCode.UInt32:
                    inst = (T)(object)(((uint)(object)inst) & (~(uint)(object)flags) | (state ? (uint)(object)flags : 0));
                    break;
                default:
                    throw new NotImplementedException(Convert.GetTypeCode(inst).ToString());
            }
        }
        /// <summary>
        /// Sets flags that are already bit-shifted in enum
        /// </summary>
        /// <typeparam name="T">Enum</typeparam>
        /// <param name="inst">The enum to set</param>
        /// <param name="flags">The flags to set</param>
        /// <param name="state">The state to set</param>
        public static void SetFlagsBitShift<T>(ref this T inst, T flags, bool state) where T : struct, Enum
        {
            SetFlags(ref inst, (T)(object)(1 << (int)(object)flags), state);
        }
        /// <summary>
        /// Sets flags that aren't bit-shifted in enum
        /// </summary>
        /// <typeparam name="T">Enum</typeparam>
        /// <param name="inst"></param>
        /// <param name="state">The state to set</param>
        /// <param name="flags">All targeted flags</param>
        public static void SetFlagsBitShift<T>(ref this T inst, bool state, params T[] flags) where T : struct, Enum
        {
            int combinedFlag = 0;
            switch (Convert.GetTypeCode(inst))
            {
                case TypeCode.SByte:
                    foreach (var flag in flags)
                        combinedFlag |= 1 << (sbyte)(object)flag;
                    inst.SetFlags((T)(object)(sbyte)combinedFlag, state);
                    break;
                case TypeCode.Byte:
                    foreach (var flag in flags)
                        combinedFlag |= 1 << (int)(object)flag;
                    inst.SetFlags((T)(object)(byte)combinedFlag, state);
                    break;
                case TypeCode.Int16:
                    foreach (var flag in flags)
                        combinedFlag |= 1 << (int)(object)flag;
                    inst.SetFlags((T)(object)(short)combinedFlag, state);
                    break;
                case TypeCode.UInt16:
                    foreach (var flag in flags)
                        combinedFlag |= 1 << (int)(object)flag;
                    inst.SetFlags((T)(object)(ushort)combinedFlag, state);
                    break;
                case TypeCode.Int32:
                    foreach (var flag in flags)
                        combinedFlag |= 1 << (int)(object)flag;
                    inst.SetFlags((T)(object)combinedFlag, state);
                    break;
                case TypeCode.UInt32:
                    uint combinedFlags32U = 0;
                    foreach (var flag in flags)
                        combinedFlags32U |= 1U << (int)(uint)(object)flag;
                    inst.SetFlags((T)(object)combinedFlags32U, state);
                    break;
                default:
                    throw new NotImplementedException(Convert.GetTypeCode(inst).ToString());
            }
        }


        /// <summary>
        /// Iterate a grid in terms of circular volume
        /// </summary>
        /// <param name="iVec2">Origin</param>
        /// <param name="radius">Radius outwards</param>
        /// <returns>Iterator for every element the the circle, iterating from <paramref name="iVec2"/> outwards</returns>
        public static IEnumerable<IntVector2> IterateCircleVolume(this IntVector2 iVec2, float radius)
        {
            //tan = sin/cos
            yield return iVec2;
            int outerLim = Mathf.RoundToInt(radius);
            for (int step = 1; step < outerLim; step++)
            {
                yield return new IntVector2(step, 0) + iVec2;
                yield return new IntVector2(0, step) + iVec2;
                yield return new IntVector2(-step, 0) + iVec2;
                yield return new IntVector2(0, -step) + iVec2;
            }
            //Debug_TTExt.Log("IterateCircleVolume(1) iterated " + circleElements.Count.ToString() + " entries for radius " + radius);
            // does one QUADRANT and then re-uses that
            // a^2 = b^2 + c^2
            for (int x = 1; x < outerLim; x++)
            {
                int widthOffset = x - iVec2.x;
                int yMax = Mathf.RoundToInt(Mathf.Sqrt((radius * radius) - (widthOffset * widthOffset)) + iVec2.y);
                for (int y = 1; y < yMax; y++)
                {
                    yield return new IntVector2(x, y) + iVec2;
                    yield return new IntVector2(-x, y) + iVec2;
                    yield return new IntVector2(x, -y) + iVec2;
                    yield return new IntVector2(-x, -y) + iVec2;
                }
            }
            /*Debug_TTExt.Log("IterateCircleVolume(2) iterated " + circleElements.Count.ToString() + " entries for radius " + radius);
            foreach (var item in circleElements)
            {
                Debug_TTExt.Log(item.ToString());
            }*/
        }
        private static List<IntVector2> circleElements = new List<IntVector2>();
        /// <inheritdoc cref=" IterateCircleVolume(IntVector2, float)"/>
        /// <summary>
        /// <para>This legacy version uses caching and is more memory heavy, but may act differently</para>
        /// </summary>
        /// <param name="iVec2"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public static IEnumerable<IntVector2> IterateCircleVolume_LEGACY(this IntVector2 iVec2, float radius)
        {
            //tan = sin/cos
            circleElements.Clear();
            circleElements.Add(iVec2);
            int outerLim = Mathf.RoundToInt(radius);
            for (int step = 1; step < outerLim; step++)
            {
                circleElements.Add(new IntVector2(step, 0) + iVec2);
                circleElements.Add(new IntVector2(0, step) + iVec2);
                circleElements.Add(new IntVector2(-step, 0) + iVec2);
                circleElements.Add(new IntVector2(0, -step) + iVec2);
            }
            //Debug_TTExt.Log("IterateCircleVolume(1) iterated " + circleElements.Count.ToString() + " entries for radius " + radius);
            // does one QUADRANT and then re-uses that
            // a^2 = b^2 + c^2
            for (int x = 1; x < outerLim; x++)
            {
                int widthOffset = x - iVec2.x;
                int yMax = Mathf.RoundToInt(Mathf.Sqrt((radius * radius) - (widthOffset * widthOffset)) + iVec2.y);
                for (int y = 1; y < yMax; y++)
                {
                    circleElements.Add(new IntVector2(x, y) + iVec2);
                    circleElements.Add(new IntVector2(-x, y) + iVec2);
                    circleElements.Add(new IntVector2(x, -y) + iVec2);
                    circleElements.Add(new IntVector2(-x, -y) + iVec2);
                }
            }
            /*Debug_TTExt.Log("IterateCircleVolume(2) iterated " + circleElements.Count.ToString() + " entries for radius " + radius);
            foreach (var item in circleElements)
            {
                Debug_TTExt.Log(item.ToString());
            }*/
            return circleElements;
        }
        /// <summary>
        /// Iterate a grid in terms of filling a rectangle from the <b>corner</b> given start <paramref name="iVec2"/>
        /// </summary>
        /// <param name="iVec2">Origin</param>
        /// <param name="Dimensions">extents x and y positive only</param>
        /// <returns>Iterator for every element of the rect, iterating from <paramref name="iVec2"/> outwards</returns>
        public static IEnumerable<IntVector2> IterateRectVolume(this IntVector2 iVec2, IntVector2 Dimensions)
        {
            for (int x = 0; x < Dimensions.x; x++)
            {
                for (int y = 0; y < Dimensions.y; y++)
                {
                    yield return new IntVector2(x, y) + iVec2;
                }
            }
        }
        /// <summary>
        /// <para>CHECK THIS LEGIONITE THE CODE DOESN'T MAKE SENSE!!!</para>
        /// Iterate a grid in terms of filling a rectangle from the <b>center</b> given start <paramref name="iVec2"/>
        /// </summary>
        /// <param name="iVec2">Origin</param>
        /// <param name="Dimensions">extents x and y positive only</param>
        /// <returns>Iterator for every element of the rect, iterating from <paramref name="iVec2"/> outwards</returns>
        public static IEnumerable<IntVector2> IterateRectVolumeCentered(this IntVector2 iVec2, IntVector2 Dimensions)
        {
            return IterateRectVolume(new IntVector2(iVec2.x + (Dimensions.x / 2), iVec2.y + (Dimensions.y / 2)), Dimensions);
        }

        /// <summary>
        /// Iterate a grid in terms of filling a rectangle from the closest <b>corner</b> given start <paramref name="WP"/>,
        /// trying to stay within a set diameter
        /// </summary>
        /// <param name="WP"><see cref="WorldPosition"/> to use</param>
        /// <param name="MaxTileLoadingDiameter">Only goes up to 4 for even diameter widths</param>
        public static IEnumerable<IntVector2> IterateRectVolumeDiameter(this WorldPosition WP, int MaxTileLoadingDiameter)
        {
            IntVector2 centerTile = WP.TileCoord;
            int radCentered;
            Vector2 posTechCentre;
            Vector2 posTileCentre;

            switch (MaxTileLoadingDiameter)
            {
                case 0:
                case 1:
                    yield return centerTile;
                    break;
                case 2:
                    posTechCentre = WP.ScenePosition.ToVector2XZ();
                    posTileCentre = ManWorld.inst.TileManager.CalcTileCentreScene(centerTile).ToVector2XZ();
                    if (posTechCentre.x > posTileCentre.x)
                    {
                        if (posTechCentre.y > posTileCentre.y)
                        {
                            yield return centerTile;
                            yield return centerTile + new IntVector2(1, 0);
                            yield return centerTile + new IntVector2(1, 1);
                            yield return centerTile + new IntVector2(0, 1);
                        }
                        else
                        {
                            yield return centerTile;
                            yield return centerTile + new IntVector2(1, 0);
                            yield return centerTile + new IntVector2(1, -1);
                            yield return centerTile + new IntVector2(0, -1);
                        }
                    }
                    else
                    {
                        if (posTechCentre.y > posTileCentre.y)
                        {
                            yield return centerTile;
                            yield return centerTile + new IntVector2(-1, 0);
                            yield return centerTile + new IntVector2(-1, 1);
                            yield return centerTile + new IntVector2(0, 1);
                        }
                        else
                        {
                            yield return centerTile;
                            yield return centerTile + new IntVector2(-1, 0);
                            yield return centerTile + new IntVector2(-1, -1);
                            yield return centerTile + new IntVector2(0, -1);
                        }
                    }
                    break;
                case 3:
                    radCentered = 1;
                    for (int step = -radCentered; step <= radCentered; step++)
                    {
                        for (int step2 = -radCentered; step2 <= radCentered; step2++)
                        {
                            yield return centerTile + new IntVector2(step, step2);
                        }
                    }
                    break;
                case 4:
                    radCentered = 1;
                    for (int step = -radCentered; step <= radCentered; step++)
                    {
                        for (int step2 = -radCentered; step2 <= radCentered; step2++)
                        {
                            yield return centerTile + new IntVector2(step, step2);
                        }
                    }
                    posTechCentre = WP.ScenePosition.ToVector2XZ();
                    posTileCentre = ManWorld.inst.TileManager.CalcTileCentreScene(centerTile).ToVector2XZ();
                    if (posTechCentre.x > posTileCentre.x)
                    {
                        if (posTechCentre.y > posTileCentre.y)
                        {
                            yield return centerTile + new IntVector2(2, -1);
                            yield return centerTile + new IntVector2(2, 0);
                            yield return centerTile + new IntVector2(2, 1);
                            yield return centerTile + new IntVector2(2, 2);
                            yield return centerTile + new IntVector2(1, 2);
                            yield return centerTile + new IntVector2(0, 2);
                            yield return centerTile + new IntVector2(-1, 2);
                        }
                        else
                        {
                            yield return centerTile + new IntVector2(2, 1);
                            yield return centerTile + new IntVector2(2, 0);
                            yield return centerTile + new IntVector2(2, -1);
                            yield return centerTile + new IntVector2(2, -2);
                            yield return centerTile + new IntVector2(1, -2);
                            yield return centerTile + new IntVector2(0, -2);
                            yield return centerTile + new IntVector2(-1, -2);
                        }
                    }
                    else
                    {
                        if (posTechCentre.y > posTileCentre.y)
                        {
                            yield return centerTile + new IntVector2(-2, -1);
                            yield return centerTile + new IntVector2(-2, 0);
                            yield return centerTile + new IntVector2(-2, 1);
                            yield return centerTile + new IntVector2(-2, 2);
                            yield return centerTile + new IntVector2(-1, 2);
                            yield return centerTile + new IntVector2(0, 2);
                            yield return centerTile + new IntVector2(1, 2);
                        }
                        else
                        {
                            yield return centerTile + new IntVector2(-2, 1);
                            yield return centerTile + new IntVector2(-2, 0);
                            yield return centerTile + new IntVector2(-2, -1);
                            yield return centerTile + new IntVector2(-2, -2);
                            yield return centerTile + new IntVector2(-1, -2);
                            yield return centerTile + new IntVector2(0, -2);
                            yield return centerTile + new IntVector2(1, -2);
                        }
                    }
                    break;
                default:
                    radCentered = MaxTileLoadingDiameter / 2;
                    for (int step = -radCentered; step <= radCentered; step++)
                    {
                        for (int step2 = -radCentered; step2 <= radCentered; step2++)
                        {
                            yield return centerTile + new IntVector2(step, step2);
                        }
                    }
                    break;
            }
        }
        /// <summary>
        /// Iterate a grid in terms of filling a rectangle from the closest <b>corner</b> given start <paramref name="WP"/>,
        /// trying to stay within a set diameter
        /// </summary>
        /// <param name="cache">To add the results to.  Make sure to Clear() in advance</param>
        /// <param name="WP"><see cref="WorldPosition"/> to use</param>
        /// <param name="MaxTileLoadingDiameter">Only goes up to 4 for even diameter widths</param>
        public static void IterateRectVolumeDiameter_LEGACY(List<IntVector2> cache, WorldPosition WP, int MaxTileLoadingDiameter)
        {
            IntVector2 centerTile = WP.TileCoord;
            int radCentered;
            Vector2 posTechCentre;
            Vector2 posTileCentre;

            switch (MaxTileLoadingDiameter)
            {
                case 0:
                case 1:
                    cache.Add(centerTile);
                    break;
                case 2:
                    posTechCentre = WP.ScenePosition.ToVector2XZ();
                    posTileCentre = ManWorld.inst.TileManager.CalcTileCentreScene(centerTile).ToVector2XZ();
                    if (posTechCentre.x > posTileCentre.x)
                    {
                        if (posTechCentre.y > posTileCentre.y)
                        {
                            cache.Add(centerTile);
                            cache.Add(centerTile + new IntVector2(1, 0));
                            cache.Add(centerTile + new IntVector2(1, 1));
                            cache.Add(centerTile + new IntVector2(0, 1));
                        }
                        else
                        {
                            cache.Add(centerTile);
                            cache.Add(centerTile + new IntVector2(1, 0));
                            cache.Add(centerTile + new IntVector2(1, -1));
                            cache.Add(centerTile + new IntVector2(0, -1));
                        }
                    }
                    else
                    {
                        if (posTechCentre.y > posTileCentre.y)
                        {
                            cache.Add(centerTile);
                            cache.Add(centerTile + new IntVector2(-1, 0));
                            cache.Add(centerTile + new IntVector2(-1, 1));
                            cache.Add(centerTile + new IntVector2(0, 1));
                        }
                        else
                        {
                            cache.Add(centerTile);
                            cache.Add(centerTile + new IntVector2(-1, 0));
                            cache.Add(centerTile + new IntVector2(-1, -1));
                            cache.Add(centerTile + new IntVector2(0, -1));
                        }
                    }
                    break;
                case 3:
                    radCentered = 1;
                    for (int step = -radCentered; step <= radCentered; step++)
                    {
                        for (int step2 = -radCentered; step2 <= radCentered; step2++)
                        {
                            cache.Add(centerTile + new IntVector2(step, step2));
                        }
                    }
                    break;
                case 4:
                    radCentered = 1;
                    for (int step = -radCentered; step <= radCentered; step++)
                    {
                        for (int step2 = -radCentered; step2 <= radCentered; step2++)
                        {
                            cache.Add(centerTile + new IntVector2(step, step2));
                        }
                    }
                    posTechCentre = WP.ScenePosition.ToVector2XZ();
                    posTileCentre = ManWorld.inst.TileManager.CalcTileCentreScene(centerTile).ToVector2XZ();
                    if (posTechCentre.x > posTileCentre.x)
                    {
                        if (posTechCentre.y > posTileCentre.y)
                        {
                            cache.Add(centerTile + new IntVector2(2, -1));
                            cache.Add(centerTile + new IntVector2(2, 0));
                            cache.Add(centerTile + new IntVector2(2, 1));
                            cache.Add(centerTile + new IntVector2(2, 2));
                            cache.Add(centerTile + new IntVector2(1, 2));
                            cache.Add(centerTile + new IntVector2(0, 2));
                            cache.Add(centerTile + new IntVector2(-1, 2));
                        }
                        else
                        {
                            cache.Add(centerTile + new IntVector2(2, 1));
                            cache.Add(centerTile + new IntVector2(2, 0));
                            cache.Add(centerTile + new IntVector2(2, -1));
                            cache.Add(centerTile + new IntVector2(2, -2));
                            cache.Add(centerTile + new IntVector2(1, -2));
                            cache.Add(centerTile + new IntVector2(0, -2));
                            cache.Add(centerTile + new IntVector2(-1, -2));
                        }
                    }
                    else
                    {
                        if (posTechCentre.y > posTileCentre.y)
                        {
                            cache.Add(centerTile + new IntVector2(-2, -1));
                            cache.Add(centerTile + new IntVector2(-2, 0));
                            cache.Add(centerTile + new IntVector2(-2, 1));
                            cache.Add(centerTile + new IntVector2(-2, 2));
                            cache.Add(centerTile + new IntVector2(-1, 2));
                            cache.Add(centerTile + new IntVector2(0, 2));
                            cache.Add(centerTile + new IntVector2(1, 2));
                        }
                        else
                        {
                            cache.Add(centerTile + new IntVector2(-2, 1));
                            cache.Add(centerTile + new IntVector2(-2, 0));
                            cache.Add(centerTile + new IntVector2(-2, -1));
                            cache.Add(centerTile + new IntVector2(-2, -2));
                            cache.Add(centerTile + new IntVector2(-1, -2));
                            cache.Add(centerTile + new IntVector2(0, -2));
                            cache.Add(centerTile + new IntVector2(1, -2));
                        }
                    }
                    break;
                default:
                    radCentered = MaxTileLoadingDiameter / 2;
                    for (int step = -radCentered; step <= radCentered; step++)
                    {
                        for (int step2 = -radCentered; step2 <= radCentered; step2++)
                        {
                            cache.Add(centerTile + new IntVector2(step, step2));
                        }
                    }
                    break;
            }
        }



        /// <summary>
        /// Log the contents of the target <see cref="GameObject"/>
        /// </summary>
        /// <param name="GO"></param>
        /// <param name="Maxdepth"></param>
        /// <exception cref="Exception"></exception>
        public static void LogGameObjectHierachy(GameObject GO, int Maxdepth = 16)
        {
            try
            {
                ExtractGameObjectHierachy_Internal(GO, 0, Maxdepth);
            }
            catch (Exception e)
            {
                throw new Exception("LogGameObjectHierachy FAILED - ", e);
            }
        }
        private static void ExtractGameObjectHierachy_Internal(GameObject GO, int depth, int leftoverDepth)
        {
            leftoverDepth--;
            string depthParse = "";
            for (int i = 0; i < depth; i++)
                depthParse += "  ";
            Debug_TTExt.Log(depthParse + "{");
            Transform trans = GO.transform;
            Debug_TTExt.Log(depthParse + "Name:     " + (GO.name.NullOrEmpty() ? "<NULL_NAME>" : GO.name));
            Debug_TTExt.Log(depthParse + "Pos:      " + trans.position.ToString());
            Debug_TTExt.Log(depthParse + "Rot:      " + trans.eulerAngles.ToString());
            if (trans.parent != null)
                Debug_TTExt.Log(depthParse + "Parent:   " + (trans.parent.name.NullOrEmpty() ? "<NULL_NAME>" : trans.parent.name));
            else
                Debug_TTExt.Log(depthParse + "Parent:   <NONE>");
            foreach (var item in GO.GetComponents<Component>())
                Debug_TTExt.Log(depthParse + "Component: " + item.GetType());
            if (leftoverDepth > 0)
            {
                depth++;
                for (int i = 0; i < trans.childCount; i++)
                    ExtractGameObjectHierachy_Internal(trans.GetChild(i).gameObject, depth, leftoverDepth);
            }
            Debug_TTExt.Log(depthParse + "}");
        }

        /// <summary>
        /// Remove and replace the username from a given path string
        /// </summary>
        /// <param name="toSearch">The path to clean</param>
        /// <returns>the cleaned path</returns>
        public static string RemoveUsername(string toSearch)
        {
            StringBuilder SB = new StringBuilder();
            bool ignoreThisCase = true;
            int stoppingPos = toSearch.IndexOf("Users") + 6;
            for (int step = 0; step < toSearch.Length; step++)
            {
                if (stoppingPos <= step)
                {
                    if (stoppingPos == step)
                    {
                        SB.Append("userName");
                    }
                    //DebugRandAddi.Log("TTETCUtil: " + toSearch[step] + " | " );
                    if (toSearch[step] == '/')
                        ignoreThisCase = false;
                    if (ignoreThisCase)
                        continue;
                }
                SB.Append(toSearch[step]);
            }
            return SB.ToString(); //"(Error on OS fetch request)"; 
        }


        /// <summary>
        /// When a ExtModule must respond to block attach updates, use this.
        /// <para>For block attachment updates when Tank is set to a valid reference.</para>
        /// </summary>
        /// <param name="TB">TankBlock instance</param>
        /// <param name="attachEvent">Action to call on block attach.</param>
        /// <param name="detachEvent">Action to call on block detach.</param>
        public static void SubToBlockAttachConnected(this TankBlock TB, Action attachEvent, Action detachEvent)
        {
            if (attachEvent != null)
                TB.AttachedEvent.Subscribe(attachEvent);
            if (detachEvent != null)
                TB.DetachingEvent.Subscribe(detachEvent);
        }

        /// <summary>
        /// When a ExtModule no longer needs to respond to block attach updates, use this.
        /// <para>For block attachment updates when Tank is set to a valid reference.</para>
        /// </summary>
        /// <param name="TB">TankBlock instance</param>
        /// <param name="attachEvent">Action to call on block attach.</param>
        /// <param name="detachEvent">Action to call on block detach.</param>
        public static void UnSubToBlockAttachConnected(this TankBlock TB, Action attachEvent, Action detachEvent)
        {
            if (attachEvent != null)
                TB.AttachedEvent.Unsubscribe(attachEvent);
            if (detachEvent != null)
                TB.DetachingEvent.Unsubscribe(detachEvent);
        }
        /// <summary>
        /// Sub/Unsub to a block's circuits system for Circuit updates (5x a frame)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="module"></param>
        /// <param name="OnRec"></param>
        /// <param name="unsub">Set this to true if we unsubscribe instead</param>
        /// <param name="collectAPSpecificData"></param>
        /// <returns>True if it performed it</returns>
        public static bool SubToLogicReceiverCircuitUpdate<T>(this T module, Action<Circuits.BlockChargeData> OnRec,
            bool unsub, bool collectAPSpecificData) where T : ExtModule
        {
            if (module.block.CircuitNode?.Receiver)
            {
                if (unsub)
                    module.block.CircuitNode?.Receiver.UnSubscribeFromChargeData(null, OnRec, null, null);
                else
                    module.block.CircuitNode?.Receiver.SubscribeToChargeData(null, OnRec, null, null, collectAPSpecificData);
                return true;
            }
            return false;
        }/// <summary>
         /// Sub/Unsub to a block's circuits system for frame-based updates
         /// </summary>
         /// <typeparam name="T"></typeparam>
         /// <param name="module"></param>
         /// <param name="OnRec"></param>
         /// <param name="unsub">Set this to true if we unsubscribe instead</param>
         /// <param name="collectAPSpecificData"></param>
         /// <returns>True if it performed it</returns>
        public static bool SubToLogicReceiverFrameUpdate<T>(this T module, Action<Circuits.BlockChargeData> OnRec,
            bool unsub, bool collectAPSpecificData) where T : ExtModule
        {
            if (module.block.CircuitNode?.Receiver)
            {
                if (unsub)
                    module.block.CircuitNode?.Receiver.UnSubscribeFromChargeData(null, null, null, OnRec);
                else
                    module.block.CircuitNode?.Receiver.SubscribeToChargeData(null, null, null, OnRec, collectAPSpecificData);
                return true;
            }
            return false;
        }



        /// <summary>
        /// Iterates <see cref="ExtModule"/>s
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="BM"></param>
        /// <returns></returns>
        public static IEnumerable<T> IterateExtModules<T>(this BlockManager BM) where T : ExtModule
        {
            foreach (var item in BM.IterateBlocks())
            {
                T get = item.GetComponent<T>();
                if (get)
                    yield return get;
            }
        }
        /// <summary>
        /// Iterates <see cref="ChildModule"/>s
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="BM"></param>
        /// <returns></returns>
        public static IEnumerable<T> IterateChildModules<T>(this BlockManager BM) where T : ExtModule
        {
            foreach (var item in BM.IterateBlocks())
            {
                T[] get = item.GetComponentsInChildren<T>();
                if (get != null)
                {
                    foreach (var child in get)
                    {
                        if (child)
                            yield return child;
                    }
                }
            }
        }
        /// <summary>
        /// Gets a list of <see cref="ExtModule"/>s
        /// <para><b>CREATES A NEW LIST</b></para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="BM"></param>
        /// <returns></returns>
        public static List<T> GetExtModules<T>(this BlockManager BM) where T : ExtModule
        {
            List<T> got = new List<T>();
            foreach (var item in BM.IterateBlocks())
            {
                T get = item.GetComponent<T>();
                if (get)
                    got.Add(get);
            }
            return got;
        }
        /// <summary>
        /// Gets a list of <see cref="ChildModule"/>s
        /// <para><b>CREATES A NEW LIST</b></para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="BM"></param>
        /// <returns></returns>
        public static List<T> GetChildModules<T>(this BlockManager BM) where T : ChildModule
        {
            List<T> got = new List<T>();
            foreach (var item in BM.IterateBlocks())
            {
                T[] get = item.GetComponentsInChildren<T>();
                if (get != null)
                    got.AddRange(get);
            }
            return got;
        }


        /// <summary>
        /// Check to see if <paramref name="vec"/> is within a cube at origin defined by <paramref name="extents"/>
        /// </summary>
        /// <param name="vec"></param>
        /// <param name="extents"></param>
        /// <returns>True if it is within the box</returns>
        public static bool WithinBox(this Vector3 vec, float extents)
        {
            return vec.x >= -extents && vec.x <= extents && vec.y >= -extents && vec.y <= extents && vec.z >= -extents && vec.z <= extents;
        }

        /// <summary>
        /// Get an unloaded tech representation of a <see cref="TrackedVisible"/>
        /// </summary>
        /// <param name="TV"></param>
        /// <returns></returns>
        public static ManSaveGame.StoredTech GetUnloadedTech(this TrackedVisible TV)
        {
            if (TV.visible != null)
                return null;
            try
            {
                if (Singleton.Manager<ManSaveGame>.inst.CurrentState.m_StoredTiles.TryGetValue(TV.GetWorldPosition().TileCoord, out var val))
                {
                    if (val.m_StoredVisibles.TryGetValue(1, out List<ManSaveGame.StoredVisible> techs))
                    {
                        var techD = techs.Find(x => x.m_ID == TV.ID);
                        if (techD != null && (techD is ManSaveGame.StoredTech tech))
                        {
                            return tech;
                        }
                    }
                }
            }
            catch { }
            return null;
        }

        private static FieldInfo blockFlags = typeof(TankBlock).GetField("m_Flags", BindingFlags.NonPublic | BindingFlags.Instance);
        /// <summary>
        /// Set the block flag on a block
        /// </summary>
        /// <param name="block"></param>
        /// <param name="flag"></param>
        /// <param name="trueState"></param>
        /// <exception cref="NullReferenceException"></exception>
        public static void TrySetBlockFlag(this TankBlock block, TankBlock.Flags flag, bool trueState)
        {
            if (block == null)
                throw new NullReferenceException("TrySetBlockFlag was given a NULL block to handle!");
            TankBlock.Flags val = (TankBlock.Flags)blockFlags.GetValue(block);
            if (val.GetSetFlag(flag, trueState))
            {
                blockFlags.SetValue(block, val);
                Debug_TTExt.Info("TrySetBlockFlag " + val + ", value: " + trueState);
            }
        }




        /// <summary>
        /// Get the primary prefab list the game uses to spawn most 
        /// <see cref="TerrainObject"/>s and <see cref="ResourceDispenser"/>s.
        /// <para>WARNING: THIS LIST CAN CHANGE BETWEEN GAME SESSIONS</para>
        /// </summary>
        /// <returns>The prefab list</returns>
        /// <exception cref="NullReferenceException"></exception>
        public static Dictionary<string, TerrainObject> GetLookupList(this TerrainObjectTable TOT)
        {
            FieldInfo sce2 = typeof(TerrainObjectTable).GetField("m_GUIDToPrefabLookup", BindingFlags.NonPublic | BindingFlags.Instance);
            if (sce2 == null)
                throw new NullReferenceException("Field \"TerrainObjectTable.m_GUIDToPrefabLookup\" no longer exists!");

            Dictionary<string, TerrainObject> objsRaw = (Dictionary<string, TerrainObject>)sce2.GetValue(TOT);
            if (objsRaw == null)
            {
                TOT.InitLookupTable();
                objsRaw = (Dictionary<string, TerrainObject>)sce2.GetValue(TOT);
                if (objsRaw == null)
                    throw new NullReferenceException("SpawnHelper: TerrainObjectTable has not allocated m_GUIDToPrefabLookup for some reason and SpawnHelper fetch failed");
            }
            return objsRaw;
        }





        /// <summary>
        /// Finds the first Transform that matches the given name.
        /// Case sensitive.
        /// </summary>
        /// <param name="trans">The parent Transform to search through.</param>
        /// <param name="name">The name of the Transform to find. Case sensitive.</param>
        /// <returns>The found Transform if any, otherwise returns null.</returns>
        public static Transform HeavyTransformSearch(this Transform trans, string name)
        {
            if (name.NullOrEmpty())
                return null;
            return trans.gameObject.GetComponentsInChildren<Transform>().FirstOrDefault(delegate (Transform cand)
            {
                if (cand.name.NullOrEmpty())
                    return false;
                return cand.name.CompareTo(name) == 0;
            });
        }

        /// <summary>
        /// Finds all <see cref="Component"/> of <see cref="Type"/> <typeparamref name="T"/> within a given <see cref="GameObject"/>'s children at
        /// to <b>output_log.txt</b> at <see cref="Application.consoleLogPath"/>
        /// <para>For more advanced operations, see:</para> 
        /// <para><list type="bullet">
        /// <item>For simple <seealso cref="GameObject"/><b>-only</b> extraction <seealso cref="GameObjectDocumentator.GetHierachyAndPrintToLog"/></item>
        /// <item>For detailed extraction - <seealso cref="AutoDataExtractor.AutoDataExtractor(string, Type, object, HashSet{object})"/></item></list></para>
        /// </summary>
        /// <typeparam name="T">The <see cref="Type"/> to search for.</typeparam>
        /// <param name="GO">The <see cref="GameObject"/> to search through.</param>
        public static void PrintAllComponentsGameObjectDepth<T>(this GameObject GO) where T : Component
        {
            Debug_TTExt.Log("-------------------------------------------");
            Debug_TTExt.Log("PrintAllComponentsGameObjectDepth - For " + typeof(T).Name);
            Debug_TTExt.Log(" -- " + GO.name);
            foreach (var item in GO.GetComponentsInChildren<T>(true))
            {
                Debug_TTExt.Log("-------------------------------------------");
                Debug_TTExt.Log(" - " + item.gameObject.name);
                Transform trans = item.transform.parent;
                while (trans != null)
                {
                    Debug_TTExt.Log("  " + trans.gameObject.name);
                    trans = trans.parent;
                }
            }
            Debug_TTExt.Log("-------------------------------------------");
        }


        /// <summary>
        /// Add to a ICollection within a IDictionary.
        /// </summary>
        /// <typeparam name="T">IDictionary</typeparam>
        /// <typeparam name="V">ICollection</typeparam>
        /// <typeparam name="E">Within ICollection</typeparam>
        /// <param name="dict">IDictionary instance</param>
        /// <param name="key">Key to look up in the IDictionary</param>
        /// <param name="typeToAdd">The element to add to the list nested in the dictionary.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void AddInlined<T, V, E>(this IDictionary<T,V> dict, T key, E typeToAdd) where V : ICollection<E>
        {
            if (dict.TryGetValue(key, out V val))
            {
                val.Add(typeToAdd);
            }
            else
            {
                V newIEnumerable = ((V)Activator.CreateInstance(typeof(V)));
                newIEnumerable.Add(typeToAdd);
                dict.Add(key, newIEnumerable);
            }
        }

        /// <summary>
        /// Add to a ICollection within a IDictionary.
        /// </summary>
        /// <typeparam name="T">IDictionary</typeparam>
        /// <typeparam name="V">ICollection</typeparam>
        /// <typeparam name="E">Within ICollection</typeparam>
        /// <param name="dict">IDictionary instance</param>
        /// <param name="key">Key to look up in the IDictionary</param>
        /// <param name="typeToAdd">The element to add to the list nested in the dictionary.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void GetInlined<T, V, E>(this IDictionary<T, V> dict, T key, E typeToAdd) where V : ICollection<E>
        {
            if (dict.TryGetValue(key, out V val))
            {
                val.Add(typeToAdd);
            }
            else
            {
                V newIEnumerable = ((V)Activator.CreateInstance(typeof(V)));
                newIEnumerable.Add(typeToAdd);
                dict.Add(key, newIEnumerable);
            }
        }
        /// <summary>
        /// Remove from an ICollection within a IDictionary.
        /// </summary>
        /// <typeparam name="T">IDictionary</typeparam>
        /// <typeparam name="V">ICollection</typeparam>
        /// <typeparam name="E">Within ICollection</typeparam>
        /// <param name="dict">IDictionary instance</param>
        /// <param name="key">Key to look up in the IDictionary.</param>
        /// <param name="typeToRemove">The element to remove from the list nested in the dictionary.</param>
        /// <returns>true if the element was successfully removed.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static bool RemoveInlined<T, V, E>(this IDictionary<T, V> dict, T key, E typeToRemove) where V : ICollection<E>
        {
            if (dict.TryGetValue(key, out V val))
            {
                bool worked = val.Remove(typeToRemove);
                dict[key] = val;
                return worked;
            }
            return false;
        }

        /// <summary>
        /// Add to a IDictionary within a IDictionary.
        /// </summary>
        /// <typeparam name="T">IDictionary key</typeparam>
        /// <typeparam name="V">IDictionary nested within T</typeparam>
        /// <typeparam name="E">Nested IDictionary key</typeparam>
        /// <typeparam name="A">Within IDictionary</typeparam>
        /// <param name="dict">Dictionary instance</param>
        /// <param name="key">Key to look up in the Dictionary</param>
        /// <param name="keyNested">The nested key to look up in the Dictionary</param>
        /// <param name="typeToAdd">The element to add to the dictionary nested in the dictionary.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void AddInlined<T, V, E, A>(this IDictionary<T, V> dict, T key, E keyNested, A typeToAdd) where V : IDictionary<E,A>
        {
            if (dict.TryGetValue(key, out V val))
            {
                val.Add(keyNested, typeToAdd);
            }
            else
            {
                V newIDictionary = ((V)Activator.CreateInstance(typeof(V)));
                newIDictionary.Add(keyNested, typeToAdd);
                dict.Add(key, newIDictionary);
            }
        }
        /// <summary>
        /// Remove from an IDictionary within a IDictionary.
        /// </summary>
        /// <typeparam name="T">IDictionary key</typeparam>
        /// <typeparam name="V">IDictionary nested within T</typeparam>
        /// <typeparam name="E">Nested IDictionary key</typeparam>
        /// <typeparam name="A">Within IDictionary</typeparam>
        /// <param name="dict">Dictionary instance</param>
        /// <param name="key">Key to look up in the IDictionary.</param>
        /// <param name="typeToRemove">The key to remove from the dictionary nested in the dictionary.</param>
        /// <returns>true if the element was successfully removed.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static bool RemoveInlined<T, V, E, A>(this IDictionary<T, V> dict, T key, E typeToRemove) where V : IDictionary<E, A>
        {
            if (dict.TryGetValue(key, out V val))
            {
                bool worked = val.Remove(typeToRemove);
                dict[key] = val;
                return worked;
            }
            return false;
        }
    }
}
