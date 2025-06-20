using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using UnityEngine;
using static LocalisationEnums;
using static VectorLineRenderer;

namespace TerraTechETCUtil
{
    public class ManIngameWiki : TinySettings
    {
        public static Event<Wiki> OnModWikiCreated = new Event<Wiki>();
        private static byte renderedIcon = 0;
        public static HashSet<string> ModulesOpened = new HashSet<string>();

        public static bool DisplayingNonEnglish => Localisation.inst.CurrentLanguage != Languages.English &&
            Localisation.inst.CurrentLanguage != Languages.US_English;

        public static Sprite WikiSprite { get; } = ResourcesHelper.GetTexture2DFromBaseGameAllFast("ICON_SEE_BLOCKS").ConvertToSprite();
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
        public static Sprite ScenerySprite => ManUI.inst.GetSprite(ObjectTypes.Block, (int)BlockTypes.SPEXmasTree_353);

        public static Sprite ToolsSprite { get; } = ResourcesHelper.GetTexture2DFromBaseGameAllFast("Icon_RecipesMenu_01_White").ConvertToSprite();
        public static Sprite BiomesSprite { get; } = ResourcesHelper.GetTexture2DFromBaseGameAllFast("NewGame_CreateAWorld").ConvertToSprite();

        public static void ApplyNewWikiBlockDescOverride(string typeName, string newName)
        {
            AutoDataExtractor.SpecialNames[typeName] = newName;
        }
        public static void IgnoreWikiBlockExtModule<T>() where T : ExtModule
        {
            AutoDataExtractor.ignoreModuleTypes.Add(typeof(T));
        }
        public static void RecurseCheckWikiBlockExtModule<T>()
        {
            ModuleInfo.AllowedTypesUIWiki.Add(typeof(T));
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

        public struct WikiIconInfo
        {
            Sprite Icon;
            string Description;
            public WikiIconInfo(Sprite icon, string description)
            {
                Icon = icon;
                Description = description;
            }
            public void OnGUI(GUIStyle style)
            {
                if (Icon == null)
                {
                    GUILayout.Button("Error", AltUI.ButtonGrey);
                    AltUI.Tooltip.GUITooltip("Icon Missing");
                }
                else
                {
                    AltUI.SpriteButton(Icon, style, GUILayout.Height(28), GUILayout.Width(28));
                    AltUI.Tooltip.GUITooltip(Description);
                }
            }
            public bool OnGUILarge(GUIStyle style, GUIStyle styleText)
            {
                if (Icon == null)
                {
                    GUILayout.BeginHorizontal(AltUI.ButtonGrey);
                    GUILayout.Label("Error", styleText);
                    GUILayout.EndHorizontal();
                    AltUI.Tooltip.GUITooltip("Icon Missing");
                    return false;
                }
                else
                {
                    GUILayout.BeginHorizontal(style);
                    AltUI.Sprite(Icon, AltUI.TRANSPARENT, GUILayout.Height(32), GUILayout.Width(32));
                    GUILayout.Label(Description, styleText);
                    GUILayout.EndHorizontal();
                    return GUI.Button(GUILayoutUtility.GetLastRect(), string.Empty, AltUI.TRANSPARENT);
                }
            }
        }
        public struct WikiLink
        {
            public readonly WikiPage linked;
            public WikiLink(WikiPage theLink)
            { linked = theLink; }
            public bool OnGUI(GUIStyle style)
            {
                if (linked == null)
                {
                    GUILayout.Button("Error", AltUI.ButtonGrey);
                    AltUI.Tooltip.GUITooltip("Link Broken");
                    return false;
                }
                else
                {
                    if (linked.icon != null && linked.icon != UIHelpersExt.NullSprite)
                    {
                        bool clicked = AltUI.SpriteButton(linked.icon, style, GUILayout.Height(28), GUILayout.Width(28));
                        AltUI.Tooltip.GUITooltip(linked.displayName);
                        return clicked;
                    }
                    else
                    {
                        linked.GetIcon();
                        return GUILayout.Button(linked.displayName, style);
                    }
                }
            }
            public bool OnGUILarge(GUIStyle style, GUIStyle styleText)
            {
                if (linked == null)
                {
                    GUILayout.BeginHorizontal(AltUI.ButtonGrey);
                    GUILayout.Label("Error", styleText);
                    GUILayout.EndHorizontal();
                    AltUI.Tooltip.GUITooltip("Link Broken");
                    return false;
                }
                else
                {
                    GUILayout.BeginHorizontal(style);
                    if (linked.icon != null && linked.icon != UIHelpersExt.NullSprite)
                        AltUI.Sprite(linked.icon, AltUI.TRANSPARENT, GUILayout.Height(32), GUILayout.Width(32));
                    else
                        linked.GetIcon();
                    GUILayout.Label(linked.displayName, styleText);
                    GUILayout.EndHorizontal();
                    return GUI.Button(GUILayoutUtility.GetLastRect(), string.Empty, AltUI.TRANSPARENT);
                }
            }
        }

        public abstract class WikiPage : GUILayoutHelpers.SlowSortable
        {
            public readonly Wiki wiki;
            /// <summary>
            /// The ENGLISH title
            /// </summary>
            public readonly string title;
            public LocExtString titleLoc;
            public Sprite icon { get; protected set; } = null;
            public string displayName => titleLoc != null ? titleLoc.ToString() : title;
            public Action infoOverride = null;

            protected WikiPage(string ModID, LocExtString Title, Sprite Icon)
            {
                wiki = InsureWiki(ModID);
                title = Title.GetEnglish();
                titleLoc = Title;
                icon = Icon;
                wiki.RegisterPage(this);
                //if (title == LOC_Hints.GetEnglish())
                //    Debug_TTExt.Assert("Added Hints");
            }
            protected WikiPage(string ModID, LocExtString Title, Sprite Icon, WikiPageGroup group)
            {
                wiki = InsureWiki(ModID);
                title = Title.GetEnglish();
                titleLoc = Title;
                icon = Icon;
                if (group != null)
                {
                    group.NestedPages.Add(this);
                    wiki.RegisterPage(this, false);
                }
                else
                    wiki.RegisterPage(this);
                //if (title == LOC_Hints.GetEnglish())
                //    Debug_TTExt.Assert("Added Hints");
            }
            protected WikiPage(string ModID, LocExtString Title, Sprite Icon, string WikiGroupName, Sprite IconGroup)
            {
                wiki = InsureWiki(ModID);
                title = Title.GetEnglish();
                titleLoc = Title;
                icon = Icon;
                if (wiki.RegisterPage(this, false))
                    InsureWikiGroup(wiki.ModID, WikiGroupName, IconGroup).NestedPages.Add(this);
                //if (title == LOC_Hints.GetEnglish())
                //    Debug_TTExt.Assert("Added Hints");
            }
            protected WikiPage(string ModID, LocExtString Title, Sprite Icon, LocExtString WikiGroupName, Sprite IconGroup)
            {
                wiki = InsureWiki(ModID);
                title = Title.GetEnglish();
                titleLoc = Title;
                icon = Icon;
                if (wiki.RegisterPage(this, false))
                    InsureWikiGroup(wiki.ModID, WikiGroupName, IconGroup).NestedPages.Add(this);
                //if (title == LOC_Hints.GetEnglish())
                //    Debug_TTExt.Assert("Added Hints");
            }

            protected WikiPage(string ModID, string Title, Sprite Icon)
            {
                wiki = InsureWiki(ModID);
                title = Title;
                icon = Icon;
                wiki.RegisterPage(this);
                //if (title == LOC_Hints.GetEnglish())
                //    Debug_TTExt.Assert("Added Hints");
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
                //if (title == LOC_Hints.GetEnglish())
                //    Debug_TTExt.Assert("Added Hints");
            }
            protected WikiPage(string ModID, string Title, Sprite Icon, string WikiGroupName, Sprite IconGroup)
            {
                wiki = InsureWiki(ModID);
                title = Title;
                icon = Icon;
                if (wiki.RegisterPage(this, false))
                    InsureWikiGroup(wiki.ModID, WikiGroupName, IconGroup).NestedPages.Add(this);
                //if (title == LOC_Hints.GetEnglish())
                //    Debug_TTExt.Assert("Added Hints");
            }
            protected WikiPage(string ModID, string Title, Sprite Icon, LocExtString WikiGroupName, Sprite IconGroup)
            {
                wiki = InsureWiki(ModID);
                title = Title;
                icon = Icon;
                if (wiki.RegisterPage(this, false))
                    InsureWikiGroup(wiki.ModID, WikiGroupName, IconGroup).NestedPages.Add(this);
                //if (title == LOC_Hints.GetEnglish())
                //    Debug_TTExt.Assert("Added Hints");
            }

            /// <summary>
            /// The GUI call for generating it's button on the left sidebar
            /// </summary>
            public abstract void DisplaySidebar();
            /// <summary>
            /// USE AT YOUR OWN RISK!
            ///   Displays the wiki page contents in whatever OnGUI() window system you call this in.
            /// </summary>
            public void ExternalDisplayGUI()
            {
                InsureUpdateGameModeSwitch();
                if (infoOverride != null)
                    infoOverride();
                else
                    DisplayGUI();
            }
            internal void DoDisplayGUI()
            {
                if (infoOverride != null)
                    infoOverride();
                else
                    DisplayGUI();
            }
            /// <summary>
            /// All of the contents of the WikiPage to display context of
            /// </summary>
            protected virtual void DisplayGUI()
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("This wiki is", AltUI.LabelBlackTitle);
                GUILayout.Label("UNDER CONSTRUCTION", AltUI.LabelBlueTitle);
                GUILayout.Label("(Mostly) English only!", AltUI.LabelBlackTitle);
                GUILayout.FlexibleSpace();
            }
            /// <summary>
            /// A call to cache the icon that represents this page
            /// </summary>
            public abstract void GetIcon();
            private static List<MonoBehaviour> wasActiveMB = new List<MonoBehaviour>();

            /// <summary>
            /// Brute-force generates an icon image for the given GameObject using the game's camera.  
            ///   Using this is not advised.
            /// </summary>
            /// <param name="GO">The GameObject to generate a icon for.</param>
            /// <param name="removalCallback">Called after the icon image was taken for cleanup.</param>
            /// <exception cref="OperationCanceledException"></exception>
            public void GetIconImage(GameObject GO, Action removalCallback)
            {
                try
                {
                    try
                    {
                        foreach (var MB in GO.GetComponentsInChildren<MonoBehaviour>(true))
                        {
                            if (MB.enabled)
                            {
                                MB.enabled = false;
                                wasActiveMB.Add(MB);
                            }
                        }
                        bool wasActive = GO.activeSelf;
                        if (!wasActive)
                            GO.SetActive(true);
                        Vector3 scenePos = GO.transform.position;
                        Bounds approxBounds = new Bounds(scenePos + new Vector3(0, 0, 0), new Vector3(2, 2, 2));
                        //Bounds approxBounds = default;

                        foreach (var item in GO.GetComponentsInChildren<Collider>(true))
                        {
                            Bounds bounds = default;
                            bounds = item.bounds;
                            /*
                            if (item is BoxCollider BC)
                                bounds = new Bounds(BC.center, BC.size);
                            else if (item is CapsuleCollider CC)
                                bounds = new Bounds(CC.center, Vector3.one * ((CC.radius + CC.height) * 2));
                            else if (item is SphereCollider SC)
                                bounds = new Bounds(SC.center, Vector3.one * (SC.radius * 2));
                            else
                                bounds = new Bounds(new Vector3(0, 3, 0), new Vector3(4, 6, 4));
                           // */
                            /*
                             if (item is BoxCollider BC)
                                 bounds = new Bounds(new Vector3(0, 3, 0), BC.size);
                             else if (item is CapsuleCollider CC)
                                 bounds = new Bounds(new Vector3(0, 3, 0), Vector3.one * ((CC.radius + CC.height) * 2));
                             else if (item is SphereCollider SC)
                                 bounds = new Bounds(new Vector3(0, 3, 0), Vector3.one * (SC.radius * 2));
                             else
                                 bounds = new Bounds(new Vector3(0, 3, 0), new Vector3(4, 6, 4));
                             // */
                            if (approxBounds == default)
                                approxBounds = bounds;
                            else
                                approxBounds.Encapsulate(bounds);
                        }

                        if (approxBounds == default)
                            approxBounds = new Bounds(scenePos + new Vector3(0, 0, 0), new Vector3(2, 2, 2));
                        approxBounds.center = new Vector3(0, approxBounds.center.y - scenePos.y, 0);
                        InvokeHelper.Invoke(() =>
                        {
                            ResourcesHelper.GeneratePreviewForGameObjectOnSite((Texture2D tex) =>
                            {
                                icon = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.zero);
                                if (!wasActive)
                                    GO.SetActive(false);
                                foreach (var MB in GO.GetComponentsInChildren<MonoBehaviour>(true))
                                {
                                    if (wasActiveMB.Remove(MB))
                                        MB.enabled = true;
                                }
                                removalCallback.Invoke();
                            }, GO, approxBounds,
                            new Vector3(0.73f, 0.25f, 0.73f).normalized * approxBounds.size.magnitude,
                            new List<Globals.ObjectLayer>());
                        }, 0.1f);
                    }
                    catch (Exception e)
                    {
                        foreach (var MB in wasActiveMB)
                            MB.enabled = true;
                        wasActiveMB.Clear();
                        icon = UIHelpersExt.NullSprite;
                        removalCallback.Invoke();
                        Debug_TTExt.Log("Crash while handling" +
                            " GameObject \"" + (GO.name.NullOrEmpty() ? "<NULL>" : GO.name) +
                            "\" in the spriter, but we failed. " + e);
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
            /// <summary>
            /// Iterate EVERY page of the same type to change or collect data from.  Slow!
            /// </summary>
            /// <typeparam name="T">The type of page to iterate for</typeparam>
            /// <returns></returns>
            public static IEnumerable<T> IterateForInfo<T>() where T : WikiPage
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
            /// <summary>
            /// Called before the page is displayed to the user.  Useful for last-second actions!
            /// </summary>
            public virtual void OnBeforeDisplay() { }
            /// <summary>
            /// Called when the game switched Modes.  
            ///   Only updates immedeately if the Wiki is open or when the Wiki is opened.
            /// </summary>
            /// <param name="mode"></param>
            public virtual void OnGameModeSwitch(Mode mode) { }
            /// <summary>
            /// Called when the whole Wiki is closed.  
            ///   Useful to save memory.
            /// </summary>
            /// <returns></returns>
            public abstract bool OnWikiClosed();

            /// <summary>
            /// Replace a target wiki page
            /// </summary>
            /// <param name="replacement">the page to replace the targeted with</param>
            /// <exception cref="InvalidOperationException"></exception>
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
                GUILayout.Label(displayName, AltUI.LabelWhite, GUILayout.Height(35));
                bool selected = false;
                if (icon && AltUI.Button(icon, ManSFX.UISfxType.Select, AltUI.LabelWhite, GUILayout.Height(35), GUILayout.Width(35)))
                    selected = true;
                GUILayout.EndHorizontal();
                Rect lastRect = GUILayoutUtility.GetLastRect();
                if (GUI.Button(lastRect, string.Empty, AltUI.TRANSPARENT) || selected)
                {
                    ManSFX.inst.PlayUISFX(ManSFX.UISfxType.Select);
                    GoHere();
                }
            }
            protected void ButtonGUIDispLateIcon()
            {
                if (icon == null && renderedIcon == 0)
                {
                    renderedIcon = 14;
                    GetIcon();
                }
                GUILayout.BeginHorizontal(CurrentWikiPage == this ? AltUI.ButtonGreen : AltUI.ButtonBlue, GUILayout.Height(35));
                GUILayout.Label(displayName, AltUI.LabelWhite, GUILayout.Height(35));
                bool selected = false;
                if (icon)
                {
                    if (AltUI.Button(icon, ManSFX.UISfxType.Select, AltUI.LabelWhite, GUILayout.Height(35), GUILayout.Width(35)))
                        selected = true;
                }
                GUILayout.EndHorizontal();
                Rect lastRect = GUILayoutUtility.GetLastRect();
                if (GUI.Button(lastRect, string.Empty, AltUI.TRANSPARENT) || selected)
                {
                    ManSFX.inst.PlayUISFX(ManSFX.UISfxType.Select);
                    GoHere();
                }
            }

            public void GoBack()
            {
                if (backtrace.Any())
                {
                    WikiPage WP = backtrace.Last();
                    if (curWikiPage != WP)
                        WP.OnBeforeDisplay();
                    curWikiPage = WP;
                    backtrace.RemoveAt(backtrace.Count - 1);
                }
                else
                {
                    if (wiki.MainPage == null)
                    {
                        DefaultWikiPage.OnBeforeDisplay();
                        curWikiPage = DefaultWikiPage;
                    }
                    else
                    {
                        wiki.MainPage.OnBeforeDisplay();
                        CurrentWikiPage = wiki.MainPage;
                    }
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
            public Func<WikiPage, bool> PageHunterPostQuery;
            public GUILayoutHelpers.SlowSorter<WikiPage> PageHunter;
            public Action onOpen = null;
            public bool open = false;
            public WikiPageGroup(string ModID, LocExtString Title, Sprite Icon = null, WikiPageGroup Group = null, Func<WikiPage, bool> preSearchQuery = null) : base(ModID, Title, Icon, Group)
            {
                PageHunterPostQuery = preSearchQuery;
            }
            public WikiPageGroup(string ModID, string Title, Sprite Icon = null, WikiPageGroup Group = null, Func<WikiPage, bool> preSearchQuery = null) : base(ModID, Title, Icon, Group)
            {
                PageHunterPostQuery = preSearchQuery;
            }
            public override void OnGameModeSwitch(Mode mode)
            {
                if (PageHunter != null)
                    PageHunter.SetNewSearchQueryIfNeeded(searchQuery, true);
            }
            public override void GetIcon() { }
            public override void DisplaySidebar()
            {
                GUILayout.BeginHorizontal(open ? AltUI.ButtonBlueActive : AltUI.ButtonBlue, GUILayout.Height(35));
                GUILayout.Label(displayName, AltUI.LabelWhite, GUILayout.Height(35));
                bool selected = false;
                if (icon)
                    selected = AltUI.Button(icon, ManSFX.UISfxType.Select, AltUI.LabelWhite, GUILayout.Height(35), GUILayout.Width(35));
                GUILayout.EndHorizontal();
                Rect lastRect = GUILayoutUtility.GetLastRect();
                if (GUI.Button(lastRect, string.Empty, AltUI.TRANSPARENT) || selected)
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
                            PageHunter = new GUILayoutHelpers.SlowSorter<WikiPage>(32, PageHunterPostQuery);
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
            public override bool OnWikiClosed()
            {
                return false;
            }
        }
        public class WikiPageMainDefault : WikiPage
        {
            public WikiPageMainDefault(string ModID, LocExtString Title, Sprite Icon = null) : base(ModID, Title, Icon, null)
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
            public override bool OnWikiClosed()
            {
                return false;
            }
            public override void GetIcon() { }
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
            internal bool RegisterPage(WikiPage page, bool rootLevel = true, bool throwOnReplaceAttempt = false)
            {
                if (ALLPages.TryGetValue(page.title, out var pageInst))
                {
                    if (throwOnReplaceAttempt)
                        throw new InvalidOperationException("(throwOnReplaceAttempt) ~ Cannot add two pages of the exact same title to a wiki!");
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

        private static bool TryCleanupHint(string item, string name, int ID)
        {
            if (item.StartsWith(name))
            {
                InjectHint(VanillaGameName, name, new LocExtStringVanilla(name, StringBanks.LoadingHints, ID));//item.Replace(name, "")
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
                for (int i = 0; i < banks.Length; i++)
                {
                    string item = banks[i];
                    if (!item.NullOrEmpty())
                    {
                        string itemCleaned = item.Replace(
                            "<color=#e4ac41ff><size=32><b><i>", "").Replace(
                            "</i></b></size></color>\n", "");
                        if (TryCleanupHint(itemCleaned, "GENERAL HINT", i) ||
                            TryCleanupHint(itemCleaned, "MULTIPLAYER HINT", i) ||
                            TryCleanupHint(itemCleaned, "GAUNTLET MODE", i) ||
                            TryCleanupHint(itemCleaned, "CREATIVE MODE HINT", i) ||
                            TryCleanupHint(itemCleaned, "CAMPAIGN HINT", i))
                        {
                        }
                        else
                            InjectHint(VanillaGameName, "Other", new LocExtStringVanilla(itemCleaned, StringBanks.LoadingHints, i));
                    }
                }
            }
        }

        private static void GUITools()
        {
            if (ActiveGameInterop.inst)// && ActiveGameInterop.IsReady)
            {
                if (!DebugExtUtilities.AllowEnableDebugGUIMenu_KeypadEnter)
                    DebugExtUtilities.AllowEnableDebugGUIMenu_KeypadEnter = true;
                if (AltUI.Button("Unhook from Editor", ManSFX.UISfxType.Close, AltUI.ButtonBlueLarge))
                    ActiveGameInterop.DeInitBothEnds();
            }/*
            else if (ActiveGameInterop.inst)
            {
                if (GUILayout.Button("Waiting For Editor...", AltUI.ButtonGreyLarge))
                    ActiveGameInterop.DeInitJustThisSide();
                GUILayout.Label("You might have to hover your mouse over UnityEditor to get it to update");
            }*/
            else if (AltUI.Button("Try Hook To UnityEditor", ManSFX.UISfxType.Open, AltUI.ButtonOrangeLarge))
            {
                ActiveGameInterop.Init();
                InvokeHelper.InvokeSingleRepeat(ActiveGameInterop.UpdateNow, 1f);
            }
            if (ActiveGameInterop.inst)
            {
                GUILayout.BeginHorizontal();
                if (AltUI.Button("Update", ManSFX.UISfxType.EarnXP, AltUI.ButtonGreen))
                    ActiveGameInterop.UpdateNow();
                if (ActiveGameInterop.IsReceiving)
                    GUILayout.Label("Receiving");
                else
                    GUILayout.Label("Transmitting");
                GUILayout.EndHorizontal();
                if (ActiveGameInterop.IsReady && AltUI.Button("Say Whoa", ManSFX.UISfxType.MissionLog, AltUI.ButtonBlue))
                    ActiveGameInterop.TryTransmitTest("Whoa");
                GUILayout.Label(ActiveGameInterop._debug, AltUI.TextfieldBlackHuge);
            }

            if (ManDLC.inst.HasAnyDLCOfType(ManDLC.DLCType.RandD))
            {
                if (DebugExtUtilities.AllowEnableDebugGUIMenu_KeypadEnter &&
                AltUI.Button("Open Mod Developer Tools", ManSFX.UISfxType.Enter, AltUI.ButtonOrangeLarge))
                    DebugExtUtilities.Open();
            }
            if (inst.enabledJSONExport)
            {
                if (AltUI.Button("Showing JSON Exporter Options", ManSFX.UISfxType.Button, AltUI.ButtonBlueActive))
                    inst.enabledJSONExport = false;
            }
            else if (AltUI.Button("Show JSON Exporter Options", ManSFX.UISfxType.Button, AltUI.ButtonBlue))
                inst.enabledJSONExport = true;
            AltUI.Tooltip.GUITooltip("This is EXPERIMENTAL. It is WIP and not guarenteed to work properly!\nUse Misc Mods on the Steam Workshop. It is advised over this.");

            GUILayout.FlexibleSpace();
        }

        internal static FieldInfo
            Biomes = typeof(BiomeMap).GetField("m_BiomeGroups", BindingFlags.NonPublic | BindingFlags.Instance),
            hintBatch = typeof(ManHints).GetField("m_HintDefinitions", BindingFlags.NonPublic | BindingFlags.Instance),
            hintBatchCore = typeof(HintDefinitionList).GetField("m_HintsList", BindingFlags.NonPublic | BindingFlags.Instance);

        public static LocExtStringMod LOC_Blocks = new LocExtStringMod(new Dictionary<Languages, string>
            {{ Languages.US_English, "Blocks" },
            {Languages.Japanese, "ブロック" }});
        public static LocExtStringMod LOC_Corps = new LocExtStringMod(new Dictionary<Languages, string>
            {{ Languages.US_English, "Corporations" },
            {Languages.Japanese, "企業" }});
        public static LocExtStringMod LOC_Chunks = new LocExtStringMod(new Dictionary<Languages, string>
            {{ Languages.US_English, "Chunks" },
            {Languages.Japanese, "資源" }});
        public static LocExtStringMod LOC_Scenery = new LocExtStringMod(new Dictionary<Languages, string>
            {{ Languages.US_English, "Resources" },
            {Languages.Japanese, "木や石 鉱石 等" }});
        public static LocExtStringMod LOC_Biomes = new LocExtStringMod(new Dictionary<Languages, string>
            {{ Languages.US_English, "Biomes" },
            {Languages.Japanese, "バイオーム" }});
        public static LocExtStringMod LOC_General = new LocExtStringMod(new Dictionary<Languages, string>
            {{ Languages.US_English, "General" },
            {Languages.Japanese, "全般" }});
        public static LocExtStringMod LOC_Combat = new LocExtStringMod(new Dictionary<Languages, string>
            {{ Languages.US_English, "Combat" },
            {Languages.Japanese, "戦闘" }});
        public static LocExtStringMod LOC_Tools = new LocExtStringMod(new Dictionary<Languages, string>
            {{ Languages.US_English, "Tools" },
            {Languages.Japanese, "道具" }});
        public static LocExtStringMod LOC_Hints = new LocExtStringMod(new Dictionary<Languages, string>
            {{ Languages.US_English, "Hints" },
            {Languages.Japanese, "ヒント" }});
        private static void AutoPopulateVanilla(Wiki wiki)
        {
            Sprite specialIcon;
            WikiPageGroup corps = new WikiPageGroup(wiki.ModID, LOC_Corps, CorpsSprite);
            WikiPageGroup blocks = new WikiPageGroup(wiki.ModID, LOC_Blocks, BlocksSprite, null, WikiPageBlock.ValidBlockSearchQuery);
            for (int step = 0; step < Enum.GetValues(typeof(BlockTypes)).Length; step++)
            {
                BlockTypes BT = (BlockTypes)step;
                if (ManSpawn.inst.IsBlockAllowedInLaunchedConfig(BT) &&
                     ManSpawn.inst.GetCategory(BT) != BlockCategories.Null &&
                    !wiki.ALLPages.ContainsKey(StringLookup.GetItemName(ObjectTypes.Block, step)))
                {
                    new WikiPageBlock(step, blocks);
                }
            }
            for (int step = 1; step < Enum.GetValues(typeof(FactionSubTypes)).Length; step++)
            {
                FactionSubTypes FST = (FactionSubTypes)step;
                if (!wiki.ALLPages.ContainsKey(FST.ToString() + "_corp"))
                {
                    new WikiPageCorp(wiki.ModID, step, corps);
                }
            }
            WikiPageGroup chunks = new WikiPageGroup(wiki.ModID, LOC_Chunks, ChunksSprite);
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
            WikiPageGroup biomes = new WikiPageGroup(wiki.ModID, LOC_Biomes, BiomesSprite);
            foreach (var biome in biomesCollected)
            {
                new WikiPageBiome(biome, biomes);
            }
            WikiPageGroup scenery = new WikiPageGroup(wiki.ModID, LOC_Scenery, ScenerySprite);
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
                    InjectHint(VanillaGameName, LOC_General, new LocExtStringVanillaText(item.m_HintMessage));
            }
            new WikiPageDamageStats(wiki.ModID, LOC_Combat, ManUI.inst.GetBlockCatIcon(BlockCategories.Weapons));
            new WikiPageInfo(wiki.ModID, LOC_Tools, ToolsSprite, GUITools);

            ExtendedWiki.AutoPopulateWikiExtras(wiki);

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
                        WikiPageGroup blocks = new WikiPageGroup(wiki.ModID, LOC_Blocks, BlocksSprite, null, WikiPageBlock.ValidBlockSearchQuery);
                        foreach (var item in contents.m_Blocks)
                        {
                            if (!wiki.ALLPages.ContainsKey(item.m_BlockDisplayName))
                            {
                                new WikiPageBlock(ManMods.inst.GetBlockID(item.name), blocks);
                            }
                        }
                    }
                    var corps = contents.m_Corps;
                    if (corps != null && corps.Any())
                    {
                        WikiPageGroup corpsPage = (WikiPageGroup)GetPage(MC.ModID, LOC_Corps);
                        if (corpsPage == null)
                            corpsPage = new WikiPageGroup(MC.ModID, LOC_Corps, CorpsSprite);
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

        public static WikiPageGroup InsureWikiGroup(string ModID, LocExtString WikiGroupName, Sprite Icon = null)
        {
            if (ModID.NullOrEmpty())
                throw new NullReferenceException("ModID should NOT be null.  WHY IS IT NULL");
            if (WikiGroupName.ToString().NullOrEmpty())
                throw new NullReferenceException("WikiGroupName should NOT be null.  WHY IS IT NULL");
            Wiki wiki = InsureWiki(ModID);
            if (wiki.ALLPages.TryGetValue(WikiGroupName.GetEnglish(), out var page))
            {
                if (page is WikiPageGroup pageR2)
                    return pageR2;
                else
                    throw new InvalidOperationException("InsureWikiGroup tried to add a new entry to wiki \"" +
                        ModID + "\", but it was already taken by a non-group of the same name!\n" +
                        page.GetType().ToString() + ", name " + page.title);
            }
            else 
                return new WikiPageGroup(ModID, WikiGroupName, Icon);
        }
        public static WikiPageGroup InsureWikiGroup(string ModID, string WikiGroupName, Sprite Icon = null)
        {
            if (ModID.NullOrEmpty())
                throw new NullReferenceException("ModID should NOT be null.  WHY IS IT NULL");
            if (WikiGroupName.ToString().NullOrEmpty())
                throw new NullReferenceException("WikiGroupName should NOT be null.  WHY IS IT NULL");
            Wiki wiki = InsureWiki(ModID);
            if (wiki.ALLPages.TryGetValue(WikiGroupName, out var page))
            {
                if (page is WikiPageGroup pageR2)
                    return pageR2;
                else
                    throw new InvalidOperationException("InsureWikiGroup tried to add a new entry to wiki \"" +
                        ModID + "\", but it was already taken by a non-group of the same name!\n" + 
                        page.GetType().ToString() + ", name " + page.title);
            }
            else
                return new WikiPageGroup(ModID, WikiGroupName, Icon);
        }
        public static void InjectHint(string ModID, LocExtString Title, LocExtString hintLOC)
        {
            if (!ModID.NullOrEmpty())
            {
                Wiki wiki = InsureWiki(ModID);
                if (!Title.ToString().NullOrEmpty())
                {
                    if (wiki.ALLPages.TryGetValue(Title.GetEnglish(), out var page))
                    {
                        if (page is WikiPageHints pageHintsGet)
                        {
                            pageHintsGet.AddHint(hintLOC);
                        }
                        else
                            throw new InvalidOperationException("We tried to add a Hints page but something else in the wiki has taken the name of a different type " + page.GetType());
                    }
                    else
                    {
                        new WikiPageHints(ModID, Title, hintLOC);
                    }
                    return;
                }
            }
            throw new InvalidOperationException("ManIngameWiki.ReplaceDescription cannot be replaced because the ModID or title does not match");
        }
        public static void InjectHint(string ModID, string Title, LocExtString hintLOC)
        {
            if (!ModID.NullOrEmpty())
            {
                Wiki wiki = InsureWiki(ModID);
                if (!Title.ToString().NullOrEmpty())
                {
                    if (wiki.ALLPages.TryGetValue(Title, out var page))
                    {
                        if (page is WikiPageHints pageHintsGet)
                        {
                            pageHintsGet.AddHint(hintLOC);
                        }
                        else
                            throw new InvalidOperationException("We tried to add a Hints page but something else in the wiki has taken the name of a different type " + page.GetType());
                    }
                    else
                    {
                        new WikiPageHints(ModID, Title, hintLOC);
                    }
                    return;
                }
            }
            throw new InvalidOperationException("ManIngameWiki.ReplaceDescription cannot be replaced because the ModID or title does not match");
        }


        public static WikiPage GetPage(LocExtString pageName)
        {
            try
            {
                return Wikis.Values.FirstOrDefault(x => x.ALLPages.ContainsKey(pageName.GetEnglish()))?.ALLPages[pageName.GetEnglish()];
            }
            catch
            {
                return null;
            }
        }
        public static WikiPage GetPage(string ModID, LocExtString pageName)
        {
            try
            {
                return InsureWiki(ModID).ALLPages[pageName.GetEnglish()];
            }
            catch
            {
                return null;
            }
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
        public static WikiPage GetPage(string ModID, string pageName)
        {
            try
            {
                return InsureWiki(ModID).ALLPages[pageName];
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
        public static WikiPageCorp GetCorpPage(FactionSubTypes corp)
        {
            try
            {
                string searchKey = WikiPageCorp.GetShortName((int)corp) + "_corp";
                return (WikiPageCorp)Wikis.Values.FirstOrDefault(x => x.ALLPages.ContainsKey(searchKey))?.ALLPages[searchKey];
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

        private static LocExtStringMod LOC_WikiMainName = new LocExtStringMod(new Dictionary<Languages, string>
            {{ Languages.US_English, "Terra Tech Wiki V0.4" },
            {Languages.Japanese, "テラテックウィキ V0.4" }});
        private static WikiPage DefaultWikiPage = new WikiPageMainDefault(VanillaGameName,
            LOC_WikiMainName, WikiSprite);


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

        public static KeyCode WikiButtonKeybind = KeyCode.Slash;
        public bool sideOpen = true;
        public bool enabledJSONExport = false;
        public string wikiID;
        public string pageName;
        public float scrollY;
        public static Vector2 scrollSide;
        public static Vector2 scroll;
        public static bool ShowJSONExport => inst.enabledJSONExport;


        private static GUIInst instGUI;
        private static Rect guiWindow = new Rect(20, 20, 1000, 600);
        private const int ExtWikiID = 1002248;

        private static LocExtStringMod LOC_WikiTopName = new LocExtStringMod(new Dictionary<Languages, string>
            {{ Languages.US_English, "Mod Wiki" },
            {Languages.Japanese, "改造ウィキ" }});
        public class GUIInst : MonoBehaviour
        {
            /// <summary>
            /// To ONLY be set by GUIInst.SetGUI(bool state)!!!
            /// </summary>
            private bool _WikiWindowOpen = false;
            public bool WikiWindowOpen => _WikiWindowOpen;
            internal void ToggleGUI()
            {
                SetGUI(!_WikiWindowOpen);
            }
            internal void SetGUI(bool state)
            {
                if (state != _WikiWindowOpen)
                {
                    _WikiWindowOpen = state;
                    if (_WikiWindowOpen)
                    {
                        //ManModGUI.HideAllObstructingUI();
                        InsureUpdateGameModeSwitch();
                        ManSFX.inst.PlayUISFX(ManSFX.UISfxType.Open);
                        ManModGUI.AddEscapeableCallback(QuitWiki, false);
                    }
                    else
                    {
                        ManSFX.inst.PlayUISFX(ManSFX.UISfxType.Close);
                        ManModGUI.RemoveEscapeableCallback(QuitWiki, false);
                        OnWikiClosed();
                    }
                    try
                    {
                        WikiButton.SetToggleState(_WikiWindowOpen);
                    }
                    catch { }
                }
            }
            private void QuitWiki() => SetGUI(false);
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
                    if (_WikiWindowOpen)
                    {
                        guiWindow = AltUI.Window(ExtWikiID, guiWindow, GUILayouter, LOC_WikiTopName.ToString(), CloseGUI);
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
        private static LocExtStringMod LOC_WikiGoBack = new LocExtStringMod(new Dictionary<Languages, string>
            {{ Languages.US_English, "Go Back" },
            {Languages.Japanese, "ページに戻る" }});
        private static LocExtStringMod LOC_WikiContents = new LocExtStringMod(new Dictionary<Languages, string>
            {{ Languages.US_English, "Contents" },
            {Languages.Japanese, "目次" }});
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
                AltUI.Tooltip.GUITooltip(LOC_WikiGoBack.ToString());
            }
            if (GUILayout.Button(LOC_WikiContents.ToString(), inst.sideOpen ? AltUI.ButtonBlueLargeActive : AltUI.ButtonBlueLarge, GUILayout.Width(280)))
                inst.sideOpen = !inst.sideOpen;
            if (curWikiPage != null)
                GUILayout.Label(curWikiPage.displayName, AltUI.LabelBlackTitle);
            else
                GUILayout.Label(DefaultWikiPage.displayName, AltUI.LabelBlackTitle);
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
        private static void CloseGUI() => instGUI.SetGUI(false);




        private static Mode ModeSwitchDelta = null;
        private static Texture2D HintIcon;
        private static Texture2D HintIconBlack;
        private static LocExtStringMod wikiName = new LocExtStringMod( new Dictionary<Languages, string>
            {{ Languages.US_English, "In-Game Wiki" },
            {Languages.Japanese, "ゲーム内ウィキ" }}
        );
        private static ManToolbar.ToolbarToggle WikiButton = new ManToolbar.ToolbarToggle(wikiName, WikiSprite, SetGUI);
        internal static void InitWiki()
        {
            _ = InfoSprite;

            if (instGUI == null)
            {
                instGUI = new GameObject("ManIngameWiki").AddComponent<GUIInst>();
                LoadPage();
                Debug_TTExt.Log("TerraTechETCUtil: InitWiki()");
                ManGameMode.inst.ModeStartEvent.Subscribe(OnGameModeSwitch);
                //WikiPageBlock.GetImagesForPending();
            }
            //InvokeHelper.InvokeSingleRepeat(SpamIt, 0.1f);
            /*
            Singleton.Manager<ManUI>.inst.GetScreen(ManUI.ScreenType.WhatIsGrabIt);
            Singleton.Manager<ManUI>.inst.GoToScreen(ManUI.ScreenType.WhatIsGrabIt);
            InvokeHelper.InvokeSingleRepeat(SpamIt, 0.1f);
            */
        }
        internal static void InsureUpdateGameModeSwitch()
        {
            if (ModeSwitchDelta != null)
            {
                foreach (var item in AllWikis)
                {
                    foreach (var item2 in item.Value.ALLPages)
                    {
                        item2.Value.OnGameModeSwitch(ModeSwitchDelta);
                    }
                }
                ModeSwitchDelta = null;
            }
        }
        internal static void OnGameModeSwitch(Mode mode)
        {
            ModeSwitchDelta = mode;
            if (instGUI.WikiWindowOpen)
                InsureUpdateGameModeSwitch();
        }



        private static void OnWikiClosed()
        {
            bool released = false;
            foreach (var item in Wikis.Values)
            {
                foreach (var item2 in item.ALLPages.Values)
                {
                    if (item2.OnWikiClosed())
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
