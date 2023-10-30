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
    public class ManAbilities
    {
        private static List<AbilityElement> updating = new List<AbilityElement>();
        public abstract class AbilityElement
        {
            public readonly string Name;
            public readonly Sprite Sprite;
            protected GameObject inst;
            protected float Cooldown = 1;
            private float cooldownCur = 0;
            protected Image[] images;
            public AbilityElement(string name, Sprite iconSprite, float cooldown)
            {
                Name = name;
                Sprite = iconSprite;
                Cooldown = cooldown;
            }
            public void Remove()
            {
                UnityEngine.Object.Destroy(inst);
                Active.Remove(this);
                updating.Remove(this);
            }
            internal abstract void Initiate();
            internal abstract void SetAvail(bool state);
            public void SetFillState(float fillPercent)
            {
                if (inst)
                {
                    foreach (var item in images)
                    {
                        item.fillAmount = fillPercent;
                    }
                }
            }
            public void TriggerCooldown()
            {
                if (inst)
                {
                    cooldownCur = Cooldown;
                    if (!updating.Any())
                    {
                        InvokeHelper.InvokeSingleRepeat(CheckCooldowns, 0);
                    }
                    SetAvail(false);
                    updating.Add(this);
                }
            }
            private static void CheckCooldowns()
            {
                for (int step = 0; step < updating.Count; step++)
                {
                    var item = updating[step];
                    if (item.inst)
                    {
                        item.cooldownCur -= Time.deltaTime;
                        if (item.cooldownCur <= 0)
                        {
                            item.cooldownCur = 0;
                            item.SetFillState(1);
                            item.SetAvail(true);
                            updating.RemoveAt(step);
                        }
                        else
                            item.SetFillState(1 - Mathf.Clamp01(item.cooldownCur / item.Cooldown));
                    }
                }
                if (!updating.Any())
                {
                    InvokeHelper.CancelInvokeSingleRepeat(CheckCooldowns);
                }
            }
        }
        public class AbilityButton : AbilityElement
        {
            public Button button;
            public readonly UnityAction Callback;
            public AbilityButton(string name, Sprite iconSprite, UnityAction callback, float cooldown) :
                base(name, iconSprite, cooldown)
            {
                Callback = callback;
                InsuredStartup(this);
            }
            public void TriggerCooldownWrapper()
            {
                Callback.Invoke();
                //TriggerCooldown();
            }
            internal override void SetAvail(bool state)
            {
                button.interactable = state;
            }
            internal override void Initiate()
            {
                button = MakePrefabButton(Name, Sprite, TriggerCooldownWrapper);
                inst = button.gameObject;
                images = inst.GetComponentsInChildren<Image>(true);
                Active.Add(this);
            }
        }
        public class AbilityToggle : AbilityElement
        {
            public Toggle toggle;
            public readonly UnityAction<bool> Callback;
            private UIHUDToggleButton togUI;
            public AbilityToggle(string name, Sprite iconSprite, UnityAction<bool> callback, float cooldown) :
                base(name, iconSprite, cooldown)
            {
                Callback = callback;
                InsuredStartup(this);
            }
            public void TriggerCooldownWrapper(bool input)
            {
                Callback.Invoke(input);
                //TriggerCooldown();
            }
            internal override void SetAvail(bool state)
            {
                toggle.interactable = state;
            }
            internal override void Initiate()
            {
                toggle = MakePrefabToggle(Name, Sprite, TriggerCooldownWrapper);
                inst = toggle.gameObject;
                images = inst.GetComponentsInChildren<Image>(true);
                togUI = inst.GetComponentInChildren<UIHUDToggleButton>(true);
                Active.Add(this);
            }
            public void SetToggleState(bool state)
            {
                if (inst)
                    togUI.SetToggledState(state);
            }
        }


        private static bool ready = false;
        private static GameObject inst;
        private static Queue<AbilityElement> queued = null;
        private static List<AbilityElement> Active = null;

        private static void InsuredStartup(AbilityElement add)
        {
            InitAbilityBar();
            if (queued == null)
            {
                queued = new Queue<AbilityElement>();
                Active = new List<AbilityElement>();
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
                Debug_TTExt.Log("ManAbilities.TrySetup) ~ error - " + e);
            }
        }
        private static void NotReady(Mode ignor)
        {
            ready = false;
        }
        public static void InitAbilityBar()
        {
            if (inst != null)
                return;
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
                Singleton.Manager<ManHUD>.inst.InitialiseHudElement(ManHUD.HUDElementType.Speedo);
                bool prev = Singleton.Manager<ManHUD>.inst.IsHudElementVisible(ManHUD.HUDElementType.Speedo);
                if (!prev)
                    Singleton.Manager<ManHUD>.inst.ShowHudElement(ManHUD.HUDElementType.Speedo);
                GameObject GO = Singleton.Manager<ManHUD>.inst.GetHudElement(ManHUD.HUDElementType.Speedo).gameObject;
                //GameObject GO = Resources.FindObjectsOfTypeAll<GameObject>().Last(x => x.name == "HUD_AnchorTech_Button");

                if (GO)
                {
                    //Debug_TTExt.Log("Search " + Nuterra.NativeOptions.UIUtilities.GetComponentTree(GO, " - "));
                    if (!GO.transform.parent)
                        throw new NullReferenceException("ManAbilities.InitAbilityBar() - GO.transform.parent null");
                    var trans = UnityEngine.Object.Instantiate(GO.transform, GO.transform.parent);
                    /*
                    if (!trans.transform.parent)
                        throw new NullReferenceException("InitWiki() - trans.transform.parent null");
                    */

                    var lockedText = trans.GetComponentInChildren<UILocalisedText>(true);
                    if (lockedText == null)
                        throw new NullReferenceException("ManAbilities.InitAbilityBar() - lockedText null");
                    UnityEngine.Object.Destroy(lockedText);
                    var speedText = trans.GetComponentInChildren<UISpeedo>(true);
                    if (speedText == null)
                        throw new NullReferenceException("ManAbilities.InitAbilityBar() - speedText null");
                    UnityEngine.Object.Destroy(speedText);

                    var tooltips = trans.GetComponentsInChildren<TooltipComponent>(true);
                    if (tooltips == null || tooltips.Length == 0)
                        throw new NullReferenceException("ManAbilities.InitAbilityBar() - tooltip null or empty");
                    foreach (var tooltip in tooltips)
                    {
                        ManToolbar.textSet.SetValue(tooltip, false);
                        tooltip.SetText("Abilities");
                    }

                    var Texts = trans.GetComponentsInChildren<Text>(true);
                    if (Texts == null || Texts.Length == 0)
                        throw new NullReferenceException("ManAbilities.InitAbilityBar() - Texts null or empty");
                    foreach (var TextCase in Texts)
                    {
                        TextCase.text = "Abilities";
                    }
                    var toRem = trans.HeavyTransformSearch("Speedo_Text");
                    if (toRem == null)
                        throw new NullReferenceException("ManAbilities.InitAbilityBar() - Speedo_Text null");
                    UnityEngine.Object.Destroy(toRem.gameObject);

                    /*
                    var images = trans.GetComponentsInChildren<Image>(true);
                    if (images == null || images.Length == 0)
                        throw new NullReferenceException("ManAbilities.InitAbilityBar() - images null or empty");
                    foreach (var image in images)
                    {
                        image.sprite = ManIngameWiki.BlocksSprite;
                    }*/


                    RectTransform rectT = trans.GetComponent<RectTransform>();
                    if (!rectT)
                        throw new NullReferenceException("ManAbilities.InitAbilityBar() - rectTrans null");
                    Vector3 ver = rectT.anchoredPosition3D;
                    ver.x = ver.x + 40;
                    rectT.anchoredPosition3D = ver;

                    inst = rectT.gameObject;
                    Debug_TTExt.Log("ManAbilities.InitAbilityBar() - Prefab Init");
                    inst.SetActive(true);
                }
                else
                    Debug_TTExt.Assert("ManAbilities.InitAbilityBar()  - UI FAILED to init!!! (GO null)");
                if (!prev)
                    Singleton.Manager<ManHUD>.inst.HideHudElement(ManHUD.HUDElementType.Speedo);
            }
            catch (Exception e)
            {
                Debug_TTExt.Log("ManAbilities.InitAbilityBar() - UI Ability Bar FAILED to init!!! - " + e);
            }
        }

        private static Toggle MakePrefabToggle(string name, Sprite iconSprite, UnityAction<bool> callback)
        {
            var ob = ManToolbar.MakePrefabToggle(name, iconSprite, callback, inst.transform);
            RectTransform RT = ob.GetComponent<RectTransform>();
            RT.anchoredPosition = RT.anchoredPosition + new Vector2(0, 40);
            foreach (var item in ob.GetComponentsInChildren<Image>(true))
            {
                item.fillMethod = Image.FillMethod.Radial360;
                item.fillOrigin = (int)item.fillMethod;
                item.fillClockwise = true;
                item.fillCenter = false;
                item.fillAmount = 1;
            }
            return ob;
        }
        private static Button MakePrefabButton(string name, Sprite iconSprite, UnityAction callback)
        {
            var ob = ManToolbar.MakePrefabButton(name, iconSprite, callback, inst.transform);
            RectTransform RT = ob.GetComponent<RectTransform>();
            RT.anchoredPosition = RT.anchoredPosition + new Vector2(0, 40);
            foreach (var item in ob.GetComponentsInChildren<Image>(true))
            {
                item.fillMethod = Image.FillMethod.Radial360;
                item.fillOrigin = (int)item.fillMethod;
                item.fillClockwise = true;
                item.fillCenter = false;
                item.fillAmount = 1;
            }
            return ob;
        }
    }
}
