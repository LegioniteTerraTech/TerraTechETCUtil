using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace TerraTechETCUtil
{
    public class ManIngameWiki : TinySettings
    {
        private static Sprite infoSprite;
        public static Sprite InfoSprite {
            get
            {
                if (infoSprite == null)
                {
                    HintIcon = Resources.FindObjectsOfTypeAll<Texture2D>().FirstOrDefault(x => x.name == "ICONS_HINTS_SURROUND");
                    if (HintIcon)
                        AltUI.ComesInBlack(HintIcon, ref HintIconBlack);
                    else
                        throw new NullReferenceException("Could not find ICONS_HINTS_SURROUND (hint icon)");
                    infoSprite = Sprite.Create(HintIcon, new Rect(0, 0, HintIcon.width, HintIcon.height), new Vector2(0.5f, 0.5f));
                }
                return infoSprite;
            }
        }
        public static Sprite CorpsSprite => ManUI.inst.GetModernCorpIcon(FactionSubTypes.GSO);
        public static Sprite BlocksSprite => ManUI.inst.GetSprite(ObjectTypes.Block, (int)BlockTypes.GSOBlock_111);

        public static void ApplyNewWikiBlockDescOverride(string typeName, string newName)
        {
            WikiPageBlock.SpecialNames[typeName] = newName;
        }
        public static void IgnoreWikiBlockExtModule<T>(T toIgnore) where T : ExtModule
        {
            WikiPageBlock.ignoreTypes.Add(typeof(T));
        }
        public static void RecurseCheckWikiBlockExtModule<T>(T toIncludeInCollection)
        {
            WikiPageBlock.AllowedTypes.Add(typeof(T));
        }

        public static void ReplaceDescription(string ModID, string Title, Action replacement)
        {
            if (!ModID.NullOrEmpty() && Wikis.TryGetValue(ModID, out var wiki))
            {
                if (!Title.NullOrEmpty() && wiki.ALLPages.TryGetValue(Title, out var page))
                {
                    page.infoOverride = replacement;
                    return;
                }
            }
            throw new InvalidOperationException("ManIngameWiki.ReplaceDescription cannot be replaced because the ModID or title does not match");
        }

        public static void InjectHint(string ModID, string Title, string hintString)
        {
            if (!ModID.NullOrEmpty())
            {
                Wiki wiki = InsureWiki(ModID);
                if (!Title.NullOrEmpty())
                {
                    if (wiki.ALLPages.TryGetValue(Title, out var page))
                    {
                        if (page is WikiPageHints pageHintsGet)
                        {
                            if (!pageHintsGet.hints.Contains(hintString))
                                pageHintsGet.hints.Add(hintString);
                        }
                    }
                    else
                    {
                        new WikiPageHints(ModID, Title, hintString, InsureWikiGroup(ModID, "Hints", InfoSprite));
                    }
                    return;
                }
            }
            throw new InvalidOperationException("ManIngameWiki.ReplaceDescription cannot be replaced because the ModID or title does not match");
        }


        public abstract class WikiPage : GUILayoutHelpers.SlowSortable
        {
            public readonly Wiki wiki;
            public readonly string title;
            public readonly Sprite icon = null;
            public string name => title;
            public Action infoOverride = null;
            protected WikiPage(string ModID, string Title, Sprite Icon)
            {
                wiki = InsureWiki(ModID);
                title = Title;
                icon = Icon;
                wiki.RegisterPage(this);
            }
            protected WikiPage(string ModID, string Title, Sprite Icon, WikiPageGroup group)
            {
                wiki = InsureWiki(ModID);
                title = Title;
                icon = Icon;
                if (group != null)
                {
                    group.NestedPages.Add(this);
                    wiki.RegisterPage(this, false);
                }
                else
                    wiki.RegisterPage(this);
            }
            protected WikiPage(string ModID, string Title, Sprite Icon, string WikiGroupName, Sprite IconGroup)
            {
                wiki = InsureWiki(ModID);
                title = Title;
                icon = Icon;
                InsureWikiGroup(wiki.ModID, WikiGroupName, IconGroup).NestedPages.Add(this);
                wiki.RegisterPage(this, false);
            }
            public abstract void DisplaySidebar();
            internal void DoDisplayGUI()
            {
                if (infoOverride != null)
                    infoOverride();
                else
                    DisplayGUI();
            }
            public virtual void DisplayGUI()
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("This wiki is", AltUI.LabelBlackTitle);
                GUILayout.Label("UNDER CONSTRUCTION", AltUI.LabelBlueTitle);
                GUILayout.FlexibleSpace();
            }
            public abstract bool ReleaseAsMuchAsPossible();

            public void ReplaceThis(WikiPage replacement)
            {
                if (replacement.title != title)
                    throw new InvalidOperationException("ManIngameWiki.WikiPage.ReplaceThis cannot be replaced because the titles do not match");
                for (int step = 0; step < wiki.ALLPages.Count; step++)
                {
                    var item = wiki.ALLPages.ElementAt(step);
                    if (item.Value is WikiPageGroup grouper)
                    {
                        int val = grouper.NestedPages.IndexOf(this);
                        if (val != -1)
                        {
                            grouper.NestedPages.RemoveAt(val);
                            grouper.NestedPages.Insert(val, replacement);
                        }
                    }
                }
                wiki.ALLPages[replacement.title] = replacement;
            }

            public static void ReplaceDescription(string ModID, string Title, Action replacement)
            {
                if (!inst.wikiID.NullOrEmpty() && Wikis.TryGetValue(ModID, out var wiki))
                {
                    if (!inst.pageName.NullOrEmpty() && wiki.ALLPages.TryGetValue(Title, out var page))
                    {
                        page.infoOverride = replacement;
                        return;
                    }
                }
                throw new InvalidOperationException("ManIngameWiki.WikiPage.ReplaceDescription cannot be replaced because the titles do not match");
            }
            public void ReplaceDescription(Action replacement)
            {
                infoOverride = replacement;
            }


            protected void ButtonGUIDisp()
            {
                GUILayout.BeginHorizontal(CurrentWikiPage == this ? AltUI.ButtonGreen : AltUI.ButtonBlue, GUILayout.Height(35));
                if (AltUI.Button(title, ManSFX.UISfxType.Select, AltUI.LabelWhite, GUILayout.Height(35)))
                    GoHere();
                if (icon && AltUI.Button(icon, ManSFX.UISfxType.Select, AltUI.LabelWhite, GUILayout.Height(35), GUILayout.Width(35)))
                    GoHere();
                GUILayout.EndHorizontal();
            }

            public void GoBack()
            {
                if (backtrace.Any())
                {
                    CurrentWikiPage = backtrace.Last();
                    backtrace.RemoveAt(backtrace.Count - 1);
                }
                else
                {
                    if (wiki.MainPage == null)
                    {
                        CurrentWikiPage = DefaultWikiPage;
                    }
                    else
                        CurrentWikiPage = wiki.MainPage;
                }
            }
            public void GoHere()
            {
                CurrentWikiPage = this;
            }
        }

        public sealed class WikiPageGroup : WikiPage
        {
            public List<WikiPage> NestedPages = new List<WikiPage>();
            public string searchQuery = "";
            public GUILayoutHelpers.SlowSorter<WikiPage> PageHunter;
            public bool open = false;
            public WikiPageGroup(string ModID, string Title, Sprite Icon = null) : base(ModID, Title, Icon, null)
            {
            }
            public override void DisplaySidebar()
            {
                GUILayout.BeginHorizontal(open ? AltUI.ButtonBlueActive : AltUI.ButtonBlue, GUILayout.Height(35));
                if (AltUI.Button(title, ManSFX.UISfxType.Select, AltUI.LabelWhite, GUILayout.Height(35)))
                    open = !open;
                if (icon && AltUI.Button(icon, ManSFX.UISfxType.Select, AltUI.LabelWhite, GUILayout.Height(35), GUILayout.Width(35)))
                    open = !open;
                GUILayout.EndHorizontal();
                if (open)
                {
                    GUILayout.BeginVertical(AltUI.TextfieldBordered);
                    if (NestedPages.Count >= 32)
                    {
                        if (PageHunter == null)
                        {
                            PageHunter = new GUILayoutHelpers.SlowSorter<WikiPage>(32);
                            PageHunter.SetSearchArrayAndSearchQuery(NestedPages.ToArray(), searchQuery, true);
                        }
                        if (GUILayoutHelpers.GUITextFieldDisp("Block Name:", ref searchQuery))
                            PageHunter.SetNewSearchQueryIfNeeded(searchQuery, true);
                        if (PageHunter.namesValid.Count > 0)
                        {
                            foreach (var item in PageHunter.valid)
                            {
                                item.DisplaySidebar();
                            }
                        }
                    }
                    else
                    {
                        foreach (var item in NestedPages)
                        {
                            item.DisplaySidebar();
                        }
                    }
                    GUILayout.EndVertical();
                }
            }
            public override bool ReleaseAsMuchAsPossible()
            {
                return false;
            }
        }
        public class WikiPageMainDefault : WikiPage
        {
            public WikiPageMainDefault(string ModID, string Title, Sprite Icon = null) : base(ModID, Title, Icon, null)
            {
            }
            public override void DisplaySidebar()
            {
                GUILayout.BeginHorizontal(CurrentWikiPage == this ? AltUI.ButtonGreen : AltUI.ButtonBlue, GUILayout.Height(35));
                if (AltUI.Button("<b>Home</b>", ManSFX.UISfxType.Select, AltUI.LabelWhite, GUILayout.Height(35)))
                    GoHere();
                if (icon && AltUI.Button(icon, ManSFX.UISfxType.Select, AltUI.LabelWhite, GUILayout.Height(35), GUILayout.Width(35)))
                    GoHere();
                GUILayout.EndHorizontal();
            }
            public override bool ReleaseAsMuchAsPossible()
            {
                return false;
            }
        }



        public class Wiki
        {
            public readonly string ModID;
            public ModContainer modContainer => ManMods.inst.FindMod(ModID);
            public WikiPageGroup MainPage = null;
            internal List<WikiPage> Pages => MainPage.NestedPages;
            internal Dictionary<string, WikiPage> ALLPages = new Dictionary<string, WikiPage>();

            private int registeredNum = 0;

            public Wiki(string ModID)
            {
                this.ModID = ModID;
                Wikis.Add(ModID, this);
                MainPage = new WikiPageGroup(ModID, ModID);
            }
            internal void Prepare()
            {
                MainPage = new WikiPageGroup(ModID, ModID);
            }
            internal int RegisterPage(WikiPage page, bool rootLevel = true, bool throwOnOverlap = false)
            {
                if (ALLPages.TryGetValue(page.title, out var pageInst))
                {
                    if (throwOnOverlap)
                        throw new InvalidOperationException("(throwOnOverlap) ~ Cannot add two pages of the exact same title to a wiki!");
                    pageInst.ReplaceThis(page);
                    return registeredNum;
                }
                ALLPages.Add(page.title, page);
                if (rootLevel && MainPage != null)
                    Pages.Add(page);
                return registeredNum++;
            }
        }

        public static Wiki GetModWiki(string ModID)
        {
            return InsureWiki(ModID);
        }

        private static bool TryCleanupHint(string item, string name)
        {
            if (item.StartsWith(name))
            {
                InjectHint(VanillaGameName, name, item.Replace(name, ""));
                return true;
            }
            return false;
        }
        private static void FinishVanillaWikiSearch()
        {
            var banks = Localisation.inst.GetLocalisedStringBank(LocalisationEnums.StringBanks.LoadingHints);
            if (banks.Length == 0)
            {
                InvokeHelper.Invoke(FinishVanillaWikiSearch, 0.5f);
            }
            else
            {
                foreach (var item in Localisation.inst.GetLocalisedStringBank(LocalisationEnums.StringBanks.LoadingHints))
                {
                    if (!item.NullOrEmpty())
                    {
                        string itemCleaned = item.Replace(
                            "<color=#e4ac41ff><size=32><b><i>", "").Replace(
                            "</i></b></size></color>\n", "");
                        if (TryCleanupHint(item, "GENERAL HINT") ||
                            TryCleanupHint(item, "MULTIPLAYER HINT") ||
                            TryCleanupHint(item, "GAUNTLET MODE") ||
                            TryCleanupHint(item, "CREATIVE MODE HINT") ||
                            TryCleanupHint(item, "CAMPAIGN HINT"))
                        {
                        }
                        else
                            InjectHint(VanillaGameName, "Other", item);
                    }
                }
            }
        }

        internal static FieldInfo
            hintBatch = typeof(ManHints).GetField("m_HintDefinitions", BindingFlags.NonPublic | BindingFlags.Instance),
            hintBatchCore = typeof(HintDefinitionList).GetField("m_HintsList", BindingFlags.NonPublic | BindingFlags.Instance);
        private static void AutoPopulateVanilla(Wiki wiki)
        {
            WikiPageGroup corps = new WikiPageGroup(wiki.ModID, "Corporations", CorpsSprite);
            for (int step = 0; step < Enum.GetValues(typeof(BlockTypes)).Length; step++)
            {
                if (ManSpawn.inst.IsBlockAllowedInLaunchedConfig((BlockTypes)step) &&
                    ManSpawn.inst.IsBlockAllowedInCurrentGameMode((BlockTypes)step) &&
                    !wiki.ALLPages.ContainsKey(StringLookup.GetItemName(ObjectTypes.Block, step)))
                {
                    new WikiPageBlock(step);
                }
            }
            for (int step = 1; step < Enum.GetValues(typeof(FactionSubTypes)).Length; step++)
            {
                FactionSubTypes FST = (FactionSubTypes)step;
                if (!wiki.ALLPages.ContainsKey(StringLookup.GetCorporationName(FST)))
                {
                    new WikiPageCorp(wiki.ModID, step, corps);
                }
            }
            List<ManHints.HintDefinition> lingo = (List<ManHints.HintDefinition>)hintBatchCore.GetValue(
                (HintDefinitionList)hintBatch.GetValue(ManHints.inst));
            foreach (var item in lingo)
            {
                if (item.m_HintMessage != null && item.m_HintMessage.IsValid)
                    InjectHint(VanillaGameName, "General", item.m_HintMessage.Value);
            }
            InvokeHelper.Invoke(FinishVanillaWikiSearch, 3f);
        }
        private static void AutoPopulateWiki(Wiki wiki)
        {
            if (wiki.ModID == VanillaGameName)
            {
                AutoPopulateVanilla(wiki);
            }
            else
            {
                var MC = wiki.modContainer;
                if (MC != null)
                {
                    var contents = MC.Contents;
                    WikiPageGroup corps = null;
                    if (contents.m_Corps != null && contents.m_Corps.Any())
                        corps = new WikiPageGroup(wiki.ModID, "Corporations", CorpsSprite);
                    WikiPageGroup blocks = null;
                    if (contents.m_Blocks != null && contents.m_Blocks.Any())
                    {
                        foreach (var item in contents.m_Blocks)
                        {
                            if (!wiki.ALLPages.ContainsKey(item.m_BlockDisplayName))
                            {
                                new WikiPageBlock(ManMods.inst.GetBlockID(item.name));
                            }
                        }
                    }
                    if (corps != null)
                    {
                        foreach (var item in contents.m_Corps)
                        {
                            if (!wiki.ALLPages.ContainsKey(item.m_DisplayName))
                            {
                                new WikiPageCorp(wiki.ModID, (int)ManMods.inst.GetCorpIndex(item.name), corps);
                            }
                        }
                    }
                }
            }
        }
        public static Wiki InsureWiki(string ModID)
        {
            if (ModID.NullOrEmpty())
                throw new NullReferenceException("ModID should NOT be null.  WHY IS IT NULL");
            if (Wikis.TryGetValue(ModID, out Wiki wiki))
            {
                return wiki;
            }
            wiki = new Wiki(ModID);
            AutoPopulateWiki(wiki);
            return wiki;
        }

        public static WikiPageGroup InsureWikiGroup(string ModID, string WikiGroupName, Sprite Icon = null)
        {
            if (ModID.NullOrEmpty())
                throw new NullReferenceException("ModID should NOT be null.  WHY IS IT NULL");
            if (WikiGroupName.NullOrEmpty())
                throw new NullReferenceException("WikiGroupName should NOT be null.  WHY IS IT NULL");
            Wiki wiki = InsureWiki(ModID);
            if (wiki.ALLPages.TryGetValue(WikiGroupName, out var page))
            {
                if (page is WikiPageGroup pageR2)
                    return pageR2;
                else
                    throw new InvalidOperationException("InsureWikiGroup tried to add a new entry to wiki \"" + 
                        ModID + "\", but it was already taken by a non-group of the same name!");
            }
            else
                return new WikiPageGroup(ModID, WikiGroupName, Icon);
        }

        public static WikiPageBlock GetBlockPage(string blockName)
        {
            try
            {
                return (WikiPageBlock)Wikis.Values.FirstOrDefault(x => x.ALLPages.ContainsKey(blockName))?.ALLPages[blockName];
            }
            catch
            {
                return null;
            }
        }


        public const string VanillaGameName = "Terra Tech";
        public string DirectoryInExtModSettings => "IngameWiki";

        private static ManIngameWiki inst = new ManIngameWiki();
        private static Dictionary<string, Wiki> Wikis = new Dictionary<string, Wiki>();

        private static WikiPage DefaultWikiPage = new WikiPageMainDefault(VanillaGameName, "Terra Tech Wiki V0.1", UIHelpersExt.NullSprite);


        private static bool assignedQuit = false;
        private static WikiPage curWikiPage = default;
        public static WikiPage CurrentWikiPage
        {
            get => curWikiPage;
            set
            {
                if (curWikiPage != value)
                {
                    backtrace.Add(curWikiPage);
                    if (backtrace.Count > 64)
                        backtrace.RemoveAt(0);
                }
                curWikiPage = value;
                if (!assignedQuit)
                {
                    assignedQuit = true;
                    Application.quitting += SaveState;
                }
            }
        }
        private static List<WikiPage> backtrace = new List<WikiPage>();

        public static KeyCode WikiButtonKeybind = KeyCode.Backslash;
        public bool sideOpen = true;
        public string wikiID;
        public string pageName;
        public float scrollY;
        public static Vector2 scrollSide;
        public static Vector2 scroll;


        private static GUIInst instGUI;
        private static Rect guiWindow = new Rect(20, 20, 1000, 600);
        private const int ExtWikiID = 1002248;
        public static AltUI.GUIToolTip Tooltip => tooltip;
        private static AltUI.GUIToolTip tooltip = new AltUI.GUIToolTip();
        public class GUIInst : MonoBehaviour
        {
            private bool open = false;
            internal void ToggleGUI()
            {
                SetGUI(!open);
            }
            internal void SetGUI(bool state)
            {
                if (state != open)
                {
                    open = state;
                    if (open)
                        ManSFX.inst.PlayUISFX(ManSFX.UISfxType.Open);
                    else
                        ManSFX.inst.PlayUISFX(ManSFX.UISfxType.Close);
                    WikiButton.SetToggleState(open);
                }
            }
            public void Update()
            {
                if (Input.GetKeyDown(WikiButtonKeybind))
                    ToggleGUI();
                tooltip.DoUpdate();
            }
            public void OnGUI()
            {  
                try
                {
                    if (open)
                    {
                        guiWindow = AltUI.Window(ExtWikiID, guiWindow, GUILayouter, "Mod Wiki", CloseGUI);
                    }
                }
                catch (ExitGUIException e)
                {
                    throw e;
                }
                catch (Exception) { }
            }
        }
        public static Rect InGUIRect = new Rect();
        public static void GUILayouter(int ID)
        {
            GUILayout.BeginHorizontal(GUILayout.Height(40));
            {
                if (CurrentWikiPage != null)
                {
                    if (GUILayout.Button("<", AltUI.ButtonOrangeLarge, GUILayout.Width(40)))
                        CurrentWikiPage.GoBack();
                }
                else
                    GUILayout.Button("<", AltUI.ButtonGreyLarge, GUILayout.Width(40));
            }
            if (GUILayout.Button("Contents", inst.sideOpen ? AltUI.ButtonBlueLargeActive : AltUI.ButtonBlueLarge, GUILayout.Width(280)))
                inst.sideOpen = !inst.sideOpen;
            if (curWikiPage != null)
                GUILayout.Label(curWikiPage.title, AltUI.LabelBlackTitle);
            else
                GUILayout.Label(DefaultWikiPage.title, AltUI.LabelBlackTitle);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (inst.sideOpen)
            {
                GUILayout.BeginVertical(GUILayout.Width(320));
                scrollSide = GUILayout.BeginScrollView(scrollSide);
                foreach (var item in Wikis.Values)
                {
                    if (item.MainPage != null)
                    {
                        item.MainPage.DisplaySidebar();
                    }
                }
                GUILayout.EndScrollView();
                GUILayout.EndVertical();
            }
            GUILayout.BeginVertical();
            scroll = GUILayout.BeginScrollView(scroll, false, true);
            if (curWikiPage != null)
                curWikiPage.DoDisplayGUI();
            else
                DefaultWikiPage.DoDisplayGUI();
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            tooltip.EndDisplayGUIToolTip();
            GUI.DragWindow();
        }


        internal static void ToggleGUI()
        {
            instGUI.ToggleGUI();
        }
        internal static void  SetGUI(bool state)
        {
            instGUI.SetGUI(state);
        }
        internal static void CloseGUI()
        {
            instGUI.SetGUI(false);
            ReleaseAsMuchMemoryAsPossible();
        }





        private static Texture2D HintIcon;
        private static Texture2D HintIconBlack;
        private static ManToolbar.ToolbarToggle WikiButton = new ManToolbar.ToolbarToggle("In-Game Wiki", 
            InfoSprite, SetGUI);
        internal static void InitWiki()
        {
            LoadPage();
            _ = InfoSprite;

            if (instGUI == null)
            {
                instGUI = new GameObject("ManIngameWiki").AddComponent<GUIInst>();
            }
            //InvokeHelper.InvokeSingleRepeat(SpamIt, 0.1f);
            /*
            Singleton.Manager<ManUI>.inst.GetScreen(ManUI.ScreenType.WhatIsGrabIt);
            Singleton.Manager<ManUI>.inst.GoToScreen(ManUI.ScreenType.WhatIsGrabIt);
            InvokeHelper.InvokeSingleRepeat(SpamIt, 0.1f);
            */
        }
        private static void SpamIt()
        {
            Singleton.Manager<ManUI>.inst.GoToScreen(ManUI.ScreenType.WhatIsGrabIt);
        }



        public static void ReleaseAsMuchMemoryAsPossible()
        {
            bool released = false;
            foreach (var item in Wikis.Values)
            {
                foreach (var item2 in item.ALLPages.Values)
                {
                    if (item2.ReleaseAsMuchAsPossible())
                        released = true;
                }
            }
            if (released)
                GC.Collect();
        }

        private static void SaveState()
        {
            if (curWikiPage != null)
            {
                inst.wikiID = curWikiPage.wiki.ModID;
                inst.pageName = curWikiPage.title;
                inst.scrollY = scroll.y;
            }
            else
            {
                inst.wikiID = null;
                inst.pageName = null;
                inst.scrollY = 0;
            }
            inst.TrySaveToDisk();
        }

        private static void LoadPage()
        {   // Loading from memory
            inst.TryLoadFromDisk(ref inst);
            if (!inst.wikiID.NullOrEmpty() && Wikis.TryGetValue(inst.wikiID, out var wiki))
            { 
                if (!inst.pageName.NullOrEmpty() && wiki.ALLPages.ContainsKey(inst.pageName))
                {
                    scroll.y = inst.scrollY;
                    CurrentWikiPage = curWikiPage;
                    return;
                }
            }
        }
    }
}
