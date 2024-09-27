using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

namespace TerraTechETCUtil
{
    public static class UIHelpersExt
    {
        private static FieldInfo delay = typeof(ManHUD).GetField("m_RadialShowDelay", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo dist = typeof(ManHUD).GetField("m_RadialMouseDistanceThreshold", BindingFlags.NonPublic | BindingFlags.Instance);
        private static MethodInfo overlays = typeof(ManOverlay).GetMethod("AddQueuedOverlay", BindingFlags.NonPublic | BindingFlags.Instance);

        public static Sprite ModContentIcon { get; } = ResourcesHelper.GetTexture2DFromBaseGameAllFast("ICON_MOD").ConvertToSprite();
        public static Sprite NullSprite => ManUI.inst.GetSprite(ObjectTypes.Block, -1);
        internal static bool UseNullIfNoSpriteProvided = true;
        internal static float _ROROpenTimeDelay = 0.10f;
        public static float ROROpenTimeDelay => _ROROpenTimeDelay;
        internal static float _ROROpenAllowedMouseDeltaSqr = 0;
        public static float ROROpenAllowedMouseDeltaSqr => _ROROpenAllowedMouseDeltaSqr;
        public static readonly ManHUD.HUDElementType customElement = (ManHUD.HUDElementType)(-1);
        internal static Dictionary<string, Sprite> CachedUISprites = new Dictionary<string, Sprite>();

        public static bool IsIngame { get { return !ManPauseGame.inst.IsPaused && !ManPointer.inst.IsInteractionBlocked; } }

        public static void ReleaseControl(string Name = null)
        {
            if (Name == null)
            {
                GUI.FocusControl(null);
                GUI.UnfocusWindow();
                GUIUtility.hotControl = 0;
            }
            else
            {
                if (GUI.GetNameOfFocusedControl() == Name)
                {
                    GUI.FocusControl(null);
                    GUI.UnfocusWindow();
                    GUIUtility.hotControl = 0;
                }
            }
        }


        public static void Init()
        {
            _ROROpenTimeDelay = (float)delay.GetValue(ManHUD.inst);
            int dis = (int)dist.GetValue(ManHUD.inst);
            _ROROpenAllowedMouseDeltaSqr = dis * dis;
        }

        public static void LogCachedIcons()
        {
            GUIButtonMadness.InsureRadialMenuPrefabs();
            Debug_TTExt.Log("------ ICONS CACHED ------");
            foreach (var item in CachedUISprites)
            {
                Debug_TTExt.Log(item.Key.NullOrEmpty() ? "NULL" : item.Key);
            }
            Debug_TTExt.Log("------ END CACHED ------");
        }
        public static Sprite GetGUIIcon(string name)
        {
            GUIButtonMadness.InsureRadialMenuPrefabs();
            if (!CachedUISprites.TryGetValue(name, out var val))
                throw new NullReferenceException("Icon " + name + " does not exist in UIHelpers");
            return val;
        } 
        public static Sprite GetIconFromBundle(ModContainer MC, string name)
        {
            var holder = ResourcesHelper.GetTextureFromModAssetBundle(MC, name, false);
            if (holder != null)
            {
                return Sprite.Create(holder, new Rect(0, 0, holder.width, holder.height), new Vector2(0.5f, 0.5f));
            }
            else if (UseNullIfNoSpriteProvided)
                return NullSprite;
            return null;
        }
        public static void PrintAllComponentsGameObjectDepth<T>(GameObject GO) where T : Component
        {
            Debug_TTExt.Log("-------------------------------------------");
            Debug_TTExt.Log("PrintAllComponentsGameObjectDepth - For " + typeof(T).Name);
            Debug_TTExt.Log(" -- " + GO.name);
            foreach (var item in GO.GetComponentsInChildren<T>(true))
            {
                Debug_TTExt.Log("-------------------------------------------");
                Debug_TTExt.Log(" - " + item.gameObject.name);
                Transform trans = item.transform.parent;
                while (trans != null)
                {
                    Debug_TTExt.Log("  " + trans.gameObject.name);
                    trans = trans.parent;
                }
            }
            Debug_TTExt.Log("-------------------------------------------");
        }


        public static void GUIWarningPopup(Tank tank, ref InfoOverlay IO, string title, string desc, string typeDesc = null, Sprite icon = null)
        {
            if (tank == null)
                return;
            Visible vis = tank.visible;
            if (vis != null)
            {
                if (IOD == null)
                {
                    IO = ManOverlay.inst.AddWarningOverlay(vis, true);
                    IO.OverrideDataHook(GUIWarningPopup_IODSetup);
                    ManOverlay.inst.RemoveWarningOverlay(IO);
                    IO = null;
                }
                if (IO == null || IO.HasExpired())
                {
                    IOD.m_DissappearDelay = 4f;
                    IO = new InfoOverlay(IOD);
                    IO.Clear();
                    IO.Setup(vis);
                    LastWarnState = title;
                    LastWarnDesc = desc;
                    LastWarnType = typeDesc;
                    LastWarnSprite = icon;
                    IO.OverrideDataHook(GUIWarningPopup_Internal);
                    overlays.Invoke(ManOverlay.inst, new object[1] { IO });
                }
                else if (IO.Subject != vis)
                {
                    ManOverlay.inst.RemoveWarningOverlay(IO);
                    IOD.m_DissappearDelay = 4f;
                    IO = new InfoOverlay(IOD);
                    IO.Clear();
                    IO.Setup(vis);
                    LastWarnState = title;
                    LastWarnDesc = desc;
                    LastWarnType = typeDesc;
                    LastWarnSprite = icon;
                    IO.OverrideDataHook(GUIWarningPopup_Internal);
                    overlays.Invoke(ManOverlay.inst, new object[1] { IO });
                }
                IO.ResetDismissTimer();
                LastWarnState = title;
                LastWarnDesc = desc;
                LastWarnType = typeDesc;
                LastWarnSprite = icon;
                IO.OverrideDataHook(GUIWarningPopup_Internal);
            }
        }
        private static string LastWarnState = null;
        private static string LastWarnDesc = null;
        private static string LastWarnType = null;
        private static Sprite LastWarnSprite = null;
        private static InfoOverlayData IOD = null;
        private static InfoOverlayDataValues GUIWarningPopup_IODSetup(InfoOverlayDataValues data)
        {
            Debug_TTExt.Log("RandomAdditions.IOD");
            IOD = new GameObject("RandomAdditions.IOD").AddComponent<InfoOverlayData>();
            IOD.m_DismissWhenGrabbed = true;
            IOD.m_DismissWhenOffScreen = true;
            IOD.m_DissappearDelay = 2.5f;
            IOD.m_Expandable = true;
            IOD.m_PanelMaxDisplayDistance = 750;
            IOD.m_Priority = 9001;//-2;
            IOD.m_IconSprite = null;
            IOD.m_IconColour = data.m_Config.m_IconColour;
            IOD.m_LineMat = data.m_Config.m_LineMat;
            IOD.m_PanelPrefab = data.m_Config.m_PanelPrefab;
            IOD.m_SubtitleTickerDuration = data.m_Config.m_SubtitleTickerDuration;
            IOD.m_ZPos = data.m_Config.m_ZPos;
            IOD.m_HiddenInModes = new List<ManGameMode.GameType>();
            return data;
        }
        private static InfoOverlayDataValues GUIWarningPopup_Internal(InfoOverlayDataValues data)
        {
            Debug_TTExt.Log("RandomAdditions.GUIWarningPopup_Internal");
            data.OverrideSubtitle = false;
            data.m_MainTitle = LastWarnState;
            data.m_Config.m_IconSprite = LastWarnSprite;
            if (LastWarnDesc.Length > 20)
            {
                data.m_Config.m_Expandable = true;
                data.m_Description = LastWarnDesc;
            }
            else
            {
                data.m_Subtitle = LastWarnDesc;
                data.m_Config.m_Expandable = false;
            }
            data.IconSprite = LastWarnSprite;
            data.m_Category = LastWarnType;
            Debug_TTExt.Log("RandomAdditions.GUIWarningPopup_Internal end " + data.m_MainTitle);
            return data;
        }



        public class NetBigMessage : MessageBase
        {
            public NetBigMessage() { }
            public NetBigMessage(int team, string desc, bool noise)
            {
                Noise = noise;
                Team = team;
                Desc = desc;
            }

            public bool Noise;
            public int Team;
            public string Desc;
        }
        private static NetworkHook<NetBigMessage> netHook = new NetworkHook<NetBigMessage>(OnReceiveBigMessage, NetMessageType.FromClientToServerThenClients);

        private static bool OnReceiveBigMessage(NetBigMessage command, bool isServer)
        {
            if (!isServer)
            {
                if (command.Team == 0)
                {
                    DoBigF5broningBanner(command.Desc, command.Noise);
                }
                else
                {
                    if (ManPlayer.inst.PlayerTeam == command.Team)
                        DoBigF5broningBanner(command.Desc, command.Noise);
                }
                return true;
            }
            else
                return true;
            return false;
        }

        internal static void InsureNetHooks()
        {
            netHook.Register();
        }


        /// <summary>
        /// The BigF5broningBanner is active and shown on the screen.
        /// </summary>
        public static bool BigF5broningBannerActive => bannerActive;
        private static UIMultiplayerHUD warningBanner;
        private static bool bannerActive = false;
        private static bool subbed = false;
        /// <summary>
        /// Make a big, nice obvious warning on the screen that's nearly impossible to miss.
        /// </summary>
        /// <param name="Text">What text to show on the banner.  Set to nothing to hide immedeately.</param>
        /// <param name="startNoise">Play payload inbound warning SFX for duration of showing.</param>
        public static void BigF5broningBanner(string Text, bool startNoise = true)
        {
            if (netHook.CanBroadcast())
                netHook.TryBroadcast(new NetBigMessage(0, Text, startNoise));
            else
                DoBigF5broningBanner(Text, startNoise);
        }
        public static void BigF5broningBanner(int team, string Text, bool startNoise = true)
        {
            if (netHook.CanBroadcast())
                netHook.TryBroadcast(new NetBigMessage(team, Text, startNoise));
            else
                DoBigF5broningBanner(Text, startNoise);
        }
        private static void DoBigF5broningBanner(string Text, bool startNoise)
        {
            if (!Singleton.Manager<ManHUD>.inst.GetHudElement(ManHUD.HUDElementType.Multiplayer))
            {
                Debug_TTExt.Log("TTUtil: UIHelpersExt - init warningBanner");
                Singleton.Manager<ManHUD>.inst.InitialiseHudElement(ManHUD.HUDElementType.Multiplayer);
            }
            Singleton.Manager<ManHUD>.inst.ShowHudElement(ManHUD.HUDElementType.Multiplayer);
            if (!warningBanner)
                warningBanner = (UIMultiplayerHUD)Singleton.Manager<ManHUD>.inst.GetHudElement(ManHUD.HUDElementType.Multiplayer);
            if (!warningBanner)
            {
                Debug_TTExt.Assert("TTUtil: UIHelpersExt - warningBanner IS NULL");
                return;
            }
            if (warningBanner.Message.NullOrEmpty())
            {
                if (startNoise)
                    ManSFX.inst.PlayMiscLoopingSFX(ManSFX.MiscSfxType.PayloadIncoming);
            }
            else
                InvokeHelper.CancelInvokeSingle(RemoveWarning);
            if (Text == null)
                Text = string.Empty;
            warningBanner.Message = Text;
            InvokeHelper.InvokeSingle(RemoveWarning, 4f);
            if (!subbed)
            {
                warningBanner.OnMessageChanged.Subscribe(RemoveSub);
                subbed = true;
            }
            bannerActive = true;
        }
        private static void RemoveSub(string unused)
        {
            if (subbed)
            {
                InvokeHelper.CancelInvokeSingle(RemoveWarning);
                warningBanner.OnMessageChanged.Unsubscribe(RemoveSub);
                subbed = false;
            }
        }
        private static void RemoveWarning()
        {
            RemoveSub(null);
            ManSFX.inst.StopMiscLoopingSFX(ManSFX.MiscSfxType.PayloadIncoming);
            warningBanner.Message = "";
            bannerActive = false;
        }

        /// <summary>
        /// Client Only!
        /// </summary>
        public static void PointAtTransform(Transform trans, Vector3 offset, float duration)
        {
            if (!Singleton.Manager<ManHUD>.inst.GetHudElement(ManHUD.HUDElementType.BouncingArrow))
            {
                Debug_TTExt.Log("TTUtil: UIHelpersExt - init BouncingArrow");
                Singleton.Manager<ManHUD>.inst.InitialiseHudElement(ManHUD.HUDElementType.BouncingArrow);
            }
            UIBouncingArrow.BouncingArrowContext context = new UIBouncingArrow.BouncingArrowContext
            {
                targetTransform = trans,
                targetOffset = offset,
                forTime = duration,
            };
            Singleton.Manager<ManHUD>.inst.ShowHudElement(ManHUD.HUDElementType.BouncingArrow, context);
        }
        public static void StopPointing()
        {
            if (Singleton.Manager<ManHUD>.inst.GetHudElement(ManHUD.HUDElementType.BouncingArrow))
            {
                Singleton.Manager<ManHUD>.inst.HideHudElement(ManHUD.HUDElementType.BouncingArrow);
            }
        }

        /// <summary>
        /// Client Only!
        /// </summary>
        public static void PointAtThis(this Transform trans, Vector3 offset, float duration = 8)
        {
            PointAtTransform(trans, offset, duration);
        }
        public static void StopPointing(this Transform trans)
        {
            StopPointing();
        }

        /// <summary>
        /// Check if the mouse is over the given GUI.Window Rect
        /// </summary>
        /// <param name="pos">The Window's Rect on the screen</param>
        /// <returns>If the mouse is DIRECTLY within the specified rect on the screen</returns>
        public static bool MouseIsOverSubMenu(Rect pos)
        {
            Vector3 Mous = Input.mousePosition;
            Mous.y = Display.main.renderingHeight - Mous.y;
            float xMenuMin = pos.x;
            float xMenuMax = pos.x + pos.width;
            float yMenuMin = pos.y;
            float yMenuMax = pos.y + pos.height;
            //Debug_TTExt.Log(Mous + " | " + xMenuMin + " | " + xMenuMax + " | " + yMenuMin + " | " + yMenuMax);
            if (Mous.x > xMenuMin && Mous.x < xMenuMax && Mous.y > yMenuMin && Mous.y < yMenuMax)
            {
                return true;
            }
            return false;
        }
        /// <summary>
        /// Clamps the given Rect to be fully within bounds of the screen.  
        /// This may not be possible if the screen is too small to fit the Rect entirely.
        /// </summary>
        /// <param name="pos">The Rect to clamp within the screen bounds</param>
        /// <param name="centerOnMouse">If it should re-center on the mouse, then clamp to screen</param>
        public static void ClampMenuToScreen(ref Rect pos, bool centerOnMouse)
        {
            if (centerOnMouse)
            {
                Vector3 Mous = Input.mousePosition;
                pos.x = Mous.x - (pos.width / 2);
                pos.y = Display.main.renderingHeight - Mous.y - 90;
            }
            pos.x = Mathf.Clamp(pos.x, 0, Display.main.renderingWidth - pos.width);
            pos.y = Mathf.Clamp(pos.y, 0, Display.main.renderingHeight - pos.height);
        }
    }
    public interface GUI_BM_Element
    {
        string GetName { get; }
        bool GetSlider { get; }
        string GetSliderDescName { get; }
        Sprite GetIcon { get; }
        int GetClampSteps { get; }
        float GetLastVal { get; set; }
        float GetSet { get; set; }
    }
    public class GUI_BM_Element_Simple : GUI_BM_Element
    {
        public string GetName { get { return Name; } }
        public Sprite GetIcon
        {
            get
            {
                if (OnIcon != null)
                    return OnIcon.Invoke();
                return null;
            }
        }
        public bool GetSlider { get { return OnDesc != null; } }
        public string GetSliderDescName { get { return OnDesc.Invoke(); } }
        public int GetClampSteps { get { return ClampSteps; } }
        public float GetLastVal { get { return LastVal; } set { LastVal = value; } }
        public float GetSet { get { return OnSet(float.NaN); } set { OnSet.Invoke(value); } }
        public string Name;
        public Func<string> OnDesc;
        public Func<Sprite> OnIcon;
        public int ClampSteps;
        public float LastVal;
        public Func<float, float> OnSet;
    }
    public class GUI_BM_Element_Complex : GUI_BM_Element
    {
        public string GetName { get { return Name.Invoke(); } }
        public Sprite GetIcon
        {
            get
            {
                if (OnIcon != null)
                    return OnIcon.Invoke();
                return null;
            }
        }
        public bool GetSlider { get { return OnDesc != null; } }
        public string GetSliderDescName { get { return OnDesc.Invoke(); } }
        public int GetClampSteps { get { return ClampSteps; } }
        public float GetLastVal { get { return LastVal; } set { LastVal = value; } }
        public float GetSet
        {
            get
            {
                return OnSet(float.NaN);
            }
            set
            {
                OnSet.Invoke(value);
            }
        }
        public Func<string> Name;
        public Func<string> OnDesc;
        public Func<Sprite> OnIcon;
        public int ClampSteps;
        public float LastVal;
        public Func<float, float> OnSet;
    }
    /// <summary>
    /// THE POCKET BUTTON MANAGER
    /// </summary>
    public class GUIButtonMadness : MonoBehaviour
    {
        public static GUIButtonMadness Initiate(int ID, string Name, GUI_BM_Element[] elementsToUse, Func<bool> CanDisplay = null)
        {
            var GUIWindow = new GameObject();
            var madness = GUIWindow.AddComponent<GUIButtonMadness>();
            madness.Name = "<b>" + Name + "</b>";
            madness.setID = ID;
            if (CanDisplay == null)
                madness.displayRules = madness.DefaultCanContinueDisplay;
            else
                madness.displayRules = CanDisplay;
            madness.elements = elementsToUse;
            GUIWindow.SetActive(false);
            return madness;
        }
        public GUIButtonMadness ReInitiate(int ID, string Name, GUI_BM_Element[] elementsToUse, Func<bool> CanDisplay = null)
        {
            CloseGUI();
            this.Name = "<b>" + Name + "</b>";
            setID = ID;
            if (CanDisplay == null)
                displayRules = DefaultCanContinueDisplay;
            else
                displayRules = CanDisplay;
            elements = elementsToUse;
            return this;
        }
        public void DeInit()
        {
            Destroy(gameObject);
        }

        internal Rect HotWindow = new Rect(0, 0, 250, 260);   // the "window"
        private bool UseRadialMode = true;
        private string Name;
        private int setID;
        private Func<bool> displayRules;
        private GUI_BM_Element[] elements;
        private void Update()
        {
            if (openTime > 0)
                openTime -= Time.deltaTime;
        }
        private void OnGUI()
        {
            if (displayRules.Invoke())
            {
                AltUI.StartUI();
                HotWindow = GUI.Window(setID, HotWindow, GUIHandler, Name);
                if (UIHelpersExt.MouseIsOverSubMenu(HotWindow))
                    openTime = 1;
                AltUI.EndUI();
            }
            else
                CloseGUI();
        }
        public void SetDirty()
        {
            if (UseRadialMode)
            {
                InsureRadialMenuPrefabs();
                if (MenuPanelPrefabs.TryGetValue(elements.Length, out PlaceholderRadialMenu PRM))
                {
                    //Debug_TTExt.Log("GUIButtonMadness.OpenGUI() - " + Time.time);
                    PRM.SetDirty();
                }
            }
        }
        public bool DefaultCanDisplay()
        {
            return UIHelpersExt.IsIngame;
        }
        public bool DefaultCanContinueDisplay()
        {
            return UIHelpersExt.IsIngame && (UseRadialMode || openTime > 0 || UIHelpersExt.MouseIsOverSubMenu(HotWindow));
        }

        private float openTime = 0;
        private static Vector2 scrolll = new Vector2(0, 0);
        private static float scrolllSize = 50;
        private const int ButtonWidth = 200;
        private const int MaxWindowHeight = 500;
        private void GUIHandler(int ID)
        {

            bool clicked = false;
            int VertPosOff = 0;
            bool MaxExtensionY = false;

            scrolll = GUI.BeginScrollView(new Rect(0, 30, HotWindow.width - 20, HotWindow.height - 40), scrolll, new Rect(0, 0, HotWindow.width - 50, scrolllSize));

            int Entries = elements.Length;
            for (int step = 0; step < Entries; step++)
            {
                try
                {
                    try
                    {
                        GUI_BM_Element ele = elements[step];

                        string disp = "<color=#ffffffff>" + ele.GetName + "</color>";

                        if (ele.GetSlider)
                        {
                            int offset = ButtonWidth / 2;
                            GUI.Label(new Rect(20, VertPosOff, offset, 30), disp);
                            float cache = GUI.HorizontalSlider(new Rect(20 + offset, VertPosOff, offset, 30)
                                , ele.GetLastVal, 0, 1);
                            if (ele.GetClampSteps > 1)
                                cache = Mathf.RoundToInt(cache * ele.GetClampSteps) / (float)ele.GetClampSteps;
                            if (!ele.GetSet.Approximately(cache))
                            {
                                ele.GetLastVal = cache;
                                ele.GetSet = cache;
                                ManSFX.inst.PlayUISFX(ManSFX.UISfxType.Slider);
                            }
                        }
                        else if (GUI.Button(new Rect(20, VertPosOff, ButtonWidth, 30), disp))
                        {
                            clicked = true;
                            ele.GetSet = 1;
                            ManSFX.inst.PlayUISFX(ManSFX.UISfxType.Button);
                        }
                    }
                    catch { }
                    VertPosOff += 30;
                    if (VertPosOff >= MaxWindowHeight)
                        MaxExtensionY = true;
                }
                catch { }// error on handling something
            }

            GUI.EndScrollView();
            scrolllSize = VertPosOff + 50;

            if (MaxExtensionY)
                HotWindow.height = MaxWindowHeight + 80;
            else
                HotWindow.height = VertPosOff + 80;

            HotWindow.width = ButtonWidth + 60;
            if (clicked)
            {
                ManSFX.inst.PlayUISFX(ManSFX.UISfxType.CheckBox);
            }

            GUI.DragWindow();
        }

        private static Dictionary<int, PlaceholderRadialMenu> MenuPanelPrefabs = null;

        private static FieldInfo fabs = typeof(UIHUD).GetField("m_HUDElements", BindingFlags.NonPublic | BindingFlags.Instance);
        internal static FieldInfo sidle = typeof(UISliderControlRadialMenu).GetField("m_Slider", BindingFlags.NonPublic | BindingFlags.Instance);
        internal static FieldInfo ig = typeof(UISliderControlRadialMenu).GetField("m_LeftOptionIconGroups", BindingFlags.NonPublic | BindingFlags.Instance);
        internal static Type ig2 = typeof(UISliderControlRadialMenu).GetNestedType("LeftOptionIconGroup", BindingFlags.NonPublic | BindingFlags.Instance);
        internal static FieldInfo ig3 = ig2.GetField("m_IconSprite", BindingFlags.Public | BindingFlags.Instance);
        public static void InsureRadialMenuPrefabs()
        {
            if (MenuPanelPrefabs != null)
                return;
            MenuPanelPrefabs = new Dictionary<int, PlaceholderRadialMenu>();
            Dictionary<int, UIHUDElement> fabD = (Dictionary<int, UIHUDElement>)fabs.GetValue(ManHUD.inst.CurrentHUD);

            UIRadialMenuSlider slider;

            GrabIconsFrom(fabD, ManHUD.HUDElementType.ConveyorMenu);
            GrabIconsFrom(fabD, ManHUD.HUDElementType.TrimControl);
            GrabIconsFrom(fabD, ManHUD.HUDElementType.MPTechActions);

            MakeRadialMenuPrefabType<UIPowerToggleBlockMenu>(fabD, ManHUD.HUDElementType.PowerToggleBlockMenu, 1, null);
            MakeRadialMenuPrefabTypeSlider(fabD, ManHUD.HUDElementType.SliderControlRadialMenu, 2, out slider);
            MakeRadialMenuPrefabType<UIRadialBlockControllerMenu>(fabD, ManHUD.HUDElementType.BlockControl, 3, slider);
            MakeRadialMenuPrefabType<UIRadialTechHeartbeatMenu>(fabD, ManHUD.HUDElementType.Pacemaker, 4, slider);
            MakeRadialMenuPrefabType<UIRadialTechControlMenu>(fabD, ManHUD.HUDElementType.TechControlChoice, 5, slider);
            MakeRadialMenuPrefabType<UIRadialTechAndBlockActionsMenu>(fabD, ManHUD.HUDElementType.TechAndBlockActions, 6, slider);
        }
        private static void MakeRadialMenuPrefabTypeSlider(Dictionary<int, UIHUDElement> fabD, ManHUD.HUDElementType hudHost,
            int numButtons, out UIRadialMenuSlider slider)
        {
            slider = null;
            fabD.TryGetValue((int)hudHost, out UIHUDElement val);
            if (val is UISliderControlRadialMenu rad)
            {
                //Debug_TTExt.Log("UISliderControlRadialMenu is: \n"
                //    + Nuterra.NativeOptions.UIUtilities.GetComponentTree(rad.gameObject, "|"));
                GameObject GO = Instantiate(rad.gameObject, rad.transform.parent);
                var overrider = GO.AddComponent<PlaceholderRadialMenu>();
                int unstableStep = 0;
                try
                {
                    var arr = (Array)ig.GetValue(GO.GetComponent<UISliderControlRadialMenu>());
                    unstableStep++;
                    int step = 0;
                    foreach (var item in arr)
                    {
                        unstableStep += 10;
                        var spr = (Sprite)ig3.GetValue(Convert.ChangeType(item, ig2));
                        unstableStep += 100;
                        if (spr)
                        {
                            UIHelpersExt.CachedUISprites.Add("GUI_" + ((UISliderControlRadialMenu.LeftOptionIconType)step).ToString(), spr);
                            Debug_TTExt.Log("UIHelpers - Added Sprite GUI_" + ((UISliderControlRadialMenu.LeftOptionIconType)step).ToString() + " to quick lookup");
                            unstableStep += 1000;
                        }
                        step++;
                    }
                }
                catch
                {
                    Debug_TTExt.Log("UIHelpers - Could not fetch UISliderControlRadialMenu left icons due to error " + unstableStep);
                }
                slider = (UIRadialMenuSlider)sidle.GetValue(rad);
                overrider.AwakeAndModify<UISliderControlRadialMenu>(false, null, true);
                MenuPanelPrefabs.Add(numButtons, overrider);
            }
        }
        private static void MakeRadialMenuPrefabType<T>(Dictionary<int, UIHUDElement> fabD, ManHUD.HUDElementType hudHost,
            int numButtons, UIRadialMenuSlider slider) where T : UIHUDElement
        {
            fabD.TryGetValue((int)hudHost, out UIHUDElement val);
            if (val is T rad)
            {
                GameObject GO = Instantiate(rad.gameObject, rad.transform.parent);
                var overrider = GO.AddComponent<PlaceholderRadialMenu>();
                overrider.AwakeAndModify<T>(numButtons == 1, slider, false);
                MenuPanelPrefabs.Add(numButtons, overrider);
            }
        }

        private static void GrabIconsFrom(Dictionary<int, UIHUDElement> fabD, ManHUD.HUDElementType hudHost)
        {
            fabD.TryGetValue((int)hudHost, out UIHUDElement val);
            if (val != null && val.GetComponent<RadialMenu>())
            {
                PlaceholderRadialMenu.TryGetExistingTexturesAndLabels(val.GetComponent<RadialMenu>());
            }
        }

        public bool GUIIsOpen()
        {
            if (UseRadialMode)
            {
                InsureRadialMenuPrefabs();
                if (MenuPanelPrefabs.TryGetValue(elements.Length, out PlaceholderRadialMenu PRM))
                {
                    //Debug_TTExt.Log("GUIButtonMadness.OpenGUI() - " + Time.time);
                    return PRM.IsOpen;
                }
                return false;
            }
            else
            {
                UIHelpersExt.ClampMenuToScreen(ref HotWindow, true);
                return gameObject.activeSelf;
            }
        }
        public void OpenGUI(TankBlock opener)
        {
            openTime = 1f;
            //UseRadialMode = PlaceholderRadialMenu.CanUseWheel(elements);
            if (UseRadialMode)
            {
                InsureRadialMenuPrefabs();
                if (MenuPanelPrefabs.TryGetValue(elements.Length, out PlaceholderRadialMenu PRM))
                {
                    if (opener == null)
                        throw new NullReferenceException("OpenGUI - opener was null for some reason. It should NOT be null EVER!");
                    //Debug_TTExt.Log("GUIButtonMadness.OpenGUI() - " + Time.time);
                    PRM.ShowThis(opener, elements);
                }
                else
                    throw new IndexOutOfRangeException("OpenGUI - Index [" + elements.Length + "] is out of range of given: [1 - 6]");
            }
            else
            {
                UIHelpersExt.ClampMenuToScreen(ref HotWindow, true);
                gameObject.SetActive(true);
            }
        }
        public void CloseGUI()
        {
            if (UseRadialMode)
            {
                InsureRadialMenuPrefabs();
                if (MenuPanelPrefabs.TryGetValue(elements.Length, out PlaceholderRadialMenu PRM))
                {
                    PRM.Hide(null);
                }
            }
            else
            {
                UIHelpersExt.ReleaseControl();
                gameObject.SetActive(false);
            }
        }

    }
    /// <summary>
    /// Use Exund's over this one as it is crude
    /// </summary>
    public class PlaceholderRadialMenu : UIHUDElement
    {
        private const float timerDelay = 0.4f;
        private static FieldInfo radCOp = typeof(RadialMenu).GetField("m_CenterOption", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo radOpSub = typeof(RadialMenu).GetField("m_Submenus", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo radOp = typeof(RadialMenu).GetField("m_RadialOptions", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo radOpNum = typeof(RadialMenu).GetField("m_NumOptions", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo radOpSubMenuRect = typeof(RadialMenu).GetField("m_FakeSubMenuRect", BindingFlags.NonPublic | BindingFlags.Instance);
        private static MethodInfo radInit = typeof(RadialMenu).GetMethod("OnPool", BindingFlags.NonPublic | BindingFlags.Instance);
        private static MethodInfo radInitOp = typeof(UIRadialMenuOptionWithWarning).GetMethod("OnPool", BindingFlags.NonPublic | BindingFlags.Instance);
        //private static FieldInfo allowed = typeof(UIRadialMenuOptionWithWarning).GetField("m_IsAllowed", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo manual = typeof(UIRadialMenuOptionWithWarning).GetField("m_TooltipString", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo manualW = typeof(UIRadialMenuOptionWithWarning).GetField("m_TooltipWarningString", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo tog = typeof(UIPowerToggleBlockMenu).GetField("m_TimeoutBar", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo back = typeof(UIPowerToggleBlockMenu).GetField("m_BackgroundImage", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo setMan = typeof(TooltipComponent).GetField("m_ManuallySetText", BindingFlags.NonPublic | BindingFlags.Instance);

        //private static FieldInfo sidleTitle = typeof(UISliderControlRadialMenu).GetField("m_SettingValueTextTitle", BindingFlags.NonPublic | BindingFlags.Instance);
        //private static FieldInfo sidleDesc = typeof(UISliderControlRadialMenu).GetField("m_SettingValueTextDisplay", BindingFlags.NonPublic | BindingFlags.Instance);

        private bool SnapSlider = true;

        public bool IsOpen => radMenu.enabled;
        private bool single = false;
        private bool dirty = false;
        private float timer = 0;
        private Image clock;
        private Image backG;
        private GUI_BM_Element sliderSelected = null;

        public void ShowThis(TankBlock targ, GUI_BM_Element[] elements)
        {
            OpenMenuEventData OMED = new OpenMenuEventData
            {
                m_AllowNonRadialMenu = false,
                m_AllowRadialMenu = true,
                m_RadialInputController = ManInput.RadialInputController.Mouse,
                m_TargetTankBlock = targ,
            };

            elementsC = elements;
            InsureDominance();
            TryOverrideTexturesAndLabels(elementsC);
            //Debug_TTExt.Log("PlaceholderRadialMenu.ShowThis() - " + Time.time);
            Show(OMED);
        }
        public override void Show(object context)
        {
            if (context == null)
                throw new NullReferenceException("Show was given NULL context!");
            OpenMenuEventData OMED = (OpenMenuEventData)context;
            if (OMED.m_TargetTankBlock == null)
                throw new NullReferenceException("Show was given NULL m_TargetTankBlock");
            base.Show(context);
            radMenu.Show(OMED.m_RadialInputController, OMED.m_TargetTankBlock.tank != Singleton.playerTank);
            InsureDominance();
            sliderSelected = null;
            if (slider)
                SetSliderVis(false);
            //Debug_TTExt.Log("UIHelpers.Show() - " + Time.time);
            if (clock != null)
            {
                timer = timerDelay;
                clock.fillAmount = 0f;
                backG.color = new Color(0.25f, 0.45f, 0.9f, 0.8f);
                Debug_TTExt.Log("Show(single) - OPENED");
            }
            else
                Debug_TTExt.Log("Show(multi) - OPENED");
            radMenu.enabled = true;
            gameObject.SetActive(true);
        }

        public override void Hide(object context)
        {
            gameObject.SetActive(false);
            InsureDominance();
            base.Hide(context);
            //radMenu.OnClose.PrintSubscribers("PlaceholderRadialMenu.radMenu.OnClose Subs: \n");
            radMenu.Hide();
            radMenu.enabled = false;
        }

        public void SetDirty()
        {
            dirty = true;
        }


        private void Update()
        {
            if (dirty)
            {
                TryOverrideTexturesAndLabels(elementsC);
                dirty = false;
            }
            if (clock != null)
            {
                if (timer > 0)
                {
                    timer -= Time.deltaTime;
                    float val = 1f - (timer / timerDelay);
                    clock.fillAmount = val;
                    backG.color = new Color((val * 0.2f) + 0.05f, (val * 0.4f) + 0.05f, (val * 0.85f) + 0.05f, 0.8f);
                }
                else
                {
                    if (timer != 0)
                    {
                        timer = 0;
                        clock.fillAmount = 1;
                        backG.color = new Color(0.25f, 0.85f, 0.45f, 0.8f);
                    }
                }
            }
        }
        private void OnOptionSelect(int wedgeNum)
        {
            if (single)
            {
                if (timer == 0)
                {
                    elementsC[0].GetSet = 1;
                    Debug_TTExt.Log("OnOptionSelect(single) - sent " + wedgeNum);
                }
            }
            else
            {
                if (wedgeNum >= 0 && wedgeNum < elementsC.Length)
                {
                    if (elementsC[wedgeNum].GetSlider)
                    {
                        if (!sliderPanel)
                            throw new InvalidOperationException("PlaceholderRadialMenu - OnOptionSelect() called for a sliderPanel when there is none attached!");
                        if (!sliderPanel.activeSelf)
                        {
                            if (!slider)
                                throw new InvalidOperationException("PlaceholderRadialMenu - OnOptionSelect() called for a slider when there is none attached!");
                            sliderSelected = null;
                            sliderDesc.text = elementsC[wedgeNum].GetSliderDescName;
                            sliderTitle.text = elementsC[wedgeNum].GetName;
                            slider.SetValue(elementsC[wedgeNum].GetSet);
                            sliderSelected = elementsC[wedgeNum];
                            slider.value = sliderSelected.GetSet;
                            SetSliderVis(true);
                            radMenu.SetModal(true, wedgeNum, false);
                            return;
                        }
                    }
                    else
                    {
                        if (slider)
                            SetSliderVis(false);
                        elementsC[wedgeNum].GetSet = 1;
                        Debug_TTExt.Log("OnOptionSelect - sent " + wedgeNum);
                    }
                }
            }
            Hide(null);
        }

        internal static bool CanUseWheel(GUI_BM_Element[] elements)
        {
            foreach (var item in elements)
            {
                if (item.GetSlider)
                    return false;
            }
            return true;
        }
        internal void AwakeAndModify<T>(bool singleButton, UIRadialMenuSlider Slider, bool IsPrevSlider) where T : UIHUDElement
        {
            single = singleButton;
            radMenu = gameObject.GetComponent<RadialMenu>();
            var RT = radOpSubMenuRect.GetValue(radMenu);
            radMenu.OnOptionSelected.EnsureNoSubscribers();
            radMenu.OnClose.EnsureNoSubscribers();
            radMenu.OnOptionHovered.EnsureNoSubscribers();

            bool sliderPrefab = true;
            var pruneTarg = gameObject.GetComponent<T>();
            if (pruneTarg)
            {
                if (Slider == null && IsPrevSlider && pruneTarg is UISliderControlRadialMenu radM)
                {
                    Slider = radMenu.GetComponentInChildren<UIRadialMenuSlider>(true);
                    sliderPrefab = false;
                    //Debug_TTExt.Log("AwakeAndModify - UIRadialMenuSlider repurposed");
                    if (Slider == null)
                        Debug_TTExt.LogError("AwakeAndModify - UIRadialMenuSlider MISSING SLIDER");
                }
                if (pruneTarg is UIPowerToggleBlockMenu UIPTBM)
                {
                    clock = (Image)tog.GetValue(UIPTBM);
                    backG = (Image)back.GetValue(UIPTBM);
                }
                //Debug_TTExt.Log("AwakeAndModify - removed " + typeof(T).Name);
                DestroyImmediate(pruneTarg);
            }
            if (Slider != null)
            {
                if (sliderPrefab)
                {
                    //Debug_TTExt.Log("Log 1");
                    Transform depther = Slider.transform;
                    GameObject GO = Instantiate(Slider.transform.parent.parent.gameObject, radMenu.transform);
                    //Debug_TTExt.Log("Log 2");
                    var rTrans = GO.transform;
                    var rTransO = Slider.transform.parent.parent;
                    rTrans.localPosition = rTransO.localPosition;
                    rTrans.localRotation = rTransO.localRotation;
                    rTrans.localScale = rTransO.localScale;
                    //Debug_TTExt.Log("Log 3");
                    Slider = GO.transform.GetComponentInChildren<UIRadialMenuSlider>(true);
                }
                //Debug_TTExt.Log("Log 4");
                slider = Slider;
                sliderPanel = Slider.transform.parent.parent.gameObject;
                slider.minValue = 0;
                slider.maxValue = 1;
                slider.value = 0;
                slider.onValueChanged.RemoveAllListeners();
                slider.onValueChanged.AddListener(OnSliderValChanged);
                //Debug_TTExt.Log("Log 5");
                try
                {
                    sliderTitle = transform.HeavyTransformSearch("SliderTitle").GetComponent<Text>();
                }
                catch { Debug_TTExt.LogError("COULD NOT FETCH SliderTitle"); }
                try
                {
                    sliderDesc = transform.HeavyTransformSearch("SliderSetting").GetComponent<Text>();
                }
                catch { Debug_TTExt.LogError("COULD NOT FETCH SliderSetting"); }
            }

            CorrectAllInvalid();
            radInit.Invoke(radMenu, new object[0]);

            radMenu.OnOptionSelected.Subscribe(OnOptionSelect);
            radMenu.OnClose.Subscribe(Hide);
            radOpSub.SetValue(radMenu, new RadialMenuSubmenu[0]);
            if (slider)
                SetSliderVis(false);
            radOpSubMenuRect.SetValue(radMenu, RT);
            TryGetExistingTexturesAndLabels(radMenu);
        }
        internal void InsureDominance()
        {
            radMenu.OnClose.EnsureNoSubscribers();
            radMenu.OnClose.Subscribe(Hide);
        }

        private void CorrectAllInvalid()
        {
            var radMenuOp = (UIRadialMenuOption[])radOp.GetValue(radMenu);
            for (int step = 0; step < radMenuOp.Length; step++)
            {
                if (radMenuOp[step] is UIRadialMenuOptionDelayTimer crasher)
                {
                    var button = radMenuOp[step];
                    GameObject GO = button.gameObject;
                    for (int step2 = 1; step2 < button.transform.childCount; step2++)
                    {
                        Destroy(button.transform.GetChild(step2).gameObject);
                    }
                    var cache2 = crasher.colors;
                    var cache3 = crasher.image;
                    var cache4 = crasher.targetGraphic;
                    var cache8 = crasher.TooltipEnabled;
                    DestroyImmediate(crasher);
                    var newM = GO.AddComponent<UIRadialMenuOptionWithWarning>();
                    newM.colors = cache2;
                    newM.image = cache3;
                    newM.targetGraphic = cache4;
                    newM.TooltipEnabled = cache8;
                    radMenuOp[step] = newM;
                    //radInitOp2.Invoke(crasher, new object[0]); // breaks 
                }
                if (radMenuOp[step] is UIRadialMenuOptionWithWarning warn)
                {
                    LocalisedString sharedLoc = new LocalisedString()
                    {
                        m_Bank = "Unset",
                        m_GUIExpanded = false,
                        m_InlineGlyphs = new Localisation.GlyphInfo[0],
                        m_Id = "MOD",
                    };
                    manual.SetValue(warn, sharedLoc);
                    manualW.SetValue(warn, sharedLoc);
                    radInitOp.Invoke(warn, new object[0]);
                    warn.SetIsAllowed(true);
                }
            }
            radOp.SetValue(radMenu, radMenuOp);
            radOpNum.SetValue(radMenu, radMenuOp.Length);
        }

        internal static void TryGetExistingTexturesAndLabels(RadialMenu rm)
        {
            int step;
            var radMenuOp = (UIRadialMenuOption[])radOp.GetValue(rm);
            var radMenuC = (UIRadialMenuOption)radCOp.GetValue(rm);
            var transform = rm.transform;
            if (radMenuC)
            {
                var button = radMenuC;
                Image buttonImage = null;
                try
                {
                    buttonImage = transform.Find("Icon").GetComponent<Image>();
                }
                catch { }
                if (buttonImage == null)
                    buttonImage = button.GetComponent<Image>();
                if (buttonImage != null && buttonImage.sprite != null && !UIHelpersExt.CachedUISprites.ContainsKey(buttonImage.sprite.name))
                {
                    UIHelpersExt.CachedUISprites.Add(buttonImage.sprite.name, buttonImage.sprite);
                    Debug_TTExt.Log("UIHelpers - Added Sprite " + buttonImage.sprite.name + " to quick lookup");
                }
                for (step = 0; step < radMenuOp.Length; step++)
                {
                    button = radMenuOp[step];
                    buttonImage = button.transform.GetChild(0).GetComponent<Image>();
                    if (buttonImage != null && buttonImage.sprite != null && !UIHelpersExt.CachedUISprites.ContainsKey(buttonImage.sprite.name))
                    {
                        UIHelpersExt.CachedUISprites.Add(buttonImage.sprite.name, buttonImage.sprite);
                        Debug_TTExt.Log("UIHelpers - Added Sprite " + buttonImage.sprite.name + " to quick lookup");
                    }
                }
            }
            else
            {
                for (step = 0; step < radMenuOp.Length; step++)
                {
                    var button = radMenuOp[step];
                    Image buttonImage = button.transform.GetChild(0).GetComponent<Image>();
                    if (buttonImage != null && buttonImage.sprite != null && !UIHelpersExt.CachedUISprites.ContainsKey(buttonImage.sprite.name))
                    {
                        UIHelpersExt.CachedUISprites.Add(buttonImage.sprite.name, buttonImage.sprite);
                        Debug_TTExt.Log("UIHelpers - Added Sprite " + buttonImage.sprite.name + " to quick lookup");
                    }
                }
            }
        }
        private void TryOverrideTexturesAndLabels(GUI_BM_Element[] elements)
        {
            ModContainer MC = ManMods.inst.FindMod("Random Additions");
            int lowNum;
            int step;
            var radMenuOp = (UIRadialMenuOption[])radOp.GetValue(radMenu);
            var radMenuC = (UIRadialMenuOption)radCOp.GetValue(radMenu);
            if (single && radMenuC && elements.Length > 0)
            {
                var button = radMenuC;
                var ele = elements[0];
                Image buttonImage = null;
                try
                {
                    buttonImage = transform.Find("Icon").GetComponent<Image>();
                }
                catch { }
                if (buttonImage == null)
                    buttonImage = button.GetComponent<Image>();
                if (buttonImage != null)
                {
                    if (ele.GetIcon != null)
                    {
                        buttonImage.sprite = ele.GetIcon;
                    }
                    else
                    {
                        buttonImage.sprite = UIHelpersExt.GetIconFromBundle(MC, "GUI_" + ele.GetName);
                    }
                }
                var tooltip = button.GetComponent<TooltipComponent>();
                if (tooltip != null)
                {
                    setMan.SetValue(tooltip, false);
                    tooltip.SetText(ele.GetName);
                }
                if (button is UIRadialMenuOptionWithWarning warn)
                {
                    warn.SetIsAllowed(true);
                    LocalisedString sharedLoc = new LocalisedString()
                    {
                        m_Bank = ele.GetName,
                        m_GUIExpanded = false,
                        m_InlineGlyphs = new Localisation.GlyphInfo[0],
                        m_Id = "MOD",
                    };
                    manual.SetValue(warn, sharedLoc);
                    manualW.SetValue(warn, sharedLoc);
                }

                lowNum = Mathf.Min(radMenuOp.Length, elements.Length - 1);
                step = 0;
                for (; step < lowNum; step++)
                {
                    button = radMenuOp[step];
                    ele = elements[step + 1];
                    buttonImage = button.transform.GetChild(0).GetComponent<Image>();
                    if (buttonImage != null)
                    {
                        if (ele.GetIcon != null)
                        {
                            buttonImage.sprite = ele.GetIcon;
                        }
                        else
                        {
                            buttonImage.sprite = UIHelpersExt.GetIconFromBundle(MC, "GUI_" + ele.GetName);
                        }
                    }
                    tooltip = button.GetComponent<TooltipComponent>();
                    if (tooltip != null)
                    {
                        setMan.SetValue(tooltip, false);
                        tooltip.SetText(ele.GetName);
                    }
                    if (button is UIRadialMenuOptionWithWarning warn2)
                    {
                        warn2.SetIsAllowed(true);
                        LocalisedString sharedLoc = new LocalisedString()
                        {
                            m_Bank = ele.GetName,
                            m_GUIExpanded = false,
                            m_InlineGlyphs = new Localisation.GlyphInfo[0],
                            m_Id = "MOD",
                        };
                        manual.SetValue(warn2, sharedLoc);
                        manualW.SetValue(warn2, sharedLoc);
                    }
                }
            }
            else
            {
                lowNum = Mathf.Min(radMenuOp.Length, elements.Length);
                step = 0;
                for (; step < lowNum; step++)
                {
                    var button = radMenuOp[step];
                    var ele = elements[step];
                    Image buttonImage = button.transform.GetChild(0).GetComponent<Image>();
                    if (buttonImage != null)
                    {
                        if (ele.GetIcon != null)
                        {
                            buttonImage.sprite = ele.GetIcon;
                        }
                        else
                        {
                            buttonImage.sprite = UIHelpersExt.GetIconFromBundle(MC, "GUI_" + ele.GetName);
                        }
                    }
                    var tooltip = button.GetComponent<TooltipComponent>();
                    if (tooltip != null)
                    {
                        setMan.SetValue(tooltip, false);
                        tooltip.SetText(ele.GetName);
                    }
                    if (button is UIRadialMenuOptionWithWarning warn)
                    {
                        warn.SetIsAllowed(true);
                        LocalisedString sharedLoc = new LocalisedString()
                        {
                            m_Bank = ele.GetName,
                            m_GUIExpanded = false,
                            m_InlineGlyphs = new Localisation.GlyphInfo[0],
                            m_Id = "MOD",
                        };
                        manual.SetValue(warn, sharedLoc);
                        manualW.SetValue(warn, sharedLoc);
                    }
                }
            }
        }
        internal static Rect SnapVerticalScale(Rect refRect, float imageWidth, float imageHeight)
        {
            return new Rect(refRect.x, refRect.y, (imageWidth / imageHeight) * refRect.height, refRect.height);
        }

        private static bool ignoreOneFrame = false;
        private void OnSliderValChanged(float val)
        {
            if (sliderSelected != null && !ignoreOneFrame)
            {
                sliderSelected.GetSet = val;
                sliderSelected.GetLastVal = val;
                if (SnapSlider)
                {
                    ignoreOneFrame = true;
                    slider.value = sliderSelected.GetSet;
                    ignoreOneFrame = false;
                }
                sliderDesc.text = sliderSelected.GetSliderDescName;
            }
        }
        private void SetSliderVis(bool active)
        {
            if (sliderPanel.activeSelf != active)
            {
                sliderPanel.SetActive(active);
                slider.gameObject.SetActive(active);
                slider.SetIsHighlighted(active);
            }
        }


        private RadialMenu radMenu;
        private GUI_BM_Element[] elementsC;
        private UIRadialMenuSlider slider = null;
        private GameObject sliderPanel;
        private Text sliderTitle;
        private Text sliderDesc;
    }
}
