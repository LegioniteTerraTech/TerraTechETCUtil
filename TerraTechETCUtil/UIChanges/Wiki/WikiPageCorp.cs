using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;

namespace TerraTechETCUtil
{
    public class WikiPageCorp : ManIngameWiki.WikiPage
    {
        public static Func<FactionSubTypes, string> GetCorpData = null;
        public static Func<FactionSubTypes, string> GetCorpDescription = GetDescriptionDefault;
        public static Event<WikiPageCorp> OnWikiPageMade = new Event<WikiPageCorp>();
        public static string GetDescriptionDefault(FactionSubTypes faction)
        {
            switch (faction)
            {
                case FactionSubTypes.NULL:
                    return "The most powerful force in the universe." +
                        "\n\nLittle is known about the impervious Nullspace Armada other than" +
                        " them being responsible for sudden, and quite strange Tech disappearances." +
                        "\n\nYou might have encountered black dots trailing along the screen.  That is one " +
                        "of their scouts, the " + AltUI.ObjectiveString("Void Snek");
                case FactionSubTypes.GSO:
                    return
                        "A dusty old department that lost sight of their goal and gave up long ago." +
                        "\n\nA tired eastern-bloc-style government run organization, " +
                        "full to the brim with bureaucracy and health and safety paranoia. " +
                        "\nBudgets are stretched thin. Equipment is old, battered hand-me-downs" +
                        " that have been patched and chipped so many times that you can’t really " +
                        "tell what they must have looked like originally. \nWeapons are retro-fitted," +
                        " low grade rubbish that is clearly ill suited to the job at hand.";
                case FactionSubTypes.GC:
                    return "Bigger is Better. It’s tool time." +
                        "\n\nMining and construction specialists packing the biggest, heaviest power tools. " +
                        "\nThese hard-hat wearing, utility belt men like machines that pack a punch. " +
                        "\nThe bigger the better. They aren’t interested in weapons; jack hammers and" +
                        " industrial grade drills are what gets these guys going.";
                case FactionSubTypes.EXP:
                    return "Evil geniuses who’ve found the keys to the toy box." +
                        "\n\nMad professors running wild making crazy weaponry. The vehicles are standard " +
                        "modern day fair, but the weapons mounted on to them are turbocharged and out " +
                        "of this world. \nThese bizarre weapons give little indication of their actual " +
                        "function and they are just as likely to work as to blow their operator to pieces.";
                case FactionSubTypes.VEN:
                    return
                        "Adrenaline junkies looking for the next big wave to ride." +
                        "\n\nA charming, charismatic extreme sports champion and businessman starts a " +
                        "new company selling a grand adventure to any and all who are brave enough to " +
                        "take up the challenge. \nTricked out jet buggies, coated in bright decals, " +
                        "sponsorship stickers and caked in mud, blast across the landscape looking for " +
                        "thrills and riches in an unexplored world.";
                case FactionSubTypes.HE:
                    return "Ex-military meat-heads form a private military company." +
                        "\n\nSpecial forces mercenaries for hire who work for the richest people oppress " +
                        "the poorest. \n\nThese cold hard killers tout high-tech gear that would be way " +
                        "beyond the reach of the average grunt’s pay packet. We supply the tools for " +
                        "badass black-ops guys who are here to get the job done, and get back in time for chow.";
                case FactionSubTypes.SPE:
                    return "Some magical, mystical doodads from across the cosmos." +
                        "\n\nYou'll find many of these itching a familiar spot, or " +
                        "scattered about on Techs for greebles.";
                case FactionSubTypes.BF:
                    return "The next generation of hype." +
                        "\n\nDesign geeks creating ultra sleek, minimalist, desirable objects have turned " +
                        "their styluses to developing a range of green, carbon negative, enviro harvesters. " +
                        "\nThese secretive hype-teases are selling us the opportunity to prospect the new world " +
                        "in style, for a luxury fee. They’re changing everything, yet again!";
                case FactionSubTypes.SJ:
                    return "Space hermits riding mad contraptions." +
                        "\n\nThe scrap heap heroes of the off world mining circuit. " +
                        "These are survivalist sci-fi mountain men who are battle hardened " +
                        "and scarred from years of living off the land in the prospectors frontier. " +
                        "They are reactionary guys who are sick of having their hard fought spoils " +
                        "conned from them by the bigger Corps. Their machines are hand made patch " +
                        "works of old salvaged wrecks, covered over with war paint.";
                default:
                    return "Unknown.\n  Engage at your own risk.";
            }
        }


        public int corpID;
        private ModdedCorpDefinition MCD;
        private FactionLicense FL;
        public string corpShortName;
        public string corpDesc;
        public HashSet<BlockCategories> openCategories = new HashSet<BlockCategories>();
        public Dictionary<BlockCategories, List<WikiPageBlock>> categories = new Dictionary<BlockCategories, List<WikiPageBlock>>();
        public Action infoExtra = null;
        internal GatheredInfo info = null;

        internal static string GetShortName(int corpID)
        {
            if (ManMods.inst.IsModdedCorp((FactionSubTypes)corpID))
                return ManMods.inst.FindCorpShortName((FactionSubTypes)corpID);
            else
                return ((FactionSubTypes)corpID).ToString();
        }
        public WikiPageCorp(string modID, int corpID) : 
            base(modID, new LocExtStringFunc(GetShortName(corpID) + "_corp", 
                () => { return StringLookup.GetCorporationName((FactionSubTypes)corpID); }),
            ManUI.inst.GetModernCorpIcon((FactionSubTypes)corpID), ManIngameWiki.LOC_Corps, ManIngameWiki.CorpsSprite)
        {
            this.corpID = corpID;
            corpShortName = GetShortName(corpID);
            OnWikiPageMade.Send(this);
        }
        public WikiPageCorp(string modID, int corpID, ManIngameWiki.WikiPageGroup grouper) :
            base(modID, new LocExtStringFunc(GetShortName(corpID) + "_corp", 
                () => { return StringLookup.GetCorporationName((FactionSubTypes)corpID); }),
            ManUI.inst.GetModernCorpIcon((FactionSubTypes)corpID), grouper)
        {
            this.corpID = corpID;
            corpShortName = GetShortName(corpID);
            OnWikiPageMade.Send(this);
        }
        public override void GetIcon() { }
        public override void DisplaySidebar() => ButtonGUIDisp();
        protected override void DisplayGUI()
        {
            GUILayout.BeginHorizontal();
            ModdedCorpDefinition MCDC = ManMods.inst.FindCorp(corpShortName);
            if (MCDC != MCD)
            {
                FL = ManLicenses.inst.GetLicense((FactionSubTypes)corpID);
                if (MCDC?.m_Icon != null)
                {
                    icon = Sprite.Create(MCDC.m_Icon, new Rect(0, 0, MCDC.m_Icon.width, MCDC.m_Icon.height),
                        new Vector2(0.5f, 0.5f));
                    if (ManMods.inst.FindCorpShortName((FactionSubTypes)corpID) != corpShortName)
                    {
                        for (int i = 16; i < 128; i++)
                        {
                            if (ManMods.inst.FindCorpShortName((FactionSubTypes)corpID) == corpShortName)
                            {
                                corpID = i;
                                break;
                            }
                        }
                    }
                }
                MCD = MCDC;
            }


            if (icon)
                AltUI.Sprite(icon, AltUI.TextfieldBorderedBlue, GUILayout.Height(128), GUILayout.Width(128));

            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.Label("AKA: ", AltUI.LabelBlackTitle);
            GUILayout.Label(corpShortName, AltUI.LabelBlackTitle);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("CorpID: ", AltUI.LabelBlackTitle);
            GUILayout.Label(corpID.ToString(), AltUI.LabelBlackTitle);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if (FL != null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Corp Levels: ", AltUI.LabelBlackTitle);
                GUILayout.Label(FL.NumXpLevels.ToString(), AltUI.LabelBlackTitle);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();


            GUILayout.BeginHorizontal();
            GUILayout.Label("Lore:", AltUI.LabelBlackTitle);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            if (corpDesc == null)
                corpDesc = GetCorpDescription((FactionSubTypes)corpID);
            if (corpDesc != null)
                GUILayout.Label(corpDesc, AltUI.TextfieldBlackHuge);

            if (infoExtra != null)
                infoExtra();


            for (int i = 1; i < Enum.GetValues(typeof(BlockCategories)).Length; i++)
            {
                BlockCategories category = (BlockCategories)i;
                if (GUILayout.Button(StringLookup.GetBlockCategoryName(category)))
                {
                    if (openCategories.Contains(category))
                        openCategories.Remove(category);
                    else
                        openCategories.Add(category);
                }
                if (openCategories.Contains(category))
                {
                    if (!categories.TryGetValue(category, out var WPBL))
                    {
                        WPBL = new List<WikiPageBlock>();
                        foreach (var item in ManIngameWiki.AllWikis)
                        {
                            if (item.Value?.Pages != null)
                            {
                                foreach (var item2 in item.Value.Pages)
                                {
                                    if (item2 != null && item2 is ManIngameWiki.WikiPageGroup group &&
                                        group.NestedPages != null && "Blocks" == group.title)
                                    {
                                        foreach (var item3 in group.NestedPages)
                                        {
                                            if (item3 is WikiPageBlock WPB &&
                                                ManSpawn.inst.GetCorporation((BlockTypes)WPB.blockID) == (FactionSubTypes)corpID &&
                                                ManSpawn.inst.GetCategory((BlockTypes)WPB.blockID) == category)
                                                WPBL.Add(WPB);
                                        }
                                    }
                                }
                            }
                        }
                        categories.Add(category, WPBL);
                    }
                    foreach (var item in WPBL)
                    {
                        GUILayout.BeginHorizontal();
                        if (GUILayout.Button(item.displayName, AltUI.LabelBlackTitle))
                            item.GoHere();
                        if (item.icon != null)
                            AltUI.Sprite(item.icon, AltUI.TextfieldBorderedBlue, GUILayout.Height(64), GUILayout.Width(64));
                        GUILayout.EndHorizontal();
                    }
                }
            }


            if (GUILayout.Button("Generate Report", AltUI.ButtonOrangeLarge, GUILayout.Height(40)))
            {
                info = new GatheredInfo(corpID);
            }
            if (info != null)
            {
                info.DisplayGUI();
            }
            if (ManIngameWiki.ShowJSONExport && GetCorpData != null)
            {
                if (AltUI.Button("ENTIRE CORP JSON to system clipboard", ManSFX.UISfxType.Craft))
                {
                    AutoDataExtractor.clipboard.Clear();
                    AutoDataExtractor.clipboard.Append(GetCorpData.Invoke((FactionSubTypes)corpID));
                    GUIUtility.systemCopyBuffer = AutoDataExtractor.clipboard.ToString();
                    ManSFX.inst.PlayUISFX(ManSFX.UISfxType.SaveGameOverwrite);
                }
            }
            if (ActiveGameInterop.inst)
            {
                if (GUILayout.Button("Make ALL Nuterra Blocks load faster", AltUI.ButtonOrangeLarge, GUILayout.Height(40)))
                {
                    List<TankBlock> ToSend = new List<TankBlock>();
                    foreach (var item in ManIngameWiki.AllWikis)
                    {
                        if (item.Value?.Pages != null)
                        {
                            foreach (var item2 in item.Value.Pages)
                            {
                                if (item2 != null && item2 is ManIngameWiki.WikiPageGroup group &&
                                    group.NestedPages != null && "Blocks" == group.title)
                                {
                                    foreach (var item3 in group.NestedPages)
                                    {
                                        if (item3 is WikiPageBlock WPB)
                                        {
                                            var BT = (BlockTypes)WPB.blockID;
                                            if (ManMods.inst.IsModdedBlock(BT) &&
                                            ManSpawn.inst.GetCorporation(BT) == (FactionSubTypes)corpID)
                                                ToSend.Add(ManSpawn.inst.GetBlockPrefab(BT));
                                        }
                                    }
                                }
                            }
                        }
                    }
                    ActiveGameInterop.TransmitAllBlocks(ToSend);
                }
            }
        }
        public override bool OnWikiClosed()
        {
            if (info != null)
            {
                info = null;
                return true;
            }
            return false;
        }

        internal class GatheredInfo
        {
            private const int iterationBudget = 256;
            private const int iterationCostPerBlock = 7;
            private readonly FactionSubTypes corpID;

            private BlockTypes iterating = 0;

            private List<WikiPageBlock> blocks = new List<WikiPageBlock>();

            public double CombinedHealth = 0;
            public long CombinedCost = 0;
            public long CombinedVolume = 0;
            public double CombinedMass = 0;
            public double CombinedFragility = 0;

            public double HPToCostDensity = -1;
            public double BlockToHPDensity = -1;
            public double BlockToMassDensity = -1;
            public double AvgFragility = -1;


            /// <summary>
            /// Gather ALL the details by mathmatical, computational BRUTE-FORCE
            /// </summary>
            public GatheredInfo(int corpID)
            {
                this.corpID = (FactionSubTypes)corpID;

                if (ManMods.inst.IsModdedCorp(this.corpID))
                {
                    iterating = (BlockTypes)ManMods.k_FIRST_MODDED_BLOCK_ID;
                }

                ManGameMode.inst.ModeCleanUpEvent.Subscribe(ABORT);
                InvokeHelper.Invoke(Calc,0.1f);
            }
            internal void Calc()
            {
                if (iterating == (BlockTypes)(-1))
                    return;
                if ((int)iterating < Enum.GetValues(typeof(BlockTypes)).Length)
                {
                    for (int i = 0; i < iterationBudget; )
                    {
                        if (CalcStep(ref i))
                        {
                            OnDone();
                            return;
                        }
                    }
                }
                else if (iterating > (BlockTypes)ManMods.k_FIRST_MODDED_BLOCK_ID)
                {
                    for (int i = 0; i < iterationBudget; )
                    {
                        if (CalcStepModded(ref i))
                        {
                            OnDone();
                            return;
                        }
                    }
                }
                InvokeHelper.Invoke(Calc, 0.1f);
            }
            internal void OnDone()
            {
                ManGameMode.inst.ModeCleanUpEvent.Unsubscribe(ABORT);
                try
                {
                    HPToCostDensity = CombinedHealth / CombinedCost;
                }
                catch 
                {
                }
                try
                {
                    BlockToHPDensity = CombinedHealth / CombinedVolume;
                }
                catch
                {
                }
                try
                {
                    BlockToMassDensity = CombinedMass / CombinedVolume;
                }
                catch
                {
                }
                try
                {
                    AvgFragility = CombinedFragility / blocks.Count;
                }
                catch
                {
                }
            }
            internal void ABORT(Mode unused)
            {
                iterating = (BlockTypes)(-1);
                ManGameMode.inst.ModeCleanUpEvent.Unsubscribe(ABORT);
            }
            internal void TryGatherPrefab(BlockTypes toTry)
            {
                var tryMe = ManSpawn.inst.GetBlockPrefab(toTry);
                if (tryMe)
                {
                    CombinedCost += RecipeManager.inst.GetBlockBuyPrice(toTry);
                    CombinedVolume += tryMe.filledCells.Length;
                    CombinedMass += tryMe.m_DefaultMass;
                    var MD = tryMe.GetComponent<ModuleDamage>();
                    CombinedHealth += MD.maxHealth;
                    CombinedFragility += MD.m_DamageDetachFragility;
                }
            }
            internal bool CalcStep(ref int iterator)
            {
                if ((int)iterating < Enum.GetValues(typeof(BlockTypes)).Length)
                {
                    if (ManSpawn.inst.GetCorporation(iterating) == corpID)
                    {
                        blocks.Add(ManIngameWiki.GetBlockPage(ManMods.inst.FindBlockName((int)iterating)));
                        TryGatherPrefab(iterating);
                        iterator += iterationCostPerBlock;
                    }
                    iterating++;
                    iterator++;
                    return false;
                }
                return true;
            }
            internal bool CalcStepModded(ref int iterator)
            {
                if (ManMods.inst.IsModdedBlock(iterating))
                {
                    if (ManSpawn.inst.GetCorporation(iterating) == corpID)
                    {
                        blocks.Add(ManIngameWiki.GetBlockPage(ManMods.inst.FindBlockName((int)iterating)));
                        TryGatherPrefab(iterating);
                        iterator += iterationCostPerBlock;
                    }
                    iterating++;
                    return false;
                }
                return true;
            }

            private void TinyStringDisp(string desc, double val)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(desc, AltUI.LabelBlack);
                GUILayout.Label(val.ToString(), AltUI.TextfieldBlackAdjusted);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }


            internal void DisplayGUI()
            {
                if (BlockToHPDensity != -1)
                {
                    TinyStringDisp("Total Blocks: ", blocks.Count);
                    TinyStringDisp("Total Health: ", CombinedHealth);
                    TinyStringDisp("Total Volume: ", CombinedVolume);
                    TinyStringDisp("Total Cost: ", CombinedCost);
                    TinyStringDisp("Health per Cost Average: ", HPToCostDensity);
                    TinyStringDisp("Health per Volume Average: ", BlockToHPDensity);
                    TinyStringDisp("Averaged Mass: ", BlockToMassDensity);
                    TinyStringDisp("Averaged Fragility: ", AvgFragility);
                }
                else
                {
                    TinyStringDisp("Total Blocks: ", blocks.Count);
                    TinyStringDisp("Total Health: ", CombinedHealth);
                    TinyStringDisp("Total Volume: ", CombinedVolume);
                    TinyStringDisp("Total Mass: ", CombinedMass);
                    TinyStringDisp("Total Fragility: ", CombinedFragility);
                }
            }
        }
    }

}
