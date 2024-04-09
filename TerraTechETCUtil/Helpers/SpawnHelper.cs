using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using System.Collections;

#if !EDITOR
namespace TerraTechETCUtil
{
    public class SpawnHelper : MonoBehaviour
    {
        private MethodInfo death = typeof(ResourceDispenser).GetMethod("PlayDeathAnimation", BindingFlags.NonPublic | BindingFlags.Instance);
        private FieldInfo deathParticles = typeof(ResourceDispenser).GetField("m_BigDebrisPrefab", BindingFlags.NonPublic | BindingFlags.Instance);
        private FieldInfo deathSound = typeof(ManSFX).GetField("m_SceneryDebrisEvents", BindingFlags.NonPublic | BindingFlags.Instance);
        private MethodInfo deathSoundGet = typeof(ManSFX).GetMethod("GetScenerySFXType", BindingFlags.NonPublic | BindingFlags.Instance);


        private static SpawnHelper inst;

        private Dictionary<SceneryTypes, Dictionary<BiomeTypes, List<TerrainObject>>> objs = new Dictionary<SceneryTypes, Dictionary<BiomeTypes, List<TerrainObject>>>();
        private Dictionary<string, TerrainObject> objsNonRes = new Dictionary<string, TerrainObject>();

        private static HashSet<int> captured = new HashSet<int>();
        public static IEnumerable<SceneryTypes> IterateSceneryTypesByIngredient(ChunkTypes type)
        {
            GrabInitList();
            captured.Clear();
            foreach (var item in inst.objs.Values)
            {
                foreach (var item2 in item)
                {
                    foreach (var item3 in item2.Value)
                    {
                        ResourceDispenser RD = item3.GetComponent<ResourceDispenser>();
                        Visible vis = item3.GetComponent<Visible>();
                        if (RD != null && captured.Contains(vis.ItemType) && RD.AllDispensableItems().Any(x => x == type))
                        {
                            captured.Add(vis.ItemType);
                            yield return (SceneryTypes)vis.ItemType;
                        }
                    }
                }
            }
        }
        public static Dictionary<BiomeTypes, List<TerrainObject>> GetSceneryByType(SceneryTypes type)
        {
            GrabInitList();
            inst.objs.TryGetValue(type, out var outcome);
            return outcome;
        }
        public static IEnumerable<TerrainObject> IterateSceneryByBiome(BiomeTypes type)
        {
            GrabInitList();
            foreach (var item in inst.objs.Values)
            {
                if (item.TryGetValue(type, out var TOs))
                {
                    foreach (var item2 in TOs)
                        yield return item2;
                }
            }
        }
        public static IEnumerable<Dictionary<BiomeTypes, List<TerrainObject>>> IterateSceneryTypes()
        {
            GrabInitList();
            foreach (var item in inst.objs)
            {
                yield return item.Value;
            }
        }

        public static BiomeTypes GetBiomeFromName(string name)
        {
            if (name.Contains("SaltFlats"))
                return BiomeTypes.SaltFlats;
            if (name.Contains("Mountain"))
                return BiomeTypes.Mountains;
            if (name.Contains("Pillar"))
                return BiomeTypes.Pillars;
            if (name.Contains("Desert"))
                return BiomeTypes.Desert;
            if (name.Contains("Biome7"))
                return (BiomeTypes)7;
            if (name.Contains("Ice"))
                return BiomeTypes.Ice;
            return BiomeTypes.Grassland;
        }
        private SpawnHelper()
        {
            try
            {
                FieldInfo sce = typeof(ManSpawn).GetField("spawnableScenery", BindingFlags.NonPublic | BindingFlags.Instance);

                List<Visible> objsRaw = (List<Visible>)sce.GetValue(ManSpawn.inst);
                if (objsRaw == null) throw new NullReferenceException("SpawnHelper: ManSpawn.inst has not allocated spawnableScenery for some reason and SpawnHelper fetch failed");
                foreach (var item in objsRaw)
                {
                    try
                    {
                        if (item == null || item.GetComponent<TerrainObject>() == null)
                            return;
                        if (item.GetComponent<ResourceDispenser>())
                        {
                            SceneryTypes ST = (SceneryTypes)item.ItemType;
                            BiomeTypes BT = GetBiomeFromName(item.name);
                            //Debug_TTExt.Log("- " + item.name + " | " + BT + " | " + ST);
                            List<TerrainObject> objRand;
                            Dictionary<BiomeTypes, List<TerrainObject>> objBiome;
                            if (objs.TryGetValue(ST, out objBiome))
                            {
                                if (objBiome.TryGetValue(BT, out objRand))
                                    objRand.Add(item.GetComponent<TerrainObject>());
                                else
                                {
                                    objRand = new List<TerrainObject> { item.GetComponent<TerrainObject>() };
                                    objBiome.Add(BT, objRand);
                                }
                            }
                            else
                            {
                                objRand = new List<TerrainObject> { item.GetComponent<TerrainObject>() };
                                objBiome = new Dictionary<BiomeTypes, List<TerrainObject>>();
                                objBiome.Add(BT, objRand);
                                objs.Add(ST, objBiome);
                            }
                        }
                        else
                        {
                            try
                            {
                                objsNonRes.Add(item.name, item.GetComponent<TerrainObject>());
                            }
                            catch
                            {
                                Debug_TTExt.Log("Item of name " + item.name + " already exists in objsNonRes, but there's more than one of them!");
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug_TTExt.Log("SpawnHelper: FAILED TO FETCH article - " + (item == null ? "NULL_OBJ" : (item.name == null ? "NULL_NAME" : item.name)) + " - " + e);
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception("SpawnHelper: Critical failiure on indexing Scenery - " + e);
            }
            if (objs.Count == 0)
                Debug_TTExt.Assert("SpawnHelper grabbed no resource nodes.  Did we init too early or are there none in this game mode?");
        }

        public static void GrabInitList(bool forceReInit = false)
        {
            if (inst != null && !forceReInit)
                return;
            inst = new SpawnHelper();
        }

        public static void PrintAllRegisteredResourceNodes()
        {
            GrabInitList();
            Debug_TTExt.Log("Resources:");
            foreach (var item in inst.objs)
            {
                Debug_TTExt.Log("  Type: " + item.Key);
                foreach (var item2 in item.Value)
                {
                    Debug_TTExt.Log("   Biome: " + item2.Key);
                    foreach (var item3 in item2.Value)
                    {
                        Debug_TTExt.Log("     Name: " + item3.name);
                    }
                }
            }
            Debug_TTExt.Log("END");
        }
        public static void PrintAllRegisteredNonResourceObjects()
        {
            GrabInitList();
            Debug_TTExt.Log("TerrainObject:");
            foreach (var item in inst.objsNonRes)
            {
                Debug_TTExt.Log("  Name: " + item.Key);
            }
            Debug_TTExt.Log("END");
        }


        public static TerrainObject GetNonResourcePrefab(string Name)
        {
            GrabInitList();
            try
            {
                if (inst.objsNonRes.TryGetValue(Name, out var TO))
                    return TO;
            }
            catch (Exception)
            {
            }
            throw new NullReferenceException("GetNonResourcePrefab entry for " + Name + " has no match!");
        }

        public static TerrainObject GetResourceNodePrefab(SceneryTypes type, BiomeTypes biome)
        {
            GrabInitList();
            try
            {
                TerrainObject TO = null;
                if (inst.objs.TryGetValue(type, out var objBiome))
                {
                    if (objBiome.TryGetValue(biome, out var objRand))
                    {
                        TO = objRand.GetRandomEntry();
                    }
                    else
                        TO = objBiome[BiomeTypes.Grassland].GetRandomEntry();
                    if (TO == null)
                        throw new NullReferenceException("GetResourceNodePrefab entry for Biome " + biome.ToString() + " has NO entries!");
                }
                else
                    throw new NullReferenceException("GetResourceNodePrefab entry for Scenery " + type.ToString() + " has NO entries!");
                return TO;
            }
            catch (Exception)
            {
                throw new NullReferenceException("GetResourceNodePrefab entry for " + type.ToString() + " | " + biome.ToString() + " has NO entries!");
            }
        }
        public static void SpawnResourceNodeExplosion(Vector3 scenePos, SceneryTypes type, BiomeTypes biome)
        {
            GrabInitList();
            var resDisp = GetResourceNodePrefab(type, biome).GetComponent<ResourceDispenser>();
            ((Transform)inst.deathParticles.GetValue(resDisp)).Spawn(null, scenePos, Quaternion.LookRotation(Vector3.up, Vector3.back));
            ((FMODEvent[])inst.deathSound.GetValue(ManSFX.inst))[(int)inst.deathSoundGet.Invoke(ManSFX.inst, new object[1] { type })].PlayOneShot(scenePos);
        }
        public static void SpawnResourceNodeExplosion(Vector3 scenePos, ResourceDispenser resDisp)
        {
            GrabInitList();
            var type = resDisp.GetSceneryType();
            ((Transform)inst.deathParticles.GetValue(resDisp)).Spawn(null, scenePos, Quaternion.LookRotation(Vector3.up, Vector3.back));
            ((FMODEvent[])inst.deathSound.GetValue(ManSFX.inst))[(int)inst.deathSoundGet.Invoke(ManSFX.inst, new object[1] { type })].PlayOneShot(scenePos);
        }
        public static ResourceDispenser SpawnResourceNodeSnapTerrain(Vector3 scenePos, SceneryTypes type, BiomeTypes biome)
        {
            GrabInitList();
            Vector3 pos = scenePos;
            ManWorld.inst.GetTerrainHeight(pos, out pos.y);
            Quaternion flatRot = Quaternion.LookRotation((UnityEngine.Random.rotation * Vector3.forward).SetY(0).normalized, Vector3.up);
            return SpawnResourceNode(pos, flatRot, type, biome);
        }
        public static ResourceDispenser SpawnResourceNode(Vector3 scenePos, Quaternion rotation, SceneryTypes type, BiomeTypes biome)
        {
            GrabInitList();
            try
            {
                TerrainObject TO = GetResourceNodePrefab(type, biome);
                TrackableObject track = TO.SpawnFromPrefabAndAddToSaveData(scenePos, rotation).TerrainObject;
                ResourceDispenser RD = track.GetComponent<ResourceDispenser>();
                return RD;
            }
            catch (Exception e)
            {
                throw new NullReferenceException("TTExtUtil: SpawnResourceNode encountered an error - " + e.Message, e);
            }
        }
        public static ResourceDispenser SpawnResourceNodeAnimated(Vector3 scenePos, Quaternion rotation, SceneryTypes type, BiomeTypes biome)
        {
            GrabInitList();
            try
            {
                TerrainObject TO = GetResourceNodePrefab(type, biome);
                TrackableObject track = TO.SpawnFromPrefabAndAddToSaveData(scenePos, rotation).TerrainObject;
                ResourceDispenser RD = track.GetComponent<ResourceDispenser>();
                RD.RemoveFromWorld(false, false, true, false);
                RD.SetRegrowOverrideTime(0.1f); // Check later - spawns too quickly after first spawn
                return RD;
            }
            catch (Exception e)
            {
                throw new NullReferenceException("TTExtUtil: SpawnResourceNodeAnimated encountered an error - " + e.Message, e);
            }
        }
        public static void DestroyResourceNode(ResourceDispenser resDisp, Vector3 impactVec, bool spawnChunks)
        {
            GrabInitList();
            if (spawnChunks)
                resDisp.RemoveFromWorld(true, true, false, false);
            else
            {
                inst.death.Invoke(resDisp, new object[1] { impactVec });
                ManSFX.inst.PlaySceneryDestroyedSFX(resDisp);
                resDisp.RemoveFromWorld(false, true, true, true);
            }
        }



        private static FieldInfo SetPiecesGet = typeof(ManWorld).GetField("m_AllSetPieces", BindingFlags.NonPublic | BindingFlags.Instance);
        private static List<TerrainSetPiece> piecesCached = new List<TerrainSetPiece>();
        public static List<TerrainSetPiece> GetAllSetPieces()
        {
            piecesCached.Clear();
            foreach (var item in (List<TerrainSetPiece>)SetPiecesGet.GetValue(ManWorld.inst))
            {
                if (item.name.StartsWith("SPM") || item.name.StartsWith("RD") || item.name.StartsWith("EXP"))
                { 
                    if (ManDLC.inst.HasAnyDLCOfType(ManDLC.DLCType.RandD))
                        piecesCached.Add(item);
                }
                else
                    piecesCached.Add(item);
            }
            return piecesCached;
        }
        public static IEnumerable<string> GetAllSetPieceNames()
        {
            return GetAllSetPieces().ConvertAll(x => x.name);
        }
        public static TerrainSetPiece GetSetPiece(string SetPieceName, bool complainWhenFail = true)
        {
            List<TerrainSetPiece> TSPL = GetAllSetPieces();
            TerrainSetPiece TSP = TSPL.Find(x => x.name == SetPieceName);
            if (TSP == null)
            {
                Debug_TTExt.Assert(complainWhenFail, SetPieceName + " SetPiece in base game could not be found!");
                return null;
            }
            return TSP;
        }
        public static List<TerrainSetPiece> GetSetPieces(HashSet<string> SetPieceNames)
        {
            return GetAllSetPieces().FindAll(x => SetPieceNames.Contains(x.name));
        }
        private static FieldInfo ActiveSetPiecesGet = typeof(ManWorld).GetField("m_SetPiecesPlacement", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo SetPieceContents = typeof(TerrainSetPiece).GetField("m_TerrainObjectsList", BindingFlags.NonPublic | BindingFlags.Instance);
        public static bool DestroySetPiece(WorldPosition worldPos)
        {
            LegModExt.BypassSetPieceChecks = true;
            InvokeHelper.Invoke(() => { LegModExt.BypassSetPieceChecks = false; }, 2f);
            List<ManWorld.TerrainSetPiecePlacement> places = (List<ManWorld.TerrainSetPiecePlacement>)ActiveSetPiecesGet.GetValue(ManWorld.inst);
            bool remov = places.RemoveAll(x => x.m_WorldPosition.TileCoord == worldPos.TileCoord) > 0;
            ActiveSetPiecesGet.SetValue(ManWorld.inst, places);
            return remov;
        }
        private static List<ManWorld.SavedSetPiece> savedCache = new List<ManWorld.SavedSetPiece>();
        public static TerrainSetPiece ForceSpawnSetPiece(string name, WorldPosition worldPos, float groundOffset, Vector3 forwardsSnapped)
        {
            DestroySetPiece(worldPos);
            return SpawnSetPiece(name, worldPos, groundOffset, forwardsSnapped);
        }
        public static TerrainSetPiece SpawnSetPiece(string name, WorldPosition worldPos, float groundOffset, Vector3 forwardsSnapped)
        {
            try
            {
                ManWorld.SavedSetPiece SSP = new ManWorld.SavedSetPiece
                {
                    m_Name = name,
                    m_WorldPosition = worldPos,
                    m_Rotation = Mathf.RoundToInt(Vector3.SignedAngle(Vector3.forward, forwardsSnapped, Vector3.up) / 90),
                    m_BaseHeight = groundOffset,
                };
                savedCache.Add(SSP);
                ManWorld.inst.AddTerrainSetPiecesForNetworkedGame(savedCache);
                if (ManWorld.inst.GetSetPieceDataForTile(worldPos.TileCoord, true, out var TSP, out _, out _))
                {
                    ManWorld.inst.GetTilesTouchedBySetPiece(TSP, worldPos, SSP.m_Rotation, out IntVector2 min, out IntVector2 max);
                    for (int x = min.x - 1; x <= max.x; x++)
                    {
                        for (int y = min.y - 1; y <= max.y; y++)
                        {
                            ManWorldTileExt.ReloadTile(new IntVector2(x, y));
                        }
                    }
                    if (TSP.name != name)
                        throw new NullReferenceException("SpawnSetPiece failed to find the CORRECT SetPiece it had spawned!");
                    return TSP;
                }
                throw new NullReferenceException("SpawnSetPiece failed to find the SetPiece it had spawned!");
            }
            finally
            {
                savedCache.Clear();
            }
        }
        

        public static IEnumerable<TerrainObject> GetContents(TerrainSetPiece TSP)
        {
            List<TerrainSetPiece.TerrainObjectData> obs = (List<TerrainSetPiece.TerrainObjectData>)SetPieceContents.GetValue(TSP);
            if (obs == null)
                throw new NullReferenceException("GetContents could not get contents of the SetPiece!");
            return obs.ConvertAll(x => x.m_TerrainObject);
        }
        public static TerrainObject SpawnObject(TerrainObject TO, Vector3 scenePos, Quaternion rotation)
        {
            try
            {
                TrackableObject track = TO.SpawnFromPrefabAndAddToSaveData(scenePos, rotation).TerrainObject;
                return track.GetComponent<TerrainObject>();
            }
            catch (Exception e)
            {
                throw new NullReferenceException("TTExtUtil: SpawnObject encountered an error - " + e.Message, e);
            }
        }

        public static void DestroyObject(TerrainObject Obj)
        {
            Obj.Recycle();
        }




        // General-Purpose explosion for use beyond vanilla
        public static int BombsActive => queuedBombs.Count;

        private const float vertOffsetDist = 250;
        internal static List<QueuedBomb> queuedBombs = new List<QueuedBomb>();
        public static void LaunchBaseBomb(Vector3 scenePos, Vector3 forwards, Action<QueuedBomb, Vector3> PostEvent, Visible target = null)
        {
            //  The bomb spawns about 500 meters off the ground, this will have to predict based on that
            DeliveryBombSpawner DBS = ManSpawn.inst.SpawnDeliveryBombNew(scenePos, DeliveryBombSpawner.ImpactMarkerType.Tech);
            DBS.SetSpawnParams(scenePos + (Vector3.up * vertOffsetDist), DeliveryBombSpawner.ImpactMarkerType.Tech);
            QueuedBomb QB = new QueuedBomb(DBS, forwards, PostEvent, target);
            queuedBombs.Add(QB);
        }
        public struct QueuedBomb
        {
            public Vector3 forwards { get; private set; }
            public Action<QueuedBomb, Vector3> postEvent { get; private set; }
            internal KineticDriver bomb { get; private set; }
            public Visible target { get; private set; }

            internal QueuedBomb(DeliveryBombSpawner DBS, Vector3 forward, Action<QueuedBomb, Vector3> PostEvent, Visible aimTarget = null)
            {
                postEvent = PostEvent;
                target = aimTarget;
                forwards = forward;
                if (target)
                {
                    bomb = DBS.gameObject.AddComponent<KineticDriver>();
                    bomb.turnSped = UnityEngine.Random.Range(2, 16) + UnityEngine.Random.Range(1, 4);
                    bomb.bomb = this;
                }
                else
                    bomb = null;
                DBS.BombDeliveredEvent.Subscribe(OnImpact);
            }
            private void OnImpact(Vector3 strike)
            {
                postEvent.Invoke(this, strike);
                if (bomb)
                    Destroy(bomb);
                queuedBombs.Remove(this);
            }
        }
        internal class KineticDriver : MonoBehaviour
        {
            internal QueuedBomb bomb;
            internal float turnSped;
            private void FixedUpdate()
            {
                var rbody = gameObject.GetComponent<Rigidbody>();
                if (rbody)
                {
                    if (!bomb.target)
                    {
                        enabled = false;
                        return;
                    }
                    var aiming = Quaternion.LookRotation(bomb.target.centrePosition - rbody.position, rbody.rotation * Vector3.up).normalized;
                    rbody.MoveRotation(Quaternion.RotateTowards(rbody.rotation, aiming, turnSped * Time.fixedDeltaTime));
                    /*
                    if (rbody.useGravity)
                        rbody.AddForceAtPosition(Physics.gravity * 0.25f, rbody.position, ForceMode.Impulse);
                    */
                }
            }
        }

        internal class GUIManaged : GUILayoutHelpers
        {
            private static bool controlledDisp = false;
            private static HashSet<string> enabledTabs = null;
            private static BiomeTypes curBiome = BiomeTypes.Grassland;
            private static bool showRes = false;
            private static bool snapTerrain = false;
            public static void GUIGetTotalManaged()
            {
                if (enabledTabs == null)
                {
                    enabledTabs = new HashSet<string>();
                }
                GUILayout.Box("--- Spawner --- ");
                bool show = controlledDisp && Singleton.playerTank;
                if (GUILayout.Button(" Enabled Loading: " + show))
                    controlledDisp = !controlledDisp;
                if (controlledDisp)
                {
                    try
                    {
                        GUIResNodes();
                        GUISetPieces();
                    }
                    catch (ExitGUIException e)
                    {
                        throw e;
                    }
                    catch (Exception e)
                    {
                        Debug_TTExt.Log("SpawnHelper UI Debug errored - " + e);
                    }
                }
            }

            private static void GUIResNodes()
            {
                showRes = AltUI.Toggle(showRes, "Spawn Resource Nodes");
                if (showRes)
                {
                    Vector3 spawnPos = Singleton.playerTank.boundsCentreWorld + (Singleton.cameraTrans.forward * 64);
                    curBiome = (BiomeTypes)GUIToolbarCategoryDisp<BiomeTypes>((int)curBiome);
                    if (GUILayout.Button("Set Biome Based On Own Tech"))
                        curBiome = ManWorld.inst.GetBiomeWeightsAtScenePosition(Singleton.playerTank.boundsCentreWorld).Biome(0).BiomeType;
                    snapTerrain = AltUI.Toggle(snapTerrain, "Snap To Terrain");
                    GUICategoryDisp<SceneryTypes>(ref enabledTabs, "Explosions", x => SpawnResourceNodeExplosion(spawnPos, x, curBiome));
                    GUICategoryDisp<SceneryTypes>(ref enabledTabs, "Nodes", x => SpawnResourceNode(spawnPos, Quaternion.identity, x, curBiome)); 
                    if (GUILayout.Button("Destroy Close Resource Node"))
                    {
                        var iterate = ManVisible.inst.VisiblesTouchingRadius(spawnPos, 64, new Bitfield<ObjectTypes>(new ObjectTypes[1] { ObjectTypes.Scenery }));
                        if (iterate.Any())
                            DestroyResourceNode(iterate.FirstOrDefault().resdisp, 
                                (iterate.FirstOrDefault().centrePosition - Singleton.playerTank.boundsCentreWorld).normalized, true);
                    }
                    if (GUILayout.Button("Remove Close Resource Node"))
                    {
                        var iterate = ManVisible.inst.VisiblesTouchingRadius(spawnPos, 64, new Bitfield<ObjectTypes>(new ObjectTypes[1] { ObjectTypes.Scenery }));
                        if (iterate.Any())
                            DestroyResourceNode(iterate.FirstOrDefault().resdisp, Vector3.forward, false);
                    }
                }
            }

            private static bool showSetPieces = false;
            private static bool showALLSetPieces = false;
            private static bool showSetPieceContents = false;
            private static TerrainSetPiece selectedSP = null;
            private static TerrainObject unmanaged = null;
            private static Vector2 scroller5 = Vector2.zero;
            private static void GUISetPieces()
            {
                showSetPieces = AltUI.Toggle(showSetPieces, "Spawn Set Pieces");
                if (showSetPieces)
                {
                    GUILayout.BeginVertical(AltUI.TextfieldBordered);
                    showALLSetPieces = AltUI.Toggle(showALLSetPieces, "Show Set Pieces");
                    if (showALLSetPieces)
                    {
                        scroller5 = GUILayout.BeginScrollView(scroller5,AltUI.TextfieldBordered, GUILayout.Height(300));
                        GUILayout.Label("Set Pieces:");
                        foreach (var item in GetAllSetPieceNames())
                        {
                            GUILayout.BeginHorizontal();
                            if (GUILayout.Button(item))
                            {
                                selectedSP = GetSetPiece(item);
                            }
                            GUILayout.EndHorizontal();
                        }
                        GUILayout.EndVertical();
                    }
                    if (selectedSP != null)
                    {
                        GUILayout.BeginVertical(AltUI.TextfieldBorderedBlue);
                        GUILayout.Label(selectedSP.name, AltUI.LabelBlackTitle);
                        if (GUILayout.Button("Spawn Naow"))
                        {
                            var TSP = ForceSpawnSetPiece(selectedSP.name, 
                                WorldPosition.FromScenePosition(Singleton.playerPos + Vector3.forward * 64),
                             0, Vector3.forward);
                        }
                        if (unmanaged != null && GUILayout.Button("Remove Nearby"))
                        {
                            DestroyObject(unmanaged);
                        }
                        showSetPieceContents = AltUI.Toggle(showSetPieceContents, "Show Contents");
                        if (showSetPieceContents)
                        {
                            GUILayout.BeginVertical(AltUI.TextfieldBordered);
                            foreach (var item in GetContents(selectedSP))
                            {
                                if (GUILayout.Button(item.name))
                                {
                                    if (unmanaged != null)
                                        DestroyObject(unmanaged);
                                    Vector3 spawnPos = Singleton.playerTank.boundsCentreWorld + (Singleton.cameraTrans.forward * 64);
                                    unmanaged = SpawnObject(item, spawnPos, Quaternion.identity);
                                }
                            }
                            GUILayout.EndVertical();
                        }
                        GUILayout.EndVertical();
                    }
                    GUILayout.EndVertical();
                }
            }

        }
    }
}
#endif