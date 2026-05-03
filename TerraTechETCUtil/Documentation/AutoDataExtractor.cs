using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Experimental.UIElements.StyleEnums;
using static ManSplashScreen;
using static NetController;

namespace TerraTechETCUtil
{
    /// <summary>
    /// Extracts fine data from a target Component. 
    /// <para>Use <see cref="AutoDataExtractorInst"/> to keep track of active instances</para>
    /// <para>Use <see cref="ModuleInfo"/> to keep track of block instances</para>
    /// <para>Use the <see cref="ManIngameWiki"/> system to access end-user information</para>
    /// </summary>
    public class AutoDataExtractor
    {
        internal static bool AdvancedBatchDisplayer(object toExplore, HashSet<string> openEntries, int depth, bool ignoreNextLayer = false)
        {
            if (toExplore is BlockRarity BR)
            {
                if (openEntries != null)
                {
                    GUILayout.Label(BR.ToString(), AltUI.LabelBlue);
                    GUILayout.Space(6);
                    AltUI.Sprite(ManUI.inst.GetBlockRarityIcon(BR),
                        AltUI.TextfieldBorderedBlue, GUILayout.Height(64), GUILayout.Width(64));
                }
                return true;
            }
            else if (toExplore is BlockCategories BC)
            {
                if (openEntries != null)
                {
                    GUILayout.Label(BC.ToString(), AltUI.LabelBlue);
                    GUILayout.Space(6);
                    AltUI.Sprite(ManUI.inst.GetBlockCatIcon(BC),
                        AltUI.TextfieldBorderedBlue, GUILayout.Height(64), GUILayout.Width(64));
                }
                return true;
            }
            else if (toExplore is ManDamage.DamageableType DAT)
            {
                if (openEntries != null)
                {
                    GUILayout.Label(DAT.ToString(), AltUI.LabelBlue);
                    GUILayout.Space(6);
                    if (AltUI.SpriteButton(ManUI.inst.GetDamageableTypeIcon(DAT),
                        AltUI.TextfieldBorderedBlue, GUILayout.Height(64), GUILayout.Width(64)))
                    {
                        ManIngameWiki.GetDamageStatsPage().GoHere();
                        ManSFX.inst.PlayUISFX(ManSFX.UISfxType.CheckBox);
                    }
                }
                return true;
            }
            else if (toExplore is ManDamage.DamageType DT)
            {
                if (openEntries != null)
                {
                    GUILayout.Label(DT.ToString(), AltUI.LabelBlue);
                    GUILayout.Space(6);
                    if (AltUI.SpriteButton(ManUI.inst.GetDamageTypeIcon(DT),
                        AltUI.TextfieldBorderedBlue, GUILayout.Height(64), GUILayout.Width(64)))
                    {
                        ManIngameWiki.GetDamageStatsPage().GoHere();
                        ManSFX.inst.PlayUISFX(ManSFX.UISfxType.CheckBox);
                    }
                }
                return true;
            }//*
            else if (toExplore is Transform trans)
            {
                if (!ignoreNextLayer)
                {
                    if (openEntries == null)
                        return trans.GetComponents<Component>().Any(x => AdvancedBatchDisplayer(x, null, depth + 1, true));
                    else
                    {
                        foreach (Component c in trans.GetComponents<Component>())
                            AdvancedBatchDisplayer(c, openEntries, depth + 1, true);
                        return true;
                    }
                }
                else
                    return false;
            }//*/
            else if (toExplore is WeaponRound WR)
            {
                if (openEntries != null)
                    DisplayInfoOnWeaponRound(WR, openEntries, depth + 1);
                return true;
            }
            else if (toExplore is Projectile proj)
            {
                if (openEntries != null)
                    DisplayInfoOnWeaponRound(proj, openEntries, depth + 1);
                return true;
            }
            else if (toExplore is ShotgunRound SR)
            {
                if (openEntries != null)
                    DisplayInfoOnWeaponRound(SR, openEntries, depth + 1);
                return true;
            }
            else if (toExplore is Explosion explo)
            {
                if (openEntries != null)
                    DisplayInfoOnExplosion(explo, openEntries, depth + 1);
                return true;
            }
            return EnumDisplayer<TechAudio.SFXType>(toExplore, openEntries != null);
        }
        private static void DisplayInfoOnWeaponRound(WeaponRound WR, HashSet<string> openEntries, int depth)
        {
            GUILayout.EndHorizontal();
            GUILayout.BeginVertical(AltUI.BoxBlack);
            if (WR == null)
            {
                GUILayout.Label("Weapon Round: None", AltUI.LabelGoldTitle);
            }
            else
            {
                string nameFiltered = "Weapon Round" + depth;
                bool isOpen = openEntries.Contains(nameFiltered);
                if (GUILayout.Button("Weapon Round", isOpen ? AltUI.LabelBlueTitle : AltUI.LabelGoldTitle,
                    GUILayout.ExpandWidth(true)))
                {
                    ManSFX.inst.PlayUISFX(ManSFX.UISfxType.CheckBox);
                    if (isOpen)
                        openEntries.Remove(nameFiltered);
                    else
                        openEntries.Add(nameFiltered);
                }
                if (isOpen)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Projectile Type", AltUI.LabelWhite);
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(WR.GetType().ToString(), AltUI.LabelBlue);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    AdvancedBatchDisplayer(WR.DamageType, openEntries, depth + 1, true);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Damage", AltUI.LabelWhite);
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(WR.Damage.ToString(), AltUI.LabelBlue);
                    GUILayout.EndHorizontal();

                    if (WR is Projectile proj)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Lifespan", AltUI.LabelWhite);
                        GUILayout.FlexibleSpace();
                        GUILayout.Label((string)DisplaySec((float)ManExtProj.deathTimerProj.GetValue(proj)), AltUI.LabelBlue);
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Is Sticky", AltUI.LabelWhite);
                        GUILayout.FlexibleSpace();
                        GUILayout.Label(((bool)ManExtProj.stickyProj.GetValue(proj)) ? trueString : falseString, AltUI.LabelBlue);
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Is Affected By Gravity", AltUI.LabelWhite);
                        GUILayout.FlexibleSpace();
                        GUILayout.Label(((bool)ManExtProj.gravityAffectProj.GetValue(proj)) ? trueString : falseString, AltUI.LabelBlue);
                        GUILayout.EndHorizontal();

                        Explosion Explo = ((Transform)ManExtProj.explodeProj.GetValue(proj))?.GetComponent<Explosion>();
                        DisplayInfoOnExplosion(Explo, openEntries, depth + 1);
                    }
                }
            }
            GUILayout.EndVertical();
            GUILayout.BeginHorizontal();
        }
        private static void DisplayInfoOnExplosion(Explosion explo, HashSet<string> openEntries, int depth)
        {
            GUILayout.EndHorizontal();
            GUILayout.BeginVertical(AltUI.BoxBlack);
            if (explo == null)
            {
                GUILayout.Label("Explosion: None", AltUI.LabelGoldTitle);
            }
            else
            {
                string nameFiltered = "Explosion" + depth;
                bool isOpen = openEntries.Contains(nameFiltered);
                if (GUILayout.Button("Explosion", isOpen ? AltUI.LabelBlueTitle : AltUI.LabelGoldTitle,
                    GUILayout.ExpandWidth(true)))
                {
                    ManSFX.inst.PlayUISFX(ManSFX.UISfxType.CheckBox);
                    if (isOpen)
                        openEntries.Remove(nameFiltered);
                    else
                        openEntries.Add(nameFiltered);
                }
                if (isOpen)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Does Damage", AltUI.LabelWhite);
                    GUILayout.Space(6);
                    GUILayout.Label(explo.DoDamage ? trueString : falseString, AltUI.LabelBlue);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    AdvancedBatchDisplayer(ExplosionHelper.explodeType.GetValue(explo), openEntries, depth + 1);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Explosion Max Damage", AltUI.LabelWhite);
                    GUILayout.Space(6);
                    GUILayout.Label(explo.m_MaxDamageStrength.ToString(), AltUI.LabelBlue);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Explosion Max Force", AltUI.LabelWhite);
                    GUILayout.Space(6);
                    GUILayout.Label(explo.m_MaxImpulseStrength.ToString(), AltUI.LabelBlue);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Explosion Max Radius", AltUI.LabelWhite);
                    GUILayout.Space(6);
                    GUILayout.Label((string)DisplayDist(explo.m_EffectRadius), AltUI.LabelBlue);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Explosion Max Damage Radius", AltUI.LabelWhite);
                    GUILayout.Space(6);
                    GUILayout.Label((string)DisplayDist(explo.m_EffectRadiusMaxStrength), AltUI.LabelBlue);
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndVertical();
            GUILayout.BeginHorizontal();
        }

        internal static bool EnumDisplayer<T>(object toExplore, bool ActuallyDisplay) where T : Enum
        {
            if (ActuallyDisplay)
            {
                if (toExplore is T DT)
                {
                    GUILayout.Label(DT.ToString(), AltUI.LabelBlue);
                    return true;
                }
                return false;
            }
            else
            {
                return toExplore is T;
            }
        }
        internal static HashSet<Type> enumTypes = new HashSet<Type>() {
                    typeof(ManDamage.DamageableType),
                    typeof(ManWheels.TorqueParams),
                    typeof(ManWheels.WheelParams),
                    typeof(ModuleAnimator),
                    //typeof(ModuleAntenna),
                    typeof(ModuleHUDContextControlField),
                    typeof(ModulePlacementZoneEffect),
                };
        /// <summary>
        /// Module types to ignore when extracting data
        /// </summary>
        public static List<Type> ignoreModuleTypesExt = new List<Type>();


        private static HashSet<object> checkedAlready = new HashSet<object>();
        internal void Explorer<T>(Type type, T toExplore, HashSet<object> checkedAlready, 
            ref Dictionary<string, object> infos, bool baseLayer)
        {
            try
            {
                Explorer_InternalRecurse(type, toExplore, checkedAlready, ref infos);
            }
            finally
            {
                if (baseLayer)
                    checkedAlready.Clear();
            }
        }

        /// <summary>
        /// Called after <c>Explorer()</c>
        /// </summary>
        /// <param name="type"></param>
        /// <param name="FIs"></param>
        protected virtual void Explorer_InternalRecursePost(Type type, FieldInfo[] FIs) { }
        private void Explorer_InternalRecurse<T>(Type type, T toExplore, HashSet<object> checkedAlready, ref Dictionary<string, object> infos)
        {
            if (toExplore == null)
                return;
            FieldInfo[] FIs = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var item in FIs)
            {
                object itemGet;
                try
                {
                    if (!AutoDocumentator.LogStaticsAndBlocked && (item.IsStatic || item.Name.Contains("k__") || ignoreFieldTypes.Contains(item.FieldType) ||
                        (item.FieldType.IsGenericType && ignoreFieldTypes.Contains(item.FieldType.GetGenericTypeDefinition())) ||
                        ignoreFieldNames.Contains(item.Name))) //|| !item.GetCustomAttributes<SerializeField>().Any())
                    {
                        continue;
                    }

                    itemGet = item.GetValue(toExplore);
                    if (SpecialNamesFuncs.TryGetValue(item.Name, out var guiFunc))
                    {
                        infos.Add(guiFunc.Key, guiFunc.Value(itemGet));
                    }
                    else if (item.FieldType == typeof(int) || item.FieldType == typeof(float) ||
                        item.FieldType == typeof(bool) || item.FieldType == typeof(string))
                    {
                        if (SpecialNames.TryGetValue(item.Name, out string altName))
                            infos.Add(altName, itemGet);
                        else
                            infos.Add(item.Name.Replace("m_", "").SplitCamelCase(), itemGet);
                    }
                    else if (enumTypes.Contains(item.FieldType))
                    {
                        try
                        {
                            if (SpecialNames.TryGetValue(item.Name, out string altName))
                                infos.Add(altName, itemGet);
                            else
                                infos.Add(item.Name.Replace("m_", "").SplitCamelCase(), itemGet);
                        }
                        catch (Exception e)
                        {
                            Debug_TTExt.Log("Failed to cast " + item.FieldType + " as an enum - " + e);
                        }
                    }
                    else if (AdvancedBatchDisplayer(itemGet, null, 0))
                    {
                        if (SpecialNames.TryGetValue(item.Name, out string altName))
                            infos.Add(altName, itemGet);
                        else
                            infos.Add(item.Name.Replace("m_", "").SplitCamelCase(), itemGet);
                    }
                    else if (ModuleInfo.AllowedTypesUIWiki.Contains(item.FieldType))
                    {
                        if (checkedAlready.Contains(itemGet))
                            continue;
                        checkedAlready.Add(itemGet);

                        string nameToUse;
                        if (SpecialNames.TryGetValue(item.Name, out string altName))
                            nameToUse = altName;
                        else
                            nameToUse = item.Name.Replace("m_", "").SplitCamelCase();

                        if (itemGet == null)
                            infos.Add(nameToUse, AltUI.EnemyString("<b>NULL</b>"));
                        else if (item.FieldType.IsClass)
                        {
                            string nameToUse2;
                            if (SpecialNames.TryGetValue(itemGet.GetType().ToString(), out string altName2))
                                nameToUse2 = altName;
                            else
                                nameToUse2 = itemGet.GetType().ToString().Replace("m_", "").SplitCamelCase();
                            infos.Add(nameToUse, new AutoDataExtractor(nameToUse2,
                                itemGet.GetType(), itemGet, checkedAlready, false));
                        }
                        else
                            infos.Add(nameToUse, "Unknown type - not class?");
                        //Explorer_InternalRecurse(item.FieldType, itemGet, checkedAlready, ref infos);
                    }
                }
                catch (Exception e)
                {
                    if (item == null)
                        throw new Exception("Explorer_InternalRecurse<" + typeof(T).Name + ">() Failed on subject <NULL>, type <NULL>", e);
                    throw new Exception("Explorer_InternalRecurse<" + typeof(T).Name + ">() Failed on subject " + item.Name + ", type " + item.FieldType.ToString(), e);
                }
                Explorer_InternalRecursePost(type, FIs);
            }
        }

        /// <summary> Standardized unit to display in the wiki for respective method calls here </summary>
        public const string powerUnit = " kW";
        /// <summary> Standardized unit to display in the wiki for respective method calls here </summary>
        public const string distUnit = " m";
        /// <summary> Standardized unit to display in the wiki for respective method calls here </summary>
        public const string volumeUnit = distUnit + "^3";
        /// <summary> Standardized unit to display in the wiki for respective method calls here </summary>
        public const string thrustUnit = " kN";
        /// <summary> Standardized unit to display in the wiki for respective method calls here </summary>
        public const string weightUnit = " T";
        /// <summary> Standardized unit to display in the wiki for respective method calls here </summary>
        public const string secUnit = " sec";
        /// <summary> Standardized unit to display in the wiki for respective method calls here </summary>
        public const string degUnit = " Degrees";
        /// <summary> Standardized unit to display in the wiki for respective method calls here </summary>
        public const string degSecUnit = " Degrees/sec";

        private static readonly string trueString = AltUI.FriendlyString("True");
        private static readonly string falseString = AltUI.EnemyString("False");

        internal readonly string name;
        internal bool open = false;
        internal bool recipesOpen = false;
        internal Dictionary<string, object> infos = new Dictionary<string, object>();

        /// <summary>
        /// Creates a new AutoDataExtractor to extract and cache data about a target gameobject immedeately.
        /// <para>Does not instance information for later direct access. For that use <see cref="AutoDataExtractorInst"/> instead.</para>
        /// </summary>
        /// <param name="Name">What to name the target in JSON</param>
        /// <param name="grabbedType">The type this is targeting</param>
        /// <param name="prefab">The actual prefab to gather context from</param>
        /// <param name="grabbedAlready">For recursive calls, what has already been extracted to prevent duplicates</param>
        /// <param name="baseLayer">Do not touch this value, for telling it when to stop</param>
        public AutoDataExtractor(string Name, Type grabbedType, object prefab, HashSet<object> grabbedAlready,
            bool baseLayer = true)
        {
            name = Name;
            Explorer(grabbedType, prefab, grabbedAlready, ref infos, baseLayer);
        }

        /// <summary>
        /// The copy buffer for AutoDataExtractor
        /// </summary>
        public static StringBuilder clipboard = new StringBuilder();
        internal void DisplayGUI(HashSet<string> openEntries = null, int depth = 0)
        {
            if (openEntries == null)
                openEntries = ManIngameWiki.ModulesOpened;
            GUILayout.BeginVertical(AltUI.BoxBlack);
            string nameFiltered = (name.NullOrEmpty() ? "NULL NAME" : name);
            string nameFilteredID = nameFiltered + depth;
            bool isOpen = openEntries.Contains(nameFilteredID);
            if (GUILayout.Button(nameFiltered, isOpen ? AltUI.LabelBlueTitle : AltUI.LabelWhiteTitleBlueHover, 
                GUILayout.ExpandWidth(true)))
            {
                ManSFX.inst.PlayUISFX(ManSFX.UISfxType.CheckBox);
                if (isOpen)
                    openEntries.Remove(nameFilteredID);
                else
                    openEntries.Add(nameFilteredID);
            }
            if (isOpen)
            {
                if (infos == null)
                    GUILayout.Label("INFO IS NULL", AltUI.LabelRed);
                else
                {
                    foreach (var item in infos)
                    {
                        try
                        {
                            var val = item.Value;
                            if (item.Key == null || val == null)
                                continue;
                            GUILayout.BeginHorizontal(AltUI.TextfieldBlackSearch);
                            GUILayout.Label(item.Key, AltUI.LabelWhite);
                            GUILayout.FlexibleSpace();
                            if (val is Sprite spriteCase)
                                AltUI.Sprite(spriteCase, AltUI.TextfieldBorderedBlue, GUILayout.Height(64), GUILayout.Width(64));
                            else if (AdvancedBatchDisplayer(val, openEntries, depth + 1))
                            {
                            }
                            else if (val is AutoDataExtractor dispCase)
                                dispCase.DisplayGUI(openEntries, depth + 1);
                            else if (val is int intCase)
                                GUILayout.Label(intCase.ToString(), AltUI.LabelBlue);
                            else if (val is float floatCase)
                                GUILayout.Label(floatCase.ToString("F2"), AltUI.LabelBlue);
                            else if (val is bool bCase)
                                GUILayout.Label(bCase ? trueString : falseString, AltUI.LabelBlue);
                            else if (val is string sCase)
                                GUILayout.Label(sCase, AltUI.LabelWhite);
                            else if (val is List<string> sLCase)
                            {
                                recipesOpen = AltUI.ToggleLone(recipesOpen);
                                if (recipesOpen)
                                {
                                    GUILayout.BeginVertical(AltUI.TextfieldBordered);
                                    foreach (var item2 in sLCase)
                                    {
                                        GUILayout.Label(item2, AltUI.LabelBlackWrap);
                                    }
                                    GUILayout.EndVertical();
                                }
                            }
                            else if (val is ManIngameWiki.WikiLink linkCase)
                            {
                                if (linkCase.OnGUILarge(AltUI.ButtonBlue, AltUI.LabelWhite))
                                    linkCase.linked.GoHere();
                            }
                            else if (val is List<ManIngameWiki.WikiLink> linksCase)
                            {
                                if (linksCase.Count > 12)
                                {
                                    recipesOpen = AltUI.Toggle(recipesOpen, "Links");
                                    if (recipesOpen)
                                    {
                                        GUILayout.BeginVertical(AltUI.TextfieldBordered);
                                        foreach (var item2 in linksCase)
                                        {
                                            if (item2.OnGUI(AltUI.LabelBlue))
                                                item2.linked.GoHere();
                                        }
                                        GUILayout.EndVertical();
                                    }
                                }
                                else
                                {
                                    GUILayout.BeginHorizontal(AltUI.TextfieldBordered);
                                    foreach (var item2 in linksCase)
                                    {
                                        if (item2.OnGUI(AltUI.LabelBlue))
                                            item2.linked.GoHere();
                                    }
                                    GUILayout.EndHorizontal();
                                }
                            }
                            else if (val is List<KeyValuePair<int, ManIngameWiki.WikiLink>> linksCase2)
                            {
                                if (linksCase2.Count > 5)
                                {
                                    recipesOpen = AltUI.Toggle(recipesOpen, "Links");
                                    if (recipesOpen)
                                    {
                                        GUILayout.BeginVertical();
                                        foreach (var item2 in linksCase2)
                                        {
                                            GUILayout.BeginHorizontal();
                                            if (item2.Value.OnGUI(AltUI.LabelBlue))
                                                item2.Value.linked.GoHere();
                                            GUILayout.Label("x", AltUI.LabelWhite);
                                            GUILayout.Label(item2.Key.ToString(), AltUI.LabelBlue);
                                            GUILayout.EndHorizontal();
                                        }
                                        GUILayout.EndVertical();
                                    }
                                }
                                else
                                {
                                    foreach (var item2 in linksCase2)
                                    {
                                        if (item2.Value.OnGUI(AltUI.LabelBlue))
                                            item2.Value.linked.GoHere();
                                        GUILayout.Label("x", AltUI.LabelWhite);
                                        GUILayout.Label(item2.Key.ToString(), AltUI.LabelBlue);
                                        GUILayout.FlexibleSpace();
                                    }
                                }
                            }
                            GUILayout.EndHorizontal();
                        }
                        catch (Exception e)
                        {
                            throw new Exception("Exception in infos, item " + item.Key + " ~ ", e);
                        }
                    }
                }

                EndDetails();
            }
            GUILayout.EndVertical();
        }

        /// <summary>
        /// Displayed in the Wiki for more data and extraction for block JSONs.
        /// </summary>
        protected virtual void EndDetails() { }

        /// <summary>
        /// Class and Field names to change when displayed in the Wiki
        /// </summary>
        public static Dictionary<string, string> SpecialNames = new Dictionary<string, string>()
                {
                    { "TankBlock", "General"},
                    { "ResourceDispenser", "Scenery"},
                    { "ResourcePickup", "Chunk"},
                    { "Damageable", "Armoring"},
                    { "ModuleDamage", "Durability"},
                    { "ModuleAIBot", "AI Module"},
                    { "ModuleDriveBot", "Drive Bot"},
                    { "ModuleAnimatedMeleeWeapon", "Melee Weapon"},
                    { "ModuleDetachableLink", "Detachable Bolt"},
                    { "ModuleEnergyStore", "Energy Battery"},
                    { "ModuleItemConsume", "Item Consumer"},
                    { "ModuleItemProducer", "Item Producer"},
                    { "ModuleItemStore", "Item Storage"},
                    { "ModuleRadarMarker", "Radra Marker"},
                    { "ModuleShieldGenerator", "Shield Generator"},
                    { "ModuleTechController", "Tech Controller"},
                    { "ModuleVision", "Vision for Weapons"},
                    { "ModuleBlockStateController", "Block State Control Panel"},
                    { "ModuleCircuitReceiver", "Circuit Receiver"},
                    { "ModuleCircuitDispensor", "Circuit Dispensor"},
                    { "ModuleRemoteCharger", "Remote Charger"},
                    { "ModuleItemHolderBeam", "Item Holder Beam"},
                    //
                    //{ "m_Tier", "Corporation Grade"},
                    { "m_PreventSelfDestructOnFirstDetach", "Easy To Collect"},
                    //
                    { "m_DamageableType", "Armor Type"},
                    { "m_DamageType", "Damage Type"},
                    //
                    { "m_IsUsedOnCircuit", "Works with Circuits"},
                    { "m_CircuitControlSkipsChain", "Circuits Activate Immedeately"},

                    { "maxHealth", "Maximum Health"},
                    { "m_DamageDetachFragility", "Fragility"},
                    { "m_HealOnDetach", "Prevent Death On Detach"},

                    { "m_AoEDamageBlockPercent", "Shotgun Penetration Resistance"},

                    { "m_DontFireIfNotAimingAtTarget", "Only Fire When Aimed"},
                    { "m_LimitedShootAngle", "Restricted Upwards Aiming Cone"},
                    { "m_PreventShootingTowardsFloor", "Is Strictly AA"},
                    { "m_AutoFire", "Auto-Fire On Aim"},
                    { "m_ChangeTargetInteval", "Target Refresh Rate"},

                    { "m_ResetBurstOnInterrupt", "Reload Burst Immedeately When Idle"},
                    { "m_SeekingRounds", "Rounds Home In"},
                    { "m_NumCannonBarrels", "Number Of Barrels"},

                    { "m_AcceptRemoteCharge", "Wireless Chargeable"},
                    { "m_ProvideUnlimitedCharge", "Unlimited Power"},

                    { "m_Healing", "Can Heal Blocks"},
                    { "m_HealingHeartbeatInterval", "Healing Period"},
                    { "m_MaxHealingPerHeartbeat", "Distributed Health Healed"},
                    { "m_PowerUpDelay", "Startup Delay"},
                    { "m_ArcFiringInterval", "Charge Arc Cooldown"},
                    { "m_ChargeConeAngleDegrees", "Max Charging Angle"},

                    { "m_DisabledWhenAttachedToBlocks", "Emergeny Use Only"},

                    { "m_IsRetractedByDefault", "Starts Retracted"},

                    { "m_Capacity", "Maximum Capacity"},
                    { "m_SingleType", "Is Filtered Node Silo"},
                    { "m_IsOmniDirectionalStack", "Spherical Stack"},

                    { "m_PickUpRadiusAngle", "Limited Collection Cone"},

                    { "m_DropAfterMinTime", "Retry After Time"},
                    { "m_DropUnstuckAfterTime", "Retry Stuck After Time"},
                    { "m_DropOnDamageThreshold", "Max Damage Resistance"},

                    { "m_CollectionTimeout", "Dropped Off-Limits Duration"},

                    { "m_KickOutItem", "Ejects With Force"},
                    { "m_KickSpeed", "Ejection Force"},

                    { "m_RefillRate", "Refill Rate"},

                    { "m_MustBeOnGround", "Wheels Grounded To Work"},

                    { "m_UseBoostControls", "Activate on Boost Button"},
                    { "m_UseDriveControls", "Activate on Normal Controls"},
                    { "m_IsRocketBooster", "Heavy Thruster"},
                    { "m_EnablesThrottleControl", "Throttles Axis"},

                    { "m_ForceHorizontal", "Aligns with Gravity"},
                };


        private static StringBuilder SB = new StringBuilder();
        /// <summary> Standardized unit method to display in the wiki </summary>
        public static object DisplayBool(object valIn)
        {
            if ((bool)valIn)
                return trueString;
            else
                return falseString;
        }
        /// <summary> Standardized unit method to display in the wiki </summary>
        public static object DisplayDist(object valIn)
        {
            try
            {
                return AltUI.BlueStringHUD(((float)valIn).ToString("F2") + distUnit);
            }
            catch (Exception)
            {
                return AltUI.BlueStringHUD("Error");
            }
        }
        /// <summary> Standardized unit method to display in the wiki </summary>
        public static object DisplayVol(object valIn)
        {
            try
            {
                return AltUI.BlueStringHUD(((float)valIn).ToString("F2") + volumeUnit);
            }
            catch (Exception)
            {
                return AltUI.BlueStringHUD("Error");
            }
        }
        /// <summary> Standardized unit method to display in the wiki </summary>
        public static object DisplaykW(object valIn)
        {
            try
            {
                return AltUI.BlueStringHUD(((float)valIn).ToString("F2") + powerUnit);
            }
            catch (Exception)
            {
                return AltUI.BlueStringHUD("Error");
            }
        }
        /// <summary> Standardized unit method to display in the wiki </summary>
        public static object DisplayNewtons(object valIn)
        {
            try
            {
                return AltUI.BlueStringHUD(((float)valIn).ToString("F2") + thrustUnit);
            }
            catch (Exception)
            {
                return AltUI.BlueStringHUD("Error");
            }
        }
        /// <summary> Standardized unit method to display in the wiki </summary>
        public static object DisplayWeight(object valIn)
        {
            try
            {
                return AltUI.BlueStringHUD(((float)valIn).ToString("F2") + weightUnit);
            }
            catch (Exception)
            {
                return AltUI.BlueStringHUD("Error");
            }
        }
        /// <summary> Standardized unit method to display in the wiki </summary>
        public static object DisplaySec(object valIn)
        {
            try
            {
                return AltUI.BlueStringHUD(((float)valIn).ToString("F2") + secUnit);
            }
            catch (Exception)
            {
                return AltUI.BlueStringHUD("Error");
            }
        }
        /// <summary> Standardized unit method to display in the wiki </summary>
        public static object DisplayDeg(object valIn)
        {
            try
            {
                return AltUI.BlueStringHUD(((float)valIn).ToString("F2") + degUnit);
            }
            catch (Exception)
            {
                return AltUI.BlueStringHUD("Error");
            }
        }
        /// <summary> Standardized unit method to display in the wiki </summary>
        public static object DisplayDegSec(object valIn)
        {
            try
            {
                return AltUI.BlueStringHUD(((float)valIn).ToString("F2") + degSecUnit);
            }
            catch (Exception)
            {
                return AltUI.BlueStringHUD("Error");
            }
        }
        /// <summary> Standardized unit method to display in the wiki </summary>
        public static object DisplayDirection(object valIn)
        {
            try
            {
                Vector3 localSpaceHeading = (Vector3)valIn;
                if (localSpaceHeading.x > 0.5f)
                    return AltUI.BlueStringHUD("Right");
                else if (localSpaceHeading.x < -0.5f)
                    return AltUI.BlueStringHUD("Left");
                else if (localSpaceHeading.y > 0.5f)
                    return AltUI.BlueStringHUD("Upwards");
                else if (localSpaceHeading.y < -0.5f)
                    return AltUI.BlueStringHUD("Downwards");
                else if (localSpaceHeading.z > 0.5f)
                    return AltUI.BlueStringHUD("Forwards");
                else if (localSpaceHeading.z < -0.5f)
                    return AltUI.BlueStringHUD("Backwards");
                return AltUI.BlueStringHUD("Neutral");
            }
            catch (Exception)
            {
                return AltUI.BlueStringHUD("Error");
            }
        }

        /// <summary> Standardized unit method to display in the wiki </summary>
        public static object DisplayPercent(object valIn)
        {
            return AltUI.BuyString(((float)valIn).ToString("P"));
        }
        /// <summary> Standardized unit method to display in the wiki </summary>
        public static object DisplayPercentInv(object valIn)
        {
            if ((float)valIn <= 0)
                return AltUI.BuyString("0 %");
            return AltUI.BuyString((1 / (float)valIn).ToString("P"));
        }

        /// <summary> Standardized unit method to display in the wiki </summary>
        public static object DisplayCountOfRate(object numSec, string unit)
        {
            float count = (float)numSec;
            return DisplayCountOfCooldown(1f / count, unit);
        }
        /// <summary> Standardized unit method to display in the wiki </summary>
        public static object DisplayCountOfCooldown(object cooldownSec, string unit)
        {
            float count = (float)cooldownSec;
            if (count < 1f)
            {
                if (unit.NullOrEmpty())
                    return AltUI.BlueStringHUD((1f / count).ToString("0.00") + " per " + secUnit);
                return AltUI.BlueStringHUD((1f / count).ToString("0.00") + " " + unit + "/" + secUnit.Substring(1));
            }
            else
            {
                if (unit.NullOrEmpty())
                    return AltUI.BlueStringHUD("Every " + count.ToString("0.00") + secUnit);
                return AltUI.BlueStringHUD(count.ToString("0.00") + secUnit + " per " + unit);
            }
        }

        /// <summary> Standardized unit method to display in the wiki </summary>
        public static object DisplayProjectileDetails(object cooldownSec, string unit)
        {
            float count = (float)cooldownSec;
            if (count < 1f)
            {
                if (unit.NullOrEmpty())
                    return AltUI.BlueStringHUD((1f / count).ToString("0.00") + " per " + secUnit);
                return AltUI.BlueStringHUD((1f / count).ToString("0.00") + " " + unit + "/" + secUnit.Substring(1));
            }
            else
            {
                if (unit.NullOrEmpty())
                    return AltUI.BlueStringHUD("Every " + count.ToString("0.00") + secUnit);
                return AltUI.BlueStringHUD(count.ToString("0.00") + secUnit + " per " + unit);
            }
        }

        /*
        /// <summary> Standardized unit method to display in the wiki </summary>
        public static object DisplayExplosionDetails(object cooldownSec)
        {
            float count = (float)cooldownSec;
            if (count < 1f)
            {
                if (unit.NullOrEmpty())
                    return AltUI.BlueStringHUD((1f / count).ToString("0.00") + " per " + secUnit);
                return AltUI.BlueStringHUD((1f / count).ToString("0.00") + " " + unit + "/" + secUnit.Substring(1));
            }
            else
            {
                if (unit.NullOrEmpty())
                    return AltUI.BlueStringHUD("Every " + count.ToString("0.00") + secUnit);
                return AltUI.BlueStringHUD(count.ToString("0.00") + secUnit + " per " + unit);
            }
        }//*/

        private static object CountItems(object valIn)
        {
            if (valIn == null)
                return 0;
            var wrapped = (IEnumerable)valIn;
            int count = 0;
            foreach (var item in wrapped)
            {
                count++;
            }
            return count;
        }
        private static object CountItemsAPs(object valIn) => CountItems(valIn) + " AP(s)";
        private static object CountItemsVolume(object valIn) => CountItems(valIn) + volumeUnit;
        private static object GenerateListItems(object valIn)
        {
            List<string> doDisp = new List<string>();
            RecipeListWrapper[] wrapped = (RecipeListWrapper[])valIn;
            if (wrapped == null || !wrapped.Any())
                return "None";
            for (int step = 0; step < wrapped.Length; step++)
            {
                if (wrapped[step].target != null)
                {
                    foreach (var item2 in wrapped[step].target)
                    {
                        if (item2.m_OutputItems == null || !item2.m_OutputItems.Any())
                            continue;

                        string input = AltUI.HintString("Patience");
                        if (item2.m_InputItems != null && item2.m_InputItems.Any())
                        {
                            try
                            {
                                foreach (var item in item2.EnumerateInputs())
                                {
                                    ItemTypeInfo ITI = new ItemTypeInfo(item.ObjectType, item.ItemType);
                                    switch (item.ObjectType)
                                    {
                                        case ObjectTypes.Block:
                                            SB.Append(StringLookup.GetItemName(ITI) + ", ");
                                            break;
                                        case ObjectTypes.Chunk:
                                            SB.Append(AltUI.SideCharacterString(StringLookup.GetItemName(ITI)) + ", ");
                                            break;
                                    }
                                }
                                input = SB.ToString();
                            }
                            finally
                            {
                                SB.Clear();
                            }
                        }
                        switch (item2.m_OutputType)
                        {
                            case RecipeTable.Recipe.OutputType.Items:
                                try
                                {
                                    foreach (var item3 in item2.m_OutputItems)
                                    {
                                        if (item3.m_Quantity > 1)
                                            doDisp.Add(StringLookup.GetItemName(item3.m_Item) + " X " + item3.m_Quantity + ", ");
                                        else
                                            doDisp.Add(StringLookup.GetItemName(item3.m_Item) + ", ");
                                    }
                                    doDisp.Add(input + " => " + SB.ToString());
                                }
                                finally
                                {
                                    SB.Clear();
                                }
                                break;
                            case RecipeTable.Recipe.OutputType.Energy:
                                doDisp.Add(input + " => " + AltUI.BlueStringHUD(item2.m_EnergyOutput +
                                    (item2.m_EnergyType == TechEnergy.EnergyType.Electric ? " kW" : " L")));
                                break;
                            case RecipeTable.Recipe.OutputType.Money:
                                doDisp.Add(input + " => " + AltUI.BuyString(item2.m_MoneyOutput + " ¥¥"));
                                break;
                            default:
                                doDisp.Add(input + " => " + AltUI.EnemyString("Nothing"));
                                break;
                        }
                    }
                }
            }
            return doDisp;
        }
        private static object GenerateWorkingRanges(object valIn)
        {
            ModuleRadar.RadarScanType RangeSupport = (ModuleRadar.RadarScanType)valIn;
            try
            {
                string[] names = Enum.GetNames(typeof(ModuleRadar.RadarScanType));
                for (int step = 0; step < names.Length; step++)
                {
                    if ((RangeSupport & (ModuleRadar.RadarScanType)(1 << step)) > 0)
                    {
                        if (SB.Length > 0)
                            SB.Append(", ");
                        SB.Append(names[step]);
                    }
                }
                return SB.ToString();
            }
            finally
            {
                SB.Clear();
            }
        }
        private static object GenerateListRanges(object valIn)
        {
            List<string> doDisp = new List<string>();
            float[] Ranges = (float[])valIn;
            if (Ranges == null || !Ranges.Any())
                return "None";
            for (int step = 0; step < Ranges.Length; step++)
            {
                float range = Ranges[step];
                if (range <= 0)
                {
                    switch (step)
                    {
                        case 0://ModuleRadar.RadarScanType.Techs:
                            doDisp.Add(AltUI.BlueStringMsg("Techs") + " => " + AltUI.EnemyString("N/A"));
                            break;
                        case 1://ModuleRadar.RadarScanType.Resources:
                            doDisp.Add(AltUI.FriendlyString("Resources") + " => " + AltUI.EnemyString("N/A"));
                            break;
                        case 2://ModuleRadar.RadarScanType.Terrain:
                            doDisp.Add("Terrain" + " => " + AltUI.EnemyString("N/A"));
                            break;
                    }
                }
                else
                {
                    switch (step)
                    {
                        case 0://ModuleRadar.RadarScanType.Techs:
                            doDisp.Add(AltUI.BlueStringMsg("Techs") + " => " + range + distUnit);
                            break;
                        case 1://ModuleRadar.RadarScanType.Resources:
                            doDisp.Add(AltUI.FriendlyString("Resources") + " => " + range + distUnit);
                            break;
                        case 2://ModuleRadar.RadarScanType.Terrain:
                            doDisp.Add("Terrain" + " => " + range + distUnit);
                            break;
                    }
                }
            }
            return doDisp;
        }

        /// <summary>
        /// Specialized functions to display certain fields in the wiki
        /// </summary>
        public static Dictionary<string, KeyValuePair<string, Func<object, object>>> SpecialNamesFuncs =
            new Dictionary<string, KeyValuePair<string, Func<object, object>>>()
                {
                    { "m_DefaultMass", new KeyValuePair<string, Func<object, object>>(
                        "Mass", DisplayWeight)},
                    { "attachPoints", new KeyValuePair<string, Func<object, object>>(
                        "Attach Point Count", CountItemsAPs)},
                    { "filledCells", new KeyValuePair<string, Func<object, object>>(
                        "Volume", CountItemsVolume)},
                    /*
                    { "m_BlockCellBounds", new KeyValuePair<string, Func<object, object>>(
                        "Dimensions",  (x) => {
                        return ((Bounds)x).ToString();
                    } )},*/

                    { "m_AoEDamageBlockPercent", new KeyValuePair<string, Func<object, object>>(
                        "Shotgun Penetration Resistance", DisplayPercent)},

                    { "m_RotateSpeed", new KeyValuePair<string, Func<object, object>>("Aiming Speed", (x) => {
                        float rot = (float)x;
                        if (rot <= 0)
                            return "Does Not Rotate";
                        return DisplayDegSec(rot);
                    } )},

                    { "m_BurstShotCount", new KeyValuePair<string, Func<object, object>>("Shots Per Burst Fire", (x) => {
                        int rounds = (int)x;
                        if (rounds <= 0)
                            return "Does Not Burst-Fire";
                        return rounds.ToString() + " round(s)";
                    } )},
                    { "m_BurstCooldown", new KeyValuePair<string, Func<object, object>>(
                        "Burst Cooldown", (x) => {
                        float rounds = (float)x;
                        if (rounds <= 0)
                            return "Does Not Burst-Fire";
                        return DisplayCountOfCooldown(x, "burst(s)");
                    })},
                    { "m_ShotCooldown", new KeyValuePair<string, Func<object, object>>(
                        "Reload Time", (x) => DisplayCountOfCooldown(x, "round(s)"))},
                    { "m_CooldownVariancePct", new KeyValuePair<string, Func<object, object>>("Firing Stagger", DisplayPercent)},

                    { "m_PowerUsagePerArc", new KeyValuePair<string, Func<object, object>>("Power Use Per Shot", DisplaykW)},

                    { "m_OutputPerSecond", new KeyValuePair<string, Func<object, object>>("Generated Power Per Second", DisplaykW )},
                    //{ "m_Capacity", new KeyValuePair<string, Func<object, object>>("Maximum Power Stored", DisplaykW)},
                    { "m_Radius", new KeyValuePair<string, Func<object, object>>("Radius", DisplayDist)},
                    { "m_EnergyConsumptionPerSec", new KeyValuePair<string, Func<object, object>>("Idle Power Use", DisplaykW)},
                    { "m_InitialChargeEnergy", new KeyValuePair<string, Func<object, object>>("Startup Power Cost", DisplaykW)},
                    { "m_EnergyConsumedPerDamagePoint", new KeyValuePair<string, Func<object, object>>("Shield Efficiency", DisplayPercentInv)},
                    { "m_EnergyConsumedPerPointHealed", new KeyValuePair<string, Func<object, object>>("Repair Efficiency", DisplayPercentInv)},

                    { "m_ChargingRadius", new KeyValuePair<string, Func<object, object>>("Max Charging Radius", DisplayDist)},
                    { "m_PowerTransferPerArc", new KeyValuePair<string, Func<object, object>>("Max Power Transferred Per Arc", DisplaykW)},

                    { "m_RecipeLists", new KeyValuePair<string, Func<object, object>>("Craftable Recipes", GenerateListItems)},

                    { "m_ScanType", new KeyValuePair<string, Func<object, object>>("Radar Tracks", GenerateWorkingRanges)},
                    { "m_Ranges", new KeyValuePair<string, Func<object, object>>("Radar Ranges", GenerateListRanges)},
                    { "m_MiniMapType", new KeyValuePair<string, Func<object, object>>("Map Size", (x) => {
                        switch ((ManRadar.MiniMapType)x)
                        {
                            case ManRadar.MiniMapType.Compass:
                                return "Minimal";
                            case ManRadar.MiniMapType.ProximityRadar:
                                return "Standard";
                            case ManRadar.MiniMapType.MiniMap:
                                return "Detailed";
                            default:
                                return "Unknown";
                        }
                    } )},

                    { "m_ForcePerEffector", new KeyValuePair<string, Func<object, object>>("Thrust Applied Per Thruster", DisplayNewtons)},
                    { "m_PowerUseAtMaxThrottlePerSecond", new KeyValuePair<string, Func<object, object>>("Energy Use At Max Thrust", DisplaykW)},
                    { "m_Effectors", new KeyValuePair<string, Func<object, object>>("Thruster Count", CountItems)},

                    { "m_DeployBelowAltitude", new KeyValuePair<string, Func<object, object>>("Retract Above", DisplayDist)},
                    { "m_PickupRange", new KeyValuePair<string, Func<object, object>>("Collection Range", DisplayDist)},
                    { "m_Strength", new KeyValuePair<string, Func<object, object>>("Max Load Tolerence", DisplayNewtons)},
                    { "m_PowerDecayMassThreshold", new KeyValuePair<string, Func<object, object>>("Safe Mass Limit", DisplayWeight)},

                    { "m_BeamStrength", new KeyValuePair<string, Func<object, object>>("Radius", DisplayNewtons)},
                    { "m_HeightIncrementScale", new KeyValuePair<string, Func<object, object>>("Item Spacing", DisplayDist)},

                    { "m_DurationMultiplier", new KeyValuePair<string, Func<object, object>>("Effective Speed", DisplayPercentInv)},
                    { "m_EnergyMultiplier", new KeyValuePair<string, Func<object, object>>("Power Efficency", DisplayPercentInv)},

                    //{ "deathExplosion", new KeyValuePair<string, Func<object, object>>("Explosion On Death", DisplayExplosionDetails)},
                };
        internal static HashSet<Type> ignoreModuleTypes = new HashSet<Type>() {
                    typeof(ModuleBlockAttributes),
                    typeof(ModuleAnimator),
                    typeof(ModuleAntenna),
                    typeof(ModuleHUDContextControlField),
                    typeof(ModuleHUDContextControl_ColorPickerField),
                    typeof(ModuleHUDSliderControl),
                    typeof(ModulePlacementZoneEffect),
                    typeof(ModuleCircuitNode),
                    typeof(ModuleScriptedMenu),
                    typeof(ModulePlatformRestrictions),
                    typeof(ModuleAudioProvider),
                };

        internal static HashSet<Type> ignoreFieldTypes = new HashSet<Type>() {
                    typeof(EventNoParams),
                    typeof(Event<>),
                    typeof(Event<,>),
                    typeof(Event<,,>),
                    typeof(Event<,,,>),
                    typeof(Action),
                    typeof(Action<>),
                    typeof(Action<,>),
                    typeof(Action<,,>),
                    typeof(Action<,,,>),
                    typeof(Action<,,,,>),
                    typeof(Action<,,,,,>),
                    typeof(Action<,,,,,,>),
                    typeof(Action<,,,,,,,>),
                    typeof(Func<>),
                    typeof(Func<,>),
                    typeof(Func<,,>),
                    typeof(Func<,,,>),
                    typeof(Func<,,,,>),
                    typeof(Func<,,,,,>),
                    typeof(Func<,,,,,,>),
                    typeof(Func<,,,,,,,>),
        };
        /// <summary>
        /// Fields that shouldn't be displayed in the wiki or exported in the JSON as they are set at runtime
        /// </summary>
        public static HashSet<string> ignoreFieldNames = new HashSet<string>() {
                    "m_Tier",

                    "m_Priority",
                    "m_ShowOnHUD",
                    "m_IsPartOfTech",
                    "m_IsActive",
                    "m_HasPower",
                    "m_AnimatorController",

                    "m_ContextMenuForPlayerTechOnly",
                    "m_firstDetachment",
                    "m_CurrentMass",
                    "m_RecurseIndex",
                    "m_BlockPoolID",
                    "m_EnableTutorialCollision",
                    "m_IsTerrainOnly",
                    "m_HasRBodyInPrefab",
                    "m_SkinIndex",
                    "m_HasTriggerCatcher",
                    "m_AverageGravityScaleFactor",
                    "m_NeedsCentreOfGravityUpdate",
                    "m_AverageGravityScaleDirty",
                    "m_GravityAdjustmentTouched",
                    "m_GravityApplicationTouched",
                    "m_GravityAdjustmentTouched",
                    "m_GravityAdjustmentTouched",
                    "m_GravityAdjustmentTouched",
                    "m_GravityAdjustmentTouched",
                    "m_GravityAdjustmentTouched",

                    "m_OrigMaxHealth",
                    "destroyOnDeath",
                    "m_NextThreshold",
                    "m_InvulnerableEndTime",
                    "m_Invulnerable",
                    "m_HealthFixed",
                    "m_MaxHealthFixed",

                    "m_ExplodeCountdownTimer",
                    "m_HealOnDetachPercent",
                    "m_DamageDetachMeter",
                    "m_DamageDetachMeterTimestamp",

                    "m_RegisterWarningAfter",
                    "m_ResetFiringTAfterNotFiredFor",
                    "m_HasSpinUpDownAnim",
                    "m_HasCooldownAnim",
                    "m_CanInterruptSpinUpAnim",
                    "m_CanInterruptSpinDownAnim",
                    "m_SpinUpAnimLayerIndex",
                    "m_OverheatTime",
                    "m_OverheatPauseWindow",
                    "m_DisableMainAudioLoop",
                    "m_AudioLoopDelay",
                    "m_AnimatorSpinUpId",
                    "m_AnimatorCoolingDownId",
                    "m_AnimatorCooldownRemainingId",
                    "m_AnimatorOverheatedId",
                    "m_AnimatorOverheatId",
                    "m_ShotTimer",
                    "m_NextBarrelToFire",
                    "m_IdleTime",
                    "m_FailedToFireTime",
                    "m_BurstShotsRemaining",
                    "m_SpinUp",
                    "m_Overheat",
                    "m_OverheatPause",
                    "m_DetachTimeStamp",

                    "m_AccumulatedCharge",
                    "m_ArcTimer",
                    "m_ChargeTimer",
                    "m_ArcEffectIndex",
                    "m_ArcEffectVariant",
                    "m_SfxTimer",
                    "m_EnergyRefundAmount",
                    "m_ClientFiredArc",
                    "m_NextTargetUpdateTime",
                    "m_FireWarningShotsAtPlayer",
                    "m_WarningShotMinimumRange",
                    "m_checkLOS",

                    "m_DustMinimumRPM",
                    "m_SuspensionWarningWaitTime",
                    "m_UseTireTracks",
                    "m_Enabled",
                    "m_Animated",
                    "m_WarningWaitTimer",
                    "m_TireTrackWidth",
                    "m_TracksSkidSpeed",
                    "m_TracksMaxSkidSpeed",
                    "m_TracksMinSpeed",
                    "m_TracksMaxSpeed",
                    "m_HasWheelParticles",
                    "m_TracksSkidSpeed",
                    "m_TracksSkidSpeed",

                    "m_PushStackIndex",
                    "m_PullStackIndex",
                    "m_NextPushStackIndex",

                    "m_ForceLegacyVariance",
                    "m_CasingVelocity",
                    "m_CasingEjectVariance",
                    "m_CasingEjectSpin",

                    "m_RemoteShotFiredPending",
                    "m_HasTargetInFiringCone",

                    "m_SequentialPowerUp",
                    "m_InterpTimeOn",
                    "m_InterpTimeOff",
                    "m_ParticleLife",
                    "m_ParticleSpeed",
                    "m_RepelKick",
                    "m_RepelKickAngular",
                    "m_ScriptDisabled",
                    "m_ForceLegacyVariance",
                    "m_MinTechForce",
                    "m_MaxTechForce",
                    "m_localXAxisCheck",
                    "m_localYAxisCheck",
                    "m_localZAxisCheck",
                    "m_EnergyDeficit",
                    "m_EnergyDrain",
                    "m_HealingHeartbeatNextTime",
                    "m_TimeWithoutEnergy",
                    "m_EnoughPower",
                    "m_ChargingEffect",
                    "m_PowerUpTimer",
                    "m_CachedCollisionFixedFrameCount",

                    "m_LiftActive",
                    "m_WantsLift",
                    "m_UpAndDownMode",
                    "m_ControllerInputting",

                    "m_AccumulatedCharge",
                    "m_ArcTimer",
                    "m_ArcEffectIndex",
                    "m_ArcEffectVariant",
                    "m_EnableRemoteCharging",

                    "m_ScaleHitParticles",
                    "m_AudioUpdateRate",
                    "m_ActivatedFlags",
                    "m_ReceivingChargeCheck",
                    "m_LastCollisionTime",
                    "m_IsEnabledWithCurrentBlockConnections",

                    "m_CurrentPower",
                    "m_WantsThrottleControl",

                    "m_ProduceSFXDelay",
                    "m_ResourceGroundRadius",
                    "m_ProduceWhileUnloaded",
                    "m_DialAnimationName",
                    "m_DialAnimatorLayer",
                    "m_MinDispenseInterval",
                    "m_SecPerItemProduced",

                    "m_PickupSortBuckets",
                    "m_AcceptNeutral",
                    "m_PrePickupPeriod",
                    "m_PickupAfterMinInterval",
                    "m_HandoverAfterMinInterval",
                    "m_VisionRefreshInterval",
                    "m_VisionRefreshTimer",
                    "m_AcceptNeutral",
                    "m_SortFirstBucket",
                    "m_RangeIndicatorSpinAngle",

                    "m_DropRadiusVsPickup",
                    "m_GluePeriod",
                    "m_SettlingSpeedThreshold",
                    "m_GluedMass",
                    "m_SettleThresholdSqr",
                    "m_PickupDelayTimeout",
                    "HummPlaying",

                    "m_ScaleChanged",
                    "m_BeamBaseHeight",
                    "m_ShowParticlesWhenHeld",
                    "m_UsePhysicsForHeldItems",
                    "m_OverrideDropAfterMinTime",
                    "m_OverrideHeightCorrectionLiftFactor",

                    "m_CapacityPerStack",
                    "m_HorizontalBoundsRadius",
                    "m_ContentsModificationIndex",
                    "m_AnonItemTimeStamp",
                    "m_ModificationIndexLastStackBalance",

                    "m_FirstPasserThisHeartbeat",
                    "m_LastPassHeartbeat",

                    "m_ManualControl",
                    "m_UseRecipeBuildTime",
                    "m_ActivityBlocksInput",
                    "m_OperationBlocksConsume",
                    "m_AllowMultItemsOnInput",
                    "m_AcceptsRecipeDonges",
                    "m_ProduceSFXDelay",
                    "m_AddSoldBlocksToShop",
                    "m_NextInputIndex",
                    "m_TechDonglesDirty",
                    "m_CanHonourRequests",
                    "m_FirstOperatingHeartbeat",
                    "m_LastRequestReceivedHeartbeat",
                    "m_LastBuildRequestHeartbeat",
                    "m_Purging",
                    "m_OperateItemInterceptPos",
                    "m_OperateItemInterceptedPrevPos",
                    "m_HasDirtyNetState",
                    "m_NetClientIsOperating",

                    "m_ReadyAfterTime",
                    "m_ShouldSetAsTracked",
                    "m_HasAnchor",

                    "m_IsTriggered",

                    "m_RotationAnimSpeed",
                    "m_RotationMinSpeed",
                    "m_RotationMaxSpeed",
                    "m_Trim",
                    "m_HasTurnInput",
                    "m_HasDriveInput",
                    "m_ControlTrim",
                    "m_ControlTrimTarget",
                    "m_TrimAdjustSpeed",

                    "m_LedMaterialColorName",
                    "m_LedMaterialFloatName",
                    "m_DetachedFuelAmount",
                    "m_PrevFuelLevel",

                    "m_CurrentSpeed",
                    "m_ForwardSpeed",
                    "m_CanGenerate",
                    "m_SpinnerRotationModifier",
                    "m_SpinnerMaxRotationSpeedChange",

                    "m_DetachQueued",

                    "m_IsEnabled",
                    "m_IsFiringSteer",
                    "m_IsFiringBoost",
                    "m_BoosterAudioType",

                    "m_SwitchedOn",
                    "m_HasPower",
                    "m_GravityTargetsDirty",
                    "m_GravityDelta",
                    "m_GravityManipulationZoneDirty",
                    "m_LayerMask",

                    "m_HasBeenRestored",

                    "m_Deployed",
                    "m_NoInput",
                    "m_HasVelocity",
                };


    }
}
