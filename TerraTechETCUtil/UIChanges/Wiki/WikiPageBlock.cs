using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using UnityEngine;
using static TerraTechETCUtil.ManIngameWiki;

namespace TerraTechETCUtil
{
    /// <inheritdoc cref="ManIngameWiki.WikiPage"/>
    /// <summary>
    /// <para>Wiki page for block information</para>
    /// </summary>
    public class WikiPageBlock : ManIngameWiki.WikiPage
    {
        /// <summary>
        /// An additional event displayer for mods to add their own wiki data
        /// </summary>
        public static Event<BlockTypes> AdditionalDisplayOnUI = new Event<BlockTypes>();
        /// <summary>
        /// page, stringSection, searchText - 
        /// Invoke the Action as true if the filter was triggered and true, or false if the filter failed to cancel the search target.
        /// Do not invoke if the filter was not triggered!
        /// <para>Use ManIngameWiki.WikiPage.FilterBlockPass for quick text filtering.  Be sure to end it with '<b>:</b>'!</para>
        /// <para>Also add the annotation to AdditionalSearchFiltersPopup with a newline call</para>
        /// </summary>
        public static Event<WikiPageBlock, string, string, Action<bool>> AdditionalSearchFilters = new Event<WikiPageBlock, string, string, Action<bool>>();
        /// <summary>
        /// Use the stringbuilder given to AppendLine() new search filters for the user
        /// </summary>
        public static Event<StringBuilder> AdditionalSearchFiltersPopup = new Event<StringBuilder> ();

        //private static List<WikiPageBlock> IconsPending = null;

        /// <inheritdoc cref="WikiPageBiome.biomeInst"/>
        public int blockID;
        /// <inheritdoc cref="WikiPageBiome.desc"/>
        public string desc = "unset";
        /// <summary>
        /// The cached instance of the <see cref="TankBlock"/> for this block.
        /// <para><b>MOST OF THE TIME THIS FIELD IS NULL! DO NOT USE!!!</b></para>
        /// </summary>
        public TankBlock inst;
        /// <summary>
        /// If this is shown in the wiki for normal players
        /// </summary>
        public bool publicVisible { get; private set; } = false;
        internal ModuleInfo mainInfo;
        internal ModuleInfo damage;
        internal ModuleInfo damageable;
        internal ModuleInfo[] modules;
        private static Sprite GetSprite(int itemType)
        {
            Sprite sprite = ManUI.inst.GetSprite(ObjectTypes.Block, itemType);
            if (sprite == UIHelpersExt.NullSprite)
                return null;
            return sprite;
        }

        /// <inheritdoc cref="ManIngameWiki.WikiPage.WikiPage(string, LocExtString, Sprite, ManIngameWiki.WikiPageGroup)"/>
        /// <summary>
        /// 
        /// </summary>
        /// <param name="BlockID">The block ID (<see cref="BlockTypes"/>) to affiliate with this page</param>
        public WikiPageBlock(int BlockID) :
            base(GetBlockModName(BlockID), new LocExtStringFunc(StringLookup.GetItemName(ObjectTypes.Block, BlockID), 
                () => { return StringLookup.GetItemName(ObjectTypes.Block, BlockID); }),
            GetSprite(BlockID), ManIngameWiki.LOC_Blocks, ManIngameWiki.BlocksSprite)
        {
            blockID = BlockID;
            desc = StringLookup.GetItemDescription(ObjectTypes.Block, BlockID);
            string titleRaw = ManMods.inst.GetModNameForBlockID((BlockTypes)BlockID);
            if (!titleRaw.Equals("Unknown Mod"))
            {
                /*
                if (IconsPending == null)
                    IconsPending = new List<WikiPageBlock>();
                IconsPending.Add(this);
                */
            }
            publicVisible = StringLookup.GetItemName(ObjectTypes.Block, BlockID) != StringLookup.GetItemName(ObjectTypes.Block, -1);
        }
        /// <inheritdoc cref=" WikiPageBlock.WikiPageBlock(int)"/>
        public WikiPageBlock(int BlockID, ManIngameWiki.WikiPageGroup group) : 
            base(GetBlockModName(BlockID), new LocExtStringFunc(StringLookup.GetItemName(ObjectTypes.Block, BlockID),
                () => { return StringLookup.GetItemName(ObjectTypes.Block, BlockID); }),
            GetSprite(BlockID), group)
        {
            blockID = BlockID;
            desc = StringLookup.GetItemDescription(ObjectTypes.Block, BlockID);
            string titleRaw = ManMods.inst.GetModNameForBlockID((BlockTypes)BlockID);
            if (!titleRaw.Equals("Unknown Mod"))
            {
                /*
                if (IconsPending == null)
                    IconsPending = new List<WikiPageBlock>();
                IconsPending.Add(this);
                */
            }
            // Catch "This Block No Longer Exists"
            publicVisible = StringLookup.GetItemName(ObjectTypes.Block, BlockID) != StringLookup.GetItemName(ObjectTypes.Block, -1);
        }
        /// <inheritdoc/>
        protected override WikiPageGroup InsureWikiGroup(string ModID, LocExtString WikiGroupName, Sprite Icon = null) =>
            InsureBlockWikiGroup(ModID);
        /// <inheritdoc/>
        protected override WikiPageGroup InsureWikiGroup(string ModID, string WikiGroupName, Sprite Icon = null) =>
            InsureBlockWikiGroup(ModID);


        /// <inheritdoc/>
        public override void GetIcon()
        {
            icon = ManUI.inst.GetSprite(ObjectTypes.Block, blockID);
            /*
            if (IconsPending == null)
                return;
            foreach (var item in IconsPending)
            {
                item.icon = ManUI.inst.GetSprite(ObjectTypes.Block, item.blockID);
            }
            */
        }

        internal static void ValidBlockSearchQueryPopup()
        {
            try
            {
                AdditionalSearchFiltersPopup.Send(additionalFilters);
                AltUI.Tooltip.GUITooltip("Supports searching in english with no spaces for:\n" +
                    "corp:[Shorthand for corporation]\n" +
                    "type:[block category name]\n" +
                    "attribute:[attribute name(s)]\n" +
                    "control:[control category name(s)]\n" +
                    "chunk:[resource name(s)]\n" +
                    "module:[block module name(s)]\n" +
                    "dmg:[block main damageable type]\n" +
                    "hp[g/l]:[max health greater/less than]\n" +
                    "grade:[exact grade level(s)]\n" +
                    "grade[g/l]:[grade level greater/less than]\n" +
                    "buy[g/l]:[buying cost greater/less than]\n" +
                    "sell[g/l]:[selling cost greater/less than]\n" +
                    "mass:[exact mass(es) of block]\n" +
                    "mass[g/l]:[mass of block greater/less than]\n" +
                    additionalFilters.ToString());
            }
            finally
            {
                additionalFilters.Clear();
            }
        }
        internal static bool ValidBlockSearchQueryPost(ManIngameWiki.WikiPage page, string pageName, string searchText)
        {
            if (page is WikiPageBlock WPB)
            {
                BlockTypes BT = (BlockTypes)WPB.blockID;
                int ignoreExtra = 0;
                var stringSections = searchText.Split(' ');
                foreach (var stringSection in stringSections)
                {
                    string[] queries = null;
                    if (FilterPass(stringSection, "corp:", ref queries))
                    {
                        FactionSubTypes FST = ManSpawn.inst.GetCorporation(BT);
                        if (FST < FactionSubTypes.NULL || (
                            !queries.Any(x => ((int)FST < Enum.GetValues(typeof(FactionSubTypes)).Length &&
                            FST.ToString().ToLower().Contains(x)) ||
                            ManMods.inst.FindCorpShortName(FST).ToLower().Contains(x) ||
                            StringLookup.GetCorporationName(FST).Replace(" ", string.Empty).
                            ToLower().Contains(x))))
                            return false;
                    }
                    else if (FilterPass(stringSection, "faction:", ref queries))
                    {
                        FactionSubTypes FST = ManSpawn.inst.GetCorporation(BT);
                        if (FST < FactionSubTypes.NULL || (
                            !queries.Any(x => ((int)FST < Enum.GetValues(typeof(FactionSubTypes)).Length &&
                            FST.ToString().ToLower().Contains(x)) ||
                            ManMods.inst.FindCorpShortName(FST).ToLower().Contains(x) ||
                            StringLookup.GetCorporationName(FST).Replace(" ", string.Empty).
                            ToLower().Contains(x))))
                            return false;
                    }
                    else if (FilterPass(stringSection, "type:", ref queries))
                    {
                        if (!queries.Any(x => StringLookup.GetBlockCategoryName(
                            ManSpawn.inst.GetCategory(BT)).
                            Replace(" ", string.Empty).ToLower().Contains(x)))
                            return false;
                    }
                    else if (FilterPass(stringSection, "attribute:", ref queries))
                    {
                        if (!queries.All(x => ManSpawn.inst.GetBlockAttributes(BT).
                            Any(y => StringLookup.GetBlockAttribute(y).
                            Replace(" ", string.Empty).ToLower().Contains(x))))
                            return false;
                    }
                    else if (FilterPass(stringSection, "control:", ref queries))
                    {
                        if (!queries.All(x => ManSpawn.inst.GetBlockControlAttributes(BT).
                            Any(y => StringLookup.GetBlockControlAttribute(y).
                            Replace(" ", string.Empty).ToLower().Contains(x))))
                            return false;
                    }
                    else if (FilterPass(stringSection, "chunk:", ref queries))
                    {
                        try
                        {
                            if (!queries.All(x => RecipeManager.inst.GetRecipeByOutputType(
                                new ItemTypeInfo(ObjectTypes.Block, WPB.blockID))
                                .m_InputItems.Any(y => StringLookup.GetItemName(y.m_Item).
                                Replace(" ", string.Empty).ToLower().Contains(x))))
                                return false;
                        }
                        catch
                        {
                            return false;
                        }
                    }
                    else if (FilterPass(stringSection, "module:", ref queries))
                    {
                        var prefab = ManSpawn.inst.GetBlockPrefab(BT);
                        if (prefab != null)
                        {
                            foreach (var x in queries)
                            {
                                if (!ManSpawn.inst.GetBlockPrefab(BT).GetComponents<Module>()?.
                                    FirstOrDefault(y => y.GetType().ToString().ToLower().Contains(x)) &&
                                    !ManSpawn.inst.GetBlockPrefab(BT).GetComponents<ExtModule>()?.
                                    FirstOrDefault(y => y.GetType().ToString().ToLower().Contains(x)))
                                    return false;
                            }
                        }
                        else
                            return false;
                    }
                    else if (FilterPass(stringSection, "dmg:", ref queries))
                    {
                        if (ManSpawn.inst.GetBlockPrefab(BT)?.GetComponent<Damageable>() == null ||
                            !queries.Any(x => StringLookup.GetDamageableTypeName(
                                ManSpawn.inst.GetBlockPrefab(BT).GetComponent<Damageable>().
                             DamageableType).Replace(" ", string.Empty).ToLower().Contains(x)))
                            return false;
                    }
                    else if (FilterPass(stringSection, "grade:", ref queries))
                    {
                        if (!queries.Any(x => int.TryParse(x, out int num) && 
                            (ManLicenses.inst.GetBlockTier(BT) + 1) == num))
                            return false;
                    }
                    else if (FilterPass(stringSection, "gradeg:", ref queries))
                    {
                        if (!int.TryParse(queries[0], out int num) || 
                            (ManLicenses.inst.GetBlockTier(BT) + 1) >= num)
                            return false;
                    }
                    else if (FilterPass(stringSection, "gradel:", ref queries))
                    {
                        if (!int.TryParse(queries[0], out int num) || 
                            (ManLicenses.inst.GetBlockTier(BT) + 1) <= num)
                            return false;
                    }
                    else if (FilterPass(stringSection, "hpg:", ref queries))
                    {
                        if (!float.TryParse(queries[0], out float num) || 
                            ManSpawn.inst.GetBlockPrefab(BT)?.GetComponent<Damageable>() == null ||
                            ManSpawn.inst.GetBlockPrefab(BT).GetComponent<Damageable>().MaxHealth <= num)
                            return false;
                    }
                    else if (FilterPass(stringSection, "hpl:", ref queries))
                    {
                        if (!float.TryParse(queries[0], out float num) || 
                            ManSpawn.inst.GetBlockPrefab(BT)?.GetComponent<Damageable>() == null ||
                            ManSpawn.inst.GetBlockPrefab(BT).GetComponent<Damageable>().MaxHealth >= num)
                            return false;
                    }
                    else if (FilterPass(stringSection, "buyg:", ref queries))
                    {
                        if (!int.TryParse(queries[0], out int num) || 
                            RecipeManager.inst.GetBlockBuyPrice(BT, true) <= num)
                            return false;
                    }
                    else if (FilterPass(stringSection, "buyl:", ref queries))
                    {
                        if (!int.TryParse(queries[0], out int num) || 
                            RecipeManager.inst.GetBlockBuyPrice(BT, true) >= num)
                            return false;
                    }
                    else if (FilterPass(stringSection, "sellg:", ref queries))
                    {
                        if (!int.TryParse(queries[0], out int num) ||
                            RecipeManager.inst.GetBlockSellPrice(BT) <= num)
                            return false;
                    }
                    else if (FilterPass(stringSection, "selll:", ref queries))
                    {
                        if (!int.TryParse(queries[0], out int num) ||
                            RecipeManager.inst.GetBlockSellPrice(BT) >= num)
                            return false;
                    }
                    else if (FilterPass(stringSection, "mass:", ref queries))
                    {
                        if (!queries.Any(x => float.TryParse(x, out float num) &&
                            ManSpawn.inst.GetBlockPrefab(BT) != null &&
                            ManSpawn.inst.GetBlockPrefab(BT).m_DefaultMass == num))
                            return false;
                    }
                    else if (FilterPass(stringSection, "massg:", ref queries))
                    {
                        if (!float.TryParse(queries[0], out float num) || 
                            ManSpawn.inst.GetBlockPrefab(BT) == null ||
                            ManSpawn.inst.GetBlockPrefab(BT).m_DefaultMass <= num)
                            return false;
                    }
                    else if (FilterPass(stringSection, "massl:", ref queries))
                    {
                        if (!float.TryParse(queries[0], out float num) || 
                            ManSpawn.inst.GetBlockPrefab(BT) == null ||
                            ManSpawn.inst.GetBlockPrefab(BT).m_DefaultMass >= num)
                            return false;
                    }
                    else
                    {
                        bool filteredAtLeastOnce = false;
                        bool cancel = false;
                        AdditionalSearchFilters.Send(WPB, stringSection, searchText, (state) =>
                        {
                            filteredAtLeastOnce = true;
                            if (!state)
                                cancel = true;
                        });
                        if (cancel)
                            return false;
                        if (!filteredAtLeastOnce)
                            break;
                    }
                    ignoreExtra = stringSection.Length + 1;
                }
                if (searchText.Length <= ignoreExtra)
                    return true;
                string leftOverText = searchText.Substring(ignoreExtra);
                if (int.TryParse(leftOverText, out int potBlockID) && 
                    potBlockID == WPB.blockID)
                    return true;
                if (Enum.TryParse(leftOverText, out BlockTypes potBlockIDEnum) && 
                    ((int)potBlockIDEnum) == WPB.blockID)
                    return true;
                return pageName.ToLower().Contains(searchText.Substring(ignoreExtra));
            }
            return false;
        }
        internal static bool ValidBlockSearchQuery(ManIngameWiki.WikiPage page, string pageName, string searchText)
        {
            if (page is WikiPageBlock WPB)
            {
                BlockTypes BT = (BlockTypes)WPB.blockID;
                return WPB.publicVisible && ManSpawn.inst.IsBlockAllowedInCurrentGameMode(BT) &&
                    ManGameMode.inst.CheckBlockAllowed(BT) &&
                    !ManSpawn.inst.IsBlockUsageRestrictedInGameMode(BT);
            }
            return false;
        }

        /// <inheritdoc/>
        public override void DisplaySidebar() => ButtonGUIDispLateIcon();
        /// <inheritdoc/>
        public override bool OnWikiClosed()
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
        private static FieldInfo beamRetractGet = AccessTools.Field(typeof(BeamWeapon), "m_FadeOutTime");
        private static FieldInfo thrustGet = AccessTools.Field(typeof(Thruster), "m_Force");
        private static FieldInfo thrustEffectGet = AccessTools.Field(typeof(Thruster), "m_Effector");
        private static FieldInfo boosterDeltaGet = AccessTools.Field(typeof(BoosterJet), "m_FireRateFalloff");
        private static FieldInfo fanDeltaGet = AccessTools.Field(typeof(FanJet), "spinDelta");
        private static FieldInfo hoverDeltaGet = AccessTools.Field(typeof(HoverJet), "m_HoverPowerChangePerSecond");
        private static FieldInfo hoverAngleGet = AccessTools.Field(typeof(HoverJet), "m_VectoredThrustMaxAngle");
        private static FieldInfo hoverDeltaAngleGet = AccessTools.Field(typeof(HoverJet), "m_VectoredThrustAnglePerSecond");
        private static FieldInfo wheelGet = AccessTools.Field(typeof(ModuleWheels), "m_Wheels");
        /// <inheritdoc/>
        public override void OnBeforeDisplay()
        {
            if (ManMods.inst.IsModdedBlock((BlockTypes)blockID))
                icon = ManUI.inst.GetSprite(ObjectTypes.Block, blockID);
        }
        private void OnFirstInitGUI()
        {
            BlockTypes BT = (BlockTypes)blockID;
            var inst = ManSpawn.inst.GetBlockPrefab(BT);
            if (inst)
            {
                try
                {
                    var modulesC = ModuleInfo.TryGetModules(inst.gameObject, ModuleInfo.AllowedTypesUIWiki);
                    foreach (var item in modulesC)
                    {
                        if (item.name == "General")
                        {
                            item.infos.Add("Base Name", inst.name);
                            bool isVanilla = blockID <= Enum.GetValues(typeof(BlockTypes)).Length;
                            item.infos.Add("Is Vanilla", isVanilla.ToString());
                            if (isVanilla)
                            {
                                item.infos.Add("Block ID (Vanilla - Enum)", ((BlockTypes)blockID).ToString());
                                item.infos.Add("Block ID (Vanilla - Int)", blockID.ToString());
                            }
                            else
                            {
                                item.infos.Add("Block ID (Warning!)", AltUI.EnemyString("Changes with mod selection!"));
                                item.infos.Add("Block ID (Modded - Int)", blockID.ToString());
                            }
                            item.infos.Add("Cost", RecipeManager.inst.GetBlockBuyPrice(BT));
                            int chunkCount = 0;
                            List<KeyValuePair<int, ManIngameWiki.WikiLink>> links =
                                new List<KeyValuePair<int, ManIngameWiki.WikiLink>>();
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
                                        links.Add(new KeyValuePair<int, ManIngameWiki.WikiLink>(item2.m_Quantity,
                                        new ManIngameWiki.WikiLink(ManIngameWiki.GetPage(
                                            StringLookup.GetItemName(item2.m_Item.ObjectType, item2.m_Item.ItemType)))));
                                    }
                                    return true;
                                }
                                return false;
                            }));
                            item.infos.Add("Ingredient Count", chunkCount);
                            item.infos.Add("Ingredients", links);
                            mainInfo = item;
                        }
                        else if (item.name == "Armoring")
                            damageable = item;
                        else if (item.name == "Durability")
                            damage = item;
                        else
                        {
                            try
                            {
                                if (item.inst != null)
                                {
                                    if (item.inst is IModuleDamager MDR)
                                    {
                                        item.infos.Add("Damage Type", MDR.DamageType);
                                        item.infos.Add("Estimated Damage On Hit", float.IsInfinity(MDR.GetHitDamage()) ? "Error" : MDR.GetHitDamage().ToString("0.00"));
                                        item.infos.Add("Estimated Hit Rate", float.IsInfinity(MDR.GetHitsPerSec()) ? "Error" : AutoDataExtractor.DisplayCountOfRate(MDR.GetHitsPerSec(), "round(s)"));
                                        item.infos.Add("Damage Per Second", float.IsInfinity(MDR.GetHitDamage() * MDR.GetHitsPerSec()) ? "Error" : 
                                            (MDR.GetHitDamage() * MDR.GetHitsPerSec()).ToString("0.00"));
                                        if (MDR is ModuleWeaponGun MWG)
                                        {
                                            List<string> doDisp = new List<string>();
                                            foreach (var item2 in MWG.GetComponentsInChildren<BeamWeapon>())
                                            {
                                                if (item2 != null)
                                                    doDisp.Add("DPS: " + item2.DamagePerSecond + 
                                                        ", Range: " + AutoDataExtractor.DisplayDist(item2.Range) +
                                                        ", Retract Time: " + AutoDataExtractor.DisplaySec((float)beamRetractGet.GetValue(item2)));
                                            }
                                            if (doDisp.Any())
                                                item.infos.Add("Beam Weapons", doDisp);
                                        }
                                    }
                                    else if (item.inst is ModuleWheels MW)
                                    {
                                        item.infos.Add("Wheel Count", ((List<ManWheels.Wheel>)wheelGet.GetValue(MW)).Count);
                                        item.infos.Add("Wheel Radius", AutoDataExtractor.DisplayDist(MW.m_WheelParams.radius));
                                        item.infos.Add("Suspension Max", AutoDataExtractor.DisplayDist(MW.m_WheelParams.suspensionTravel));
                                        item.infos.Add("Max Steer Angle", AutoDataExtractor.DisplayDeg(MW.m_WheelParams.steerAngleMax));
                                        item.infos.Add("Normal Steer Rate", AutoDataExtractor.DisplayDegSec(MW.m_WheelParams.steerSpeed));
                                        if (MW.m_WheelParams.strafeSteeringSpeed == 0)
                                            item.infos.Add("Strafing Steer Rate", AltUI.EnemyString("Does not strafe"));
                                        else
                                            item.infos.Add("Strafing Steer Rate", AutoDataExtractor.DisplayDegSec(MW.m_WheelParams.strafeSteeringSpeed));
                                        item.infos.Add("Max Potential RPM", MW.m_TorqueParams.torqueCurveMaxRpm);
                                        item.infos.Add("Torque", AutoDataExtractor.DisplayNewtons(MW.m_TorqueParams.torqueCurveMaxTorque));
                                    }
                                    else if (item.inst is ModuleHover MH)
                                    {
                                        List<string> doDisp = new List<string>();
                                        foreach (var item2 in MH.gameObject.GetComponentsInChildren<HoverJet>(true))
                                        {
                                            if (item2 != null)
                                                doDisp.Add("Est. Direction: " + AutoDataExtractor.DisplayDirection(MH.transform.InverseTransformDirection(-item2.effector.forward)) +
                                                    ", Force: " + AutoDataExtractor.DisplayNewtons(item2.forceMax) +
                                                    ", Ramp Time: " + AutoDataExtractor.DisplaySec(1f / (float)hoverDeltaGet.GetValue(item2)) +
                                                    (((float)hoverAngleGet.GetValue(item2) == 0 || (float)hoverDeltaAngleGet.GetValue(item2) == 0) ?
                                                    AltUI.EnemyString("Does not gimbal") :
                                                    (", Max Gimbal Angle: " + AutoDataExtractor.DisplayDeg((float)hoverAngleGet.GetValue(item2)) +
                                                    ", Gimbal Rate: " + AutoDataExtractor.DisplayDegSec((float)hoverDeltaAngleGet.GetValue(item2)))));
                                        }
                                        item.infos.Add("Hover Forces", doDisp);
                                    }
                                    else if (item.inst is ModuleBooster MB)
                                    {
                                        List<string> doDispF = new List<string>();
                                        foreach (var item2 in MB.gameObject.GetComponentsInChildren<FanJet>(true))
                                        {
                                            if (item2 != null)
                                                doDispF.Add("Est. Direction: " + AutoDataExtractor.DisplayDirection(MB.transform.InverseTransformDirection(-item2.EffectorForward)) +
                                                    ", Force: " + AutoDataExtractor.DisplayNewtons((float)thrustGet.GetValue(item2)) +
                                                    ", Ramp Time: " + AutoDataExtractor.DisplaySec(1f / (float)fanDeltaGet.GetValue(item2)));
                                        }
                                        if (doDispF.Any())
                                            item.infos.Add("Propeller Forces", doDispF);
                                        List<string> doDispB = new List<string>();
                                        foreach (var item2 in MB.gameObject.GetComponentsInChildren<BoosterJet>(true))
                                        {
                                            if (item2 != null)
                                                doDispB.Add("Est. Direction: " + AutoDataExtractor.DisplayDirection(MB.transform.InverseTransformDirection(-item2.EffectorForward)) +
                                                    ", Force: " + AutoDataExtractor.DisplayNewtons((float)thrustGet.GetValue(item2)) +
                                                    ", Shutoff Delay: " + AutoDataExtractor.DisplaySec((float)boosterDeltaGet.GetValue(item2)));
                                        }
                                        if (doDispB.Any())
                                            item.infos.Add("Booster Forces", doDispB);
                                    }
                                    else if (item.inst is ModuleWing MWA)
                                    {
                                        item.infos.Add("Aerofoil Count", MWA.m_Aerofoils.Length);
                                        List<string> doDisp = new List<string>();
                                        foreach (var item2 in MWA.m_Aerofoils)
                                        {
                                            if (item2 != null)
                                                doDisp.Add("Force: " + AutoDataExtractor.DisplayNewtons(item2.liftStrength) +
                                                    ", Max Redirection Angle: " + AutoDataExtractor.DisplayDeg(item2.flapAngleRangeActual) +
                                                    ", Max Turn Angle: " + AutoDataExtractor.DisplayDeg(item2.flapAngleRangeVisual) +
                                                    ", Turn Speed: " + AutoDataExtractor.DisplayDegSec(item2.flapTurnSpeed * (item2.flapAngleRangeVisual / item2.flapAngleRangeActual)));
                                        }
                                        if (doDisp.Any())
                                            item.infos.Add("Aerofoils", doDisp);
                                    }
                                }
                            }
                            catch { }
                            Combiler.Add(item);
                        }
                    }
                    modules = Combiler.ToArray();
                }
                finally
                {
                    Combiler.Clear();
                }
            }
        }
        private static bool AttributesShown = false;
        /// <inheritdoc/>
        protected override void DisplayGUI()
        {
            if (modules == null)
                OnFirstInitGUI();
            GUILayout.BeginHorizontal();
            AltUI.Sprite(icon, AltUI.TextfieldBorderedBlue, GUILayout.Height(128), GUILayout.Width(128));
            GUILayout.BeginVertical(AltUI.TextfieldBordered);

            GUILayout.BeginVertical(AltUI.BoxBlack);
            ManIngameWiki.WikiLink link = new ManIngameWiki.WikiLink(ManIngameWiki.GetCorpPage(ManSpawn.inst.GetCorporation((BlockTypes)blockID)));
            if (link.OnGUILarge(AltUI.ButtonBlue, AltUI.LabelWhite))
                link.linked.GoHere();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localisation.inst.GetLocalisedString(LocalisationEnums.Purchasing.BlockGradeTitle), AltUI.LabelWhiteTitle);
            GUILayout.Label(": ", AltUI.LabelWhiteTitle);
            GUILayout.FlexibleSpace();
            GUILayout.Label(StringLookup.GetBlockTierName((BlockTypes)blockID, false), AltUI.LabelGoldTitle);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localisation.inst.GetLocalisedString(LocalisationEnums.Purchasing.BlockCategoryTitle), AltUI.LabelWhite);
            GUILayout.Label(": ", AltUI.LabelWhite);
            GUILayout.FlexibleSpace();
            new ManIngameWiki.WikiIconInfo(ManUI.inst.GetBlockCatIcon(ManSpawn.inst.GetCategory((BlockTypes)blockID)),
                StringLookup.GetBlockCategoryName(ManSpawn.inst.GetCategory((BlockTypes)blockID))).
                OnGUILarge(AltUI.TextfieldBorderedBlue, AltUI.LabelWhite);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localisation.inst.GetLocalisedString(LocalisationEnums.Purchasing.BlockRarityTitle), AltUI.LabelWhite);
            GUILayout.Label(": ", AltUI.LabelWhite);
            GUILayout.FlexibleSpace();
            new ManIngameWiki.WikiIconInfo(ManUI.inst.GetBlockRarityIcon(ManSpawn.inst.GetRarity((BlockTypes)blockID)), 
                StringLookup.GetBlockRarityName(ManSpawn.inst.GetRarity((BlockTypes)blockID))).
                OnGUILarge(AltUI.TextfieldBorderedBlue, AltUI.LabelWhite);
            GUILayout.EndHorizontal();
            var BA = ManSpawn.inst.GetBlockAttributes((BlockTypes)blockID);
            if (BA != null && BA.Any())
            {
                if (AttributesShown)
                {
                    GUILayout.BeginVertical(AltUI.BoxBlack);
                    if (GUILayout.Button("Attributes: Shown"))
                        AttributesShown = !AttributesShown;
                    foreach (var item in BA)
                    {
                        new ManIngameWiki.WikiIconInfo(ManUI.inst.GetBlockAttributeIcon(item), StringLookup.GetBlockAttribute(item)).
                            OnGUILarge(AltUI.TextfieldBorderedBlue, AltUI.LabelWhite);
                    }
                    GUILayout.EndVertical();
                }
                else
                {
                    if (GUILayout.Button("Attributes: Hidden"))
                        AttributesShown = !AttributesShown;
                }
            }
            GUILayout.EndVertical();

            if (AdditionalDisplayOnUI.HasSubscribers())
                AdditionalDisplayOnUI.Send((BlockTypes)blockID);

            if (mainInfo != null)
                mainInfo.DisplayGUI();
            if (damage != null)
                damage.DisplayGUI();
            if (damageable != null)
                damageable.DisplayGUI();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            if (ActiveGameInterop.IsReady && GUILayout.Button("Try Export GameObject hierarchy"))
                ActiveGameInterop.TransmitBlock(ManSpawn.inst.GetBlockPrefab((BlockTypes)blockID));

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
            if (ManIngameWiki.ShowJSONExport)
            {
                if (AltUI.Button("ENTIRE BLOCK JSON to system clipboard", ManSFX.UISfxType.Craft))
                {
                    TankBlock TB = ManSpawn.inst.GetBlockPrefab((BlockTypes)blockID);
                    AutoDataExtractor.clipboard.Clear();
                    GameObjectDocumentator.GetStrings(TB.gameObject, AutoDataExtractor.clipboard, 0, SlashState.None);
                    GUIUtility.systemCopyBuffer = AutoDataExtractor.clipboard.ToString();
                    ManSFX.inst.PlayUISFX(ManSFX.UISfxType.SaveGameOverwrite);
                }
            }
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


    }
}
