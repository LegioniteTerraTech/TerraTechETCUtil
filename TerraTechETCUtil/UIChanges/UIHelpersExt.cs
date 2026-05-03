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
    /// <summary>
    /// A bunch of helpers for UI and IMGUI.
    /// <para>See <seealso cref="AltUI"/> and <seealso cref="ManModGUI"/> for more UI helpers</para>
    /// </summary>
    public static class UIHelpersExt
    {
        private static FieldInfo radialShowDelay = typeof(ManHUD).GetField("m_RadialShowDelay", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo radialMaxMouseDist = typeof(ManHUD).GetField("m_RadialMouseDistanceThreshold", BindingFlags.NonPublic | BindingFlags.Instance);
        private static MethodInfo overlays = typeof(ManOverlay).GetMethod("AddQueuedOverlay", BindingFlags.NonPublic | BindingFlags.Instance);

        /// <summary>
        /// That default wrench icon used for mod blocks
        /// </summary>
        public static Sprite ModContentIcon { get; } = ResourcesHelper.GetTexture2DFromBaseGameAllFast("ICON_MOD").ConvertToSprite();
        /// <summary>
        /// The same as <see cref="ModContentIcon"/> but as a block sprite
        /// </summary>
        public static Sprite NullSprite => ManUI.inst.GetSprite(ObjectTypes.Block, -1);
        internal static bool UseNullIfNoSpriteProvided = true;
        internal static float _ROROpenTimeDelay = 0.10f;
        /// <summary>
        /// The Vanilla GUI <see cref="RadialMenu"/> modal show delay
        /// </summary>
        public static float ROROpenTimeDelay => _ROROpenTimeDelay;
        internal static float _ROROpenAllowedMouseDeltaSqr = 0;
        /// <summary>
        /// The Vanilla GUI <see cref="RadialMenu"/> modal minimum mouse move distance before <see cref="ROROpenTimeDelay"/> happens.
        /// <para>If the mouse exceeds this distance the radial menu shall not open</para>
        /// </summary>
        public static float ROROpenAllowedMouseDeltaSqr => _ROROpenAllowedMouseDeltaSqr;
        /// <summary>
        /// The custom element enum type to flag it as a <see cref="ManModGUI"/> managed UI element
        /// </summary>
        public static readonly ManHUD.HUDElementType customElement = (ManHUD.HUDElementType)(-1);
        internal static Dictionary<string, Sprite> CachedUISprites = new Dictionary<string, Sprite>();

        /// <summary>
        /// If the player can use the mouse to interact with blocks
        /// </summary>
        public static bool IsIngame { get { return !ManPauseGame.inst.IsPaused && !ManPointer.inst.IsInteractionBlocked; } }

        /// <summary>
        /// Release control of all IMGUI windows. Excludes vanilla's UI system
        /// </summary>
        /// <param name="Name"></param>
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

        /// <summary>
        /// Init <see cref="UIHelpersExt"/>
        /// </summary>
        public static void Init()
        {
            _ROROpenTimeDelay = (float)radialShowDelay.GetValue(ManHUD.inst);
            int dis = (int)radialMaxMouseDist.GetValue(ManHUD.inst);
            _ROROpenAllowedMouseDeltaSqr = dis * dis;
        }

        /// <summary>
        /// Log all of the available vanilla options that <see cref="GetGUIIcon(string)"/> can re-use for mods
        /// </summary>
        public static void LogCachedIcons()
        {
            GUIModModal.InsureRadialMenuPrefabs();
            Debug_TTExt.Log("------ ICONS CACHED ------");
            foreach (var item in CachedUISprites)
            {
                Debug_TTExt.Log(item.Key.NullOrEmpty() ? "NULL" : item.Key);
            }
            Debug_TTExt.Log("------ END CACHED ------");
        }
        /// <summary>
        /// Get a sprite for use in the modal or UI.
        /// <para>You can get them all at runtime by calling <see cref="LogCachedIcons"/></para>
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        /// <exception cref="NullReferenceException"></exception>
        public static Sprite GetGUIIcon(string name)
        {
            GUIModModal.InsureRadialMenuPrefabs();
            if (!CachedUISprites.TryGetValue(name, out var val))
                throw new NullReferenceException("Icon " + name + " does not exist in UIHelpers");
            return val;
        }
        /// <summary>
        /// Get a sprite from an image file in a <see cref="ModContainer"/>.
        /// <para>You can access <see cref="ModContainer"/>s of mods quickly through <see cref="ResourcesHelper"/></para>
        /// </summary>
        /// <param name="MC"></param>
        /// <param name="name">Name of the texture file to use as a sprite</param>
        /// <returns></returns>
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
        /// <inheritdoc cref=" Utilities.PrintAllComponentsGameObjectDepth{T}(GameObject)"/>
        public static void PrintAllComponentsGameObjectDepth<T>(GameObject GO) where T : Component =>
            Utilities.PrintAllComponentsGameObjectDepth<T>(GO);

        /// <summary>
        /// Show a popup in the style of the vanilla UI shown when hovering over <see cref="Visible"/>s
        /// </summary>
        /// <param name="tank"></param>
        /// <param name="IO"></param>
        /// <param name="title"></param>
        /// <param name="desc"></param>
        /// <param name="typeDesc"></param>
        /// <param name="icon"></param>
        public static void GUIWarningPopup(Tank tank, ref InfoOverlay IO, string title, string desc, string typeDesc = null, Sprite icon = null)
        {
            if (tank == null)
                return;
            Visible vis = tank?.visible;
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


        /// <summary>
        /// Network message for <see cref="BigF5broningBannerMP(string, bool)"/>
        /// </summary>
        internal class NetBigMessage : MessageBase
        {
            public NetBigMessage() { }
            public NetBigMessage(int team, string desc, bool noise, float duration)
            {
                Noise = noise;
                Team = team;
                Desc = desc;
                Duration = duration;
            }

            public bool Noise;
            public int Team;
            public string Desc;
            public float Duration;
        }
        private static NetworkHook<NetBigMessage> netHook = new NetworkHook<NetBigMessage>(
            "TerraTechETCUtil.NetBigMessage", OnReceiveBigMessage, NetMessageType.FromClientToServerThenClients);

        private static bool OnReceiveBigMessage(NetBigMessage command, bool isServer)
        {
            if (!isServer)
            {
                if (command.Team == 0)
                {
                    DoBigF5broningBanner(command.Desc, command.Noise, command.Duration);
                }
                else
                {
                    if (ManPlayer.inst.PlayerTeam == command.Team)
                        DoBigF5broningBanner(command.Desc, command.Noise, command.Duration);
                }
                return true;
            }
            else
                return true;
            //return false;
        }

        internal static void InsureNetHooks()
        {
            netHook.Enable();
        }


        /// <summary>
        /// The BigF5broningBanner is active and shown on the screen.
        /// </summary>
        public static bool BigF5broningBannerActive => bannerActive;
        private static UIMultiplayerHUD warningBanner;
        private static bool bannerActive = false;
        private static bool subbed = false;
        /// <inheritdoc cref="BigF5broningBannerMP(int, string, bool, float)"/>
        public static void BigF5broningBannerMP(string Text, bool startNoise = true, float duration = 4f) =>
            BigF5broningBannerMP(int.MinValue, Text, startNoise, duration);
        /// <summary>
        /// Make a big, nice obvious warning on the screen that's nearly impossible to miss.
        /// </summary>
        /// <param name="team">The team we should only show the banner to. 
        /// <para>Leave at <see cref="int.MinValue"/> to do all</para></param>
        /// <param name="Text">What text to show on the banner.  Set to nothing to hide immedeately.</param>
        /// <param name="startNoise">Play payload inbound warning SFX for duration of showing.</param>
        /// <param name="duration">How long to show the banner for in seconds</param>
        public static void BigF5broningBannerMP(int team, string Text, bool startNoise = true, float duration = 4f)
        {
            if (netHook.CanBroadcast())
                netHook.TryBroadcast(new NetBigMessage(int.MinValue, Text, startNoise, duration));
            else if (team == int.MinValue || team == ManPlayer.inst.PlayerTeam)
                BigF5broningBannerSP(Text, startNoise);
        }
        /// <summary>
        /// <inheritdoc cref="BigF5broningBannerMP(int, string, bool, float)"/>
        /// <para><b>This is the singleplayer or our client only version</b></para>
        /// </summary>
        /// <inheritdoc cref="BigF5broningBannerMP(int, string, bool, float)"/>
        /// <param name="Text"></param>
        /// <param name="startNoise"></param>
        /// <param name="duration"></param>
        public static void BigF5broningBannerSP(string Text, bool startNoise = true, float duration = 4f) =>
            DoBigF5broningBanner(Text, startNoise, duration);
        private static void DoBigF5broningBanner(string Text, bool startNoise, float duration)
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
            if (warningBanner.Message1.Text.NullOrEmpty())
            {
                if (startNoise)
                    ManSFX.inst.PlayMiscLoopingSFX(ManSFX.MiscSfxType.PayloadIncoming);
            }
            else
                InvokeHelper.CancelInvokeSingle(RemoveWarning);
            if (Text == null)
                Text = string.Empty;
            warningBanner.Message1.SetText(Text);
            InvokeHelper.InvokeSingle(RemoveWarning, duration);
            if (!subbed)
            {
                warningBanner.Message1.SetEvent.Subscribe(RemoveSub);
                subbed = true;
            }
            bannerActive = true;
        }
        private static void RemoveSub(string unused)
        {
            if (subbed)
            {
                InvokeHelper.CancelInvokeSingle(RemoveWarning);
                warningBanner.Message1.SetEvent.Unsubscribe(RemoveSub);
                subbed = false;
            }
        }
        private static void RemoveWarning()
        {
            RemoveSub(null);
            ManSFX.inst.StopMiscLoopingSFX(ManSFX.MiscSfxType.PayloadIncoming);
            warningBanner.Message1.SetText("");
            bannerActive = false;
        }

        /// <summary>
        /// Point at something like the way the vanilla tutorial does it
        /// <para><b>Client Only!</b></para>
        /// </summary>
        /// <param name="trans">Transform to point at and follow</param>
        /// <param name="offset">Offset from the traget transform</param>
        /// <param name="duration">How long to point at it in seconds</param>
        public static void PointAtTransform(Transform trans, Vector3 offset, float duration = 8)
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

        /// <summary>
        /// Stop pointing at the transform
        /// <para><b>Client Only!</b></para>
        /// </summary>
        public static void StopPointing()
        {
            if (Singleton.Manager<ManHUD>.inst.GetHudElement(ManHUD.HUDElementType.BouncingArrow))
            {
                Singleton.Manager<ManHUD>.inst.HideHudElement(ManHUD.HUDElementType.BouncingArrow);
            }
        }


        /// <summary>
        /// Point at this like the way the vanilla tutorial does it
        /// <para><b>Client Only!</b></para>
        /// </summary>
        /// <param name="trans">Transform to point at and follow</param>
        /// <param name="offset">Offset from the traget transform</param>
        /// <param name="duration">How long to point at it in seconds</param>
        public static void PointAtThis(this Transform trans, Vector3 offset, float duration = 8)
        {
            PointAtTransform(trans, offset, duration);
        }
        /// <summary>
        /// Stop pointing at this
        /// <para><b>Client Only!</b></para>
        /// </summary>
        /// <param name="trans">Transform to point at and follow</param>
        public static void StopPointing(this Transform trans)
        {
            StopPointing();
        }

        /// <summary>
        /// Check if the mouse is over the given GUI.Window Rect
        /// <para>Scale is relative to <see cref="ManModGUI.CurrentGUIScale"/></para>
        /// </summary>
        /// <param name="pos">The Window's Rect on the screen</param>
        /// <returns>If the mouse is DIRECTLY within the specified rect on the screen</returns>
        public static bool MouseIsOverGUIMenu(Rect pos)
        {
            Vector3 Mous = Input.mousePosition * ManModGUI.CurrentGUIScale;
            Mous.y = ManModGUI.GameWindowScaledHeight - Mous.y;
            float xMenuMin = pos.x;
            float xMenuMax = pos.x + pos.width * ManModGUI.CurrentGUIWindowScale;
            float yMenuMin = pos.y;
            float yMenuMax = pos.y + pos.height * ManModGUI.CurrentGUIWindowScale;
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
        /// <para>Scale is relative to <see cref="ManModGUI.CurrentGUIScale"/></para>
        /// </summary>
        /// <param name="pos">The Rect to clamp within the screen bounds</param>
        /// <param name="centerOnMouse">If it should re-center on the mouse, then clamp to screen</param>
        /// <param name="centerOnMouseXOffset">The extra offset to apply in range of <b>[0 ~ 1]</b> to offset relative to the mouse.
        /// <para>While values higher and lower than <b>[0 ~ 1]</b> are supported, it is not advised.</para></param>
        /// <param name="centerOnMouseYOffset">The extra offset to apply in range of <b>[0 ~ 1]</b> to offset relative to the mouse.
        /// <para>While values higher and lower than <b>[0 ~ 1]</b> are supported, it is not advised.</para></param>
        /// <param name="extraSpacing">Extra spacing in pixels to add <b>around</b> the given Rect</param>
        public static void ClampGUIToScreen(ref Rect pos, bool centerOnMouse, float extraSpacing = 0f,
            float centerOnMouseXOffset = 0.5f, float centerOnMouseYOffset = 0f)
        {
            float widthAdj = pos.width * ManModGUI.CurrentGUIWindowScale;
            float heightAdj = pos.height * ManModGUI.CurrentGUIWindowScale;
            if (centerOnMouse)
            {
                Vector3 vector = Input.mousePosition * ManModGUI.CurrentGUIScaleInv;
                pos.x = vector.x - widthAdj * centerOnMouseXOffset;
                pos.y = ManModGUI.GameWindowScaledHeight - vector.y - heightAdj * centerOnMouseYOffset;
            }

            pos.x = Mathf.Clamp(pos.x, -extraSpacing, ManModGUI.GameWindowScaledWidth - widthAdj + extraSpacing);
            pos.y = Mathf.Clamp(pos.y, -extraSpacing, ManModGUI.GameWindowScaledHeight - heightAdj + extraSpacing);
        }
        /// <inheritdoc cref="ClampGUIToScreen(ref Rect, bool, float, float, float)"/>
        public static void ClampGUIToScreen(ref Rect pos, bool centerOnMouse)
        {
            float widthAdj = pos.width * ManModGUI.CurrentGUIWindowScale;
            float heightAdj = pos.height * ManModGUI.CurrentGUIWindowScale;
            if (centerOnMouse)
            {
                Vector3 Mous = Input.mousePosition * ManModGUI.CurrentGUIScaleInv;
                pos.x = Mous.x - (widthAdj / 2);
                pos.y = ManModGUI.GameWindowScaledHeight - Mous.y - 90;
            }
            pos.x = Mathf.Clamp(pos.x, 0, ManModGUI.GameWindowScaledWidth - widthAdj);
            pos.y = Mathf.Clamp(pos.y, 0, ManModGUI.GameWindowScaledHeight - heightAdj);
        }
        /// <summary>
        /// Clamps the given Rect to at lesst be partially within bounds of the screen.  
        /// This may not be possible if the screen is too small to fit the Rect entirely.
        /// <para>Scale is relative to <see cref="ManModGUI.CurrentGUIScale"/></para>
        /// </summary>
        /// <param name="pos">The Rect to clamp within the screen bounds</param>
        /// <param name="extraSpacing">Extra spacing where the <b>ui shouldn't be allowed further out from</b></param>
        public static void ClampGUIToScreenNonStrict(ref Rect pos, float extraSpacing = 10)
        {
            float widthAdj = pos.width * ManModGUI.CurrentGUIWindowScale;
            float heightAdj = pos.height * ManModGUI.CurrentGUIWindowScale;
            pos.x = Mathf.Clamp(pos.x, extraSpacing - widthAdj, ManModGUI.GameWindowScaledWidth - extraSpacing);
            pos.y = Mathf.Clamp(pos.y, extraSpacing - heightAdj, ManModGUI.GameWindowScaledHeight - extraSpacing);
        }
        /// <inheritdoc cref="ClampGUIToScreenNonStrict(ref Rect, float)"/>
        public static void ClampGUIToScreenNonStrict(ref Rect pos)
        {
            float widthAdj = pos.width * ManModGUI.CurrentGUIWindowScale;
            float heightAdj = pos.height * ManModGUI.CurrentGUIWindowScale;
            pos.x = Mathf.Clamp(pos.x, 10 - widthAdj, ManModGUI.GameWindowScaledWidth - 10);
            pos.y = Mathf.Clamp(pos.y, 10 - heightAdj, ManModGUI.GameWindowScaledHeight - 10);
        }

        /// <inheritdoc cref="ClampGUIToScreenNonStrict(ref Rect, float)"/>
        public static void ClampGUIToScreenNonStrictHeader(ref Rect pos, float extraSpacing = 10f)
        {
            float widthAdj = pos.width * ManModGUI.CurrentGUIWindowScale;
            float heightAdj = AltUI.HeaderBarHeight * ManModGUI.CurrentGUIWindowScale;
            pos.x = Mathf.Clamp(pos.x, extraSpacing - widthAdj, ManModGUI.GameWindowScaledWidth - extraSpacing);
            pos.y = Mathf.Clamp(pos.y, Mathf.Min(-heightAdj, extraSpacing - heightAdj), ManModGUI.GameWindowScaledHeight - extraSpacing);
        }
    }
}
