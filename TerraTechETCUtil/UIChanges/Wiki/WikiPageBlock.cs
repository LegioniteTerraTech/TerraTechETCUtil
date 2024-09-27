using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;

namespace TerraTechETCUtil
{
    public class WikiPageBlock : ManIngameWiki.WikiPage
    {
        //private static List<WikiPageBlock> IconsPending = null;

        public int blockID;
        public string desc = "unset";
        public TankBlock inst;
        internal ModuleInfo mainInfo;
        internal ModuleInfo damage;
        internal ModuleInfo damageable;
        internal ModuleInfo[] modules;
        private static Sprite GetSprite(int itemType)
        {
            Sprite sprite = ManUI.inst.GetSprite(ObjectTypes.Block, itemType);
            if (sprite == UIHelpersExt.NullSprite)
                return null;
            return sprite;
        }
        public WikiPageBlock(int BlockID) :
            base(GetBlockModName(BlockID), StringLookup.GetItemName(ObjectTypes.Block, BlockID),
            GetSprite(BlockID), "Blocks", ManIngameWiki.BlocksSprite)
        {
            blockID = BlockID;
            desc = StringLookup.GetItemDescription(ObjectTypes.Block, BlockID);
            string titleRaw = ManMods.inst.GetModNameForBlockID((BlockTypes)BlockID);
            if (!titleRaw.Equals("Unknown Mod"))
            {
                /*
                if (IconsPending == null)
                    IconsPending = new List<WikiPageBlock>();
                IconsPending.Add(this);
                */
            }
        }
        public WikiPageBlock(int BlockID, ManIngameWiki.WikiPageGroup group) : 
            base(GetBlockModName(BlockID), StringLookup.GetItemName(ObjectTypes.Block, BlockID),
            GetSprite(BlockID), group)
        {
            blockID = BlockID;
            desc = StringLookup.GetItemDescription(ObjectTypes.Block, BlockID);
            string titleRaw = ManMods.inst.GetModNameForBlockID((BlockTypes)BlockID);
            if (!titleRaw.Equals("Unknown Mod"))
            {
                /*
                if (IconsPending == null)
                    IconsPending = new List<WikiPageBlock>();
                IconsPending.Add(this);
                */
            }
        }
        public override void GetIcon()
        {
            icon = ManUI.inst.GetSprite(ObjectTypes.Block, blockID);
            /*
            if (IconsPending == null)
                return;
            foreach (var item in IconsPending)
            {
                item.icon = ManUI.inst.GetSprite(ObjectTypes.Block, item.blockID);
            }
            */
        }

        public static bool ValidBlockSearchQuery(ManIngameWiki.WikiPage page)
        {
            if (page is WikiPageBlock WPB)
                return ManSpawn.inst.IsBlockAllowedInCurrentGameMode((BlockTypes)WPB.blockID) &&
                    ManGameMode.inst.CheckBlockAllowed((BlockTypes)WPB.blockID);
            return true;
        }

        public override void DisplaySidebar() => ButtonGUIDispLateIcon();
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
        private static List<ModuleInfo> Combiler = new List<ModuleInfo>();
        public override void OnBeforeDisplay()
        {
            if (ManMods.inst.IsModdedBlock((BlockTypes)blockID))
                icon = ManUI.inst.GetSprite(ObjectTypes.Block, blockID);
        }
        private void OnFirstInitGUI()
        {
            BlockTypes BT = (BlockTypes)blockID;
            var inst = ManSpawn.inst.GetBlockPrefab(BT);
            if (inst)
            {
                try
                {
                    var modulesC = ModuleInfo.TryGetModules(inst);
                    foreach (var item in modulesC)
                    {
                        if (item.name == "General")
                        {
                            item.infos.Add("Base Name", inst.name);
                            item.infos.Add("Block ID", blockID);
                            item.infos.Add("Is Vanilla", blockID <= Enum.GetValues(typeof(BlockTypes)).Length);
                            item.infos.Add("Cost", RecipeManager.inst.GetBlockBuyPrice(BT));
                            int chunkCount = 0;
                            List<KeyValuePair<int, ManIngameWiki.WikiLink>> links =
                                new List<KeyValuePair<int, ManIngameWiki.WikiLink>>();
                            RecipeManager.inst.recipeTable.m_RecipeLists.Find(x =>
                            x.m_Recipes.Exists(y =>
                            {
                                if (y.m_OutputType == RecipeTable.Recipe.OutputType.Items &&
                                    y.m_OutputItems.Any(z =>
                                        z.m_Item.ObjectType == ObjectTypes.Block &&
                                        z.m_Item.ItemType == blockID
                                    ))
                                {
                                    foreach (var item2 in y.m_InputItems)
                                    {
                                        chunkCount += item2.m_Quantity;
                                        links.Add(new KeyValuePair<int, ManIngameWiki.WikiLink>(item2.m_Quantity,
                                        new ManIngameWiki.WikiLink(ManIngameWiki.GetPage(
                                            StringLookup.GetItemName(item2.m_Item.ObjectType, item2.m_Item.ItemType)))));
                                    }
                                    return true;
                                }
                                return false;
                            }));
                            item.infos.Add("Ingredient Count", chunkCount);
                            item.infos.Add("Ingredients", links);
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
        public override void DisplayGUI()
        {
            if (modules == null)
                OnFirstInitGUI();
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

            if (ActiveGameInterop.IsReady && GUILayout.Button("Try Export GameObject hierarchy"))
                ActiveGameInterop.TransmitBlock(ManSpawn.inst.GetBlockPrefab((BlockTypes)blockID));

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

        internal static string GetBlockModName(int blockType)
        {
            string titleRaw = ManMods.inst.GetModNameForBlockID((BlockTypes)blockType);
            if (titleRaw.Equals("Unknown Mod"))
                return ManIngameWiki.VanillaGameName;
            else if (ResourcesHelper.TryGetModContainer(titleRaw, out ModContainer MC))
            {
                return MC.ModID;
            }
            return titleRaw;
        }


        internal class ModuleInfo : AutoDataExtractorInst
        {
            private static HashSet<object> grabbed = new HashSet<object>();
            private static List<ModuleInfo> cacher = new List<ModuleInfo>();
            internal static ModuleInfo[] TryGetModules(TankBlock toExplore)
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
                            cacher.Add(new ModuleInfo(typeCase, item, grabbed));
                        }
                        else if (item is Module)
                        {
                            if (!ignoreTypes.Contains(typeCase))
                                cacher.Add(new ModuleInfo(typeCase, item, grabbed));
                        }
                        else if (item is ExtModule)
                        {
                            if (!ignoreTypesExt.Contains(typeCase))
                                cacher.Add(new ModuleInfo(typeCase, item, grabbed));
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

            public ModuleInfo(Type grabbedType, MonoBehaviour prefab, HashSet<object> grabbedAlready) : base (
                SpecialNames.TryGetValue(grabbedType.Name, out string altName) ? altName : 
                grabbedType.Name.Replace("Module", "").ToString().SplitCamelCase(), 
                grabbedType, prefab, grabbedAlready)
            {
            }
        }
    }
}
