﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using Ionic.Zlib;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using static CompoundExpression;

#if !EDITOR
namespace TerraTechETCUtil
{
    public interface ITileLoader
    {
        void GetActiveTiles(List<IntVector2> tileCache);
    }


    public class ManWorldTileExt
    {
        public const float BroadphasePhysicsExtendSizeMultiplier = 64;
        public enum PhysicsMode
        {
            DefaultBroadphase,
            Standard,
            BroadphaseExtended,
            BroadphaseDynamic,
        }

        private static bool init = false;
        private static PhysicsMode phyMode = PhysicsMode.DefaultBroadphase;
        public static bool AutomaticBroadphase => Globals.inst.DynamicMultiBoxBroadphaseRegions;

        public static Bounds BoundsPhysics = default;


        
        public enum NetWTMsgType
        {
            None,
            TileStatesUpdate,
            FixedTilesUpdate,
            DynamicTilesUpdate,
        }
        public class NetWTMessage : MessageBase
        {
            public NetWTMessage() { }
            public NetWTMessage(Dictionary<IntVector2, WorldTile.State> rawData)
            {
                this.type = (uint)NetWTMsgType.TileStatesUpdate;
                AssignData(rawData);
            }
            public NetWTMessage(HashSet<IntVector2> rawData)
            {
                this.type = (uint)NetWTMsgType.FixedTilesUpdate;
                AssignData(rawData);
            }
            public NetWTMessage(List<ITileLoader> rawData)
            {
                this.type = (uint)NetWTMsgType.DynamicTilesUpdate;
                AssignData(rawData);
            }
            private void AssignData(object rawData)
            {
                using (MemoryStream FS = new MemoryStream())
                {
                    using (GZipStream GZS = new GZipStream(FS, CompressionMode.Compress))
                    {
                        using (StreamWriter SW = new StreamWriter(GZS))
                        {
                            SW.WriteLine(JsonConvert.SerializeObject(rawData, Formatting.None));
                            SW.Flush();
                        }
                    }
                    infoBytes = FS.ToArray();
                }
            }
            public Dictionary<IntVector2, WorldTile.State> ExtractTileState() =>
                ExtractData<Dictionary<IntVector2, WorldTile.State>>(NetWTMsgType.TileStatesUpdate);
            public HashSet<IntVector2> ExtractFixedTileState() =>
                ExtractData<HashSet<IntVector2>>(NetWTMsgType.FixedTilesUpdate);

            public List<ITileLoader> ExtractDynamicTileState() =>
                ExtractData<List<ITileLoader>>(NetWTMsgType.DynamicTilesUpdate);


            private T ExtractData<T>(NetWTMsgType type) where T : class
            {
                if ((NetWTMsgType)this.type != type)
                    throw new InvalidCastException("Message is not of casted type \"" + (NetWTMsgType)this.type + "\", it is type \"" + type + "\"");
                using (MemoryStream FS = new MemoryStream(infoBytes))
                {
                    using (GZipStream GZS = new GZipStream(FS, CompressionMode.Decompress))
                    {
                        using (StreamReader SR = new StreamReader(GZS))
                        {
                            return JsonConvert.DeserializeObject<T>(SR.ReadToEnd());
                        }
                    }
                }
            }

            public uint type;
            public byte[] infoBytes;
        }
        private static NetworkHook<NetWTMessage>  netHook = new NetworkHook<NetWTMessage>("TerraTechETCUtil.NetWTMessage", 
            OnWTMsgNetwork, NetMessageType.ToClientsOnly);
            
        private static bool OnWTMsgNetwork(NetWTMessage command, bool isServer)
        {
            NetWTMsgType type = (NetWTMsgType)command.type;
            switch (type)
            {
                case NetWTMsgType.None:
                    break;
                case NetWTMsgType.TileStatesUpdate:
                    SetTiles.Clear();
                    var state = command.ExtractTileState();
                    foreach (var item in state)
                        SetTiles.Add(item.Key, item.Value);
                    break;
                case NetWTMsgType.FixedTilesUpdate:
                    FixedTileLoaders.Clear();
                    var state2 = command.ExtractFixedTileState();
                    foreach (var item in state2)
                        FixedTileLoaders.Add(item);
                    break;
                case NetWTMsgType.DynamicTilesUpdate:

                    break;
                default:
                    break;
            }
            return true;
        }


        public static Dictionary<IntVector2, WorldTile.State> RequestedLoaded => LoadedTileCoords;
        private static readonly Dictionary<IntVector2, float> TempLoaders = new Dictionary<IntVector2, float>();
        private static readonly HashSet<IntVector2> FixedTileLoaders = new HashSet<IntVector2>();
        private static readonly List<ITileLoader> DynamicTileLoaders = new List<ITileLoader>();
        private static readonly Dictionary<IntVector2, WorldTile.State> SetTiles = new Dictionary<IntVector2, WorldTile.State>();

        private static bool rushTiles = false;
        public static bool RushingTiles => rushTiles;

        public const float TempDurationDefault = 6; // In seconds
        public const float RushLoadingDuration = 1.5f; // In seconds
        public const float MaxWorldPhysicsSafeDistance = 100_000; // In blocks

        private static int _MaxWorldPhysicsSafeDistanceTiles = Mathf.CeilToInt(MaxWorldPhysicsSafeDistance / ManWorld.inst.TileSize) - 1; // In worldTile Coords
        public static int MaxWorldPhysicsSafeDistanceTiles => _MaxWorldPhysicsSafeDistanceTiles; // In worldTile Coords

        private static Dictionary<IntVector2, WorldTile.State> LoadedTileCoords = new Dictionary<IntVector2, WorldTile.State>();

        private static Dictionary<IntVector2, WorldTile.State> PerimeterTileSubLoaded = new Dictionary<IntVector2, WorldTile.State>();

        public static void FORCEExtendedBroadphase() => SetExtendedBroadphase();
        internal static void SetDynamicBroadphase()
        {
            if (phyMode == PhysicsMode.BroadphaseDynamic)
                return;
            Globals.inst.DynamicMultiBoxBroadphaseRegions = true;
            phyMode = PhysicsMode.BroadphaseDynamic;
            Debug_TTExt.Log("Setting physics to " + phyMode);
        }
        internal static void SetExtendedBroadphase()
        {
            if (phyMode == PhysicsMode.BroadphaseExtended)
                return;
            if (BoundsPhysics == default)
                BoundsPhysics = (Bounds)typeof(TileManager).GetField("physicsBounds", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ManWorld.inst.TileManager);

            if (BoundsPhysics == default)
            {
                Debug_TTExt.Log("BoundsPhysics is not set, making new...");
                float tileSizeExts = ManWorld.inst.TileSize * 8;
                BoundsPhysics.min = new Vector3(-tileSizeExts, Globals.inst.m_VisibleEmergencyKillHeight, -tileSizeExts);
                BoundsPhysics.max = new Vector3(tileSizeExts, Globals.inst.m_VisibleEmergencyKillMaxHeight, tileSizeExts);
            }

            //Globals.inst.DynamicMultiBoxBroadphaseRegions = false;
            if (Globals.inst.DynamicMultiBoxBroadphaseRegions)
                Debug_TTExt.Log("Note: physics was already set to dynamic");
            Globals.inst.DynamicMultiBoxBroadphaseRegions = true;
            phyMode = PhysicsMode.BroadphaseExtended;
            Debug_TTExt.Log("Setting physics to " + phyMode);
            Debug_TTExt.Log("Physics bounds was previously " + BoundsPhysics.min + ", " + BoundsPhysics.max);
            Vector3 temp = BoundsPhysics.min;
            temp.Scale(new Vector3(BroadphasePhysicsExtendSizeMultiplier, 1f, BroadphasePhysicsExtendSizeMultiplier));
            BoundsPhysics.min = temp;
            temp = BoundsPhysics.max;
            temp.Scale(new Vector3(BroadphasePhysicsExtendSizeMultiplier, 1f, BroadphasePhysicsExtendSizeMultiplier));
            BoundsPhysics.max = temp;
            Physics.RebuildBroadphaseRegions(BoundsPhysics, 16);
            typeof(TileManager).GetField("physicsBounds", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(ManWorld.inst.TileManager, BoundsPhysics);
            Debug_TTExt.Log("Expanded physics bounds to " + BoundsPhysics.min + ", " + BoundsPhysics.max);
        }

        public static void InsureInit()
        {
            if (init)
                return;
            init = true;
            netHook.Enable();
            ManTechs.inst.TankPostSpawnEvent.Subscribe(InsureTechNotUnderTerrain);
            InvokeHelper.InvokeSingleRepeat(SlowUpdateTileLoading, 0.6f);
            MassPatcher.MassPatchAllWithin(LegModExt.harmonyInstance, typeof(WorldTilePatches), "TerraTechModExt");
            if (Globals.inst.DynamicMultiBoxBroadphaseRegions)
                phyMode = PhysicsMode.BroadphaseDynamic;
            else
                phyMode = PhysicsMode.DefaultBroadphase;
            Debug_TTExt.Log("Current physics is " + phyMode);
        }
        private static void InsureTechNotUnderTerrain(Tank tech)
        {
            if (tech == null || !ManNetwork.IsHost || tech.IsAnchored)
                return;
            Vector3 pos = tech.boundsCentreWorldNoCheck;
            float height = ManWorld.inst.TileManager.GetTerrainHeightAtPosition(pos, out _, true);
            if (height > pos.y + 2)
            {
                tech.PositionBaseCentred(pos.SetY(height));
                Debug_TTExt.Log("Tech " + (tech.name.NullOrEmpty() ? "NULL" : tech.name) + " spawned but was BELOW terrain and not anchored, setting ABOVE terrain");
            }
        }
        /// <summary>
        /// Rush loading ALL Worldtiles.  Only affects our client
        /// </summary>
        public static void RushTileLoading()
        {
            rushTiles = true;
            InvokeHelper.InvokeSingle(EndOverclockTileLoading, RushLoadingDuration);
        }
        private static void EndOverclockTileLoading() => rushTiles = false;

        /// <summary>
        /// Returns true if we are the host and we can command the ManWorldTileExt. Silent variant with no log spam
        /// </summary>
        public static bool HostCanCommandTileLoaderQuiet()
        {
            return ManNetwork.IsHost;
        }
        /// <summary>
        /// Returns true if we are the host and we can command the ManWorldTileExt
        /// </summary>
        public static bool HostCanCommandTileLoader()
        {
            if (HostCanCommandTileLoaderQuiet())
                return true;
            Debug_TTExt.Assert("Tried to control Tile Loading on client (use the non-Host functions for this!)");
            return false;
        }

        /// <summary>
        /// CLears All WorldTile loaders EXCEPT the Dynamic Tile Loaders.
        /// </summary>
        public static void HostClearAll()
        {
            if (!HostCanCommandTileLoader())
                return;
            TempLoaders.Clear();
            SetTiles.Clear();
            FixedTileLoaders.Clear();
            TileStateDirty = true;
            FixedTileLoadersDirty = true;
        }
        /// <summary>
        /// Set the World tile at a specific coordinate to stay loaded.  
        /// </summary>
        /// <param name="worldTilePos"></param>
        /// <param name="Yes"></param>
        public static void HostSetTileLoading(IntVector2 worldTilePos, bool Yes)
        {
            if (!HostCanCommandTileLoader())
                return;
            InsureInit();
            if (Yes)
            {
                if (!FixedTileLoaders.Contains(worldTilePos))
                {
                    FixedTileLoaders.Add(worldTilePos);
                    FixedTileLoadersDirty = true;
                }
            }
            else
            {
                if (FixedTileLoaders.Remove(worldTilePos))
                {
                    FixedTileLoadersDirty = true;
                }
            }
        }
        private static bool FixedTileLoadersDirty = false;
        /// <summary>
        /// Set a specific tile coordinate to be loaded with a specific state.
        /// </summary>
        /// <param name="worldTilePos"></param>
        /// <param name="state"></param>
        public static void HostSetTileState(IntVector2 worldTilePos, WorldTile.State state)
        {
            if (!HostCanCommandTileLoader())
                return;
            InsureInit();
            SetTiles[worldTilePos] = state;
            TileStateDirty = true;
        }
        /// <summary>
        /// Set a specific tile coordinate to be cleared of it's specific state
        /// </summary>
        public static void HostClearTileState(IntVector2 worldTilePos)
        {
            if (!HostCanCommandTileLoader())
                return;
            if (SetTiles.Remove(worldTilePos))
                TileStateDirty = true;
        }
        private static bool TileStateDirty = false;

        /// <summary>
        /// Check to see if a tile is being loaded.  This must be called once on every client.
        /// </summary>
        public static bool ClientIsRequestLoadingTile(IntVector2 posTile)
        {
            return TempLoaders.ContainsKey(posTile);
        }
        /// <summary>
        /// Make a tile load completely by force!  This must be called once on every client.
        /// </summary>
        public static bool ClientTempLoadTile(IntVector2 posTile, bool rush, float loadTime = TempDurationDefault)
        {
            InsureInit();
            if (rush)
                RushTileLoading();
            if (!TempLoaders.ContainsKey(posTile))
            {
                SetExtendedBroadphase();
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
        /// Force unload and reload a tile.  This is Single-Player only!
        /// </summary>
        public static bool HostOnly_ReloadTile(Vector3 scenePos, bool rush)
        {
            if (!HostCanCommandTileLoader())
                return false;
            InsureInit();
            if (rush)
                RushTileLoading();
            if (ManWorld.inst.TileManager.LookupTile(scenePos)?.IsCreated ?? false)
            {
                if (!IsWithinPhysicsRegions(scenePos, ManWorld.inst.TileSize))
                {
                    Debug_TTExt.Log("TILE at " + WorldPosition.FromScenePosition(scenePos).TileCoord + 
                        " was out of our physics broadphase regions.  We are upscaling the regions!");
                    SetExtendedBroadphase();
                }
                IntVector2 tilePos = WorldPosition.FromScenePosition(scenePos).TileCoord;
                HostSetTileState(tilePos, WorldTile.State.Empty);
                InvokeHelper.Invoke(HostClearTileState, 1, tilePos);
                //LoadedTileCoords
            }
            else
                return true;
            return false;
        }
        /// <summary>
        /// Force unload and reload a tile.  This is Single-Player only!
        /// </summary>
        public static bool HostOnly_ReloadTile(IntVector2 tilePos, bool rush)
        {
            if (!HostCanCommandTileLoader())
                return false;
            InsureInit();
            if (rush)
                RushTileLoading();
            if (ManWorld.inst.TileManager.LookupTile(tilePos)?.IsCreated ?? false)
            {
                if (!IsWithinPhysicsRegions(new WorldPosition(tilePos, Vector3.zero).ScenePosition, ManWorld.inst.TileSize))
                {
                    Debug_TTExt.Log("TILE at " + tilePos +
                        " was out of our physics broadphase regions.  We are upscaling the regions!");
                    SetExtendedBroadphase();
                }
                HostSetTileState(tilePos, WorldTile.State.Empty); 
                InvokeHelper.Invoke(HostClearTileState, 1, tilePos);
                //LoadedTileCoords
            }
            else
                return true;
            return false;
        }

        /// <summary>
        /// Force unload and reload THE ENTIRE SCENE.  This is Single-Player only!
        /// </summary>
        public static void HostOnly_ReloadENTIREScene(bool rush)
        {
            if (!HostCanCommandTileLoader())
                return;
            if (rush)
                RushTileLoading();
            foreach (var item in ManWorld.inst.TileManager.IterateTiles(WorldTile.State.Created))
            {
                HostOnly_ReloadTile(item.Coord, false);
            }
        }
        /// <summary>
        /// Register a Dynamically moving tile loader.  This must be called once on every client.
        /// </summary>
        /// <param name="loader"></param>
        /// <returns></returns>
        public static bool ClientRegisterDynamicTileLoader(ITileLoader loader)
        {
            InsureInit();
            if (loader != null)
            {
                if (!DynamicTileLoaders.Contains(loader))
                {
                    SetExtendedBroadphase();
                    DynamicTileLoaders.Add(loader);
                    return true;
                }
            }
            else
            {
            }
            return false;
        }

        /// <summary>
        /// Unregister a Dynamically moving tile loader.  This must be called once on every client.
        /// </summary>
        public static bool ClientUnregisterDynamicTileLoader(ITileLoader loader)
        {
            if (loader != null)
            {
                if (DynamicTileLoaders.Remove(loader))
                {
                    return true;
                };
            }
            else
            {
            }
            return false;
        }


        private static readonly List<IntVector2> tilesPosCache = new List<IntVector2>();
        private static void SlowUpdateTileLoading()
        {
            if (LoadedTileCoords != null)
            {
                LoadedTileCoords.Clear();
                if (ManWorld.inst.TileManager.IsClearing || ManWorld.inst.CurrentBiomeMap == null)
                    return;
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

                SendNetworkUpdateIfNeeded();
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

        public static bool IsWithinPhysicsRegions(Vector3 posScene, float boundsRadius = 76)
        {
            Bounds bounds = BoundsPhysics;
            if (posScene.x < bounds.center.x)
            {
                if (posScene.x < bounds.center.x + boundsRadius)
                    return false;
            }
            else
            {
                if (posScene.x > bounds.center.x - boundsRadius)
                    return false;
            }

            if (posScene.y < bounds.center.y)
            {
                if (posScene.y < bounds.center.y + boundsRadius)
                    return false;
            }
            else
            {
                if (posScene.y > bounds.center.y - boundsRadius)
                    return false;
            }

            if (posScene.z < bounds.center.z)
            {
                if (posScene.z < bounds.center.z + boundsRadius)
                    return false;
            }
            else
            {
                if (posScene.z > bounds.center.z - boundsRadius)
                    return false;
            }
            return true;
        }

        private static void SendNetworkUpdateIfNeeded()
        {
            if (ManNetwork.IsHost && ManNetwork.IsNetworked)
            {
                if (TileStateDirty)
                {
                    TileStateDirty = false;
                    netHook.TryBroadcast(new NetWTMessage(SetTiles));
                }
                if (FixedTileLoadersDirty)
                {
                    FixedTileLoadersDirty = false;
                    netHook.TryBroadcast(new NetWTMessage(FixedTileLoaders));
                }
            }
        }
        private static void GetActiveTilePerimeterForRequestedLoaded()
        {
            PerimeterTileSubLoaded.Clear();
            foreach (var item in LoadedTileCoords)
            {
                //TryAddTilePerimeterAtPosition(item.Key, ref PerimeterTileSubLoaded, WorldTile.State.Created);
                AddActiveTilePerimeterAroundPositionLazy(item.Key, ref PerimeterTileSubLoaded);
            }
            foreach (var item in LoadedTileCoords)
            {
                PerimeterTileSubLoaded.Remove(item.Key);
            }
            foreach (var item in PerimeterTileSubLoaded)
            {
                LoadedTileCoords.Add(item.Key, WorldTile.State.Created);
            }
        }
        private static void AddTilePerimeterAroundPosition(IntVector2 posSpot, 
            ref Dictionary<IntVector2, WorldTile.State> perimeter, WorldTile.State state = WorldTile.State.Loaded)
        {
            for (int i = -1; i < 2; i++)
            {
                for (int j = -1; j < 2; j++)
                {
                    if (i != 0 && j != 0)
                    {
                        TryAddTilePerimeterAtPosition(posSpot + new IntVector2(i, j), ref perimeter, state);
                    }
                }
            }
        }
        private static void AddPopulatedTilePerimeterAroundPosition(IntVector2 posSpot, ref Dictionary<IntVector2, WorldTile.State> perimeter)
        {
            for (int i = -1; i < 2; i++)
            {
                for (int j = -1; j < 2; j++)
                {
                    if (i != 0 && j != 0 && !perimeter.ContainsKey(posSpot))
                    {
                        TryAddTilePerimeterAtPosition(posSpot + new IntVector2(i, j), ref perimeter, WorldTile.State.Populated);
                        AddTilePerimeterAroundPosition(posSpot + new IntVector2(i, j), ref perimeter, WorldTile.State.Created);
                    }
                }
            }
        }
        // cruddy search algor, we shall see...
        private static void AddActiveTilePerimeterAroundPosition(IntVector2 posSpot, ref Dictionary<IntVector2, WorldTile.State> perimeter)
        {
            for (int i = -1; i < 2; i++)
            {
                for (int j = -1; j < 2; j++)
                {
                    if (i != 0 && j != 0 && !perimeter.ContainsKey(posSpot))
                    {
                        TryAddTilePerimeterAtPosition(posSpot + new IntVector2(i, j), ref perimeter);
                        AddPopulatedTilePerimeterAroundPosition(posSpot + new IntVector2(i, j), ref perimeter);
                    }
                }
            }
        }
        // cruddy search algor, we shall see...
        private static void AddActiveTilePerimeterAroundPositionLazy(IntVector2 posSpot, ref Dictionary<IntVector2, WorldTile.State> perimeter)
        {
            for (int i = -1; i < 2; i++)
                for (int j = -1; j < 2; j++)
                    TryAddTilePerimeterAtPosition(posSpot + new IntVector2(i, j), ref perimeter);
        }
        private static void TryAddTilePerimeterAtPosition(IntVector2 perimeterToAdd, 
            ref Dictionary<IntVector2, WorldTile.State> perimeter, WorldTile.State state = WorldTile.State.Loaded)
        {
            if (perimeter.ContainsKey(perimeterToAdd))
                return;
            else
                perimeter.Add(perimeterToAdd, state);
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

        internal class GUIManaged : GUILayoutHelpers
        {
            private static bool controlledDisp = false;
            private static HashSet<string> enabledTabs = null;
            public static void GUIGetTotalManaged()
            {
                if (enabledTabs == null)
                {
                    enabledTabs = new HashSet<string>();
                }
                GUILayout.Box("--- Tile Loaders --- ");
                bool show = controlledDisp && Singleton.playerTank;
                if (GUILayout.Button("Enabled Loading: " + show))
                    controlledDisp = !controlledDisp;
                if (controlledDisp)
                {
                    try
                    {
                        GUILayout.Label("Active loaders:", AltUI.LabelBlueTitle);
                        GUILabelDispFast("Count ", DynamicTileLoaders.Count);
                        GUILayout.Label("All loaded tiles:", AltUI.LabelBlueTitle);
                        foreach (var item in LoadedTileCoords)
                        {
                            GUILayout.BeginHorizontal(AltUI.TextfieldBlackAdjusted);
                            GUILayout.Label(item.Key.ToString());
                            GUILayout.FlexibleSpace();
                            GUILayout.Label(item.Value.ToString());
                            GUILayout.EndHorizontal();
                        }
                    }
                    catch (ExitGUIException e)
                    {
                        throw e;
                    }
                    catch (Exception e)
                    {
                        Debug_TTExt.Log("ResourcesHelper UI Debug errored - " + e);
                    }
                }
            }

        }

    }

    internal class WorldTilePatches
    {
        internal static class TileManagerPatches
        {
            internal static Type target = typeof(TileManager);

            [HarmonyLib.HarmonyPriority(50)]
            internal static bool GetWorkBudget_Prefix(TileManager __instance, ref int __result)
            {
                if (ManWorldTileExt.RushingTiles)
                {
                    __result = int.MaxValue;
                    return false;
                }
                return true;
            }

            private static bool removeCorruptedTest = false;
            private const float maxDistFromOrigin = 80000;
            private static bool broadphase => Globals.inst.DynamicMultiBoxBroadphaseRegions;
            private static List<IntVector2> curTiles = null;
            private static Dictionary<IntVector2, WorldTile> exisTiles = null;
            static FieldInfo TilesNew = typeof(TileManager).GetField("m_TileCoordsToCreateWorking", BindingFlags.NonPublic | BindingFlags.Instance);
            static FieldInfo Tiles = typeof(TileManager).GetField("m_TileLookup", BindingFlags.NonPublic | BindingFlags.Instance);
            static FieldInfo TileBounds = typeof(TileManager).GetField("physicsBounds", BindingFlags.NonPublic | BindingFlags.Instance);
            /// <summary>
            /// EnableTileLoading
            /// </summary>
            [HarmonyLib.HarmonyPriority(50)]
            internal static void UpdateTileRequestStates_Postfix(TileManager __instance, ref List<IntVector2> tileCoordsToCreate)
            {
                try
                {
                    if (__instance.IsClearing || ManWorld.inst.CurrentBiomeMap == null)
                        return;
                    if (exisTiles == null)
                    {
                        Debug_TTExt.Log("ManTileLoader - Are we broadphase physics? " + broadphase);
                        ManWorldTileExt.BoundsPhysics = (Bounds)TileBounds.GetValue(__instance);
                        //Debug_TTExt.Log("ManTileLoader - Bounds physics? " + ManWorldTileExt.BoundsPhysics.ToString("F"));
                        curTiles = (List<IntVector2>)TilesNew.GetValue(__instance);
                        Debug_TTExt.Log("ManTileLoader - Fetching tile lookup");
                        exisTiles = (Dictionary<IntVector2, WorldTile>)Tiles.GetValue(__instance);
                        Debug_TTExt.Log("ManTileLoader - Fetched tile lookup");
                    }
                    if (broadphase)
                    {
                        ManWorldTileExt.BoundsPhysics = (Bounds)TileBounds.GetValue(__instance);
                    }
                    if (tileCoordsToCreate == null)
                    {
                        Debug_TTExt.Log("ManTileLoader - tileCoordsToCreate is still null, waiting...");
                        return;
                    }
                    if (exisTiles == null)
                    {
                        Debug_TTExt.Log("ManTileLoader - exisTiles is still null, waiting...");
                        return;
                    }
                    //tileCoordsToCreate = 

                    Debug_TTExt.Assert(ManWorldTileExt.RequestedLoaded == null, "ManTileLoader - RequestedLoaded IS NULL");
                    int requests = ManWorldTileExt.RequestedLoaded.Count;
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
                                        if (item.Value == WorldTile.State.Empty)
                                            WT.m_RequestState = WorldTile.State.Empty;
                                        else if (WT.m_RequestState < item.Value)
                                            WT.m_RequestState = item.Value;
                                    }
                                    else
                                        Debug_TTExt.Assert("ManTileLoader - Tile at " + item.Key + " is NULL");
                                }
                                else
                                {
                                    if (!tileCoordsToCreate.Contains(item.Key))
                                    {
                                        tileCoordsToCreate.Add(item.Key);
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
                    Debug_TTExt.Log("ManTileLoader encountered an error - " + e);
                    throw new Exception("ManTileLoader encountered an error - " + e);
                }
            }
        }
    }

}
#endif