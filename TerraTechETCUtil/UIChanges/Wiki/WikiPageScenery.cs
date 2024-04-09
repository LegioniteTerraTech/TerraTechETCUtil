using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TerraTechETCUtil
{
    public class WikiPageScenery : ManIngameWiki.WikiPage
    {
        public static Func<int, string> GetSceneryModName = GetSceneryModNameDefault;
        public int sceneryID;
        public Dictionary<BiomeTypes, List<TerrainObject>> prefabBase => 
            SpawnHelper.GetSceneryByType((SceneryTypes)sceneryID);
        public TerrainObject prefabMain => prefabBase?.Values?.First()?.First();
        public ResourceDispenser prefab => prefabMain?.GetComponent<ResourceDispenser>();
        public string desc = "unset";
        internal SceneryDataInfo mainInfo;
        internal SceneryDataInfo damage;
        internal SceneryDataInfo damageable;
        internal SceneryDataInfo[] modules;
        public WikiPageScenery(int SceneryID) :
            base(GetSceneryModName(SceneryID), StringLookup.GetItemName(ObjectTypes.Scenery, SceneryID),
            null, "Resources", ManIngameWiki.ScenerySprite)
        {
            sceneryID = SceneryID;
            desc = StringLookup.GetItemDescription(ObjectTypes.Scenery, SceneryID);
        }

        public WikiPageScenery(int SceneryID, ManIngameWiki.WikiPageGroup group) : 
            base(GetSceneryModName(SceneryID), StringLookup.GetItemName(ObjectTypes.Scenery, SceneryID),
            null, group)
        {
            sceneryID = SceneryID;
            desc = StringLookup.GetItemDescription(ObjectTypes.Scenery, SceneryID);
        }
        public override void DisplaySidebar() => ButtonGUIDispLateIcon();
        public override void GetIcon()
        {
            if (prefabMain == null)
            {
                icon = UIHelpersExt.NullSprite;
            }
            else
            {
                try
                {
                    TrackableObject track = prefabMain.SpawnFromPrefabAndAddToSaveData(Vector3.zero, Quaternion.identity).TerrainObject;
                    if (track)
                    {
                        track.GetComponent<ResourceDispenser>().Restore(new ResourceDispenser.PersistentState 
                        {
                            health = 1000,
                            regrowDelay = 0,
                            chunksSpawned = 0,
                            resourceReservoirSaveData = new ResourceReservoir.SerialData
                            {
                                numChunksRemaining = 1,
                                totalChunks = 2,
                            },
                            removedFromWorld = false,
                            currentStage = 0,
                        });
                        track.GetComponent<ResourceDispenser>().OnClientUpdateDamageState(0);
                        GetIcon(track.gameObject, () =>
                        {
                            track.GetComponent<ResourceDispenser>().RemoveFromWorld(false, true, true, true);
                        });
                    }
                }
                catch
                {
                    icon = UIHelpersExt.NullSprite;
                }
            }
        }
        public override bool ReleaseAsMuchAsPossible()
        {
            if (mainInfo != null)
                mainInfo = null;
            if (damage != null)
                damage = null;
            if (damageable != null)
                damageable = null;
            if (modules != null)
            {
                modules = null;
                return true;
            }
            return false;
        }
        private static List<SceneryDataInfo> Combiler = new List<SceneryDataInfo>();
        public override void OnBeforeDisplay()
        {
            GetIcon();
            if (icon == null)
                GetIcon();
        }
        public override void DisplayGUI()
        {
            if (modules == null)
            {
                if (prefab != null)
                {
                    try
                    {
                        var modulesC = SceneryDataInfo.TryGetModules(prefab);
                        foreach (var item in modulesC)
                        {
                            if (item.name == "Scenery")
                            {
                                item.infos.Add("Base Name", prefab.name == null ? "<NULL>" : prefab.name);
                                item.infos.Add("Scenery ID", sceneryID);
                                item.infos.Add("Is Vanilla", sceneryID <= Enum.GetValues(typeof(SceneryTypes)).Length);
                                List<ManIngameWiki.WikiLink> links = new List<ManIngameWiki.WikiLink>();
                                foreach (ChunkTypes itemChunk in prefab.AllDispensableItems())
                                {
                                    WikiPageChunk WPC = ManIngameWiki.GetChunkPage(
                                        StringLookup.GetItemName(ObjectTypes.Chunk, (int)itemChunk));
                                    if (WPC != null)
                                        links.Add(new ManIngameWiki.WikiLink(WPC));
                                }
                                item.infos.Add("Yield", links);
                                List<ManIngameWiki.WikiLink> typesLocations = new List<ManIngameWiki.WikiLink>();
                                if (ManIngameWiki.GetPage("Biomes") is ManIngameWiki.WikiPageGroup group)
                                {
                                    foreach (var itemL in prefabBase)
                                    {
                                        foreach (var item2 in group.NestedPages)
                                        {
                                            if (item2 is WikiPageBiome biome && biome.biomeInst.BiomeType == itemL.Key)
                                                typesLocations.Add(new ManIngameWiki.WikiLink(biome));
                                        }
                                    }
                                }
                                item.infos.Add("Biomes", typesLocations);
                                mainInfo = item;
                            }
                            else if (item.name == "Armoring")
                                damageable = item;
                            else if (item.name == "Durability")
                                damage = item;
                            else
                                Combiler.Add(item);
                        }
                        modules = Combiler.ToArray();
                    }
                    finally
                    {
                        Combiler.Clear();
                    }
                }
            }
            GUILayout.BeginHorizontal();
            if (icon != null)
                AltUI.Sprite(icon, AltUI.TextfieldBorderedBlue, GUILayout.Height(128), GUILayout.Width(128));
            GUILayout.BeginVertical(AltUI.TextfieldBordered);
            if (mainInfo != null)
                mainInfo.DisplayGUI();
            if (damage != null)
                damage.DisplayGUI();
            if (damageable != null)
                damageable.DisplayGUI();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            if (desc != null)
                GUILayout.Label(desc, AltUI.TextfieldBlackHuge);

            GUILayout.BeginVertical(AltUI.TextfieldBordered);
            if (modules != null && modules.Any())
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Modules: ", AltUI.LabelBlackTitle);
                GUILayout.Label(modules.Length.ToString(), AltUI.LabelBlackTitle);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                foreach (var item in modules)
                {
                    item.DisplayGUI();
                }
            }
            else
                GUILayout.Label("Modules: None", AltUI.LabelBlackTitle);
            GUILayout.EndVertical();
        }

        internal static string GetSceneryModNameDefault(int chunkType)
        {
            return ManIngameWiki.VanillaGameName;
        }

        internal class SceneryDataInfo : AutoDataExtractor
        {
            private static HashSet<object> grabbed = new HashSet<object>();
            private static List<SceneryDataInfo> cacher = new List<SceneryDataInfo>();
            internal static SceneryDataInfo[] TryGetModules(ResourceDispenser toExplore)
            {
                try
                {
                    foreach (var item in toExplore.GetComponents<MonoBehaviour>())
                    {
                        Type typeCase = item.GetType();
                        if (grabbed.Contains(item))
                            continue;
                        grabbed.Add(item);
                        if (AllowedTypes.Contains(typeCase))
                        {
                            cacher.Add(new SceneryDataInfo(typeCase, item, grabbed));
                        }
                        else if (item is Module)
                        {
                            if (!ignoreTypes.Contains(typeCase))
                                cacher.Add(new SceneryDataInfo(typeCase, item, grabbed));
                        }
                        else if (item is ExtModule)
                        {
                            if (!ignoreTypesExt.Contains(typeCase))
                                cacher.Add(new SceneryDataInfo(typeCase, item, grabbed));
                        }
                    }
                    return cacher.ToArray();
                }
                catch (Exception e)
                {
                    throw new Exception("TryGetModules failed - ", e);
                }
                finally
                {
                    cacher.Clear();
                    grabbed.Clear();
                }
            }
            public SceneryDataInfo(Type grabbedType, object prefab, HashSet<object> grabbedAlready) : base (
                SpecialNames.TryGetValue(grabbedType.Name, out string altName) ? altName : 
                grabbedType.Name.Replace("Module", "").ToString().SplitCamelCase(), 
                grabbedType, prefab, grabbedAlready)
            {
            }
        }
    }
}
