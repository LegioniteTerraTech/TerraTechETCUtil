using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using static TerraTechETCUtil.ManIngameWiki;

namespace TerraTechETCUtil
{
    /// <inheritdoc cref="ManIngameWiki.WikiPage"/>
    /// <summary>
    /// <para>Wiki page for chunk information</para>
    /// </summary>
    public class WikiPageChunk : ManIngameWiki.WikiPage
    {
        /// <summary>
        /// An additional event displayer for mods to add their own wiki data
        /// </summary>
        public static Event<ChunkTypes> AdditionalDisplayOnUI = new Event<ChunkTypes>();
        /// <summary>
        /// page, stringSection, searchText - 
        /// Invoke the Action as true if the filter was triggered and true, or false if the filter failed to cancel the search target.
        /// Do not invoke if the filter was not triggered!
        /// <para>Use ManIngameWiki.WikiPage.FilterBlockPass for quick text filtering.  Be sure to end it with '<b>:</b>'!</para>
        /// <para>Also add the annotation to AdditionalSearchFiltersPopup with a newline call</para>
        /// </summary>
        public static Event<WikiPageChunk, string, string, Action<bool>> AdditionalSearchFilters = 
            new Event<WikiPageChunk, string, string, Action<bool>>();
        /// <summary>
        /// Use the stringbuilder given to AppendLine() new search filters for the user
        /// </summary>
        public static Event<StringBuilder> AdditionalSearchFiltersPopup = new Event<StringBuilder>();

        /// <inheritdoc cref="WikiPageBiome.GetBiomeData"/>
        public static Func<ChunkTypes, string> GetChunkData = null;
        /// <inheritdoc cref="WikiPageBiome.GetBiomeModName"/>
        public static Func<int, string> GetChunkModName = GetChunkModNameDefault;
        /// <inheritdoc cref="WikiPageBiome.biomeInst"/>
        public int chunkID;
        /// <inheritdoc cref="WikiPageBiome.desc"/>
        public string desc = "unset";
        internal ChunkDataInfo mainInfo;
        internal ChunkDataInfo damage;
        internal ChunkDataInfo damageable;
        internal ChunkDataInfo[] modules;

        /// <inheritdoc cref="ManIngameWiki.WikiPage.WikiPage(string, LocExtString, Sprite, ManIngameWiki.WikiPageGroup)"/>
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ChunkID">The chunk ID (<see cref="ChunkTypes"/>) to affiliate with this page</param>
        public WikiPageChunk(int ChunkID) :
            base(GetChunkModName(ChunkID), GetChunkName(ChunkID),
            ManUI.inst.GetSprite(ObjectTypes.Chunk, ChunkID), ManIngameWiki.LOC_Chunks, ManIngameWiki.ChunksSprite)
        {
            chunkID = ChunkID;
            desc = StringLookup.GetItemDescription(ObjectTypes.Chunk, ChunkID);
        }

        /// <inheritdoc cref=" WikiPageChunk.WikiPageChunk(int)"/>
        public WikiPageChunk(int ChunkID, ManIngameWiki.WikiPageGroup group) : 
            base(GetChunkModName(ChunkID), GetChunkName(ChunkID),
            ManUI.inst.GetSprite(ObjectTypes.Chunk, ChunkID), group)
        {
            chunkID = ChunkID;
            desc = StringLookup.GetItemDescription(ObjectTypes.Chunk, ChunkID);
        }
        /// <inheritdoc/>
        protected override WikiPageGroup InsureWikiGroup(string ModID, LocExtString WikiGroupName, Sprite Icon = null) =>
            InsureChunksWikiGroup(ModID);
        /// <inheritdoc/>
        protected override WikiPageGroup InsureWikiGroup(string ModID, string WikiGroupName, Sprite Icon = null) =>
            InsureChunksWikiGroup(ModID);
        /// <summary>
        /// Get the localised name from the ID
        /// </summary>
        /// <param name="ChunkID"></param>
        /// <returns></returns>
        public static LocExtStringVanillaOT GetChunkName(int ChunkID)
        {
            return new LocExtStringVanillaOT(ObjectTypes.Chunk, ChunkID);
        }
        /// <inheritdoc/>
        public override void DisplaySidebar() => ButtonGUIDisp();
        /// <inheritdoc/>
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
        private static List<ChunkDataInfo> Combiler = new List<ChunkDataInfo>();
        /// <inheritdoc/>
        public override void GetIcon() { }

        internal static void ValidChunkSearchQueryPopup()
        {
            try
            {
                AdditionalSearchFiltersPopup.Send(additionalFilters);
                AltUI.Tooltip.GUITooltip("Supports searching in english with no spaces for:\n" +
                    "type:[chunk type name]\n" +
                    "raw:[the name of the raw version]\n" +
                    "module:[chunk module name(s)]\n" +
                    "dmg:[chunk main damageable type]\n" +
                    "hp[g/l]:[max health greater/less than]\n" +
                    additionalFilters.ToString());
            }
            finally
            {
                additionalFilters.Clear();
            }
        }
        internal static bool ValidChunkSearchQueryPost(ManIngameWiki.WikiPage page, string pageName, string searchText)
        {
            if (page is WikiPageChunk WPC)
            {
                ChunkTypes CT = (ChunkTypes)WPC.chunkID;
                int ignoreExtra = 0;
                var stringSections = searchText.Split(' ');
                foreach (var stringSection in stringSections)
                {
                    string[] queries = null;
                    if (FilterPass(stringSection, "type:", ref queries))
                    {
                        if (!queries.Any(x => CT.ToString().ToLower().Contains(x)))
                            return false;
                    }
                    else if (FilterPass(stringSection, "raw:", ref queries))
                    {
                        try
                        {
                            if (!queries.Any(x => StringLookup.GetItemName(new ItemTypeInfo(ObjectTypes.Chunk, 
                                (int)ResourceManager.inst.GetRawResource(CT))).ToLower().Contains(x)))
                                return false;
                        }
                        catch
                        {
                            return false;
                        }
                    }
                    else if (FilterPass(stringSection, "module:", ref queries))
                    {
                        var chunkInst = ResourceManager.inst.resourceTable?.resources?.
                            FirstOrDefault(x => x.m_ChunkType == CT)?.basePrefab?.GetComponent<ResourcePickup>();
                        if (chunkInst != null)
                        {
                            if (!queries.All(x => chunkInst.GetComponents<Component>().
                                Any(y => y.GetType().ToString().ToLower().Contains(x))))
                                return false;
                        }
                        else
                            return false;
                    }
                    else if (FilterPass(stringSection, "dmg:", ref queries))
                    {
                        var chunkInst = ResourceManager.inst.resourceTable?.resources?.
                            FirstOrDefault(x => x.m_ChunkType == CT)?.basePrefab?.GetComponent<ResourcePickup>();
                        if (chunkInst != null)
                        {
                            if (chunkInst?.GetComponent<Damageable>() == null ||
                                !queries.Any(x => StringLookup.GetDamageableTypeName(
                                chunkInst.GetComponent<Damageable>().
                                DamageableType).Replace(" ", string.Empty).ToLower().Contains(x)))
                                return false;
                        }
                        else
                            return false;
                    }
                    else if (FilterPass(stringSection, "hpg:", ref queries))
                    {
                        var chunkInst = ResourceManager.inst.resourceTable?.resources?.
                            FirstOrDefault(x => x.m_ChunkType == CT)?.basePrefab?.GetComponent<ResourcePickup>();
                        if (chunkInst != null)
                        {
                            if (!float.TryParse(queries[0], out float num) ||
                                chunkInst?.GetComponent<Damageable>() == null ||
                                chunkInst.GetComponent<Damageable>().MaxHealth <= num)
                                return false;
                        }
                        else
                            return false;
                    }
                    else if (FilterPass(stringSection, "hpl:", ref queries))
                    {
                        var chunkInst = ResourceManager.inst.resourceTable?.resources?.
                            FirstOrDefault(x => x.m_ChunkType == CT)?.basePrefab?.GetComponent<ResourcePickup>();
                        if (chunkInst != null)
                        {
                            if (!float.TryParse(queries[0], out float num) ||
                                chunkInst?.GetComponent<Damageable>() == null ||
                                chunkInst.GetComponent<Damageable>().MaxHealth >= num)
                                return false;
                        }
                        else
                            return false;
                    }
                    else
                    {
                        bool filteredAtLeastOnce = false;
                        bool cancel = false;
                        AdditionalSearchFilters.Send(WPC, stringSection, searchText, (state) =>
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
                    potID == WPC.chunkID)
                    return true;
                if (Enum.TryParse(leftOverText, out ChunkTypes potIDEnum) &&
                    ((int)potIDEnum) == WPC.chunkID)
                    return true;
                return pageName.ToLower().Contains(searchText.Substring(ignoreExtra));
            }
            return false;
        }


        /// <inheritdoc/>
        public override void OnBeforeDisplay()
        {
            desc = StringLookup.GetItemDescription(ObjectTypes.Chunk, chunkID);
        }
        /// <inheritdoc/>
        protected override void DisplayGUI()
        {
            if (modules == null)
            {
                ChunkTypes CT = (ChunkTypes)chunkID;
                var prefabBase = ResourceManager.inst.GetResourceDef(CT);
                if (prefabBase != null && prefabBase.basePrefab != null)
                {
                    ResourcePickup prefab = prefabBase.basePrefab.GetComponent<ResourcePickup>();
                    try
                    {
                        var modulesC = ChunkDataInfo.TryGetModules(prefab);
                        foreach (var item in modulesC)
                        {
                            if (item.name == "Chunk")
                            {
                                item.infos.Add("Base Name", prefab.name);
                                item.infos.Add("Chunk ID", chunkID);
                                item.infos.Add("Is Vanilla", chunkID <= Enum.GetValues(typeof(ChunkTypes)).Length);
                                item.infos.Add("Cost", RecipeManager.inst.GetChunkPrice(CT));

                                bool GetFromBlock = false;

                                List<ManIngameWiki.WikiLink> links = new List<ManIngameWiki.WikiLink>();
                                int chunkTypeFlags = ManSpawn.inst.VisibleTypeInfo.GetDescriptorFlags<ChunkCategory>(
                                    ItemTypeInfo.GetHashCode(ObjectTypes.Chunk, chunkID));
                                if ((chunkTypeFlags & (int)ChunkCategory.Refined) != 0)
                                {
                                    item.infos.Add("Type", "Refined");
                                    WikiPageChunk WPC = ManIngameWiki.GetChunkPage(
                                        StringLookup.GetItemName(ObjectTypes.Chunk, (int)ResourceManager.inst.GetRawResource(CT)));
                                    if (WPC != null)
                                        links.Add(new ManIngameWiki.WikiLink(WPC));
                                }
                                else if ((chunkTypeFlags & (int)ChunkCategory.Component) != 0)
                                {
                                    GetFromBlock = true;
                                    item.infos.Add("Type", "Component");
                                }
                                else if ((chunkTypeFlags & (int)ChunkCategory.Raw) != 0)
                                    item.infos.Add("Type", "Raw");
                                else
                                    item.infos.Add("Type", "Unknown");
                                item.infos.Add("Is Burnable", (chunkTypeFlags & (int)ChunkCategory.Fuel) != 0);

                                foreach (var item2 in IterateForInfo<WikiPageScenery>())
                                {
                                    var prefab2 = item2.prefab;
                                    if (prefab2 && prefab2.AllDispensableItems() != null &&
                                        prefab2.AllDispensableItems().Contains(CT))
                                    {
                                        links.Add(new ManIngameWiki.WikiLink(ManIngameWiki.GetPage(
                                            StringLookup.GetItemName(ObjectTypes.Scenery, item2.sceneryID))));
                                    }
                                }
                                if (GetFromBlock)
                                {
                                    foreach (var item2 in IterateForInfo<WikiPageBlock>())
                                    {
                                        var prefab2 = item2.inst;
                                        if (prefab2 != null && prefab2.GetComponent<ModuleRecipeProvider>() != null)
                                        {
                                            bool gotOne = false;
                                            foreach (var recipieList in prefab2.GetComponent<ModuleRecipeProvider>())
                                            {
                                                if (recipieList != null)
                                                {
                                                    foreach (var recipie in recipieList)
                                                    {
                                                        if (recipie?.m_OutputItems != null && recipie.m_OutputItems.Any(x => x?.m_Item != null &&
                                                            x.m_Item.ObjectType == ObjectTypes.Chunk && x.m_Item.ItemType == chunkID))
                                                        {
                                                            gotOne = true;
                                                            break;
                                                        }
                                                    }
                                                }
                                                if (gotOne)
                                                    break;
                                            }
                                            if (gotOne)
                                            {
                                                links.Add(new ManIngameWiki.WikiLink(ManIngameWiki.GetPage(
                                                    StringLookup.GetItemName(ObjectTypes.Block, item2.blockID))));
                                            }
                                        }
                                    }
                                }

                                item.infos.Add("Sources", links);

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
            AltUI.Sprite(icon, AltUI.TextfieldBorderedBlue, GUILayout.Height(128), GUILayout.Width(128));
            GUILayout.BeginVertical(AltUI.TextfieldBordered);
            if (AdditionalDisplayOnUI.HasSubscribers())
                AdditionalDisplayOnUI.Send((ChunkTypes)chunkID);
            if (mainInfo != null)
                mainInfo.DisplayGUI();
            if (damage != null)
                damage.DisplayGUI();
            if (damageable != null)
                damageable.DisplayGUI();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

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
            if (ManIngameWiki.ShowJSONExport && GetChunkData != null)
            {
                if (AltUI.Button("ENTIRE CHUNK JSON to system clipboard", ManSFX.UISfxType.Craft))
                {
                    AutoDataExtractor.clipboard.Clear();
                    AutoDataExtractor.clipboard.Append(GetChunkData.Invoke((ChunkTypes)chunkID));
                    GUIUtility.systemCopyBuffer = AutoDataExtractor.clipboard.ToString();
                    ManSFX.inst.PlayUISFX(ManSFX.UISfxType.SaveGameOverwrite);
                }
            }
        }

        /// <inheritdoc cref="WikiPageBiome.GetBiomeModNameDefault(Biome)"/>
        /// <summary>
        /// 
        /// </summary>
        /// <param name="chunkType">The chunk type this is for</param>
        /// <returns></returns>
        public static string GetChunkModNameDefault(int chunkType)
        {
            return ManIngameWiki.VanillaGameName;
        }

        internal class ChunkDataInfo : AutoDataExtractor
        {
            private static HashSet<object> grabbed = new HashSet<object>();
            private static List<ChunkDataInfo> cacher = new List<ChunkDataInfo>();
            internal static ChunkDataInfo[] TryGetModules(ResourcePickup toExplore)
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
                            cacher.Add(new ChunkDataInfo(typeCase, item, grabbed));
                        }
                        else if (item is Module)
                        {
                            if (!ignoreModuleTypes.Contains(typeCase))
                                cacher.Add(new ChunkDataInfo(typeCase, item, grabbed));
                        }
                        else if (item is ExtModule)
                        {
                            if (!ignoreModuleTypesExt.Contains(typeCase))
                                cacher.Add(new ChunkDataInfo(typeCase, item, grabbed));
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
            public ChunkDataInfo(Type grabbedType, object prefab, HashSet<object> grabbedAlready) : base (
                SpecialNames.TryGetValue(grabbedType.Name, out string altName) ? altName : 
                grabbedType.Name.Replace("Module", "").ToString().SplitCamelCase(), 
                grabbedType, prefab, grabbedAlready)
            {
            }
        }
    }
}
