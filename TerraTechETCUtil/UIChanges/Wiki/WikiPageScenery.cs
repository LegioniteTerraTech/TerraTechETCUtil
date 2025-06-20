using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static LocalisationEnums;

namespace TerraTechETCUtil
{
    public class WikiPageScenery : ManIngameWiki.WikiPage
    {
        public static Func<ResourceDispenser, string> GetSceneryData = null;
        public static Func<int, string> GetSceneryModName = GetSceneryModNameDefault;
        public int sceneryID;
        public Dictionary<string, List<TerrainObject>> prefabBase => 
            SpawnHelper.GetSceneryByType((SceneryTypes)sceneryID);
        public TerrainObject prefabMain => prefabBase?.Values?.First()?.First();
        public ResourceDispenser prefab => prefabMain?.GetComponent<ResourceDispenser>();
        public string desc = "unset";
        internal SceneryDataInfo mainInfo;
        internal SceneryDataInfo damage;
        internal SceneryDataInfo damageable;
        internal SceneryDataInfo[] modules;
        public WikiPageScenery(int SceneryID) :
            base(GetSceneryModName(SceneryID), new LocExtStringVanilla(StringLookup.GetItemName(ObjectTypes.Scenery, SceneryID),
                LocalisationEnums.StringBanks.SceneryName, SceneryID),
            null, ManIngameWiki.LOC_Scenery, ManIngameWiki.ScenerySprite)
        {
            sceneryID = SceneryID;
            desc = StringLookup.GetItemDescription(ObjectTypes.Scenery, SceneryID);
        }

        public WikiPageScenery(int SceneryID, ManIngameWiki.WikiPageGroup group) : 
            base(GetSceneryModName(SceneryID), new LocExtStringVanilla(StringLookup.GetItemName(ObjectTypes.Scenery, SceneryID),
                LocalisationEnums.StringBanks.SceneryName, SceneryID),
            null, group)
        {
            sceneryID = SceneryID;
            desc = StringLookup.GetItemDescription(ObjectTypes.Scenery, SceneryID);
        }
        public override void DisplaySidebar() => ButtonGUIDispLateIcon();
        private bool TriedGetIcon = false;
        public override void GetIcon()
        {
            if (TriedGetIcon)
                return;
            TriedGetIcon = true;
            if (prefabMain == null)
            {
                icon = UIHelpersExt.NullSprite;
            }
            else
            {
                try
                {
                    Vector3 pos = Singleton.playerPos + new Vector3(0, 80, 0);
                    //TerrainObject track = prefabMain.SpawnFromPrefab(ManWorld.inst.TileManager.LookupTile(pos),pos, Quaternion.identity).GetComponent<TerrainObject>();
                    TerrainObject track = prefabMain.SpawnFromPrefabAndAddToSaveData(pos, Quaternion.identity).TerrainObject;
                    if (track)
                    {
                        var Resdisp = track.GetComponent<ResourceDispenser>();
                        Resdisp.Restore(new ResourceDispenser.PersistentState 
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
                        Resdisp.OnClientUpdateDamageState(0);
                        track.transform.position = pos;
                        Resdisp.visible.SetCollidersEnabled(true);
                        GetIconImage(track.gameObject, () =>
                        {
                            /*
                            InvokeHelper.Invoke(() => { track.GetComponent<ResourceDispenser>().RemoveFromWorld(false, true, true, true); },
                                0.2f);*/
                            track.GetComponent<ResourceDispenser>().RemoveFromWorld(false, true, true, true);
                        });
                    }
                    else
                        Debug_TTExt.Log("Missing prefab for " + title);
                }
                catch (Exception e)
                {
                    Debug_TTExt.Log("Crash while handling GetIcon() - " + e);
                    icon = UIHelpersExt.NullSprite;
                }
            }
        }
        public override bool OnWikiClosed()
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
            //GetIcon();
            if (icon == null)
                GetIcon();
        }
        protected override void DisplayGUI()
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
                                            if (item2 is WikiPageBiome biome && biome.biomeInst.name == itemL.Key)
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

            if (ManIngameWiki.ShowJSONExport && prefab != null)
            {
                if (AltUI.Button("ENTIRE SCENERY JSON to system clipboard", ManSFX.UISfxType.Craft))
                {
                    if (GetSceneryData == null)
                    {
                        AutoDataExtractor.clipboard.Clear();
                        GameObjectDocumentator.GetStrings(prefab.gameObject, AutoDataExtractor.clipboard, 0, SlashState.None);
                        GUIUtility.systemCopyBuffer = AutoDataExtractor.clipboard.ToString();
                        ManSFX.inst.PlayUISFX(ManSFX.UISfxType.SaveGameOverwrite);
                    }
                    else
                    {
                        AutoDataExtractor.clipboard.Clear();
                        AutoDataExtractor.clipboard.Append(GetSceneryData.Invoke(prefab));
                        GUIUtility.systemCopyBuffer = AutoDataExtractor.clipboard.ToString();
                        ManSFX.inst.PlayUISFX(ManSFX.UISfxType.SaveGameOverwrite);
                    }
                }
                if (GetSceneryData == null)
                    AltUI.Tooltip.GUITooltip("This might not work properly (WIP)");
            }
        }

        public static string GetSceneryModNameDefault(int chunkType)
        {
            return ManIngameWiki.VanillaGameName;
        }

        internal class SceneryDataInfo : AutoDataExtractorInst
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
                        if (ModuleInfo.AllowedTypesUIWiki.Contains(typeCase))
                        {
                            cacher.Add(new SceneryDataInfo(typeCase, item, grabbed));
                        }
                        else if (item is Module)
                        {
                            if (!ignoreModuleTypes.Contains(typeCase))
                                cacher.Add(new SceneryDataInfo(typeCase, item, grabbed));
                        }
                        else if (item is ExtModule)
                        {
                            if (!ignoreModuleTypesExt.Contains(typeCase))
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
            public SceneryDataInfo(Type grabbedType, MonoBehaviour prefab, HashSet<object> grabbedAlready) : base (
                SpecialNames.TryGetValue(grabbedType.Name, out string altName) ? altName : 
                grabbedType.Name.Replace("Module", "").ToString().SplitCamelCase(), 
                grabbedType, prefab, grabbedAlready)
            {
            }
        }
    }
}
