using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;

namespace TerraTechETCUtil
{
    public class WikiPageChunk : ManIngameWiki.WikiPage
    {
        public static Func<int, string> GetChunkModName = GetChunkModNameDefault;
        public int chunkID;
        public string desc = "unset";
        internal ChunkDataInfo mainInfo;
        internal ChunkDataInfo damage;
        internal ChunkDataInfo damageable;
        internal ChunkDataInfo[] modules;
        public WikiPageChunk(int ChunkID) :
            base(GetChunkModName(ChunkID), StringLookup.GetItemName(ObjectTypes.Chunk, ChunkID),
            ManUI.inst.GetSprite(ObjectTypes.Chunk, ChunkID), "Chunks", ManIngameWiki.ChunksSprite)
        {
            chunkID = ChunkID;
            desc = StringLookup.GetItemDescription(ObjectTypes.Chunk, ChunkID);
        }

        public WikiPageChunk(int ChunkID, ManIngameWiki.WikiPageGroup group) : 
            base(GetChunkModName(ChunkID), StringLookup.GetItemName(ObjectTypes.Chunk, ChunkID),
            ManUI.inst.GetSprite(ObjectTypes.Chunk, ChunkID), group)
        {
            chunkID = ChunkID;
            desc = StringLookup.GetItemDescription(ObjectTypes.Chunk, ChunkID);
        }
        public override void DisplaySidebar() => ButtonGUIDisp();
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
        private static List<ChunkDataInfo> Combiler = new List<ChunkDataInfo>();
        public override void GetIcon() { }
        public override void OnBeforeDisplay()
        {
            desc = StringLookup.GetItemDescription(ObjectTypes.Chunk, chunkID);
        }
        public override void DisplayGUI()
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
                                    item.infos.Add("Type", "Component");
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
        }

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
                        if (AllowedTypes.Contains(typeCase))
                        {
                            cacher.Add(new ChunkDataInfo(typeCase, item, grabbed));
                        }
                        else if (item is Module)
                        {
                            if (!ignoreTypes.Contains(typeCase))
                                cacher.Add(new ChunkDataInfo(typeCase, item, grabbed));
                        }
                        else if (item is ExtModule)
                        {
                            if (!ignoreTypesExt.Contains(typeCase))
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
