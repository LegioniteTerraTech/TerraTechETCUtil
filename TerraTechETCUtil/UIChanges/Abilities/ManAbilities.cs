using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static LocalisationEnums;
using static ModeAttract;

namespace TerraTechETCUtil
{
    public class ManAbilities
    {
        internal static List<AbilityElement> updating = new List<AbilityElement>();

        internal static bool ready = false;
        internal static bool dirtyBar = false;
        internal static GameObject inst;
        internal static Queue<AbilityElement> queuedCreate = null;
        internal static List<AbilityElement> Ready = null;
        internal static List<AbilityElement> Shown = null;
        internal static AbilityButton NextPageButton = null;

        public static KeyCode AbilityTogglePage = KeyCode.Alpha1;
        public static KeyCode ability1 = KeyCode.Alpha2;
        public static KeyCode ability2 = KeyCode.Alpha3;
        public static KeyCode ability3 = KeyCode.Alpha4;
        public static KeyCode ability4 = KeyCode.Alpha5;

        internal static void InitElement(AbilityElement add)
        {
            InitAbilityBar();
            if (queuedCreate == null)
            {
                queuedCreate = new Queue<AbilityElement>();
                Ready = new List<AbilityElement>();
                Shown = new List<AbilityElement>();
                ready = ManGameMode.inst.GetModePhase() == ManGameMode.GameState.InGame;
                if (ready)
                    ManGameMode.inst.ModeCleanUpEvent.Subscribe(NotReady);
                else
                    ManGameMode.inst.ModeStartEvent.Subscribe(TrySetup);
            }
            if (ready)
                add.Initiate();
            else
                queuedCreate.Enqueue(add);
        }
        private static LocExtStringMod abilitiesName = new LocExtStringMod(new Dictionary<Languages, string>
            {{ Languages.US_English, "Abilities" },
            {Languages.Japanese, "アビリティー" }});
        private static LocExtStringMod wikiNextPageName = new LocExtStringMod(new Dictionary<Languages, string>
            {{ Languages.US_English, "Next Page" },
            {Languages.Japanese, "次のページ" }});
        internal static void TrySetup(Mode ignor)
        {
            try
            {
                if (!ManGameMode.inst.GetIsInPlayableMode())
                    return;
                while (queuedCreate.Any())
                {
                    queuedCreate.Dequeue().Initiate();
                }
                ManGameMode.inst.ModeStartEvent.Subscribe(TrySetup);
                ManGameMode.inst.ModeCleanUpEvent.Unsubscribe(NotReady);
                ManUpdate.inst.AddAction(ManUpdate.Type.Update, ManUpdate.Order.First, UpdateThis, 9001);
                NextPageButton = new AbilityButton(wikiNextPageName,
                    ResourcesHelper.GetTexture2DFromBaseGameAllFast("ArrowRight").ConvertToSprite(), NextPage, 0f);
                NextPageButton.Hide();
                ready = true;
            }
            catch (Exception e)
            {
                Debug_TTExt.Log("ManAbilities.TrySetup) ~ error - " + e);
            }
        }
        internal static void NotReady(Mode ignor)
        {
            ready = false;
            ManUpdate.inst.RemoveAction(ManUpdate.Type.Update, ManUpdate.Order.First, UpdateThis);
            ManGameMode.inst.ModeCleanUpEvent.Subscribe(NotReady);
            ManGameMode.inst.ModeStartEvent.Unsubscribe(TrySetup);
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
                    lockedText.m_String = abilitiesName.CreateNewLocalisedString();
                    //UnityEngine.Object.Destroy(lockedText);
                    var speedText = trans.GetComponentInChildren<UISpeedo>(true);
                    if (speedText == null)
                        throw new NullReferenceException("ManAbilities.InitAbilityBar() - speedText null");
                    UnityEngine.Object.Destroy(speedText);

                    var tooltips = trans.GetComponentsInChildren<TooltipComponent>(true);
                    if (tooltips == null || tooltips.Length == 0)
                        throw new NullReferenceException("ManAbilities.InitAbilityBar() - tooltip null or empty");
                    foreach (var tooltip in tooltips)
                    {
                        abilitiesName.SetTextAuto(tooltip);
                    }

                    var Texts = trans.GetComponentsInChildren<Text>(true);
                    if (Texts == null || Texts.Length == 0)
                        throw new NullReferenceException("ManAbilities.InitAbilityBar() - Texts null or empty");
                    foreach (var TextCase in Texts)
                    {
                        TextCase.text = abilitiesName.GetEnglish();
                    }
                    lockedText.UpdateText();
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
                    inst.SetActive(false);
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

        private static int shownIndex = 0;
        private static bool NextPageButtonActive = false;
        private static void ShowNextPageButton(bool state)
        {
            if (NextPageButtonActive != state)
            {
                NextPageButtonActive = state;
                if (state)
                {
                    NextPageButton.Show();
                    Shown.Remove(NextPageButton);
                }
                else
                {
                    NextPageButton.Hide();
                }
            }
        }
        private static void UpdateThis()
        {
            if (Shown.Count > shownIndex)
            {
                AbilityElement AE = Shown[shownIndex];
                if (AE is AbilityToggle)
                {
                    if (Input.GetKeyDown(ability1))
                        AE.TriggerNow();
                }
                else if (Input.GetKey(ability1))
                    AE.TriggerNow();
            }
            if (Shown.Count > shownIndex + 1)
            {
                AbilityElement AE = Shown[shownIndex + 1];
                if (AE is AbilityToggle)
                {
                    if (Input.GetKeyDown(ability2))
                        AE.TriggerNow();
                }
                else if (Input.GetKey(ability2))
                    AE.TriggerNow();
            }
            if (Shown.Count > shownIndex + 2)
            {
                AbilityElement AE = Shown[shownIndex + 2];
                if (AE is AbilityToggle)
                {
                    if (Input.GetKeyDown(ability3))
                        AE.TriggerNow();
                }
                else if (Input.GetKey(ability3))
                    AE.TriggerNow();
            }
            if (Shown.Count > shownIndex + 3)
            {
                AbilityElement AE = Shown[shownIndex + 3];
                if (AE is AbilityToggle)
                {
                    if (Input.GetKeyDown(ability4))
                        AE.TriggerNow();
                }
                else if (Input.GetKey(ability4))
                    AE.TriggerNow();
            }

            if (Input.GetKey(AbilityTogglePage))
                NextPageButton.TriggerNow();
        }
        private static void NextPage() 
        {
            if (shownIndex > Shown.Count - 4)
            {
                shownIndex = 0;
            }
            else if (shownIndex > Shown.Count - 3)
                shownIndex = Shown.Count - 4;
            else
                shownIndex += 4;
            RefreshPage();
        }
        internal static void RefreshPage()
        {
            ShowNextPageButton(Shown.Count > 4);

            foreach (var shown in Shown)
            {
                shown.ShowInUI_Internal(false);
            }
            for (int i = shownIndex; i < shownIndex + 3; i++)
            {
                Shown[i].ShowInUI_Internal(true);
            }
        }

        internal static Toggle MakePrefabToggle(LocExtStringMod name, Sprite iconSprite, UnityAction<bool> callback)
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
            ob.gameObject.SetActive(false);
            return ob;
        }
        internal static Toggle MakePrefabToggle(string name, Sprite iconSprite, UnityAction<bool> callback)
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
            ob.gameObject.SetActive(false);
            return ob;
        }
        internal static Button MakePrefabButton(LocExtStringMod name, Sprite iconSprite, UnityAction callback)
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
            ob.gameObject.SetActive(false);
            return ob;
        }
        internal static Button MakePrefabButton(string name, Sprite iconSprite, UnityAction callback)
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
            ob.gameObject.SetActive(false);
            return ob;
        }
    }
}
