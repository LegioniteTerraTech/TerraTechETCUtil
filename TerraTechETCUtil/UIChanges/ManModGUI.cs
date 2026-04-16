using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Text;
using FMOD;
using UnityEngine;

namespace TerraTechETCUtil
{
    /// <summary>
    /// The global mod popup manager
    /// <para>See <seealso cref="UIHelpersExt"/> and <seealso cref="AltUI"/> for more UI helpers</para>
    /// </summary>
    public class ManModGUI : MonoBehaviour
    {   // Global popup manager
        /// <summary>
        /// The main manager instance
        /// </summary>
        public static ManModGUI inst;

        /// <summary>
        /// <see cref="ManModGUI"/> starts windows at this ID and increments upwards with each new one
        /// </summary>
        public const int IDOffset = 136000;
        private const int MaxPopups = 48;
        private const int MaxPopupsActive = 32;

        /// <summary>
        /// Window scale preset width
        /// </summary>
        public const int LargeWindowWidth = 1000;
        /// <summary> Window dimension preset </summary>
        public static Rect DefaultWindow = new Rect(0, 0, 300, 300);   // the "window"
        /// <summary> Window dimension preset </summary>
        public static Rect LargeWindow = new Rect(0, 0, 1000, 600);
        /// <summary> Window dimension preset </summary>
        public static Rect WideWindow = new Rect(0, 0, 700, 180);
        /// <summary> Window dimension preset </summary>
        public static Rect SmallWideWindow = new Rect(0, 0, 600, 140);
        /// <summary> Window dimension preset </summary>
        public static Rect SmallHalfWideWindow = new Rect(0, 0, 350, 140);
        /// <summary> Window dimension preset </summary>
        public static Rect SideWindow = new Rect(0, 0, 200, 125);
        /// <summary> Window dimension preset </summary>
        public static Rect TinyWindow = new Rect(0, 0, 160, 75);
        /// <summary> Window dimension preset </summary>
        public static Rect MicroWindow = new Rect(0, 0, 110, 40);
        /// <summary> Window dimension preset </summary>
        public static Rect TinyWideWindow = new Rect(0, 0, 260, 100);
        /// <summary> Window dimension preset </summary>
        public static Rect SmallWindow = new Rect(0, 0, 160, 120);

        //public static GUIStyle styleSmallFont;
        /// <summary> Special font used for window </summary>
        public static GUIStyle styleDescFont;
        /// <summary> Special font used for window </summary>
        public static GUIStyle styleDescLargeFont;
        /// <summary> Special font used for window </summary>
        public static GUIStyle styleDescLargeFontScroll;
        /// <summary> Special font used for window </summary>
        public static GUIStyle styleLargeFont;
        /// <summary> Special font used for window </summary>
        public static GUIStyle styleHugeFont;
        /// <summary> Special font used for window </summary>
        public static GUIStyle styleGinormusFont;
        /// <summary> Special font used for window </summary>
        public static GUIStyle styleBlueFont;
        /// <summary> Special font used for window </summary>
        public static GUIStyle styleBorderedFont;
        /// <summary> Special font used for window </summary>
        public static GUIStyle styledFont;
        /// <summary> Special font used for window </summary>
        public static GUIStyle styleBlackLargeFont;
        /// <summary> Special font used for window </summary>
        public static GUIStyle styleScrollFont;
        /// <summary> Special font used for window </summary>
        public static GUIStyle styleLabelLargerFont;
        /// <summary> Special font used for window </summary>
        public static GUIStyle styleButtonHugeFont;
        /// <summary> Special font used for window </summary>
        public static GUIStyle styleButtonGinormusFont;

        /// <summary>
        /// All popups managed by <see cref="ManModGUI"/>
        /// </summary>
        public static List<GUIPopupDisplay> AllPopups = new List<GUIPopupDisplay>();
        /// <summary>
        /// Hide all windows managed by <see cref="ManModGUI"/> when the player is dragging a block or the camera
        /// </summary>
        public static bool HideGUICompletelyWhenDragging = true;

        /// <summary>
        /// Number of active popups managed
        /// </summary>
        public static int numActivePopups;
        /// <summary>
        /// Bool to tell if the custom GUI is ready to go
        /// </summary>
        public static bool SetupAltWins = false;


        private static int IDCur = IDOffset;
        private static GUIPopupDisplay currentGUIWindow;
        private static int updateClock = 0;
        private static int updateClockDelay = 50;

        private static HashSet<ModBase> registered = new HashSet<ModBase>();
        private static List<Action> EscHoldup = new List<Action>();
        private static List<Action> EscHoldupPre = new List<Action>();


        /// <summary>
        /// Mod request to use <see cref="ManModGUI"/>
        /// </summary>
        /// <param name="modRequestor"></param>
        public static void RequestInit(ModBase modRequestor)
        {
            if (!inst)
            {
                inst = new GameObject("ManModGUI").AddComponent<ManModGUI>();
                Debug_TTExt.Log("TerraTechETCUtil: ManModGUI initated");
                InvokeHelper.InvokeSingleRepeat(inst.LateInitiate, 0.5f);
            }
            if (!registered.Contains(modRequestor))
                registered.Add(modRequestor);
        }
        private void LateInitiate()
        {
            try
            {
                ManPauseGame.inst.PauseEvent.Subscribe(SetVisibilityOfAllPopups);
                InvokeHelper.CancelInvokeSingleRepeat(inst.LateInitiate);
            }
            catch { }
        }
        /// <summary>
        /// Mod request to stop using <see cref="ManModGUI"/>
        /// </summary>
        /// <param name="modRequestor"></param>
        public static void DeInit(ModBase modRequestor)
        {
            if (registered.Contains(modRequestor))
            {
                registered.Remove(modRequestor);
                if (registered.Count > 0)
                    return;
                if (!inst)
                    return;
                RemoveALLPopups();
                inst.CancelInvoke("LateInitiate");
                ManPointer.inst.PreventInteraction((ManPointer.PreventChannel)16, false);
                ManPauseGame.inst.PauseEvent.Unsubscribe(SetVisibilityOfAllPopups);
                Debug_TTExt.Log("TerraTechETCUtil: ManModGUI De-Init");
            }
        }

        /// <summary>
        /// Inserts an event QUEUE-wise that will trigger and then disconnect after an ESC keypress.
        /// <b>MAKE SURE TO CATCH THE ACTION!</b>
        /// </summary>
        /// <param name="action">The event to trigger once on ESC keypess</param>
        /// <param name="beforeAllGUI">True to trigger BEFORE closing vanilla UI</param>
        public static void AddEscapeableCallback(Action action, bool beforeAllGUI)
        {
            if (beforeAllGUI)
                EscHoldupPre.Add(action);
            else
                EscHoldup.Add(action);
        }
        /// <summary>
        /// Inserts an event STACK-wise an event that will trigger and then disconnect after an ESC keypress
        /// </summary>
        /// <param name="action">The event to trigger once on ESC keypess</param>
        /// <param name="beforeAllGUI">True to trigger BEFORE closing vanilla UI</param>
        public static void AddEscapeableCallbackStack(Action action, bool beforeAllGUI)
        {
            if (beforeAllGUI)
                EscHoldupPre.Insert(0, action);
            else
                EscHoldup.Insert(0, action);
        }
        /// <summary>
        /// Removes an ESC keypress event from ANYWHERE in the event list
        /// </summary>
        /// <param name="action">The event that is currently triggering once on ESC keypess</param>
        /// <param name="beforeAllGUI">True if the trigger is BEFORE closing vanilla UI</param>
        public static void RemoveEscapeableCallback(Action action, bool beforeAllGUI)
        {
            if (beforeAllGUI)
                EscHoldupPre.Remove(action);
            else
                EscHoldup.Remove(action);
        }
        internal static bool CallEscapeCallbackPre()
        {
            if (EscHoldupPre.Any())
            {
                while (EscHoldupPre.Any())
                {
                    Action act = EscHoldupPre[0];
                    EscHoldupPre.RemoveAt(0);
                    if (act != null)
                    {
                        try
                        {
                            act();
                        }
                        catch (Exception e)
                        {
                            ShowErrorPopup("Unhandled error whilist processing EscapeableCallbacks(Pre) in ManModGUI:\n" + e);
                        }
                        return true;
                    }
                }
            }
            return false;
        }
        internal static bool CallEscapeCallbackPost()
        {
            if (EscHoldup.Any())
            {
                while (EscHoldup.Any())
                {
                    Action act = EscHoldup[0];
                    EscHoldup.RemoveAt(0);
                    if (act != null)
                    {
                        try
                        {
                            act();
                        }
                        catch (Exception e)
                        {
                            ShowErrorPopup("Unhandled error whilist processing EscapeableCallbacks(Post) in ManModGUI:\n" + e);
                        }
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Set the current focussed popup
        /// </summary>
        /// <param name="disp"></param>
        public static void SetCurrentPopup(GUIPopupDisplay disp)
        {
            if (AllPopups.Contains(disp))
            {
                currentGUIWindow = disp;
                GUI.FocusWindow(disp.ID);
            }
            else
            {
                ShowErrorPopup("TerraTechETCUtil: GUIPopupDisplay \"" + disp.Header + "\" is not in the AllPopups list!");
            }
        }
        /// <summary>
        /// Set the current focussed popup
        /// </summary>
        /// <param name="menu"></param>
        public static void SetCurrentPopup(GUIMiniMenu menu)
        {
            var disp = menu.Display;
            if (disp == null)
                throw new NullReferenceException("GUIMiniMenu is not assigned to a GUIPopupDisplay. " +
                    "Register it first in ManModGUI.RegisterPopupSingle() or create it using " +
                    "ManModGUI.AddPopupSingle() or ManModGUI.AddPopupStackable()");
            SetCurrentPopup(disp);
        }
        /// <summary>
        /// Get the current focussed popup
        /// </summary>
        public static GUIPopupDisplay GetCurrentPopup()
        {
            return currentGUIWindow;
        }
        /// <summary>
        /// Keep the popup <b>ENTIRELY</b> within screen bounds
        /// </summary>
        /// <param name="disp"></param>
        public static void KeepWithinScreenBounds(GUIPopupDisplay disp)
        {
            disp.Window.x = Mathf.Clamp(disp.Window.x, 0, Display.main.renderingWidth - disp.Window.width);
            disp.Window.y = Mathf.Clamp(disp.Window.y, 0, Display.main.renderingHeight - disp.Window.height);
        }
        /// <summary>
        /// Keep the popup at least partially within screen bounds
        /// </summary>
        /// <param name="disp"></param>
        public static void KeepWithinScreenBoundsNonStrict(GUIPopupDisplay disp)
        {
            disp.Window.x = Mathf.Clamp(disp.Window.x, 10 - disp.Window.width, Display.main.renderingWidth - 10);
            disp.Window.y = Mathf.Clamp(disp.Window.y, 10 - disp.Window.height, Display.main.renderingHeight - 10);
        }
        /// <summary>
        /// screenPos x and y must be within 0-1 float range!
        /// </summary>
        /// <param name="screenPosPercent"></param>
        /// <param name="disp"></param>
        public static void ChangePopupPositioning(Vector2 screenPosPercent, GUIPopupDisplay disp)
        {
            disp.Window.x = (Display.main.renderingWidth - disp.Window.width) * screenPosPercent.x;
            disp.Window.y = (Display.main.renderingHeight - disp.Window.height) * screenPosPercent.y;
        }
        /// <summary>
        /// screenPos x and y must be within 0-1 float range!
        /// </summary>
        /// <param name="disp"></param>
        /// <param name="squareDelta"></param>
        public static bool PopupPositioningApprox(GUIPopupDisplay disp, float squareDelta = 0.1f)
        {
            float valX = disp.Window.x / (Display.main.renderingWidth - disp.Window.width);
            float valY = disp.Window.y / (Display.main.renderingHeight - disp.Window.height);
            return valX > -squareDelta && valX < squareDelta && valY > -squareDelta && valY < squareDelta;
        }
        /// <summary>
        /// Check to see if the popup is still registered with <see cref="ManModGUI"/>
        /// </summary>
        /// <param name="title"></param>
        /// <param name="typeHash"></param>
        /// <param name="exists"></param>
        /// <returns>True if it exists and is registered</returns>
        public static bool DoesPopupExist(string title, int typeHash, out GUIPopupDisplay exists)
        {
            exists = AllPopups.Find(delegate (GUIPopupDisplay cand)
            {
                return cand.typeHash == typeHash && cand.Header.CompareTo(title) == 0;
            }
            );
            return exists;
        }
        /// <summary>
        /// Get a popup based on title and type hash
        /// </summary>
        /// <param name="title"></param>
        /// <param name="typeHash"></param>
        /// <returns>The popup if found</returns>
        public static GUIPopupDisplay GetPopup(string title, int typeHash)
        {
            return AllPopups.Find(delegate (GUIPopupDisplay cand)
            {
                return cand.typeHash == typeHash && cand.Header.CompareTo(title) == 0;
            }
            );
        }
        /// <summary>
        /// Get all popups based on a type hash
        /// </summary>
        /// <param name="typeHash"></param>
        /// <returns>The popup if found</returns>
        public static List<GUIPopupDisplay> GetAllPopups(int typeHash)
        {
            return AllPopups.FindAll(delegate (GUIPopupDisplay cand)
            {
                return cand.typeHash == typeHash;
            }
            );
        }
        /// <summary>
        /// Get all <b>active</b> popups based on a type hash
        /// </summary>
        /// <param name="typeHash"></param>
        /// <param name="open">If this is true, get all open popups, otherwise all closed</param>
        /// <returns>The popup if found</returns>
        public static List<GUIPopupDisplay> GetAllActivePopups(int typeHash, bool open = true)
        {
            return AllPopups.FindAll(delegate (GUIPopupDisplay cand)
            {
                return cand.isOpen == open && cand.typeHash == typeHash;
            }
            );
        }

        /// <summary>
        /// True if the local player's mouse is hovering over any IMGUI managed by <see cref="ManModGUI"/>
        /// </summary>
        public static bool IsMouseOverModGUI { get; private set; } = false;
        /// <summary>
        /// True if the <see cref="ManModGUI"/> is reserving the mouse and keyboard controls for use on the UI elements
        /// </summary>
        public static bool UIKickoffState { get; private set; } = false;
        /// <summary>
        /// True if the <see cref="ManModGUI"/> managed UI is not being hovered over and thus should be faded out to reduce obscurity
        /// </summary>
        public static bool UIFadeState { get; private set; } = false;
        /// <summary>
        /// This value will increase based on the number of GUIs the mouse is hovering over
        /// </summary>
        public static int IsMouseOverAnyModGUI = 0;
        /// <summary>
        /// Is the interaction with the world blocked?
        /// </summary>
        public static bool IsWorldInteractionBlocked => ManPointer.inst.IsInteractionBlocked || IsMouseOverModGUI;
        /// <summary>
        /// Is the UI interaction blocked? (EG. Dragging block)
        /// </summary>
        public static bool IsUIInteractionBlocked => ManPointer.inst.DraggingItem;
        private static Dictionary<ManHUD.HUDElementType, object> TempHidden = 
            new Dictionary<ManHUD.HUDElementType, object>();
        /// <summary>
        /// Reset the UI so it doesn't try to reopen menus
        /// </summary>
        public static void ClearTempHidden()
        {
            TempHidden.Clear();
        }
        /// <summary>
        /// Hide the target Vanilla <see cref="ManHUD.HUDElementType"/>
        /// </summary>
        /// <param name="type"></param>
        public static void HideGUI(ManHUD.HUDElementType type)
        {
            object context = null;
            bool wasVisible = false;
            switch (type)
            {
                //case ManHUD.HUDElementType.BlockMenuSelection:
                case ManHUD.HUDElementType.BlockPalette:
                //case ManHUD.HUDElementType.BlockShop:
                //case ManHUD.HUDElementType.TechShop:
                    wasVisible = ManHUD.inst.IsHudElementExpanded(type);
                    ManPurchases.inst.ExpandPalette(false, UIShopBlockSelect.ExpandReason.Beam, true);
                    break;
                case ManHUD.HUDElementType.SkinsPalette:
                case ManHUD.HUDElementType.TechLoader:
                case ManHUD.HUDElementType.TechManager:
                case ManHUD.HUDElementType.WorldMap:
                case ManHUD.HUDElementType.RaDTestChamber:
                case ManHUD.HUDElementType.TeleportMenu:
                case ManHUD.HUDElementType.MissionBoard:
                    wasVisible = ManHUD.inst.IsHudElementVisible(type);
                    ManHUD.inst.HideHudElement(type);
                    break;
                default:
                    // Unsupported!!!
                    //ShowErrorPopup("ManModGUI.HideGUI() does not support type \"" + type.ToString() + "\"");
                    break;
            }
            if (wasVisible && !TempHidden.ContainsKey(type))
                TempHidden.Add(type, context);
        }
        /// <summary>
        /// Show the target Vanilla <see cref="ManHUD.HUDElementType"/>
        /// <para><b>DO NOT ITERATE TempHidden WHEN CALLING THIS</b></para>
        /// </summary>
        /// <param name="type"></param>
        public static void ShowGUI(ManHUD.HUDElementType type)
        {
            ShowGUI_Internal(type);
            TempHidden.Remove(type);
        }
        private static void ShowGUI_Internal(ManHUD.HUDElementType type)
        {
            TempHidden.TryGetValue(type, out object context);
            switch (type)
            {
                case ManHUD.HUDElementType.BlockPalette:
                //case ManHUD.HUDElementType.BlockShop:
                //case ManHUD.HUDElementType.TechShop:
                    ManPurchases.inst.ExpandPalette(true, UIShopBlockSelect.ExpandReason.Beam);
                    //case ManHUD.HUDElementType.BlockMenuSelection:
                    //ManPurchases.inst.ShowPalette(true);
                    break;
                case ManHUD.HUDElementType.SkinsPalette:
                case ManHUD.HUDElementType.TechLoader:
                case ManHUD.HUDElementType.TechManager:
                case ManHUD.HUDElementType.WorldMap:
                case ManHUD.HUDElementType.RaDTestChamber:
                case ManHUD.HUDElementType.TeleportMenu:
                case ManHUD.HUDElementType.MissionBoard:
                    ManHUD.inst.ShowHudElement(type, context);
                    break;
                default:
                    // Unsupported!!!
                    break;
            }
        }
        /// <summary>
        /// Hide all of the Vanilla <see cref="ManHUD.HUDElementType"/>s which may be displaying 
        /// </summary>
        public static void HideAllObstructingUI()
        {
            HideGUI(ManHUD.HUDElementType.BlockControl);
            HideGUI(ManHUD.HUDElementType.BlockControlOnOff);
            HideGUI(ManHUD.HUDElementType.BlockRecipeSelect);
            HideGUI(ManHUD.HUDElementType.BlockOptionsContextMenu);
            HideGUI(ManHUD.HUDElementType.PowerToggleBlockMenu);

            HideGUI(ManHUD.HUDElementType.RaDTestChamber);
            HideGUI(ManHUD.HUDElementType.TechManager);
            HideGUI(ManHUD.HUDElementType.WorldMap);

            //HideGUI(ManHUD.HUDElementType.BlockMenuSelection);
            HideGUI(ManHUD.HUDElementType.BlockPalette);
            HideGUI(ManHUD.HUDElementType.BlockShop);
            HideGUI(ManHUD.HUDElementType.TechShop);
            HideGUI(ManHUD.HUDElementType.TechLoader);
        }
        /// <summary>Attempts to reopen any windows closed by HideAllObstructingUI() or HideGUI()</summary>
        public static void TryReopenPreviouslyClosedUI()
        {
            try
            {
                foreach (var item in TempHidden)
                {
                    ShowGUI_Internal(item.Key);
                }
            }
            finally
            {
                TempHidden.Clear();
            }
        }
        private static FieldInfo lmb = typeof(ManPointer).GetField("m_LMBDownPos", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo rmb = typeof(ManPointer).GetField("m_RMBDownPos", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo mmb = typeof(ManPointer).GetField("m_MMBDownPos", BindingFlags.NonPublic | BindingFlags.Instance);

        /// <summary>
        /// Right mouse is down
        /// </summary>
        public static bool ManPointerMouseRightDown => ((Vector3)rmb.GetValue(ManPointer.inst)).x != -1;
        /// <summary>
        /// Middle mouse is down
        /// </summary>
        public static bool ManPointerMouseMiddleDown => ((Vector3)mmb.GetValue(ManPointer.inst)).x != -1;
        /// <summary>
        /// Left mouse is down
        /// </summary>
        public static bool ManPointerMouseLeftDown => ((Vector3)lmb.GetValue(ManPointer.inst)).x != -1;

        /// <summary>
        ///   Makes the mouse release hold of whatever it was holding previously. 
        ///    USE SPARINGLY AS THIS IS ANNOYING TO DEAL WITH!!!
        /// </summary>
        public static void ForceReleaseRightMouse()
        {
            Vector3 RMB = (Vector3)rmb.GetValue(ManPointer.inst);
            if (RMB.x != -1)
            {
                ManPointer.inst.MouseEvent.Send(ManPointer.Event.RMB, false, Input.mousePosition == RMB);
                rmb.SetValue(ManPointer.inst, RMB.SetX(-1f));
            }
        }
        /// <summary>
        ///   Makes the mouse release hold of whatever it was holding previously. 
        ///    USE SPARINGLY AS THIS IS ANNOYING TO DEAL WITH!!!
        /// </summary>
        public static void ForceReleaseMiddleMouse()
        {
            Vector3 MMB = (Vector3)mmb.GetValue(ManPointer.inst);
            if (MMB.x != -1)
            {
                ManPointer.inst.MouseEvent.Send(ManPointer.Event.MMB, false, Input.mousePosition == MMB);
                mmb.SetValue(ManPointer.inst, MMB.SetX(-1f));
            }
        }
        /// <summary>
        ///   Makes the mouse release hold of whatever it was holding previously. 
        ///    USE SPARINGLY AS THIS IS ANNOYING TO DEAL WITH!!!
        /// </summary>
        public static void ForceReleaseLeftMouse()
        {
            Vector3 LMB = (Vector3)lmb.GetValue(ManPointer.inst);
            if (LMB.x != -1)
            {
                ManPointer.inst.MouseEvent.Send(ManPointer.Event.LMB, false, Input.mousePosition == LMB);
                lmb.SetValue(ManPointer.inst, LMB.SetX(-1f));
            }
        }
        internal static void MainUpdateMouseOverAnyWindow()
        {
            bool mouseOverMenu = false;
            if (IsMouseOverAnyModGUI > 0)
                mouseOverMenu = true;
            if (mouseOverMenu != IsMouseOverModGUI)
            {
                //Debug_TTExt.Log("ManModGUI.released - " + mouseOverMenu);
                IsMouseOverModGUI = mouseOverMenu;
            }
            else if (IsMouseOverAnyModGUI > 0)
                IsMouseOverAnyModGUI--;
            var buildMode = ManPointer.inst.BuildMode;
            bool IsInteracting = Input.GetMouseButton(1) ||Input.GetMouseButton(2) ||
                (buildMode == ManPointer.BuildingMode.Grab && ManPointer.inst.DraggingItem != null) ||
                buildMode == ManPointer.BuildingMode.PaintBlock ||
                buildMode == ManPointer.BuildingMode.PaintSkin ||
                buildMode == ManPointer.BuildingMode.PaintSkinTech;
            if (UIFadeState && !IsInteracting)
            {
                AltUI.UIAlphaAuto = 1f;
                UIFadeState = false;
            }
            if (UIKickoffState != IsMouseOverModGUI)
            {
                if (IsMouseOverModGUI && IsInteracting)
                {   // Hold off on our UI and fade it for now!
                    if (!UIFadeState)
                    {
                        AltUI.UIAlphaAuto = 0.2f;
                        UIFadeState = true;
                    }
                }
                else
                {
                    UIKickoffState = IsMouseOverModGUI;
                    //Debug_TTExt.Log("ManModGUI.UI_released - " + mouseOverMenu);
                    // The below does not work properly.  AT ALL
                    //ManPointer.inst.PreventInteraction(ManPointer.PreventChannel.HUD, IsMouseOverModGUI);
                    if (IsMouseOverModGUI)
                    {
                        HideAllObstructingUI();
                        //ForceReleaseRightMouse();
                        //ForceReleaseMiddleMouse();
                        //TankCamera.inst.SetMouseControlEnabled(false); // Already patched in AllUIPatches
                    }
                    else
                    {
                        //TankCamera.inst.SetMouseControlEnabled(true); // Already patched in AllUIPatches
                        UIHelpersExt.ReleaseControl();
                        TryReopenPreviouslyClosedUI();
                    }
                }
            }
        }
        internal static void UpdateMouseOverAnyWindow()
        {
            foreach (var item in AllPopups)
            {
                if (item.isOpen && UIHelpersExt.MouseIsOverSubMenu(item.Window))
                {
                    IsMouseOverAnyModGUI = 2;
                    break;
                }
            }
        }

        /// <summary>
        /// Add a popup of type <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Header"></param>
        /// <param name="windowOverride"></param>
        /// <returns>True if it was added</returns>
        public static bool AddPopupStackable<T>(string Header, GUIDisplayStats windowOverride = null)
            where T : GUIMiniMenu<T>, new()
        {
            if (MaxPopups <= AllPopups.Count)
            {
                Debug_TTExt.Log("TerraTechETCUtil: Too many popups!!!  Aborting AddPopup!");
                return false;
            }
            currentGUIWindow = CreateNewPopupSingle(Header);
            currentGUIWindow.SetupGUI<T>(windowOverride, IDCur);
            IDCur++;

            AllPopups.Add(currentGUIWindow);
            return true;
        }

        /// <summary>
        /// Add a popup of type <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Header"></param>
        /// <param name="windowOverride"></param>
        /// <returns>True if it was added</returns>
        public static bool AddPopupSingle<T>(string Header, GUIDisplayStats windowOverride = null)
            where T : GUIMiniMenu<T>, new()
        {
            if (DoesPopupExist(Header, typeof(T).GetHashCode(), out GUIPopupDisplay exists))
            {
                SetCurrentPopup(exists);
                RefreshPopup<T>(exists, windowOverride);
                return true;
            }
            if (MaxPopups <= AllPopups.Count)
            {
                Debug_TTExt.Log("TerraTechETCUtil: Too many popups!!!  Aborting AddPopup!");
                return false;
            }
            currentGUIWindow = CreateNewPopupSingle(Header);
            currentGUIWindow.SetupGUI<T>(windowOverride, IDCur);
            IDCur++;

            AllPopups.Add(currentGUIWindow);
            return true;
        }
        /// <summary>
        /// Add a popup of type <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="instance"></param>
        /// <param name="Header"></param>
        /// <param name="windowOverride"></param>
        /// <returns>True if it was added</returns>
        public static bool RegisterPopupSingle<T>(T instance, string Header, GUIDisplayStats windowOverride = null)
            where T : GUIMiniMenu<T>, new()
        {
            if (instance == null)
                throw new ArgumentNullException("instance");
            if (DoesPopupExist(Header, typeof(T).GetHashCode(), out GUIPopupDisplay exists))
            {
                SetCurrentPopup(exists);
                exists.SetupExistingGUI(instance, windowOverride, exists.ID);
                return true;
            }
            if (MaxPopups <= AllPopups.Count)
            {
                Debug_TTExt.Log("TerraTechETCUtil: Too many popups!!!  Aborting AddPopup!");
                return false;
            }
            currentGUIWindow = CreateNewPopupSingle(Header);
            currentGUIWindow.SetupExistingGUI(instance, windowOverride, IDCur);
            IDCur++;

            AllPopups.Add(currentGUIWindow);
            return true;
        }
        /// <summary>
        /// Creates a new hidden GUIPopupDisplay. 
        /// DOES NOT NULL CHECK!!!  DOES NOT ADD TO MANAGED POPUP LIST
        /// </summary>
        private static GUIPopupDisplay CreateNewPopupSingle(string Header)
        {
            var gameObj = new GameObject(Header);
            GUIPopupDisplay newDisp = gameObj.AddComponent<GUIPopupDisplay>();
            newDisp.obj = gameObj;
            newDisp.Header = Header;
            newDisp.Window = new Rect(DefaultWindow);
            gameObj.SetActive(false);
            newDisp.isOpen = false;
            return newDisp;
        }
        private static void RefreshPopup<T>(GUIPopupDisplay disp, GUIDisplayStats windowOverride = null)
            where T : GUIMiniMenu<T>, new()
        {
            disp.SetupGUI<T>(windowOverride, disp.ID);
        }
        /// <summary>
        /// Removes and deletes the popup
        /// </summary>
        /// <param name="disp"></param>
        /// <returns>True if it was deleted</returns>
        public static bool RemovePopup(GUIPopupDisplay disp)
        {
            try
            {
                if (disp == null)
                {
                    Debug_TTExt.Log("TerraTechETCUtil: RemoveCurrentPopup - POPUP IS NULL!!");
                    return false;
                }
                disp.GUIFormat.OnRemoval();
                Destroy(disp.gameObject);
                AllPopups.Remove(disp);
                return true;
            }
            catch (Exception e)
            {
                Debug_TTExt.Log("TerraTechETCUtil: RemoveCurrentPopup - Exception: " + e);
                return false;
            }
        }/// <summary>
         /// Removes and deletes the popup
         /// </summary>
         /// <param name="menu"></param>
         /// <returns>True if it was deleted</returns>
        public static bool RemovePopup(GUIMiniMenu menu)
        {
            var disp = menu.Display;
            if (disp == null)
                throw new NullReferenceException("GUIMiniMenu is not assigned to a GUIPopupDisplay. " +
                    "Register it first in ManModGUI.RegisterPopupSingle() or create it using " +
                    "ManModGUI.AddPopupSingle() or ManModGUI.AddPopupStackable()");
            return RemovePopup(disp);
        }
        /// <summary>
        /// Removes and deletes ALL popups.
        /// <para><b>Do not use unless absolutely nesseary!</b></para>
        /// </summary>
        /// <returns>True if any were deleted</returns>
        public static bool RemoveALLPopups()
        {
            int FireTimes = AllPopups.Count;
            bool worked = true;
            for (int step = 0; step < FireTimes; step++)
            {
                try
                {
                    if (!RemovePopup(AllPopups.FirstOrDefault()))
                        worked = false;
                }
                catch { }
            }
            return worked;
        }

        /// <summary>
        /// Show an error popup screen. Has the same modified effect as <see cref="ManUI.ShowErrorPopup(string)"/>
        /// </summary>
        /// <param name="Warning"></param>
        /// <param name="IsSeriousError">The error is in a loop and will spam.  Set this to true to prevent spamming</param>
        /// <param name="OnFixRequested">Show a "Fix" button that the user can press to fix then issue.
        /// <para>Use this for issues that might affect things negatively like crashes</para></param>
        public static void ShowErrorPopup(string Warning, bool IsSeriousError = false, Action OnFixRequested = null) =>
            InvokeHelper.ShowErrorPopup(Warning, IsSeriousError, OnFixRequested);


        /// <summary>
        /// Show a specific popup
        /// </summary>
        /// <param name="disp">Specific window instance target</param>
        /// <param name="focusImmediately">Rise this window to the top of all menus</param>
        /// <returns>True if successful</returns>
        public static bool ShowPopup(GUIPopupDisplay disp, bool focusImmediately = false)
        {
            if (disp == null)
                throw new ArgumentNullException(nameof(disp));
            if (disp.isOpen)
            {
                //Debug_TTExt.Log("TerraTechETCUtil: ShowPopup - Window is already open");
                if (focusImmediately)
                    GUI.FocusWindow(disp.ID);
                return false;
            }
            if (MaxPopupsActive <= numActivePopups)
            {
                Debug_TTExt.Assert(false, "Too many popups active!!!  Limit is " + MaxPopupsActive + ".  Aborting ShowPopup!");
                return false;
            }
            Debug_TTExt.Info("TerraTechETCUtil: Popup " + disp.Header + " active");
            disp.GUIFormat.OnOpen();
            disp.obj.SetActive(true);
            disp.isOpen = true;
            Singleton.Manager<ManSFX>.inst.PlayUISFX(ManSFX.UISfxType.InfoOpen);
            numActivePopups++;
            return true;
        }
        /// <summary>
        /// Show a specific popup
        /// </summary>
        /// <param name="disp">Specific window instance target</param>
        /// <param name="screenPosPercent">Position relative to the entire main screen [0 ~ 1]</param>
        /// <param name="focusImmediately">Rise this window to the top of all menus</param>
        /// <returns>True if successful</returns>
        public static bool ShowPopup(GUIPopupDisplay disp, Vector2 screenPosPercent, bool focusImmediately = false)
        {
            bool shown = ShowPopup(disp, focusImmediately);
            if (screenPosPercent.x > 1)
                screenPosPercent.x = 1;
            if (screenPosPercent.y > 1)
                screenPosPercent.y = 1;
            disp.Window.x = (Display.main.renderingWidth - disp.Window.width) * screenPosPercent.x;
            disp.Window.y = (Display.main.renderingHeight - disp.Window.height) * screenPosPercent.y;

            return shown;
        }
        /// <summary>
        /// Show a specific popup
        /// </summary>
        /// <param name="menu">Specific window menu target</param>
        /// <param name="focusImmediately">Rise this window to the top of all menus</param>
        /// <returns>True if successful</returns>
        public static bool ShowPopup(GUIMiniMenu menu, bool focusImmediately = false)
        {
            var disp = menu.Display;
            if (disp == null)
                throw new NullReferenceException("GUIMiniMenu is not assigned to a GUIPopupDisplay. " +
                    "Register it first in ManModGUI.RegisterPopupSingle() or create it using " +
                    "ManModGUI.AddPopupSingle() or ManModGUI.AddPopupStackable()");
            return ShowPopup(disp, focusImmediately);
        }
        /// <summary>
        /// Show a specific popup
        /// </summary>
        /// <param name="menu">Specific window menu target</param>
        /// <param name="screenPosPercent">Position relative to the entire main screen [0 ~ 1]</param>
        /// <param name="focusImmediately">Rise this window to the top of all menus</param>
        /// <returns>True if successful</returns>
        public static bool ShowPopup(GUIMiniMenu menu, Vector2 screenPosPercent, bool focusImmediately = false)
        {
            var disp = menu.Display;
            if (disp == null)
                throw new NullReferenceException("GUIMiniMenu is not assigned to a GUIPopupDisplay. " +
                    "Register it first in ManModGUI.RegisterPopupSingle() or create it using " +
                    "ManModGUI.AddPopupSingle() or ManModGUI.AddPopupStackable()");
            return ShowPopup(disp, screenPosPercent, focusImmediately);
        }


        /// <summary>
        /// Show the currently focussed popup
        /// </summary>
        /// <param name="focusImmediately">Rise this window to the top of all menus</param>
        public static bool ShowPopup(bool focusImmediately = false)
        {
            if (currentGUIWindow == null)
                throw new NullReferenceException("currentGUIWindow is null.  There is no displayued window!");
            return ShowPopup(currentGUIWindow, focusImmediately);
        }
        /// <summary> 
        /// Show the currently focussed popup
        /// </summary>
        /// <param name="screenPosPercent">Position relative to the entire main screen [0 ~ 1]</param>
        /// <param name="focusImmediately">Rise this window to the top of all menus</param>
        public static bool ShowPopup(Vector2 screenPosPercent, bool focusImmediately = false)
        {
            bool shown = ShowPopup(focusImmediately);
            if (screenPosPercent.x > 1)
                screenPosPercent.x = 1;
            if (screenPosPercent.y > 1)
                screenPosPercent.y = 1;
            currentGUIWindow.Window.x = (Display.main.renderingWidth - currentGUIWindow.Window.width) * screenPosPercent.x;
            currentGUIWindow.Window.y = (Display.main.renderingHeight - currentGUIWindow.Window.height) * screenPosPercent.y;

            return shown;
        }

        /// <summary>
        /// Hide a specific popup
        /// </summary>
        /// <param name="disp">Specific window instance target</param>
        /// <returns>True if successful</returns>
        public static bool HidePopup(GUIPopupDisplay disp)
        {
            if (disp == null)
            {
                Debug_TTExt.Log("TerraTechETCUtil: HidePopup - THE WINDOW IS NULL!!");
                return false;
            }
            if (!disp.isOpen)
            {
                //Debug_TTExt.Log("TerraTechETCUtil: HidePopup - Window is already closed");
                return false;
            }
            numActivePopups--;
            disp.obj.SetActive(false);
            disp.isOpen = false;
            Singleton.Manager<ManSFX>.inst.PlayUISFX(ManSFX.UISfxType.InfoClose);
            return true;
        }
        /// <summary>
        /// Hide a specific popup
        /// </summary>
        /// <param name="menu">Specific window menu target</param>
        /// <returns>True if successful</returns>
        public static bool HidePopup(GUIMiniMenu menu)
        {
            var disp = menu.Display;
            if (disp == null)
                throw new NullReferenceException("GUIMiniMenu is not assigned to a GUIPopupDisplay. " +
                    "Register it first in ManModGUI.RegisterPopupSingle() or create it using " +
                    "ManModGUI.AddPopupSingle() or ManModGUI.AddPopupStackable()");
            return HidePopup(disp);
        }

        private static bool allPopupsOpen = true;
        private static List<GUIPopupDisplay> PopupsClosed = new List<GUIPopupDisplay>();
        /// <summary>
        /// Set the visiblity of all popups managed by <see cref="ManModGUI"/>
        /// </summary>
        /// <param name="Closed"></param>
        public static void SetVisibilityOfAllPopups(bool Closed)
        {
            bool isOpen = !Closed;
            if (allPopupsOpen != isOpen)
            {
                allPopupsOpen = isOpen;
                if (isOpen)
                {
                    foreach (GUIPopupDisplay disp in PopupsClosed)
                    {
                        try
                        {
                            disp.obj.SetActive(true);
                            disp.isOpen = true;
                        }
                        catch { }
                    }
                    PopupsClosed.Clear();
                }
                else
                {
                    foreach (GUIPopupDisplay disp in AllPopups)
                    {
                        try
                        {
                            if (disp.obj.activeSelf)
                            {
                                disp.obj.SetActive(false);
                                disp.isOpen = false;
                                PopupsClosed.Add(disp);
                            }
                        }
                        catch { }
                    }
                }
            }
        }


        private void Update()
        {
            UpdateAllFast();
            if (updateClock > updateClockDelay)
            {
                UpdateAllPopups();
                updateClock = 0;
            }
            updateClock++;
        }

        /// <summary>
        /// Once every 50 visual frames
        /// </summary>
        private static void UpdateAllPopups()
        {
            int FireTimes = AllPopups.Count;
            for (int step = 0; step < FireTimes; step++)
            {
                try
                {
                    GUIPopupDisplay disp = AllPopups.ElementAt(step);
                    disp.GUIFormat.DelayedUpdate();
                }
                catch { }
            }
        }
        private void UpdateAllFast()
        {
            int FireTimes = AllPopups.Count;
            for (int step = 0; step < FireTimes; step++)
            {
                try
                {
                    GUIPopupDisplay disp = AllPopups.ElementAt(step);
                    disp.GUIFormat.FastUpdate();
                }
                catch { }
            }
        }


    }

    /// <summary>
    /// This manages the window that HOLDS the GUIMiniMenu.
    /// <b>Use GUIMiniMenu for the window contents!</b>
    /// </summary>
    public class GUIPopupDisplay : MonoBehaviour
    {
        internal int ID = ManModGUI.IDOffset;
        /// <summary>
        /// The gameobject hosting this
        /// </summary>
        public GameObject obj;
        /// <summary>
        /// The title of the display
        /// </summary>
        public string Header = "error";
        /// <summary>
        /// The popup is being displayed to the player
        /// </summary>
        public bool isOpen { get; internal set; } = false;
        /// <summary>
        /// The transparancy of the window [0 ~ 1]
        /// </summary>
        public float alpha = 0.65f;
        /// <summary>
        /// The dimensions of the window
        /// </summary>
        public Rect Window = new Rect(ManModGUI.DefaultWindow);   // the "window"
        /// <summary>
        /// The assigned typeHash of the window which is <b>typeof(<c>T</c>).GetHashCode()</b>
        /// </summary>
        public int typeHash = 0;
        /// <summary>
        /// The local player has their cursor hovering over this window.
        /// <para>Does not check if this is the foremost window they are hovering over!</para>
        /// </summary>
        public bool CursorWithinWindow => UIHelpersExt.MouseIsOverSubMenu(Window);
        /// <summary>
        /// The GUI menu assigned to this display
        /// </summary>
        public GUIMiniMenu GUIFormat { get; private set; }

        /// <summary>
        /// Show this popup
        /// </summary>
        public void Show() => ManModGUI.ShowPopup(this);
        /// <summary>
        /// Hide this popup
        /// </summary>
        public void Hide() => ManModGUI.HidePopup(this);
        /// <summary>
        /// Remove and DELETE this popup. Does not remove external references to attached GUIMiniMenu.
        /// </summary>
        public void Remove() => ManModGUI.RemovePopup(this);
        /// <summary>
        /// Rise this to the top and focus it
        /// </summary>
        public void FocusOnMe() => ManModGUI.SetCurrentPopup(this);

        /// <summary>
        /// Make sure the WHOLE POPUP is in view of the player
        /// </summary>
        public void MoveMeWithinScreenBoundsStrict() => ManModGUI.KeepWithinScreenBounds(this);
        /// <summary>
        /// Make sure at least a small section of the popup is in view of the player
        /// </summary>
        public void MoveMeWithinScreenBoundsNonStrict() => ManModGUI.KeepWithinScreenBoundsNonStrict(this);


        private void OnGUI()
        {
            if (isOpen)
            {
                if (!ManModGUI.SetupAltWins)
                {
                    AltUI.StartUI(alpha, alpha);
                    ManModGUI.styleDescLargeFont = new GUIStyle(GUI.skin.textField);
                    ManModGUI.styleDescLargeFont.fontSize = 16;
                    ManModGUI.styleDescLargeFont.alignment = TextAnchor.MiddleLeft;
                    ManModGUI.styleDescLargeFont.wordWrap = true;
                    ManModGUI.styleDescLargeFontScroll = new GUIStyle(ManModGUI.styleDescLargeFont);
                    ManModGUI.styleDescLargeFontScroll.alignment = TextAnchor.UpperLeft;
                    ManModGUI.styleDescFont = new GUIStyle(GUI.skin.textField);
                    ManModGUI.styleDescFont.fontSize = 12;
                    ManModGUI.styleDescFont.alignment = TextAnchor.UpperLeft;
                    ManModGUI.styleDescFont.wordWrap = true;
                    ManModGUI.styleLargeFont = new GUIStyle(GUI.skin.label);
                    ManModGUI.styleLargeFont.fontSize = 16;
                    ManModGUI.styleHugeFont = new GUIStyle(GUI.skin.button);
                    ManModGUI.styleHugeFont.fontSize = 20;
                    ManModGUI.styleGinormusFont = new GUIStyle(GUI.skin.button);
                    ManModGUI.styleGinormusFont.fontSize = 38;
                    ManModGUI.SetupAltWins = true;

                    ManModGUI.styleBlueFont = new GUIStyle(AltUI.TextfieldBlue);
                    ManModGUI.styleBlueFont.fontSize = 12;
                    ManModGUI.styleBlueFont.alignment = TextAnchor.UpperLeft;
                    ManModGUI.styleBlueFont.wordWrap = true;

                    ManModGUI.styleBorderedFont = new GUIStyle(AltUI.TextfieldBordered);
                    ManModGUI.styleBorderedFont.fontSize = 12;
                    ManModGUI.styleBorderedFont.wordWrap = true;
                    ManModGUI.styleBorderedFont.alignment = TextAnchor.UpperLeft;

                    ManModGUI.styledFont = new GUIStyle(AltUI.MenuGUI.label);
                    ManModGUI.styledFont.fontSize = 12;
                    ManModGUI.styledFont.wordWrap = true;
                    ManModGUI.styledFont.alignment = TextAnchor.UpperLeft;

                    ManModGUI.styleLargeFont = new GUIStyle(AltUI.MenuGUI.label);
                    ManModGUI.styleLargeFont.fontSize = 16;
                    ManModGUI.styleLargeFont.wordWrap = true;
                    ManModGUI.styleLargeFont.alignment = TextAnchor.MiddleCenter;

                    ManModGUI.styleBlackLargeFont = new GUIStyle(AltUI.TextfieldBlackHuge);
                    ManModGUI.styleBlackLargeFont.fontSize = 16;
                    ManModGUI.styleBlackLargeFont.wordWrap = true;
                    ManModGUI.styleBlackLargeFont.alignment = TextAnchor.MiddleLeft;

                    ManModGUI.styleScrollFont = new GUIStyle(AltUI.MenuGUI.label);
                    ManModGUI.styleScrollFont.fontSize = 16;
                    ManModGUI.styleScrollFont.wordWrap = true;
                    ManModGUI.styleScrollFont.alignment = TextAnchor.UpperLeft;

                    ManModGUI.styleLabelLargerFont = new GUIStyle(AltUI.MenuGUI.label);
                    ManModGUI.styleLabelLargerFont.fontSize = 16;

                    ManModGUI.styleButtonHugeFont = new GUIStyle(AltUI.MenuGUI.button);
                    ManModGUI.styleButtonHugeFont.fontSize = 20;

                    ManModGUI.styleButtonGinormusFont = new GUIStyle(AltUI.ButtonBlueLarge);
                    ManModGUI.styleButtonGinormusFont.fontSize = 38;

                    Debug_TTExt.Log("TerraTechETCUtil: ManModGUI performed first setup");
                    AltUI.EndUI();
                }
                if (Window.height > 400)
                {
                    Window = AltUI.Window(ID, Window, GUIFormat.RunGUI, Header, alpha, Hide);
                }
                else
                {
                    AltUI.StartUI(alpha, alpha);
                    Window = GUI.Window(ID, Window, GUIFormat.RunGUI, Header);
                    AltUI.EndUI();
                }
            }
        }

        internal void SetupGUI<T>(GUIDisplayStats stats, int IDSet)
            where T : GUIMiniMenu<T>, new()
        {
            if (GUIFormat != null)
            {
                if (typeHash != typeof(T).GetHashCode())
                {
                    Debug_TTExt.Assert("TerraTechETCUtil: SetupGUI - Illegal type change on inited GUIPopupDisplay!");
                    GUIFormat.OnRemoval();
                    GUIFormat = null;
                }
            }
            if (stats != null)
            {
                Window = new Rect(stats.windowSize);
                if (stats.alpha >= 0)
                    alpha = stats.alpha;
            }
            T newType = new T
            {
                Display = this
            };
            typeHash = newType.typeHash;
            GUIFormat = newType;
            ID = IDSet;
            GUIFormat.Setup(stats);
        }
        internal void SetupExistingGUI<T>(T instance, GUIDisplayStats stats, int IDSet)
            where T : GUIMiniMenu<T>, new()
        {
            if (GUIFormat != null)
            {
                if (typeHash != typeof(T).GetHashCode())
                {
                    Debug_TTExt.Assert("TerraTechETCUtil: SetupGUI - Illegal type change on inited GUIPopupDisplay!");
                    GUIFormat.OnRemoval();
                    GUIFormat = null;
                }
            }
            if (stats != null)
            {
                Window = new Rect(stats.windowSize);
                if (stats.alpha >= 0)
                    alpha = stats.alpha;
            }
            instance.Display = this;
            typeHash = instance.typeHash;
            GUIFormat = instance;
            ID = IDSet;
            GUIFormat.Setup(stats);
        }
    }

    /// <summary>
    /// Additional information when creating the window
    /// </summary>
    public class GUIDisplayStats
    {
        /// <summary>
        /// Size of the window
        /// </summary>
        public Rect windowSize = new Rect(ManModGUI.DefaultWindow);
        /// <summary>
        /// Transparancy of the window
        /// </summary>
        public float alpha = -1;
    }
}
