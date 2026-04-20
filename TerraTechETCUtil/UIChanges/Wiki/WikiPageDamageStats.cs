using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using static LocalisationEnums;

namespace TerraTechETCUtil
{
    /// <inheritdoc cref="ManIngameWiki.WikiPage"/>
    /// <summary>
    /// <para>Wiki page for combat damage stat information</para>
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

        /// <summary>
        /// A custom damageable to display in <see cref="WikiPageDamageStats"/>
        /// </summary>
        public struct CustomDamageable
        {
            /// <summary>
            /// The icon to use
            /// </summary>
            public ManDamage.DamageableType icon;
            /// <summary>
            /// The calculation used for this damageable
            /// </summary>
            public Func<ManDamage.DamageType, KeyValuePair<float, string>> calc;
        }

        private static void InsureDamageLookup()
        {
            if (table == null)
                table = (DamageMultiplierTable)dmgtable.GetValue(ManDamage.inst);
        }
        /// <inheritdoc />
        protected override void OnBeforeDataRequested(bool getFullData) => InsureDamageLookup();
        /// <summary>
        /// Get the base game damage lookup table
        /// </summary>
        /// <param name="type"></param>
        /// <param name="dType"></param>
        /// <returns></returns>
        public static float GetDamageLookup(ManDamage.DamageType type, ManDamage.DamageableType dType)
        {
            InsureDamageLookup();
            return table.GetDamageMultiplier(type, dType);
        }
        /// <summary>
        /// Override and change the damage lookup table displayed on <see cref="WikiPageDamageStats"/>
        /// </summary>
        /// <param name="damageables"></param>
        /// <param name="damages"></param>
        /// <param name="table"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void OverrideDamageLookup(int damageables, int damages, float[] table)
        {
            if (table == null)
                throw new ArgumentNullException("float[] \"table\"");
            Damageables = damageables;
            Damages = damages;
        }
        /// <summary>
        /// Add a custom mod damageable (ModuleReinforced) to display here
        /// </summary>
        /// <param name="name"></param>
        /// <param name="icon"></param>
        /// <param name="calc"></param>
        /// <param name="tooltip">Tooptip to display on hover, overriding the damage assessment</param>
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

        internal WikiPageDamageStats(string modID, LocExtString hintTitle, Sprite icon, ManIngameWiki.WikiPageGroup group = null) :
            base(modID, hintTitle, icon, group)
        { }

        /// <inheritdoc/>
        public override void GetIcon() { }
        /// <inheritdoc/>
        public override void DisplaySidebar() => ButtonGUIDisp();
        /// <inheritdoc/>
        public override bool OnWikiClosedOrDeallocateMemory()
        {
            return false;
        }
        private const int heightTable = 60;
        /// <summary> Localisation text for this page </summary>
        public static LocExtStringMod LOC_DamageDesc = new LocExtStringMod(new Dictionary<Languages, string>
            {{ Languages.US_English, "Some blocks have resistances against certain attacks.\nThis is critical in the outcome of a battle!" },
            {Languages.Japanese, "一部のブロックは特定の攻撃に対して耐性を持っています。\nこれは戦闘において重要です！" }});
        /// <summary> Localisation text for this page </summary>
        public static LocExtStringMod LOC_DamageNorm = new LocExtStringMod(new Dictionary<Languages, string>
            {{ Languages.US_English, "Normal" },
            {Languages.Japanese, "通常ダメージ" }});
        /// <summary> Localisation text for this page </summary>
        public static LocExtStringMod LOC_DamageNone = new LocExtStringMod(new Dictionary<Languages, string>
            {{ Languages.US_English, "Ineffective" },
            {Languages.Japanese, "損傷なし" }});
        /// <summary> Localisation text for this page </summary>
        public static LocExtStringMod LOC_DamageWeak = new LocExtStringMod(new Dictionary<Languages, string>
            {{ Languages.US_English, "Weak" },
            {Languages.Japanese, "ダメージが低い" }});
        /// <summary> Localisation text for this page </summary>
        public static LocExtStringMod LOC_DamageStrong = new LocExtStringMod(new Dictionary<Languages, string>
            {{ Languages.US_English, "Strong" },
            {Languages.Japanese, "高ダメージ" }});
        /// <inheritdoc/>
        protected override void DisplayGUI()
        {
            GUILayout.Label(LOC_DamageDesc.ToString());
            GUILayout.BeginVertical(AltUI.TextfieldBorderedBlue);
            GUILayout.BeginHorizontal(AltUI.TextfieldBlackHuge, GUILayout.Height(heightTable));
            AltUI.Sprite(ManUI.inst.GetBlockCatIcon(BlockCategories.Weapons), GUILayout.Width(heightTable));
            for (int step = 0; step < Damages; step++)
            {
                AltUI.Sprite(ManUI.inst.GetDamageTypeIcon((ManDamage.DamageType)step));
                AltUI.Tooltip.GUITooltip(Localisation.inst.GetLocalisedString(
                    (LocalisationEnums.DamageTypeNames)step));
            }
            GUILayout.EndHorizontal();
            for (int y = 0; y < Damageables; y++)
            {
                GUILayout.BeginHorizontal(AltUI.TextfieldBlackHuge, GUILayout.Height(heightTable));
                AltUI.Sprite(ManUI.inst.GetDamageableTypeIcon((ManDamage.DamageableType)y), GUILayout.Width(heightTable));
                AltUI.Tooltip.GUITooltip(Localisation.inst.GetLocalisedString(
                    (LocalisationEnums.DamageableTypeNames)y));
                for (int x = 0; x < Damages; x++)
                {
                    float val = table.GetDamageMultiplier((ManDamage.DamageType)x, (ManDamage.DamageableType)y);
                    if (val == 1)
                    {
                        GUILayout.Label(val.ToString("0.00"), AltUI.ButtonBlue, GUILayout.ExpandHeight(true));
                        AltUI.Tooltip.GUITooltip(LOC_DamageNorm.ToString());
                    }
                    else if (val == 0)
                    {
                        GUILayout.Label("0.00", AltUI.ButtonGrey, GUILayout.ExpandHeight(true));
                        AltUI.Tooltip.GUITooltip(LOC_DamageNone.ToString());
                    }
                    else if (val < 1)
                    {
                        GUILayout.Label(val.ToString("0.00"), AltUI.ButtonRed, GUILayout.ExpandHeight(true));
                        AltUI.Tooltip.GUITooltip(LOC_DamageWeak.ToString());
                    }
                    else
                    {
                        GUILayout.Label(val.ToString("0.00"), AltUI.ButtonGreen, GUILayout.ExpandHeight(true));
                        AltUI.Tooltip.GUITooltip(LOC_DamageStrong.ToString());
                    }
                }
                GUILayout.EndHorizontal();
            }
            foreach (var item in CustomDamageableLookup)
            {
                GUILayout.BeginHorizontal(AltUI.TextfieldBlackHuge, GUILayout.Height(heightTable));
                AltUI.Sprite(ManUI.inst.GetDamageableTypeIcon(item.Value.icon),
                    GUILayout.Width(heightTable));
                AltUI.Tooltip.GUITooltip(item.Key);
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
                        AltUI.Tooltip.GUITooltip(tooltip);
                    }
                    else
                    {
                        if (multi == 1)
                        {
                            GUILayout.Label(multi.ToString("0.00"), AltUI.ButtonBlue, GUILayout.ExpandHeight(true));
                            AltUI.Tooltip.GUITooltip(LOC_DamageNorm.ToString());
                        }
                        else if (multi == 0)
                        {
                            GUILayout.Label("0.00", AltUI.ButtonGrey, GUILayout.ExpandHeight(true));
                            AltUI.Tooltip.GUITooltip(LOC_DamageNone.ToString());
                        }
                        else if (multi < 1)
                        {
                            GUILayout.Label(multi.ToString("0.00"), AltUI.ButtonRed, GUILayout.ExpandHeight(true));
                            AltUI.Tooltip.GUITooltip(LOC_DamageWeak.ToString());
                        }
                        else
                        {
                            GUILayout.Label(multi.ToString("0.00"), AltUI.ButtonGreen, GUILayout.ExpandHeight(true));
                            AltUI.Tooltip.GUITooltip(LOC_DamageStrong.ToString());
                        }
                    }
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }
    }
}
