using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using HarmonyLib;
using System.Reflection.Emit;
using System.Collections;

#if !EDITOR
namespace TerraTechETCUtil
{
    public class ManWorldDeformerExt : MonoBehaviour
    {
        private static readonly string DataDirectory = "DeformerToolsDev";

        public static ManWorldDeformerExt inst;

        [JsonIgnore]
        public string DirectoryInExtModSettings => DataDirectory;

        internal static HashSet<IntVector2> terrainsDeformed = new HashSet<IntVector2>();
        internal static HashSet<IntVector2> terrainsByDeformed = new HashSet<IntVector2>();
        public static Event<WorldTile> OnTerrainDeformed = new Event<WorldTile>();



        private Dictionary<string, int> ModsToDynamicTerrainModPriority = new Dictionary<string, int>();
        internal List<KeyValuePair<int, Dictionary<IntVector2, TerrainModifier>>> TerrainModsByPriority = new List<KeyValuePair<int, Dictionary<IntVector2, TerrainModifier>>>();

        public static Dictionary<TerraformerType, TerrainModifier> TerrainDefaults;

        public static Dictionary<TerraformerType, TerrainModifier> TerrainDefaultsSmall;
        public static Dictionary<TerraformerType, TerrainModifier> TerrainDefaultsLarge;


        //private static bool SetupSaveSystem = false;
        private static int ToolSize = 8;

        public static void InsureInit()
        {
            if (inst == null)
            {
                inst = new GameObject("WorldDeformer").AddComponent<ManWorldDeformerExt>();
                TerrainDefaultsSmall = new Dictionary<TerraformerType, TerrainModifier>();
                TerrainDefaultsLarge = new Dictionary<TerraformerType, TerrainModifier>();
                TerrainDefaults = TerrainDefaultsSmall;
                RecalibrateTools(TerrainDefaultsSmall);
                ToolSize = 16;
                RecalibrateTools(TerrainDefaultsLarge);
                ManGameMode.inst.ModeSetupEvent.Subscribe(OnGameSetup);
                MassPatcher.MassPatchAllWithin(LegModExt.harmonyInstance, typeof(WorldTerraformEnabler), "TerraTechModExt", true);
                inst.enabled = false;
            }
        }
        private static void RecalibrateTools(Dictionary<TerraformerType, TerrainModifier> tools)
        {
            int dualSize = (int)(ToolSize * 2f);
            tools.Remove(TerraformerType.Circle);
            var terra = new TerrainModifier(ToolSize);
            terra.AddHeightsAtPositionRadius(Vector3.zero, ToolSize, 1, true);
            tools[TerraformerType.Circle] = terra;

            tools.Remove(TerraformerType.Square);
            terra = new TerrainModifier(ToolSize);
            terra.AddHeightsAtPosition(Vector3.zero, new Vector2(dualSize, dualSize), 1, true);
            tools[TerraformerType.Square] = terra;
        }

        private static void OnGameSetup(Mode mode)
        {
            terrainsDeformed.Clear();
            terrainsByDeformed.Clear();
        }

        /// <summary>
        /// Assign a mod that alters terrain here!  Values altered in the object "terraChange" will be 
        /// applied automatically on world tile load or immedeately by call to TempResetALLTerrain() then ReloadTerrainMods().
        /// </summary>
        /// <param name="modName">The name of the mod to store. Must be unique</param>
        /// <param name="priority">The assigned priority of the mod.  Lower priorities go first!</param>
        /// <param name="terraChange">The TerrainMod to use.  You can change the given instance to directly edit the registered entry</param>
        /// <returns>The final set priority of the mod</returns>
        public static int RegisterModdedTerrain(string modName, int priority, Dictionary<IntVector2, TerrainModifier> terraChange)
        {
            InsureInit();
            if (inst.ModsToDynamicTerrainModPriority.TryGetValue(modName, out priority))
                return priority;
            while (inst.TerrainModsByPriority.Exists(x => x.Key == priority))
            {
                priority++;
                if (priority == int.MaxValue)
                    throw new IndexOutOfRangeException("Tried to insert mod " + modName + " priority of " + priority + 
                        " but we appear to have exceeded the integer range.  Notify Legionite!");
            }
            inst.ModsToDynamicTerrainModPriority.Add(modName, priority);
            for (int i = 0; i < inst.TerrainModsByPriority.Count; i++)
            {
                if (inst.TerrainModsByPriority[i].Key > priority)
                {
                    inst.TerrainModsByPriority.Insert(i, new KeyValuePair<int, Dictionary<IntVector2, TerrainModifier>>(priority, terraChange));
                    return priority;
                }
            }
            inst.TerrainModsByPriority.Add(new KeyValuePair<int, Dictionary<IntVector2, TerrainModifier>>(priority, terraChange));
            return priority;
        }
        /// <summary>
        /// Get the priority of the terrain affilated with a mod
        /// </summary>
        /// <param name="modName">The name of the mod to store. Must be unique</param>
        /// <param name="priority">The set priority of the mod</param>
        /// <returns>True if we found it</returns>
        public static bool GetModdedTerrainPriorityIndex(string modName, out int priority)
        {
            InsureInit();
            return inst.ModsToDynamicTerrainModPriority.TryGetValue(modName, out priority);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="modName">The modname to remove</param>
        /// <returns>True if we removed it</returns>
        public static bool UnregisterModdedTerrain(string modName)
        {
            InsureInit();
            if (inst.ModsToDynamicTerrainModPriority.TryGetValue(modName, out int priority))
            {
                inst.ModsToDynamicTerrainModPriority.Remove(modName);
                return inst.TerrainModsByPriority.RemoveAll(x => x.Key == priority) > 0;
            }
            return false;
        }


        // TOOL
        public static bool GrabTerrainCursorPos(out Vector3 posScene)
        {
            if (!Singleton.camera)
            {
                posScene = Vector3.zero;
                return false;
            }
            Vector3 posD = Singleton.camera.ScreenPointToRay(Input.mousePosition).direction.normalized;
            RaycastHit rayman;
            int layerMask = Globals.inst.layerTerrain.mask | Globals.inst.layerLandmark.mask;

            if (Physics.Raycast(new Ray(Singleton.camera.transform.position, posD), out rayman, 
                5000, layerMask, QueryTriggerInteraction.Ignore))
            {
                posScene = rayman.point;
                return true;
            }
            else
            {
                posScene = Vector3.zero;
                return false;
            }
        }
        public static bool GrabTerrainCursorPos(out Vector3 posScene, out float height)
        {
            if (!Singleton.camera)
            {
                posScene = Vector3.zero;
                height = 0;
                return false;
            }
            Vector3 posD = Singleton.camera.ScreenPointToRay(Input.mousePosition).direction.normalized;
            RaycastHit rayman;
            int layerMask = Globals.inst.layerTerrain.mask | Globals.inst.layerLandmark.mask;

            if (Physics.Raycast(new Ray(Singleton.camera.transform.position, posD), out rayman,
                5000, layerMask, QueryTriggerInteraction.Ignore))
            {
                posScene = rayman.point;
                height = posScene.y;
                return true;
            }
            else
            {
                posScene = Vector3.zero;
                height = 0;
                return false;
            }
        }

        internal static void SaveAllToSaveNonCompactedJson()
        {
            var path = Path.Combine(TinySettingsUtil.ExtModSettingsDirectory, inst.DirectoryInExtModSettings);
            File.WriteAllText(path, JsonConvert.SerializeObject(inst));
        }

        public enum TerraformerType
        {
            Circle,
            Square,
            Level,
            Reset,
            Slope
        }

        public static IEnumerable<IntVector2> IterateModifiedCoords() => terrainsDeformed;
        public static IEnumerable<IntVector2> IterateModifiedCoordsAndBordering() => terrainsDeformed;
        public static IEnumerable<WorldTile> IterateModifiedWorldTiles()
        {
            foreach (var item in terrainsDeformed)
            {
                WorldTile tile = ManWorld.inst.TileManager.LookupTile(item);
                if (tile != null)
                    yield return tile;
            }
        }
        public static IEnumerable<WorldTile> IterateModifiedAndBorderingWorldTiles()
        {
            foreach (var item in terrainsByDeformed)
            {
                WorldTile tile = ManWorld.inst.TileManager.LookupTile(item);
                if (tile != null)
                    yield return tile;
            }
        }

        /// <summary>
        /// Reloads ALL terrain affected by TerrainMods!
        ///   This is only TEMPORARY
        /// </summary>
        public static void ResetALLModifiedTerrain(bool rush)
        {
            if (rush)
                ManWorldTileExt.RushTileLoading();
            foreach (var item in terrainsByDeformed)
            {
                ManWorldTileExt.HostOnly_ReloadTile(item, false);
            }
            terrainsByDeformed.Clear();
            terrainsDeformed.Clear();
        }

        /// <summary>
        /// Reloads terrain affected by TerrainMods, assuming the terrain mods were not changed beforehand!
        /// </summary>
        public static void ResetCurrentlyTargetedTerrain(bool rush)
        {
            InsureInit();
            if (inst == null)
                return;
            if (rush)
                ManWorldTileExt.RushTileLoading();
            foreach (var item in inst.TerrainModsByPriority)
            {
                foreach (var item2 in item.Value)
                {
                    terrainsDeformed.Remove(item2.Key);
                    terrainsByDeformed.Remove(item2.Key);
                    ManWorldTileExt.HostOnly_ReloadTile(item2.Value.Position.TileCoord, false);
                }
            }
        }

        private static HashSet<IntVector2> terrainsNeedReloadCache = new HashSet<IntVector2>();
        //private static List<KeyValuePair<IntVector2, TerrainModifier>> terrainsNeedReloadCached = new List<KeyValuePair<IntVector2, TerrainModifier>>();
        /// <summary>
        /// ADDITIVELY applies all registered TerrainMods to the world all at once.
        ///   You may want to call ResetCurrentlyTargetedTerrain() before this!
        /// </summary>
        public static void ReloadALLTerrainMods()
        {
            InsureInit();
            if (inst == null)
                return;
            try
            {
                foreach (var item in inst.TerrainModsByPriority)
                {
                    foreach (var item2 in item.Value)
                    {
                        Vector3 ModSceneOverride = new WorldPosition(item2.Key, Vector3.zero).ScenePosition;
                        item2.Value.GetModTileCoordsWithEdging(ModSceneOverride, out IntVector2 vec1, out IntVector2 vec2);
                        for (int x = vec1.x; x <= vec2.x; x++)
                        {
                            for (int y = vec1.y; y <= vec2.y; y++)
                                terrainsNeedReloadCache.Add(new IntVector2(x, y));
                        }
                    }
                }
                foreach (var vec in terrainsNeedReloadCache)
                {
                    WorldTile tile = ManWorld.inst.TileManager.LookupTile(vec);
                    if (tile != null)
                    {
                        TerrainModifier.AutoDelta0(tile, out float[,] pointsPrev, out IntVector2 origin, out float[,] OGHeights);
                        foreach (var item in inst.TerrainModsByPriority)
                        {
                            if (item.Value != null)
                            {
                                foreach (var item2 in item.Value)
                                {
                                    if (item2.Value != null)
                                    {
                                        Vector3 ModSceneOverride = new WorldPosition(item2.Key, Vector3.zero).ScenePosition;
                                        if (item2.Value.IntersectsWithEdges(tile, ModSceneOverride))
                                        {
                                            IntVector2 delta = TerrainModifier.SceneToModCellPos(tile.Terrain.transform.position, ModSceneOverride);
                                            item2.Value.AutoDelta1(pointsPrev, delta, origin, OGHeights, 1, 1, ModSceneOverride, TerraDampenMode.DampenOnly);
                                        }
                                    }
                                }
                            }
                        }
                        int tileFlushOps = 0;
                        foreach (var item in inst.TerrainModsByPriority)
                        {
                            if (item.Value != null)
                            {
                                foreach (var item2 in item.Value)
                                {
                                    if (item2.Value != null)
                                    {
                                        Vector3 ModSceneOverride = new WorldPosition(item2.Key, Vector3.zero).ScenePosition;
                                        if (item2.Value.Intersects(tile, ModSceneOverride))
                                        {
                                            IntVector2 delta = TerrainModifier.SceneToModCellPos(tile.Terrain.transform.position, ModSceneOverride);
                                            item2.Value.AutoDelta1(pointsPrev, delta, origin, OGHeights, 1, 1, ModSceneOverride, TerraDampenMode.DeformOnly);
                                            if (item2.Value.AutoMode == TerraApplyMode.FlushAutoHeightAdjust)
                                                tileFlushOps++;
                                        }
                                    }
                                }
                            }
                        }
                        TerrainModifier.PushChanges(tile.Terrain.terrainData, pointsPrev);
                        if (tileFlushOps > 1)
                            Debug_TTExt.Assert("ManWorldDeformerExt: Multiple flush operations in tile " + vec.ToString());
                    }
                }
                foreach (var vec in terrainsNeedReloadCache)
                {
                    WorldTile tile = ManWorld.inst.TileManager.LookupTile(vec);
                    if (tile != null)
                        TerrainModifier.PushFlush(tile);
                }
            }
            finally
            {
                terrainsNeedReloadCache.Clear();
            }
        }

        public static void ReloadTerrainMods(Dictionary<IntVector2, TerrainModifier> mods, IntVector2 coordOffset)
        {
            if (mods == null)
                throw new NullReferenceException("tile is NULL");
            if (coordOffset == IntVector2.invalid)
                throw new NullReferenceException("coordOffset is invalid");
            InsureInit();
            if (inst == null)
                return;
            try
            {
                foreach (var item2 in mods)
                {
                    Vector3 ModSceneOverride = new WorldPosition(item2.Key + coordOffset, Vector3.zero).ScenePosition;
                    item2.Value.GetModTileCoordsWithEdging(ModSceneOverride, out var vec1, out var vec2);
                    for (int x = vec1.x; x <= vec2.x; x++)
                    {
                        for (int y = vec1.y; y <= vec2.y; y++)
                            terrainsNeedReloadCache.Add(new IntVector2(x, y));
                    }
                }
                foreach (var vec in terrainsNeedReloadCache)
                {
                    WorldTile tile = ManWorld.inst.TileManager.LookupTile(vec);
                    if (tile != null)
                    {
                        TerrainModifier.AutoDelta0(tile, out float[,] pointsPrev, out IntVector2 origin, out float[,] OGHeights); 
                        foreach (var item in mods)
                        {
                            if (item.Value != null)
                            {
                                Vector3 ModSceneOverride = new WorldPosition(item.Key + coordOffset, Vector3.zero).ScenePosition;
                                if (item.Value.IntersectsWithEdges(tile, ModSceneOverride))
                                {
                                    IntVector2 delta = TerrainModifier.SceneToModCellPos(tile.Terrain.transform.position, ModSceneOverride);
                                    item.Value.AutoDelta1(pointsPrev, delta, origin, OGHeights, 1, 1, ModSceneOverride, TerraDampenMode.DampenOnly);
                                }
                            }
                        }
                        int tileFlushOps = 0;
                        foreach (var item in mods)
                        {
                            if (item.Value != null)
                            {
                                Vector3 ModSceneOverride = new WorldPosition(item.Key + coordOffset, Vector3.zero).ScenePosition;
                                if (item.Value.Intersects(tile, ModSceneOverride))
                                {
                                    IntVector2 delta = TerrainModifier.SceneToModCellPos(tile.Terrain.transform.position, ModSceneOverride);
                                    item.Value.AutoDelta1(pointsPrev, delta, origin, OGHeights, 1, 1, ModSceneOverride, TerraDampenMode.DeformOnly);
                                    if (item.Value.AutoMode == TerraApplyMode.FlushAutoHeightAdjust)
                                        tileFlushOps++;
                                }
                            }
                        }
                        TerrainModifier.PushChanges(tile.Terrain.terrainData, pointsPrev);
                        if (tileFlushOps > 1)
                            Debug_TTExt.Assert("ManWorldDeformerExt: Multiple flush operations in tile " + vec.ToString());
                    }
                }
                foreach (var vec in terrainsNeedReloadCache)
                {
                    WorldTile tile = ManWorld.inst.TileManager.LookupTile(vec);
                    if (tile != null)
                        TerrainModifier.PushFlush(tile);
                }
            }
            finally
            {
                terrainsNeedReloadCache.Clear();
            }
        }


        public static void ApplyRegisteredTerrainModsAtTile(WorldTile tile)
        {
            if (tile == null)
                throw new NullReferenceException("tile is NULL");
            IntVector2 vec = tile.Coord;
            TerrainModifier.AutoDelta0(tile, out float[,] pointsPrev, out IntVector2 origin, out float[,] OGHeights);
            bool any = false;
            foreach (var item in inst.TerrainModsByPriority)
            {
                if (item.Value != null)
                {
                    foreach (var item2 in item.Value)
                    {
                        if (item2.Value != null)
                        {
                            Vector3 ModSceneOverride = new WorldPosition(item2.Key, Vector3.zero).ScenePosition;
                            if (item2.Value.IntersectsWithEdges(tile, ModSceneOverride))
                            {
                                IntVector2 delta = TerrainModifier.SceneToModCellPos(tile.Terrain.transform.position, ModSceneOverride);
                                item2.Value.AutoDelta1(pointsPrev, delta, origin, OGHeights, 1, 1, ModSceneOverride, TerraDampenMode.DampenOnly);
                                any = true;
                            }
                        }
                    }
                }
            }
            int tileFlushOps = 0;
            foreach (var item in inst.TerrainModsByPriority)
            {
                if (item.Value != null)
                {
                    foreach (var item2 in item.Value)
                    {
                        if (item2.Value != null)
                        {
                            Vector3 ModSceneOverride = new WorldPosition(item2.Key, Vector3.zero).ScenePosition;
                            if (item2.Value.Intersects(tile, ModSceneOverride))
                            {
                                IntVector2 delta = TerrainModifier.SceneToModCellPos(tile.Terrain.transform.position, ModSceneOverride);
                                item2.Value.AutoDelta1(pointsPrev, delta, origin, OGHeights, 1, 1, ModSceneOverride, TerraDampenMode.DeformOnly);
                                any = true;
                                if (item2.Value.AutoMode == TerraApplyMode.FlushAutoHeightAdjust)
                                    tileFlushOps++;
                            }
                        }
                    }
                }
            }
            if (tileFlushOps > 1)
                Debug_TTExt.Assert("ManWorldDeformerExt: Multiple flush operations in tile " + vec.ToString());
            if (any)
            {
                TerrainModifier.PushChanges(tile.Terrain.terrainData, pointsPrev);
                TerrainModifier.PushFlush(tile);
            }
        }
        public static float CalcPointHeightAtTile(float initialHeight, Vector3 posScene)
        {
            WorldPosition wp = WorldPosition.FromScenePosition(posScene);
            IntVector2 inTileCoord = TerrainModifier.InTilePosToInTileCoord(wp.TileRelativePos);
            float newHeight = initialHeight;
            foreach (var item in inst.TerrainModsByPriority)
            {
                if (item.Value != null)
                {
                    foreach (var item2 in item.Value)
                    {
                        if (item2.Value != null)
                        {
                            Vector3 ModSceneOverride = new WorldPosition(item2.Key, Vector3.zero).ScenePosition;
                            if (item2.Value.IntersectsWithEdges(wp.TileCoord, ModSceneOverride))
                            {
                                IntVector2 delta = TerrainModifier.SceneToModCellPos(posScene, ModSceneOverride);
                                newHeight = item2.Value.AutoCalcPoint(newHeight, inTileCoord, delta,
                                    TerrainModifier.TileWorldSceneOriginCellPos(wp.TileCoord), initialHeight, 1, 1,
                                    ModSceneOverride, TerraDampenMode.DampenOnly);
                            }
                        }
                    }
                }
            }
            foreach (var item in inst.TerrainModsByPriority)
            {
                if (item.Value != null)
                {
                    foreach (var item2 in item.Value)
                    {
                        if (item2.Value != null)
                        {
                            Vector3 ModSceneOverride = new WorldPosition(item2.Key, Vector3.zero).ScenePosition;
                            if (item2.Value.Intersects(wp.TileCoord, ModSceneOverride))
                            {
                                IntVector2 delta = TerrainModifier.SceneToModCellPos(posScene, ModSceneOverride);
                                newHeight = item2.Value.AutoCalcPoint(newHeight, inTileCoord, delta,
                                    TerrainModifier.TileWorldSceneOriginCellPos(wp.TileCoord), initialHeight, 1, 1,
                                    ModSceneOverride, TerraDampenMode.DeformOnly);
                            }
                        }
                    }
                }
            }
            return newHeight;
        }

        public static IEnumerable<TerrainModifier> GetOverlappingTerrain(IntVector2 vec)
        {
            WorldTile tile = ManWorld.inst.TileManager.LookupTile(vec);
            Vector3 ModSceneOverride = new WorldPosition(vec, Vector3.zero).ScenePosition;
            foreach (var item in inst.TerrainModsByPriority)
            {
                if (item.Value != null)
                {
                    foreach(var item2 in item.Value)
                    {
                        if (item2.Value != null && item2.Value.IntersectsWithEdges(tile, ModSceneOverride))
                            yield return item2.Value;
                    }
                }
            }
        }
    }

    internal class WorldTerraformEnabler
    {
        internal static class TileManagerPatches
        {
            internal static Type target = typeof(TileManager);

            [HarmonyPriority(-30)]
            internal static void GetTerrainHeightAtPosition_Postfix(TileManager __instance, ref float __result,
                Vector3 scenePos, ref bool onTile, bool forceCalculate)
            {
                if (!onTile)
                    __result = 100f * (ManWorldDeformerExt.CalcPointHeightAtTile((__result / 100f) + 0.5f, scenePos) - 0.5f);
            }
            internal static void CreateTile_Postfix(TileManager __instance, ref WorldTile tile)
            {
                if (tile != null)
                {
                    foreach (var item in ManWorldDeformerExt.inst.TerrainModsByPriority)
                    {
                        if (item.Value.TryGetValue(tile.Coord, out var posSet))
                        {
                            posSet.Flush();
                        }
                    }
                }
            }
        }

    }
}
#endif