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
    public class WorldDeformer : MonoBehaviour
    {
        private static readonly string DataDirectory = "DeformerToolsDev";

        public static WorldDeformer inst;

        [JsonIgnore]
        public string DirectoryInExtModSettings => DataDirectory;

        internal static HashSet<WorldTile> terrainsDeformed = new HashSet<WorldTile>();
        public static Event<WorldTile> OnTerrainDeformed = new Event<WorldTile>();



        private Dictionary<string, int> ModsToTerrainModPriority = new Dictionary<string, int>();
        internal List<KeyValuePair<int, Dictionary<IntVector2, TerrainModifier>>> TerrainModsByPriority = new List<KeyValuePair<int, Dictionary<IntVector2, TerrainModifier>>>();

        public static Dictionary<TerraformerType, TerrainModifier> TerrainDefaults;

        public static Dictionary<TerraformerType, TerrainModifier> TerrainDefaultsSmall;
        public static Dictionary<TerraformerType, TerrainModifier> TerrainDefaultsLarge;


        //private static bool SetupSaveSystem = false;
        private static int ToolSize = 8;

        public static void Init()
        {
            if (inst == null)
            {
                inst = new GameObject("WorldDeformer").AddComponent<WorldDeformer>();
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
            if (inst.ModsToTerrainModPriority.TryGetValue(modName, out priority))
                return priority;
            while (inst.TerrainModsByPriority.Exists(x => x.Key == priority))
            {
                priority++;
                if (priority == int.MaxValue)
                    throw new IndexOutOfRangeException("Tried to insert mod " + modName + " priority of " + priority + 
                        " but we appear to have exceeded the integer range.  Notify Legionite!");
            }
            inst.ModsToTerrainModPriority.Add(modName, priority);
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
            return inst.ModsToTerrainModPriority.TryGetValue(modName, out priority);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="modName">The modname to remove</param>
        /// <returns>True if we removed it</returns>
        public static bool UnregisterModdedTerrain(string modName)
        {
            if (inst.ModsToTerrainModPriority.TryGetValue(modName, out int priority))
            {
                inst.ModsToTerrainModPriority.Remove(modName);
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

        public static IEnumerable<WorldTile> IterateModifiedWorldTiles() => terrainsDeformed;

        /// <summary>
        /// Reloads ALL terrain affected by TerrainMods!
        /// </summary>
        public static void ResetALLModifiedTerrain()
        {
            foreach (var item in terrainsDeformed)
            {
                ManWorldTileExt.ReloadTile(item.Coord);
            }
            terrainsDeformed.Clear();
        }

        /// <summary>
        /// Reloads terrain affected by TerrainMods, assuming the terrain mods were not changed beforehand!
        /// </summary>
        public static void ResetCurrentlyTargetedTerrain()
        {
            if (inst == null)
                return;
            foreach (var item in inst.TerrainModsByPriority)
            {
                foreach (var item2 in item.Value)
                {
                    terrainsDeformed.Remove(ManWorld.inst.TileManager.LookupTile(item2.Key));
                    ManWorldTileExt.ReloadTile(item2.Value.Position.TileCoord);
                }
            }
        }
        /// <summary>
        /// ADDITIVELY applies all registered TerrainMods to the world all at once.
        ///   You may want to call ResetCurrentlyTargetedTerrain() before this!
        /// </summary>
        public static void ReloadALLTerrainMods()
        {
            if (inst == null)
                return;
            foreach (var item in inst.TerrainModsByPriority)
            {
                foreach (var item2 in item.Value)
                {
                    item2.Value.FlushApply(1, new WorldPosition(item2.Key, Vector3.zero).ScenePosition);
                }
            }
        }

    }

    internal class WorldTerraformEnabler
    {
        internal static class TileManagerPatches
        {
            internal static Type target = typeof(TileManager);

            internal static void CreateTile_Postfix(TileManager __instance, ref WorldTile tile)
            {
                if (tile != null)
                {
                    foreach (var item in WorldDeformer.inst.TerrainModsByPriority)
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