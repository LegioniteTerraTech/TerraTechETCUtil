using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace TerraTechETCUtil
{
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
        private static FieldInfo sliceDesc = typeof(UIRadialMenuOptionWithWarning).GetField("m_TooltipString", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo sliceWarn = typeof(UIRadialMenuOptionWithWarning).GetField("m_TooltipWarningString", BindingFlags.NonPublic | BindingFlags.Instance);
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
        public void ShowThisNoBlock(GUI_BM_Element[] elements)
        {
            OpenMenuEventData OMED = new OpenMenuEventData
            {
                m_AllowNonRadialMenu = false,
                m_AllowRadialMenu = true,
                m_RadialInputController = ManInput.RadialInputController.Mouse,
                m_TargetTankBlock = null,
            };

            elementsC = elements;
            InsureDominance();
            TryOverrideTexturesAndLabels(elementsC);
            //Debug_TTExt.Log("PlaceholderRadialMenu.ShowThisNoBlock() - " + Time.time);
            Show(OMED);
        }
        public override void Show(object context)
        {
            if (context == null)
                throw new NullReferenceException("Show was given NULL context!");
            OpenMenuEventData OMED = (OpenMenuEventData)context;
            base.Show(context);
            if (OMED.m_TargetTankBlock != null)
                radMenu.Show(OMED.m_RadialInputController, OMED.m_TargetTankBlock.tank != Singleton.playerTank);
            else
                radMenu.Show(OMED.m_RadialInputController, false);
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
                    sliceDesc.SetValue(warn, sharedLoc);
                    sliceWarn.SetValue(warn, sharedLoc);
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
                    sliceDesc.SetValue(warn, sharedLoc);
                    sliceWarn.SetValue(warn, sharedLoc);
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
                        sliceDesc.SetValue(warn2, sharedLoc);
                        sliceWarn.SetValue(warn2, sharedLoc);
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
                        sliceDesc.SetValue(warn, sharedLoc);
                        sliceWarn.SetValue(warn, sharedLoc);
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
