using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

#if !EDITOR
namespace TerraTechETCUtil
{
    public interface ITileLoader
    {
        void GetActiveTiles(List<IntVector2> tileCache);
    }


    public class ManWorldTileExt
    {
        private static bool init = false;
        public static Dictionary<IntVector2, WorldTile.State> RequestedLoaded => LoadedTileCoords;
        public static Dictionary<IntVector2, WorldTile.State> Perimeter => PerimeterTileSubLoaded;
        private static readonly Dictionary<IntVector2, float> TempLoaders = new Dictionary<IntVector2, float>();
        private static readonly HashSet<IntVector2> FixedTileLoaders = new HashSet<IntVector2>();
        private static readonly List<ITileLoader> DynamicTileLoaders = new List<ITileLoader>();
        private static readonly Dictionary<IntVector2, WorldTile.State> SetTiles = new Dictionary<IntVector2, WorldTile.State>();

        public const float TempDurationDefault = 6; // In seconds
        public const float MaxWorldPhysicsSafeDistance = 100_000; // In blocks
        private static int _MaxWorldPhysicsSafeDistanceTiles = Mathf.CeilToInt(100_000 / ManWorld.inst.TileSize) - 1; // In blocks
        public static int MaxWorldPhysicsSafeDistanceTiles => _MaxWorldPhysicsSafeDistanceTiles; // In blocks

        public static Dictionary<IntVector2, WorldTile.State> LoadedTileCoords = new Dictionary<IntVector2, WorldTile.State>();

        public static Dictionary<IntVector2, WorldTile.State> PerimeterTileSubLoaded = new Dictionary<IntVector2, WorldTile.State>();

        public static void InsureInit()
        {
            if (init)
                return;
            init = true;
            InvokeHelper.InvokeSingleRepeat(UpdateTileLoading, 0.6f);
            MassPatcher.MassPatchAllWithin(LegModExt.harmonyInstance, typeof(WorldTilePatches), "TerraTechModExt");
        }

        public static void ClearAll()
        {
            FixedTileLoaders.Clear();
            DynamicTileLoaders.Clear();
        }
        public static void SetTileLoading(IntVector2 worldTilePos, bool Yes)
        {
            InsureInit();
            if (Yes)
            {
                if (!FixedTileLoaders.Contains(worldTilePos))
                {
                    FixedTileLoaders.Add(worldTilePos);
                }
            }
            else
            {
                if (FixedTileLoaders.Remove(worldTilePos))
                {
                }
            }
        }
        public static void SetTileState(IntVector2 worldTilePos, WorldTile.State state)
        {
            InsureInit();
            SetTiles[worldTilePos] = state;
        }
        public static void ClearTileState(IntVector2 worldTilePos)
        {
            SetTiles.Remove(worldTilePos);
        }
        public static bool TempLoadTile(IntVector2 posTile, float loadTime = TempDurationDefault)
        {
            InsureInit();
            if (!TempLoaders.ContainsKey(posTile))
            {
                //DebugRandAddi.Info("TEMP LOADING TILE (extended) " + posTile.ToString());
                TempLoaders.Add(posTile, loadTime + Time.time);
            }
            else
            {
                Debug_TTExt.Info("TEMP LOADING TILE " + posTile.ToString());
                TempLoaders[posTile] = loadTime + Time.time;
            }
            return true;
        }
        /// <summary>
        /// DANGEROUS - ONLY USE IN DEBUG TESTING ENVIRONMENT
        /// </summary>
        public static bool ReloadTile(Vector3 scenePos)
        {
            InsureInit();
            if (ManWorld.inst.TileManager.IsTileAtPositionLoaded(scenePos))
            {
                IntVector2 tilePos = WorldPosition.FromScenePosition(scenePos).TileCoord;
                SetTileState(tilePos, WorldTile.State.Empty);
                InvokeHelper.Invoke(ClearTileState, 1, tilePos);
                //LoadedTileCoords
            }
            else
                return true;
            return false;
        }/// <summary>
         /// DANGEROUS - ONLY USE IN DEBUG TESTING ENVIRONMENT
         /// </summary>
        public static bool ReloadTile(IntVector2 tilePos)
        {
            InsureInit();
            if (ManWorld.inst.TileManager.IsTileAtPositionLoaded(ManWorld.inst.TileManager.CalcTileCentreScene(tilePos)))
            {
                SetTileState(tilePos, WorldTile.State.Empty);
                InvokeHelper.Invoke(ClearTileState, 1, tilePos);
                //LoadedTileCoords
            }
            else
                return true;
            return false;
        }
        public static bool RegisterDynamicTileLoader(ITileLoader loader)
        {
            InsureInit();
            if (loader != null)
            {
                if (!DynamicTileLoaders.Contains(loader))
                {
                    DynamicTileLoaders.Add(loader);
                    return true;
                }
            }
            else
            {
            }
            return false;
        }
        public static bool UnregisterDynamicTileLoader(ITileLoader loader)
        {
            if (loader != null)
            {
                return DynamicTileLoaders.Remove(loader);
            }
            else
            {
            }
            return false;
        }

        private static readonly List<IntVector2> tilesPosCache = new List<IntVector2>();
        private static void UpdateTileLoading()
        {
            if (LoadedTileCoords != null)
            {
                LoadedTileCoords.Clear();
                foreach (var item in TempLoaders.Keys)
                {
                    if (!LoadedTileCoords.ContainsKey(item))
                        LoadedTileCoords.Add(item, WorldTile.State.Loaded);
                }
                foreach (var item in FixedTileLoaders)
                {
                    if (!LoadedTileCoords.ContainsKey(item))
                        LoadedTileCoords.Add(item, WorldTile.State.Loaded);
                }
                foreach (var item in SetTiles)
                {
                    if (!LoadedTileCoords.ContainsKey(item.Key))
                        LoadedTileCoords.Add(item.Key, item.Value);
                }

                // UPDATE THE DYNAMIC TILES
                foreach (var item in DynamicTileLoaders)
                {
                    item.GetActiveTiles(tilesPosCache);
                    foreach (var pos in tilesPosCache)
                    {
                        if (!LoadedTileCoords.ContainsKey(pos))
                            LoadedTileCoords.Add(pos, WorldTile.State.Loaded);
                    }
                    tilesPosCache.Clear();
                }

                // UPDATE THE TEMP TILES
                int length = TempLoaders.Count;
                for (int step = 0; step < length;)
                {
                    var pos = TempLoaders.ElementAt(step);
                    if (pos.Value < Time.time)
                    {
                        TempLoaders.Remove(pos.Key);
                        length--;
                    }
                    else
                        step++;
                }
                IntVector2 loadOrigin = ManWorld.inst.FloatingOriginTile;
                int minCoordsX = loadOrigin.x - MaxWorldPhysicsSafeDistanceTiles;
                int minCoordsY = loadOrigin.y - MaxWorldPhysicsSafeDistanceTiles;
                int maxCoordsX = loadOrigin.x + MaxWorldPhysicsSafeDistanceTiles;
                int maxCoordsY = loadOrigin.y + MaxWorldPhysicsSafeDistanceTiles;
                for (int step = LoadedTileCoords.Count - 1; step > -1; step--)
                {
                    IntVector2 pos = LoadedTileCoords.ElementAt(step).Key;
                    if (pos.x < minCoordsX || pos.y < minCoordsY || pos.x > maxCoordsX || pos.y > maxCoordsY)
                        LoadedTileCoords.Remove(pos);
                }
                GetActiveTilePerimeterForRequestedLoaded();
            }
        }

        public static int lastTechID = -1;
        public static float lastTechUpdateTime = -1;

        /*
        public void Update()
        {
            if (!ManGameMode.inst.GetIsInPlayableMode())
                MaintainPlayerTech();
        }*/

        private static void GetActiveTilePerimeterForRequestedLoaded()
        {
            PerimeterTileSubLoaded.Clear();
            foreach (var item in LoadedTileCoords)
            {
                AddActiveTilePerimeterAroundPosition(item.Key, ref PerimeterTileSubLoaded);
            }
            foreach (var item in LoadedTileCoords)
            {
                PerimeterTileSubLoaded.Remove(item.Key);
            }
        }
        private static void AddActiveTilePerimeterAroundPosition(IntVector2 posSpot, ref Dictionary<IntVector2, WorldTile.State> perimeter)
        {
            IntVector2 newSpot;
            newSpot = posSpot + new IntVector2(-1, -1);
            TryAddTilePerimeterAroundPosition(ref newSpot, ref perimeter);
            newSpot = posSpot + new IntVector2(0, -1);
            TryAddTilePerimeterAroundPosition(ref newSpot, ref perimeter);
            newSpot = posSpot + new IntVector2(1, -1);
            TryAddTilePerimeterAroundPosition(ref newSpot, ref perimeter);
            newSpot = posSpot + new IntVector2(-1, 0);
            TryAddTilePerimeterAroundPosition(ref newSpot, ref perimeter);
            newSpot = posSpot + new IntVector2(1, 0);
            TryAddTilePerimeterAroundPosition(ref newSpot, ref perimeter);
            newSpot = posSpot + new IntVector2(-1, 1);
            TryAddTilePerimeterAroundPosition(ref newSpot, ref perimeter);
            newSpot = posSpot + new IntVector2(0, 1);
            TryAddTilePerimeterAroundPosition(ref newSpot, ref perimeter);
            newSpot = posSpot + new IntVector2(1, 1);
            TryAddTilePerimeterAroundPosition(ref newSpot, ref perimeter);
        }
        private static void TryAddTilePerimeterAroundPosition(ref IntVector2 perimeterToAdd, ref Dictionary<IntVector2, WorldTile.State> perimeter)
        {
            if (perimeter.ContainsKey(perimeterToAdd))
                return;
            else
                perimeter.Add(perimeterToAdd, WorldTile.State.Loaded);
        }

        public static void GetActiveTilesAround(List<IntVector2> cache, WorldPosition WP, int MaxTileLoadingDiameter)
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

    }

    internal class WorldTilePatches
    {
        internal static class TileManagerPatches
        {
            internal static Type target = typeof(TileManager);

            private static bool removeCorruptedTest = false;
            private const float maxDistFromOrigin = 80000;
            private static List<IntVector2> starTiles = null;
            private static Dictionary<IntVector2, WorldTile> exisTiles = null;
            static FieldInfo TilesNew = typeof(TileManager).GetField("m_TileCoordsToCreateWorking", BindingFlags.NonPublic | BindingFlags.Instance);
            static FieldInfo Tiles = typeof(TileManager).GetField("m_TileLookup", BindingFlags.NonPublic | BindingFlags.Instance);
            /// <summary>
            /// EnableTileLoading
            /// </summary>
            private static void UpdateTileRequestStatesInStandardMode_Postfix(TileManager __instance)
            {
                try
                {
                    if (starTiles == null)
                    {
                        Debug_TTExt.Log("ManTileLoader - Fetching tiles to create");
                        starTiles = (List<IntVector2>)TilesNew.GetValue(__instance);
                        Debug_TTExt.Log("ManTileLoader - Fetched tiles to create");
                    }
                    if (exisTiles == null)
                    {
                        Debug_TTExt.Log("ManTileLoader - Fetching tile lookup");
                        exisTiles = (Dictionary<IntVector2, WorldTile>)Tiles.GetValue(__instance);
                        Debug_TTExt.Log("ManTileLoader - Fetched tile lookup");
                    }

                    Debug_TTExt.Assert(ManWorldTileExt.RequestedLoaded == null, "ManTileLoader - RequestedLoaded IS NULL");
                    int requests = ManWorldTileExt.Perimeter.Count;
                    for (int step = 0; step < requests; step++)
                    {
                        var item = ManWorldTileExt.Perimeter.ElementAt(step);
                        if (item.Key != null)
                        {
                            Vector3 pos = ManWorld.inst.TileManager.CalcTileCentreScene(item.Key);
                            if (pos.x > -maxDistFromOrigin && pos.x < maxDistFromOrigin &&
                                pos.y > -maxDistFromOrigin && pos.y < maxDistFromOrigin &&
                                pos.z > -maxDistFromOrigin && pos.z < maxDistFromOrigin)
                            {
                                if (exisTiles.TryGetValue(item.Key, out WorldTile WT))
                                {
                                    if (WT != null)
                                    {
                                        //DebugRandAddi.Log("ManTileLoader - Loading tile at " + WT.Coord);
                                        if (WT.m_RequestState < WorldTile.State.Created)
                                            WT.m_RequestState = WorldTile.State.Created;
                                    }
                                    else
                                        Debug_TTExt.Assert("ManTileLoader(Perimeter) - Tile at " + item.Key + " is NULL");
                                }
                                else
                                {
                                    if (!starTiles.Contains(item.Key))
                                    {
                                        starTiles.Add(item.Key);
                                        Debug_TTExt.Info("ManTileLoader(Perimeter) - Force-loading NEW Tile at " + item.Key);
                                    }
                                }
                            }
                        }
                    }
                    requests = ManWorldTileExt.RequestedLoaded.Count;
                    for (int step = 0; step < requests;)
                    {
                        var item = ManWorldTileExt.RequestedLoaded.ElementAt(step);
                        if (item.Key != null)
                        {
                            Vector3 pos = ManWorld.inst.TileManager.CalcTileCentreScene(item.Key);
                            if (pos.x > -maxDistFromOrigin && pos.x < maxDistFromOrigin &&
                                pos.y > -maxDistFromOrigin && pos.y < maxDistFromOrigin &&
                                pos.z > -maxDistFromOrigin && pos.z < maxDistFromOrigin)
                            {
                                if (exisTiles.TryGetValue(item.Key, out WorldTile WT))
                                {
                                    if (WT != null)
                                    {
                                        //DebugRandAddi.Log("ManTileLoader - Loading tile at " + WT.Coord);
                                        WT.m_RequestState = item.Value;
                                    }
                                    else
                                        Debug_TTExt.Assert("ManTileLoader - Tile at " + item.Key + " is NULL");
                                }
                                else
                                {
                                    if (!starTiles.Contains(item.Key))
                                    {
                                        starTiles.Add(item.Key);
                                        Debug_TTExt.Info("ManTileLoader - Force-loading NEW Tile at " + item.Key);
                                    }
                                }
                            }
                            step++;
                        }
                        else
                        {
                            Debug_TTExt.Assert("ManTileLoader - NULL TILE IN RequestedLoaded, canceled.");
                            ManWorldTileExt.RequestedLoaded.Remove(item.Key);
                            requests--;
                        }
                    }
                    if (removeCorruptedTest)
                    {
                        foreach (var item in new Dictionary<IntVector2, WorldTile>(exisTiles))
                        {
                            if (item.Key == null || item.Value == null || item.Value.patchesToPopulate == null
                                || item.Value.SaveData == null)
                            {
                                Debug_TTExt.Assert(item.Key == null, "ManTileLoader - NULL TILE  Key in TileManager somehow!?");
                                Debug_TTExt.Assert(item.Value == null, "ManTileLoader - NULL TILE  WorldTile in TileManager somehow? Removing...");
                                Debug_TTExt.Assert(item.Value.patchesToPopulate == null,
                                    "ManTileLoader - NULL TILE  patchesToPopulate in TileManager somehow? Removing...");
                                Debug_TTExt.Assert(item.Value.SaveData == null,
                                    "ManTileLoader - NULL TILE " + item.Key + "  SaveData in TileManager somehow? Removing...");
                                exisTiles.Remove(item.Key);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    throw new Exception("ManTileLoader encountered an error - " + e);
                }
            }
        }
    }

}
#endif