using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;

namespace TerraTechETCUtil
{
    public class WikiPageBlock : ManIngameWiki.WikiPage
    {
        public int blockID;
        public string desc = "unset";
        internal ModuleInfo mainInfo;
        internal ModuleInfo damage;
        internal ModuleInfo damageable;
        internal ModuleInfo[] modules;
        public WikiPageBlock(int BlockID) :
            base(GetBlockModName(BlockID), StringLookup.GetItemName(ObjectTypes.Block, BlockID),
            ManUI.inst.GetSprite(ObjectTypes.Block, BlockID), "Blocks", ManIngameWiki.BlocksSprite)
        {
            blockID = BlockID;
            desc = StringLookup.GetItemDescription(ObjectTypes.Block, BlockID);
        }

        public WikiPageBlock(int BlockID, ManIngameWiki.WikiPageGroup group) : 
            base(GetBlockModName(BlockID), StringLookup.GetItemName(ObjectTypes.Block, BlockID),
            ManUI.inst.GetSprite(ObjectTypes.Block, BlockID), group)
        {
            blockID = BlockID;
            desc = StringLookup.GetItemDescription(ObjectTypes.Block, BlockID);
        }
        public override void DisplaySidebar()
        {
            ButtonGUIDisp();
        }
        public override bool ReleaseAsMuchAsPossible()
        {
            if (mainInfo != null)
                mainInfo = null;
            if (damage != null)
                damage = null;
            if (damageable != null)
                damageable = null;
            if (modules != null)
            {
                modules = null;
                return true;
            }
            return false;
        }
        private static List<ModuleInfo> Combiler = new List<ModuleInfo>();
        public override void DisplayGUI()
        {
            if (modules == null)
            {
                BlockTypes BT = (BlockTypes)blockID;
                var prefab = ManSpawn.inst.GetBlockPrefab(BT);
                if (prefab)
                {
                    try
                    {
                        var modulesC = ModuleInfo.TryGetModules(prefab);
                        foreach (var item in modulesC)
                        {
                            if (item.name == "General")
                            {
                                item.infos.Add("Cost", RecipeManager.inst.GetBlockBuyPrice(BT));
                                int chunkCount = 0;
                                RecipeManager.inst.recipeTable.m_RecipeLists.Find(x =>
                                x.m_Recipes.Exists(y =>
                                {
                                    if (y.m_OutputType == RecipeTable.Recipe.OutputType.Items &&
                                        y.m_OutputItems.Any(z =>
                                            z.m_Item.ObjectType == ObjectTypes.Block &&
                                            z.m_Item.ItemType == blockID
                                        ))
                                    {
                                        foreach (var item2 in y.m_InputItems)
                                        {
                                            chunkCount += item2.m_Quantity;
                                        }
                                        return true;
                                    }
                                    return false;
                                }));
                                item.infos.Add("Ingredient Count", chunkCount);
                                mainInfo = item;
                            }
                            else if (item.name == "Armoring")
                                damageable = item;
                            else if (item.name == "Durability")
                                damage = item;
                            else
                                Combiler.Add(item);
                        }
                        modules = Combiler.ToArray();
                    }
                    finally
                    {
                        Combiler.Clear();
                    }
                }
            }
            GUILayout.BeginHorizontal();
            AltUI.Sprite(icon, AltUI.TextfieldBorderedBlue, GUILayout.Height(128), GUILayout.Width(128));
            GUILayout.BeginVertical(AltUI.TextfieldBordered);
            if (mainInfo != null)
                mainInfo.DisplayGUI();
            if (damage != null)
                damage.DisplayGUI();
            if (damageable != null)
                damageable.DisplayGUI();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.Label(desc, AltUI.TextfieldBlackHuge);

            GUILayout.BeginVertical(AltUI.TextfieldBordered);
            if (modules != null && modules.Any())
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Modules: ", AltUI.LabelBlackTitle);
                GUILayout.Label(modules.Length.ToString(), AltUI.LabelBlackTitle);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                foreach (var item in modules)
                {
                    item.DisplayGUI();
                }
            }
            else
                GUILayout.Label("Modules: None", AltUI.LabelBlackTitle);
            GUILayout.EndVertical();
        }

        internal static string GetBlockModName(int blockType)
        {
            string titleRaw = ManMods.inst.GetModNameForBlockID((BlockTypes)blockType);
            if (titleRaw.Equals("Unknown Mod"))
                return ManIngameWiki.VanillaGameName;
            else if (ResourcesHelper.TryGetModContainer(titleRaw, out ModContainer MC))
            {
                return MC.ModID;
            }
            return titleRaw;
        }


        public const string powerUnit = " kW";
        public const string distUnit = " m";
        public const string thrustUnit = " N";
        public const string weightUnit = " T";
        internal class ModuleInfo
        {
            internal static bool AdvancedBatchDisplayer(object toExplore, bool ActuallyDisplay)
            {
                if (toExplore is BlockRarity BR)
                {
                    if (ActuallyDisplay)
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
                    if (ActuallyDisplay)
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
                    if (ActuallyDisplay)
                    {
                        GUILayout.Label(DAT.ToString(), AltUI.LabelBlue);
                        GUILayout.Space(6);
                        AltUI.Sprite(ManUI.inst.GetDamageableTypeIcon(DAT), 
                            AltUI.TextfieldBorderedBlue, GUILayout.Height(64), GUILayout.Width(64));
                    }
                    return true;
                }
                else if (toExplore is ManDamage.DamageType DT)
                {
                    if (ActuallyDisplay)
                    {
                        GUILayout.Label(DT.ToString(), AltUI.LabelBlue);
                        GUILayout.Space(6);
                        AltUI.Sprite(ManUI.inst.GetDamageTypeIcon(DT),
                            AltUI.TextfieldBorderedBlue, GUILayout.Height(64), GUILayout.Width(64));
                    }
                    return true;
                }
                else if (toExplore is WeaponRound WR)
                {
                    if (ActuallyDisplay)
                    {
                        DT = WR.DamageType;
                        GUILayout.Label(DT.ToString(), AltUI.LabelBlue);
                        GUILayout.Space(6);
                        AltUI.Sprite(ManUI.inst.GetDamageTypeIcon(DT),
                            AltUI.TextfieldBorderedBlue, GUILayout.Height(64), GUILayout.Width(64));
                    }
                    return true;
                }
                return EnumDisplayer<TechAudio.SFXType>(toExplore, ActuallyDisplay);
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
                    typeof(ModuleAnimator),
                    typeof(ModuleAntenna),
                    typeof(ModuleHUDContextControlField),
                    typeof(ModulePlacementZoneEffect),
                };
            internal static List<Type> ignoreTypesExt = new List<Type>();

            private static HashSet<object> grabbed = new HashSet<object>();
            private static List<ModuleInfo> cacher = new List<ModuleInfo>();
            internal static ModuleInfo[] TryGetModules(TankBlock toExplore)
            {
                try
                {
                    foreach (var item in toExplore.GetComponents<MonoBehaviour>())
                    {
                        Type typeCase = item.GetType();
                        if (grabbed.Contains(item))
                            continue;
                        grabbed.Add(item);
                        if (AllowedTypes.Contains(typeCase))
                        {
                            cacher.Add(new ModuleInfo(typeCase, item, grabbed));
                        }
                        else if (item is Module)
                        {
                            if (!ignoreTypes.Contains(typeCase))
                                cacher.Add(new ModuleInfo(typeCase, item, grabbed));
                        }
                        else if (item is ExtModule)
                        {
                            if (!ignoreTypesExt.Contains(typeCase))
                                cacher.Add(new ModuleInfo(typeCase, item, grabbed));
                        }
                    }
                    return cacher.ToArray();
                }
                catch (Exception e)
                {
                    throw new Exception("TryGetModules failed - ", e);
                }
                finally
                {
                    cacher.Clear();
                    grabbed.Clear();
                }
            }
            private static HashSet<object> checkedAlready = new HashSet<object>();
            internal static void Explorer<T>(Type type, T toExplore, HashSet<object> checkedAlready, ref Dictionary<string, object> infos)
            {
                try
                {
                    Explorer_InternalRecurse(type, toExplore, checkedAlready, ref infos);
                }
                finally
                {
                    checkedAlready.Clear();
                }
            }
            private static void Explorer_InternalRecurse<T>(Type type, T toExplore, HashSet<object> checkedAlready, ref Dictionary<string, object> infos)
            {
                if (toExplore == null)
                    return;
                FieldInfo[] FIs = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (var item in FIs)
                {
                    object itemGet;
                    if (item.IsStatic || item.Name.Contains("k__") || ignoreNames.Contains(item.Name))
                    {
                        continue;
                    }
                    else if (SpecialNamesFuncs.TryGetValue(item.Name, out var guiFunc))
                    {
                        infos.Add(guiFunc.Key, guiFunc.Value(item.GetValue(toExplore)));
                    }
                    else if (item.FieldType == typeof(int) || item.FieldType == typeof(float) ||
                        item.FieldType == typeof(bool) || item.FieldType == typeof(string) ||
                        enumTypes.Contains(item.FieldType))
                    {
                        itemGet = item.GetValue(toExplore);
                        if (SpecialNames.TryGetValue(item.Name, out string altName))
                            infos.Add(altName, itemGet);
                        else
                            infos.Add(item.Name.Replace("m_", "").SplitCamelCase(), itemGet);
                    }
                    else if (AllowedTypes.Contains(item.FieldType))
                    {
                        itemGet = item.GetValue(toExplore);
                        if (checkedAlready.Contains(itemGet))
                            continue;
                        checkedAlready.Add(itemGet);
                        Explorer_InternalRecurse(item.FieldType, itemGet, checkedAlready, ref infos);
                    }
                    else if (AdvancedBatchDisplayer(item.FieldType, false))
                    {
                        itemGet = item.GetValue(toExplore);
                        if (SpecialNames.TryGetValue(item.Name, out string altName))
                            infos.Add(altName, itemGet);
                        else
                            infos.Add(item.Name.Replace("m_", "").SplitCamelCase(), itemGet);
                    }
                }
            }



            internal readonly string name;
            internal bool open;
            internal bool recipesOpen;
            internal Dictionary<string, object> infos = new Dictionary<string, object>();
            public ModuleInfo(Type grabbedType, object prefab, HashSet<object> grabbedAlready)
            {
                if (SpecialNames.TryGetValue(grabbedType.Name, out string altName))
                    name = altName;
                else
                    name = grabbedType.Name.Replace("Module", "").ToString().SplitCamelCase();
                Explorer(grabbedType, prefab, grabbedAlready, ref infos);
            }

            internal void DisplayGUI()
            {
                GUILayout.BeginVertical(AltUI.BoxBlack);
                if (GUILayout.Button(name.NullOrEmpty() ? "NULL NAME" : name, open ? AltUI.LabelBlueTitle : AltUI.LabelWhiteTitle))
                    open = !open;
                if (open)
                {
                    if (infos == null)
                    {
                        GUILayout.Label("INFO IS NULL", AltUI.LabelRed);
                    }
                    else
                    {
                        foreach (var item in infos)
                        {
                            try
                            {
                                if (item.Key == null || item.Value == null)
                                    continue;
                                GUILayout.BeginHorizontal(AltUI.TextfieldBlackSearch);
                                GUILayout.Label(item.Key, AltUI.LabelWhite);
                                GUILayout.FlexibleSpace();
                                if (item.Value is Sprite spriteCase)
                                    AltUI.Sprite(spriteCase, AltUI.TextfieldBorderedBlue, GUILayout.Height(64), GUILayout.Width(64));
                                else if (AdvancedBatchDisplayer(item.Value, true))
                                {
                                }
                                else if (item.Value is int intCase)
                                    GUILayout.Label(intCase.ToString(), AltUI.LabelBlue);
                                else if (item.Value is float floatCase)
                                    GUILayout.Label(floatCase.ToString("F2"), AltUI.LabelBlue);
                                else if (item.Value is bool bCase)
                                    GUILayout.Label(bCase.ToString(), AltUI.LabelBlue);
                                else if (item.Value is string sCase)
                                    GUILayout.Label(sCase, AltUI.LabelWhite);
                                else if (item.Value is List<string> sLCase)
                                {
                                    recipesOpen = AltUI.Toggle(recipesOpen, "Recipies");
                                    if (recipesOpen)
                                    {
                                        GUILayout.BeginVertical(AltUI.TextfieldBordered);
                                        foreach (var item2 in sLCase)
                                        {
                                            GUILayout.Label(item2, AltUI.LabelBlack);
                                        }
                                        GUILayout.EndVertical();
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
                }
                GUILayout.EndVertical();
            }
        }



        internal static HashSet<Type> AllowedTypes = new HashSet<Type>() {
                    typeof(TankBlock),
                    typeof(Damageable),
                    typeof(FireData),
                    typeof(ModuleWing.Aerofoil),
                    typeof(ManWheels.WheelParams),
                    typeof(ManWheels.TireProperties),
                };
        internal static Dictionary<string, string> SpecialNames = new Dictionary<string, string>()
                {
                    { "TankBlock", "General"},
                    { "Damageable", "Armoring"},
                    { "ModuleDamage", "Durability"},
                    { "ModuleAIBot", "AI Module"},
                    { "ModuleDriveBot", "Combat Autopilot"},
                    { "ModuleAnimatedMeleeWeapon", "Advanced Melee Weapon"},
                    { "ModuleDetachableLink", "Explosive Bolt"},
                    { "ModuleEnergyStore", "Battery"},
                    { "ModuleItemConsume", "Item Processor"},
                    { "ModuleItemProducer", "Autominer"},
                    { "ModuleItemStore", "Item Silo"},
                    { "ModuleRadarMarker", "Beacon"},
                    { "ModuleShieldGenerator", "Bubble Generator"},
                    { "ModuleTechController", "Cab"},
                    { "ModuleVision", "Weapon Radar"},
                    { "ModuleBlockStateController", "Button Panel"},
                    { "ModuleCircuitReceiver", "Circuit Input"},
                    { "ModuleCircuitDispensor", "Circuit Output"},
                    { "ModuleRemoteCharger", "Wireless Charger"},
                    { "ModuleItemHolderBeam", "Logistics Beam"},
                    //
                    { "m_DefaultMass", "Mass"},
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
        private static object DisplayDist(object valIn)
        {
            try
            {
                return AltUI.BlueString(((float)valIn).ToString("F2") + distUnit);
            }
            catch (Exception)
            {
                return AltUI.BlueString("Error");
            }
        }
        private static object DisplaykW(object valIn)
        {
            try
            {
                return AltUI.BlueString(((float)valIn).ToString("F2") + powerUnit);
            }
            catch (Exception)
            {
                return AltUI.BlueString("Error");
            }
        }
        private static object DisplayNewtons(object valIn)
        {
            try
            {
                return AltUI.BlueString(((float)valIn).ToString("F2") + thrustUnit);
            }
            catch (Exception)
            {
                return AltUI.BlueString("Error");
            }
        }
        private static object DisplayWeight(object valIn)
        {
            try
            {
                return AltUI.BlueString(((float)valIn).ToString("F2") + weightUnit);
            }
            catch (Exception)
            {
                return AltUI.BlueString("Error");
            }
        }

        private static object DisplayPercent(object valIn)
        {
            return AltUI.BuyString(((float)valIn).ToString("P"));
        }
        private static object DisplayPercentInv(object valIn)
        {
            if ((float)valIn <= 0)
                return AltUI.BuyString("0 %");
            return AltUI.BuyString((1 / (float)valIn).ToString("P"));
        }

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
                                doDisp.Add(input + " => " + AltUI.BlueString(item2.m_EnergyOutput +
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
                ModuleRadar.RadarScanType type = (ModuleRadar.RadarScanType)step;
                float range = Ranges[step];
                if (range <= 0)
                {
                    switch (type)
                    {
                        case ModuleRadar.RadarScanType.Techs:
                            doDisp.Add(AltUI.BlueString("Techs") + " => " + AltUI.EnemyString("N/A"));
                            break;
                        case ModuleRadar.RadarScanType.Resources:
                            doDisp.Add(AltUI.SideCharacterString("Chunks") + " => " + AltUI.EnemyString("N/A"));
                            break;
                        case ModuleRadar.RadarScanType.Terrain:
                            doDisp.Add(AltUI.HighlightString("Terrain") + " => " + AltUI.EnemyString("N/A"));
                            break;
                    }
                }
                else
                {
                    switch (type)
                    {
                        case ModuleRadar.RadarScanType.Techs:
                            doDisp.Add(AltUI.BlueString("Techs") + " => " + range + distUnit);
                            break;
                        case ModuleRadar.RadarScanType.Resources:
                            doDisp.Add(AltUI.SideCharacterString("Chunks") + " => " + range + distUnit);
                            break;
                        case ModuleRadar.RadarScanType.Terrain:
                            doDisp.Add(AltUI.HighlightString("Terrain") + " => " + range + distUnit);
                            break;
                    }
                }
            }
            return doDisp;
        }

        internal static Dictionary<string, KeyValuePair<string, Func<object, object>>> SpecialNamesFuncs =
            new Dictionary<string, KeyValuePair<string, Func<object, object>>>()
                {
                    { "attachPoints", new KeyValuePair<string, Func<object, object>>(
                        "Attach Point Count", CountItems)},
                    { "filledCells", new KeyValuePair<string, Func<object, object>>(
                        "Volume", CountItems)},
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
                        return rot.ToString("F2") + " Degrees/sec";
                    } )},
                    { "m_BurstShotCount", new KeyValuePair<string, Func<object, object>>("Shots Per Burst Fire", (x) => {
                        int rounds = (int)x;
                        if (rounds <= 0)
                            return "Does Not Burst-Fire";
                        return rounds.ToString();
                    } )},
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
                };
        internal static HashSet<Type> ignoreTypes = new HashSet<Type>() {
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
                    typeof(ModulePlatformRestrictions),
                };

        internal static HashSet<string> ignoreNames = new HashSet<string>() {
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
