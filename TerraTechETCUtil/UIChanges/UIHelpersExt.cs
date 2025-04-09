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
            GUIModModal.InsureRadialMenuPrefabs();
            Debug_TTExt.Log("------ ICONS CACHED ------");
            foreach (var item in CachedUISprites)
            {
                Debug_TTExt.Log(item.Key.NullOrEmpty() ? "NULL" : item.Key);
            }
            Debug_TTExt.Log("------ END CACHED ------");
        }
        public static Sprite GetGUIIcon(string name)
        {
            GUIModModal.InsureRadialMenuPrefabs();
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
            InvokeHelper.InvokeSingle(RemoveWarning, 4f);
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
}
