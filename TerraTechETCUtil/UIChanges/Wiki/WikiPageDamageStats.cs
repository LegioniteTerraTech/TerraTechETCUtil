using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace TerraTechETCUtil
{
    /// <summary>
    /// Displays various stats
    /// </summary>
    public class WikiPageDamageStats : ManIngameWiki.WikiPage
    {
        private static FieldInfo dmgtable = typeof(ManDamage).GetField("m_DamageMultiplierTable", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo dmglookup = typeof(DamageMultiplierTable).GetField("m_DamageTypeMultiplierLookup", BindingFlags.NonPublic | BindingFlags.Instance);

        private static int Damageables = ManDamage.NumDamageableTypes;
        private static int Damages = ManDamage.NumDamageTypes;
        private static DamageMultiplierTable table;
        private static Dictionary<string, CustomDamageable> CustomDamageableLookup = 
            new Dictionary<string, CustomDamageable>();

        public struct CustomDamageable
        {
            public ManDamage.DamageableType icon;
            public Func<ManDamage.DamageType, KeyValuePair<float, string>> calc;
        }

        private static void InsureDamageLookup()
        {
            if (table == null)
                table = (DamageMultiplierTable)dmgtable.GetValue(ManDamage.inst);
        }
        public static float GetDamageLookup(ManDamage.DamageType type, ManDamage.DamageableType dType)
        {
            InsureDamageLookup();
            return table.GetDamageMultiplier(type, dType);
        }
        public static void OverrideDamageLookup(int damageables, int damages, float[] table)
        {
            if (table == null)
                throw new ArgumentNullException("float[] \"table\"");
            Damageables = damageables;
            Damages = damages;
        }
        public static void AddCustomDamageable(string name, ManDamage.DamageableType icon, 
            Func<ManDamage.DamageType, KeyValuePair<float, string>> calc, string tooltip = null)
        {
            if (!CustomDamageableLookup.ContainsKey(name))
                CustomDamageableLookup.Add(name, new CustomDamageable
                {
                    icon = icon,
                    calc = calc,
                });
        }
        internal static void ResetAllCustomDamageables()
        {
            CustomDamageableLookup.Clear();
        }


        internal WikiPageDamageStats(string modID, string hintTitle, Sprite icon, ManIngameWiki.WikiPageGroup group = null) :
            base(modID, hintTitle, icon, group)
        { }

        public override void GetIcon() { }
        public override void DisplaySidebar() => ButtonGUIDisp();
        public override bool ReleaseAsMuchAsPossible()
        {
            return false;
        }
        private const int heightTable = 60;
        public override void DisplayGUI()
        {
            InsureDamageLookup();
            GUILayout.Label("Some blocks have resistances against certain attacks.\nThis is critical in the outcome of a battle!");
            GUILayout.BeginVertical(AltUI.TextfieldBorderedBlue);
            GUILayout.BeginHorizontal(AltUI.TextfieldBlackHuge, GUILayout.Height(heightTable));
            AltUI.Sprite(ManUI.inst.GetBlockCatIcon(BlockCategories.Weapons), GUILayout.Width(heightTable));
            for (int step = 0; step < Damages; step++)
            {
                AltUI.Sprite(ManUI.inst.GetDamageTypeIcon((ManDamage.DamageType)step));
                ManIngameWiki.Tooltip.GUITooltip(Localisation.inst.GetLocalisedString(
                    (LocalisationEnums.DamageTypeNames)step));
            }
            GUILayout.EndHorizontal();
            for (int y = 0; y < Damageables; y++)
            {
                GUILayout.BeginHorizontal(AltUI.TextfieldBlackHuge, GUILayout.Height(heightTable));
                AltUI.Sprite(ManUI.inst.GetDamageableTypeIcon((ManDamage.DamageableType)y), GUILayout.Width(heightTable));
                ManIngameWiki.Tooltip.GUITooltip(Localisation.inst.GetLocalisedString(
                    (LocalisationEnums.DamageableTypeNames)y));
                for (int x = 0; x < Damages; x++)
                {
                    float val = table.GetDamageMultiplier((ManDamage.DamageType)x, (ManDamage.DamageableType)y);
                    if (val == 1)
                    {
                        GUILayout.Label(val.ToString("0.00"), AltUI.ButtonBlue, GUILayout.ExpandHeight(true));
                        ManIngameWiki.Tooltip.GUITooltip("Normal");
                    }
                    else if (val == 0)
                    {
                        GUILayout.Label("0.00", AltUI.ButtonGrey, GUILayout.ExpandHeight(true));
                        ManIngameWiki.Tooltip.GUITooltip("Ineffective");
                    }
                    else if (val < 1)
                    {
                        GUILayout.Label(val.ToString("0.00"), AltUI.ButtonRed, GUILayout.ExpandHeight(true));
                        ManIngameWiki.Tooltip.GUITooltip("Weak");
                    }
                    else
                    {
                        GUILayout.Label(val.ToString("0.00"), AltUI.ButtonGreen, GUILayout.ExpandHeight(true));
                        ManIngameWiki.Tooltip.GUITooltip("Strong");
                    }
                }
                GUILayout.EndHorizontal();
            }
            foreach (var item in CustomDamageableLookup)
            {
                GUILayout.BeginHorizontal(AltUI.TextfieldBlackHuge, GUILayout.Height(heightTable));
                AltUI.Sprite(ManUI.inst.GetDamageableTypeIcon(item.Value.icon),
                    GUILayout.Width(heightTable));
                ManIngameWiki.Tooltip.GUITooltip(item.Key);
                AltUI.AttachModWrenchIcon();
                for (int x = 0; x < Damages; x++)
                {
                    var val = item.Value.calc.Invoke((ManDamage.DamageType)x);
                    float multi = val.Key;
                    string tooltip = val.Value;
                    if (!tooltip.NullOrEmpty())
                    {
                        if (multi == 1)
                            GUILayout.Label(multi.ToString("0.00") + "*", AltUI.ButtonBlue, GUILayout.ExpandHeight(true));
                        else if (multi == 0)
                            GUILayout.Label("0.00" + "*", AltUI.ButtonGrey, GUILayout.ExpandHeight(true));
                        else if (multi < 1)
                            GUILayout.Label(multi.ToString("0.00") + "*", AltUI.ButtonRed, GUILayout.ExpandHeight(true));
                        else
                            GUILayout.Label(multi.ToString("0.00") + "*", AltUI.ButtonGreen, GUILayout.ExpandHeight(true));
                        ManIngameWiki.Tooltip.GUITooltip(tooltip);
                    }
                    else
                    {
                        if (multi == 1)
                        {
                            GUILayout.Label(multi.ToString("0.00"), AltUI.ButtonBlue, GUILayout.ExpandHeight(true));
                            ManIngameWiki.Tooltip.GUITooltip("Normal");
                        }
                        else if (multi == 0)
                        {
                            GUILayout.Label("0.00", AltUI.ButtonGrey, GUILayout.ExpandHeight(true));
                            ManIngameWiki.Tooltip.GUITooltip("Ineffective");
                        }
                        else if (multi < 1)
                        {
                            GUILayout.Label(multi.ToString("0.00"), AltUI.ButtonRed, GUILayout.ExpandHeight(true));
                            ManIngameWiki.Tooltip.GUITooltip("Weak");
                        }
                        else
                        {
                            GUILayout.Label(multi.ToString("0.00"), AltUI.ButtonGreen, GUILayout.ExpandHeight(true));
                            ManIngameWiki.Tooltip.GUITooltip("Strong");
                        }
                    }
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }
    }
}
