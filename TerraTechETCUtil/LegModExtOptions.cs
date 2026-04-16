using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ModHelper;
using Nuterra.NativeOptions;
using UnityEngine;

namespace TerraTechETCUtil
{
    /// <summary>
    /// The options for <see cref="TerraTechETCUtil"/> shared content
    /// </summary>
    public class LegModExtOptions
    {
        internal static ModConfig config;
        private static bool init = false;

        internal static OptionKey WikiSettings;
        internal static OptionToggle WikiHideFull;
        /// <summary>
        /// The wiki keybind
        /// </summary>
        public static int wikiBind = (int)ManIngameWiki.WikiButtonKeybind;

        internal static OptionKey Ability1;
        internal static OptionKey Ability2;
        internal static OptionKey Ability3;
        internal static OptionKey Ability4;
        internal static OptionKey AbilityPage;
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
                if (init || WikiHideFull != null)
                    return;
                init = true;
                string modID = LegModExt.modID;
                ModConfig thisModConfig = new ModConfig(modID);
                thisModConfig.ReadConfigJsonFile();

                string mod = "Mod Wiki";
                OptionKey keyTestTemp = new OptionKey("In-Game Wiki Key", mod, ManIngameWiki.WikiButtonKeybind);
                keyTestTemp.onValueSaved.AddListener(() =>
                {
                    ManIngameWiki.WikiButtonKeybind = keyTestTemp.SavedValue;
                    wikiBind = (int)ManIngameWiki.WikiButtonKeybind;
                });
                WikiSettings = keyTestTemp;
                WikiHideFull = new OptionToggle("Hide Mod GUI Entirely When Mouse is Held", mod, ManModGUI.HideGUICompletelyWhenDragging);
                WikiHideFull.onValueSaved.AddListener(() =>
                {
                    ManModGUI.HideGUICompletelyWhenDragging = WikiHideFull.SavedValue;
                });

                ManAbilities.ability1 = (KeyCode)abil1;
                ManAbilities.ability2 = (KeyCode)abil2;
                ManAbilities.ability3 = (KeyCode)abil3;
                ManAbilities.ability4 = (KeyCode)abil4;
                ManAbilities.AbilityTogglePage = (KeyCode)abilPage;

                mod = "Abilities";
                Ability1 = new OptionKey("Ability 1", mod, ManAbilities.ability1);
                Ability1.onValueSaved.AddListener(() =>
                {
                    ManAbilities.ability1 = Ability1.SavedValue;
                    abil1 = (int)ManAbilities.ability1;
                });
                Ability2 = new OptionKey("Ability 2", mod, ManAbilities.ability2);
                Ability2.onValueSaved.AddListener(() =>
                {
                    ManAbilities.ability2 = Ability2.SavedValue;
                    abil2 = (int)ManAbilities.ability2;
                });
                Ability3 = new OptionKey("Ability 3", mod, ManAbilities.ability3);
                Ability3.onValueSaved.AddListener(() =>
                {
                    ManAbilities.ability3 = Ability3.SavedValue;
                    abil3 = (int)ManAbilities.ability3;
                });
                Ability4 = new OptionKey("Ability 4", mod, ManAbilities.ability4);
                Ability4.onValueSaved.AddListener(() =>
                {
                    ManAbilities.ability4 = Ability4.SavedValue;
                    abil4 = (int)ManAbilities.ability4;
                });
                AbilityPage = new OptionKey("Next Page", mod, ManAbilities.AbilityTogglePage);
                AbilityPage.onValueSaved.AddListener(() =>
                {
                    ManAbilities.AbilityTogglePage = AbilityPage.SavedValue;
                    abilPage = (int)ManAbilities.AbilityTogglePage;
                });

                thisModConfig.BindConfig<LegModExtOptions>(null, "wikiBind");
                thisModConfig.BindConfig<ManModGUI>(null, "HideGUICompletelyWhenDragging");
                thisModConfig.BindConfig<LegModExtOptions>(null, "abil1");
                thisModConfig.BindConfig<LegModExtOptions>(null, "abil2");
                thisModConfig.BindConfig<LegModExtOptions>(null, "abil3");
                thisModConfig.BindConfig<LegModExtOptions>(null, "abil4");
                thisModConfig.BindConfig<LegModExtOptions>(null, "abilPage");

                NativeOptionsMod.onOptionsSaved.AddListener(() => { thisModConfig.WriteConfigJsonFile(); });
                config = thisModConfig;
                Debug_TTExt.Log("TerraTechETCUtil: Init LegModExtOptions");
            }
            catch { }
        }
    }
}
