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
        public void SetDirty()
        {
            if (UseRadialMode)
                GUIModModal.SetDirty(elements.Length);
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

        public bool GUIIsOpen()
        {
            if (UseRadialMode)
            {
                GUIModModal.ModalIsOpen(elements.Length);
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
                GUIModModal.OpenVanillaModal(opener, elements);
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
                GUIModModal.CloseModal(elements.Length);
            }
            else
            {
                UIHelpersExt.ReleaseControl();
                gameObject.SetActive(false);
            }
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
}
