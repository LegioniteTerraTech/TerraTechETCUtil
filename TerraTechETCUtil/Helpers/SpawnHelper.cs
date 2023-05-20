using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;

namespace TerraTechETCUtil
{
    public class SpawnHelper
    {
        private static SpawnHelper inst;
        private MethodInfo death = typeof(ResourceDispenser).GetMethod("PlayDeathAnimation", BindingFlags.NonPublic | BindingFlags.Instance);
        private FieldInfo deathParticles = typeof(ResourceDispenser).GetField("m_BigDebrisPrefab", BindingFlags.NonPublic | BindingFlags.Instance);
        private FieldInfo deathSound = typeof(ManSFX).GetField("m_SceneryDebrisEvents", BindingFlags.NonPublic | BindingFlags.Instance);
        private MethodInfo deathSoundGet = typeof(ManSFX).GetMethod("GetScenerySFXType", BindingFlags.NonPublic | BindingFlags.Instance);
        private FieldInfo sce = typeof(ManSpawn).GetField("spawnableScenery", BindingFlags.NonPublic | BindingFlags.Instance);
        private Dictionary<SceneryTypes, Dictionary<BiomeTypes, List<TerrainObject>>> objs = new Dictionary<SceneryTypes, Dictionary<BiomeTypes, List<TerrainObject>>>();
        private Dictionary<string, TerrainObject> objsNonRes = new Dictionary<string, TerrainObject>();


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
                List<Visible> objsRaw = (List<Visible>)inst.sce.GetValue(ManSpawn.inst);
                foreach (var item in objsRaw)
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
                        if (inst.objs.TryGetValue(ST, out objBiome))
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
                            inst.objs.Add(ST, objBiome);
                        }
                    }
                    else
                    {
                        try
                        {
                            inst.objsNonRes.Add(item.name, item.GetComponent<TerrainObject>());
                        }
                        catch
                        {
                            Debug_TTExt.Log("Item of name " + item.name + " already exists in objsNonRes, but there's more than one of them!");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug_TTExt.Log("FAILED TO FETCH - " + e);
            }
            if (inst.objs.Count == 0)
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
                RD.RemoveFromWorld(false, false, true, false);
                RD.SetRegrowOverrideTime(2); // Check later - spawns too quickly after first spawn
                return RD;
            }
            catch (Exception e)
            {
                throw new NullReferenceException("RandomAdditions: SpawnResourceNode encountered an error - " + e.Message, e);
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
                        if (GUILayout.Button("Resource Nodes"))
                            showRes = !showRes;
                        if (showRes)
                        {
                            Vector3 spawnPos = Singleton.playerTank.boundsCentreWorld + (Singleton.cameraTrans.forward * 64);
                            curBiome = (BiomeTypes)GUIToolbarCategoryDisp<BiomeTypes>((int)curBiome);
                            if (GUILayout.Button("Set Biome Based On Own Tech"))
                                curBiome = ManWorld.inst.GetBiomeWeightsAtScenePosition(Singleton.playerTank.boundsCentreWorld).Biome(0).BiomeType;
                            snapTerrain = GUILayout.Toggle(snapTerrain, "Snap To Terrain");
                            GUICategoryDisp<SceneryTypes>(ref enabledTabs, "Explosions", x => SpawnResourceNodeExplosion(spawnPos, x, curBiome));
                            GUICategoryDisp<SceneryTypes>(ref enabledTabs, "Nodes", x => SpawnResourceNode(spawnPos, Quaternion.identity, x, curBiome));
                            if (GUILayout.Button("Remove Resource Node"))
                            {
                                var iterate = ManVisible.inst.VisiblesTouchingRadius(spawnPos, 64, new Bitfield<ObjectTypes>(new ObjectTypes[1] { ObjectTypes.Scenery }));
                                if (iterate.Any())
                                    DestroyResourceNode(iterate.FirstOrDefault().resdisp, Vector3.forward, false);
                            }
                        }
                    }
                    catch (ExitGUIException e)
                    {
                        throw e;
                    }
                    catch { }
                }
            }
        }
    }
}
