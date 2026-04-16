using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Text;
using UnityEngine;
using static LocalisationEnums;
using static TerraTechETCUtil.ManIngameWiki;

namespace TerraTechETCUtil
{
    /// <inheritdoc cref="ManIngameWiki.WikiPage"/>
    /// <summary>
    /// <para>Wiki page for biome information</para>
    /// </summary>
    public class WikiPageBiome : ManIngameWiki.WikiPage
    {
        /// <summary>
        /// An additional event displayer for mods to add their own wiki data
        /// </summary>
        public static Event<Biome> AdditionalDisplayOnUI = new Event<Biome>();

        /// <summary>
        /// Attach a function here to get generated data for JSON serialization
        /// </summary>
        public static Func<Biome, string> GetBiomeData = null;
        /// <summary>
        /// Attach a function here to set the display name for the page in the GUI
        /// </summary>
        public static Func<Biome, string> GetBiomeName = GetBiomeNameDefault;
        /// <summary>
        /// Attach a function here to set the mod name for the page in the GUI
        /// </summary>
        public static Func<Biome, string> GetBiomeModName = GetBiomeModNameDefault;
        /// <summary>
        /// Attach a function here to set the custom description for this
        /// </summary>
        public static Func<Biome, string> GetBiomeDescription = GetBiomeDescriptionDefault;
        /// <summary>
        /// The default subject name determining function. 
        /// <para>Be sure to integrate this in your own if you plan on replacing it</para>
        /// </summary>
        /// <param name="biomeInst">The biome this is for</param>
        /// <returns>The display name for this</returns>
        public static string GetBiomeNameDefault(Biome biomeInst)
        {
            //new LocExtString(LocalisationEnums.StringBanks., corpID)
            return CleanupBiomeName(biomeInst.name.ToString());
        }
        /// <summary>
        /// The default mod name determining function. 
        /// <para>Be sure to integrate this in your own if you plan on replacing it</para>
        /// </summary>
        /// <param name="biomeInst">The biome this is for</param>
        /// <returns>The ModID this is affiliated with</returns>
        public static string GetBiomeModNameDefault(Biome biomeInst)
        {
            return ManIngameWiki.VanillaGameName;
        }

        /// <summary>
        /// The default description display function. 
        /// <para>Be sure to integrate this in your own if you plan on replacing it</para>
        /// </summary>
        /// <param name="biomeInst">The biome this is for</param>
        /// <returns>The description</returns>
        public static string GetBiomeDescriptionDefault(Biome biomeInst)
        {
            //LocalisationEnums.GetLocalisedString()


            switch (biomeInst.BiomeType)
            {
                case BiomeTypes.Grassland:
                    return "Nice, fairly traverse-able terrain for budding prospectors.  Invaders do not appear here.";
                case BiomeTypes.Desert:
                    return "Smooth, lumpy dunes of sand make for a spicy ride, but scarce trees leave you in the open.";
                case BiomeTypes.SaltFlats:
                    return "Nearly flat, perfect for testing out bigger Techs or building level fortresses.";
                case BiomeTypes.Mountains:
                    return "Sharp, jagged cleaves make traversal difficult, but it comes with nice Geothermal Vents. " +
                        "Whatever can resist getting stuck is the best option.";
                case BiomeTypes.Pillars:
                    return "Mysterious pillars keep it interesting, but they make it tough to navigate with anything large. Small will have to do.";
                case BiomeTypes.Ice:
                    return "Painfully slippery, many reconsider even entering this area.  Hovers are at their best here.";
                default:
                    return "Nothing is known about this place, enter at your own risk.";
            }
        }
        /// <summary>
        /// The main identification the game uses for this
        /// </summary>
        public Biome biomeInst;
        /// <summary>
        /// Displayed description
        /// </summary>
        public string desc = "unset";
        internal BiomeDataInfo mainInfo;
        internal BiomeDataInfo damage;
        internal BiomeDataInfo damageable;
        internal BiomeDataInfo[] modules;

        /// <inheritdoc cref="ManIngameWiki.WikiPage.WikiPage(string, LocExtString, Sprite, ManIngameWiki.WikiPageGroup)"/>
        /// <summary>
        /// 
        /// </summary>
        /// <param name="BiomeInst">The biome instance to affiliate with this page</param>
        public WikiPageBiome(Biome BiomeInst) :
            base(GetBiomeModName(BiomeInst), GetBiomeName(BiomeInst),
            null, ManIngameWiki.LOC_Biomes, null)
        {
            if (BiomeInst == null)
                throw new NullReferenceException("BiomeInst");
            biomeInst = BiomeInst;
            desc = GetBiomeDescription(BiomeInst);
        }

        /// <inheritdoc cref=" WikiPageBiome.WikiPageBiome(Biome)"/>
        public WikiPageBiome(Biome BiomeInst, ManIngameWiki.WikiPageGroup group) : 
            base(GetBiomeModName(BiomeInst), GetBiomeName(BiomeInst), null, group)
        {
            if (BiomeInst == null)
                throw new NullReferenceException("BiomeInst");
            biomeInst = BiomeInst;
            desc = GetBiomeDescription(BiomeInst);
        }
        /// <inheritdoc/>
        protected override WikiPageGroup InsureWikiGroup(string ModID, LocExtString WikiGroupName, Sprite Icon = null) =>
            InsureBiomesWikiGroup(ModID);
        /// <inheritdoc/>
        protected override WikiPageGroup InsureWikiGroup(string ModID, string WikiGroupName, Sprite Icon = null) =>
            InsureBiomesWikiGroup(ModID);

        /// <inheritdoc/>
        public override void GetIcon() { }
        /// <summary>
        /// Cleanup the biome name of other unnesseary words
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string CleanupBiomeName(string name)
        {
            if (name.NullOrEmpty())
                return "<NULL>";
            return name.Replace("SubBiome", string.Empty).Replace("Biome", string.Empty).
                Replace("Basic", string.Empty).Replace("_", string.Empty).SplitCamelCase();
        }
        /// <inheritdoc/>
        public override void DisplaySidebar() => ButtonGUIDisp();
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
        /// <inheritdoc/>
        public override void OnBeforeDisplay()
        {

        }
        private static List<BiomeDataInfo> Combiler = new List<BiomeDataInfo>();
        /// <inheritdoc/>
        protected override void DisplayGUI()
        {
            if (modules == null)
            {
                BiomeTypes BT = biomeInst.BiomeType;

                try
                {
                    var modulesC = BiomeDataInfo.TryGetModules(biomeInst);
                    foreach (var item in modulesC)
                    {
                        if (item.name == "Biome")
                        {
                            item.infos.Add("Base Name", biomeInst.name == null ? "<NULL>" : biomeInst.name);
                            item.infos.Add("Biome ID", BT.ToString());
                            item.infos.Add("Is Vanilla", (int)BT <= Enum.GetValues(typeof(BiomeTypes)).Length);
                            item.infos.Add("Has Traders", biomeInst.AllowVendors);
                            item.infos.Add("Artifacts", biomeInst.AllowLandmarks);
                            item.infos.Add("Safe for Stunts", biomeInst.AllowStuntRamps);
                            item.infos.Add("Grippiness", biomeInst.SurfaceFriction);

                            List<ManIngameWiki.WikiLink> links = new List<ManIngameWiki.WikiLink>();
                            foreach (var item2 in IterateForInfo<WikiPageScenery>())
                            {
                                var prefabBase = item2.prefabBase;
                                var prefab = item2.prefab;
                                string nameCached = item2.title;
                                if (prefabBase != null && prefab)
                                {
                                    foreach (var item3 in prefabBase)
                                    {
                                        if (item3.Key == biomeInst.name && !links.Exists(x => x.linked.title == nameCached))
                                            links.Add(new ManIngameWiki.WikiLink(item2));
                                    }
                                }
                            }
                            item.infos.Add("Scenery", links);

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
            GUILayout.BeginVertical(AltUI.TextfieldBordered);
            if (mainInfo != null)
                mainInfo.DisplayGUI();
            if (damage != null)
                damage.DisplayGUI();
            if (damageable != null)
                damageable.DisplayGUI();
            if (AdditionalDisplayOnUI.HasSubscribers())
                AdditionalDisplayOnUI.Send(biomeInst);
            GUILayout.EndVertical();
            if (desc != null)
                GUILayout.Label(desc, AltUI.TextfieldBlackHuge);
            if (ManIngameWiki.ShowJSONExport && GetBiomeData != null && biomeInst != null)
            {
                if (AltUI.Button("ENTIRE BIOME JSON to system clipboard", ManSFX.UISfxType.Craft))
                {
                    AutoDataExtractor.clipboard.Clear();
                    AutoDataExtractor.clipboard.Append(GetBiomeData.Invoke(biomeInst));
                    GUIUtility.systemCopyBuffer = AutoDataExtractor.clipboard.ToString();
                    ManSFX.inst.PlayUISFX(ManSFX.UISfxType.SaveGameOverwrite);
                }
            }
        }


        internal class BiomeDataInfo : AutoDataExtractor
        {
            private static HashSet<object> grabbed = new HashSet<object>();
            private static List<BiomeDataInfo> cacher = new List<BiomeDataInfo>();
            internal static BiomeDataInfo[] TryGetModules(Biome toExplore)
            {
                try
                {
                    Type typeCase = toExplore.GetType();
                    if (grabbed.Contains(toExplore))
                        return new BiomeDataInfo[] { };
                    grabbed.Add(toExplore);
                    cacher.Add(new BiomeDataInfo(typeCase, toExplore, grabbed));
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
            public BiomeDataInfo(Type grabbedType, object prefab, HashSet<object> grabbedAlready) : base (
                SpecialNames.TryGetValue(grabbedType.Name, out string altName) ? altName : 
                grabbedType.Name.Replace("Module", "").ToString().SplitCamelCase(), 
                grabbedType, prefab, grabbedAlready)
            {
            }
        }
    }
}
