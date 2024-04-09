using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

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

        public Dictionary<IntVector2, TerrainModifier> TerrainModsActive = new Dictionary<IntVector2, TerrainModifier>();

        public static Dictionary<TerraformerType, TerrainModifier> TerrainDefaults;

        public static Dictionary<TerraformerType, TerrainModifier> TerrainDefaultsSmall;
        public static Dictionary<TerraformerType, TerrainModifier> TerrainDefaultsLarge;


        //private static bool SetupSaveSystem = false;
        private static int ToolSize = 8;

        internal static void Init()
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

        /*
        private static float applyStrength => 0.01f / TerrainOperations.RescaleFactor;
        private static float levelingStrength => 0.1f / TerrainOperations.RescaleFactor;
        private static bool active = false;
        private static bool Large = true;
        private static TerraformerType ToolMode = TerraformerType.Circle;
        private static float cachedHeight = 0;
        private static float delayTimer = 0;
        private static float delayTimerDelay = 0.0356f;
        private static KeyCode altHotKey = KeyCode.LeftShift;
        public void Update_LEGACY()
        {
            bool delayTimed;
            bool deltaed = false;
            if (delayTimer <= 0)
            {
                delayTimer += delayTimerDelay;
                delayTimed = true;
            }
            else
            {
                delayTimer -= Time.deltaTime;
                delayTimed = false;
            }
            if (Singleton.playerTank)
            {
                if (Input.GetKeyDown(KeyCode.Insert))
                    active = !active;
                if (active)
                {
                    if (ManPointer.inst.DraggingItem != null)
                    {
                        active = false;
                        return;
                    }
                    Vector3 terrainPosSpot;
                    if (Input.GetKeyDown(KeyCode.Home))
                    {
                        SaveAllToSaveNonCompactedJson();
                    }
                    if (Input.GetKeyDown(KeyCode.Delete))
                    {
                        Large = !Large;
                        if (Large)
                        {
                            ToolSize = 16;
                            TerrainDefaults = TerrainDefaultsLarge;
                        }
                        else
                        {
                            ToolSize = 8;
                            TerrainDefaults = TerrainDefaultsSmall;
                        }
                        UIHelpersExt.BigF5broningBanner("Tool Size: " + ToolSize, false);
                    }
                    if (Input.GetMouseButtonDown(2))
                    {
                        ToolMode = (TerraformerType)Mathf.Repeat((int)ToolMode + 1, Enum.GetValues(typeof(TerraformerType)).Length);
                        UIHelpersExt.BigF5broningBanner("Tool: " + ToolMode, false);
                    }
                    else if (Input.GetMouseButtonDown(0) && Input.GetKey(altHotKey) &&
                        GrabTerrainCursorPos(out terrainPosSpot))
                    {
                        var worldT = ManWorld.inst.TileManager.LookupTile(terrainPosSpot);
                        if (worldT != null)
                        {
                            IntVector2 tilePosInTile = new IntVector2(
                                (terrainPosSpot - worldT.Terrain.transform.position).ToVector2XZ() / TerrainModifier.tilePosToTileScale);
                            cachedHeight = worldT.Terrain.terrainData.GetHeight(tilePosInTile.x, tilePosInTile.y) / TerrainOperations.RescaledFactor;
                        }
                    }
                    if (delayTimed && GrabTerrainCursorPos(out terrainPosSpot))
                    {
                        Vector3 terrainPosSpotCorrect = terrainPosSpot;
                        switch (ToolMode)
                        {
                            case TerraformerType.Circle:
                                terrainPosSpotCorrect += new Vector3(0, 0, -ToolSize);
                                if (Input.GetKey(altHotKey))
                                {
                                    if (Input.GetMouseButton(0))
                                    {
                                        deltaed = true;
                                        TerrainDefaults[ToolMode].FlushAdd(applyStrength, terrainPosSpotCorrect +
                                            TerrainDefaults[ToolMode].Position.GameWorldPosition);
                                    }
                                    DebugExtUtilities.DrawDirIndicatorCircle(terrainPosSpot + new Vector3(0, 2, 0),
                                        Vector3.up, Vector3.forward, ToolSize, Color.cyan, delayTimerDelay);
                                }
                                else
                                {
                                    if (Input.GetMouseButton(0))
                                    {
                                        deltaed = true;
                                        TerrainDefaults[ToolMode].FlushAdd(-applyStrength, terrainPosSpotCorrect +
                                        TerrainDefaults[ToolMode].Position.GameWorldPosition);
                                    }
                                    DebugExtUtilities.DrawDirIndicatorCircle(terrainPosSpot,
                                        Vector3.up, Vector3.forward, ToolSize, Color.cyan, delayTimerDelay);
                                }
                                break;
                            case TerraformerType.Square:
                                terrainPosSpotCorrect += new Vector3(0, 0, -(ToolSize * 2));
                                if (Input.GetKey(altHotKey))
                                {
                                    if (Input.GetMouseButton(0))
                                    {
                                        deltaed = true;
                                        TerrainDefaults[ToolMode].FlushAdd(applyStrength, terrainPosSpotCorrect + new Vector3(-ToolSize, 0, -ToolSize) +
                                            TerrainDefaults[ToolMode].Position.GameWorldPosition);
                                    }
                                    DebugExtUtilities.DrawDirIndicatorRecPriz(terrainPosSpot + new Vector3(0, 2, 0),
                                        new Vector3(ToolSize * 2, 1, ToolSize * 2), Color.cyan, delayTimerDelay);
                                }
                                else
                                {
                                    if (Input.GetMouseButton(0))
                                    {
                                        deltaed = true;
                                        TerrainDefaults[ToolMode].FlushAdd(-applyStrength, terrainPosSpotCorrect + new Vector3(-ToolSize, 0, -ToolSize) +
                                            TerrainDefaults[ToolMode].Position.GameWorldPosition);
                                    }
                                    DebugExtUtilities.DrawDirIndicatorRecPriz(terrainPosSpot,
                                        new Vector3(ToolSize * 2, 1, ToolSize * 2), Color.cyan, delayTimerDelay);
                                }
                                break;
                            case TerraformerType.Level:
                                terrainPosSpotCorrect += new Vector3(0, 0, -ToolSize);
                                if (Input.GetKey(altHotKey))
                                {
                                    if (Input.GetMouseButton(0))
                                    {
                                        deltaed = true;
                                        var worldT = ManWorld.inst.TileManager.LookupTile(terrainPosSpot);
                                        if (worldT != null)
                                        {
                                            DebugExtUtilities.DrawDirIndicatorCircle(terrainPosSpot + Vector3.up, Vector3.up,
                                            Vector3.forward, ToolSize, Color.black, delayTimerDelay);
                                            TerrainDefaults[TerraformerType.Circle].FlushLevel(cachedHeight, levelingStrength, 
                                                terrainPosSpotCorrect + TerrainDefaults[TerraformerType.Circle].Position.GameWorldPosition);
                                        }
                                        else
                                            DebugExtUtilities.DrawDirIndicatorCircle(terrainPosSpot + Vector3.up,
                                                Vector3.up, Vector3.forward, ToolSize, Color.red, delayTimerDelay);
                                    }
                                    else
                                        DebugExtUtilities.DrawDirIndicatorCircle(terrainPosSpot + Vector3.up, Vector3.up,
                                        Vector3.forward, ToolSize, Color.white, delayTimerDelay);
                                }
                                else
                                {
                                    if (Input.GetMouseButton(0))
                                    {
                                        deltaed = true;
                                        DebugExtUtilities.DrawDirIndicatorCircle(terrainPosSpot + Vector3.up,
                                            Vector3.up, Vector3.forward, ToolSize, Color.yellow, delayTimerDelay);
                                        TerrainDefaults[TerraformerType.Circle].FlushLevel(-1, levelingStrength, terrainPosSpotCorrect +
                                            TerrainDefaults[TerraformerType.Circle].Position.GameWorldPosition);
                                    }
                                    else
                                        DebugExtUtilities.DrawDirIndicatorCircle(terrainPosSpot + Vector3.up,
                                            Vector3.up, Vector3.forward, ToolSize, Color.magenta, delayTimerDelay);
                                }
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
            if (delayTimed && !deltaed)
            {
                foreach (var item in terrainsDeformed)
                {
                    OnTerrainDeformed.Send(item);
                    if (TerrainModsActive.TryGetValue(item.Coord, out TerrainModifier TM))
                    {
                        TM.Setup(item);
                    }
                    else
                        TerrainModsActive.Add(item.Coord, new TerrainModifier(item));
                }
                terrainsDeformed.Clear();
            }
        }
        */
        /// <summary>
        /// Reloads allTerrainMods
        /// </summary>
        public static void TempResetALLTerrain()
        {
            if (inst == null)
                return;
            foreach (var item in inst.TerrainModsActive)
            {
                ManWorldTileExt.ReloadTile(item.Value.Position.TileCoord);
            }
        }
        public static void ReloadTerrainMods()
        {
            if (inst == null)
                return;
            foreach (var item in inst.TerrainModsActive)
            {
                item.Value.FlushApply(1, new WorldPosition(item.Key, Vector3.zero).ScenePosition);
            }
        }

        public static void AddTerrainDefault()
        { 
        }

    }
}
#endif