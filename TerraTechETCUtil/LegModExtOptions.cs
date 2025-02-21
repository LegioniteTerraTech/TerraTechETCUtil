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
    internal class LegModExtOptions
    {
        public static ModConfig config;

        public static OptionKey WikiSettings;
        public static OptionToggle WikiHideFull;
        public static int wikiBind = (int)ManIngameWiki.WikiButtonKeybind;

        public static OptionKey Ability1;
        public static OptionKey Ability2;
        public static OptionKey Ability3;
        public static OptionKey Ability4;
        public static OptionKey AbilityPage;
        public static int abil1 = (int)ManAbilities.ability1;
        public static int abil2 = (int)ManAbilities.ability2;
        public static int abil3 = (int)ManAbilities.ability3;
        public static int abil4 = (int)ManAbilities.ability4;
        public static int abilPage = (int)ManAbilities.AbilityTogglePage;
        public static void InitOptionsAndConfig()
        {
            try
            {
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
            }
            catch (Exception e) { }
        }
    }
}
