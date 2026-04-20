using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Text;
using UnityEngine;
using static LocalisationEnums;
using static TerraTechETCUtil.ManIngameWiki;

namespace TerraTechETCUtil
{
    /// <inheritdoc cref="ManIngameWiki.WikiPage"/>
    /// <summary>
    /// <para>Wiki page for scenery data information</para>
    /// </summary>
    public class WikiPageScenery : ManIngameWiki.WikiPage<SceneryTypes, TerrainObject>
    {
        /// <summary>
        /// An additional event displayer for mods to add their own wiki data
        /// </summary>
        public static Event<ResourceDispenser> AdditionalDisplayOnUI = new Event<ResourceDispenser>();
        /// <summary>
        /// page, stringSection, searchText - 
        /// Invoke the Action as true if the filter was triggered and true, or false if the filter failed to cancel the search target.
        /// Do not invoke if the filter was not triggered!
        /// <para>Use ManIngameWiki.WikiPage.FilterBlockPass for quick text filtering.  Be sure to end it with '<b>:</b>'!</para>
        /// <para>Also add the annotation to AdditionalSearchFiltersPopup with a newline call</para>
        /// </summary>
        public static Event<WikiPageScenery, string, string, Action<bool>> AdditionalSearchFilters = new Event<WikiPageScenery, string, string, Action<bool>>();
        /// <summary>
        /// Use the stringbuilder given to AppendLine() new search filters for the user
        /// </summary>
        public static Event<StringBuilder> AdditionalSearchFiltersPopup = new Event<StringBuilder>();

        /// <inheritdoc cref="WikiPageBiome.GetBiomeData"/>
        public static Func<ResourceDispenser, string> GetSceneryData = null;
        /// <inheritdoc cref="WikiPageBiome.GetBiomeModName"/>
        public static Func<int, string> GetSceneryModName = GetSceneryModNameDefault;
        /// <summary>
        /// Get the prefab
        /// </summary>
        public Dictionary<string, List<TerrainObject>> prefabBase => 
            SpawnHelper.GetSceneryByType(ID);
        /// <summary>
        /// Get the main prefab component
        /// </summary>
        public TerrainObject prefabMain => prefabBase?.Values?.First()?.First();
        /// <summary>
        /// Get the resource spawner component
        /// </summary>
        public ResourceDispenser prefab => prefabMain?.GetComponent<ResourceDispenser>();

        /// <inheritdoc cref="WikiPageBiome.desc"/>
        public string desc = "unset";
        internal SceneryDataInfo mainInfo;
        internal SceneryDataInfo damage;
        internal SceneryDataInfo damageable;
        internal SceneryDataInfo[] modules;


        /// <inheritdoc />
        public override bool HasInst() => _inst != null;
        /// <inheritdoc />
        protected override void OnBeforeDataRequested(bool getFullData)
        {
            _inst = prefabMain;
            if (getFullData && modules == null)
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
                                bool isVanilla = (int)ID < Enum.GetValues(typeof(SceneryTypes)).Length;
                                item.infos.Add("Is Vanilla", isVanilla);
                                if (isVanilla)
                                    item.infos.Add("Scenery ID (Enum)", ID.ToString());
                                item.infos.Add("Scenery ID (Int)", ((int)ID).ToString());
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
                                            if (item2 is WikiPageBiome biome && biome.inst.name == itemL.Key)
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
        }
        /// <inheritdoc cref="WikiPageInfo.WikiPageInfo(string, LocExtString, Sprite, Action, ManIngameWiki.WikiPageGroup)"/>
        /// <summary>
        /// 
        /// </summary>
        /// <param name="SceneryID">The scenery ID to target</param>
        public WikiPageScenery(int SceneryID) :
            base(GetSceneryModName(SceneryID), (SceneryTypes)SceneryID, GetSceneryName(SceneryID),
            null, ManIngameWiki.LOC_Scenery, ManIngameWiki.ScenerySprite)
        {
            desc = StringLookup.GetItemDescription(ObjectTypes.Scenery, SceneryID);
        }

        /// <inheritdoc cref="WikiPageScenery.WikiPageScenery(int)"/>
        public WikiPageScenery(int SceneryID, ManIngameWiki.WikiPageGroup group) : 
            base(GetSceneryModName(SceneryID), (SceneryTypes)SceneryID, GetSceneryName(SceneryID), null, group)
        {
            desc = StringLookup.GetItemDescription(ObjectTypes.Scenery, SceneryID);
        }
        /// <inheritdoc/>
        protected override WikiPageGroup InsureWikiGroup(string ModID, LocExtString WikiGroupName, Sprite Icon = null) =>
            InsureSceneryWikiGroup(ModID);
        /// <inheritdoc/>
        protected override WikiPageGroup InsureWikiGroup(string ModID, string WikiGroupName, Sprite Icon = null) =>
            InsureSceneryWikiGroup(ModID);
        /// <inheritdoc cref="WikiPageChunk.GetChunkName(int)"/>
        /// <summary>
        /// 
        /// </summary>
        /// <param name="SceneryID"></param>
        /// <returns></returns>
        public static LocExtStringVanillaOT GetSceneryName(int SceneryID)
        {
            return new LocExtStringVanillaOT(ObjectTypes.Scenery, SceneryID);
        }
        /// <inheritdoc/>
        public override void DisplaySidebar() => ButtonGUIDispLateIcon();
        private bool TriedGetIcon = false;
        /// <inheritdoc/>
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

        internal static void ValidScenerySearchQueryPopup()
        {
            try
            {
                AdditionalSearchFiltersPopup.Send(additionalFilters);
                AltUI.Tooltip.GUITooltip("Supports searching in english with no spaces for:\n" +
                    "type:[scenery type name]\n" +
                    "chunk:[resource name(s)]\n" +
                    "module:[scenery module name(s)]\n" +
                    "dmg:[scenery main damageable type]\n" +
                    "hp[g/l]:[max health greater/less than]\n" +
                    additionalFilters.ToString());
            }
            finally
            {
                additionalFilters.Clear();
            }
        }
        internal static bool ValidScenerySearchQueryPost(ManIngameWiki.WikiPage page, string pageName, string searchText)
        {
            if (page is WikiPageScenery WPS)
            {
                ResourceDispenser resdisp = WPS.prefab;
                int ignoreExtra = 0;
                var stringSections = searchText.Split(' ');
                foreach (var stringSection in stringSections)
                {
                    string[] queries = null;
                    if (FilterPass(stringSection, "type:", ref queries))
                    {
                        if (!queries.Any(x => WPS.ID.
                            ToString().ToLower().Contains(x)))
                            return false;
                    }
                    else if (FilterPass(stringSection, "chunk:", ref queries))
                    {
                        try
                        {
                            if (!queries.All(x => resdisp.AllDispensableItems().Any(y =>
                                StringLookup.GetItemName(new ItemTypeInfo(ObjectTypes.Chunk, (int)y)).
                                Replace(" ", string.Empty).ToLower().Contains(x))))
                                return false;
                        }
                        catch
                        {
                            return false;
                        }
                    }
                    else if (FilterPass(stringSection, "module:", ref queries))
                    {
                        if (resdisp != null)
                        {
                            foreach (var x in queries)
                            {
                                if (!resdisp.GetComponents<Module>()?.
                                    FirstOrDefault(y => y.GetType().ToString().ToLower().Contains(x)) &&
                                    !resdisp.GetComponents<ExtModule>()?.
                                    FirstOrDefault(y => y.GetType().ToString().ToLower().Contains(x)))
                                    return false;
                            }
                        }
                        else
                            return false;
                    }
                    else if (FilterPass(stringSection, "dmg:", ref queries))
                    {
                        if (resdisp?.GetComponent<Damageable>() == null ||
                            !queries.Any(x => StringLookup.GetDamageableTypeName(
                                resdisp.GetComponent<Damageable>().
                             DamageableType).Replace(" ", string.Empty).ToLower().Contains(x)))
                            return false;
                    }
                    else if (FilterPass(stringSection, "hpg:", ref queries))
                    {
                        if (!float.TryParse(queries[0], out float num) ||
                            resdisp?.GetComponent<Damageable>() == null ||
                            resdisp.GetComponent<Damageable>().MaxHealth <= num)
                            return false;
                    }
                    else if (FilterPass(stringSection, "hpl:", ref queries))
                    {
                        if (!float.TryParse(queries[0], out float num) ||
                            resdisp?.GetComponent<Damageable>() == null ||
                            resdisp.GetComponent<Damageable>().MaxHealth >= num)
                            return false;
                    }
                    else
                    {
                        bool filteredAtLeastOnce = false;
                        bool cancel = false;
                        AdditionalSearchFilters.Send(WPS, stringSection, searchText, (state) =>
                        {
                            filteredAtLeastOnce = true;
                            if (!state)
                                cancel = true;
                        });
                        if (cancel)
                            return false;
                        if (!filteredAtLeastOnce)
                            break;
                    }
                    ignoreExtra = stringSection.Length + 1;
                }
                if (searchText.Length <= ignoreExtra)
                    return true;
                string leftOverText = searchText.Substring(ignoreExtra);
                if (int.TryParse(leftOverText, out int potID) &&
                    potID == (int)WPS.ID)
                    return true;
                if (Enum.TryParse(leftOverText, out SceneryTypes potIDEnum) &&
                    ((int)potIDEnum) == (int)WPS.ID)
                    return true;
                return pageName.ToLower().Contains(searchText.Substring(ignoreExtra));
            }
            return false;
        }


        /// <inheritdoc/>
        public override bool OnWikiClosedOrDeallocateMemory()
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
        /// <inheritdoc/>
        public override void OnBeforeDisplay()
        {
            //GetIcon();
            if (icon == null)
                GetIcon();
        }
        /// <inheritdoc/>
        protected override void DisplayGUI()
        {
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
                GUILayout.Label(desc, AltUI.TextfieldBlackHuge, GUILayout.ExpandHeight(false));

            if (AdditionalDisplayOnUI.HasSubscribers())
                AdditionalDisplayOnUI.Send(prefab);

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

        /// <inheritdoc cref="WikiPageBiome.GetBiomeModNameDefault(Biome)"/>
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sceneryType">The scenery type type this is for</param>
        /// <returns></returns>
        public static string GetSceneryModNameDefault(int sceneryType)
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
