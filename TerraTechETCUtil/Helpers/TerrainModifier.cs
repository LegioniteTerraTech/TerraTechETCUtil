using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using Newtonsoft.Json;
using static CompoundExpression.EEInstance;
using static BiomeMap.MapData;

#if !EDITOR
namespace TerraTechETCUtil
{
    public enum TerraApplyMode
    {
        /// <summary>Sets the terrain to the values set in the TerrainModifier with a offset based on mode</summary>
        FlushAutoHeightAdjust,
        /// <summary>Sets the terrain to EXACTLY the values set in the TerrainModifier</summary>
        Flush,
        /// <summary>Do Nothing</summary>
        None,
        /// <summary>Precisely controls the terrain elevation to the values set in the TerrainModifier</summary>
        Apply,
        /// <summary>Adds the terrain elevation to the values set in the TerrainModifier</summary>
        Add,
        /// <summary>Smooths out the terrain, using the TerrainModifier as a multiplier for the smoothing intensity</summary>
        //Level,
        /// <summary>Returns the terrain to it's original form, using the TerrainModifier as a multiplier for the reset intensity</summary>
        Reset
    }
    public enum TerraDampenMode
    {
        DeformOnly,
        DeformAndDampen,
        DampenOnly,
    }
    public static class ModifiedTerrainExt
    {

        private static Dictionary<IntVector2, TerrainModifier> ModsTemp =
            new Dictionary<IntVector2, TerrainModifier>();
        public static void ApplyAll(this Dictionary<IntVector2, TerrainModifier> Mods,
            IntVector2 offsetTileCoord = default)
        {
           ManWorldDeformerExt.ReloadTerrainMods(Mods, offsetTileCoord);
        }
        public static void NudgeAll(this Dictionary<IntVector2, TerrainModifier> Mods, IntVector2 vec)
        {
            foreach (var item in Mods)
            {
                ModsTemp.Add(item.Key + vec, item.Value);
            }
            Mods.Clear();
            foreach (var item in ModsTemp)
            {
                Mods.Add(item.Key, item.Value);
            }
            ModsTemp.Clear();
        }
    }

    /// <summary>
    /// The compressed variant of TerrainModifier suitable for less disk space and less memory demand
    /// </summary>
    [Serializable]
    public class TerrainModifierJSON
    {
        public WorldPosition Position;
        public ushort[,] HeightDeltas;
        public byte[,] additionalInfo;
        public float EdgeDampening;
        public bool UseAmplifiedTerrain;
        public TerraApplyMode AutoMode = TerraApplyMode.None;

        [JsonIgnore]
        public bool IsCompressed => HeightDeltas != null;

        public TerrainModifierJSON() { }

        public TerrainModifierJSON(TerrainModifier toConvert)
        {
            Position = toConvert.Position;
            EdgeDampening = toConvert.EdgeDampening;
            UseAmplifiedTerrain = toConvert.UseAmplifiedTerrain;
            AutoMode = toConvert.AutoMode;
            var deltas = new ushort[toConvert.HeightmapDeltas.GetLength(0), toConvert.HeightmapDeltas.GetLength(1)];

            for (int i = 0; i < toConvert.HeightmapDeltas.GetLength(0); i++)
            {
                for (int j = 0; j < toConvert.HeightmapDeltas.GetLength(1); j++)
                {
                    deltas[i, j] = (ushort)(toConvert.HeightmapDeltas[i, j] * TerrainModifier.compressSize);
                    additionalInfo[i, j] = toConvert.AddInfo[i, j];
                }
            }

            HeightDeltas = deltas;
        }

        public TerrainModifier ToInstance()
        {
            TerrainModifier inst = new TerrainModifier()
            {
                Position = Position,
                EdgeDampening = EdgeDampening,
                UseAmplifiedTerrain = UseAmplifiedTerrain,
                AutoMode = AutoMode
            };
            var deltas = new float[HeightDeltas.GetLength(0), HeightDeltas.GetLength(1)];
            var deltaInfo = new byte[HeightDeltas.GetLength(0), HeightDeltas.GetLength(1)];

            for (int i = 0; i < HeightDeltas.GetLength(0); i++)
            {
                for (int j = 0; j < HeightDeltas.GetLength(1); j++)
                {
                    deltas[i, j] = (float)HeightDeltas[i, j] / TerrainModifier.compressSize;
                    deltaInfo[i, j] = additionalInfo[i, j];
                }
            }

            inst.HeightmapDeltas = deltas;
            inst.AddInfo = deltaInfo;
            return inst;
        }
    }
    /// <summary>
    /// The TerrainModifier at maximum quality
    /// </summary>
    [Serializable]
    public class TerrainModifier
    {
        public const ushort compressSize = ushort.MaxValue;

        public static int CellsInTile => Mathf.Max(1, 2 >> QualitySettingsExtended.ReducedHeightmapDetail) * ManWorld.inst.CellsPerTileEdge;
        public static int CellsInTileIndexer => CellsInTile + 1;
        public static float tilePosToTileScale => ManWorld.inst.TileSize / CellsInTile;
        public static float TileHeight = 100;
        //private static float[,] HeightmapDeltasApplier = new float[CellsInTile, CellsInTile];

        /// <summary>
        /// This is ALWAYS the southwest corner of the entire TerrainModifier
        /// </summary>
        public WorldPosition Position = WorldPosition.FromGameWorldPosition(Vector3.zero);
        public float[,] HeightmapDeltas;
        public byte[,] AddInfo;
        public float EdgeDampening = 6;
        /// <summary>
        /// Set this to true if this is working off of the 4x height range when ManWorldGeneratorExt is set to extend terrain heights
        /// </summary>
        public bool UseAmplifiedTerrain = false;
        public TerraApplyMode AutoMode = TerraApplyMode.FlushAutoHeightAdjust;


        [JsonIgnore]
        public bool IsCompressed => HeightmapDeltas == null;
        [JsonIgnore]
        public bool Changed => deltaed;
        [JsonIgnore]
        public Action OnChanged = null;

        /// <summary>
        /// The size of ManWorld.inst.CellScale to convert to World scale
        /// </summary>
        [JsonIgnore]
        private int Width = 0;
        /// <summary>
        /// The size of ManWorld.inst.CellScale to convert to World scale
        /// </summary>
        [JsonIgnore]
        private int Height = 0;
        /// <summary>
        /// The magnitude radius of Width & Height
        /// </summary>
        [JsonIgnore]
        private float ApproxRadius = 0;
        /// <summary>
        /// The magnitude radius of Width & Height with EdgeDampening applied
        /// </summary>
        [JsonIgnore]
        private float ApproxRadiusDampen = 0;
        [JsonIgnore]
        private bool deltaed = false;
        [JsonIgnore]
        private IntVector2 offset = IntVector2.zero;

        public TerrainModifier Clone()
        {
            InsureSetup();
            int widthTile = HeightmapDeltas.GetLength(0);
            int heightTile = HeightmapDeltas.GetLength(1);
            float[,] newHeightmap = new float[widthTile, heightTile];
            for (int x = 0; x < widthTile; x++)
            {
                for (int y = 0; y < heightTile; y++)
                {
                    newHeightmap[x,y] = HeightmapDeltas[x,y];
                }
            }
            return new TerrainModifier(newHeightmap)
            {
                EdgeDampening = EdgeDampening,
                ApproxRadius = ApproxRadius,
                ApproxRadiusDampen = ApproxRadiusDampen,
                Height = Height,
                Width = Width,
                OnChanged = OnChanged,
                Position = Position,
                UseAmplifiedTerrain = UseAmplifiedTerrain,
            };
        }

        /// <summary>
        /// SERIALIZATION ONLY
        /// </summary>
        public TerrainModifier()
        {
            //Debug_TTExt.Log("Created new TerrainModifier");
        }
        public TerrainModifier(int StartSize)
        {
            float half = StartSize / 2f;
            Position = WorldPosition.FromGameWorldPosition(Vector3.zero);
            HeightmapDeltas = new float[StartSize, StartSize];
            for (int x = 0; x < StartSize; x++)
            {
                for (int y = 0; y < StartSize; y++)
                {
                    HeightmapDeltas[x, y] = 0;
                }
            }
            Setup();
        }
        public static bool TerrainHasDelta(WorldTile tile)
        {
            if (tile == null)
                throw new NullReferenceException("tile is NULL");
            var heights = GetCurrentHeights(tile);
            int widthTile = heights.GetLength(0);
            int heightTile = heights.GetLength(1);
            float[,] DefaultHeights = tile.BiomeMapData.heightData.heights;
            for (int x = 0; x < widthTile; x++)
            {
                for (int y = 0; y < heightTile; y++)
                {
                    if (!heights[y, x].Approximately(DefaultHeights[y, x]))
                        return true;
                }
            }
            return false;
        }
        public TerrainModifier(WorldTile tile, TerraApplyMode modifierMode)
        {
            if (tile == null)
                throw new NullReferenceException("Tile null");
            Setup(tile);
            AutoMode = modifierMode;
        }
        public TerrainModifier(WorldTile tile, Vector3 overrideOrigin, TerraApplyMode modifierMode)
        {
            if (tile == null)
                throw new NullReferenceException("Tile null");
            Setup(tile, overrideOrigin);
            AutoMode = modifierMode;
        }
        public TerrainModifier(float[,] heightmapDeltaDirect)
        {
            if (heightmapDeltaDirect == null)
                throw new NullReferenceException("heightmapDeltaDirect null");
            HeightmapDeltas = heightmapDeltaDirect;
            Setup();
        }

        public void Setup(WorldTile tile, Vector3 overrideOrigin)
        {
            if (tile == null)
                throw new NullReferenceException("tile is NULL");
            UseAmplifiedTerrain = ManWorldGeneratorExt.AmplifiedTerrain;
            float[,] DefaultHeights = tile.BiomeMapData.heightData.heights;
            var heights = GetCurrentHeights(tile);
            int widthTile = heights.GetLength(0);
            int heightTile = heights.GetLength(1);
            Position = WorldPosition.FromScenePosition(tile.CalcSceneOrigin() - overrideOrigin);
            HeightmapDeltas = new float[widthTile, heightTile];
            AddInfo = new byte[widthTile, heightTile];
            Setup();
            for (int x = 0; x < widthTile; x++)
            {
                for (int y = 0; y < heightTile; y++)
                {
                    SetModCellPosFromTileHeight(new IntVector2(x, y), heights[y, x], DefaultHeights[y, x]);
                }
            }
        }
        public void Setup(WorldTile tile)
        {
            if (tile == null)
                throw new NullReferenceException("tile is NULL");
            UseAmplifiedTerrain = ManWorldGeneratorExt.AmplifiedTerrain;
            float[,] DefaultHeights = tile.BiomeMapData.heightData.heights;
            var heights = GetCurrentHeights(tile);
            int widthTile = heights.GetLength(0);
            int heightTile = heights.GetLength(1);
            Position = new WorldPosition(tile.Coord, Vector3.zero);
            HeightmapDeltas = new float[widthTile, heightTile];
            AddInfo = new byte[widthTile, heightTile];
            Setup();
            for (int x = 0; x < widthTile; x++)
            {
                for (int y = 0; y < heightTile; y++)
                {
                    SetModCellPosFromTileHeight(new IntVector2(x, y), heights[y, x], DefaultHeights[y, x]);
                }
            }
        }
        public void Setup()
        {
            if (HeightmapDeltas == null)
                throw new NullReferenceException("HeightmapDeltas is NULL");
            Width = HeightmapDeltas.GetLength(0);
            Height = HeightmapDeltas.GetLength(1);
            if (AddInfo == null)
                AddInfo = new byte[Width, Height];
            ApproxRadius = new Vector2(Width, Height).magnitude;
            ApproxRadiusDampen = (ApproxRadius + EdgeDampening) * ManWorld.inst.CellScale;
            //Debug_TTExt.Log("New TerrainModifier set to [" + Width + ", " + Height + "], radius " + ApproxRadius);
        }
        private void InsureSetup()
        {
            if (Width == 0)
                Setup();
        }

        public void DampenSurrounding()
        { 
        }

        public void OnDelta()
        {
            if (!deltaed)
            {
                deltaed = true;
                if (OnChanged != null)
                    InvokeHelper.InvokeNextUpdate(OnChanged);
            }
        }

        private static StringBuilder SB = new StringBuilder();
        public override string ToString()
        {
            try
            {
                for (int x = 0; x < Width; x++)
                {
                    for (int y = 0; y < Height; y++)
                    {
                        SB.Append(HeightmapDeltas[x,y].ToString() + ", ");
                    }
                    SB.Append("\n");
                }
                return SB.ToString();
            }
            finally
            {
                SB.Clear();
            }
        }
        public static IntVector2 TileWorldSceneOriginCellPos(WorldTile tile) => StandardPosToCellPos(tile.CalcSceneOrigin());
        public static IntVector2 TileWorldSceneOriginCellPos(IntVector2 tile) => StandardPosToCellPos(ManWorld.inst.TileManager.CalcTileOriginScene(tile));
        public IntVector2 SceneToModCellPos(Vector3 scenePos)
        {
            return SceneToModCellPos(scenePos, Position.ScenePosition);
        }
        public static IntVector2 SceneToModCellPos(Vector3 scenePos, Vector3 ModSceneOverride)
        {
            Vector2 pos = scenePos.ToVector2XZ() - ModSceneOverride.ToVector2XZ();
            return new IntVector2(pos / tilePosToTileScale);
        }
        public Vector3 ModCellToScenePos(IntVector2 modPos)
        {
            Vector3 scenePos = Position.ScenePosition;
            return (((Vector3)modPos.ToVector3XZ(0) * tilePosToTileScale) + scenePos.SetY(0)).SetY(scenePos.y);
        }
        public static IntVector2 StandardPosToCellPos(Vector3 scenePos) => 
            new IntVector2(scenePos.ToVector2XZ() / tilePosToTileScale);
        public Vector3 CellPosToStandardPos(IntVector2 sceneCellPos) =>
            (Vector3)sceneCellPos.ToVector3XZ() * tilePosToTileScale;
        public Vector3 GetCenterOffset() => new Vector3(0.5f * Width * tilePosToTileScale, 50, 0.5f * Height * tilePosToTileScale);

        public void GetModTileCoords(Vector3 ModSceneOverride, out IntVector2 min, out IntVector2 max) =>
            ManWorld.inst.TileManager.GetTileCoordRange(new Bounds(ModSceneOverride + GetCenterOffset(),
                new Vector3(Width, 100, Height) * tilePosToTileScale), out min, out max);
        public void GetModTileCoordsWithEdging(Vector3 ModSceneOverride, out IntVector2 min, out IntVector2 max) => 
            ManWorld.inst.TileManager.GetTileCoordRange(new Bounds(ModSceneOverride + GetCenterOffset(),
                Vector3.one * ApproxRadiusDampen), out min, out max);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="modPos">TerrainModifier Coord to set</param>
        /// <param name="interpHeight">0 is Default, 1 is TerrainModifier</param>
        public void SetAddInfoInterpHeight(IntVector2 modPos, float interpHeight)
        {
            AddInfo[modPos.x, Height - 1 - modPos.y] &= (byte)(byte.MaxValue & ~30U);
            AddInfo[modPos.x, Height - 1 - modPos.y] |= (byte)((Mathf.RoundToInt(
                Mathf.Clamp(interpHeight * 15, 0, 15)) << 1) | 1);
        }
        public byte GetAddInfoInterpHeight(IntVector2 modPos)
        {
            return (byte)((AddInfo[modPos.x, Height - 1 - modPos.y] & 30) >> 1);
        }
        public bool UsesDefaultTerrain(IntVector2 modPos)
        {
            return (AddInfo[modPos.x, Height - 1 - modPos.y] & 1) == 1;
        }
        private float GetInterpoliatedHeight(IntVector2 modPos, float modVal, float defaultVal) 
        {
            return Mathf.Lerp(defaultVal, modVal, Mathf.InverseLerp(0, 15, GetAddInfoInterpHeight(modPos)));
        }
        private float GetTileHeightFromModCellPos(IntVector2 modPos, float defaultVal)
        {
            InsureSetup();
            try
            {
                float modVal = HeightmapDeltas[modPos.x, Height - 1 - modPos.y];
                if (UsesDefaultTerrain(modPos))
                    return GetInterpoliatedHeight(modPos, modVal, defaultVal);
                if (UseAmplifiedTerrain)
                {
                    if (ManWorldGeneratorExt.AmplifiedTerrain)
                        return modVal;
                    else
                        return Mathf.Clamp01(modVal * TerrainOperations.RescaleFactor);
                }
                else
                {
                    if (ManWorldGeneratorExt.AmplifiedTerrain)
                        return modVal / TerrainOperations.RescaleFactor;
                    else
                        return modVal;
                }
            }
            catch (IndexOutOfRangeException e)
            {
                throw new IndexOutOfRangeException("Failed with " + (modPos.x) + ", " +
                    (modPos.y) + "", e);
            }
        }

        /// <summary>
        /// Returns true if tile height was altered
        /// </summary>
        /// <param name="modPos"></param>
        /// <param name="val"></param>
        /// <param name="defaultVal"></param>
        /// <returns></returns>
        private bool SetModCellPosFromTileHeight(IntVector2 modPos, float val, float defaultVal)
        {
            HeightmapDeltas[modPos.y, modPos.x] = val;
            if (val.Approximately(defaultVal))
            {
                AddInfo[modPos.y, modPos.x] &= (byte)(byte.MaxValue & ~30U);
                AddInfo[modPos.y, modPos.x] |= 1;
                return false;
            }
            return true;
        }

        public static IntVector2 InTilePosToInTileCoord(Vector3 pos)
        {
            return new IntVector2(
                Mathf.Clamp(Mathf.RoundToInt(pos.x / tilePosToTileScale), 0, CellsInTile),
                Mathf.Clamp(Mathf.RoundToInt(pos.y / tilePosToTileScale), 0, CellsInTile)
                );
        }

        public Vector3 TryEncapsulate(IntVector2 modPos)
        {
            Vector3 scenePos = Position.ScenePosition;
            return ((modPos.ToVector3XZ(0) + scenePos.SetY(0)) * tilePosToTileScale).SetY(scenePos.y);
        }
        private void Encapsulate(IntVector2 modPos, bool GC_Call = false)
        {
            modPos += offset;
            if (modPos.x < 0 || modPos.y < 0)
            {
                IntVector2 moveDelta = new IntVector2(-Mathf.Min(modPos.x, 0), -Mathf.Min(modPos.y, 0));
                offset -= moveDelta;
                Vector3 deltaPos = ((Vector2)modPos * tilePosToTileScale).ToVector3XZ();
                //Debug_TTExt.Log("Encapsulate expand negative moveDelta: " + (moveDelta) + ", deltaPos: " + deltaPos.ToString());
                Position = WorldPosition.FromGameWorldPosition(Position.GameWorldPosition + deltaPos);
                int prevWidth = Width;
                int prevHeight = Height;
                float[,] newArray = new float[Width + moveDelta.x, Height + moveDelta.y];

                for (int x = prevWidth - 1; x >= 0; x--)
                {
                    for (int y = prevHeight - 1; y >= 0; y--)
                    {
                        IntVector2 inTilePos = new IntVector2(x, y);
                        IntVector2 modPosSwitch = inTilePos + moveDelta;
                        newArray[modPosSwitch.x, modPosSwitch.y] = HeightmapDeltas[x, y];
                    }
                }
                HeightmapDeltas = newArray;
                Setup();

                if (GC_Call)
                    GC.Collect();
            }
            else if (modPos.x >= Width || modPos.y >= Height)
            {
                IntVector2 moveDelta = new IntVector2(Mathf.Min(modPos.x - Width, 0), Mathf.Min(modPos.y - Height, 0));
                //Debug_TTExt.Log("Encapsulate expand positive " + (moveDelta) + "");
                int prevWidth = Width;
                int prevHeight = Height;
                float[,] newArray = new float[Width + moveDelta.x, Height + moveDelta.y];

                for (int x = prevWidth - 1; x >= 0; x--)
                {
                    for (int y = prevHeight - 1; y >= 0; y--)
                    {
                        newArray[x, y] = HeightmapDeltas[x, y];
                    }
                }
                HeightmapDeltas = newArray;
                Setup();

                if (GC_Call)
                    GC.Collect();
            }
        }

        private void EncapsulateDampened(IntVector2 modPos, bool GC_Call = false)
        {
            IntVector2 dampener = new IntVector2(Mathf.RoundToInt(EdgeDampening), Mathf.RoundToInt(EdgeDampening));
            Encapsulate(modPos - dampener);
            Encapsulate(modPos + dampener);
            if (GC_Call)
                GC.Collect();
        }
        public void EncapsulateRecenter() =>
            Position = WorldPosition.FromGameWorldPosition(CellPosToStandardPos(new IntVector2(
                Mathf.RoundToInt(Width / 2f), Mathf.RoundToInt(Height / 2f))));


        public void SetHeightAtPosition(Vector3 inModPos, float newHeight, bool resize = false)
        {
            bool CanDo;
            newHeight = Mathf.Clamp01(newHeight);
            IntVector2 modPos = StandardPosToCellPos(inModPos);
            if (resize)
            {
                Encapsulate(StandardPosToCellPos(inModPos));
                modPos = StandardPosToCellPos(inModPos);
                CanDo = true;
            }
            else
                CanDo = Within(modPos);
            if (CanDo)
            {
                HeightmapDeltas[modPos.x, modPos.y] = newHeight;
            }
            deltaed = true;
        }
        public void SetHeightAtPosition(Vector3 inModPos, float newHeight, float intensity, bool resize = false)
        {
            bool CanDo;
            newHeight = Mathf.Clamp01(newHeight);
            IntVector2 modPos = StandardPosToCellPos(inModPos);
            if (resize)
            {
                Encapsulate(StandardPosToCellPos(inModPos));
                modPos = StandardPosToCellPos(inModPos);
                CanDo = true;
            }
            else
                CanDo = Within(modPos);

            if (CanDo)
            {
                intensity = Mathf.Clamp01(intensity);
                float intensityInv = 1 - intensity;
                HeightmapDeltas[modPos.x, modPos.y] = (HeightmapDeltas[modPos.x, modPos.y] 
                    * intensityInv) + (newHeight * intensity);
                deltaed = true;
            }
        }
        public void AddHeightAtPosition(Vector3 inModPos, float addHeight, bool resize = false)
        {
            bool CanDo;
            IntVector2 modPos = StandardPosToCellPos(inModPos);
            if (resize)
            {
                Encapsulate(StandardPosToCellPos(inModPos));
                modPos = StandardPosToCellPos(inModPos);
                CanDo = true;
            }
            else
                CanDo = Within(modPos);
            if (CanDo)
            {
                HeightmapDeltas[modPos.x, modPos.y] = Mathf.Clamp01(HeightmapDeltas[modPos.x, modPos.y] + addHeight);
            }
            deltaed = true;
        }

        public void SetHeightsAtPosition(Vector3 inModPos, Vector2 size, float newHeight, float intensity, bool resize = false)
        {
            bool CanDo;
            newHeight = Mathf.Clamp01(newHeight);
            IntVector2 sizeMod = new IntVector2(size / tilePosToTileScale);
            IntVector2 modPosPrev = StandardPosToCellPos(inModPos);
            if (resize)
            {
                Encapsulate(modPosPrev + sizeMod);
                Encapsulate(modPosPrev - sizeMod);
                modPosPrev = StandardPosToCellPos(inModPos);
                CanDo = true;
            }
            else
            {
                Vector2 pos2D = inModPos.ToVector2XZ();
                CanDo = Intersects(pos2D, pos2D + size);
            }

            if (CanDo)
            {
                intensity = Mathf.Clamp01(intensity);
                float invIntensity = 1 - intensity;

                for (int x = 0; x < sizeMod.x; x++)
                {
                    for (int y = 0; y < sizeMod.y; y++)
                    {
                        IntVector2 inTilePos = new IntVector2(x, y);
                        IntVector2 modPos = inTilePos + modPosPrev;
                        if (Within(modPos))
                        {
                            HeightmapDeltas[x, y] = (HeightmapDeltas[modPos.x, modPos.y] * invIntensity) +
                                (newHeight * intensity);
                        }
                    }
                }
            }
        }
        public void SetHeightsAtPositionSmooth(Vector3 inModPos, Vector2 size, float newHeight, float intensity, bool resize = false)
        {
            bool CanDo;
            newHeight = Mathf.Clamp01(newHeight);
            IntVector2 sizeMod = new IntVector2(size / tilePosToTileScale);
            IntVector2 modPosPrev = StandardPosToCellPos(inModPos);
            if (resize)
            {
                EncapsulateDampened(modPosPrev + sizeMod);
                EncapsulateDampened(modPosPrev - sizeMod);
                modPosPrev = StandardPosToCellPos(inModPos);
                CanDo = true;
            }
            else
            {
                Vector2 pos2D = inModPos.ToVector2XZ();
                CanDo = Intersects(pos2D, pos2D + size);
            }

            if (CanDo)
            {
                float dampen;
                float dampenInv;
                intensity = Mathf.Clamp01(intensity);
                float invIntensity = 1 - intensity;

                for (int x = 0; x < sizeMod.x; x++)
                {
                    for (int y = 0; y < sizeMod.y; y++)
                    {
                        IntVector2 inTilePos = new IntVector2(x, y);
                        IntVector2 modPos = inTilePos + modPosPrev;
                        if (Within(modPos))
                        {
                            HeightmapDeltas[x, y] = (HeightmapDeltas[modPos.x, modPos.y] * invIntensity) +
                                (newHeight * intensity);
                        }
                        else
                        {
                            modPos = new IntVector2(Mathf.Clamp(modPos.x, 0, Width), Mathf.Clamp(modPos.y, 0, Height));
                            dampenInv = Mathf.Clamp01(Mathf.Max(Mathf.Abs(x - modPos.x), Mathf.Abs(y - modPos.y)) / EdgeDampening) * intensity;
                            dampen = 1 - dampenInv;
                            HeightmapDeltas[x, y] = (HeightmapDeltas[modPos.x, modPos.y] * dampenInv) +
                                (newHeight * dampen);
                        }
                    }
                }
            }
        }

        public void AddHeightsAtPosition(Vector3 inModPos, Vector2 size, float addHeight, bool resize = false)
        {
            bool CanDo;
            IntVector2 sizeMod = new IntVector2(size / tilePosToTileScale);
            IntVector2 modPosPrev = StandardPosToCellPos(inModPos);
            if (resize)
            {
                Encapsulate(modPosPrev + sizeMod);
                Encapsulate(modPosPrev - sizeMod);
                modPosPrev = StandardPosToCellPos(inModPos);
                CanDo = true;
            }
            else
            {
                Vector2 pos2D = inModPos.ToVector2XZ();
                CanDo = Intersects(pos2D, pos2D + size);
            }

            if (CanDo)
            {
                IntVector2 inTilePos;
                IntVector2 modPos;
                for (int x = 0; x < sizeMod.x; x++)
                {
                    for (int y = 0; y < sizeMod.y; y++)
                    {
                        inTilePos = new IntVector2(x, y);
                        modPos = inTilePos + modPosPrev;
                        if (Within(modPos))
                        {
                            HeightmapDeltas[modPos.x, modPos.y] = Mathf.Clamp01(HeightmapDeltas[modPos.x, modPos.y] + addHeight);
                        }
                    }
                }
            }
        }
        public void AddHeightsAtPositionSmooth(Vector3 inModPos, Vector2 size, float addHeight, bool resize = false)
        {
            bool CanDo;
            IntVector2 sizeMod = new IntVector2(size / tilePosToTileScale);
            IntVector2 modPosPrev = StandardPosToCellPos(inModPos);
            if (resize)
            {
                EncapsulateDampened(modPosPrev + sizeMod);
                EncapsulateDampened(modPosPrev - sizeMod);
                modPosPrev = StandardPosToCellPos(inModPos);
                CanDo = true;
            }
            else
            {
                Vector2 pos2D = inModPos.ToVector2XZ();
                CanDo = Intersects(pos2D, pos2D + size);
            }

            if (CanDo)
            {
                IntVector2 inTilePos;
                IntVector2 modPos;
                float dampen;
                for (int x = 0; x < sizeMod.x; x++)
                {
                    for (int y = 0; y < sizeMod.y; y++)
                    {
                        inTilePos = new IntVector2(x, y);
                        modPos = inTilePos + modPosPrev;
                        if (Within(modPos))
                        {
                            HeightmapDeltas[modPos.x, modPos.y] = Mathf.Clamp01(HeightmapDeltas[modPos.x, modPos.y] + addHeight);
                        }
                        else
                        {
                            modPos = new IntVector2(Mathf.Clamp(modPos.x, 0, Width), Mathf.Clamp(modPos.y, 0, Height));
                            dampen = 1 - Mathf.Clamp01(Mathf.Max(Mathf.Abs(x - modPos.x), Mathf.Abs(y - modPos.y)) / EdgeDampening);
                            HeightmapDeltas[modPos.x, modPos.y] = Mathf.Clamp01(HeightmapDeltas[modPos.x, modPos.y] + (addHeight * dampen));
                        }
                    }
                }
            }
        }


        private static int posCachedRad = -1;
        private static List<IntVector2> posCached = new List<IntVector2>();
        private static int posCachedRad2 = -1;
        private static HashSet<IntVector2> posCached2 = new HashSet<IntVector2>();
        public void SetHeightsAtPositionRadius(float radius, float newHeight, float intensity, bool resize = false)
        {
            SetHeightsAtPositionRadius(new Vector3(radius, 0, radius), radius, newHeight, intensity, resize);
        }
        public void SetHeightsAtPositionRadius(Vector3 inModPos, float radius, float newHeight, float intensity, bool resize = false)
        {
            newHeight = Mathf.Clamp01(newHeight);
            int radInt = Mathf.CeilToInt(radius / tilePosToTileScale);
            IntVector2 max = new Vector2(radInt, radInt);
            IntVector2 modPosPrev = StandardPosToCellPos(inModPos);
            bool CanDo;
            if (resize)
            {
                Encapsulate(modPosPrev + max);
                Encapsulate(modPosPrev - max);
                modPosPrev = StandardPosToCellPos(inModPos);
                CanDo = true;
            }
            else
            {
                Vector2 maxV = max;
                CanDo = Intersects(inModPos.ToVector2XZ() - maxV, inModPos.ToVector2XZ() + maxV);
            }

            if (CanDo)
            {
                intensity = Mathf.Clamp01(intensity);
                float invIntensity = 1 - intensity;
                IntVector2 modPos;

                if (posCachedRad != radInt)
                {
                    posCachedRad = radInt;
                    posCached.Clear();
                    posCached.AddRange(IntVector2.zero.IterateCircleVolume(radInt));
                }
                foreach (var item in posCached)
                {
                    modPos = item + modPosPrev;
                    if (Within(modPos))
                    {
                        HeightmapDeltas[modPos.x, modPos.y] = (HeightmapDeltas[modPos.x, modPos.y] * invIntensity) +
                            (newHeight * intensity);
                    }
                }
            }
        }
        public void SetHeightsAtPositionRadiusSmooth(float radius, float newHeight, float intensity, bool resize = false)
        {
            SetHeightsAtPositionRadiusSmooth(new Vector3(radius, 0, radius), radius, newHeight, intensity, resize);
        }
        public void SetHeightsAtPositionRadiusSmooth(Vector3 inModPos, float radius, float newHeight, float intensity, bool resize = false)
        {
            newHeight = Mathf.Clamp01(newHeight);
            int radInt = Mathf.CeilToInt(radius / tilePosToTileScale);
            IntVector2 max = new Vector2(radInt, radInt);
            IntVector2 modPosPrev = StandardPosToCellPos(inModPos);
            bool CanDo;
            if (resize)
            {
                EncapsulateDampened(modPosPrev + max);
                EncapsulateDampened(modPosPrev - max);
                modPosPrev = StandardPosToCellPos(inModPos);
                CanDo = true;
            }
            else
            {
                Vector2 maxV = max;
                CanDo = Intersects(inModPos.ToVector2XZ() - maxV, inModPos.ToVector2XZ() + maxV);
            }

            if (CanDo)
            {
                float dampen;
                float dampenInv;
                intensity = Mathf.Clamp01(intensity);
                float invIntensity = 1 - intensity;
                IntVector2 modPos;
                IntVector2 modPosClamped;
                if (posCachedRad != radInt)
                {
                    posCachedRad = radInt;
                    posCached.Clear();
                    posCached.AddRange(IntVector2.zero.IterateCircleVolume(radInt + EdgeDampening));
                }
                if (posCachedRad2 != radInt)
                {
                    posCachedRad2 = radInt;
                    posCached2.Clear();
                    foreach (var item in IntVector2.zero.IterateCircleVolume(radInt))
                    {
                        posCached2.Add(item);
                    }
                }
                foreach (var item in posCached)
                {
                    modPos = item + modPosPrev;
                    if (Within(modPos))
                    {
                        if (!posCached2.Contains(modPos))
                        {
                            HeightmapDeltas[modPos.x, modPos.y] = (HeightmapDeltas[modPos.x, modPos.y] * invIntensity) +
                                (newHeight * intensity);
                        }
                        else
                        {
                            modPosClamped = new IntVector2(Mathf.Clamp(modPos.x, 0, Width),
                                Mathf.Clamp(modPos.y, 0, Height));
                            dampenInv = Mathf.Clamp01(Mathf.Max(Mathf.Abs(modPosClamped.x - modPos.x), Mathf.Abs(modPosClamped.y - modPos.y)) / EdgeDampening) * intensity;
                            dampen = 1 - dampenInv;
                            HeightmapDeltas[modPos.x, modPos.y] = (HeightmapDeltas[modPos.x, modPos.y] * dampenInv) +
                                (newHeight * dampen);
                        }
                    }
                }
                /*
                for (int x = 0; x < size.x; x++)
                {
                    for (int y = 0; y < size.y; y++)
                    {
                        IntVector2 inTilePos = new IntVector2(x, y);
                        IntVector2 modPos = inTilePos + modPosPrev;
                        if (modPos.x >= 0 && modPos.y >= 0 && modPos.x < Width && modPos.y < Height &&
                             new Vector2(x, y).magnitude <= radius)
                        {
                            HeightmapDeltas[x, y] = (HeightmapDeltas[modPos.x, modPos.y] * invIntensity) +
                                (newHeight * intensity);
                        }
                        else
                        {
                            modPos = new IntVector2(Mathf.Clamp(modPos.x, 0, Width),
                                Mathf.Clamp(modPos.y, 0, Height));
                            dampenInv = Mathf.Clamp01(Mathf.Max(Mathf.Abs(x - modPos.x), Mathf.Abs(y - modPos.y)) / EdgeDampening) * intensity;
                            dampen = 1 - dampenInv;
                            HeightmapDeltas[x, y] = (HeightmapDeltas[modPos.x, modPos.y] * dampenInv) +
                                (newHeight * dampen);
                        }
                    }
                }
                */
            }
        }

        public void AddHeightsAtPositionRadius(float radius, float addHeight, bool resize = false)
        {
            AddHeightsAtPositionRadius(new Vector3(radius, 0, radius), radius, addHeight, resize);
        }
        public void AddHeightsAtPositionRadius(Vector3 inModPos, float radius, float addHeight, bool resize = false)
        {
            int radInt = Mathf.CeilToInt(radius / tilePosToTileScale);
            IntVector2 max = new Vector2(radInt, radInt);
            IntVector2 modPosPrev = StandardPosToCellPos(inModPos);
            bool CanDo;
            if (resize)
            {
                Encapsulate(modPosPrev + max);
                Encapsulate(modPosPrev - max);
                modPosPrev = StandardPosToCellPos(inModPos);
                //Debug_TTExt.Log("AddHeightsAtPositionRadius expand " + (modPosPrev).ToString() + " for radius " + radius + "  Layout:\n" + ToString());
                CanDo = true;
            }
            else
            {
                Vector2 maxV = max;
                CanDo = Intersects(inModPos.ToVector2XZ() - maxV, inModPos.ToVector2XZ() + maxV);
            }

            if (CanDo)
            {
                if (posCachedRad != radInt)
                {
                    posCachedRad = radInt;
                    posCached.Clear();
                    posCached.AddRange(IntVector2.zero.IterateCircleVolume(radInt));
                }
                foreach (var item in posCached)
                {
                    IntVector2 modPos = item + modPosPrev;
                    if (Within(modPos))
                    {
                        HeightmapDeltas[modPos.x, modPos.y] = Mathf.Clamp01(HeightmapDeltas[modPos.x, modPos.y] + addHeight);
                    }
                    //else
                    //    Debug_TTExt.Log("AddHeightsAtPositionRadius failed for " + (modPos).ToString());
                }
                //Debug_TTExt.Log("AddHeightsAtPositionRadius final is pos: " + Position.GameWorldPosition + " Layout:\n" + ToString());
            }
        }
        public void AddHeightsAtPositionRadiusSmooth(float radius, float addHeight, bool resize = false)
        {
            AddHeightsAtPositionRadiusSmooth(new Vector3(radius, 0, radius), radius, addHeight, resize);
        }
        public void AddHeightsAtPositionRadiusSmooth(Vector3 inModPos, float radius, float addHeight, bool resize = false)
        {
            int radInt = Mathf.CeilToInt(radius / tilePosToTileScale);
            IntVector2 max = new Vector2(radInt, radInt);
            IntVector2 modPosPrev = StandardPosToCellPos(inModPos);
            bool CanDo;
            if (resize)
            {
                EncapsulateDampened(modPosPrev + max);
                EncapsulateDampened(modPosPrev - max);
                modPosPrev = StandardPosToCellPos(inModPos);
                CanDo = true;
            }
            else
            {
                Vector2 maxV = max;
                CanDo = Intersects(inModPos.ToVector2XZ() - maxV, inModPos.ToVector2XZ() + maxV);
            }

            if (CanDo)
            {
                float dampen;
                if (posCachedRad != radInt)
                {
                    posCachedRad = radInt;
                    posCached.Clear();
                    posCached.AddRange(IntVector2.zero.IterateCircleVolume(radInt + EdgeDampening));
                }
                if (posCachedRad2 != radInt)
                {
                    posCachedRad2 = radInt;
                    posCached2.Clear();
                    foreach (var item in IntVector2.zero.IterateCircleVolume(radInt))
                    {
                        posCached2.Add(item);
                    }
                }
                foreach (var item in posCached)
                {
                    IntVector2 modPos = item + modPosPrev;
                    if (Within(modPos))
                    {
                        if (!posCached2.Contains(modPos))
                        {
                            HeightmapDeltas[modPos.x, modPos.y] = Mathf.Clamp01(HeightmapDeltas[modPos.x, modPos.y] + addHeight);
                        }
                        else
                        {
                            IntVector2 modPosClamped = new IntVector2(Mathf.Clamp(modPos.x, 0, Width),
                                Mathf.Clamp(modPos.y, 0, Height));
                            dampen = 1 - Mathf.Clamp01(Mathf.Max(Mathf.Abs(modPosClamped.x - modPos.x), Mathf.Abs(modPosClamped.y - modPos.y)) / EdgeDampening);
                            HeightmapDeltas[modPos.x, modPos.y] = Mathf.Clamp01(HeightmapDeltas[modPos.x, modPos.y] + (addHeight * dampen));
                        }
                    }
                }
            }
        }



        public void DisplayBoundsUpdate(float displayTime = 0)
        {
            DebugExtUtilities.DrawDirIndicatorRecPrizCorner(Position.ScenePosition, new Vector3(Width, 32, Height), 
                Color.magenta, displayTime > 0 ? displayTime : Time.deltaTime);
        }


        public bool Within(IntVector2 modPos)
        {
            InsureSetup();
            return modPos.x >= 0 && modPos.y >= 0 && modPos.x < Width && modPos.y < Height;
        }
        public bool Intersects(Vector3 scenePos)
        {
            return Within(SceneToModCellPos(scenePos));
        }
        public bool Intersects(Vector2 lowerScenePos, Vector2 higherScenePos)
        {
            InsureSetup();
            Vector2 thisOrigin = Position.ScenePosition.ToVector2XZ();
            Vector2 thisOriginEnd = thisOrigin + new Vector2(Width * tilePosToTileScale, Height * tilePosToTileScale);
            return (thisOrigin.x <= higherScenePos.x || lowerScenePos.x <= thisOriginEnd.x) &&
                (thisOrigin.y <= higherScenePos.y || lowerScenePos.y <= thisOriginEnd.y);
        }
        public bool Intersects(IntVector2 lowerModPos, IntVector2 higherModPos)
        {
            InsureSetup();
            Vector2 thisOriginEnd = new IntVector2(Width , Height);
            return (0 <= higherModPos.x || lowerModPos.x <= thisOriginEnd.x) &&
                (0 <= higherModPos.y || lowerModPos.y <= thisOriginEnd.y);
        }
        public bool Intersects(WorldTile WT)
        {
            InsureSetup();
            Vector2 thisOrigin = Position.ScenePosition.ToVector2XZ();
            Vector2 thisOriginEnd = thisOrigin + new Vector2(Width * tilePosToTileScale * 2, Height * tilePosToTileScale * 2);
            Vector2 tileOrigin = WT.CalcSceneOrigin().ToVector2XZ();
            Vector2 tileOriginEnd = tileOrigin + new Vector2(ManWorld.inst.TileSize, ManWorld.inst.TileSize);
            return (thisOrigin.x <= tileOriginEnd.x || tileOrigin.x <= thisOriginEnd.x) &&
                (thisOrigin.y <= tileOriginEnd.y || tileOrigin.y <= thisOriginEnd.y);
        }
        public bool Intersects(WorldTile WT, Vector3 ModSceneOverride)
        {
            InsureSetup();
            Vector2 thisOrigin = ModSceneOverride.ToVector2XZ();
            Vector2 thisOriginEnd = thisOrigin + new Vector2(Width * tilePosToTileScale, Height * tilePosToTileScale);
            Vector2 tileOrigin = WT.CalcSceneOrigin().ToVector2XZ();
            Vector2 tileOriginEnd = tileOrigin + new Vector2(ManWorld.inst.TileSize * 2, ManWorld.inst.TileSize * 2);
            return (thisOrigin.x <= tileOriginEnd.x || thisOriginEnd.x >= tileOrigin.x) &&
                (thisOrigin.y <= tileOriginEnd.y || thisOriginEnd.y >= tileOrigin.y);
        }
        public bool Intersects(IntVector2 tilePos, Vector3 ModSceneOverride)
        {
            InsureSetup();
            Vector2 thisOrigin = ModSceneOverride.ToVector2XZ();
            Vector2 thisOriginEnd = thisOrigin + new Vector2(Width * tilePosToTileScale, Height * tilePosToTileScale);
            Vector2 tileOrigin = ManWorld.inst.TileManager.CalcTileOrigin(tilePos).ToVector2XZ();
            Vector2 tileOriginEnd = tileOrigin + new Vector2(ManWorld.inst.TileSize * 2, ManWorld.inst.TileSize * 2);
            return (thisOrigin.x <= tileOriginEnd.x || thisOriginEnd.x >= tileOrigin.x) &&
                (thisOrigin.y <= tileOriginEnd.y || thisOriginEnd.y >= tileOrigin.y);
        }
        public bool IntersectsWithEdges(WorldTile WT, Vector3 ModSceneOverride)
        {
            InsureSetup();
            Vector2 thisOrigin = ModSceneOverride.ToVector2XZ();
            Vector2 thisOriginEnd = thisOrigin + new Vector2(Width * tilePosToTileScale, Height * tilePosToTileScale);
            Vector2 tileOrigin = WT.CalcSceneOrigin().ToVector2XZ() - new Vector2(ManWorld.inst.TileSize, ManWorld.inst.TileSize);
            Vector2 tileOriginEnd = tileOrigin + new Vector2(ManWorld.inst.TileSize * 3, ManWorld.inst.TileSize * 3);
            return (thisOrigin.x <= tileOriginEnd.x || thisOriginEnd.x >= tileOrigin.x) &&
                (thisOrigin.y <= tileOriginEnd.y || thisOriginEnd.y >= tileOrigin.y);
        }
        public bool IntersectsWithEdges(IntVector2 tilePos, Vector3 ModSceneOverride)
        {
            InsureSetup();
            Vector2 thisOrigin = ModSceneOverride.ToVector2XZ();
            Vector2 thisOriginEnd = thisOrigin + new Vector2(Width * tilePosToTileScale, Height * tilePosToTileScale);
            Vector2 tileOrigin = ManWorld.inst.TileManager.CalcTileOrigin(tilePos).ToVector2XZ() - new Vector2(ManWorld.inst.TileSize, ManWorld.inst.TileSize);
            Vector2 tileOriginEnd = tileOrigin + new Vector2(ManWorld.inst.TileSize * 3, ManWorld.inst.TileSize * 3);
            return (thisOrigin.x <= tileOriginEnd.x || thisOriginEnd.x >= tileOrigin.x) &&
                (thisOrigin.y <= tileOriginEnd.y || thisOriginEnd.y >= tileOrigin.y);
        }

        /// <summary>
        /// Flushes (applies the absolute contents to) the existing terrain.
        /// CONFLICTS WITH ANY OTHER FLUSH OPERATIONS ON THE SAME TILE.  DO FIRST!
        /// </summary>
        /// <param name="ModSceneOverride">The offset from scene space origin</param>
        public void Flush(Vector3 ModSceneOverride)
        {
            InsureSetup();
            deltaed = false;
            GetModTileCoordsWithEdging(ModSceneOverride, out var vec1, out var vec2);
            foreach (var item in ManWorld.inst.TileManager.IterateTiles(vec1, vec2, WorldTile.State.Created))
            {
                if (item.Terrain != null && IntersectsWithEdges(item, ModSceneOverride))
                {
                    float[,] heightsPrev = GetCurrentHeights(item);
                    IntVector2 delta = SceneToModCellPos(item.Terrain.transform.position, ModSceneOverride);
                    DoFlush(heightsPrev, delta, item.BiomeMapData.heightData.heights,
                        ModSceneOverride, TerraDampenMode.DeformAndDampen);

                    PushChanges(item.Terrain.terrainData, heightsPrev);
                }
            }
            foreach (var item in ManWorld.inst.TileManager.IterateTiles(vec1, vec2, WorldTile.State.Created))
            {
                if (item.Terrain != null && IntersectsWithEdges(item, ModSceneOverride))
                    PushFlush(item);
            }
        }
        /// <summary>
        /// Flushes (applies the absolute contents to) the existing terrain.
        /// DOES NOT WORK WITH MULTIPLE OVERLAPPING FLUSH OPERATIONS ON THE SAME TILE
        /// </summary>
        public void Flush()
        {
            Flush(Position.ScenePosition);
        }
        private void DoFlush(float[,] heightsPrev, IntVector2 delta, float[,] OGHeights,
            Vector3 ModSceneOverride, TerraDampenMode mode)
        {
            float dampen;
            float dampenInv;
            for (int x = 0; x < heightsPrev.GetLength(0); x++)
            {
                for (int y = 0; y < heightsPrev.GetLength(1); y++)
                {
                    IntVector2 inTilePos = new IntVector2(x, y);
                    IntVector2 modPos = inTilePos + delta;
                    if (Within(modPos) && mode <= TerraDampenMode.DeformAndDampen)
                    {
                        heightsPrev[y, x] = GetTileHeightFromModCellPos(modPos, OGHeights[y, x]);
                    }
                    else if (mode >= TerraDampenMode.DeformAndDampen)
                    {
                        IntVector2 modPosClamped = new IntVector2(
                            Mathf.Clamp(modPos.x, 0, Width - 1),
                            Mathf.Clamp(modPos.y, 0, Height - 1));
                        /*
                        dampenInv = Mathf.Clamp01(Mathf.Max(
                            Mathf.Abs(modPos.x - Mathf.Clamp(modPos.x, delta.x, delta.x + Width - 1)), 
                            Mathf.Abs(modPos.y - Mathf.Clamp(modPos.y, delta.y, delta.y + Height - 1)))
                            / EdgeDampening);
                        */
                        dampenInv = Mathf.Clamp01(Mathf.Max(
                            Mathf.Abs(modPos.x - modPosClamped.x),
                            Mathf.Abs(modPos.y - modPosClamped.y))
                            / EdgeDampening);
                        dampen = 1 - dampenInv;
                        heightsPrev[y, x] = (heightsPrev[y, x] * dampenInv) +
                            (GetTileHeightFromModCellPos(modPosClamped, OGHeights[y, x]) * dampen);
                    }
                }
            }
        }
        private float CalcPointFlush(float prevHeight, IntVector2 inTilePos, IntVector2 delta, 
            float OGHeight, Vector3 ModSceneOverride, TerraDampenMode mode)
        {
            IntVector2 modPos = inTilePos + delta;
            if (Within(modPos) && mode <= TerraDampenMode.DeformAndDampen)
            {
                prevHeight = GetTileHeightFromModCellPos(modPos, OGHeight);
            }
            else if(mode >= TerraDampenMode.DeformAndDampen)
            {
                IntVector2 modPosClamped = new IntVector2(
                    Mathf.Clamp(modPos.x, 0, Width - 1),
                    Mathf.Clamp(modPos.y, 0, Height - 1));
                float dampenInv = Mathf.Clamp01(Mathf.Max(
                    Mathf.Abs(modPos.x - modPosClamped.x),
                    Mathf.Abs(modPos.y - modPosClamped.y))
                    / EdgeDampening);
                float dampen = 1 - dampenInv;
                prevHeight = (prevHeight * dampenInv) + (GetTileHeightFromModCellPos(modPosClamped, OGHeight) * dampen);
            }
            return prevHeight;
        }

        /// <summary>
        /// Adds the contents of the TerrainModifier to the existing terrain.
        /// Multiple operations
        /// </summary>
        /// <param name="ModSceneOverride">The offset from scene space origin</param>
        public void FlushAdd(float TerraYMid, float AddMultiplier, Vector3 ModSceneOverride)
        {
            InsureSetup();
            deltaed = false;
            GetModTileCoordsWithEdging(ModSceneOverride, out var vec1, out var vec2);
            foreach (var item in ManWorld.inst.TileManager.IterateTiles(vec1, vec2, WorldTile.State.Created))
            {
                if (item.Terrain != null && IntersectsWithEdges(item, ModSceneOverride))
                {
                    float[,] heightsPrev = GetCurrentHeights(item);
                    IntVector2 delta = SceneToModCellPos(item.Terrain.transform.position, ModSceneOverride);
                    DoAdd(heightsPrev, delta, item.BiomeMapData.heightData.heights, ref TerraYMid,
                        ref AddMultiplier, ModSceneOverride, TerraDampenMode.DeformAndDampen);

                    PushChanges(item.Terrain.terrainData, heightsPrev);
                }
            }
            foreach (var item in ManWorld.inst.TileManager.IterateTiles(vec1, vec2, WorldTile.State.Created))
            {
                if (item.Terrain != null && IntersectsWithEdges(item, ModSceneOverride))
                    PushFlush(item);
            }
        }
        public void FlushAdd(float TerraYMid, float AddMultiplier)
        {
            FlushAdd(TerraYMid, AddMultiplier, Position.ScenePosition);
        }
        private void DoAdd(float[,] heightsPrev, IntVector2 delta, float[,] OGHeights, ref float yMid, 
            ref float addMulti, Vector3 ModSceneOverride, TerraDampenMode mode)
        {
            for (int x = 0; x < heightsPrev.GetLength(0); x++)
            {
                for (int y = 0; y < heightsPrev.GetLength(1); y++)
                {
                    IntVector2 inTilePos = new IntVector2(x, y);
                    IntVector2 modPos = inTilePos + delta;
                    if (Within(modPos) && mode <= TerraDampenMode.DeformAndDampen)
                    {
                        heightsPrev[y, x] = Mathf.Clamp01(heightsPrev[y, x] +
                            ((GetTileHeightFromModCellPos(modPos, OGHeights[y, x]) - yMid) * addMulti));
                    }
                }
            }
        }
        private float CalcPointAdd(float prevHeight, IntVector2 inTilePos, IntVector2 delta, float OGHeight,
            float yMid, float addMulti, Vector3 ModSceneOverride, TerraDampenMode mode)
        {
            IntVector2 modPos = inTilePos + delta;
            if (Within(modPos) && mode <= TerraDampenMode.DeformAndDampen)
            {
                prevHeight = Mathf.Clamp01(prevHeight +
                    ((GetTileHeightFromModCellPos(modPos, OGHeight) - yMid) * addMulti));
            }
            return prevHeight;
        }

        /// <summary>
        /// Apply a TerrainModifier to existing terrain.
        /// DOES NOT WORK WITH MULTIPLE OVERLAPPING FLUSH OPERATIONS ON THE SAME TILE
        /// </summary>
        /// <param name="curWeight">The weight of the terrain that already exists</param>
        /// <param name="addWeight">The weight of the terrain that this TerrainModifier suggests</param>
        /// <param name="ModSceneOverride">The offset from scene space origin</param>
        public void FlushApply(float curWeight, float addWeight, Vector3 ModSceneOverride)
        {
            InsureSetup();
            deltaed = false;
            curWeight = Mathf.Clamp01(curWeight);
            addWeight = Mathf.Clamp01(addWeight);
            GetModTileCoordsWithEdging(ModSceneOverride, out var vec1, out var vec2);
            foreach (var item in ManWorld.inst.TileManager.IterateTiles(vec1, vec2, WorldTile.State.Created))
            {
                if (item.Terrain != null && IntersectsWithEdges(item, ModSceneOverride))
                {
                    float[,] heightsPrev = GetCurrentHeights(item);
                    IntVector2 delta = SceneToModCellPos(item.Terrain.transform.position, ModSceneOverride);
                    DoApply(heightsPrev, delta, item.BiomeMapData.heightData.heights, ref curWeight, 
                        ref addWeight, ModSceneOverride, TerraDampenMode.DeformAndDampen);

                    PushChanges(item.Terrain.terrainData, heightsPrev);
                }
            }
            foreach (var item in ManWorld.inst.TileManager.IterateTiles(vec1, vec2, WorldTile.State.Created))
            {
                if (item.Terrain != null && IntersectsWithEdges(item, ModSceneOverride))
                    PushFlush(item);
            }
        }
        /// <summary>
        /// Apply a TerrainModifier to existing terrain
        /// DOES NOT WORK WITH MULTIPLE OVERLAPPING FLUSH OPERATIONS ON THE SAME TILE
        /// </summary>
        /// <param name="curWeight">The weight of the terrain that already exists</param>
        /// <param name="addWeight">The weight of the terrain that this TerrainModifier suggests</param>
        /// <param name="ModSceneOverride">The offset from origin</param>
        public void FlushApply(float curWeight, float addWeight)
        {
            FlushApply(curWeight, addWeight, Position.ScenePosition);
        }
        private void DoApply(float[,] heightsPrev, IntVector2 delta, float[,] OGHeights, ref float curWeight,
            ref float addWeight, Vector3 ModSceneOverride, TerraDampenMode mode)
        {
            float dampen;
            float dampenInv;
            for (int x = 0; x < heightsPrev.GetLength(0); x++)
            {
                for (int y = 0; y < heightsPrev.GetLength(1); y++)
                {
                    IntVector2 inTilePos = new IntVector2(x, y);
                    IntVector2 modPos = inTilePos + delta;
                    if (Within(modPos) && mode <= TerraDampenMode.DeformAndDampen)
                    {
                        heightsPrev[y, x] = Mathf.Clamp01((heightsPrev[y, x] * curWeight) +
                            (GetTileHeightFromModCellPos(modPos, OGHeights[y, x]) * addWeight));
                    }
                    else if (mode >= TerraDampenMode.DeformAndDampen)
                    {
                        IntVector2 modPosClamped = new IntVector2(
                            Mathf.Clamp(modPos.x, 0, Width - 1),
                            Mathf.Clamp(modPos.y, 0, Height - 1));
                        dampenInv = Mathf.Clamp01(Mathf.Max(
                            Mathf.Abs(modPos.x - modPosClamped.x),
                            Mathf.Abs(modPos.y - modPosClamped.y))
                            / EdgeDampening);
                        dampen = 1 - dampenInv;
                        heightsPrev[y, x] = Mathf.Clamp01((heightsPrev[y, x] * dampenInv) +
                            (GetTileHeightFromModCellPos(modPosClamped, OGHeights[y, x]) * dampen * addWeight));
                    }
                }
            }
        }
        private float CalcPointApply(float prevHeight, IntVector2 inTilePos, IntVector2 delta, float OGHeight,
            float curWeight, float addWeight, Vector3 ModSceneOverride, TerraDampenMode mode)
        {
            IntVector2 modPos = inTilePos + delta;
            if (Within(modPos) && mode <= TerraDampenMode.DeformAndDampen)
            {
                prevHeight = Mathf.Clamp01((prevHeight * curWeight) +
                    (GetTileHeightFromModCellPos(modPos, OGHeight) * addWeight));
            }
            else if (mode >= TerraDampenMode.DeformAndDampen)
            {
                IntVector2 modPosClamped = new IntVector2(
                    Mathf.Clamp(modPos.x, 0, Width - 1),
                    Mathf.Clamp(modPos.y, 0, Height - 1));
                float dampenInv = Mathf.Clamp01(Mathf.Max(
                    Mathf.Abs(modPos.x - modPosClamped.x),
                    Mathf.Abs(modPos.y - modPosClamped.y))
                    / EdgeDampening);
                float dampen = 1 - dampenInv;
                prevHeight = Mathf.Clamp01((prevHeight * dampenInv) +
                    (GetTileHeightFromModCellPos(modPosClamped, OGHeight) * dampen * addWeight));
            }
            return prevHeight;
        }

        /// <summary>
        /// Note: cannot use in Auto mode due to reliance on neighboors.  Scales poorly.
        /// </summary>
        /// <param name="gradientRadius"></param>
        /// <param name="intensity"></param>
        /// <param name="ModSceneOverride"></param>
        public void FlushLevel(int gradientRadius, float intensity, Vector3 ModSceneOverride)
        {
            InsureSetup();
            deltaed = false;
            intensity = Mathf.Clamp01(intensity);
            GetModTileCoordsWithEdging(ModSceneOverride, out var vec1, out var vec2);
            foreach (var item in ManWorld.inst.TileManager.IterateTiles(vec1, vec2, WorldTile.State.Created))
            {
                if (item.Terrain != null && IntersectsWithEdges(item, ModSceneOverride))
                {
                    float[,] heightsPrev = GetCurrentHeights(item);
                    IntVector2 origin = TileWorldSceneOriginCellPos(item);
                    IntVector2 delta = SceneToModCellPos(item.Terrain.transform.position, ModSceneOverride);
                    DoLevel(heightsPrev, delta, origin, item.BiomeMapData.heightData.heights,
                        gradientRadius, ref intensity, ModSceneOverride, TerraDampenMode.DeformAndDampen);

                    PushChanges(item.Terrain.terrainData, heightsPrev);
                };
            }
            foreach (var item in ManWorld.inst.TileManager.IterateTiles(vec1, vec2, WorldTile.State.Created))
            {
                if (item.Terrain != null && IntersectsWithEdges(item, ModSceneOverride))
                    PushFlush(item);
            }
        }
        public void FlushLevel(int gradientRadius, float intensity) =>
            FlushLevel(gradientRadius, intensity, Position.ScenePosition);
        private void DoLevel(float[,] heightsPrev, IntVector2 delta, IntVector2 origin, float[,] OGHeights, 
            int gradientRadius, ref float intensity, Vector3 ModSceneOverride, TerraDampenMode mode)
        {
            float dampen;
            float dampenInv;
            //BoundsInt BI = new BoundsInt();
            int widthT = heightsPrev.GetLength(0);
            int heightT = heightsPrev.GetLength(1);
            for (int x = 0; x < widthT; x++)
            {
                for (int y = 0; y < heightT; y++)
                {
                    IntVector2 inTilePos = new IntVector2(x, y);
                    IntVector2 modPos = inTilePos + delta;
                    if (Within(modPos) && mode <= TerraDampenMode.DeformAndDampen)
                    {
                        float normalizingForces = 0;

                        if (gradientRadius <= 0)
                        {
                            normalizingForces = 0;
                        }
                        else
                        {
                            int normalsCount = 0;
                            for (int x2 = -gradientRadius; x2 <= gradientRadius; x2++)
                            {
                                for (int y2 = -gradientRadius; y2 <= gradientRadius; y2++)
                                {
                                    if (x2 != x && y2 != y)
                                    {
                                        if (x2 >= 0 && y2 >= 0 && x2 < widthT && y2 < heightT)
                                            normalizingForces += heightsPrev[y, x];
                                        else
                                            normalizingForces += (ManWorld.inst.TileManager.GetTerrainHeightAtPosition(
                                                 CellPosToStandardPos(origin + inTilePos), out _) -
                                                 ManWorldGeneratorExt.CurrentMinHeight) / ManWorldGeneratorExt.CurrentTotalHeight;
                                        normalsCount++;
                                    }
                                }
                            }
                            if (normalsCount > 0)
                            {
                                normalizingForces /= normalsCount;
                                normalizingForces -= heightsPrev[y, x];
                            }
                        }
                        heightsPrev[y, x] = Mathf.Clamp01(heightsPrev[y, x] - 
                            (GetTileHeightFromModCellPos(modPos, OGHeights[y, x]) * normalizingForces * intensity));
                    }
                    /*
                    else
                    {
                        modPos = new IntVector2(Mathf.Clamp(modPos.x, 0, Width - 1),
                            Mathf.Clamp(modPos.y, 0, Height - 1));
                        dampenInv = Mathf.Clamp01(Mathf.Max(Mathf.Abs(x - modPos.x), Mathf.Abs(y - modPos.y)) / EdgeDampening) * intensity;
                        dampen = 1 - dampenInv;
                        heightsPrev[y, x] = Mathf.Clamp01(heightsPrev[y, x] +
                            (GetTileHeightFromModPos(modPos) * dampen * normalizingForces));
                    }*/
                }
            }
        }


        public void FlushReset(float intensity, Vector3 ModSceneOverride)
        {
            InsureSetup();
            deltaed = false;
            intensity = Mathf.Clamp01(intensity);
            GetModTileCoordsWithEdging(ModSceneOverride, out var vec1, out var vec2);
            foreach (var item in ManWorld.inst.TileManager.IterateTiles(vec1, vec2, WorldTile.State.Created))
            {
                if (item.Terrain != null && IntersectsWithEdges(item, ModSceneOverride))
                {
                    float[,] heightsPrev = GetCurrentHeights(item);
                    IntVector2 origin = TileWorldSceneOriginCellPos(item);
                    IntVector2 delta = SceneToModCellPos(item.Terrain.transform.position, ModSceneOverride);
                    DoReset(heightsPrev, delta, item.BiomeMapData.heightData.heights, ref intensity, 
                        ModSceneOverride, TerraDampenMode.DeformAndDampen);

                    PushChanges(item.Terrain.terrainData, heightsPrev);
                }
            }
            foreach (var item in ManWorld.inst.TileManager.IterateTiles(vec1, vec2, WorldTile.State.Created))
            {
                if (item.Terrain != null && IntersectsWithEdges(item, ModSceneOverride))
                    PushFlush(item);
            }
        }
        public void FlushReset(float intensity) => FlushReset(intensity, Position.ScenePosition);
        private void DoReset(float[,] heightsPrev, IntVector2 delta, float[,] OGHeights, 
            ref float intensity, Vector3 ModSceneOverride, TerraDampenMode mode)
        {
            int widthT = heightsPrev.GetLength(0);
            int heightT = heightsPrev.GetLength(1);
            for (int x = 0; x < widthT; x++)
            {
                for (int y = 0; y < heightT; y++)
                {
                    IntVector2 inTilePos = new IntVector2(x, y);
                    IntVector2 modPos = inTilePos + delta;
                    if (Within(modPos) && mode <= TerraDampenMode.DeformAndDampen)
                    {
                        if (intensity >= 0)
                        {
                            float normalizingForces = OGHeights[y, x] - heightsPrev[y, x];
                            heightsPrev[y, x] = Mathf.Clamp01(heightsPrev[y, x] + (GetTileHeightFromModCellPos(modPos, OGHeights[y, x]) *
                                normalizingForces * intensity));
                        }
                        else
                        {
                            intensity = -intensity;
                            float normalizingForces = OGHeights[y, x] - heightsPrev[y, x];
                            heightsPrev[y, x] = Mathf.Clamp01(heightsPrev[y, x] + (GetTileHeightFromModCellPos(modPos, OGHeights[y, x]) *
                                normalizingForces * intensity));
                        }
                    }
                }
            }
        }
        private float CalcPointReset(float prevHeight, IntVector2 inTilePos, IntVector2 delta, 
            float OGHeight, float intensity, Vector3 ModSceneOverride, TerraDampenMode mode)
        {
            IntVector2 modPos = inTilePos + delta;
            if (Within(modPos) && mode <= TerraDampenMode.DeformAndDampen)
            {
                if (intensity >= 0)
                {
                    float normalizingForces = OGHeight - prevHeight;
                    prevHeight = Mathf.Clamp01(prevHeight + (GetTileHeightFromModCellPos(modPos, OGHeight) *
                        normalizingForces * intensity));
                }
                else
                {
                    intensity = -intensity;
                    float normalizingForces = OGHeight - prevHeight;
                    prevHeight = Mathf.Clamp01(prevHeight + (GetTileHeightFromModCellPos(modPos, OGHeight) *
                        normalizingForces * intensity));
                }
            }
            return prevHeight;
        }

        public void FlushRamp(float intensity, Vector3 start, Vector3 end, Vector3 ModSceneOverride)
        {
            InsureSetup();
            deltaed = false;
            GetModTileCoordsWithEdging(ModSceneOverride, out var vec1, out var vec2);
            float lowHeight = 0;
            float highHeight = 0;
            bool gotTarget = false;
            Vector3 rampCenter = default;
            Vector3 vecNormal = default;
            float Rescaler = 1f / 3f;
            //Debug_TTExt.Log("Rescaler: " + Rescaler.ToString());
            foreach (var item in ManWorld.inst.TileManager.IterateTiles(vec1, vec2, WorldTile.State.Created))
            {
                if (!gotTarget)
                {
                    Vector3 vec = (end - start).SetY(0);
                    float mag = vec.magnitude / tilePosToTileScale;
                    if (mag == 0)
                        return;
                    float TEMPL = (start.y - item.Terrain.transform.position.y) / TileHeight;
                    float TEMPH = (end.y - item.Terrain.transform.position.y) / TileHeight;
                    start.y = TEMPL;
                    end.y = TEMPH;
                    lowHeight = Mathf.Min(TEMPL, TEMPH);
                    highHeight = Mathf.Max(TEMPL, TEMPH);
                    IntVector2 startCellWorld = StandardPosToCellPos(start);
                    IntVector2 endCellWorld = StandardPosToCellPos(end);
                    IntVector2 midVec = ((endCellWorld - startCellWorld) / 2) + startCellWorld;
                    float mid = ((highHeight - lowHeight) / 2) + lowHeight;
                    rampCenter = new Vector3(midVec.x, mid, midVec.y);
                    //Debug_TTExt.Log("RampCenter: " + rampCenter.ToString());
                    Vector3 vec4 = end - start;
                    vec4.x *= Rescaler;
                    vec4.z *= Rescaler;
                    Vector3 vec4Side = Quaternion.LookRotation(Vector3.left, Vector3.up) * vec4.SetY(0).normalized;
                    vecNormal = Vector3.Cross(vec4Side, vec4.normalized);
                    gotTarget = true;
                    //Debug_TTExt.Log("VecNormal: " + vecNormal.ToString());
                    //Debug_TTExt.Log("VecDelta: " + vec4.y.ToString());
                }
                if (item.Terrain != null && IntersectsWithEdges(item, ModSceneOverride))
                    DoRamp(item, ref intensity, item.BiomeMapData.heightData.heights, ModSceneOverride, lowHeight, highHeight,
                        rampCenter, vecNormal, TerraDampenMode.DeformAndDampen);
            }
            foreach (var item in ManWorld.inst.TileManager.IterateTiles(vec1, vec2, WorldTile.State.Created))
            {
                if (item.Terrain != null && IntersectsWithEdges(item, ModSceneOverride))
                    PushFlush(item);
            }
        }
        private void DoRamp(WorldTile Target, ref float intensity, float[,] OGHeights, Vector3 ModSceneOverride,
            float lowHeight, float highHeight, Vector3 rampCenter, Vector3 vecNormal, TerraDampenMode mode)
        {
            var TD = Target.Terrain.terrainData;
            float[,] heightsPrev = GetCurrentHeights(Target);
            IntVector2 tileOriginCellWorld = TileWorldSceneOriginCellPos(Target);
            IntVector2 deltaCellTile = SceneToModCellPos(Target.Terrain.transform.position, ModSceneOverride);
            //BoundsInt BI = new BoundsInt();
            int widthT = heightsPrev.GetLength(0);
            int heightT = heightsPrev.GetLength(1);
            for (int x = 0; x < widthT; x++)
            {
                for (int y = 0; y < heightT; y++)
                {
                    IntVector2 inTilePos = new IntVector2(x, y);
                    IntVector2 CellPosWorld = tileOriginCellWorld + inTilePos;
                    IntVector2 modPos = inTilePos + deltaCellTile;
                    if (Within(modPos) && mode <= TerraDampenMode.DeformAndDampen)
                    {
                        float targetHeight = Revec(rampCenter, CellPosWorld, heightsPrev[y, x], vecNormal);
                        float normalizingForces = Mathf.Clamp(targetHeight, lowHeight, highHeight) - heightsPrev[y, x];
                        heightsPrev[y, x] = Mathf.Clamp01(heightsPrev[y, x] + Mathf.Clamp(normalizingForces * 
                            GetTileHeightFromModCellPos(modPos, OGHeights[y, x]), -intensity, intensity));
                    }
                }
            }
            PushChanges(TD, heightsPrev);
        }
        private float Revec(Vector3 toVecNormal, IntVector2 worldCellPos, float height, Vector3 vecNormal)
        {
            Vector3 vecCur = new Vector3(worldCellPos.x, height, worldCellPos.y);
            Vector3 toProject = vecCur - toVecNormal;
            float final = 0;
            for (int i = 0; i < 8; i++)
                final = Vector3.ProjectOnPlane(new Vector3(toProject.x, final, toProject.z), vecNormal).y;
            return final + toVecNormal.y;
        }



        /// <summary>
        /// YOU WILL NEED TO CALL AutoDelta1 AFTER
        /// </summary>
        internal static void AutoDelta0(WorldTile tile, out float[,] heightsPrev, out IntVector2 origin, out float[,] OGHeights)
        {
            heightsPrev = GetCurrentHeights(tile);
            origin = TileWorldSceneOriginCellPos(tile);
            OGHeights = tile.BiomeMapData.heightData.heights;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="delta">TileInPos to TerrainModifier space</param>
        /// <returns></returns>
        internal bool HasMainPartInTile(IntVector2 delta)
        {
            return delta.x + CellsInTile >= 0 && delta.y + CellsInTile >= 0 && delta.x < Width && delta.y < Height;
        }

        /// <summary>
        /// YOU WILL NEED TO CALL PushFlush() AFTER
        /// </summary>
        internal void AutoDelta1(float[,] heightsPrev, IntVector2 delta, IntVector2 origin, float[,] OGHeights,
            float curWeight, float addWeight, Vector3 ModSceneOverride, TerraDampenMode mode)
        {
            //Vector3 ModSceneOverride
            switch (AutoMode)
            {
                case TerraApplyMode.None:
                    break;
                case TerraApplyMode.FlushAutoHeightAdjust:
                    DoFlush(heightsPrev, delta, heightsPrev, ModSceneOverride, mode);
                    break;
                case TerraApplyMode.Flush:
                    DoFlush(heightsPrev, delta, heightsPrev, ModSceneOverride, mode);
                    break;
                case TerraApplyMode.Add:
                    float yMid = 0.5f;
                    DoAdd(heightsPrev, delta, heightsPrev, ref yMid, ref addWeight, ModSceneOverride, mode);
                    break;
                    /* // Scales too poorly
                case TerraApplyMode.Level:
                    DoLevel(heightsPrev, delta, origin, Mathf.CeilToInt(ApproxRadius), ref addWeight, ModSceneOverride);
                    break;*/
                case TerraApplyMode.Reset:
                    DoReset(heightsPrev, delta, OGHeights, ref addWeight, ModSceneOverride, mode);
                    break;
                case TerraApplyMode.Apply:
                default:
                    DoApply(heightsPrev, delta, heightsPrev, ref curWeight, ref addWeight, ModSceneOverride, mode);
                    break;
            }
        }

        /// <summary>
        /// DOES NOT WORK PROPERLY IF ANY OVERLAPPING ARE SET TO FLUSH
        /// </summary>
        /// <param name="curWeight"></param>
        /// <param name="addWeight"></param>
        /// <param name="ModSceneOverride"></param>
        internal void AutoDeltaSingle(float curWeight, float addWeight, Vector3 ModSceneOverride)
        {
            InsureSetup();
            deltaed = false;
            curWeight = Mathf.Clamp01(curWeight);
            addWeight = Mathf.Clamp01(addWeight);
            GetModTileCoordsWithEdging(ModSceneOverride, out var vec1, out var vec2);
            foreach (var item in ManWorld.inst.TileManager.IterateTiles(vec1, vec2, WorldTile.State.Created))
            {
                if (item.Terrain != null && IntersectsWithEdges(item, ModSceneOverride))
                {
                    float[,] heightsPrev = GetCurrentHeights(item);
                    IntVector2 origin = TileWorldSceneOriginCellPos(item);
                    IntVector2 delta = SceneToModCellPos(item.Terrain.transform.position, ModSceneOverride);
                    AutoDelta1(heightsPrev, delta, origin, item.BiomeMapData.heightData.heights, curWeight, addWeight, 
                        ModSceneOverride, TerraDampenMode.DeformAndDampen);

                    PushChanges(item.Terrain.terrainData, heightsPrev);
                };
            }
            foreach (var item in ManWorld.inst.TileManager.IterateTiles(vec1, vec2, WorldTile.State.Created))
            {
                if (item.Terrain != null && IntersectsWithEdges(item, ModSceneOverride))
                    PushFlush(item);
            }
        }
        internal float AutoCalcPoint(float prevHeight, IntVector2 inTilePos, IntVector2 delta, IntVector2 origin, float OGHeight,
            float curWeight, float addWeight, Vector3 ModSceneOverride, TerraDampenMode mode)
        {
            //Vector3 ModSceneOverride
            switch (AutoMode)
            {
                case TerraApplyMode.None:
                    return prevHeight;
                case TerraApplyMode.FlushAutoHeightAdjust:
                    return CalcPointFlush(prevHeight, inTilePos, delta, OGHeight, ModSceneOverride, mode);
                case TerraApplyMode.Flush:
                    return CalcPointFlush(prevHeight, inTilePos, delta, OGHeight, ModSceneOverride, mode);
                case TerraApplyMode.Add:
                    float yMid = 0.5f;
                    return CalcPointAdd(prevHeight, inTilePos, delta, OGHeight, yMid, addWeight, ModSceneOverride, mode);
                /* // Scales too poorly
            case TerraApplyMode.Level:
                DoLevel(heightsPrev, delta, origin, Mathf.CeilToInt(ApproxRadius), ref addWeight, ModSceneOverride);
                break;*/
                case TerraApplyMode.Reset:
                    return CalcPointReset(prevHeight, inTilePos, delta, OGHeight, addWeight, ModSceneOverride, mode);
                case TerraApplyMode.Apply:
                default:
                    return CalcPointApply(prevHeight, inTilePos, delta, OGHeight, curWeight, addWeight, ModSceneOverride, mode);
            }
        }


        public static void PushChanges(TerrainData TD, float[,] newMap)
        {
            TD.SetHeights(0, 0, newMap);
            //TD.UpdateDirtyRegion(0, 0, CellsInTile, CellsInTile, true);
        }
        private static MethodInfo flusher = typeof(TileManager).GetMethod("ConnectNeighbouringTilesAndFlush", 
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.InvokeMethod);
        private static object[] flushCall = new object[2] { null, null };
        public static void PushFlush(WorldTile WT)
        {
            flushCall[0] = WT;
            flushCall[1] = true;
            bool prevState = Globals.inst.m_ManuallyConnectTerrainTiles;
            Globals.inst.m_ManuallyConnectTerrainTiles = false;
            flusher.Invoke(ManWorld.inst.TileManager, flushCall);
            Terrain.SetConnectivityDirty();
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    var tile = ManWorld.inst.TileManager.LookupTile(WT.Coord + new IntVector2(x, y));
                    if (tile != null)
                    {
                        ManWorldDeformerExt.terrainsByDeformed.Add(WT.Coord);
                        if (x == 0 && y == 0)
                            ManWorldDeformerExt.terrainsDeformed.Add(WT.Coord);
                    }
                }
            }
            Globals.inst.m_ManuallyConnectTerrainTiles = prevState;
        }
        private static float[,] heightsCached = new float[CellsInTileIndexer, CellsInTileIndexer];
        /// <summary>
        /// DO NOT CALL WHEN USING
        /// IT IS RECYCLED FOR LATER CALLS
        /// </summary>
        /// <param name="tile"></param>
        /// <returns></returns>
        private static float[,] GetCurrentHeights(WorldTile tile)
        {
            //heightsCached = new float[CellsInTileIndexer, CellsInTileIndexer];
            float height = ManWorldGeneratorExt.CurrentTotalHeight;
            Vector2 delta = tile.WorldOrigin.ToVector2XZ();
            Terrain terra = tile.Terrain;
            for (int x = 0; x < CellsInTileIndexer; x++)
            {
                for (int y = 0; y < CellsInTileIndexer; y++)
                {
                    float heightOut = terra.SampleHeight((new Vector2(y * tilePosToTileScale, x * tilePosToTileScale)
                        + delta
                        ).ToVector3XZ()) / height;
                   // Vector3 worldSearch = (delta + new Vector2(y * tilePosToTileScale, x * tilePosToTileScale)
                    //    ).ToVector3XZ();
                    //if (!ManWorld.inst.GetTerrainHeight())
                    //    throw new NullReferenceException("Tile exists but we are searching in the wrong area " + );
                    heightsCached[x, y] = heightOut;
                }
            }
            return heightsCached;
        }
    }
}
#endif