using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TerraTechETCUtil
{
    /// <summary>
    /// THE POCKET BUTTON MANAGER
    /// </summary>
    public class GUIButtonMadness : MonoBehaviour
    {
        /// <summary>
        /// Create <see cref="GUIButtonMadness"/>.
        /// <para>Use the instance provided in the output and call 
        /// <see cref="ReInitiate(int, string, GUI_BM_Element[], Func{bool})"/> to change as needed</para>
        /// <para><b>Do not call this frequently and reuse the output!</b></para>
        /// </summary>
        /// <param name="ID">IMGUI window ID, make sure this doesn't conflict!</param>
        /// <param name="Name">Name of the window displayed if there are more buttons than vanilla supports</param>
        /// <param name="elementsToUse">The UI elements to use</param>
        /// <param name="CanDisplay">Function to determine if this is displayed or not</param>
        /// <returns>New instance that should be stored in a field for later use</returns>
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
        /// <summary>
        /// Change the settings of <see cref="GUIButtonMadness"/>.
        /// </summary>
        /// <param name="ID">IMGUI window ID, make sure this doesn't conflict!</param>
        /// <param name="Name">Name of the window displayed if there are more buttons than vanilla supports</param>
        /// <param name="elementsToUse">The UI elements to use</param>
        /// <param name="CanDisplay">Function to determine if this is displayed or not</param>
        /// <returns>New instance that should be stored in a field for later use</returns>
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
        /// <summary>
        /// Remove and destroy this immedeately
        /// </summary>
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
        /// <summary>
        /// Regenerate the modal menu UI
        /// </summary>
        public void SetDirty()
        {
            if (UseRadialMode)
                GUIModModal.SetDirty(elements.Length);
        }
        /// <summary>
        /// See if this is permitted to display
        /// </summary>
        /// <returns>True if this can display</returns>
        public bool DefaultCanDisplay()
        {
            return UIHelpersExt.IsIngame;
        }
        /// <summary>
        /// See if this is permitted to continue displaying
        /// </summary>
        /// <returns>True if this can continue displaying</returns>
        public bool DefaultCanContinueDisplay()
        {
            return UIHelpersExt.IsIngame && (UseRadialMode || openTime > 0 || UIHelpersExt.MouseIsOverGUIMenu(HotWindow));
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

        /// <summary>
        /// Call this to check if the multi-button non-vanilla modal is open
        /// </summary>
        /// <returns>True if this is open, and not the vanilla modal</returns>
        public bool GUIIsOpen()
        {
            if (UseRadialMode)
            {
                GUIModModal.ModalIsOpen(elements.Length);
                return false;
            }
            else
            {
                UIHelpersExt.ClampGUIToScreen(ref HotWindow, true);
                return gameObject.activeSelf;
            }
        }
        /// <summary>
        /// Open the GUI for a block
        /// </summary>
        /// <param name="opener">Target block</param>
        public void OpenGUI(TankBlock opener)
        {
            openTime = 1f;
            //UseRadialMode = PlaceholderRadialMenu.CanUseWheel(elements);
            if (UseRadialMode)
                GUIModModal.OpenVanillaModal(opener, elements);
            else
            {
                UIHelpersExt.ClampGUIToScreen(ref HotWindow, true);
                gameObject.SetActive(true);
            }
        }
        /// <summary>
        /// Close this
        /// </summary>
        public void CloseGUI()
        {
            if (UseRadialMode)
            {
                GUIModModal.CloseModal(elements.Length);
            }
            else
            {
                UIHelpersExt.ReleaseControl();
                gameObject.SetActive(false);
            }
        }

    }
    /// <summary>
    /// Interface for elements displayed by <see cref="GUIButtonMadness"/>
    /// <para>See also <seealso cref="ExtModuleClickable"/></para>
    /// </summary>
    public interface GUI_BM_Element
    {
        /// <summary>
        /// Name of the element to use
        /// </summary>
        string GetName { get; }
        /// <summary>
        /// Should this element request a slider on the UI?
        /// </summary>
        bool GetSlider { get; }
        /// <summary>
        /// If this has a slider, the text name for the slider
        /// </summary>
        string GetSliderDescName { get; }
        /// <summary>
        /// Icon to use for this element
        /// </summary>
        Sprite GetIcon { get; }
        /// <summary>
        /// If this has a slider, the clamp step for the slider value
        /// </summary>
        int GetClampSteps { get; }
        /// <summary>
        /// Get the last slider value if this is a slider
        /// </summary>
        float GetLastVal { get; set; }
        /// <summary>
        /// Get the value of the element directly from the target
        /// </summary>
        float GetSet { get; set; }
    }
    /// <summary>
    /// <inheritdoc cref="GUI_BM_Element"/>
    /// <para>This is for simple elements</para>
    /// </summary>
    public class GUI_BM_Element_Simple : GUI_BM_Element
    {
        /// <inheritdoc/>
        public string GetName { get { return Name; } }
        /// <inheritdoc/>
        public Sprite GetIcon
        {
            get
            {
                if (OnIcon != null)
                    return OnIcon.Invoke();
                return null;
            }
        }
        /// <inheritdoc/>
        public bool GetSlider { get { return OnDesc != null; } }
        /// <inheritdoc/>
        public string GetSliderDescName { get { return OnDesc.Invoke(); } }
        /// <inheritdoc/>
        public int GetClampSteps { get { return ClampSteps; } }
        /// <inheritdoc/>
        public float GetLastVal { get { return LastVal; } set { LastVal = value; } }
        /// <inheritdoc/>
        public float GetSet { get { return OnSet(float.NaN); } set { OnSet.Invoke(value); } }

        /// <inheritdoc cref="GetName"/>
        public string Name;
        /// <inheritdoc cref="GetSliderDescName"/>
        public Func<string> OnDesc;
        /// <inheritdoc cref="GetIcon"/>
        public Func<Sprite> OnIcon;
        /// <inheritdoc cref="GetClampSteps"/>
        public int ClampSteps;
        /// <inheritdoc cref="GetLastVal"/>
        public float LastVal;
        /// <inheritdoc cref="GetSet"/>
        public Func<float, float> OnSet;
    }
    /// <inheritdoc cref="GUI_BM_Element"/>
    /// <summary>
    /// <para>This is for advanced elements</para>
    /// </summary>
    public class GUI_BM_Element_Complex : GUI_BM_Element
    {
        /// <inheritdoc/>
        public string GetName { get { return Name.Invoke(); } }
        /// <inheritdoc/>
        public Sprite GetIcon
        {
            get
            {
                if (OnIcon != null)
                    return OnIcon.Invoke();
                return null;
            }
        }
        /// <inheritdoc/>
        public bool GetSlider { get { return OnDesc != null; } }
        /// <inheritdoc/>
        public string GetSliderDescName { get { return OnDesc.Invoke(); } }
        /// <inheritdoc/>
        public int GetClampSteps { get { return ClampSteps; } }
        /// <inheritdoc/>
        public float GetLastVal { get { return LastVal; } set { LastVal = value; } }
        /// <inheritdoc/>
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
        /// <inheritdoc cref="GetName"/>
        public Func<string> Name;
        /// <inheritdoc cref="GetSliderDescName"/>
        public Func<string> OnDesc;
        /// <inheritdoc cref="GetIcon"/>
        public Func<Sprite> OnIcon;
        /// <inheritdoc cref="GetClampSteps"/>
        public int ClampSteps;
        /// <inheritdoc cref="GetLastVal"/>
        public float LastVal;
        /// <inheritdoc cref="GetSet"/>
        public Func<float, float> OnSet;
    }
}
