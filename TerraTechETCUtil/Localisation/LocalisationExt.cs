using FMOD;
using FMOD.Studio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static LocalisationEnums;

namespace TerraTechETCUtil
{
    public static class LocalisationExt
    {
        /// <summary>
        /// For anything that should be rigistered that DOES NOT have an existing ID to assign to in StringBanks
        /// </summary>
        public const StringBanks LOC_ExtGeneralID = (StringBanks)int.MinValue;
        public const Languages defaultLanguage = Languages.US_English;
        public const Languages defaultLanguage2 = Languages.English;
        public const string ModTag = "MOD";
        public const string StringFailed = "LOC_Failiure";

        internal static Event<Languages> OnLOCChanged = new Event<Languages>();
        private static bool Init = false;
        private static Dictionary<StringBanks, LocExtCategory> LOCExt = null;
        private static Dictionary<StringBanks, LocExtCategory> LOCExtDefault = null;
        private static Dictionary<Languages, Dictionary<StringBanks, LocExtCategory>> LOCLang =
            new Dictionary<Languages, Dictionary<StringBanks, LocExtCategory>>();

        static LocalisationExt()
        {
            LOCLang.Add(defaultLanguage, new Dictionary<StringBanks, LocExtCategory>());
            LOCExtDefault = LOCLang[defaultLanguage];
            LOCExt = LOCExtDefault;
        }

        internal static void InsureInit()
        {
            if (Init)
                return;
            LegModExt.InsurePatches();
            try
            {
                if (!LOCLang.ContainsKey(Localisation.inst.CurrentLanguage))
                    LOCLang.Add(Localisation.inst.CurrentLanguage, new Dictionary<StringBanks, LocExtCategory>());
                LOCExt = LOCLang[Localisation.inst.CurrentLanguage];
                Localisation.inst.OnLanguageChanged.Subscribe(OnLanguageChanged);
                Init = true;
            }
            catch (Exception e)
            {
                LOCExt = LOCExtDefault;
                Debug_TTExt.Log("Failed to init LocalisationExt.  Were we called too early?! - " + e);
            }
        }

        private static void OnLanguageChanged()
        {
            if (LOCLang.TryGetValue(Localisation.inst.CurrentLanguage, out Dictionary<StringBanks, LocExtCategory> LOCLangBank))
            {
                LOCExt = LOCLangBank;
            }
            else
            {
                LOCExt = LOCExtDefault;
            }
            OnLOCChanged.Send(Localisation.inst.CurrentLanguage);
        }

        public static IEnumerable<Languages> IterateRegisteredLanguages()
        {
            foreach (var item in LOCLang)
            {
                yield return item.Key;
            }
        }
        public static bool TryGetFrom(StringBanks category, int ID, ref string result)
        {
            if (LOCExt.TryGetValue(category, out LocExtCategory LEC) &&
                LEC.bank.TryGetValue(ID, out string LES))
            {
                result = LES;
                return true;
            }
            else if (LOCExtDefault.TryGetValue(category, out LEC) &&
                LEC.bank.TryGetValue(ID, out LES))
            {
                result = LES;
                return true;
            }
            return false;
        }
        public static bool TryGetFromLang(Languages lang, StringBanks category, int ID, ref string result)
        {
            if (!LOCLang.TryGetValue(lang, out Dictionary<StringBanks, LocExtCategory> LOCExtS))
            {
                return false;
            }
            if (LOCExtS.TryGetValue(category, out LocExtCategory LEC) &&
                LEC.bank.TryGetValue(ID, out string LES))
            {
                result = LES.ToString();
                return true;
            }
            return false;
        }
        internal static void DoReplace(Languages lang, StringBanks category, int ID, string String)
        {
            if (!LOCLang.TryGetValue(lang, out Dictionary<StringBanks, LocExtCategory> LOCExtS))
            {
                LOCExtS = new Dictionary<StringBanks, LocExtCategory>();
                LOCLang.Add(lang, LOCExtS);
            }
            if (!LOCExtS.TryGetValue(category, out LocExtCategory LEC))
            {
                LEC = new LocExtCategory();
                LOCExtS.Add(category, LEC);
            }
            if (LEC.bank.ContainsKey(ID))
                LEC.bank.Remove(ID);
            LEC.bank.Add(ID, String);
        }


        public static void RegisterRawEng(StringBanks category, int ID, string String)
        {
            if (!LOCExtDefault.TryGetValue(category, out LocExtCategory LEC))
            {
                LEC = new LocExtCategory();
                LOCExtDefault.Add(category, LEC);
            }
            if (!LEC.bank.ContainsKey(ID))
            {
                LEC.bank.Add(ID, String);
            }
            else
                throw new Exception("Failed to insert localizationExt " + String + " of ID " +
                    ID + " of category " + category + " for language US_English");
        }

        public static void RegisterEnglish(StringBanks category, string String)
        {
            RegisterEnglishGetID(category, String);
        }
        internal static int RegisterEnglishGetID(StringBanks category, string String)
        {
            if (!LOCExtDefault.TryGetValue(category, out LocExtCategory LEC))
            {
                LEC = new LocExtCategory();
                LOCExtDefault.Add(category, LEC);
            }
            int IDSelect = LEC.NextID;
            LEC.bank.Add(IDSelect, String);
            LEC.NextID++;
            return IDSelect;
        }
        internal static void RegisterOtherLang(Languages lang, StringBanks category, int ID, string String)
        {
            InsureInit();
            if (!LOCLang.TryGetValue(lang, out Dictionary<StringBanks, LocExtCategory> LOCExtS))
            {
                LOCExtS = new Dictionary<StringBanks, LocExtCategory>();
                LOCLang.Add(lang, LOCExtS);
            }
            if (!LOCExtS.TryGetValue(category, out LocExtCategory LEC))
            {
                LEC = new LocExtCategory();
                LOCExtS.Add(category, LEC);
            }
            if (!LEC.bank.ContainsKey(ID))
            {
                LEC.bank.Add(ID, String);
            }
            else
                Debug_TTExt.Assert("Failed to insert localizationExt " + String + " of ID " +
                    ID + " of category " + category + " for language " + lang.ToString());
        }
        public static void ResetLookupList()
        {
            foreach (var item in LOCExt)
            {
                item.Value.bank.Clear();
            }
        }



        internal static string FindEnglish(Dictionary<Languages, string> stringByLang)
        {
            if (stringByLang.TryGetValue(defaultLanguage, out string val))
                return val;
            else if (stringByLang.TryGetValue(defaultLanguage2, out val))
            {
                Debug_TTExt.Log("Adding duplicate English localisation that is set to English to US_English - this wastes memory!");
                return val;
            }
            else
                throw new ArgumentException("Cannot add localisation for strings that do not have any form of English translation!");
        }

        internal static int RegisterStringDirect_Internal(Dictionary<Languages, string> stringByLang, StringBanks category = LOC_ExtGeneralID)
        {
            int retID = RegisterEnglishGetID(category, FindEnglish(stringByLang));
            foreach (var item in stringByLang)
            {
                if (item.Key != defaultLanguage)
                    RegisterOtherLang(item.Key, category, retID, item.Value);
            }
            return retID;
        }



        public static LocExtStringVanillaText GetLocExtStringLossy(this LocalisedString TC)
        {
            return new LocExtStringVanillaText(TC);
        }



        internal static FieldInfo textSet = typeof(TooltipComponent).GetField("m_ManuallySetText", BindingFlags.NonPublic | BindingFlags.Instance);
        internal static FieldInfo GetLoc = typeof(TooltipComponent).GetField("m_LocalisedString", BindingFlags.Instance | BindingFlags.NonPublic);
        internal static Localisation.GlyphInfo[] emptyGlyphs = new Localisation.GlyphInfo[0];
        public static void SetTextAuto(this string inString, TooltipComponent TC)
        {
            textSet.SetValue(TC, false);
            TC.SetText(inString);
        }
        public static LocalisedString CreateLocalisedString(this string inst, bool guiExpanded = true)
        {
            return new LocalisedString
            {
                m_Bank = "MOD",
                m_Id = inst,
                m_GUIExpanded = guiExpanded,
                m_InlineGlyphs = emptyGlyphs,
            };
        }

    }

    public interface ILocExtString
    {
        string ToString();
        string GetEnglish();
    }
    public interface ILocExtStringLSAble : ILocExtString
    {
        LocalisedString CreateNewLocalisedString(bool guiExpanded = true);
        void SetLocalisedString(LocalisedString LS);
        void SetTextAuto(TooltipComponent TC);
    }
    public interface ILocExtStringMod : ILocExtString
    {
        /// <summary>
        /// Returns only if EXACT language match exists, without doing fallback to english!
        /// </summary>
        /// <param name="lang"></param>
        /// <param name="output"></param>
        /// <returns></returns>
        bool TryLookup(Languages lang, out string output);
        void ChangeEnglish(string newDesc);
        void Change(Languages lang, string newDesc);
        IEnumerable<KeyValuePair<Languages, string>> IterateLanguages();
    }

    /// <summary>
    /// Represents a multi-lingual string instance that is managed by the base game
    /// </summary>
    public abstract class LocExtString : ILocExtString
    {
        public abstract string ToString();
        public abstract string GetEnglish();
        public static implicit operator string(LocExtString s) => s.ToString();
    }


    public class LocExtCategory
    {
        public int NextID;
        public Dictionary<int, string> bank = new Dictionary<int, string>();
        public LocExtCategory()
        {
            NextID = 9001;
        }
        /// <summary>
        /// FOR THE ENUM TYPE THAT LISTS EVERY POSSIBLE OUTPUT FOR THIS STRING
        /// </summary>
        /// <param name="enumType"></param>
        public LocExtCategory(Type enumType)
        {
            NextID = Enum.GetValues(enumType).Length + 1000;
        }
    }
}
