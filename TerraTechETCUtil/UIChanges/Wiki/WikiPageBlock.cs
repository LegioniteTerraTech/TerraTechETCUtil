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
        public bool publicVisible { get; private set; } = false;
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
            base(GetBlockModName(BlockID), new LocExtStringFunc(StringLookup.GetItemName(ObjectTypes.Block, BlockID), 
                () => { return StringLookup.GetItemName(ObjectTypes.Block, BlockID); }),
            GetSprite(BlockID), ManIngameWiki.LOC_Blocks, ManIngameWiki.BlocksSprite)
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
            publicVisible = StringLookup.GetItemName(ObjectTypes.Block, BlockID) != StringLookup.GetItemName(ObjectTypes.Block, -1);
        }
        public WikiPageBlock(int BlockID, ManIngameWiki.WikiPageGroup group) : 
            base(GetBlockModName(BlockID), new LocExtStringFunc(StringLookup.GetItemName(ObjectTypes.Block, BlockID),
                () => { return StringLookup.GetItemName(ObjectTypes.Block, BlockID); }),
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
            // Catch "This Block No Longer Exists"
            publicVisible = StringLookup.GetItemName(ObjectTypes.Block, BlockID) != StringLookup.GetItemName(ObjectTypes.Block, -1);
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
            {
                BlockTypes BT = (BlockTypes)WPB.blockID;
                return WPB.publicVisible && ManSpawn.inst.IsBlockAllowedInCurrentGameMode(BT) &&
                    ManGameMode.inst.CheckBlockAllowed(BT) &&
                    !ManSpawn.inst.IsBlockUsageRestrictedInGameMode(BT);
            }
            return true;
        }

        public override void DisplaySidebar() => ButtonGUIDispLateIcon();
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
                    var modulesC = ModuleInfo.TryGetModules(inst.gameObject, ModuleInfo.AllowedTypesUIWiki);
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
        private static bool AttributesShown = false;
        protected override void DisplayGUI()
        {
            if (modules == null)
                OnFirstInitGUI();
            GUILayout.BeginHorizontal();
            AltUI.Sprite(icon, AltUI.TextfieldBorderedBlue, GUILayout.Height(128), GUILayout.Width(128));
            GUILayout.BeginVertical(AltUI.TextfieldBordered);

            GUILayout.BeginVertical(AltUI.BoxBlack);
            ManIngameWiki.WikiLink link = new ManIngameWiki.WikiLink(ManIngameWiki.GetCorpPage(ManSpawn.inst.GetCorporation((BlockTypes)blockID)));
            if (link.OnGUILarge(AltUI.ButtonBlue, AltUI.LabelWhite))
                link.linked.GoHere();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localisation.inst.GetLocalisedString(LocalisationEnums.Purchasing.BlockGradeTitle), AltUI.LabelWhiteTitle);
            GUILayout.Label(": ", AltUI.LabelWhiteTitle);
            GUILayout.FlexibleSpace();
            GUILayout.Label(StringLookup.GetBlockTierName((BlockTypes)blockID, false), AltUI.LabelGoldTitle);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localisation.inst.GetLocalisedString(LocalisationEnums.Purchasing.BlockCategoryTitle), AltUI.LabelWhite);
            GUILayout.Label(": ", AltUI.LabelWhite);
            GUILayout.FlexibleSpace();
            new ManIngameWiki.WikiIconInfo(ManUI.inst.GetBlockCatIcon(ManSpawn.inst.GetCategory((BlockTypes)blockID)),
                StringLookup.GetBlockCategoryName(ManSpawn.inst.GetCategory((BlockTypes)blockID))).
                OnGUILarge(AltUI.TextfieldBorderedBlue, AltUI.LabelWhite);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localisation.inst.GetLocalisedString(LocalisationEnums.Purchasing.BlockRarityTitle), AltUI.LabelWhite);
            GUILayout.Label(": ", AltUI.LabelWhite);
            GUILayout.FlexibleSpace();
            new ManIngameWiki.WikiIconInfo(ManUI.inst.GetBlockRarityIcon(ManSpawn.inst.GetRarity((BlockTypes)blockID)), 
                StringLookup.GetBlockRarityName(ManSpawn.inst.GetRarity((BlockTypes)blockID))).
                OnGUILarge(AltUI.TextfieldBorderedBlue, AltUI.LabelWhite);
            GUILayout.EndHorizontal();
            var BA = ManSpawn.inst.GetBlockAttributes((BlockTypes)blockID);
            if (BA != null && BA.Any())
            {
                if (AttributesShown)
                {
                    GUILayout.BeginVertical(AltUI.BoxBlack);
                    if (GUILayout.Button("Attributes: Shown"))
                        AttributesShown = !AttributesShown;
                    foreach (var item in BA)
                    {
                        new ManIngameWiki.WikiIconInfo(ManUI.inst.GetBlockAttributeIcon(item), StringLookup.GetBlockAttribute(item)).
                            OnGUILarge(AltUI.TextfieldBorderedBlue, AltUI.LabelWhite);
                    }
                    GUILayout.EndVertical();
                }
                else
                {
                    if (GUILayout.Button("Attributes: Hidden"))
                        AttributesShown = !AttributesShown;
                }
            }
            GUILayout.EndVertical();

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
            if (ManIngameWiki.ShowJSONExport)
            {
                if (AltUI.Button("ENTIRE BLOCK JSON to system clipboard", ManSFX.UISfxType.Craft))
                {
                    TankBlock TB = ManSpawn.inst.GetBlockPrefab((BlockTypes)blockID);
                    AutoDataExtractor.clipboard.Clear();
                    GameObjectDocumentator.GetStrings(TB.gameObject, AutoDataExtractor.clipboard, 0, SlashState.None);
                    GUIUtility.systemCopyBuffer = AutoDataExtractor.clipboard.ToString();
                    ManSFX.inst.PlayUISFX(ManSFX.UISfxType.SaveGameOverwrite);
                }
            }
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


    }
}
