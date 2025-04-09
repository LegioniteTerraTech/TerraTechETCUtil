using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Nuterra.NativeOptions;
using UnityEngine;

namespace TerraTechETCUtil
{
    public static class GUIModModal
    {
        public static bool ModalShown => LastModalButtonCount != 0;

        internal static Rect HotWindow = new Rect(0, 0, 250, 260);   // the "window"
        private static bool UseRadialMode = true;
        private static string Name;
        private static int setID;
        private static Func<bool> displayRules;
        private static GUI_BM_Element[] elementsExt;

        private static float openTime = 0;
        private static Vector2 scrolll = new Vector2(0, 0);
        private static float scrolllSize = 50;
        private const int ButtonWidth = 200;
        private const int MaxWindowHeight = 500;
        private static GUIExtendedModal modalExt;

        internal class GUIExtendedModal : MonoBehaviour
        {
            internal void OnGUI()
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
                    CloseModal();
            }
            internal void Update()
            {
                if (openTime > 0)
                    openTime -= Time.deltaTime;
            }
        }

        private static void GUIHandler(int ID)
        {
            bool clicked = false;
            int VertPosOff = 0;
            bool MaxExtensionY = false;

            scrolll = GUI.BeginScrollView(new Rect(0, 30, HotWindow.width - 20, HotWindow.height - 40), scrolll, new Rect(0, 0, HotWindow.width - 50, scrolllSize));

            int Entries = elementsExt.Length;
            for (int step = 0; step < Entries; step++)
            {
                try
                {
                    try
                    {
                        GUI_BM_Element ele = elementsExt[step];

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
                    catch (ExitGUIException e) { throw e; }
                    catch { }
                    VertPosOff += 30;
                    if (VertPosOff >= MaxWindowHeight)
                        MaxExtensionY = true;
                }
                catch (ExitGUIException e) { throw e; }
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

        private static void InsureModalExt()
        {
            if (modalExt == null)
            {
                modalExt = new GameObject("ModalExt").AddComponent<GUIExtendedModal>();
                modalExt.enabled = false;
            }
        }

        public static bool DefaultCanContinueDisplay()
        {
            return UIHelpersExt.IsIngame && (UseRadialMode || openTime > 0 || UIHelpersExt.MouseIsOverSubMenu(HotWindow));
        }

        public static bool CanContinueDisplayOverlap()
        {
            return UseRadialMode || openTime > 0 || UIHelpersExt.MouseIsOverSubMenu(HotWindow);
        }

        private static Dictionary<int, PlaceholderRadialMenu> MenuPanelPrefabs = null;
        private static int LastModalButtonCount = 0;

        /// <summary>
        /// Opens a Modal for player interaction.  Supports only [1-6] inputs.
        /// </summary>
        /// <param name="name">The name of the menu for modals with more than 6 inputs</param>
        /// <param name="elements">The options in the menu</param>
        /// <param name="CanDisplay">The check to tell if the window should remain open.  Leave null for default.</param>
        /// <param name="ID">The GUI window's unique ID</param>
        /// <exception cref="NullReferenceException"></exception>
        /// <exception cref="IndexOutOfRangeException"></exception>
        public static void OpenModal(string name, GUI_BM_Element[] elements, Func<bool> CanDisplay = null, int ID = 3691337)
        {
            if (ModalShown)
                CloseModal();
            if (elements == null || elements.Length == 0)
                throw new InvalidOperationException("elements is null or empty");
            Name = "<b>" + name + "</b>";
            setID = ID;
            if (CanDisplay == null)
                displayRules = DefaultCanContinueDisplay;
            else
                displayRules = CanDisplay;
            elementsExt = elements;
            LastModalButtonCount = elements.Length;
            UseRadialMode = LastModalButtonCount <= 6;
            if (UseRadialMode)
            {
                InsureRadialMenuPrefabs();
                if (MenuPanelPrefabs.TryGetValue(elements.Length, out PlaceholderRadialMenu PRM))
                {
                    //Debug_TTExt.Log("GUIModModal.OpenModal() - " + Time.time);
                    PRM.ShowThisNoBlock(elements);
                }
                else
                    throw new IndexOutOfRangeException("OpenGUI - Element count [" + elements.Length + "] is out of range of given: [1 - 6]");
            }
            else
            {
                InsureModalExt();
                UIHelpersExt.ClampMenuToScreen(ref HotWindow, true);
                elementsExt = elements;
                modalExt.enabled = true;
            }
        }
        /// <summary>
        /// Opens a Modal with block context for player interaction.  Supports only [1-6] inputs.
        /// </summary>
        /// <param name="opener">A valid block for it to open context for</param>
        /// <param name="name">The name of the menu for modals with more than 6 inputs</param>
        /// <param name="elements">The options in the menu</param>
        /// <param name="CanDisplay">The check to tell if the window should remain open.  Leave null for default.</param>
        /// <param name="ID">The GUI window's unique ID</param>
        /// <exception cref="NullReferenceException"></exception>
        /// <exception cref="IndexOutOfRangeException"></exception>
        public static void OpenModal(TankBlock opener, string name, GUI_BM_Element[] elements, Func<bool> CanDisplay = null, int ID = 3691337)
        {
            if (ModalShown)
                CloseModal();
            if (elements == null || elements.Length == 0)
                throw new InvalidOperationException("elements is null or empty");
            Name = "<b>" + name + "</b>";
            setID = ID;
            if (CanDisplay == null)
                displayRules = DefaultCanContinueDisplay;
            else
                displayRules = CanDisplay;
            elementsExt = elements;
            LastModalButtonCount = elements.Length;
            UseRadialMode = LastModalButtonCount <= 6;
            if (UseRadialMode)
            {
                InsureRadialMenuPrefabs();
                if (MenuPanelPrefabs.TryGetValue(elements.Length, out PlaceholderRadialMenu PRM))
                {
                    if (opener == null)
                        throw new NullReferenceException("OpenGUI - opener was null for some reason. It should NOT be null EVER!");
                    //Debug_TTExt.Log("GUIModModal.OpenModal() - " + Time.time);
                    PRM.ShowThis(opener, elements);
                }
                else
                    throw new IndexOutOfRangeException("OpenGUI - Element count [" + elements.Length + "] is out of range of given: [1 - 6]");
            }
            else
            {
                InsureModalExt();
                UIHelpersExt.ClampMenuToScreen(ref HotWindow, true);
                elementsExt = elements;
                modalExt.enabled = true;
            }
        }
        /// <summary>
        /// Opens a vanilla UI Modal with block context for player interaction.  Supports only [1-6] inputs.
        /// </summary>
        /// <param name="opener">A valid block for it to open context for</param>
        /// <param name="elements">The options in the menu</param>
        /// <exception cref="NullReferenceException"></exception>
        /// <exception cref="IndexOutOfRangeException"></exception>
        public static void OpenVanillaModal(TankBlock opener, GUI_BM_Element[] elements)
        {
            if (ModalShown)
                CloseModal();
            if (elements == null || elements.Length == 0)
                throw new InvalidOperationException("elements is null or empty");
            LastModalButtonCount = elements.Length;
            InsureRadialMenuPrefabs();
            if (MenuPanelPrefabs.TryGetValue(elements.Length, out PlaceholderRadialMenu PRM))
            {
                if (opener == null)
                    throw new NullReferenceException("OpenGUI - opener was null for some reason. It should NOT be null EVER!");
                //Debug_TTExt.Log("GUIModModal.OpenModal() - " + Time.time);
                PRM.ShowThis(opener, elements);
            }
            else
                throw new IndexOutOfRangeException("OpenGUI - Element count [" + elements.Length + "] is out of range of given: [1 - 6]");
        }
        /// <summary>
        /// Closes the modal
        /// </summary>
        public static void CloseModal(int buttonCount)
        {
            if (UseRadialMode)
            {
                InsureRadialMenuPrefabs();
                if (MenuPanelPrefabs.TryGetValue(buttonCount, out PlaceholderRadialMenu PRM))
                {
                    PRM.Hide(null);
                }
            }
            else
            {
                InsureModalExt();
                UIHelpersExt.ReleaseControl();
                modalExt.enabled = false;
            }
            LastModalButtonCount = 0;
        }
        /// <summary>
        /// Closes the modal
        /// </summary>
        public static void CloseModal() => CloseModal(LastModalButtonCount);

        /// <summary>
        /// Returns true if modal is open
        /// </summary>
        public static bool ModalIsOpen(int buttonCount)
        {
            openTime = 1f;
            //UseRadialMode = PlaceholderRadialMenu.CanUseWheel(elements);
            if (UseRadialMode)
            {
                InsureRadialMenuPrefabs();
                if (MenuPanelPrefabs.TryGetValue(buttonCount, out PlaceholderRadialMenu PRM))
                {
                    //Debug_TTExt.Log("GUIModModal.ModalIsOpen() - " + Time.time);
                    return PRM.IsOpen;
                }
                return false;
            }
            else
            {
                InsureModalExt();
                UIHelpersExt.ClampMenuToScreen(ref HotWindow, true);
                modalExt.enabled = true;
                return true;
            }
        }
        /// <summary>
        /// Returns true if modal is open
        /// </summary>
        public static void ModalIsOpen() => ModalIsOpen(LastModalButtonCount);

        /// <summary>
        /// Refreshes the contents of the modal
        /// </summary>
        public static void SetDirty(int buttonCount)
        {
            if (!UseRadialMode)
                return;
            InsureRadialMenuPrefabs();
            if (MenuPanelPrefabs.TryGetValue(buttonCount, out PlaceholderRadialMenu PRM))
            {
                //Debug_TTExt.Log("GUIModModal.SetDirty() - " + Time.time);
                PRM.SetDirty();
            }
        }
        /// <summary>
        /// Refreshes the contents of the modal
        /// </summary>
        public static void SetDirty() => SetDirty(LastModalButtonCount);


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
                GameObject GO = UnityEngine.Object.Instantiate(rad.gameObject, rad.transform.parent);
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
                GameObject GO = UnityEngine.Object.Instantiate(rad.gameObject, rad.transform.parent);
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

    }

}
