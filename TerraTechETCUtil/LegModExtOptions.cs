using System;
using UnityEngine;

namespace TerraTechETCUtil
{
    /// <summary>
    /// The options for <see cref="TerraTechETCUtil"/> shared content
    /// </summary>
    public class LegModExtOptions
    {
        internal static ModHelper.ModConfig config;
        private static bool init = false;


        // -----------------------  INGAME WIKI  -----------------------  
        internal static Nuterra.NativeOptions.OptionKey WikiSettings;
        /// <summary>
        /// The wiki keybind
        /// </summary>
        public static int wikiBind = (int)ManIngameWiki.WikiButtonKeybind;
        internal static Nuterra.NativeOptions.OptionRange WikiRescaler;
        internal static Nuterra.NativeOptions.OptionRange WikiScaler;


        // -----------------------  MOD GUI  -----------------------  
        internal static Nuterra.NativeOptions.OptionToggle HideDragFull;
        internal static Nuterra.NativeOptions.OptionRange GUIRescaler;
        internal static Nuterra.NativeOptions.OptionRange GUIScaler;


        // -----------------------  ABILITIES  -----------------------  
        internal static Nuterra.NativeOptions.OptionKey Ability1;
        internal static Nuterra.NativeOptions.OptionKey Ability2;
        internal static Nuterra.NativeOptions.OptionKey Ability3;
        internal static Nuterra.NativeOptions.OptionKey Ability4;
        internal static Nuterra.NativeOptions.OptionKey AbilityPage;
        /// <summary>
        /// Ability hotkey to trigger in the <see cref="ManAbilities"/> hotbar
        /// </summary>
        public static int abil1 = (int)ManAbilities.ability1;
        /// <summary>
        /// Ability hotkey to trigger in the <see cref="ManAbilities"/> hotbar
        /// </summary>
        public static int abil2 = (int)ManAbilities.ability2;
        /// <summary>
        /// Ability hotkey to trigger in the <see cref="ManAbilities"/> hotbar
        /// </summary>
        public static int abil3 = (int)ManAbilities.ability3;
        /// <summary>
        /// Ability hotkey to trigger in the <see cref="ManAbilities"/> hotbar
        /// </summary>
        public static int abil4 = (int)ManAbilities.ability4;
        /// <summary>
        /// Ability hotkey to cycle to the next page in the <see cref="ManAbilities"/> hotbar
        /// </summary>
        public static int abilPage = (int)ManAbilities.AbilityTogglePage;
        internal static void InitOptionsAndConfig()
        {
            try
            {
                if (init || HideDragFull != null)
                    return;
                init = true;
                string modID = LegModExt.modID;
                ModHelper.ModConfig thisModConfig = new ModHelper.ModConfig(modID);

                thisModConfig.BindConfig<LegModExtOptions>(null, "wikiBind");
                ManIngameWiki.WikiButtonKeybind = (KeyCode)wikiBind;
                thisModConfig.BindConfig<ManModGUI>(null, "ModWikiGUIRescale");
                thisModConfig.BindConfig<ManModGUI>(null, "ModWikiGUIScale");

                thisModConfig.BindConfig<ManModGUI>(null, "HideGUICompletelyWhenDragging");
                thisModConfig.BindConfig<ManModGUI>(null, "GUIRescale");
                thisModConfig.BindConfig<ManModGUI>(null, "GUIScale");

                thisModConfig.BindConfig<LegModExtOptions>(null, "abil1");
                thisModConfig.BindConfig<LegModExtOptions>(null, "abil2");
                thisModConfig.BindConfig<LegModExtOptions>(null, "abil3");
                thisModConfig.BindConfig<LegModExtOptions>(null, "abil4");
                thisModConfig.BindConfig<LegModExtOptions>(null, "abilPage");


                string modWiki = "Mod Wiki";
                Nuterra.NativeOptions.OptionKey keyTestTemp = new Nuterra.NativeOptions.OptionKey("In-Game Wiki Key", modWiki, ManIngameWiki.WikiButtonKeybind);
                keyTestTemp.onValueSaved.AddListener(() =>
                {
                    ManIngameWiki.WikiButtonKeybind = keyTestTemp.SavedValue;
                    wikiBind = (int)ManIngameWiki.WikiButtonKeybind;
                });
                WikiSettings = keyTestTemp;
                WikiRescaler = SuperNativeOptions.OptionRangeAutoDisplay("Wiki Window Scaling", modWiki, ManModGUI.ModWikiGUIRescale,
                    0.25f, 1.15f, 0.05f, (inVal) => {
                        return inVal.ToString("P");
                    });
                WikiRescaler.onValueSaved.AddListener(() =>
                {
                    ManModGUI.ModWikiGUIRescale = WikiRescaler.SavedValue;
                    ManModGUI.ModWikiGUIScaler.SetWindowScale(ManModGUI.ModWikiGUIRescale);
                });
                ManModGUI.ModWikiGUIScaler.SetWindowScale(ManModGUI.ModWikiGUIRescale);
                WikiScaler = SuperNativeOptions.OptionRangeAutoDisplay("Wiki Contents Scaling", modWiki, ManModGUI.ModWikiGUIScale,
                    0.5f, 2.0f, 0.05f, (inVal) => {
                        return inVal.ToString("P");
                    });
                WikiScaler.onValueSaved.AddListener(() =>
                {
                    ManModGUI.ModWikiGUIScale = WikiScaler.SavedValue;
                    ManModGUI.ModWikiGUIScaler.SetUIScale(ManModGUI.ModWikiGUIScale);
                });
                ManModGUI.ModWikiGUIScaler.SetUIScale(ManModGUI.ModWikiGUIScale);

                string GUIControl = "Ingame Mod GUI";
                HideDragFull = new Nuterra.NativeOptions.OptionToggle("Hide Mod GUI Entirely When Mouse is Held", GUIControl, ManModGUI.HideGUICompletelyWhenDragging);
                HideDragFull.onValueSaved.AddListener(() =>
                {
                    ManModGUI.HideGUICompletelyWhenDragging = HideDragFull.SavedValue;
                });
                GUIRescaler = SuperNativeOptions.OptionRangeAutoDisplay("Mod GUI Window Scaling", GUIControl, ManModGUI.GUIRescale,
                    0.5f, 2.0f, 0.05f, (inVal) => {
                        return inVal.ToString("P");
                    });
                GUIRescaler.onValueSaved.AddListener(() =>
                {
                    ManModGUI.GUIRescale = GUIRescaler.SavedValue;
                });
                GUIScaler = SuperNativeOptions.OptionRangeAutoDisplay("Mod GUI Contents Scaling", GUIControl, ManModGUI.GUIScale,
                    0.5f, 2.0f, 0.05f, (inVal) => {
                        return inVal.ToString("P");
                    });
                GUIScaler.onValueSaved.AddListener(() =>
                {
                    ManModGUI.GUIScale = GUIScaler.SavedValue;
                });

                ManAbilities.ability1 = (KeyCode)abil1;
                ManAbilities.ability2 = (KeyCode)abil2;
                ManAbilities.ability3 = (KeyCode)abil3;
                ManAbilities.ability4 = (KeyCode)abil4;
                ManAbilities.AbilityTogglePage = (KeyCode)abilPage;

                modWiki = "Abilities";
                Ability1 = new Nuterra.NativeOptions.OptionKey("Hotbar Ability 1", modWiki, ManAbilities.ability1);
                Ability1.onValueSaved.AddListener(() =>
                {
                    ManAbilities.ability1 = Ability1.SavedValue;
                    abil1 = (int)ManAbilities.ability1;
                });
                Ability2 = new Nuterra.NativeOptions.OptionKey("Hotbar Ability 2", modWiki, ManAbilities.ability2);
                Ability2.onValueSaved.AddListener(() =>
                {
                    ManAbilities.ability2 = Ability2.SavedValue;
                    abil2 = (int)ManAbilities.ability2;
                });
                Ability3 = new Nuterra.NativeOptions.OptionKey("Hotbar Ability 3", modWiki, ManAbilities.ability3);
                Ability3.onValueSaved.AddListener(() =>
                {
                    ManAbilities.ability3 = Ability3.SavedValue;
                    abil3 = (int)ManAbilities.ability3;
                });
                Ability4 = new Nuterra.NativeOptions.OptionKey("Hotbar Ability 4", modWiki, ManAbilities.ability4);
                Ability4.onValueSaved.AddListener(() =>
                {
                    ManAbilities.ability4 = Ability4.SavedValue;
                    abil4 = (int)ManAbilities.ability4;
                });
                AbilityPage = new Nuterra.NativeOptions.OptionKey("Hotbar Next Page", modWiki, ManAbilities.AbilityTogglePage);
                AbilityPage.onValueSaved.AddListener(() =>
                {
                    ManAbilities.AbilityTogglePage = AbilityPage.SavedValue;
                    abilPage = (int)ManAbilities.AbilityTogglePage;
                });


                Nuterra.NativeOptions.NativeOptionsMod.onOptionsSaved.AddListener(() => { thisModConfig.WriteConfigJsonFile(); });
                config = thisModConfig;
                Debug_TTExt.Log("TerraTechETCUtil: Init LegModExtOptions");
            }
            catch { }
        }
    }
}
