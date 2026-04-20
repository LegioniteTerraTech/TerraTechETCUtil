using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using HarmonyLib;

#if !EDITOR
namespace TerraTechETCUtil
{
    /// <summary>
    /// Handles mod-side spawning of game objects
    /// </summary>
    public class SpawnHelper : MonoBehaviour
    {
        private MethodInfo death = typeof(ResourceDispenser).GetMethod("PlayDeathAnimation", BindingFlags.NonPublic | BindingFlags.Instance);
        private FieldInfo deathParticles = typeof(ResourceDispenser).GetField("m_BigDebrisPrefab", BindingFlags.NonPublic | BindingFlags.Instance);
        private FieldInfo deathSound = typeof(ManSFX).GetField("m_SceneryDebrisEvents", BindingFlags.NonPublic | BindingFlags.Instance);
        private MethodInfo deathSoundGet = typeof(ManSFX).GetMethod("GetScenerySFXType", BindingFlags.NonPublic | BindingFlags.Instance);


        private static SpawnHelper inst;
        /// <summary>
        /// Lookup for biomes based on their names
        /// </summary>
        public static Dictionary<string, Biome> BiomesByName = new Dictionary<string, Biome>();
        /// <summary>
        /// Lookup for biomes based on their <see cref="BiomeTypes"/>.  
        /// Not advised as <see cref="BiomeTypes"/> appears obsolete.
        /// <para>Use <see cref="BiomesByName"/> instead.</para>
        /// </summary>
        public static Dictionary<BiomeTypes, Biome> FirstBiomesByType = new Dictionary<BiomeTypes, Biome>();

        private Dictionary<SceneryTypes, Dictionary<string, List<TerrainObject>>> objs = new Dictionary<SceneryTypes, Dictionary<string, List<TerrainObject>>>();
        private Dictionary<string, TerrainObject> objsNonRes = new Dictionary<string, TerrainObject>();

        private static HashSet<int> captured = new HashSet<int>();
        /// <summary>
        /// Iterate <see cref="SceneryTypes"/> based on their ingredient <see cref="ChunkTypes"/>
        /// </summary>
        /// <param name="type">To filter by</param>
        /// <returns>Iterator for <see cref="SceneryTypes"/></returns>
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
        /// <summary>
        /// Find the <see cref="TerrainObject"/> based on the given <see cref="SceneryTypes"/>.
        /// </summary>
        /// <param name="type">To filter by</param>
        /// <returns>Lookup of <see cref="SceneryTypes"/> and variants of them based on naming</returns>
        public static Dictionary<string, List<TerrainObject>> GetSceneryByType(SceneryTypes type)
        {
            GrabInitList();
            inst.objs.TryGetValue(type, out var outcome);
            return outcome;
        }
        /// <summary>
        /// Iterate all <see cref="TerrainObject"/>s based on <see cref="Biome"/> type name
        /// </summary>
        /// <param name="type">The name of the <see cref="Biome"/> to search</param>
        /// <returns>Iterator for <see cref="TerrainObject"/>s found</returns>
        public static IEnumerable<TerrainObject> IterateSceneryByBiome(string type)
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
        /// <summary>
        /// Iterate all <see cref="TerrainObject"/>s in the game
        /// </summary>
        /// <returns>Iterator of all scenery</returns>
        public static IEnumerable<Dictionary<string, List<TerrainObject>>> IterateSceneryTypes()
        {
            GrabInitList();
            foreach (var item in inst.objs)
            {
                yield return item.Value;
            }
        }

        /// <summary>
        /// Get the primary prefab list the game uses to spawn most 
        /// <see cref="TerrainObject"/>s and <see cref="ResourceDispenser"/>s.
        /// <para>You have to patch postfix <b>TerrainObjectTable.InitLookupTable</b> and 
        /// <b></b> this to add/change content in the main game.</para>
        /// </summary>
        /// <returns>The prefab <see cref="TerrainObjectTable"/> with all the spawn prefabs in it</returns>
        /// <exception cref="NullReferenceException"></exception>
        public static TerrainObjectTable GetAllTerrainObjectPrefabTable()
        {
            FieldInfo sce = typeof(ManSpawn).GetField("m_TerrainObjectTable", BindingFlags.NonPublic | BindingFlags.Instance);
            if (sce == null)
                throw new NullReferenceException("Field \"ManSpawn.m_TerrainObjectTable\" no longer exists!");
            var lookup = (TerrainObjectTable)sce.GetValue(ManSpawn.inst);
            if (lookup == null)
                throw new NullReferenceException("SpawnHelper: ManSpawn.inst has not allocated m_TerrainObjectTable for some reason and SpawnHelper fetch failed");
            return lookup;
        }
        /// <summary>
        /// Get the primary prefab list the game uses to spawn most 
        /// <see cref="TerrainObject"/>s and <see cref="ResourceDispenser"/>s.
        /// <para>WARNING: THIS LIST CAN CHANGE BETWEEN GAME SESSIONS</para>
        /// </summary>
        /// <returns>The prefab list</returns>
        /// <exception cref="NullReferenceException"></exception>
        public static Dictionary<string, TerrainObject> GetAllTerrainObjectPrefabList()
        {
            FieldInfo sce2 = typeof(TerrainObjectTable).GetField("m_GUIDToPrefabLookup", BindingFlags.NonPublic | BindingFlags.Instance);
            if (sce2 == null)
                throw new NullReferenceException("Field \"TerrainObjectTable.m_GUIDToPrefabLookup\" no longer exists!");

            var lookup = GetAllTerrainObjectPrefabTable();

            return lookup.GetLookupList();
        }

        /// <summary>
        /// Find <see cref="Biome"/> by name
        /// </summary>
        /// <param name="name">The data name of the biome</param>
        /// <returns>The <see cref="BiomeTypes"/> of the biome</returns>
        public static BiomeTypes GetBiomeFromName(string name)
        {
            if (name.Contains("SaltFlats") || name.Contains("Flats"))
                return BiomeTypes.SaltFlats;
            if (name.Contains("Mountain") || name.Contains("Biome7"))
                return BiomeTypes.Mountains;
            if (name.Contains("Pillar") || name.Contains("Biome5") || name.Contains("5thBiome"))
                return BiomeTypes.Pillars;
            if (name.Contains("Desert"))
                return BiomeTypes.Desert;
            if (name.Contains("Ice"))
                return BiomeTypes.Ice;
            return BiomeTypes.Grassland;
        }
        /// <summary>
        /// Refresh the <see cref="BiomeMap"/>s gathered by <see cref="SpawnHelper"/> incase it was changed
        /// </summary>
        /// <param name="ToMap">The <see cref="BiomeMap"/> to refetch for, leave null to do the whole game</param>
        public static void RefetchResources(BiomeMap ToMap = null) => inst.RefreshResources(ToMap);
        /// <summary>
        /// Refresh the <see cref="BiomeMap"/>s gathered by <see cref="SpawnHelper"/> incase it was changed
        /// </summary>
        /// <param name="ToMap">The <see cref="BiomeMap"/> to refetch for, leave null to do the whole game</param>
        public void RefreshBiomeResources(BiomeMap ToMap = null)
        {
            //Debug_TTExt.Log("RefreshBiomeResources attempt");
            objs.Clear();
            BiomesByName.Clear();
            FirstBiomesByType.Clear();
            if (ToMap == null)
                ToMap = ModeMain.inst.m_BiomeMaps.MapStack.SelectCompatibleBiomeMap();
            foreach (var item in ToMap.IterateBiomes())
            {
                var BT = item.BiomeType;
                //Debug_TTExt.Log("- " + item.name + " | " + BT);
                BiomesByName.Add(item.name, item);
                if (FirstBiomesByType.ContainsKey(BT))
                    FirstBiomesByType.Add(BT, item);
                if (item.DetailLayers != null)
                {
                    foreach (var item2 in item.DetailLayers)
                    {
                        if (item2.distributor.basic != null)
                        {
                            foreach (var item4 in item2.distributor.basic.terrainObject)
                            {
                                if (item4 == null) break;
                                var scenery = item4.GetComponent<ResourceDispenser>();
                                if (scenery == null)
                                    break;
                                SceneryTypes ST = (SceneryTypes)scenery.GetComponent<Visible>().ItemType;
                               // Debug_TTExt.Log("1- " + item.name + " | " + BT + " | " + ST);
                                List<TerrainObject> objRand;
                                Dictionary<string, List<TerrainObject>> objBiome;
                                if (objs.TryGetValue(ST, out objBiome))
                                {
                                    if (objBiome.TryGetValue(item.name, out objRand))
                                        objRand.Add(item4);
                                    else
                                    {
                                        objRand = new List<TerrainObject> { item4 };
                                        objBiome.Add(item.name, objRand);
                                    }
                                }
                                else
                                {
                                    objRand = new List<TerrainObject> { item4 };
                                    objBiome = new Dictionary<string, List<TerrainObject>>()
                                    {
                                        { item.name, objRand }
                                    };
                                    objs.Add(ST, objBiome);
                                }
                            }
                        }
                        if (item2.distributor.decoration != null)
                        {
                            foreach (var item4 in item2.distributor.decoration.terrainObject)
                            {
                                if (item4 == null) break;
                                var scenery = item4.GetComponent<ResourceDispenser>();
                                if (scenery == null)
                                    break;
                                SceneryTypes ST = (SceneryTypes)scenery.GetComponent<Visible>().ItemType;
                                // Debug_TTExt.Log("2- " + item.name + " | " + BT + " | " + ST);
                                List<TerrainObject> objRand;
                                Dictionary<string, List<TerrainObject>> objBiome;
                                if (objs.TryGetValue(ST, out objBiome))
                                {
                                    if (objBiome.TryGetValue(item.name, out objRand))
                                        objRand.Add(item4);
                                    else
                                    {
                                        objRand = new List<TerrainObject> { item4 };
                                        objBiome.Add(item.name, objRand);
                                    }
                                }
                                else
                                {
                                    objRand = new List<TerrainObject> { item4 };
                                    objBiome = new Dictionary<string, List<TerrainObject>>()
                                    {
                                        { item.name, objRand }
                                    };
                                    objs.Add(ST, objBiome);
                                }
                            }
                        }
                        if (item2.distributor.variants != null)
                        {
                            foreach (var item3 in item2.distributor.variants)
                            {
                                if (item3.terrainObject != null)
                                {
                                    foreach (var item4 in item3.terrainObject)
                                    {
                                        if (item4 == null) break;
                                        var scenery = item4.GetComponent<ResourceDispenser>();
                                        if (scenery == null)
                                            break;
                                        SceneryTypes ST = (SceneryTypes)scenery.GetComponent<Visible>().ItemType;
                                       // Debug_TTExt.Log("3- " + item.name + " | " + BT + " | " + ST);
                                        List<TerrainObject> objRand;
                                        Dictionary<string, List<TerrainObject>> objBiome;
                                        if (objs.TryGetValue(ST, out objBiome))
                                        {
                                            if (objBiome.TryGetValue(item.name, out objRand))
                                                objRand.Add(item4);
                                            else
                                            {
                                                objRand = new List<TerrainObject> { item4 };
                                                objBiome.Add(item.name, objRand);
                                            }
                                        }
                                        else
                                        {
                                            objRand = new List<TerrainObject> { item4 };
                                            objBiome = new Dictionary<string, List<TerrainObject>>()
                                            {
                                                { item.name, objRand }
                                            };
                                            objs.Add(ST, objBiome);
                                        }
                                    }
                                }
                            }
                        }
                        if (item2.distributor.upgradeRules != null)
                        {
                            foreach (var item3 in item2.distributor.upgradeRules)
                            {
                                if (item3.upgrade?.terrainObject != null)
                                {
                                    foreach (var item4 in item3.upgrade.terrainObject)
                                    {
                                        if (item4 == null) break;
                                        var scenery = item4.GetComponent<ResourceDispenser>();
                                        if (scenery == null)
                                            break;
                                        SceneryTypes ST = (SceneryTypes)scenery.GetComponent<Visible>().ItemType;
                                        // Debug_TTExt.Log("4- " + item.name + " | " + BT + " | " + ST);
                                        List<TerrainObject> objRand;
                                        Dictionary<string, List<TerrainObject>> objBiome;
                                        if (objs.TryGetValue(ST, out objBiome))
                                        {
                                            if (objBiome.TryGetValue(item.name, out objRand))
                                                objRand.Add(item4);
                                            else
                                            {
                                                objRand = new List<TerrainObject> { item4 };
                                                objBiome.Add(item.name, objRand);
                                            }
                                        }
                                        else
                                        {
                                            objRand = new List<TerrainObject> { item4 };
                                            objBiome = new Dictionary<string, List<TerrainObject>>()
                                            {
                                                { item.name, objRand }
                                            };
                                            objs.Add(ST, objBiome);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            if (!objs.Any())
                Debug_TTExt.Log("RefreshBiomeResources NOTHING");
        }
        /// <summary>
        /// Refresh the <see cref="BiomeMap"/>s gathered by <see cref="SpawnHelper"/> incase it was changed
        /// </summary>
        /// <param name="ToMap">The <see cref="BiomeMap"/> to refetch for, leave null to do the whole game</param>
        public void RefreshResources(BiomeMap ToMap = null)
        {
            try
            {
                objsNonRes.Clear();
                if (ManWorld.inst.CurrentBiomeMap.GetNumBiomes() == 0)
                {
                    Debug_TTExt.Log("No biomes?");
                    objs.Clear();
                }

                Dictionary<string, TerrainObject> objsRaw = GetAllTerrainObjectPrefabList();

                RefreshBiomeResources(ToMap);
                bool gatherResTemp = !objs.Any();
                foreach (var itemPair in objsRaw)
                {
                    TerrainObject item = itemPair.Value;
                    Visible scenery = item?.GetComponent<Visible>();
                    try
                    {
                        if (item == null)
                            return;
                        if (item.GetComponent<ResourceDispenser>())
                        {
                            // Do it in the previous loop
                            if (!gatherResTemp)
                                continue;
                            SceneryTypes ST = (SceneryTypes)scenery.ItemType;
                            BiomeTypes BT = GetBiomeFromName(item.name);
                            //Debug_TTExt.Log("- " + item.name + " | " + BT + " | " + ST);
                            List<TerrainObject> objRand;
                            Dictionary<string, List<TerrainObject>> objBiome;
                            if (objs.TryGetValue(ST, out objBiome))
                            {
                                if (objBiome.TryGetValue(BT.ToString(), out objRand))
                                    objRand.Add(item.GetComponent<TerrainObject>());
                                else
                                {
                                    objRand = new List<TerrainObject> { item.GetComponent<TerrainObject>() };
                                    objBiome.Add(BT.ToString(), objRand);
                                }
                            }
                            else
                            {
                                objRand = new List<TerrainObject> { item.GetComponent<TerrainObject>() };
                                objBiome = new Dictionary<string, List<TerrainObject>>
                                {
                                    { BT.ToString(), objRand }
                                };
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
        private SpawnHelper()
        {
            RefreshResources();
        }

        /// <summary>
        /// Index all entries to be used in <see cref="SpawnHelper"/>
        /// </summary>
        /// <param name="forceReInit"></param>
        public static void GrabInitList(bool forceReInit = false)
        {
            if (inst != null && !forceReInit)
                return;
            inst = new SpawnHelper();
        }

        /// <summary>
        /// Logs all indexed <see cref="TerrainObject"/>s in <see cref="SpawnHelper"/>
        /// </summary>
        /// <param name="SB">The <see cref="StringBuilder"/> to write it all to</param>
        public static void PrintAllRegisteredResourceNodes(StringBuilder SB)
        {
            GrabInitList();
            SB.AppendLine("Resources:");
            foreach (var item in inst.objs)
            {
                SB.AppendLine("  Type: " + item.Key);
                foreach (var item2 in item.Value)
                {
                    SB.AppendLine("   Biome: " + item2.Key);
                    foreach (var item3 in item2.Value)
                    {
                        SB.AppendLine("     Name: " + (item3.name.NullOrEmpty() ? "<NULL>" : item3.name));
                    }
                }
            }
            SB.AppendLine("END");
        }
        /// <summary>
        /// Logs all indexed <b>non-</b><see cref="TerrainObject"/>s in <see cref="SpawnHelper"/>
        /// </summary>
        /// <param name="SB">The <see cref="StringBuilder"/> to write it all to</param>
        public static void PrintAllRegisteredNonResourceObjects(StringBuilder SB)
        {
            GrabInitList();
            SB.AppendLine("TerrainObject:");
            foreach (var item in inst.objsNonRes)
            {
                SB.AppendLine("  Name: " + (item.Value.name.NullOrEmpty() ? "<NULL>" : item.Value.name));
            }
            SB.AppendLine("END");
        }


        /// <summary>
        /// Find a prefab using given data
        /// </summary>
        /// <param name="Name">The <see cref="UnityEngine.Object.name"/> to search by</param>
        /// <returns>Found object, otherwise null</returns>
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

        /// <summary>
        /// Find a prefab using given data
        /// </summary>
        /// <param name="type">The <see cref="SceneryTypes"/> to search by. 
        /// <see cref="SceneryTypes"/> are partially obsolete, vague and not recommended</param>
        /// <param name="biomeName">The name of the <see cref="Biome"/> to search by. </param>
        /// <returns>Found object, otherwise null</returns>
        public static TerrainObject GetResourceNodePrefab(SceneryTypes type, string biomeName)
        {
            GrabInitList();
            try
            {
                TerrainObject TO = null;
                if (inst.objs.TryGetValue(type, out var objBiome))
                {
                    if (objBiome.TryGetValue(biomeName, out var objRand))
                    {
                        TO = objRand.GetRandomEntry();
                    }
                    else
                        TO = objBiome[BiomeTypes.Grassland.ToString()].GetRandomEntry();
                    if (TO == null)
                        throw new NullReferenceException("GetResourceNodePrefab entry for Biome " + biomeName + " has NO entries!");
                }
                else
                    throw new NullReferenceException("GetResourceNodePrefab entry for Scenery " + type.ToString() + " has NO entries!");
                return TO;
            }
            catch (Exception)
            {
                throw new NullReferenceException("GetResourceNodePrefab entry for " + type.ToString() + " | " + biomeName + " has NO entries!");
            }
        }
        /// <summary>
        /// Find a prefab using given data
        /// </summary>
        /// <param name="name">The <see cref="UnityEngine.Object.name"/> to search by</param>
        /// <returns>Found object, otherwise null</returns>
        public static TerrainObject GetResourceNodePrefab(string name)
        {
            if (name.NullOrEmpty())
                throw new ArgumentNullException("name null or empty");
            GrabInitList();
            try
            {
                foreach (var item in inst.objs)
                {
                    foreach (var item2 in item.Value)
                    {
                        if (item2.Value != null)
                        {
                            foreach (var item3 in item2.Value)
                            {
                                if (item3.name == name)
                                    return item3;
                            }
                        }
                    }
                }
                throw new NullReferenceException("GetResourceNodePrefab entry for Scenery " + name + " has NO entries!");
            }
            catch (Exception)
            {
                throw new NullReferenceException("GetResourceNodePrefab entry for " + name + " has NO entries!");
            }
        }
        /// <summary>
        /// Spawn a <see cref="ResourceDispenser"/> Scenery <b>explosion</b> using given data
        /// </summary>
        /// <param name="scenePos">The scene positioning of the explosion.</param>
        /// <param name="type">The <see cref="SceneryTypes"/> to search by.</param>
        /// <param name="biomeName">The name of the <see cref="Biome"/> to search by. </param>
        public static void SpawnResourceNodeExplosion(Vector3 scenePos, SceneryTypes type, string biomeName)
        {
            GrabInitList();
            var resDisp = GetResourceNodePrefab(type, biomeName).GetComponent<ResourceDispenser>();
            ((Transform)inst.deathParticles.GetValue(resDisp)).Spawn(null, scenePos, Quaternion.LookRotation(Vector3.up, Vector3.back));
            ((FMODEvent[])inst.deathSound.GetValue(ManSFX.inst))[(int)inst.deathSoundGet.Invoke(ManSFX.inst, new object[1] { type })].PlayOneShot(scenePos);
        }
        /// <summary>
        /// Spawn a <see cref="ResourceDispenser"/> Scenery <b>explosion</b> using given data
        /// </summary>
        /// <param name="scenePos">The scene positioning of the explosion.</param>
        /// <param name="resDisp">The direct reference to the <see cref="ResourceDispenser"/> to use.</param>
        public static void SpawnResourceNodeExplosion(Vector3 scenePos, ResourceDispenser resDisp)
        {
            GrabInitList();
            var type = resDisp.GetSceneryType();
            ((Transform)inst.deathParticles.GetValue(resDisp)).Spawn(null, scenePos, Quaternion.LookRotation(Vector3.up, Vector3.back));
            ((FMODEvent[])inst.deathSound.GetValue(ManSFX.inst))[(int)inst.deathSoundGet.Invoke(ManSFX.inst, new object[1] { type })].PlayOneShot(scenePos);
        }
        /// <summary>
        /// Spawn a <see cref="ResourceDispenser"/> Scenery using given data
        /// </summary>
        /// <param name="scenePos">The scene positioning of the spawned resource node.</param>
        /// <param name="type">The <see cref="SceneryTypes"/> to search by.</param>
        /// <param name="biomeName">The name of the <see cref="Biome"/> to search by.</param>
        /// <returns>a <see cref="ResourceDispenser"/> if found, otherwise null</returns>
        public static ResourceDispenser SpawnResourceNodeSnapTerrain(Vector3 scenePos, SceneryTypes type, string biomeName)
        {
            GrabInitList();
            Vector3 pos = scenePos;
            ManWorld.inst.GetTerrainHeight(pos, out pos.y);
            Quaternion flatRot = Quaternion.LookRotation((UnityEngine.Random.rotation * Vector3.forward).SetY(0).normalized, Vector3.up);
            return SpawnResourceNode(pos, flatRot, type, biomeName);
        }
        /// <summary>
        /// Spawn a <see cref="ResourceDispenser"/> Scenery using given data
        /// </summary>
        /// <param name="scenePos">The scene positioning of the spawned resource node.</param>
        /// <param name="rotation">The scene rotation of the spawned resource node.</param>
        /// <param name="type">The <see cref="SceneryTypes"/> to search by.</param>
        /// <param name="biomeName">The name of the <see cref="Biome"/> to search by.</param>
        /// <returns>a <see cref="ResourceDispenser"/> if found, otherwise null</returns>
        public static ResourceDispenser SpawnResourceNode(Vector3 scenePos, Quaternion rotation, SceneryTypes type, string biomeName)
        {
            GrabInitList();
            try
            {
                TerrainObject TO = GetResourceNodePrefab(type, biomeName);
                TrackableObject track = TO.SpawnFromPrefabAndAddToSaveData(scenePos, rotation).TerrainObject;
                ResourceDispenser RD = track.GetComponent<ResourceDispenser>();
                return RD;
            }
            catch (Exception e)
            {
                throw new NullReferenceException("TTExtUtil: SpawnResourceNode encountered an error - " + e.Message, e);
            }
        }
        /// <summary>
        /// Spawn a <see cref="ResourceDispenser"/> Scenery using given data, playing the <c>Regrow()</c> animation
        /// </summary>
        /// <param name="scenePos">The scene positioning of the spawned resource node.</param>
        /// <param name="rotation">The scene rotation of the spawned resource node.</param>
        /// <param name="type">The <see cref="SceneryTypes"/> to search by.</param>
        /// <param name="biomeName">The name of the <see cref="Biome"/> to search by.</param>
        /// <returns>a <see cref="ResourceDispenser"/> if found, otherwise null</returns>
        public static ResourceDispenser SpawnResourceNodeAnimated(Vector3 scenePos, Quaternion rotation, SceneryTypes type, string biomeName)
        {
            GrabInitList();
            try
            {
                TerrainObject TO = GetResourceNodePrefab(type, biomeName);
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
        /// <summary>
        /// Destroy the target <see cref="ResourceDispenser"/> immedeately
        /// </summary>
        /// <param name="resDisp">Target</param>
        /// <param name="impactVec">The angle to simulate that final blow from</param>
        /// <param name="spawnChunks">Spawn the chunks on death</param>
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
        /// <summary>
        /// Get the list of all permissiable <see cref="TerrainSetPiece"/>s in the game
        /// </summary>
        /// <returns>The list</returns>
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
        /// <summary>
        /// Get the list of names of all permissiable <see cref="TerrainSetPiece"/>s in the game
        /// </summary>
        /// <returns>The list of names</returns>
        public static IEnumerable<string> GetAllSetPieceNames()
        {
            return GetAllSetPieces().ConvertAll(x => x.name);
        }
        /// <summary>
        /// Find a <see cref="TerrainSetPiece"/> based on it's name
        /// </summary>
        /// <param name="SetPieceName">Name of the <see cref="TerrainSetPiece"/> to find</param>
        /// <param name="complainWhenFail">Log spam details about how this failed if it fails!</param>
        /// <returns>The <see cref="TerrainSetPiece"/> if found, otherwise null</returns>
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
        /// <summary>
        /// Find multiple <see cref="TerrainSetPiece"/>s based on a list of names
        /// </summary>
        /// <param name="SetPieceNames">Names of the <see cref="TerrainSetPiece"/> to find</param>
        /// <returns>The list of <see cref="TerrainSetPiece"/>s found</returns>
        public static List<TerrainSetPiece> GetSetPieces(HashSet<string> SetPieceNames)
        {
            return GetAllSetPieces().FindAll(x => SetPieceNames.Contains(x.name));
        }
        private static FieldInfo ActiveSetPiecesGet = typeof(ManWorld).GetField("m_SetPiecesPlacement", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo SetPieceContents = typeof(TerrainSetPiece).GetField("m_TerrainObjectsList", BindingFlags.NonPublic | BindingFlags.Instance);
        /// <summary>
        /// Destroys the <see cref="TerrainSetPiece"/> immedeately at location
        /// </summary>
        /// <param name="worldPos">the world position of the <see cref="TerrainSetPiece"/> to remove</param>
        /// <returns>true if removed</returns>
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
        /// <summary>
        /// Spawns the set piece then forcibly reloads the terrain there to get it into the game ASAP
        /// </summary>
        /// <param name="name">Name of the <see cref="TerrainSetPiece"/> to find</param>
        /// <param name="worldPos">the world position of the <see cref="TerrainSetPiece"/> to add</param>
        /// <param name="groundOffset">The offset from the ground the <see cref="TerrainSetPiece"/> should be positioned at</param>
        /// <param name="forwardsSnapped">Force the <see cref="TerrainSetPiece"/> to face world forwards (north)</param>
        /// <returns>The <see cref="TerrainSetPiece"/> if found and spawned, otherwise null</returns>
        public static TerrainSetPiece ForceSpawnSetPiece(string name, WorldPosition worldPos, float groundOffset, Vector3 forwardsSnapped)
        {
            DestroySetPiece(worldPos);
            return SpawnSetPiece(name, worldPos, groundOffset, forwardsSnapped);
        }
        /// <summary>
        /// Spawns the set piece into the game <b>without</b> reloading the <see cref="WorldTile"/>s there, which will not be
        /// seen until the <see cref="WorldTile"/>s are (re)generated by the player manually
        /// </summary>
        /// <param name="name">Name of the <see cref="TerrainSetPiece"/> to find</param>
        /// <param name="worldPos">the world position of the <see cref="TerrainSetPiece"/> to add</param>
        /// <param name="groundOffset">The offset from the ground the <see cref="TerrainSetPiece"/> should be positioned at</param>
        /// <param name="forwardsSnapped">Force the <see cref="TerrainSetPiece"/> to face world forwards (north)</param>
        /// <returns>The <see cref="TerrainSetPiece"/> if found and spawned, otherwise null</returns>
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
                            ManWorldTileExt.HostOnly_ReloadTile(new IntVector2(x, y), false);
                        }
                    }
                    if (TSP.name != name)
                        throw new NullReferenceException("SpawnSetPiece failed to find the CORRECT SetPiece it had spawned!");
                    ManWorldTileExt.RushTileLoading();
                    return TSP;
                }
                throw new NullReferenceException("SpawnSetPiece failed to find the SetPiece it had spawned!");
            }
            finally
            {
                savedCache.Clear();
            }
        }

        /// <summary>
        /// Get all <see cref="TerrainObject"/>s affiliated with the <see cref="TerrainSetPiece"/>.
        /// </summary>
        /// <param name="TSP">The <see cref="TerrainSetPiece"/> to check</param>
        /// <returns>An <see cref="IEnumerable{TerrainObject}"/> of all afilliates</returns>
        /// <exception cref="NullReferenceException">Set Piece does not have any contents</exception>
        public static IEnumerable<TerrainObject> GetContents(TerrainSetPiece TSP)
        {
            List<TerrainSetPiece.TerrainObjectData> obs = (List<TerrainSetPiece.TerrainObjectData>)SetPieceContents.GetValue(TSP);
            if (obs == null)
                throw new NullReferenceException("GetContents could not get contents of the SetPiece!");
            return obs.ConvertAll(x => x.m_TerrainObject);
        }
        /// <summary>
        /// Spawn a <see cref="TerrainObject"/> and add it to save data and the game scene
        /// </summary>
        /// <param name="TO">TerrainObject prefab to spawn from</param>
        /// <param name="scenePos">Scene Position of the spawned <see cref="TerrainObject"/></param>
        /// <param name="rotation">Rotation of the spawned <see cref="TerrainObject"/></param>
        /// <returns>The <see cref="TerrainObject"/> if spawned, otherwise null</returns>
        /// <exception cref="NullReferenceException">this failed somehow</exception>
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
        /// <summary>
        /// Remove the <see cref="TerrainObject"/> from savedata and the game scene
        /// </summary>
        /// <param name="Obj">instance to destroy</param>
        public static void DestroyObject(TerrainObject Obj)
        {
            Obj.Recycle();
        }


        /// <summary>
        /// Get the primary prefab list the game uses to spawn most 
        /// <see cref="ChunkTypes"/> to <see cref="ResourcePickup"/>s.
        /// <para>You can edit this to add/change content in the main game by submitting your edited 
        /// version to <see cref="OverrideResourceChunkPrefabs(ResourceTable.Definition[])"/></para>
        /// </summary>
        /// <returns>The prefab list</returns>
        public static ResourceTable.Definition[] GetResourceChunkPrefabs()
        {
            return ResourceManager.inst.resourceTable.resources;
        }
        /// <summary>
        /// Remplace the game's vanilla resource chunk list and regenerate this.
        /// <para><b>DO NOT DELETE VANILLA CHUNKS</b></para>
        /// </summary>
        /// <returns>The prefab list</returns>
        public static void OverrideResourceChunkPrefabs(ResourceTable.Definition[] toReplaceWith)
        {
            var reGenRes = AccessTools.Method(typeof(ResourceManager), "RebuildResources");
            if (reGenRes == null)
                throw new NullReferenceException("Field \"ResourceManager.RebuildResources\" no longer exists!");
            ResourceManager.inst.resourceTable.resources = toReplaceWith;
            reGenRes.Invoke(ResourceManager.inst, Array.Empty<object>());
        }
        /// <summary>
        /// Called when <see cref="OverrideResourceChunkPrefabs"/> is called to rebuild the resources
        /// </summary>
        public static EventNoParams OnResChunksRebuilt = new EventNoParams();



        // General-Purpose explosion for use beyond vanilla
        /// <summary>
        /// Number of Base Bombs queued in <see cref="SpawnHelper"/>
        /// </summary>
        public static int BombsActive => queuedBombs.Count;

        private const float vertOffsetDist = 250;
        internal static List<QueuedBomb> queuedBombs = new List<QueuedBomb>();
        /// <summary>
        /// Launch a Base Bomb with given stats. 
        /// <para><b>DOES NOT ACCOUNT FOR SAVE AND LOAD</b></para>
        /// </summary>
        /// <param name="scenePos">End target</param>
        /// <param name="forwards">The angle the bomb comes in at</param>
        /// <param name="PostEvent">Called when the bomb hits the ground</param>
        /// <param name="target">The optional target for the bomb to "chase"</param>
        public static void LaunchBaseBomb(Vector3 scenePos, Vector3 forwards, Action<QueuedBomb, Vector3> PostEvent, Visible target = null)
        {
            //  The bomb spawns about 500 meters off the ground, this will have to predict based on that
            DeliveryBombSpawner DBS = ManSpawn.inst.SpawnDeliveryBombNew(scenePos, DeliveryBombSpawner.ImpactMarkerType.Tech);
            DBS.SetSpawnParams(scenePos + (Vector3.up * vertOffsetDist), DeliveryBombSpawner.ImpactMarkerType.Tech);
            QueuedBomb QB = new QueuedBomb(DBS, forwards, PostEvent, target);
            queuedBombs.Add(QB);
        }
        /// <summary>
        /// Data for a Base Bomb already in progress
        /// </summary>
        public struct QueuedBomb
        {
            /// <summary>
            /// The forwards looking direction of the bomb
            /// </summary>
            public Vector3 forwards { get; private set; }
            /// <summary>
            /// What to invoke on ground contact
            /// </summary>
            public Action<QueuedBomb, Vector3> postEvent { get; private set; }
            /// <summary>
            /// The bomb itself
            /// </summary>
            internal KineticDriver bomb { get; private set; }
            /// <summary>
            /// The bomb's chase target
            /// </summary>
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
            private static int curBiome = 0;
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
                    curBiome = GUIVertToolbar(curBiome, BiomesByName.Keys.ToArray());
                    if (GUILayout.Button("Set Biome Based On Own Tech"))
                    {
                        var biomeThis = ManWorld.inst.GetBiomeWeightsAtScenePosition(Singleton.playerTank.boundsCentreWorld).Biome(0);
                        curBiome = BiomesByName.Values.ToList().FindIndex(x => x == biomeThis);
                    }
                    snapTerrain = AltUI.Toggle(snapTerrain, "Snap To Terrain");
                    GUICategoryDisp<SceneryTypes>(ref enabledTabs, "Explosions", x => SpawnResourceNodeExplosion(spawnPos, x, BiomesByName.Keys.ToArray()[curBiome]));
                    GUICategoryDisp<SceneryTypes>(ref enabledTabs, "Nodes", x => SpawnResourceNode(spawnPos, Quaternion.identity, x, BiomesByName.Keys.ToArray()[curBiome])); 
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
                            if (GUILayout.Button(item.NullOrEmpty() ? "<NULL>" : item))
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