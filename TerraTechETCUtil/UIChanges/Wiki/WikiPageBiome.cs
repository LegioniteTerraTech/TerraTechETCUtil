using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TerraTechETCUtil
{
    public class WikiPageBiome : ManIngameWiki.WikiPage
    {
        public static Func<Biome, string> GetBiomeName = GetBiomeNameDefault;
        public static Func<Biome, string> GetBiomeModName = GetBiomeModNameDefault;
        public static Func<Biome, string> GetBiomeDescription = GetBiomeDescriptionDefault;
        public static string GetBiomeDescriptionDefault(Biome biomeInst)
        {
            
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
        public Biome biomeInst;
        public string desc = "unset";
        internal BiomeDataInfo mainInfo;
        internal BiomeDataInfo damage;
        internal BiomeDataInfo damageable;
        internal BiomeDataInfo[] modules;
        public WikiPageBiome(Biome BiomeInst) :
            base(GetBiomeModName(BiomeInst), GetBiomeName(BiomeInst),
            null, "Biomes", null)
        {
            if (BiomeInst == null)
                throw new NullReferenceException("BiomeInst");
            biomeInst = BiomeInst;
            desc = GetBiomeDescription(BiomeInst);
        }

        public WikiPageBiome(Biome BiomeInst, ManIngameWiki.WikiPageGroup group) : 
            base(GetBiomeModName(BiomeInst), GetBiomeName(BiomeInst), null, group)
        {
            if (BiomeInst == null)
                throw new NullReferenceException("BiomeInst");
            biomeInst = BiomeInst;
            desc = GetBiomeDescription(BiomeInst);
        }
        public static string CleanupName(string name)
        {
            if (name.NullOrEmpty())
                return "<NULL>";
            return name.Replace("SubBiome", string.Empty).Replace("Biome", string.Empty).
                Replace("Basic", string.Empty).Replace("_", string.Empty).SplitCamelCase();
        }
        public override void DisplaySidebar() => ButtonGUIDisp();
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
        public override void OnBeforeDisplay()
        {

        }
        private static List<BiomeDataInfo> Combiler = new List<BiomeDataInfo>();
        public override void DisplayGUI()
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
                                string nameCached = item2.name;
                                if (prefabBase != null && prefab)
                                {
                                    foreach (var item3 in prefabBase)
                                    {
                                        if (item3.Key == BT && !links.Exists(x => x.linked.name == nameCached))
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
            GUILayout.EndVertical();
            if (desc != null)
                GUILayout.Label(desc, AltUI.TextfieldBlackHuge);
        }

        public static string GetBiomeNameDefault(Biome biomeInst)
        {
            return CleanupName(biomeInst.name.ToString());
        }
        public static string GetBiomeModNameDefault(Biome biomeInst)
        {
            return ManIngameWiki.VanillaGameName;
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
