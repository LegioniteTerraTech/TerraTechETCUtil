using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            protected GameObject inst;
            public ToolbarElement(string name, Sprite iconSprite)
            {
                Name = name;
                Sprite = iconSprite;
            }
            public void Remove()
            {
                UnityEngine.Object.Destroy(inst);
                Active.Remove(this);
            }
            internal abstract void Initiate();
        }
        public class ToolbarButton : ToolbarElement
        {
            public readonly UnityAction Callback;
            public ToolbarButton(string name, Sprite iconSprite, UnityAction callback) :
                base(name, iconSprite)
            {
                Callback = callback;
                InsuredStartup(this);
            }
            internal override void Initiate()
            {
                inst = MakePrefabButton(Name, Sprite, Callback).gameObject;
                Active.Add(this);
            }
        }
        public class ToolbarToggle : ToolbarElement
        {
            public readonly UnityAction<bool> Callback;
            private UIHUDToggleButton togUI;
            public ToolbarToggle(string name, Sprite iconSprite, UnityAction<bool> callback) :
                base(name, iconSprite)
            {
                Callback = callback;
                InsuredStartup(this);
            }
            internal override void Initiate()
            {
                inst = MakePrefabToggle(Name, Sprite, Callback).gameObject;
                togUI = inst.GetComponentInChildren<UIHUDToggleButton>(true);
                Active.Add(this);
            }
            public void SetToggleState(bool state)
            {
                if (inst && togUI)
                    togUI.SetToggledState(state);
            }
        }
        private static bool ready = false;
        private static Queue<ToolbarElement> queued = null;
        private static List<ToolbarElement> Active = null;

        private static void InsuredStartup(ToolbarElement add)
        {
            if (queued == null)
            {
                queued = new Queue<ToolbarElement>();
                Active = new List<ToolbarElement>();
                ManGameMode.inst.ModeStartEvent.Subscribe(TrySetup);
                ManGameMode.inst.ModeCleanUpEvent.Subscribe(NotReady);
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
