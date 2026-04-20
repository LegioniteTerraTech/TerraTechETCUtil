using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TerraTechETCUtil
{
    /// <summary>
    /// Remember, when we rescale the terrain, we only scale the TERRAIN, not the other things!
    /// </summary>
    public class TerrainOperations
    {
        /// <summary>
        /// Multiply the terrain scale by this much when we enable this 
        /// </summary>
        public const float RescaleFactor = 4;
        /// <summary>
        /// The default vanilla terrain height range
        /// </summary>
        public const float TileHeightDefault = 100;
        /// <summary>
        /// The default vanilla terrain height offset
        /// </summary>
        public const float TileYOffsetDefault = -50;
        /// <summary>
        /// Converter for tile scale to the actual map generation
        /// </summary>
        public const float tileScaleToMapGen = 2;

        /// <summary>
        /// Inverted rescale factor relative to <see cref="RescaleFactor"/>
        /// </summary>
        public const float RescaleFactorInv = 1 / RescaleFactor;
        /// <summary>
        /// Overhauled terrain height range relative to <see cref="RescaleFactor"/>
        /// </summary>
        public const float TileHeightRescaled = TileHeightDefault * RescaleFactor;
        /// <summary>
        /// Overhauled terrain height offset relative to <see cref="RescaleFactor"/>
        /// </summary>
        public const float TileYOffsetRescaled = TileYOffsetDefault * RescaleFactor;
        /// <summary>
        /// Overhauled terrain height max relative to <see cref="RescaleFactor"/>
        /// </summary>
        public const float TileYOffsetDelta = TileYOffsetRescaled - TileYOffsetDefault;

        /// <summary> </summary>
        public const float MaxPercentScalar = (TileHeightRescaled + TileYOffsetDelta) / TileHeightDefault;
        /// <summary> </summary>
        public const float MinPercentScalar = TileYOffsetDelta / TileHeightDefault;
        /// <summary> </summary>
        public const float TileYOffsetDefaultScalar = TileYOffsetDefault / TileHeightRescaled;
        /// <summary> </summary>
        public const float TileYOffsetDeltaScalar = TileYOffsetDelta / TileHeightRescaled;
        /// <summary> </summary>
        public const float TileYOffsetScalarSeaLevel = -100 / TileHeightRescaled;
        /// <summary> </summary>
        public const float TileYOffsetScalarSeaLevelSceneryLand = -97 / TileHeightRescaled;
        /// <summary> </summary>
        public const float TileYOffsetScalarSeaLevelScenerySea = -106 / TileHeightRescaled;

        /// <summary>
        /// BROKEN, DO NOT USE UNTIL FURTHER NOTICE!!!
        /// </summary>
        public static bool BeachingMode = false;
        const float QuarterHeight = 0.76f / 2f;
        const float QuarterHeightMid = 0.73f / 2f;
        const float QuarterHeightLow = 0.5f / 2f;
        const float QuarterHeightDelta = QuarterHeight - QuarterHeightMid;
        /// <summary>
        /// Get the terrain height relative to the special beaching modifier
        /// <para>Used in:<list type="bullet">
        /// <item><see cref="WorldVerticalExtender.MapGeneratorPatches.GeneratePoint_Transpiler(IEnumerable{HarmonyLib.CodeInstruction})"/></item>
        /// <item><see cref="WorldVerticalExtender.MapGeneratorPatches.GeneratePointLegacy_Transpiler(IEnumerable{HarmonyLib.CodeInstruction})"/></item>
        /// </list></para>
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static float TerraGenRescaled(float input)
        {
            //Debug_SMissions.Log("Rescaled " + input + " to " + (input * RescaleFactorInv));
            if (BeachingMode)
            {
                float val = input * RescaleFactorInv;
                if (val >= QuarterHeightLow && val <= QuarterHeight)
                {
                    return QuarterHeight - Mathf.SmoothStep(QuarterHeightDelta, 0,
                         Mathf.InverseLerp(QuarterHeightLow, QuarterHeight, val));
                }
                return val;
            }
            else
                return input * RescaleFactorInv;
        }

        /// <summary>
        /// Offset heights to modified terrain
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static float LerpToRescaled(float input) => input - TileYOffsetDelta;
        /// <summary>
        /// Offset heights to vanilla terrain
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static float LerpToDefault(float input) => input + TileYOffsetDelta;

    }
}
