using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace TerraTechETCUtil
{
    public class ManModGUI : MonoBehaviour
    {   // Global popup manager
        public static ManModGUI inst;

        public const int IDOffset = 136000;
        private const int MaxPopups = 48;
        private const int MaxPopupsActive = 32;

        public const int LargeWindowWidth = 1000;
        public static Rect DefaultWindow = new Rect(0, 0, 300, 300);   // the "window"
        public static Rect LargeWindow = new Rect(0, 0, 1000, 600);
        public static Rect WideWindow = new Rect(0, 0, 700, 180);
        public static Rect SmallWideWindow = new Rect(0, 0, 600, 140);
        public static Rect SmallHalfWideWindow = new Rect(0, 0, 350, 140);
        public static Rect SideWindow = new Rect(0, 0, 200, 125);
        public static Rect TinyWindow = new Rect(0, 0, 160, 75);
        public static Rect MicroWindow = new Rect(0, 0, 110, 40);
        public static Rect TinyWideWindow = new Rect(0, 0, 260, 100);
        public static Rect SmallWindow = new Rect(0, 0, 160, 120);

        //public static GUIStyle styleSmallFont;
        public static GUIStyle styleDescFont;
        public static GUIStyle styleDescLargeFont;
        public static GUIStyle styleDescLargeFontScroll;
        public static GUIStyle styleLargeFont;
        public static GUIStyle styleHugeFont;
        public static GUIStyle styleGinormusFont;
        public static GUIStyle styleBlueFont;
        public static GUIStyle styleBorderedFont;
        public static GUIStyle styledFont;
        public static GUIStyle styleBlackLargeFont;
        public static GUIStyle styleScrollFont;
        public static GUIStyle styleLabelLargerFont;
        public static GUIStyle styleButtonHugeFont;
        public static GUIStyle styleButtonGinormusFont;


        public static List<GUIPopupDisplay> AllPopups = new List<GUIPopupDisplay>();
        public static Vector3 PlayerLoc = Vector3.zero;
        public static bool isCurrentlyOpen = false;
        public static bool IndexesChanged = false;

        public static int numActivePopups;
        public static bool SetupAltWins = false;


        private static int IDCur = IDOffset;
        private static GUIPopupDisplay currentGUIWindow;
        private static int updateClock = 0;
        private static int updateClockDelay = 50;

        private static HashSet<ModBase> registered = new HashSet<ModBase>();


        public static void RequestInit(ModBase modRequestor)
        {
            if (!inst)
            {
                inst = new GameObject("ManModGUI").AddComponent<ManModGUI>();
                Debug_TTExt.Log("TerraTechETCUtil: ManModGUI initated");
                inst.InvokeRepeating("LateInitiate", 0.75f, 0.5f);
            }
            if (!registered.Contains(modRequestor))
                registered.Add(modRequestor);
        }
        public void LateInitiate()
        {
            try
            {
                ManPauseGame.inst.PauseEvent.Subscribe(SetVisibilityOfAllPopups);
                inst.CancelInvoke("LateInitiate");
            }
            catch { }
        }
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

        public static void SetCurrentPopup(GUIPopupDisplay disp)
        {
            if (AllPopups.Contains(disp))
            {
                currentGUIWindow = disp;
            }
            else
            {
                Debug_TTExt.Log("TerraTechETCUtil: GUIPopupDisplay \"" + disp.context + "\" is not in the AllPopups list!");
            }
        }
        public static GUIPopupDisplay GetCurrentPopup()
        {
            return currentGUIWindow;
        }
        public static void KeepWithinScreenBounds(GUIPopupDisplay disp)
        {
            disp.Window.x = Mathf.Clamp(disp.Window.x, 0, Display.main.renderingWidth - disp.Window.width);
            disp.Window.y = Mathf.Clamp(disp.Window.y, 0, Display.main.renderingHeight - disp.Window.height);
        }
        public static void KeepWithinScreenBoundsNonStrict(GUIPopupDisplay disp)
        {
            disp.Window.x = Mathf.Clamp(disp.Window.x, 10 - disp.Window.width, Display.main.renderingWidth - 10);
            disp.Window.y = Mathf.Clamp(disp.Window.y, 10 - disp.Window.height, Display.main.renderingHeight - 10);
        }
        /// <summary>
        /// screenPos x and y must be within 0-1 float range!
        /// </summary>
        /// <param name="screenPos"></param>
        /// <param name="disp"></param>
        public static void ChangePopupPositioning(Vector2 screenPos, GUIPopupDisplay disp)
        {
            disp.Window.x = (Display.main.renderingWidth - disp.Window.width) * screenPos.x;
            disp.Window.y = (Display.main.renderingHeight - disp.Window.height) * screenPos.y;
        }
        /// <summary>
        /// screenPos x and y must be within 0-1 float range!
        /// </summary>
        /// <param name="screenPos"></param>
        /// <param name="disp"></param>
        public static bool PopupPositioningApprox(GUIPopupDisplay disp, float squareDelta = 0.1f)
        {
            float valX = disp.Window.x / (Display.main.renderingWidth - disp.Window.width);
            float valY = disp.Window.y / (Display.main.renderingHeight - disp.Window.height);
            return valX > -squareDelta && valX < squareDelta && valY > -squareDelta && valY < squareDelta;
        }
        public static bool DoesPopupExist(string title, int typeHash, out GUIPopupDisplay exists)
        {
            exists = AllPopups.Find(delegate (GUIPopupDisplay cand)
            {
                return cand.typeHash == typeHash && cand.context.CompareTo(title) == 0;
            }
            );
            return exists;
        }
        public static GUIPopupDisplay GetPopup(string title, int typeHash)
        {
            return AllPopups.Find(delegate (GUIPopupDisplay cand)
            {
                return cand.typeHash == typeHash && cand.context.CompareTo(title) == 0;
            }
            );
        }
        public static List<GUIPopupDisplay> GetAllPopups(int typeHash)
        {
            return AllPopups.FindAll(delegate (GUIPopupDisplay cand)
            {
                return cand.typeHash == typeHash;
            }
            );
        }
        public static List<GUIPopupDisplay> GetAllActivePopups(int typeHash, bool active = true)
        {
            return AllPopups.FindAll(delegate (GUIPopupDisplay cand)
            {
                return cand.isOpen == active && cand.typeHash == typeHash;
            }
            );
        }

        public static bool IsMouseOverModGUI { get; private set; } = false;
        public static bool IsMouseOverOtherModGUI = false;
        public static bool IsWorldInteractionBlocked => ManPointer.inst.IsInteractionBlocked || IsMouseOverModGUI;
        public static bool IsUIInteractionBlocked => ManPointer.inst.DraggingItem;
        private static void UpdateMouseOverAnyWindow()
        {
            bool isCurrent = false;
            foreach (var item in AllPopups)
            {
                if (item.isOpen && UIHelpersExt.MouseIsOverSubMenu(item.Window))
                {
                    isCurrent = true;
                    break;
                }
            }
            if (IsMouseOverOtherModGUI)
                isCurrent = true;
            if (isCurrent != IsMouseOverModGUI)
            {
                IsMouseOverModGUI = isCurrent;
                ManPointer.inst.PreventInteraction(ManPointer.PreventChannel.HUD, isCurrent);
                if (!isCurrent)
                    UIHelpersExt.ReleaseControl();
            }
            IsMouseOverOtherModGUI = false;
        }


        public static bool AddPopupStackable<T>(string menuName, GUIDisplayStats windowOverride = null)
            where T : GUIMiniMenu<T>, new()
        {
            if (MaxPopups <= AllPopups.Count)
            {
                Debug_TTExt.Log("TerraTechETCUtil: Too many popups!!!  Aborting AddPopup!");
                return false;
            }
            var gameObj = new GameObject(menuName);
            currentGUIWindow = gameObj.AddComponent<GUIPopupDisplay>();
            currentGUIWindow.obj = gameObj;
            currentGUIWindow.context = menuName;
            currentGUIWindow.Window = new Rect(DefaultWindow);
            currentGUIWindow.SetupGUI<T>(windowOverride, IDCur);
            IDCur++;
            gameObj.SetActive(false);
            currentGUIWindow.isOpen = false;

            AllPopups.Add(currentGUIWindow);
            return true;
        }

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
            var gameObj = new GameObject(Header);
            currentGUIWindow = gameObj.AddComponent<GUIPopupDisplay>();
            currentGUIWindow.obj = gameObj;
            currentGUIWindow.context = Header;
            currentGUIWindow.Window = new Rect(DefaultWindow);
            currentGUIWindow.SetupGUI<T>(windowOverride, IDCur);
            IDCur++;
            gameObj.SetActive(false);
            currentGUIWindow.isOpen = false;

            AllPopups.Add(currentGUIWindow);
            return true;
        }
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
            var gameObj = new GameObject(Header);
            currentGUIWindow = gameObj.AddComponent<GUIPopupDisplay>();
            currentGUIWindow.obj = gameObj;
            currentGUIWindow.context = Header;
            currentGUIWindow.Window = new Rect(DefaultWindow);
            currentGUIWindow.SetupExistingGUI(instance, windowOverride, IDCur);
            IDCur++;
            gameObj.SetActive(false);
            currentGUIWindow.isOpen = false;

            AllPopups.Add(currentGUIWindow);
            return true;
        }
        private static void RefreshPopup<T>(GUIPopupDisplay disp, GUIDisplayStats windowOverride = null)
            where T : GUIMiniMenu<T>, new()
        {
            disp.SetupGUI<T>(windowOverride, disp.ID);
        }
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
        }
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
        /// Position in percent of the max screen width and length
        /// </summary>
        /// <param name="screenPos"></param>
        /// <returns></returns>
        public static bool ShowPopup(Vector2 screenPos, GUIPopupDisplay disp)
        {
            bool shown = ShowPopup(disp);
            if (screenPos.x > 1)
                screenPos.x = 1;
            if (screenPos.y > 1)
                screenPos.y = 1;
            disp.Window.x = (Display.main.renderingWidth - disp.Window.width) * screenPos.x;
            disp.Window.y = (Display.main.renderingHeight - disp.Window.height) * screenPos.y;

            return shown;
        }
        /// <summary>
        /// Position in percent of the max screen width and length. 
        ///  Controls the currentGUIWindow
        /// </summary>
        public static bool ShowPopup(Vector2 screenPos)
        {
            bool shown = ShowPopup();
            if (screenPos.x > 1)
                screenPos.x = 1;
            if (screenPos.y > 1)
                screenPos.y = 1;
            currentGUIWindow.Window.x = (Display.main.renderingWidth - currentGUIWindow.Window.width) * screenPos.x;
            currentGUIWindow.Window.y = (Display.main.renderingHeight - currentGUIWindow.Window.height) * screenPos.y;

            return shown;
        }
        public static bool ShowPopup(GUIPopupDisplay disp)
        {
            if (MaxPopupsActive <= numActivePopups)
            {
                Debug_TTExt.Assert(false, "Too many popups active!!!  Limit is " + MaxPopupsActive + ".  Aborting ShowPopup!");
                return false;
            }
            if (disp.isOpen)
            {
                //Debug_TTExt.Log("TerraTechETCUtil: ShowPopup - Window is already open");
                return false;
            }
            Debug_TTExt.Log("TerraTechETCUtil: Popup " + disp.context + " active");
            disp.GUIFormat.OnOpen();
            disp.obj.SetActive(true);
            disp.isOpen = true;
            Singleton.Manager<ManSFX>.inst.PlayUISFX(ManSFX.UISfxType.InfoOpen);
            numActivePopups++;
            return true;
        }
        /// <summary>
        /// Controls the currentGUIWindow
        /// </summary>
        public static bool ShowPopup()
        {
            if (MaxPopupsActive <= numActivePopups)
            {
                Debug_TTExt.Assert(false, "Too many popups active!!!  Limit is " + MaxPopupsActive + ".  Aborting ShowPopup!");
                return false;
            }
            if (currentGUIWindow.isOpen)
            {
                //Debug_TTExt.Log("TerraTechETCUtil: ShowPopup - Window is already open");
                return false;
            }
            numActivePopups++;
            Debug_TTExt.Log("TerraTechETCUtil: Popup " + currentGUIWindow.context + " active");
            currentGUIWindow.GUIFormat.OnOpen();
            currentGUIWindow.obj.SetActive(true);
            currentGUIWindow.isOpen = true;
            Singleton.Manager<ManSFX>.inst.PlayUISFX(ManSFX.UISfxType.InfoOpen);
            return true;
        }

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

        private static bool allPopupsOpen = true;
        private static List<GUIPopupDisplay> PopupsClosed = new List<GUIPopupDisplay>();
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
            UpdateMouseOverAnyWindow();
            UpdateAllFast();
            if (updateClock > updateClockDelay)
            {
                UpdateAllPopups();
                updateClock = 0;
            }
            updateClock++;
        }

        public static void UpdateAllPopups()
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

    public class GUIPopupDisplay : MonoBehaviour
    {
        internal int ID = ManModGUI.IDOffset;
        public GameObject obj;
        public string context = "error";
        public bool isOpen { get; internal set; } = false;
        public float alpha = 0.65f;
        public Rect Window = new Rect(ManModGUI.DefaultWindow);   // the "window"
        public int typeHash = 0;
        public bool CursorWithinWindow => UIHelpersExt.MouseIsOverSubMenu(Window);
        public GUIMiniMenu GUIFormat { get; private set; }

        public void Show() => ManModGUI.ShowPopup(this);
        public void Hide() => ManModGUI.HidePopup(this);
        public void Remove() => ManModGUI.RemovePopup(this);
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
                    Window = AltUI.Window(ID, Window, GUIFormat.RunGUI, context, alpha, Hide);
                }
                else
                {
                    AltUI.StartUI(alpha, alpha);
                    Window = GUI.Window(ID, Window, GUIFormat.RunGUI, context);
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

    public class GUIDisplayStats
    {
        public Rect windowSize = new Rect(ManModGUI.DefaultWindow);
        public float alpha = -1;
    }
    public abstract class GUIMiniMenu
    {
        internal int typeHash => GetType().GetHashCode();
        public GUIPopupDisplay Display { get; internal set; }

        public bool ShowPopup()
        {
            return ManModGUI.ShowPopup(Display);
        }
        public bool ShowPopup(Vector2 screenPosPercent)
        {
            return ManModGUI.ShowPopup(screenPosPercent, Display);
        }
        public bool HidePopup()
        {
            return ManModGUI.HidePopup(Display);
        }
        public void MovePopup(Vector2 screenPosPercent)
        {
            ManModGUI.ChangePopupPositioning(screenPosPercent, Display);
        }
        public bool RemovePopup()
        {
            return ManModGUI.RemovePopup(Display);
        }


        public abstract void Setup(GUIDisplayStats stats);

        public abstract void RunGUI(int ID);

        public abstract void DelayedUpdate();
        public abstract void FastUpdate();
        public abstract void OnRemoval();

        public virtual void OnOpen() { }
    }

    /// <summary>
    /// A universal menu for use
    /// </summary>
    public abstract class GUIMiniMenu<T> : GUIMiniMenu where T : GUIMiniMenu<T>, new()
    {
        public static bool RegisterMenuToManager(string name, GUIDisplayStats stats = null)
        {
            return ManModGUI.AddPopupSingle<T>(name, stats);
        }
        public static bool Register1StackableMenuToManager(string name, GUIDisplayStats stats = null)
        {
            return ManModGUI.AddPopupStackable<T>(name, stats);
        }
    }
    public class GUIMiniMenuBasic : GUIMiniMenu<GUIMiniMenuBasic>
    {
        public override void Setup(GUIDisplayStats stats) { }

        public override void RunGUI(int ID)
        {
        }

        public override void DelayedUpdate() { }
        public override void FastUpdate() { }
        public override void OnRemoval() { }
    }
}
