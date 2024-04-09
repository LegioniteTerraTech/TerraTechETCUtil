using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using Newtonsoft.Json;

#if !EDITOR
namespace TerraTechETCUtil
{
    [Serializable]
    public class TerrainModifier
    {
        public static int CellsInTile => Mathf.Max(1, 2 >> QualitySettingsExtended.ReducedHeightmapDetail) * ManWorld.inst.CellsPerTileEdge;
        public static int CellsInTileIndexer => CellsInTile + 1;
        public static float tilePosToTileScale => ManWorld.inst.TileSize / CellsInTile;
        //private static float[,] HeightmapDeltasApplier = new float[CellsInTile, CellsInTile];


        public WorldPosition Position = WorldPosition.FromGameWorldPosition(Vector3.zero);
        public float[,] HeightmapDeltas;
        public float EdgeDampening = 6;
        public static float TileHeight = 100;
        [JsonIgnore]
        public bool Changed => deltaed;
        [JsonIgnore]
        public Action OnChanged = null;

        [JsonIgnore]
        private int Width = 0;
        [JsonIgnore]
        private int Height = 0;
        [JsonIgnore]
        private float ApproxRadius = 0;
        [JsonIgnore]
        private float ApproxRadiusDampen = 0;
        [JsonIgnore]
        private bool deltaed = false;
        [JsonIgnore]
        private IntVector2 offset = IntVector2.zero;

        /// <summary>
        /// SERIALIZATION ONLY
        /// </summary>
        public TerrainModifier()
        {
            Setup();
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
        public TerrainModifier(WorldTile tile)
        {
            Setup(tile);
        }
        public TerrainModifier(WorldTile tile, Vector3 overrideOrigin)
        {
            Setup(tile, overrideOrigin);
        }
        public TerrainModifier(float[,] heightmapDeltaDirect)
        {
            HeightmapDeltas = heightmapDeltaDirect;
            Setup();
        }

        public void Setup(WorldTile tile, Vector3 overrideOrigin)
        {
            var heights = tile.Terrain.terrainData.GetHeights(0, 0, CellsInTile, CellsInTile);
            int widthTile = heights.GetLength(0);
            int heightTile = heights.GetLength(1);
            Position = WorldPosition.FromScenePosition(tile.CalcSceneOrigin() - overrideOrigin);
            HeightmapDeltas = new float[widthTile, heightTile];
            Setup();
            for (int x = 0; x < widthTile; x++)
            {
                for (int y = 0; y < heightTile; y++)
                {
                    SetModCellPosFromTileHeight(new IntVector2(x, y), heights[y, x]);
                }
            }
        }
        public void Setup(WorldTile tile)
        {
            var heights = tile.Terrain.terrainData.GetHeights(0, 0, CellsInTile, CellsInTile);
            int widthTile = heights.GetLength(0);
            int heightTile = heights.GetLength(1);
            Position = new WorldPosition(tile.Coord, Vector3.zero);
            HeightmapDeltas = new float[widthTile, heightTile];
            Setup();
            for (int x = 0; x < widthTile; x++)
            {
                for (int y = 0; y < heightTile; y++)
                {
                    SetModCellPosFromTileHeight(new IntVector2(x, y), heights[y, x]);
                }
            }
        }
        public void Setup()
        {
            Width = HeightmapDeltas.GetLength(0);
            Height = HeightmapDeltas.GetLength(1);
            ApproxRadius = new Vector2(Width, Height).magnitude;
            ApproxRadiusDampen = ApproxRadius * ManWorld.inst.CellScale + EdgeDampening;
            //Debug_TTExt.Log("New TerrainModifier set to [" + Width + ", " + Height + "], radius " + ApproxRadius);
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
        public IntVector2 TileWorldSceneOriginCellPos(WorldTile tile) => StandardPosToCellPos(tile.CalcSceneOrigin());
        public IntVector2 SceneToModCellPos(Vector3 scenePos)
        {
            return SceneToModCellPos(scenePos, Position.ScenePosition);
        }
        public IntVector2 SceneToModCellPos(Vector3 scenePos, Vector3 ModSceneOverride)
        {
            Vector2 pos = scenePos.ToVector2XZ() - ModSceneOverride.ToVector2XZ();
            return new IntVector2(pos / tilePosToTileScale);
        }
        public Vector3 ModCellToScenePos(IntVector2 modPos)
        {
            Vector3 scenePos = Position.ScenePosition;
            return (((Vector3)modPos.ToVector3XZ(0) * tilePosToTileScale) + scenePos.SetY(0)).SetY(scenePos.y);
        }
        public IntVector2 StandardPosToCellPos(Vector3 scenePos) => 
            new IntVector2(scenePos.ToVector2XZ() / tilePosToTileScale);
        public Vector3 CellPosToStandardPos(IntVector2 sceneCellPos) =>
            (Vector3)sceneCellPos.ToVector3XZ() * tilePosToTileScale;

        private float GetTileHeightFromModCellPos(IntVector2 modPos)
        {
            try
            {
                return HeightmapDeltas[modPos.x, Height - 1 - modPos.y];
                //return HeightmapDeltas[Height - 1 - modPos.y, Width - 1 - modPos.x];
            }
            catch (IndexOutOfRangeException e)
            {
                throw new IndexOutOfRangeException("Failed with " + (modPos.x) + ", " +
                    (modPos.y) + "", e);
            }
        }
        private void SetModCellPosFromTileHeight(IntVector2 modPos, float val)
        {
            HeightmapDeltas[modPos.y, modPos.x] = val;
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
            return modPos.x >= 0 && modPos.y >= 0 && modPos.x < Width && modPos.y < Height;
        }
        public bool Intersects(Vector3 scenePos)
        {
            return Within(SceneToModCellPos(scenePos));
        }
        public bool Intersects(Vector2 lowerScenePos, Vector2 higherScenePos)
        {
            Vector2 thisOrigin = Position.ScenePosition.ToVector2XZ();
            Vector2 thisOriginEnd = thisOrigin + new Vector2(Width * tilePosToTileScale, Height * tilePosToTileScale);
            return (thisOrigin.x <= higherScenePos.x || lowerScenePos.x <= thisOriginEnd.x) &&
                (thisOrigin.y <= higherScenePos.y || lowerScenePos.y <= thisOriginEnd.y);
        }
        public bool Intersects(IntVector2 lowerModPos, IntVector2 higherModPos)
        {
            Vector2 thisOriginEnd = new IntVector2(Width , Height);
            return (0 <= higherModPos.x || lowerModPos.x <= thisOriginEnd.x) &&
                (0 <= higherModPos.y || lowerModPos.y <= thisOriginEnd.y);
        }
        public bool Intersects(WorldTile WT)
        {
            Vector2 thisOrigin = Position.ScenePosition.ToVector2XZ();
            Vector2 thisOriginEnd = thisOrigin + new Vector2(Width * tilePosToTileScale * 2, Height * tilePosToTileScale * 2);
            Vector2 tileOrigin = WT.CalcSceneOrigin().ToVector2XZ();
            Vector2 tileOriginEnd = tileOrigin + new Vector2(ManWorld.inst.TileSize, ManWorld.inst.TileSize);
            return (thisOrigin.x <= tileOriginEnd.x || tileOrigin.x <= thisOriginEnd.x) &&
                (thisOrigin.y <= tileOriginEnd.y || tileOrigin.y <= thisOriginEnd.y);
        }
        public bool Intersects(WorldTile WT, Vector3 ModSceneOverride)
        {
            Vector2 thisOrigin = ModSceneOverride.ToVector2XZ();
            Vector2 thisOriginEnd = thisOrigin + new Vector2(Width * tilePosToTileScale, Height * tilePosToTileScale);
            Vector2 tileOrigin = WT.CalcSceneOrigin().ToVector2XZ();
            Vector2 tileOriginEnd = tileOrigin + new Vector2(ManWorld.inst.TileSize * 2, ManWorld.inst.TileSize * 2);
            return (thisOrigin.x <= tileOriginEnd.x || thisOriginEnd.x >= tileOrigin.x) &&
                (thisOrigin.y <= tileOriginEnd.y || thisOriginEnd.y >= tileOrigin.y);
        }

        public void Flush(Vector3 ModSceneOverride)
        {
            deltaed = false;
            ManWorld.inst.TileManager.GetTileCoordRange(
                new Bounds(ModSceneOverride, Vector3.one * ApproxRadiusDampen),
                out var vec1, out var vec2);
            foreach (var item in ManWorld.inst.TileManager.IterateTiles(vec1, vec2, WorldTile.State.Created))
            {
                if (item.Terrain != null && Intersects(item, ModSceneOverride))
                    DoFlush(item, ModSceneOverride);
            }
            foreach (var item in ManWorld.inst.TileManager.IterateTiles(vec1, vec2, WorldTile.State.Created))
            {
                if (item.Terrain != null && Intersects(item, ModSceneOverride))
                    PushFlush(item);
            }
        }
        public void Flush()
        {
            Flush(Position.ScenePosition);
        }
        private void DoFlush(WorldTile Target, Vector3 ModSceneOverride)
        {
            float dampen;
            float dampenInv;
            var TD = Target.Terrain.terrainData;
            float[,] heightsPrev = TD.GetHeights(0, 0, CellsInTileIndexer, CellsInTileIndexer);
            IntVector2 delta = SceneToModCellPos(Target.Terrain.transform.position, ModSceneOverride);
            //BoundsInt BI = new BoundsInt();
            for (int x = 0; x < heightsPrev.GetLength(0); x++)
            {
                for (int y = 0; y < heightsPrev.GetLength(1); y++)
                {
                    IntVector2 inTilePos = new IntVector2(x, y);
                    IntVector2 modPos = inTilePos + delta;
                    if (Within(modPos))
                    {
                        heightsPrev[y, x] = GetTileHeightFromModCellPos(modPos);
                    }
                    else
                    {
                        modPos = new IntVector2(Mathf.Clamp(modPos.x, 0, Width - 1),
                            Mathf.Clamp(modPos.y, 0, Height - 1));
                        dampenInv = Mathf.Clamp01(Mathf.Max(Mathf.Abs(x - modPos.x), Mathf.Abs(y - modPos.y)) / EdgeDampening);
                        dampen = 1 - dampenInv;
                        heightsPrev[y, x] = (heightsPrev[y, x] * dampenInv) +
                            (GetTileHeightFromModCellPos(modPos) * dampen);
                    }
                }
            }

            PushChanges(TD, heightsPrev);
        }

        public void FlushAdd(float AddMultiplier, Vector3 ModSceneOverride)
        {
            deltaed = false;
            ManWorld.inst.TileManager.GetTileCoordRange(
                new Bounds(ModSceneOverride, Vector3.one * ApproxRadiusDampen),
                out var vec1, out var vec2);
            foreach (var item in ManWorld.inst.TileManager.IterateTiles(vec1, vec2, WorldTile.State.Created))
            {
                if (item.Terrain != null && Intersects(item, ModSceneOverride))
                    DoAdd(item, ref AddMultiplier, ModSceneOverride);
            }
            foreach (var item in ManWorld.inst.TileManager.IterateTiles(vec1, vec2, WorldTile.State.Created))
            {
                if (item.Terrain != null && Intersects(item, ModSceneOverride))
                    PushFlush(item);
            }
        }
        public void FlushAdd(float AddMultiplier)
        {
            FlushAdd(AddMultiplier, Position.ScenePosition);
        }
        private void DoAdd(WorldTile Target, ref float addMulti, Vector3 ModSceneOverride)
        {
            var TD = Target.Terrain.terrainData;
            float[,] heightsPrev = TD.GetHeights(0, 0, CellsInTileIndexer, CellsInTileIndexer);
            IntVector2 delta = SceneToModCellPos(Target.Terrain.transform.position, ModSceneOverride);
            //BoundsInt BI = new BoundsInt();
            for (int x = 0; x < heightsPrev.GetLength(0); x++)
            {
                for (int y = 0; y < heightsPrev.GetLength(1); y++)
                {
                    IntVector2 inTilePos = new IntVector2(x, y);
                    IntVector2 modPos = inTilePos + delta;
                    if (Within(modPos))
                    {
                        heightsPrev[y, x] = Mathf.Clamp01(heightsPrev[y, x] +
                            (GetTileHeightFromModCellPos(modPos) * addMulti));
                    }
                }
            }

            PushChanges(TD, heightsPrev);
        }

        public void FlushApply(float intensity, Vector3 ModSceneOverride)
        {
            deltaed = false;
            intensity = Mathf.Clamp01(intensity);
            float invIntensity = 1 - intensity;
            ManWorld.inst.TileManager.GetTileCoordRange(
                new Bounds(ModSceneOverride, Vector3.one * ApproxRadiusDampen),
                out var vec1, out var vec2);
            foreach (var item in ManWorld.inst.TileManager.IterateTiles(vec1, vec2, WorldTile.State.Created))
            {
                if (item.Terrain != null && Intersects(item, ModSceneOverride))
                    DoApply(item, ref intensity, ref invIntensity, ModSceneOverride);
            }
            foreach (var item in ManWorld.inst.TileManager.IterateTiles(vec1, vec2, WorldTile.State.Created))
            {
                if (item.Terrain != null && Intersects(item, ModSceneOverride))
                    PushFlush(item);
            }
        }
        public void FlushApply(float intensity)
        {
            FlushApply(intensity, Position.ScenePosition);
        }
        private void DoApply(WorldTile Target, ref float intensity, ref float invIntensity, Vector3 ModSceneOverride)
        {
            float dampen;
            float dampenInv;
            var TD = Target.Terrain.terrainData;
            float[,] heightsPrev = TD.GetHeights(0, 0, CellsInTileIndexer, CellsInTileIndexer);
            IntVector2 delta = SceneToModCellPos(Target.Terrain.transform.position, ModSceneOverride);
            //BoundsInt BI = new BoundsInt();
            for (int x = 0; x < heightsPrev.GetLength(0); x++)
            {
                for (int y = 0; y < heightsPrev.GetLength(1); y++)
                {
                    IntVector2 inTilePos = new IntVector2(x, y);
                    IntVector2 modPos = inTilePos + delta;
                    if (Within(modPos))
                    {
                        heightsPrev[y, x] = Mathf.Clamp01(heightsPrev[x, y] * invIntensity) +
                            (GetTileHeightFromModCellPos(modPos) * intensity);
                    }
                    else
                    {
                        modPos = new IntVector2(Mathf.Clamp(modPos.x, 0, Width - 1),
                            Mathf.Clamp(modPos.y, 0, Height - 1));
                        dampenInv = Mathf.Clamp01(Mathf.Max(Mathf.Abs(x - modPos.x), Mathf.Abs(y - modPos.y)) / EdgeDampening) * intensity;
                        dampen = 1 - dampenInv;
                        heightsPrev[y, x] = (heightsPrev[y, x] * dampenInv) +
                            (GetTileHeightFromModCellPos(modPos) * dampen);
                    }
                }
            }

            PushChanges(TD, heightsPrev);
        }

        public void FlushLevel(int gradientRadius, float intensity, Vector3 ModSceneOverride)
        {
            deltaed = false;
            intensity = Mathf.Clamp01(intensity);
            ManWorld.inst.TileManager.GetTileCoordRange(
                new Bounds(ModSceneOverride, Vector3.one * ApproxRadiusDampen),
                out var vec1, out var vec2);
            foreach (var item in ManWorld.inst.TileManager.IterateTiles(vec1, vec2, WorldTile.State.Created))
            {
                if (item.Terrain != null && Intersects(item, ModSceneOverride))
                    DoLevel(item, gradientRadius, ref intensity, ModSceneOverride);
            }
            foreach (var item in ManWorld.inst.TileManager.IterateTiles(vec1, vec2, WorldTile.State.Created))
            {
                if (item.Terrain != null && Intersects(item, ModSceneOverride))
                    PushFlush(item);
            }
        }
        public void FlushLevel(int gradientRadius, float intensity) =>
            FlushLevel(gradientRadius, intensity, Position.ScenePosition);
        private void DoLevel(WorldTile Target, int gradientRadius, ref float intensity, Vector3 ModSceneOverride)
        {
            float dampen;
            float dampenInv;
            var TD = Target.Terrain.terrainData;
            float[,] heightsPrev = TD.GetHeights(0, 0, CellsInTileIndexer, CellsInTileIndexer);
            IntVector2 origin = TileWorldSceneOriginCellPos(Target);
            IntVector2 delta = SceneToModCellPos(Target.Terrain.transform.position, ModSceneOverride);
            //BoundsInt BI = new BoundsInt();
            int widthT = heightsPrev.GetLength(0);
            int heightT = heightsPrev.GetLength(1);
            for (int x = 0; x < widthT; x++)
            {
                for (int y = 0; y < heightT; y++)
                {
                    IntVector2 inTilePos = new IntVector2(x, y);
                    IntVector2 modPos = inTilePos + delta;
                    if (Within(modPos))
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
                                                 Target.Terrain.transform.position.y) / TD.size.y;
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
                            (GetTileHeightFromModCellPos(modPos) * normalizingForces * intensity));
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

            PushChanges(TD, heightsPrev);
        }


        public void FlushReset(float intensity, Vector3 ModSceneOverride)
        {
            deltaed = false;
            intensity = Mathf.Clamp01(intensity);
            ManWorld.inst.TileManager.GetTileCoordRange(
                new Bounds(ModSceneOverride, Vector3.one * ApproxRadiusDampen),
                out var vec1, out var vec2);
            foreach (var item in ManWorld.inst.TileManager.IterateTiles(vec1, vec2, WorldTile.State.Created))
            {
                if (item.Terrain != null && Intersects(item, ModSceneOverride))
                    DoReset(item, ref intensity, ModSceneOverride);
            }
            foreach (var item in ManWorld.inst.TileManager.IterateTiles(vec1, vec2, WorldTile.State.Created))
            {
                if (item.Terrain != null && Intersects(item, ModSceneOverride))
                    PushFlush(item);
            }
        }
        public void FlushReset(float intensity) => FlushReset(intensity, Position.ScenePosition);
        private void DoReset(WorldTile Target, ref float intensity, Vector3 ModSceneOverride)
        {
            float dampen;
            float dampenInv;
            var TD = Target.Terrain.terrainData;
            float[,] heightsPrev = TD.GetHeights(0, 0, CellsInTileIndexer, CellsInTileIndexer);
            IntVector2 delta = SceneToModCellPos(Target.Terrain.transform.position, ModSceneOverride);
            //BoundsInt BI = new BoundsInt();
            int widthT = heightsPrev.GetLength(0);
            int heightT = heightsPrev.GetLength(1);
            for (int x = 0; x < widthT; x++)
            {
                for (int y = 0; y < heightT; y++)
                {
                    IntVector2 inTilePos = new IntVector2(x, y);
                    IntVector2 modPos = inTilePos + delta;
                    if (Within(modPos))
                    {
                        if (intensity >= 0)
                        {
                            float OGHeight = Target.BiomeMapData.heightData.heights[y, x];
                            float normalizingForces = OGHeight - heightsPrev[y, x];
                            heightsPrev[y, x] = Mathf.Clamp01(heightsPrev[y, x] + (GetTileHeightFromModCellPos(modPos) *
                                normalizingForces * intensity));
                        }
                        else
                        {
                            intensity = -intensity;
                            float OGHeight = Target.BiomeMapData.heightData.heights[y, x];
                            float normalizingForces = OGHeight - heightsPrev[y, x];
                            heightsPrev[y, x] = Mathf.Clamp01(heightsPrev[y, x] + (GetTileHeightFromModCellPos(modPos) *
                                normalizingForces * intensity));
                        }
                    }
                }
            }

            PushChanges(TD, heightsPrev);
        }

        public void FlushRamp(float intensity, Vector3 start, Vector3 end, Vector3 ModSceneOverride)
        {
            deltaed = false;
            ManWorld.inst.TileManager.GetTileCoordRange(
                new Bounds(ModSceneOverride, Vector3.one * ApproxRadiusDampen),
                out var vec1, out var vec2);
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
                if (item.Terrain != null && Intersects(item, ModSceneOverride))
                    DoRamp(item, ref intensity, ModSceneOverride, lowHeight, highHeight,
                        rampCenter, vecNormal);
            }
            foreach (var item in ManWorld.inst.TileManager.IterateTiles(vec1, vec2, WorldTile.State.Created))
            {
                if (item.Terrain != null && Intersects(item, ModSceneOverride))
                    PushFlush(item);
            }
        }
        private void DoRamp(WorldTile Target, ref float intensity, Vector3 ModSceneOverride,
            float lowHeight, float highHeight, Vector3 rampCenter, Vector3 vecNormal)
        {
            var TD = Target.Terrain.terrainData;
            float[,] heightsPrev = TD.GetHeights(0, 0, CellsInTileIndexer, CellsInTileIndexer);
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
                    if (Within(modPos))
                    {
                        float targetHeight = Revec(rampCenter, CellPosWorld, heightsPrev[y, x], vecNormal);
                        float normalizingForces = Mathf.Clamp(targetHeight, lowHeight, highHeight) - heightsPrev[y, x];
                        heightsPrev[y, x] = Mathf.Clamp01(heightsPrev[y, x] + Mathf.Clamp(normalizingForces * 
                            GetTileHeightFromModCellPos(modPos), -intensity, intensity));
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


        private void PushChanges(TerrainData TD, float[,] newMap)
        {
            TD.SetHeights(0, 0, newMap);
            //TD.UpdateDirtyRegion(0, 0, CellsInTile, CellsInTile, true);
        }
        private static MethodInfo flusher = typeof(TileManager).GetMethod("ConnectNeighbouringTilesAndFlush", 
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.InvokeMethod);
        private static object[] flushCall = new object[2] { null, null };
        private void PushFlush(WorldTile WT)
        {
            flushCall[0] = WT;
            flushCall[1] = true;
            bool prevState = Globals.inst.m_ManuallyConnectTerrainTiles;
            Globals.inst.m_ManuallyConnectTerrainTiles = false;
            flusher.Invoke(ManWorld.inst.TileManager, flushCall);
            Terrain.SetConnectivityDirty();
            WorldDeformer.terrainsDeformed.Add(WT);
            Globals.inst.m_ManuallyConnectTerrainTiles = prevState;
        }
    }
}
#endif