using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json.Linq;
using Steamworks;
using UnityEngine;
using static LocalisationEnums;
using static TerraTechETCUtil.ManIngameWiki;
using static VectorLineRenderer;

namespace TerraTechETCUtil
{
    /// <summary>
    /// The modded ingame wiki manager.
    /// <para>Every <see cref="WikiPage"/> type has subscribe-able contents</para>
    /// <para>See <seealso cref="ExtendedWiki"/> for more options</para>
    /// </summary>
    public class ManIngameWiki : ITinySettings
    {
        /// <summary>
        /// Sent after the player has just opened the wiki
        /// </summary>
        public static EventNoParams OnWikiOpened = new EventNoParams();
        /// <summary>
        /// Sent after a wiki is initally created with the wiki that was created
        /// </summary>
        public static Event<Wiki> OnModWikiCreated = new Event<Wiki>();
        /// <summary>
        /// Sent in IMGUI for events that display custom UI elements on the wiki's top bar
        /// </summary>
        public static Event<Wiki> WikiTopBarEvent = new Event<Wiki>();
        private static byte renderedIcon = 0;

        /// <summary>
        /// The exact width of the sidebar.  Buttons placed there must respect this.
        /// </summary>
        public const int WikiSidebarWidth = 320;

        private static int openGetDataLayer1 = 0;
        internal static HashSet<string> ModulesOpened = new HashSet<string>();
        /// <summary>
        /// True if the language the user is using is not any form of English
        /// </summary>
        public static bool DisplayingNonEnglish => Localisation.inst.CurrentLanguage != Languages.English &&
            Localisation.inst.CurrentLanguage != Languages.US_English;

        /// <summary> Base-game sprite </summary>
        public static Sprite WikiSprite { get; } = ResourcesHelper.GetTexture2DFromBaseGameAllFast("ICON_SEE_BLOCKS").ConvertToSprite();
        private static Sprite infoSprite;
        /// <summary> Base-game sprite </summary>
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
        /// <summary> Base-game sprite </summary>
        public static Sprite CorpsSprite => ManUI.inst.GetModernCorpIcon(FactionSubTypes.GSO);
        /// <summary> Base-game sprite </summary>
        public static Sprite ChunksSprite => ManUI.inst.GetSprite(ObjectTypes.Chunk, (int)ChunkTypes.Wood);
        /// <summary> Base-game sprite </summary>
        public static Sprite BlocksSprite => ManUI.inst.GetSprite(ObjectTypes.Block, (int)BlockTypes.GSOBlock_111);
        /// <summary> Base-game sprite </summary>
        public static Sprite ScenerySprite => ManUI.inst.GetSprite(ObjectTypes.Block, (int)BlockTypes.SPEXmasTree_353);

        /// <summary> Base-game sprite </summary>
        public static Sprite ToolsSprite { get; } = ResourcesHelper.GetTexture2DFromBaseGameAllFast("Icon_RecipesMenu_01_White").ConvertToSprite();
        /// <summary> Base-game sprite </summary>
        public static Sprite BiomesSprite { get; } = ResourcesHelper.GetTexture2DFromBaseGameAllFast("NewGame_CreateAWorld").ConvertToSprite();
        /// <summary>
        /// Optional description override for a block data field ONLY in the wiki.
        /// <para>Use a DocAttribute to automatically add a description in the JSON extraction data</para>
        /// </summary>
        /// <param name="typeName">The field name to replace, like <c>m_DamageableType</c></param>
        /// <param name="newName">The field name to display in the wiki</param>
        public static void ApplyNewWikiBlockDescOverride(string typeName, string newName)
        {
            AutoDataExtractor.SpecialNames[typeName] = newName;
        }
        /// <summary>
        /// Hides the ExtModule given in type <c>T</c> in the wiki
        /// </summary>
        /// <typeparam name="T">The Type to ignore</typeparam>
        public static void IgnoreWikiBlockExtModule<T>() where T : ExtModule
        {
            AutoDataExtractor.ignoreModuleTypes.Add(typeof(T));
        }
        /// <summary>
        /// Allow an odd type to be displayed on the wiki, not guaranteed to work on all types.
        /// </summary>
        /// <typeparam name="T">The type to permit displaying</typeparam>
        public static void RecurseCheckWikiBlockExtModule<T>()
        {
            ModuleInfo.AllowedTypesUIWiki.Add(typeof(T));
        }
        /// <summary>
        /// Replace the description in a target wiki page 
        /// </summary>
        /// <param name="ModID">The wiki of the wiki page to target</param>
        /// <param name="Title">The <b>ENGLISH</b> title of the wiki page to replace</param>
        /// <param name="replacement">The description GUI replacement</param>
        /// <exception cref="InvalidOperationException">Target page does not exist</exception>
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

        /// <summary>
        /// Used to display an icon in IMGUI with a hover description
        /// </summary>
        public struct WikiIconInfo
        {
            readonly Sprite Icon;
            readonly string Description;
            /// <summary>
            /// Create a new WikiIconInfo to display information in IMGUI for a wiki page in the context of
            /// a different wiki page.
            /// </summary>
            /// <param name="icon">The icon to display. Icon should not be null but it will handle null incase it is ever null</param>
            /// <param name="description">The description to display when hovering over this</param>
            public WikiIconInfo(Sprite icon, string description)
            {
                Icon = icon;
                Description = description;
            }
            /// <summary>
            /// Display this in the GUI
            /// <para><b>Use only in OnGUI() IMGUI calls</b></para>
            /// </summary>
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
            /// <summary>
            /// Display this BIG in the GUI.
            /// <para>text [IMAGE]</para>
            /// <para><b>Use only in OnGUI() IMGUI calls</b></para>
            /// </summary>
            public bool OnGUILarge(GUIStyle style, GUIStyle styleText)
            {
                if (Icon == null)
                {
                    GUILayout.BeginHorizontal(AltUI.ButtonGrey);
                    GUILayout.Label("Error", styleText);
                    GUILayout.EndHorizontal();
                    AltUI.Tooltip.GUITooltip(AltUI.EnemyString("Icon Missing"));
                    return false;
                }
                else
                {
                    GUILayout.BeginHorizontal(style);
                    GUILayout.Label(Description, styleText);
                    AltUI.Sprite(Icon, AltUI.TRANSPARENT, GUILayout.Height(32), GUILayout.Width(32));
                    GUILayout.EndHorizontal();
                    return GUI.Button(GUILayoutUtility.GetLastRect(), string.Empty, AltUI.TRANSPARENT);
                }
            }/// <summary>
             /// Display this BIG in the GUI inverted.
             /// <para>[IMAGE] text</para>
             /// <para><b>Use only in OnGUI() IMGUI calls</b></para>
             /// </summary>
            public bool OnGUILargeInv(GUIStyle style, GUIStyle styleText)
            {
                if (Icon == null)
                {
                    GUILayout.BeginHorizontal(AltUI.ButtonGrey);
                    GUILayout.Label("Error", styleText);
                    GUILayout.EndHorizontal();
                    AltUI.Tooltip.GUITooltip(AltUI.EnemyString("Icon Missing"));
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
        /// <summary>
        /// Used to display a wiki link in IMGUI with a hover description
        /// </summary>
        public struct WikiLink
        {
            /// <summary>
            /// The linked WikiPage
            /// </summary>
            public readonly WikiPage linked;
            /// <summary>
            /// Create a new WikiIconInfo to display information in IMGUI for a wiki page in the context of
            /// a different wiki page.
            /// </summary>
            /// <param name="theLink">The wiki page to display and link to. Icon will be displayed if present or text otherwise</param>
            public WikiLink(WikiPage theLink)
            { linked = theLink; }
            /// <summary>
            /// Display this in the GUI
            /// <para><b>Use only in OnGUI() IMGUI calls</b></para>
            /// </summary>
            public bool OnGUI(GUIStyle style)
            {
                if (linked == null)
                {
                    GUILayout.Button("<BROKEN LINK>", AltUI.ButtonGrey);
                    AltUI.Tooltip.GUITooltip(AltUI.EnemyString("Wiki page is missing!"));
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
            /// <summary>
            /// Display this BIG in the GUI.
            /// <para>text [IMAGE]</para>
            /// <para><b>Use only in OnGUI() IMGUI calls</b></para>
            /// </summary>
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
                    GUILayout.Label(linked.displayName, styleText);
                    if (linked.icon != null && linked.icon != UIHelpersExt.NullSprite)
                        AltUI.Sprite(linked.icon, AltUI.TRANSPARENT, GUILayout.Height(32), GUILayout.Width(32));
                    else
                        linked.GetIcon();
                    GUILayout.EndHorizontal();
                    return GUI.Button(GUILayoutUtility.GetLastRect(), string.Empty, AltUI.TRANSPARENT);
                }
            }
            /// <summary>
            /// Display this BIG in the GUI inverted.
            /// <para>[IMAGE] text</para>
            /// <para><b>Use only in OnGUI() IMGUI calls</b></para>
            /// </summary>
            public bool OnGUILargeInv(GUIStyle style, GUIStyle styleText)
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
        /// <summary>
        /// The basis for any wiki page in the ManIngameWiki system
        /// </summary>
        public abstract class WikiPage : GUILayoutHelpers.ISlowSortable
        {
            /// <summary>
            /// Wiki this is located in
            /// </summary>
            public readonly Wiki wiki;
            /// <summary>
            /// The ENGLISH title
            /// </summary>
            public readonly string title;
            /// <summary>
            /// The title that is shown on the ui relitive to the user's settings
            /// </summary>
            public LocExtString titleLoc;
            /// <summary>
            /// Icon to display.  Can be left null to use the name only for content links
            /// </summary>
            public Sprite icon { get; protected set; } = null;
            /// <summary>
            /// The name the page displays on the Wiki UI
            /// </summary>
            public string displayName => titleLoc != null ? titleLoc.ToString() : title;
            /// <summary>
            /// Override action to replace the entire contents of the page
            /// </summary>
            public Action infoOverride = null;

            /// <summary>
            /// Creates a WikiPage.
            /// <para>If needed this automatically creates the wiki it is targeting.</para>
            /// <para><b>Does not insure and assign itself to a group!</b></para>
            /// </summary>
            /// <param name="ModID">Mod ID to make under.</param>
            /// <param name="Title">Title to display for the page</param>
            /// <param name="Icon">Optional icon to display on the page</param>
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
            /// <summary>
            /// Creates a WikiPage.
            /// <para>If needed this automatically creates the wiki it is targeting.</para>
            /// <para><b>Does not insure and assign itself to a group!</b></para>
            /// </summary>
            /// <param name="ModID">Mod ID to make under.</param>
            /// <param name="Title">Title to display for the page</param>
            /// <param name="Icon">Optional icon to display on the page</param>
            /// <param name="group">The group to assign to</param>
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
            /// <summary>
            /// Creates a WikiPage.
            /// <para>If needed this automatically creates the wiki it is targeting.</para>
            /// <para>Will automatically insure and assign itself to the given group without advanced options</para>
            /// </summary>
            /// <param name="ModID">Mod ID to make under.</param>
            /// <param name="Title">Title to display for the page</param>
            /// <param name="Icon">Optional icon to display on the page</param>
            /// <param name="WikiGroupName">The group insure and to assign to</param>
            /// <param name="IconGroup">The icon to assign to the group if it has to be made</param>
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
            /// <summary>
            /// Creates a WikiPage.
            /// <para>If needed this automatically creates the wiki it is targeting.</para>
            /// <para>Will automatically insure and assign itself to the given group without advanced options</para>
            /// </summary>
            /// <param name="ModID">Mod ID to make under.</param>
            /// <param name="Title">Title to display for the page</param>
            /// <param name="Icon">Optional icon to display on the page</param>
            /// <param name="WikiGroupName">The group insure and to assign to</param>
            /// <param name="IconGroup">The icon to assign to the group if it has to be made</param>
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

            /// <summary>
            /// Overridable custom creator for WikiGroup that overrides the constructor sequence entirely
            /// </summary>
            /// <param name="ModID">Mod ID to make under.</param>
            /// <param name="WikiGroupName">The group insure and to assign to</param>
            /// <param name="Icon">The icon to assign to the group if it has to be made</param>
            /// <returns>A WikiPageGroup that is either found or newly created.  Should never be null</returns>
            protected virtual WikiPageGroup InsureWikiGroup(string ModID, LocExtString WikiGroupName, Sprite Icon = null) => 
                InsureDefaultWikiGroup(ModID, WikiGroupName, Icon);
            /// <summary>
            /// Overridable custom creator for WikiGroup that overrides the constructor sequence entirely
            /// </summary>
            /// <param name="ModID">Mod ID to make under.</param>
            /// <param name="WikiGroupName">The group insure and to assign to</param>
            /// <param name="Icon">The icon to assign to the group if it has to be made</param>
            /// <returns>A WikiPageGroup that is either found or newly created.  Should never be null</returns>
            protected virtual WikiPageGroup InsureWikiGroup(string ModID, string WikiGroupName, Sprite Icon = null) =>
                InsureDefaultWikiGroup(ModID, WikiGroupName, Icon);

            /// <summary>
            /// Creates a WikiPage.
            /// <para>If needed this automatically creates the wiki it is targeting.</para>
            /// <para><b>Does not insure and assign itself to a group!</b></para>
            /// </summary>
            /// <param name="ModID">Mod ID to make under.</param>
            /// <param name="Title">Title to display for the page</param>
            /// <param name="Icon">Optional icon to display on the page</param>
            protected WikiPage(string ModID, string Title, Sprite Icon)
            {
                wiki = InsureWiki(ModID);
                title = Title;
                icon = Icon;
                wiki.RegisterPage(this);
                //if (title == LOC_Hints.GetEnglish())
                //    Debug_TTExt.Assert("Added Hints");
            }
            /// <summary>
            /// Creates a WikiPage.
            /// <para>If needed this automatically creates the wiki it is targeting.</para>
            /// <para><b>Does not insure and assign itself to a group!</b></para>
            /// </summary>
            /// <param name="ModID">Mod ID to make under.</param>
            /// <param name="Title">Title to display for the page</param>
            /// <param name="Icon">Optional icon to display on the page</param>
            /// <param name="group">The group to assign to</param>
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
            /// <summary>
            /// Creates a WikiPage.
            /// <para>If needed this automatically creates the wiki it is targeting.</para>
            /// <para>Will automatically insure and assign itself to the given group without advanced options</para>
            /// </summary>
            /// <param name="ModID">Mod ID to make under.</param>
            /// <param name="Title">Title to display for the page</param>
            /// <param name="Icon">Optional icon to display on the page</param>
            /// <param name="WikiGroupName">The group insure and to assign to</param>
            /// <param name="IconGroup">The icon to assign to the group if it has to be made</param>
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
            /// <summary>
            /// Creates a WikiPage.
            /// <para>If needed this automatically creates the wiki it is targeting.</para>
            /// <para>Will automatically insure and assign itself to the given group without advanced options</para>
            /// </summary>
            /// <param name="ModID">Mod ID to make under.</param>
            /// <param name="Title">Title to display for the page</param>
            /// <param name="Icon">Optional icon to display on the page</param>
            /// <param name="WikiGroupName">The group insure and to assign to</param>
            /// <param name="IconGroup">The icon to assign to the group if it has to be made</param>
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
                DoDisplayGUI();
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
            /// Call this before the page's data is needed - when the page is about to be accessed.
            /// <para><b>Automatically avoids recursive calling</b></para>
            /// <para>Aquire needed page data in this call. <b>Not called in OnGUI()</b></para>
            /// <para>Called always before <seealso cref="OnBeforeDisplay"/></para>
            /// </summary>
            public void RequestInstDataReady()
            {
                if (openGetDataLayer1 > 1)
                    return;
                openGetDataLayer1++;
                try
                {
                    OnBeforeDataRequested(openGetDataLayer1 == 1);
                }
                finally
                {
                    openGetDataLayer1--;
                }
            }
            /// <summary>
            /// Called before the page's data is needed - when the page is about to be accessed.
            /// <para>Aquire needed page data in this call. <b>Not called in OnGUI()</b></para>
            /// <para>Called always before <seealso cref="OnBeforeDisplay"/></para>
            /// </summary>
            protected abstract void OnBeforeDataRequested(bool getFullData);
            /// <summary>
            /// Called before the page is displayed to the user.  Useful for last-second actions!
            /// <para><seealso cref="OnBeforeDisplay"/> is called before this</para>
            /// </summary>
            public virtual void OnBeforeDisplay() { }
            /// <summary>
            /// Called when the game switched Modes.  
            ///   Only updates immedeately if the Wiki is open or when the Wiki is opened.
            /// </summary>
            /// <param name="mode"></param>
            public virtual void OnGameModeSwitch(Mode mode) { }
            /// <summary>
            /// Called when the whole Wiki is closed or when you need to unload the page after loading it through 
            /// <b>inst</b> for <see cref="WikiPage{T,V}"/>  
            ///   Useful to save memory.
            /// </summary>
            /// <returns>true if it deallocated something, otherwise <b>false</b></returns>
            public abstract bool OnWikiClosedOrDeallocateMemory();

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

            /// <summary>
            /// Replace the description in a target wiki page 
            /// </summary>
            /// <param name="ModID">The wiki of the wiki page to target</param>
            /// <param name="Title">The <b>ENGLISH</b> title of the wiki page to replace</param>
            /// <param name="replacement">The description GUI replacement</param>
            /// <exception cref="InvalidOperationException">Target page does not exist</exception>
            public static void ReplaceDescription(string ModID, string Title, Action replacement) =>
                ManIngameWiki.ReplaceDescription(ModID, Title, replacement);
            /// <summary>
            /// Replace the description <b>for this page specifically</b>
            /// </summary>
            /// <param name="replacement">The description GUI replacement</param>
            public void ReplaceDescription(Action replacement)
            {
                infoOverride = replacement;
            }

            /// <summary>
            /// A default option for DisplaySidebar() to display this page's button on the wiki sidebar
            /// </summary>
            protected void ButtonGUIDisp()
            {
                GUILayout.BeginHorizontal(CurrentWikiPage == this ? AltUI.ButtonGreen : AltUI.ButtonBlue,
                    GUILayout.Height(35), GUILayout.MaxWidth(WikiSidebarWidth));
                GUILayout.Label(displayName, AltUI.LabelWhite, GUILayout.Height(35), GUILayout.Width(35));
                GUILayout.FlexibleSpace();
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
            /// <summary>
            /// A default option for DisplaySidebar() to display this page's button on the wiki sidebar.
            /// <para>Use this if your icon needs to be generated at runtime when the wiki is opened later via call to <c>GetIcon()</c></para>
            /// </summary>
            protected void ButtonGUIDispLateIcon()
            {
                if (icon == null && renderedIcon == 0)
                {
                    renderedIcon = 14;
                    GetIcon();
                }
                GUILayout.BeginHorizontal(CurrentWikiPage == this ? AltUI.ButtonGreen : AltUI.ButtonBlue,
                    GUILayout.Height(35), GUILayout.MaxWidth(WikiSidebarWidth));
                GUILayout.Label(displayName, AltUI.LabelWhite, GUILayout.Height(35), GUILayout.Width(35));
                GUILayout.FlexibleSpace();
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

            /// <summary>
            /// Jump to thre previous page the user was looking at. Only useful in temporary browsing contexts
            /// </summary>
            public void GoBack()
            {
                try
                {
                    if (backtrace.Any())
                    {
                        WikiPage WP = backtrace.Last();
                        if (curWikiPage != WP)
                            WP.OnBeforeDisplay();
                        foretrace.Add(curWikiPage);
                        if (foretrace.Count > 64)
                            foretrace.RemoveAt(0);
                        curWikiPage = WP;
                        backtrace.RemoveAt(backtrace.Count - 1);
                    }
                    else
                    {
                        if (wiki?.MainPage == null)
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
                catch (Exception)
                {
                    DefaultWikiPage.OnBeforeDisplay();
                    curWikiPage = DefaultWikiPage;
                }
            }
            /// <summary>
            /// Jump to the next page the user was looking at BEFORE they went back. Only useful in temporary browsing contexts
            /// </summary>
            public void GoForward()
            {
                try
                {
                    if (foretrace.Any())
                    {
                        WikiPage WP = foretrace.Last();
                        if (curWikiPage != WP)
                            WP.OnBeforeDisplay();
                        if (curWikiPage != WP)
                        {
                            backtrace.Add(curWikiPage);
                            if (backtrace.Count > 64)
                                backtrace.RemoveAt(0);
                            if (WP != null)
                                WP.OnBeforeDisplay();
                        }
                        curWikiPage = WP;
                        foretrace.RemoveAt(foretrace.Count - 1);
                    }
                }
                catch (Exception)
                {
                    DefaultWikiPage.OnBeforeDisplay();
                    curWikiPage = DefaultWikiPage;
                }
            }
            /// <summary>
            /// Jump to this very page.  Useful for links.
            /// <para><b>Use <c>WikiLink</c> for this in GUI whenever possible instead of this!</b></para>
            /// </summary>
            public void GoHere()
            {
                CurrentWikiPage = this;
            }


            // UTILITIES
            /// <summary>
            /// Used for displaying the description for filters for the WikiPageGroupBase searchbar
            /// </summary>
            protected static StringBuilder additionalFilters = new StringBuilder();
            /// <summary>
            /// Use this to check for filters under a certain keyword.
            /// <para>The convention is: <b><c>[name]</c>:</b>(search term with no spaces)</para>
            /// </summary>
            /// <param name="toCheck">The current chunk of the search query, split by spaces</param>
            /// <param name="name">Where <b><c>[name]</c></b> is in the search filter name</param>
            /// <param name="queries">The output of each search term, split into multiple via '<c>,</c>'</param>
            /// <returns></returns>
            public static bool FilterPass(string toCheck, string name, ref string[] queries)
            {
                if (toCheck.StartsWith(name.ToLower()))
                {
                    queries = toCheck.Substring(name.Length).Split(',');
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// <inheritdoc cref="WikiPage"/>
        /// <para>The version which maintains the id for lookup and a cached instance of it</para>
        /// </summary>
        public abstract class WikiPage<T, V> : WikiPage
        {
            /// <summary>
            /// The main identification ID the game uses for this page.
            /// </summary>
            public readonly T ID;
            /// <summary>
            /// The main identification the game uses for this page.
            /// <para>Use <see cref="inst"/> to access this externally, otherwise use <see cref="_inst"/> 
            /// for accessing in functions related to  <see cref="ManIngameWiki"/></para>
            /// </summary>
            protected V _inst;
            /// <summary>
            /// The main identification the game uses for this page.
            /// <para><b>CAN RETURN NULL</b></para>
            /// <para>The prefab instance the page references in it's data. 
            /// Calling this only guarantees the immedeate page, and <b>not any pages linked to it.</b></para>
            /// <para>Use <see cref="inst"/> to access this externally, otherwise use <see cref="_inst"/> 
            /// for accessing in functions related to  <see cref="ManIngameWiki"/></para>
            /// <para>Be sure to call <seealso cref="WikiPage.OnWikiClosedOrDeallocateMemory"/> 
            /// after using it outside of the wiki page itself!</para>
            /// </summary>
            public V inst {
                get {
                    if (!HasInst())
                        RequestInstDataReady();
                    return _inst;
                }
            }
            /// <summary>
            /// Returns if our <see cref="inst"/> is valid
            /// </summary>
            /// <returns>True if it can be accessed and/or is accurate</returns>
            public abstract bool HasInst();

            /// <inheritdoc />
            protected WikiPage(string ModID, T id, LocExtString Title, Sprite Icon) : base(ModID, Title, Icon)
            {
                ID = id;
            }
            /// <inheritdoc />
            protected WikiPage(string ModID, T id, string Title, Sprite Icon) : base(ModID, Title, Icon)
            {
                ID = id;
            }
            /// <inheritdoc />
            protected WikiPage(string ModID, T id, LocExtString Title, Sprite Icon, WikiPageGroup group) : base(ModID, Title, Icon, group)
            {
                ID = id;
            }
            /// <inheritdoc />
            protected WikiPage(string ModID, T id, string Title, Sprite Icon, WikiPageGroup group) : base(ModID, Title, Icon, group)
            {
                ID = id;
            }
            /// <inheritdoc />
            protected WikiPage(string ModID, T id, LocExtString Title, Sprite Icon, string WikiGroupName, Sprite IconGroup) :
                base(ModID, Title, Icon, WikiGroupName, IconGroup)
            {
                ID = id;
            }
            /// <inheritdoc />
            protected WikiPage(string ModID, T id, LocExtString Title, Sprite Icon, LocExtString WikiGroupName, Sprite IconGroup) :
                base(ModID, Title, Icon, WikiGroupName, IconGroup)
            {
                ID = id;
            }
            /// <inheritdoc />
            protected WikiPage(string ModID, T id, string Title, Sprite Icon, string WikiGroupName, Sprite IconGroup) :
                base(ModID, Title, Icon, WikiGroupName, IconGroup)
            {
                ID = id;
            }
            /// <inheritdoc />
            protected WikiPage(string ModID, T id, string Title, Sprite Icon, LocExtString WikiGroupName, Sprite IconGroup) :
                base(ModID, Title, Icon, WikiGroupName, IconGroup)
            {
                ID = id;
            }


        }
        /// <summary>
        /// The basis for a wikipage that serves as a hub for other wikipages
        /// </summary>
        public abstract class WikiPageGroupBase : WikiPage 
        {
            /// <summary>
            /// The pages this should display
            /// </summary>
            public abstract List<WikiPage> NestedPages { get; }
            /// <summary>
            /// Current user search query in the searchbar (appears if there are more than 16 entries)
            /// </summary>
            public string searchQuery = "";
            /// <summary>
            /// The event that is called in OnGUI which can be used to add tooltips
            /// </summary>
            public Action HoverUISearch;
            /// <summary>
            /// The search query to find the page (less expensive)
            /// </summary>
            public Func<WikiPage, string, string, bool> PageHunterPreQuery;
            /// <summary>
            /// The search query to final validate the page (more expensive)
            /// </summary>
            public Func<WikiPage, string, string, bool> PageHunterPostQuery;
            /// <summary>
            /// The automatic page sorting algorithm. Set this to null to reset the search entirely (SLOW)
            /// </summary>
            public GUILayoutHelpers.SlowSorter<WikiPage> PageHunter;
            /// <summary>
            /// Called when the page is opened
            /// </summary>
            public Action onOpen = null;
            /// <summary>
            /// If the expandable tab in the sidebar is currently open
            /// </summary>
            public bool open = false;
            /// <summary>
            /// Create a wiki page group that nests pages of a category within itself. 
            /// <para>Can be nested</para>
            /// </summary>
            /// <param name="ModID">Mod ID to make under.</param>
            /// <param name="Title">The title of the wiki group to display</param>
            /// <param name="Icon">The optional icon</param>
            /// <param name="Group">The optional group to be nested inside of</param>
            /// <param name="hoverUISearch">The event that is called in OnGUI which can be used to add tooltips</param>
            /// <param name="preSearchQuery">The search query to find the page (less expensive)</param>
            /// <param name="postSearchQuery">The search query to final validate the page (more expensive)</param>
            public WikiPageGroupBase(string ModID, LocExtString Title, Sprite Icon = null, 
                WikiPageGroup Group = null, Action hoverUISearch = null, 
                Func<WikiPage, string, string, bool> preSearchQuery = null,
                 Func<WikiPage, string, string, bool> postSearchQuery = null) : 
                base(ModID, Title, Icon, Group)
            {
                HoverUISearch = hoverUISearch;
                PageHunterPreQuery = preSearchQuery;
                PageHunterPostQuery = postSearchQuery;
            }
            /// <summary>
            /// Create a wiki page group that nests pages of a category within itself. 
            /// <para>Can be nested</para>
            /// </summary>
            /// <param name="ModID">Mod ID to make under.</param>
            /// <param name="Title">The title of the wiki group to display</param>
            /// <param name="Icon">The optional icon</param>
            /// <param name="Group">The optional group to be nested inside of</param>
            /// <param name="hoverUISearch">The event that is called in OnGUI which can be used to add tooltips</param>
            /// <param name="preSearchQuery">The search query to find the page (less expensive)</param>
            /// <param name="postSearchQuery">The search query to final validate the page (more expensive)</param>
            public WikiPageGroupBase(string ModID, string Title, Sprite Icon = null, 
                WikiPageGroup Group = null, Action hoverUISearch = null,
                Func<WikiPage, string, string, bool> preSearchQuery = null,
                Func<WikiPage, string, string, bool> postSearchQuery = null) :
                base(ModID, Title, Icon, Group)
            {
                HoverUISearch = hoverUISearch;
                PageHunterPreQuery = preSearchQuery;
                PageHunterPostQuery = postSearchQuery;
            }
            /// <summary>
            /// Called when the gamemode is switched.  Helpful when the modded block lookup is changed
            /// </summary>
            /// <param name="mode">Mode it switched to</param>
            public override void OnGameModeSwitch(Mode mode)
            {
                if (PageHunter != null)
                    PageHunter.SetNewSearchQueryIfNeeded(searchQuery, true);
            }

            /// <inheritdoc />
            public override void GetIcon() { }

            /// <inheritdoc />
            protected override void OnBeforeDataRequested(bool getFullData)
            { 
            }
            /// <inheritdoc />
            public override void DisplaySidebar()
            {
                DisplaySidebarStart();
                DisplaySidebarEnd();
            }

            /// <summary>
            /// The default WikiPageGroupBase call for generating the DisplaySidebar() <b>BEFORE</b> displaying sidebar contents
            /// </summary>
            protected void DisplaySidebarStart()
            {
                GUILayout.BeginHorizontal(open ? AltUI.ButtonBlueActive : AltUI.ButtonBlue, 
                    GUILayout.Height(35), GUILayout.MaxWidth(WikiSidebarWidth));
                GUILayout.Label(displayName, AltUI.LabelWhite, GUILayout.Height(35), GUILayout.ExpandWidth(true));
                bool selected = false;
                if (icon)
                    selected = AltUI.SpriteButton(icon, AltUI.LabelWhite, GUILayout.Height(35), GUILayout.Width(35));
                GUILayout.EndHorizontal();
                Rect lastRect = GUILayoutUtility.GetLastRect();
                if (GUI.Button(lastRect, string.Empty, AltUI.TRANSPARENT) || selected)
                {
                    open = !open;
                    ManSFX.inst.PlayUISFX(ManSFX.UISfxType.Select);
                    if (open)
                        DisplaySidebarOnOpen();
                }
            }

            /// <summary>
            /// Optional event to display while the sidebar is open in default execution of DisplaySidebarStart() in DisplaySidebar().
            /// </summary>
            protected virtual void DisplaySidebarOnOpen()
            {
                if (onOpen != null)
                    onOpen();
            }

            /// <summary>
            /// The default WikiPageGroupBase call for generating the DisplaySidebar() <b>AFTER</b> displaying sidebar contents
            /// </summary>
            protected void DisplaySidebarEnd()
            {
                if (open)
                {
                    GUILayout.BeginVertical(AltUI.TextfieldBordered);
                    if (NestedPages.Count >= 16)
                    {
                        if (PageHunter == null)
                        {
                            PageHunter = new GUILayoutHelpers.SlowSorter<WikiPage>(32, 
                                PageHunterPreQuery, PageHunterPostQuery);
                            PageHunter.SetSearchArrayAndSearchQuery(NestedPages.ToArray(), searchQuery, true);
                        }
                        if (GUILayoutHelpers.GUITextFieldDisp("Search:", ref searchQuery))
                            PageHunter.SetNewSearchQueryIfNeeded(searchQuery, true);
                        HoverUISearch?.Invoke();
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
            /// <inheritdoc />
            public override bool OnWikiClosedOrDeallocateMemory()
            {
                bool hadData = PageHunter != null;
                PageHunter?.Abort();
                PageHunter = null;
                return hadData;
            }
        }
        /// <summary>
        /// Holds a list of <see cref="WikiPage"/>s in the sidebar
        /// </summary>
        public sealed class WikiPageGroup : WikiPageGroupBase
        {
            /// <inheritdoc />
            public override List<WikiPage> NestedPages => _NestedPages;
            private List<WikiPage> _NestedPages = new List<WikiPage>();

            /// <inheritdoc />
            public WikiPageGroup(string ModID, LocExtString Title, Sprite Icon = null, 
                WikiPageGroup Group = null, Action hoverUISearch = null,
                 Func<WikiPage, string, string, bool> preSearchQuery = null,
                 Func<WikiPage, string, string, bool> postSearchQuery = null) : 
                base(ModID, Title, Icon, Group, hoverUISearch, preSearchQuery, postSearchQuery)
            {
            }

            /// <inheritdoc />
            public WikiPageGroup(string ModID, string Title, Sprite Icon = null, 
                WikiPageGroup Group = null, Action hoverUISearch = null,
                Func<WikiPage, string, string, bool> preSearchQuery = null,
                 Func<WikiPage, string, string, bool> postSearchQuery = null) : 
                base(ModID, Title, Icon, Group, hoverUISearch, preSearchQuery, postSearchQuery)
            {
            }
        }
        /// <summary>
        /// The main wiki page for a wiki.  Auto-generated for new wikis.
        /// <para> Not advised to use this elsewhere</para>
        /// </summary>
        public class WikiPageMainDefault : WikiPage
        {
            /// <summary>
            /// Creates a main wiki page for a wiki.  Not advised to use this elsewhere
            /// </summary>
            /// <inheritdoc/>
            public WikiPageMainDefault(string ModID, LocExtString Title, Sprite Icon = null) : base(ModID, Title, Icon, null)
            {
            }

            /// <inheritdoc />
            protected override void OnBeforeDataRequested(bool getFullData)
            {
            }
            ///<inheritdoc/>
            public override void DisplaySidebar()
            {
                GUILayout.BeginHorizontal(CurrentWikiPage == this ? AltUI.ButtonGreen : AltUI.ButtonBlue,
                    GUILayout.Height(35), GUILayout.MaxWidth(WikiSidebarWidth));
                GUILayout.Label("<b>Home</b>", AltUI.LabelWhite, GUILayout.Height(35), GUILayout.Width(35));
                GUILayout.FlexibleSpace();
                bool selected = false;
                if (icon && AltUI.SpriteButton(icon, AltUI.LabelWhite, GUILayout.Height(35), GUILayout.Width(35)))
                    selected = true;
                GUILayout.EndHorizontal();
                Rect lastRect = GUILayoutUtility.GetLastRect();
                if (GUI.Button(lastRect, string.Empty, AltUI.TRANSPARENT) || selected)
                {
                    ManSFX.inst.PlayUISFX(ManSFX.UISfxType.Select);
                    GoHere();
                }
            }
            ///<inheritdoc/>
            public override bool OnWikiClosedOrDeallocateMemory()
            {
                return false;
            }
            ///<inheritdoc/>
            public override void GetIcon() { }
        }
        internal class WikiBroadSearch<T> : WikiPageGroupBase where T : WikiPage
        {
            public override List<WikiPage> NestedPages => _NestedPages;
            private List<WikiPage> _NestedPages = new List<WikiPage>();
            public WikiBroadSearch(string ModID, LocExtString Title, Sprite Icon = null,
                WikiPageGroup Group = null, Action hoverUISearch = null,
                Func<WikiPage, string, string, bool> preSearchQuery = null,
                 Func<WikiPage, string, string, bool> postSearchQuery = null) :
                base(ModID, Title, Icon, Group, hoverUISearch, preSearchQuery, postSearchQuery)
            {
            }
            public WikiBroadSearch(string ModID, string Title, Sprite Icon = null,
                WikiPageGroup Group = null, Action hoverUISearch = null,
                Func<WikiPage, string, string, bool> preSearchQuery = null,
                 Func<WikiPage, string, string, bool> postSearchQuery = null) :
                base(ModID, Title, Icon, Group, hoverUISearch, preSearchQuery, postSearchQuery)
            {
            }

            private void RepopulateIfNeeded()
            {
                if (!_NestedPages.Any())
                {
                    foreach (var page in IterateForInfo<T>())
                        _NestedPages.Add(page);
                }
            }
            public override void DisplaySidebar()
            {
                RepopulateIfNeeded();
                base.DisplaySidebar();
            } 
            protected override void DisplaySidebarOnOpen()
            {
                base.DisplaySidebarOnOpen();
                RepopulateIfNeeded();
            }
            public override bool OnWikiClosedOrDeallocateMemory()
            {
                _NestedPages.Clear();
                return base.OnWikiClosedOrDeallocateMemory();
            }
        }


        /// <summary>
        /// A wiki that displays the contents and data of the vanilla game or mod.  
        /// Created automatically by pages when needed, but can be manually made for more precision.
        /// </summary>
        public class Wiki
        {
            /// <summary>
            /// The name of the mod this wiki represents
            /// <para>Must be the actual mod ID of the mod</para>
            /// </summary>
            public readonly string ModID;
            /// <summary>
            /// The ModContainer of the target mod
            /// </summary>
            public ModContainer modContainer => ManMods.inst.FindMod(ModID);
            /// <summary>
            /// The main page group to use for the wiki as a fallback incase there's no content
            /// </summary>
            public WikiPageGroup MainPage = null;
            internal List<WikiPage> Pages => MainPage.NestedPages;
            internal Dictionary<string, WikiPage> ALLPages = new Dictionary<string, WikiPage>();

            /// <summary>
            /// Count of all pages in this Wiki
            /// </summary>
            public int RegisteredPagesCount => ALLPages.Count;

            /// <summary>
            /// Create a new wiki.  Registered in ManIngameWiki automatically.
            /// Created automatically by pages when needed, but can be manually made for more precision.
            /// </summary>
            /// <param name="ModID">Must be the actual mod ID of the mod</param>
            public Wiki(string ModID)
            {
                this.ModID = ModID;
                Wikis.Add(ModID, this);
                if (ModID == VanillaGameName)
                    MainPage = new WikiPageGroup(ModID, "Vanilla - Terra Tech");
                else if (ModID == GlobalName)
                    MainPage = new WikiPageGroup(ModID, "<b>" + GlobalName + "</b>");
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
        /// <summary>
        /// <b>Insure</b> a wiki by ModID
        /// </summary>
        /// <param name="ModID">Must be the actual mod ID of the mod</param>
        /// <returns>The wiki. <b>Will create a new one if missing</b></returns>
        public static Wiki GetModWiki(string ModID)
        {
            return InsureWiki(ModID);
        }
        /// <summary>
        /// Try find a wiki by ModID
        /// </summary>
        /// <param name="ModID">Must be the actual mod ID of the mod</param>
        /// <param name="wiki">The wiki. Will return null if failed</param>
        /// <returns>True if the wiki was successfully found</returns>
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

        /// <summary> Localization text file </summary>
        public static LocExtStringMod LOC_Blocks = new LocExtStringMod(new Dictionary<Languages, string>
            {{ Languages.US_English, "Blocks" },
            {Languages.Japanese, "ブロック" }});
        /// <summary> Localization text file </summary>
        public static LocExtStringMod LOC_Corps = new LocExtStringMod(new Dictionary<Languages, string>
            {{ Languages.US_English, "Corporations" },
            {Languages.Japanese, "企業" }});
        /// <summary> Localization text file </summary>
        public static LocExtStringMod LOC_Chunks = new LocExtStringMod(new Dictionary<Languages, string>
            {{ Languages.US_English, "Chunks" },
            {Languages.Japanese, "資源" }});
        /// <summary> Localization text file </summary>
        public static LocExtStringMod LOC_Scenery = new LocExtStringMod(new Dictionary<Languages, string>
            {{ Languages.US_English, "Resources" },
            {Languages.Japanese, "木や石 鉱石 等" }});
        /// <summary> Localization text file </summary>
        public static LocExtStringMod LOC_Biomes = new LocExtStringMod(new Dictionary<Languages, string>
            {{ Languages.US_English, "Biomes" },
            {Languages.Japanese, "バイオーム" }});
        /// <summary> Localization text file </summary>
        public static LocExtStringMod LOC_General = new LocExtStringMod(new Dictionary<Languages, string>
            {{ Languages.US_English, "General" },
            {Languages.Japanese, "全般" }});
        /// <summary> Localization text file </summary>
        public static LocExtStringMod LOC_Combat = new LocExtStringMod(new Dictionary<Languages, string>
            {{ Languages.US_English, "Combat" },
            {Languages.Japanese, "戦闘" }});
        /// <summary> Localization text file </summary>
        public static LocExtStringMod LOC_Tools = new LocExtStringMod(new Dictionary<Languages, string>
            {{ Languages.US_English, "Tools" },
            {Languages.Japanese, "道具" }});
        /// <summary> Localization text file </summary>
        public static LocExtStringMod LOC_Hints = new LocExtStringMod(new Dictionary<Languages, string>
            {{ Languages.US_English, "Hints" },
            {Languages.Japanese, "ヒント" }});


        private static void AutoPopulateVanilla(Wiki wiki)
        {
            try
            {
                WikiPageGroup corps = InsureCorpWikiGroup(wiki.ModID);
                try
                {
                    WikiPageGroup blocks = InsureBlockWikiGroup(wiki.ModID);
                    for (int step = 0; step < Enum.GetValues(typeof(BlockTypes)).Length; step++)
                    {
                        BlockTypes BT = (BlockTypes)step;
                        if (ManSpawn.inst.IsBlockAllowedInLaunchedConfig(BT) &&
                             ManSpawn.inst.GetCategory(BT) != BlockCategories.Null &&
                            !wiki.ALLPages.ContainsKey(StringLookup.GetItemName(ObjectTypes.Block, step)))
                            new WikiPageBlock(step, step.ToString(), blocks);
                    }
                }
                catch (Exception e)
                {
                    Debug_TTExt.Log("Failed to init blocks for vanilla wiki! - " + e);
                }
                for (int step = 1; step < Enum.GetValues(typeof(FactionSubTypes)).Length; step++)
                {
                    FactionSubTypes FST = (FactionSubTypes)step;
                    if (!wiki.ALLPages.ContainsKey(FST.ToString() + "_corp"))
                        new WikiPageCorp(wiki.ModID, step, corps);
                }
            }
            catch (Exception e)
            {
                Debug_TTExt.Log("Failed to init corps for vanilla wiki! - " + e);
            }
            try
            {
                WikiPageGroup chunks = InsureChunksWikiGroup(wiki.ModID);
                for (int step = 0; step < Enum.GetValues(typeof(ChunkTypes)).Length; step++)
                {
                    if (!StringLookup.GetItemName(ObjectTypes.Chunk, step).StartsWith("ERROR:"))
                        new WikiPageChunk(step, chunks);
                }
            }
            catch (Exception e)
            {
                Debug_TTExt.Log("Failed to init chunks for vanilla wiki! - " + e);
            }
            try
            {
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
                WikiPageGroup biomes = InsureBiomesWikiGroup(wiki.ModID);
                foreach (var biome in biomesCollected)
                    new WikiPageBiome(biome, biomes);
            }
            catch (Exception e)
            {
                Debug_TTExt.Log("Failed to init biomes for vanilla wiki! - " + e);
            }
            try
            {
                WikiPageGroup scenery = InsureSceneryWikiGroup(wiki.ModID);
                for (int step = 0; step < Enum.GetValues(typeof(SceneryTypes)).Length; step++)
                {
                    if (!StringLookup.GetItemName(ObjectTypes.Scenery, step).StartsWith("ERROR:"))
                        new WikiPageScenery(step, scenery);
                }
            }
            catch (Exception e)
            {
                Debug_TTExt.Log("Failed to init scenery for vanilla wiki! - " + e);
            }
            try
            {
                List<ManHints.HintDefinition> lingo = (List<ManHints.HintDefinition>)hintBatchCore.GetValue(
                    (HintDefinitionList)hintBatch.GetValue(ManHints.inst));
                foreach (var item in lingo)
                {
                    if (item.m_HintMessage != null && item.m_HintMessage.IsValid)
                        InjectHint(VanillaGameName, LOC_General, new LocExtStringVanillaText(item.m_HintMessage));
                }
            }
            catch (Exception e)
            {
                Debug_TTExt.Log("Failed to init hints for vanilla wiki! - " + e);
            }
            try
            {
                new WikiPageDamageStats(wiki.ModID, LOC_Combat, ManUI.inst.GetBlockCatIcon(BlockCategories.Weapons));
            }
            catch (Exception e)
            {
                Debug_TTExt.Log("Failed to init damage stats for vanilla wiki! - " + e);
            }
            try
            {
                new WikiPageInfo(wiki.ModID, LOC_Tools, ToolsSprite, GUITools);
            }
            catch (Exception e)
            {
                Debug_TTExt.Log("Failed to init tools for vanilla wiki! - " + e);
            }
            try
            {
                ExtendedWiki.AutoPopulateWikiExtras(wiki);
            }
            catch (Exception e)
            {
                Debug_TTExt.Log("Failed to init extras for vanilla wiki! - " + e);
            }

            InvokeHelper.Invoke(FinishVanillaWikiSearch, 3f);
        }
        private static void AutoPopulateGlobal(Wiki wiki)
        {
            try
            {
                CorpSearch = new WikiBroadSearch<WikiPageCorp>(wiki.ModID, LOC_Corps, CorpsSprite);
            }
            catch (Exception e)
            {
                Debug_TTExt.Log("Failed to init corps for global wiki! - " + e);
            }
            try
            {
                BlockSearch = new WikiBroadSearch<WikiPageBlock>(wiki.ModID, LOC_Blocks, BlocksSprite, null,
                    WikiPageBlock.ValidBlockSearchQueryPopup, WikiPageBlock.ValidBlockSearchQuery,
                    WikiPageBlock.ValidBlockSearchQueryPost);
            }
            catch (Exception e)
            {
                Debug_TTExt.Log("Failed to init blocks for global wiki! - " + e);
            }
            try
            {
                ChunkSearch = new WikiBroadSearch<WikiPageChunk>(wiki.ModID, LOC_Chunks, ChunksSprite, null,
                    WikiPageChunk.ValidChunkSearchQueryPopup, null,
                    WikiPageChunk.ValidChunkSearchQueryPost);
            }
            catch (Exception e)
            {
                Debug_TTExt.Log("Failed to init chunks for global wiki! - " + e);
            }
            try
            {
                BiomeSearch = new WikiBroadSearch<WikiPageBiome>(wiki.ModID, LOC_Biomes, BiomesSprite);
            }
            catch (Exception e)
            {
                Debug_TTExt.Log("Failed to init biomes for global wiki! - " + e);
            }
            try
            {
                ScenerySearch = new WikiBroadSearch<WikiPageScenery>(wiki.ModID, LOC_Scenery, ScenerySprite, null,
                    WikiPageScenery.ValidScenerySearchQueryPopup, null,
                    WikiPageScenery.ValidScenerySearchQueryPost);
            }
            catch (Exception e)
            {
                Debug_TTExt.Log("Failed to init scenery for global wiki! - " + e);
            }
            try
            {
                ExtendedWiki.AutoPopulateWikiExtras(wiki);
            }
            catch (Exception e)
            {
                Debug_TTExt.Log("Failed to init extras for global wiki! - " + e);
            }
        }

        internal static WikiBroadSearch<WikiPageCorp> CorpSearch;
        internal static WikiBroadSearch<WikiPageBlock> BlockSearch;
        internal static WikiBroadSearch<WikiPageChunk> ChunkSearch;
        internal static WikiBroadSearch<WikiPageBiome> BiomeSearch;
        internal static WikiBroadSearch<WikiPageScenery> ScenerySearch;

        private static void AutoPopulateWiki(Wiki wiki)
        {
            if (wiki.ModID == VanillaGameName)
                AutoPopulateVanilla(wiki);
            else if (wiki.ModID == GlobalName)
                AutoPopulateGlobal(wiki);
            else
            {   // Modded
                var MC = wiki.modContainer;
                if (MC != null)
                {
                    string modName = wiki.ModID.NullOrEmpty() ? "<NULL>" : wiki.ModID;
                    var contents = MC.Contents;
                    try
                    {
                        if (contents.m_Blocks != null && contents.m_Blocks.Any())
                        {
                            WikiPageGroup blocks = InsureBlockWikiGroup(wiki.ModID);
                            foreach (var item in contents.m_Blocks)
                            {
                                if (!wiki.ALLPages.ContainsKey(item.m_BlockDisplayName))
                                    new WikiPageBlock(ManMods.inst.GetBlockID(item.name), item.name, blocks);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug_TTExt.Log("Failed to init blocks for \"" + modName + "\" mod wiki! - " + e);
                    }
                    try
                    {
                        var corps = contents.m_Corps;
                        if (corps != null && corps.Any())
                        {
                            WikiPageGroup corpsPage = (WikiPageGroup)GetPage(MC.ModID, LOC_Corps);
                            if (corpsPage == null)
                                corpsPage = InsureCorpWikiGroup(wiki.ModID);
                            foreach (var item in corps)
                                new WikiPageCorp(MC.ModID, (int)ManMods.inst.GetCorpIndex(item.m_ShortName), corpsPage);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug_TTExt.Log("Failed to init corps for \"" + modName + "\" mod wiki! - " + e);
                    }
                    try
                    {
                        ExtendedWiki.AutoPopulateWikiExtras(wiki);
                    }
                    catch (Exception e)
                    {
                        Debug_TTExt.Log("Failed to init extras for \"" + modName + "\" mod wiki! - " + e);
                    }
                    try
                    {
                        if (TryGetModWiki(MC.ModID, out Wiki wiki2))
                        {
                            wiki2.MainPage.onOpen = wiki2.PrepareModded;
                            OnModWikiCreated.Send(wiki);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug_TTExt.Log("FAILED TO GET \"" + modName + "\" mod wiki! - " + e);
                    }
                }
            }
        }

        /// <summary>
        /// <b>Insure</b> a wiki by ModID
        /// </summary>
        /// <param name="ModID">Must be the actual mod ID of the mod</param>
        /// <returns>The wiki. <b>Will create a new one if missing</b></returns>
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

        /// <summary>
        /// Creates a new WikiPageGroup with default settings
        /// </summary>
        /// <param name="ModID">Must be the actual mod ID of the mod</param>
        /// <param name="WikiGroupName">The group insure and to assign to</param>
        /// <param name="Icon">The icon to assign to the group if it has to be made</param>
        /// <returns>The WikiPageGroup if it exists or a new one based on given data</returns>
        /// <exception cref="NullReferenceException">ModID or WikiGroupName is null</exception>
        /// <exception cref="InvalidOperationException">Page of the same ENGLISH name exists already in target wiki</exception>
        public static WikiPageGroup InsureDefaultWikiGroup(string ModID, LocExtString WikiGroupName, Sprite Icon = null)
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
        /// <summary>
        /// Creates a new WikiPageGroup with default settings
        /// </summary>
        /// <param name="ModID">Must be the actual mod ID of the mod</param>
        /// <param name="WikiGroupName">The group insure and to assign to</param>
        /// <param name="Icon">The icon to assign to the group if it has to be made</param>
        /// <returns>The WikiPageGroup if it exists or a new one based on given data</returns>
        /// <exception cref="NullReferenceException">ModID or WikiGroupName is null</exception>
        /// <exception cref="InvalidOperationException">Page of the same ENGLISH name exists already in target wiki</exception>
        public static WikiPageGroup InsureDefaultWikiGroup(string ModID, string WikiGroupName, Sprite Icon = null)
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

        /// <summary>
        /// Creates a new WikiPageGroup with default settings for <b>Blocks</b> 
        /// </summary>
        /// <param name="ModID">Must be the actual mod ID of the mod</param>
        /// <returns>The WikiPageGroup if it exists or a new one based on given data</returns>
        /// <exception cref="NullReferenceException">ModID is null</exception>
        /// <exception cref="InvalidOperationException">Page of the same ENGLISH name exists already in target wiki</exception>
        public static WikiPageGroup InsureBlockWikiGroup(string ModID)
        {
            if (ModID.NullOrEmpty())
                throw new NullReferenceException("ModID should NOT be null.  WHY IS IT NULL");
            Wiki wiki = InsureWiki(ModID);
            if (wiki.ALLPages.TryGetValue(LOC_Blocks.GetEnglish(), out var page))
            {
                if (page is WikiPageGroup pageR2)
                    return pageR2;
                else
                    throw new InvalidOperationException("InsureBlockWikiGroup tried to add a new entry to wiki \"" +
                        ModID + "\", but it was already taken by a non-group of the same name!\n" +
                        page.GetType().ToString() + ", name " + page.title);
            }
            else
                return new WikiPageGroup(wiki.ModID, LOC_Blocks, BlocksSprite, null,
                        WikiPageBlock.ValidBlockSearchQueryPopup,
                        WikiPageBlock.ValidBlockSearchQuery, WikiPageBlock.ValidBlockSearchQueryPost);
        }
        /// <summary>
        /// Creates a new WikiPageGroup with default settings for <b>Corporations</b> 
        /// </summary>
        /// <param name="ModID">Must be the actual mod ID of the mod</param>
        /// <returns>The WikiPageGroup if it exists or a new one based on given data</returns>
        /// <exception cref="NullReferenceException">ModID is null</exception>
        /// <exception cref="InvalidOperationException">Page of the same ENGLISH name exists already in target wiki</exception>
        public static WikiPageGroup InsureCorpWikiGroup(string ModID)
        {
            if (ModID.NullOrEmpty())
                throw new NullReferenceException("ModID should NOT be null.  WHY IS IT NULL");
            Wiki wiki = InsureWiki(ModID);
            if (wiki.ALLPages.TryGetValue(LOC_Corps.GetEnglish(), out var page))
            {
                if (page is WikiPageGroup pageR2)
                    return pageR2;
                else
                    throw new InvalidOperationException("InsureCorpWikiGroup tried to add a new entry to wiki \"" +
                        ModID + "\", but it was already taken by a non-group of the same name!\n" +
                        page.GetType().ToString() + ", name " + page.title);
            }
            else
                return new WikiPageGroup(wiki.ModID, LOC_Corps, CorpsSprite);
        }
        /// <summary>
        /// Creates a new WikiPageGroup with default settings for <b>Chunks</b> 
        /// </summary>
        /// <param name="ModID">Must be the actual mod ID of the mod</param>
        /// <returns>The WikiPageGroup if it exists or a new one based on given data</returns>
        /// <exception cref="NullReferenceException">ModID is null</exception>
        /// <exception cref="InvalidOperationException">Page of the same ENGLISH name exists already in target wiki</exception>
        public static WikiPageGroup InsureChunksWikiGroup(string ModID)
        {
            if (ModID.NullOrEmpty())
                throw new NullReferenceException("ModID should NOT be null.  WHY IS IT NULL");
            Wiki wiki = InsureWiki(ModID);
            if (wiki.ALLPages.TryGetValue(LOC_Chunks.GetEnglish(), out var page))
            {
                if (page is WikiPageGroup pageR2)
                    return pageR2;
                else
                    throw new InvalidOperationException("InsureChunksWikiGroup tried to add a new entry to wiki \"" +
                        ModID + "\", but it was already taken by a non-group of the same name!\n" +
                        page.GetType().ToString() + ", name " + page.title);
            }
            else
                return new WikiPageGroup(wiki.ModID, LOC_Chunks, ChunksSprite, null,
                    WikiPageChunk.ValidChunkSearchQueryPopup, null,
                    WikiPageChunk.ValidChunkSearchQueryPost);
        }
        /// <summary>
        /// Creates a new WikiPageGroup with default settings for <b>Biomes</b> 
        /// </summary>
        /// <param name="ModID">Must be the actual mod ID of the mod</param>
        /// <returns>The WikiPageGroup if it exists or a new one based on given data</returns>
        /// <exception cref="NullReferenceException">ModID is null</exception>
        /// <exception cref="InvalidOperationException">Page of the same ENGLISH name exists already in target wiki</exception>
        public static WikiPageGroup InsureBiomesWikiGroup(string ModID)
        {
            if (ModID.NullOrEmpty())
                throw new NullReferenceException("ModID should NOT be null.  WHY IS IT NULL");
            Wiki wiki = InsureWiki(ModID);
            if (wiki.ALLPages.TryGetValue(LOC_Biomes.GetEnglish(), out var page))
            {
                if (page is WikiPageGroup pageR2)
                    return pageR2;
                else
                    throw new InvalidOperationException("InsureBiomesWikiGroup tried to add a new entry to wiki \"" +
                        ModID + "\", but it was already taken by a non-group of the same name!\n" +
                        page.GetType().ToString() + ", name " + page.title);
            }
            else
                return new WikiPageGroup(wiki.ModID, LOC_Biomes, BiomesSprite);
        }
        /// <summary>
        /// Creates a new WikiPageGroup with default settings for <b>Scenery</b> 
        /// </summary>
        /// <param name="ModID">Must be the actual mod ID of the mod</param>
        /// <returns>The WikiPageGroup if it exists or a new one based on given data</returns>
        /// <exception cref="NullReferenceException">ModID is null</exception>
        /// <exception cref="InvalidOperationException">Page of the same ENGLISH name exists already in target wiki</exception>
        public static WikiPageGroup InsureSceneryWikiGroup(string ModID)
        {
            if (ModID.NullOrEmpty())
                throw new NullReferenceException("ModID should NOT be null.  WHY IS IT NULL");
            Wiki wiki = InsureWiki(ModID);
            if (wiki.ALLPages.TryGetValue(LOC_Scenery.GetEnglish(), out var page))
            {
                if (page is WikiPageGroup pageR2)
                    return pageR2;
                else
                    throw new InvalidOperationException("InsureSceneryWikiGroup tried to add a new entry to wiki \"" +
                        ModID + "\", but it was already taken by a non-group of the same name!\n" +
                        page.GetType().ToString() + ", name " + page.title);
            }
            else
                return new WikiPageGroup(wiki.ModID, LOC_Scenery, ScenerySprite, null,
                    WikiPageScenery.ValidScenerySearchQueryPopup, null,
                    WikiPageScenery.ValidScenerySearchQueryPost);
        }

        /// <summary>
        /// Automatically managed by ExtUsageHint for ingame hints.
        /// <para>Use this for hints you don't want to ever display during gameplay</para>
        /// </summary>
        /// <param name="ModID">Must be the actual mod ID of the mod</param>
        /// <param name="Title">The category of the hint</param>
        /// <param name="hintLOC">The hint description to display under the hint</param>
        /// <exception cref="InvalidOperationException">Page of the same name already exists in the target wiki</exception>
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
        /// <summary>
        /// Automatically managed by ExtUsageHint for ingame hints.
        /// <para>Use this for hints you don't want to ever display during gameplay</para>
        /// </summary>
        /// <param name="ModID">Must be the actual mod ID of the mod</param>
        /// <param name="Title">The category of the hint</param>
        /// <param name="hintLOC">The hint description to display under the hint</param>
        /// <exception cref="InvalidOperationException">Page of the same name already exists in the target wiki</exception>
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


        /// <summary>
        /// Find a page based on the page name.
        /// <para>Using ModID is faster and more precise!</para>
        /// </summary>
        /// <param name="pageName">The name of the page</param>
        /// <returns>The page if found, otherwise null</returns>
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
        /// <summary>
        /// Find a page based on the page name specified by ModID.
        /// </summary>
        /// <param name="ModID">Must be the actual mod ID of the mod</param>
        /// <param name="pageName">The name of the page</param>
        /// <returns>The page if found, otherwise null</returns>
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
        /// <summary>
        /// Find a page based on the page name specified by ModID.
        /// </summary>
        /// <param name="pageName">The ENGLISH name of the page</param>
        /// <returns>The page if found, otherwise null</returns>
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
        /// <summary>
        /// Find a page based on the page name.
        /// <para>Using ModID is faster and more precise!</para>
        /// </summary>
        /// <param name="ModID">Must be the actual mod ID of the mod</param>
        /// <param name="pageName">The ENGLISH name of the page</param>
        /// <returns>The page if found, otherwise null</returns>
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
        /// <summary>
        /// Find a WikiPageBlock by the name
        /// </summary>
        /// <param name="blockName">The ENGLISH name of the page</param>
        /// <returns>The page if found, otherwise null</returns>
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
        /// <summary>
        /// Find a WikiPageChunk by the name
        /// </summary>
        /// <param name="chunkName">The ENGLISH name of the page</param>
        /// <returns>The page if found, otherwise null</returns>
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
        /// <summary>
        /// Find a WikiPageCorp by the name
        /// </summary>
        /// <param name="corp">The FactionSubTypes ID of the corp</param>
        /// <returns>The page if found, otherwise null</returns>
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
        /// <summary>
        /// Get the global WikiPageDamageStats page
        /// </summary>
        /// <returns>The page, should not be null</returns>
        public static WikiPageDamageStats GetDamageStatsPage() => (WikiPageDamageStats)GetModWiki(VanillaGameName).ALLPages[LOC_Combat.ToString()];

        /// <summary>
        /// Name of the vanilla game for the wiki in ENGLISH
        /// </summary>
        public const string VanillaGameName = "Terra Tech";
        /// <summary>
        /// Name of the global search wiki in ENGLISH
        /// </summary>
        public const string GlobalName = "Global Search";

        /// <inheritdoc/>
        public string DirectoryInExtModSettings => "IngameWiki";

        private static ManIngameWiki inst = new ManIngameWiki();
        private static Dictionary<string, Wiki> Wikis = new Dictionary<string, Wiki>();
        /// <summary>
        /// All wikis registered in <see cref="ManIngameWiki"/>
        /// </summary>
        public static Dictionary<string, Wiki> AllWikis => Wikis;

        private static LocExtStringMod LOC_WikiMainName = new LocExtStringMod(new Dictionary<Languages, string>
            {{ Languages.US_English, "Terra Tech Wiki V0.7" },
            {Languages.Japanese, "テラテックウィキ V0.7" }});
        private static WikiPage DefaultWikiPage = new WikiPageMainDefault(VanillaGameName,
            LOC_WikiMainName, WikiSprite);


        private static bool assignedQuit = false;
        private static WikiPage curWikiPage = default;
        /// <summary>
        /// The current wiki page at the top of the stack
        /// </summary>
        public static WikiPage CurrentWikiPage
        {
            get => curWikiPage;
            set
            {
                if (curWikiPage != value)
                {
                    foretrace.Clear();
                    backtrace.Add(curWikiPage);
                    if (backtrace.Count > 64)
                        backtrace.RemoveAt(0);
                    if (value != null)
                    {
                        value.RequestInstDataReady();
                        value.OnBeforeDisplay();
                    }
                }
                curWikiPage = value;
                if (!assignedQuit)
                {
                    assignedQuit = true;
                    Application.quitting += SaveBrowsingState;
                }
            }
        }
        private static List<WikiPage> backtrace = new List<WikiPage>();
        private static List<WikiPage> foretrace = new List<WikiPage>();

        /// <summary>
        /// The set keybind to toggle the wiki
        /// </summary>
        public static KeyCode WikiButtonKeybind = KeyCode.Slash;
        /// <summary>
        /// If the left side panel of the wiki is is open
        /// </summary>
        public bool sideOpen = true;
        /// <summary>
        /// If the wiki should display the JSON export buttons
        /// </summary>
        public bool enabledJSONExport = false;
        /// <summary>
        /// The currently viewed wiki ID to remember for next session
        /// </summary>
        public string wikiID;
        /// <summary>
        /// The currently viewed pageName to remember for next session
        /// </summary>
        public string pageName;
        /// <summary>
        /// The currently viewed page scroll height to remember for next session
        /// </summary>
        public float scrollY;
        /// <summary>
        /// The currently viewed side panel scroll height
        /// </summary>
        public static Vector2 scrollSide;
        /// <summary>
        /// The currently viewed page scroll height
        /// </summary>
        public static Vector2 scroll;
        /// <inheritdoc cref="enabledJSONExport"/>
        public static bool ShowJSONExport => inst.enabledJSONExport;


        private static GUIInst instGUI;
        private static Rect guiWindow = default;
        private const int ExtWikiID = 1002248;

        private static LocExtStringMod LOC_WikiTopName = new LocExtStringMod(new Dictionary<Languages, string>
            {{ Languages.US_English, "MOD WIKI" },
            {Languages.Japanese, "改造ウィキ" }});
        internal class GUIInst : MonoBehaviour
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
                    if (state)
                    {
                        guiWindow = new Rect(20, 20, 
                            ManModGUI.ModWikiGUIWidthDefault * Display.main.renderingWidth,
                            ManModGUI.ModWikiGUIHeightDefaultFromWidth * Display.main.renderingWidth);
                        //ManModGUI.HideAllObstructingUI();
                        InsureUpdateGameModeSwitch();
                        ManSFX.inst.PlayUISFX(ManSFX.UISfxType.Open);
                        ManModGUI.AddEscapeableCallback(QuitWiki, false);
                        OnWikiOpened.Send();
                    }
                    else
                    {
                        ManSFX.inst.PlayUISFX(ManSFX.UISfxType.Close);
                        ManModGUI.RemoveEscapeableCallback(QuitWiki, false);
                        OnWikiClosed();
                    }
                    try
                    {
                        WikiButton.SetToggleState(state);
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
                        guiWindow = ManModGUI.ModWikiGUIScaler.Window(ExtWikiID, guiWindow, GUILayouter, 
                            LOC_WikiTopName.ToString(), CloseGUI, ExtraGUITopBar, true, true);
                    }
                }
                catch (ExitGUIException e)
                {
                    throw e;
                }
                catch (Exception) { }
            }
        }

        private static LocExtStringMod LOC_WikiGoBack = new LocExtStringMod(new Dictionary<Languages, string>
            {{ Languages.US_English, "Go Back" },
            {Languages.Japanese, "ページに戻る" }});
        private static LocExtStringMod LOC_WikiGoForward = new LocExtStringMod(new Dictionary<Languages, string>
            {{ Languages.US_English, "Go Forward" },
            {Languages.Japanese, "ページを転送する" }});
        private static LocExtStringMod LOC_WikiContents = new LocExtStringMod(new Dictionary<Languages, string>
            {{ Languages.US_English, "Contents" },
            {Languages.Japanese, "目次" }});
        private static void GUILayouter(int ID)
        {
            GUILayout.BeginHorizontal(GUILayout.Height(40));
            {
                if (CurrentWikiPage != null)
                {
                    if (GUILayout.Button("<", AltUI.ButtonOrangeLarge, GUILayout.Width(40)))
                        CurrentWikiPage.GoBack();
                    AltUI.Tooltip.GUITooltip(LOC_WikiGoBack.ToString() +
                        (backtrace.LastOrDefault()?.displayName != null ?
                        ("\n" + backtrace.LastOrDefault().displayName) : string.Empty));
                }
                else
                {
                    GUILayout.Button("<", AltUI.ButtonGreyLarge, GUILayout.Width(40));
                    AltUI.Tooltip.GUITooltip(LOC_WikiGoBack.ToString());
                }
                if (foretrace.Any())
                {
                    if (GUILayout.Button(">", AltUI.ButtonOrangeLarge, GUILayout.Width(40)))
                        CurrentWikiPage.GoForward();
                    AltUI.Tooltip.GUITooltip(LOC_WikiGoForward.ToString() +
                        (foretrace.LastOrDefault()?.displayName != null ? 
                        ("\n" + foretrace.LastOrDefault().displayName) : string.Empty));
                }
                else
                {
                    GUILayout.Button(">", AltUI.ButtonGreyLarge, GUILayout.Width(40));
                    AltUI.Tooltip.GUITooltip(LOC_WikiGoForward.ToString());
                }
            }
            if (GUILayout.Button(LOC_WikiContents.ToString(), inst.sideOpen ? AltUI.ButtonBlueLargeActive : AltUI.ButtonBlueLarge, GUILayout.Width(240)))
                inst.sideOpen = !inst.sideOpen;
            if (curWikiPage != null)
                GUILayout.Label(curWikiPage.displayName, AltUI.LabelBlackTitle);
            else
                GUILayout.Label(DefaultWikiPage.displayName, AltUI.LabelBlackTitle);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (inst.sideOpen)
            {
                GUILayout.BeginVertical(GUILayout.Width(WikiSidebarWidth));
                scrollSide = GUILayout.BeginScrollView(scrollSide, GUILayout.Width(WikiSidebarWidth));
                DisplayGlobalWiki();
                foreach (var item in Wikis.Values.Where(x => x.ModID != GlobalName))
                {
                    if (item.MainPage != null)
                        item.MainPage.DisplaySidebar();
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
        }
        private static void DisplayGlobalWiki() =>
            GlobalWiki?.MainPage?.DisplaySidebar();


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
        private static void ExtraGUITopBar()
        {
            WikiTopBarEvent.Send(CurrentWikiPage?.wiki);
        }




        private static Mode ModeSwitchDelta = null;
        private static Texture2D HintIcon;
        private static Texture2D HintIconBlack;
        private static LocExtStringMod wikiName = new LocExtStringMod( new Dictionary<Languages, string>
            {{ Languages.US_English, "In-Game Wiki" },
            {Languages.Japanese, "ゲーム内ウィキ" }}
        );
        private static ManToolbar.ToolbarToggle WikiButton = new ManToolbar.ToolbarToggle(wikiName, WikiSprite, SetGUI);
        private static Wiki GlobalWiki = null;
        internal static void InitWiki()
        {
            _ = InfoSprite;

            if (instGUI == null)
            {
                instGUI = new GameObject("ManIngameWiki").AddComponent<GUIInst>();
                Debug_TTExt.Log("TerraTechETCUtil: InitWiki()");
                ManGameMode.inst.ModeStartEvent.Subscribe(OnGameModeSwitch);
                //WikiPageBlock.GetImagesForPending();
                GlobalWiki = InsureWiki(GlobalName);
                InvokeHelper.InvokeNextUpdate(LoadPage);
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
                    if (item2.OnWikiClosedOrDeallocateMemory())
                        released = true;
                }
            }
            if (released)
                GC.Collect();
        }

        private static void SaveBrowsingState()
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
