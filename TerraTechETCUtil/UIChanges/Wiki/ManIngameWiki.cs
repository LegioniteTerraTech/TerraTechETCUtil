using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;

namespace TerraTechETCUtil
{
    public class ManIngameWiki : TinySettings
    {
        public static Event<Wiki> OnModWikiCreated = new Event<Wiki>();
        private static byte renderedIcon = 0;

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
        public static Sprite ChunksSprite => ManUI.inst.GetSprite(ObjectTypes.Chunk, (int)ChunkTypes.Wood);
        public static Sprite BlocksSprite => ManUI.inst.GetSprite(ObjectTypes.Block, (int)BlockTypes.GSOBlock_111);
        public static Sprite ScenerySprite => ManUI.inst.GetSprite(ObjectTypes.Scenery, (int)SceneryTypes.GrasslandRock);

        public static void ApplyNewWikiBlockDescOverride(string typeName, string newName)
        {
            AutoDataExtractor.SpecialNames[typeName] = newName;
        }
        public static void IgnoreWikiBlockExtModule<T>() where T : ExtModule
        {
            AutoDataExtractor.ignoreTypes.Add(typeof(T));
        }
        public static void RecurseCheckWikiBlockExtModule<T>()
        {
            AutoDataExtractor.AllowedTypes.Add(typeof(T));
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

        public struct WikiLink
        {
            public readonly WikiPage linked;
            public WikiLink(WikiPage theLink)
            {  linked = theLink; }
            public bool OnGUI(GUIStyle style)
            {
                if (linked.icon != null && linked.icon != UIHelpersExt.NullSprite)
                    return AltUI.SpriteButton(linked.icon, style, GUILayout.Height(28), GUILayout.Width(28));
                else
                {
                    linked.GetIcon();
                    return GUILayout.Button(linked.title, style);
                }
            }
            public bool OnGUILarge(GUIStyle style, GUIStyle styleText)
            {
                GUILayout.BeginHorizontal(style);
                if (linked.icon != null && linked.icon != UIHelpersExt.NullSprite)
                    AltUI.Sprite(linked.icon, AltUI.TRANSPARENT, GUILayout.Height(32), GUILayout.Width(32));
                else
                    linked.GetIcon();
                GUILayout.Label(linked.title, styleText);
                GUILayout.EndHorizontal();
                return GUI.Button(GUILayoutUtility.GetLastRect(), string.Empty, AltUI.TRANSPARENT);
            }
        }

        public abstract class WikiPage : GUILayoutHelpers.SlowSortable
        {
            public readonly Wiki wiki;
            public readonly string title;
            public Sprite icon { get; protected set; } = null;
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
                if (wiki.RegisterPage(this, false))
                    InsureWikiGroup(wiki.ModID, WikiGroupName, IconGroup).NestedPages.Add(this);
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
            public virtual void GetIcon() { }
            private static List<MonoBehaviour> wasActiveMB = new List<MonoBehaviour>();
            public void GetIconDelayed(GameObject GO, bool wasActive, Action removalCallback)
            {
                try
                {
                    try
                    {
                        Bounds approxBounds = default;
                        foreach (var item in GO.GetComponentsInChildren<Collider>(true))
                        {
                            if (approxBounds == default)
                                approxBounds = item.bounds;
                            else
                                approxBounds.Encapsulate(item.bounds);
                        }
                        if (approxBounds == default)
                            approxBounds = new Bounds(new Vector3(0, 3, 0), new Vector3(5, 6, 5));
                        Texture2D tex = ResourcesHelper.GeneratePreviewForGameObject(GO, approxBounds, 
                            new Vector3(1, 0.25f, 1).normalized * approxBounds.size.magnitude);
                        tex.Apply();
                        icon = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.zero);
                        if (!wasActive)
                            GO.SetActive(false);
                    }
                    finally
                    {
                        foreach (var item in wasActiveMB)
                            item.enabled = false;
                        wasActiveMB.Clear();
                        removalCallback.Invoke();
                    }
                }
                catch (Exception e)
                {
                    icon = UIHelpersExt.NullSprite;
                    throw new OperationCanceledException("We tried to recover from a crash while handling" +
                        " GameObject \"" + (GO.name.NullOrEmpty() ? "<NULL>" : GO.name) +
                        "\" in the spriter, but we failed.  It is now compromised", e);
                }
            }
            public void GetIcon(GameObject GO, Action removalCallback)
            {
                try
                {
                    try
                    {
                        foreach (var item in GO.GetComponentsInChildren<MonoBehaviour>(true))
                        {
                            if (item.enabled)
                            {
                                item.enabled = false;
                                wasActiveMB.Add(item);
                            }
                        }
                        bool wasActive = GO.gameObject.activeSelf;
                        if (!wasActive)
                            GO.SetActive(true);
                        GetIconDelayed(GO, wasActive, removalCallback);
                        //InvokeHelper.Invoke(GetIconDelayed, 1.001f,GO, wasActive, removalCallback);
                    }
                    catch (Exception e)
                    {
                        foreach (var item in wasActiveMB)
                            item.enabled = false;
                        wasActiveMB.Clear();
                        icon = UIHelpersExt.NullSprite;
                        removalCallback.Invoke();
                    }
                }
                catch (Exception e)
                {
                    icon = UIHelpersExt.NullSprite;
                    throw new OperationCanceledException("We tried to recover from a crash while handling" +
                        " GameObject \"" + (GO.name.NullOrEmpty() ? "<NULL>" : GO.name) +
                        "\" in the spriter, but we failed.  It is now compromised", e);
                }
            }
            public IEnumerable<T> IterateForInfo<T>() where T : WikiPage
            {
                foreach (var item in AllWikis)
                {
                    foreach (var item2 in item.Value.ALLPages)
                    {
                        if (item2.Value is T pageC)
                            yield return pageC;
                    }
                }
            }
            public virtual void OnBeforeDisplay() { }
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
                            grouper.NestedPages[val] = replacement;
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
                GUILayout.Label(title, AltUI.LabelWhite, GUILayout.Height(35));
                if (icon)
                    AltUI.Button(icon, ManSFX.UISfxType.Select, AltUI.LabelWhite, GUILayout.Height(35), GUILayout.Width(35));
                GUILayout.EndHorizontal();
                Rect lastRect = GUILayoutUtility.GetLastRect();
                if (GUI.Button(lastRect, string.Empty, AltUI.TRANSPARENT))
                {
                    ManSFX.inst.PlayUISFX(ManSFX.UISfxType.Select);
                    GoHere();
                }
            }
            protected void ButtonGUIDispLateIcon()
            {
                if (icon == null && renderedIcon == 0)
                {
                    renderedIcon = 2;
                    GetIcon();
                }
                GUILayout.BeginHorizontal(CurrentWikiPage == this ? AltUI.ButtonGreen : AltUI.ButtonBlue, GUILayout.Height(35));
                GUILayout.Label(title, AltUI.LabelWhite, GUILayout.Height(35));
                if (icon)
                    AltUI.Button(icon, ManSFX.UISfxType.Select, AltUI.LabelWhite, GUILayout.Height(35), GUILayout.Width(35));
                GUILayout.EndHorizontal();
                Rect lastRect = GUILayoutUtility.GetLastRect();
                if (GUI.Button(lastRect, string.Empty, AltUI.TRANSPARENT))
                {
                    ManSFX.inst.PlayUISFX(ManSFX.UISfxType.Select);
                    GoHere();
                }
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
            public Action onOpen = null;
            public bool open = false;
            public WikiPageGroup(string ModID, string Title, Sprite Icon = null) : base(ModID, Title, Icon, null)
            {
            }
            public override void DisplaySidebar()
            {
                GUILayout.BeginHorizontal(open ? AltUI.ButtonBlueActive : AltUI.ButtonBlue, GUILayout.Height(35));
                GUILayout.Label(title, AltUI.LabelWhite, GUILayout.Height(35));
                if (icon)
                    AltUI.Button(icon, ManSFX.UISfxType.Select, AltUI.LabelWhite, GUILayout.Height(35), GUILayout.Width(35));
                GUILayout.EndHorizontal();
                Rect lastRect = GUILayoutUtility.GetLastRect();
                if (GUI.Button(lastRect, string.Empty, AltUI.TRANSPARENT))
                {
                    open = !open;
                    ManSFX.inst.PlayUISFX(ManSFX.UISfxType.Select);
                    if (open && onOpen != null)
                        onOpen();
                }
                if (open)
                {
                    GUILayout.BeginVertical(AltUI.TextfieldBordered);
                    if (NestedPages.Count >= 16)
                    {
                        if (PageHunter == null)
                        {
                            PageHunter = new GUILayoutHelpers.SlowSorter<WikiPage>(32);
                            PageHunter.SetSearchArrayAndSearchQuery(NestedPages.ToArray(), searchQuery, true);
                        }
                        if (GUILayoutHelpers.GUITextFieldDisp("Search:", ref searchQuery))
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

            public int RegisteredPagesCount => ALLPages.Count;

            public Wiki(string ModID)
            {
                this.ModID = ModID;
                Wikis.Add(ModID, this);
                if (ModID == VanillaGameName)
                    MainPage = new WikiPageGroup(ModID, "Vanilla - Terra Tech");
                else
                    MainPage = new WikiPageGroup(ModID, "Mod - " + ModID);
                Debug_TTExt.Log("Prepared wiki for " + ModID);
            }
            internal void PrepareModded()
            {
                var corps = modContainer?.Contents?.m_Corps;
                if (corps != null && corps.Any())
                {
                    WikiPageGroup corpsPage = (WikiPageGroup)GetPage("Corporations");
                    if (corpsPage == null)
                        corpsPage = new WikiPageGroup(ModID, "Corporations", CorpsSprite);
                    foreach (var item in corps)
                    {
                        new WikiPageCorp(ModID, (int)ManMods.inst.GetCorpIndex(item.m_ShortName), corpsPage);
                    }
                }
                MainPage.onOpen = null;
            }
            internal bool RegisterPage(WikiPage page, bool rootLevel = true, bool throwOnOverlap = false)
            {
                if (ALLPages.TryGetValue(page.title, out var pageInst))
                {
                    if (throwOnOverlap)
                        throw new InvalidOperationException("(throwOnOverlap) ~ Cannot add two pages of the exact same title to a wiki!");
                    pageInst.ReplaceThis(page);
                    return false;
                }
                else
                {
                    ALLPages.Add(page.title, page);
                    if (rootLevel && MainPage != null)
                        Pages.Add(page);
                    return true;
                }
            }
        }

        public static Wiki GetModWiki(string ModID)
        {
            return InsureWiki(ModID);
        }
        public static bool TryGetModWiki(string ModID, out Wiki wiki) => Wikis.TryGetValue(ModID, out wiki);

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

        private static void GUITools()
        {
            if (ActiveGameInterop.inst && ActiveGameInterop.IsReady)
            {
                if (!DebugExtUtilities.AllowEnableDebugGUIMenu_KeypadEnter)
                    DebugExtUtilities.AllowEnableDebugGUIMenu_KeypadEnter = true;
                if (GUILayout.Button("Unhook from Editor", AltUI.ButtonBlueLarge))
                    ActiveGameInterop.DeInitBothEnds();
            }
            else if (ActiveGameInterop.inst)
            {
                if (GUILayout.Button("Waiting For Editor...", AltUI.ButtonGreyLarge))
                    ActiveGameInterop.DeInitJustThisSide();
                GUILayout.Label("You might have to hover your mouse over UnityEditor to get it to update");
            }
            else if (GUILayout.Button("Try Hook To UnityEditor", AltUI.ButtonOrangeLarge))
            {
                ActiveGameInterop.Init();
                InvokeHelper.InvokeSingleRepeat(ActiveGameInterop.UpdateNow, 1);
            }
            if (ActiveGameInterop.inst)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Update", AltUI.ButtonGreen))
                    ActiveGameInterop.UpdateNow();
                if (ActiveGameInterop.IsReceiving)
                    GUILayout.Label("Receiving");
                else
                    GUILayout.Label("Transmitting");
                GUILayout.EndHorizontal();
                if (ActiveGameInterop.IsReady && GUILayout.Button("Say Whoa", AltUI.ButtonBlue))
                    ActiveGameInterop.TryTransmitTest("Whoa");
                GUILayout.Label(ActiveGameInterop._debug, AltUI.TextfieldBlackHuge);
            }

            if (DebugExtUtilities.AllowEnableDebugGUIMenu_KeypadEnter &&
                GUILayout.Button("Open Mod Developer Tools", AltUI.ButtonOrangeLarge))
                DebugExtUtilities.Open();

            GUILayout.FlexibleSpace();
        }

        internal static FieldInfo
            Biomes = typeof(BiomeMap).GetField("m_BiomeGroups", BindingFlags.NonPublic | BindingFlags.Instance),
            hintBatch = typeof(ManHints).GetField("m_HintDefinitions", BindingFlags.NonPublic | BindingFlags.Instance),
            hintBatchCore = typeof(HintDefinitionList).GetField("m_HintsList", BindingFlags.NonPublic | BindingFlags.Instance);
        private static void AutoPopulateVanilla(Wiki wiki)
        {
            WikiPageGroup corps = new WikiPageGroup(wiki.ModID, "Corporations", CorpsSprite);
            for (int step = 0; step < Enum.GetValues(typeof(BlockTypes)).Length; step++)
            {
                if (ManSpawn.inst.IsBlockAllowedInLaunchedConfig((BlockTypes)step) &&
                    ManSpawn.inst.IsBlockAllowedInCurrentGameMode((BlockTypes)step) &&
                    //ManSpawn.inst.IsBlockUsageRestrictedInGameMode((BlockTypes)step) &&
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
            WikiPageGroup chunks = new WikiPageGroup(wiki.ModID, "Chunks", ChunksSprite);
            for (int step = 0; step < Enum.GetValues(typeof(ChunkTypes)).Length; step++)
            {
                if (!StringLookup.GetItemName(ObjectTypes.Chunk, step).StartsWith("ERROR:"))
                {
                    new WikiPageChunk(step, chunks);
                }
            }
            HashSet<Biome> biomesCollected = new HashSet<Biome>();
            BiomeGroup[] groups = (BiomeGroup[])Biomes.GetValue(ManWorld.inst.CurrentBiomeMap);
            for (int i = 0; i < groups.Length; i++)
            {
                BiomeGroup item = groups[i];
                //Debug_TTExt.Log("Biome Group " + item.name + " - Distance Multi " + item.WeightingByDistance);
                for (int j = 0; j < item.Biomes.Length; j++)
                {
                    Biome item2 = item.Biomes[j];
                    //Debug_TTExt.Log("  " + item2.name + " - Type " + item2.BiomeType + ", Weight " + item.BiomeWeights[j]);
                    if (!biomesCollected.Contains(item2))
                        biomesCollected.Add(item2);
                }
            }
            WikiPageGroup biomes = new WikiPageGroup(wiki.ModID, "Biomes", ScenerySprite);
            foreach (var biome in biomesCollected)
            {
                new WikiPageBiome(biome, biomes);
            }

            WikiPageGroup scenery = new WikiPageGroup(wiki.ModID, "Resources", ScenerySprite);
            for (int step = 0; step < Enum.GetValues(typeof(SceneryTypes)).Length; step++)
            {
                if (!StringLookup.GetItemName(ObjectTypes.Scenery, step).StartsWith("ERROR:"))
                {
                    new WikiPageScenery(step, scenery);
                }
            }
            List<ManHints.HintDefinition> lingo = (List<ManHints.HintDefinition>)hintBatchCore.GetValue(
                (HintDefinitionList)hintBatch.GetValue(ManHints.inst));
            foreach (var item in lingo)
            {
                if (item.m_HintMessage != null && item.m_HintMessage.IsValid)
                    InjectHint(VanillaGameName, "General", item.m_HintMessage.Value);
            }
            new WikiPageInfo(wiki.ModID, "Tools", UIHelpersExt.NullSprite, GUITools);
            
            InvokeHelper.Invoke(FinishVanillaWikiSearch, 3f);
        }
        private static void AutoPopulateWiki(Wiki wiki)
        {
            if (wiki.ModID == VanillaGameName)
            {
                AutoPopulateVanilla(wiki);
            }
            else
            {   // Modded
                var MC = wiki.modContainer;
                if (MC != null)
                {
                    var contents = MC.Contents;
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
                    var corps = contents.m_Corps;
                    if (corps != null && corps.Any())
                    {
                        WikiPageGroup corpsPage = (WikiPageGroup)GetPage("Corporations");
                        if (corpsPage == null)
                            corpsPage = new WikiPageGroup(MC.ModID, "Corporations", CorpsSprite);
                        foreach (var item in corps)
                        {
                            new WikiPageCorp(MC.ModID, (int)ManMods.inst.GetCorpIndex(item.m_ShortName), corpsPage);
                        }
                    }
                    if (TryGetModWiki(MC.ModID, out Wiki wiki2))
                    {
                        wiki2.MainPage.onOpen = wiki2.PrepareModded;
                        OnModWikiCreated.Send(wiki);
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

        public static WikiPage GetPage(string pageName)
        {
            try
            {
                return Wikis.Values.FirstOrDefault(x => x.ALLPages.ContainsKey(pageName))?.ALLPages[pageName];
            }
            catch
            {
                return null;
            }
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
        public static WikiPageChunk GetChunkPage(string chunkName)
        {
            try
            {
                return (WikiPageChunk)Wikis.Values.FirstOrDefault(x => x.ALLPages.ContainsKey(chunkName))?.ALLPages[chunkName];
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
        public static Dictionary<string, Wiki> AllWikis => Wikis;

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
                    if (value != null)
                        value.OnBeforeDisplay();
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
                    try
                    {
                        WikiButton.SetToggleState(open);
                    }
                    catch { }
                }
            }
            public void Update()
            {
                if (Input.GetKeyDown(WikiButtonKeybind))
                    ToggleGUI();
                if (renderedIcon > 0)
                    renderedIcon--;
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

            //tooltip.EndDisplayGUIToolTip();
            GUI.DragWindow();
        }


        internal static void ToggleGUI()
        {
            InitWiki();
            instGUI.ToggleGUI();
        }
        internal static void SetGUI(bool state)
        {
            InitWiki();
            instGUI.SetGUI(state);
        }
        internal static void CloseGUI()
        {
            InitWiki();
            instGUI.SetGUI(false);
            ReleaseAsMuchMemoryAsPossible();
        }





        private static Texture2D HintIcon;
        private static Texture2D HintIconBlack;
        private static ManToolbar.ToolbarToggle WikiButton = new ManToolbar.ToolbarToggle("In-Game Wiki", 
            InfoSprite, SetGUI);
        internal static void InitWiki()
        {
            _ = InfoSprite;

            if (instGUI == null)
            {
                instGUI = new GameObject("ManIngameWiki").AddComponent<GUIInst>();
                LoadPage();
                Debug_TTExt.Log("TerraTechETCUtil: InitWiki()");
                //WikiPageBlock.GetImagesForPending();
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
            
            if (inst.TryLoadFromDisk(ref inst) && 
                !inst.wikiID.NullOrEmpty() && Wikis.TryGetValue(inst.wikiID, out var wiki))
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
