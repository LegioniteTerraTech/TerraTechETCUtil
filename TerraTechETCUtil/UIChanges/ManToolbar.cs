using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TerraTechETCUtil
{
    public class ManToolbar
    {
        public abstract class ToolbarElement
        {
            public readonly string Name;
            public readonly Sprite Sprite;
            public bool IsShowing => Element.IsShowing;
            protected GameObject inst;
            internal abstract UIHUDElement Element { get; }
            public ToolbarElement(string name, Sprite iconSprite)
            {
                Name = name;
                Sprite = iconSprite;
            }
            public void Remove()
            {
                UnityEngine.Object.Destroy(inst);
                Elements.Remove(this);
            }
            internal abstract void Initiate();
            protected virtual void InsureWorking()
            {
                if (!ready)
                    throw new InvalidOperationException("ManToolbar is not ready yet!  The call must wait until the game loads.");
                if (inst == null)
                    throw new NullReferenceException("ManToolbar.ToolbarElement failed to get the UI instance!");
                if (Element == null)
                    throw new NullReferenceException("ManToolbar.ToolbarElement failed to get the UI controller!");
                if (!inst.transform.parent)
                    throw new NullReferenceException("ManToolbar.ToolbarElement failed to get the parent for the toggle!  There MUST be a parent, how odd?!");
            }
            public virtual void Show()
            {
                InsureWorking();
                Element.SetVisible(true);
            }
            public virtual void ShowAndExpand()
            {
                Show();
                Element.Expand(null);
            }
            public virtual void Hide()
            {
                InsureWorking();
                Element.SetVisible(false);
                Element.Collapse(null);
            }
        }
        public class ToolbarButton : ToolbarElement
        {
            public readonly UnityAction Callback;
            private UIHUDButton togUI;
            internal override UIHUDElement Element => togUI;
            public ToolbarButton(string name, Sprite iconSprite, UnityAction callback) :
                base(name, iconSprite)
            {
                Callback = callback;
                InsuredStartup(this);
            }
            internal override void Initiate()
            {
                inst = MakePrefabButton(Name, Sprite, Callback).gameObject;
                if (!inst.transform.parent)
                    throw new NullReferenceException("ManToolbar.ToolbarButton failed to get the parent for the button!  There MUST be a parent, how odd?!");
                togUI = inst.transform.parent.GetComponentInChildren<UIHUDButton>(true);
                if (togUI == null)
                {
                    Utilities.LogGameObjectHierachy(inst.transform.parent.gameObject);
                    throw new NullReferenceException("ManToolbar.ToolbarButton failed to get the Button Controller UI instance!");
                }
                Elements.Add(this);
            }
        }
        public class ToolbarToggle : ToolbarElement
        {
            private static readonly FieldInfo TogInfo = typeof(UIHUDToggleButton).GetField("m_ToggleButton", BindingFlags.Instance | BindingFlags.NonPublic);
            public readonly UnityAction<bool> Callback;
            private UIHUDToggleButton togUI;
            internal override UIHUDElement Element => togUI;
            public ToolbarToggle(string name, Sprite iconSprite, UnityAction<bool> callback) :
                base(name, iconSprite)
            {
                Callback = callback;
                InsuredStartup(this);
            }
            internal override void Initiate()
            {
                inst = MakePrefabToggle(Name, Sprite, Callback).gameObject;
                if (!inst.transform.parent)
                    throw new NullReferenceException("ManToolbar.ToolbarToggle failed to get the parent for the toggle!  There MUST be a parent, how odd?!");
                togUI = inst.transform.parent.GetComponentInChildren<UIHUDToggleButton>(true);
                if (togUI == null)
                {
                    Utilities.LogGameObjectHierachy(inst.transform.parent.gameObject);
                    throw new NullReferenceException("ManToolbar.ToolbarToggle failed to get the Toggle Controller UI instance!");
                }
                Elements.Add(this);
            }
            public void SetToggleVisibility(bool state)
            {
                if (state)
                    Show();
                else
                    Hide();
            }
            public void SetToggleState(bool state)
            {
                //if (togUI == null)
                //    togUI = inst.transform.parent.GetComponentInChildren<UIHUDToggleButton>(true);
                if (togUI == null)
                {
                    //Utilities.LogGameObjectHierachy(inst.transform.parent.gameObject);
                    throw new NullReferenceException("ManToolbar.ToolbarToggle failed to get the Toggle Controller UI instance!");
                }
                if (inst && togUI)
                {
                    togUI.SetToggledState(state);
                    Toggle tog = (Toggle)TogInfo.GetValue(togUI);
                    if (tog == null)
                        throw new NullReferenceException("ManToolbar.ToolbarToggle failed to get the Toggle visual UI instance!");
                    tog.isOn = state;
                    Debug_TTExt.Info("TerraTechModExt: Set ManToolbar.ToolbarToggle");
                    togUI.Collapse(null);
                }
                else
                    Debug_TTExt.Log("TerraTechModExt: FAILED to close ManToolbar.ToolbarToggle");
            }
        }
        private static bool ready = false;
        private static Queue<ToolbarElement> queued = null;
        private static List<ToolbarElement> Elements = null;
        public static bool Ready => ready;

        private static void InsuredStartup(ToolbarElement add)
        {
            if (queued == null)
            {
                queued = new Queue<ToolbarElement>();
                Elements = new List<ToolbarElement>();
                ready = ManGameMode.inst.GetModePhase() == ManGameMode.GameState.InGame;
                if (ready)
                    ManGameMode.inst.ModeCleanUpEvent.Subscribe(NotReady);
                else
                    ManGameMode.inst.ModeStartEvent.Subscribe(TrySetup);
            }
            if (ready)
                add.Initiate();
            else
                queued.Enqueue(add);
        }
        private static void TrySetup(Mode ignor)
        {
            try
            {
                if (!ManGameMode.inst.GetIsInPlayableMode())
                    return;
                while (queued.Any())
                {
                    queued.Dequeue().Initiate();
                }
                ManGameMode.inst.ModeCleanUpEvent.Subscribe(NotReady);
                ManGameMode.inst.ModeStartEvent.Unsubscribe(TrySetup);
                ready = true;
            }
            catch (Exception e)
            {
                Debug_TTExt.Log("ManToolbar.TrySetup() ~ error - " + e);
            }
        }
        private static void NotReady(Mode ignor)
        {
            ready = false;
            ManGameMode.inst.ModeStartEvent.Subscribe(TrySetup);
            ManGameMode.inst.ModeCleanUpEvent.Unsubscribe(NotReady);
        }


        internal static FieldInfo textSet = typeof(TooltipComponent).GetField("m_ManuallySetText", BindingFlags.NonPublic | BindingFlags.Instance);
        private static GameObject MakePrefab(ManHUD.HUDElementType element, string name, Sprite iconSprite, Transform parentSet)
        {
            try
            {
                /*
                if (!Singleton.Manager<ManHUD>.inst.GetHudElement(ManHUD.HUDElementType.WorldMapButton))
                {
                    Debug_TTExt.Log("TTUtil: InitWiki - init  wiki button");
                    Singleton.Manager<ManHUD>.inst.SetCurrentHUD(ManHUD.HUDType.MainGame);
                    Singleton.Manager<ManHUD>.inst.InitialiseHudElement(ManHUD.HUDElementType.WorldMapButton);
                }*/
                Singleton.Manager<ManHUD>.inst.SetCurrentHUD(ManHUD.HUDType.MainGame);
                Singleton.Manager<ManHUD>.inst.InitialiseHudElement(element);
                bool prev = Singleton.Manager<ManHUD>.inst.IsHudElementVisible(element); 
                if (!prev)
                    Singleton.Manager<ManHUD>.inst.ShowHudElement(element);
                GameObject GO = Singleton.Manager<ManHUD>.inst.GetHudElement(element).gameObject;
                //GameObject GO = Resources.FindObjectsOfTypeAll<GameObject>().Last(x => x.name == "HUD_AnchorTech_Button");

                if (GO)
                {
                    //Debug_TTExt.Log("Search " + Nuterra.NativeOptions.UIUtilities.GetComponentTree(GO, " - "));
                    if (!GO.transform.parent)
                        throw new NullReferenceException("ManToolbar.GetPrefab() - GO.transform.parent null");
                    if (parentSet == null)
                        parentSet = GO.transform.parent;
                    var trans = UnityEngine.Object.Instantiate(GO.transform, parentSet);
                    /*
                    if (!trans.transform.parent)
                        throw new NullReferenceException("InitWiki() - trans.transform.parent null");
                    */
                    var tooltips = trans.GetComponentsInChildren<TooltipComponent>(true);
                    if (tooltips == null || tooltips.Length == 0)
                        throw new NullReferenceException("ManToolbar.GetPrefab() - tooltip null or empty");
                    foreach (var tooltip in tooltips)
                    {
                        textSet.SetValue(tooltip, false);
                        tooltip.SetText(name);
                    }

                    var images = trans.GetComponentsInChildren<Image>(true);
                    if (images == null || images.Length == 0)
                        throw new NullReferenceException("ManToolbar.GetPrefab() - images null or empty");
                    foreach (var image in images)
                    {
                        image.sprite = iconSprite;
                    }


                    if (!trans.GetComponent<RectTransform>())
                        throw new NullReferenceException("ManToolbar.GetPrefab() - rectTrans null");
                    Vector3 ver = trans.GetComponent<RectTransform>().anchoredPosition3D;
                    ver.x = ver.x + 40;
                    trans.GetComponent<RectTransform>().anchoredPosition3D = ver;

                    Debug_TTExt.Log("ManToolbar.GetPrefab() - Prefab Init");
                    return trans.gameObject;
                }
                else
                    Debug_TTExt.Assert("ManToolbar.GetPrefab()  - ManIngameWiki Button FAILED to init!!! (GO null)");
                if (!prev)
                    Singleton.Manager<ManHUD>.inst.HideHudElement(element);
            }
            catch (Exception e)
            {
                Debug_TTExt.Log("ManToolbar.GetPrefab() - ManIngameWiki Button FAILED to init!!! - " + e);
            }
            return null;
        }
        
        private static MethodInfo startupTog = typeof(UIHUDToggleButton).GetMethod("OnSpawn", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo actionTog = typeof(UIHUDToggleButton).GetField("m_ToggleAction", BindingFlags.NonPublic | BindingFlags.Instance);
        internal static Toggle MakePrefabToggle(string name, Sprite iconSprite, UnityAction<bool> callback, Transform parentSet = null)
        {
            var ButtonTrans = MakePrefab(ManHUD.HUDElementType.WorldMapButton, name, iconSprite, parentSet);
            var bu = ButtonTrans.GetComponentInChildren<Toggle>(true);
            if (!bu)
                throw new NullReferenceException("ManToolbar.GetPrefab() - Toggle null");
            var tog = ButtonTrans.GetComponentInChildren<UIHUDToggleButton>(true);
            if (!tog)
                throw new NullReferenceException("ManToolbar.GetPrefab() - tog null");
            startupTog.Invoke(tog, new object[] { });
            actionTog.SetValue(tog, callback);
            ButtonTrans.SetActive(true);
            return bu;
        }
        internal static Button MakePrefabButton(string name, Sprite iconSprite, UnityAction callback, Transform parentSet = null)
        {
            var ButtonTrans = MakePrefab(ManHUD.HUDElementType.ReturnToTeleporter, name, iconSprite, parentSet);
            var bu = ButtonTrans.GetComponentInChildren<Button>(true);
            if (!bu)
                throw new NullReferenceException("ManToolbar.GetPrefab() - Button null");
            var buSet = new Button.ButtonClickedEvent();
            buSet.AddListener(callback);
            bu.onClick = buSet;
            ButtonTrans.SetActive(true);
            return bu;
        }
    }
}
