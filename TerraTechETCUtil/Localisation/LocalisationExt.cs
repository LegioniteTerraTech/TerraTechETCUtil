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
    /// <summary>
    /// Localization for mods.
    /// <para>Does not support <see cref="Localisation.GlyphInfo"/>, aka inlined dynamic text</para>
    /// </summary>
    public static class LocalisationExt
    {
        /// <summary>
        /// For anything that should be registered that DOES NOT have an existing ID to assign to in StringBanks
        /// </summary>
        public const StringBanks LOC_ExtGeneralID = (StringBanks)int.MinValue;
        /// <summary>
        /// The default language that <see cref="LocalisationExt"/> uses as fallback
        /// </summary>
        public const Languages defaultLanguage = Languages.US_English;
        /// <summary>
        /// The second default language that <see cref="LocalisationExt"/> uses as fallback
        /// </summary>
        public const Languages defaultLanguage2 = Languages.English;
        /// <summary>
        /// The tag applied to added stringbanks to flag them as modded
        /// </summary>
        public const string ModTag = "_MOD";
        /// <summary>
        /// This is returned if the targeted string doesn't exist
        /// </summary>
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

        /// <summary>
        /// Iterates all languages that have at least one entry in the <see cref="LocalisationExt"/> system
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<Languages> IterateRegisteredLanguages()
        {
            foreach (var item in LOCLang)
            {
                yield return item.Key;
            }
        }
        /// <summary>
        /// Use <see cref="StringLookup"/> or <see cref="Localisation.GetLocalisedString(string, string, Localisation.GlyphInfo[])"/> instead of this whenever possible!
        /// <para>Find a localised string based on a <see cref="StringBanks"/> category</para>
        /// </summary>
        /// <param name="category"></param>
        /// <param name="ID"></param>
        /// <param name="result"></param>
        /// <returns>True if string found, else false</returns>
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
        /// <summary>
        /// Use <see cref="StringLookup"/> or <see cref="Localisation.GetLocalisedString(string, string, Localisation.GlyphInfo[])"/> instead of this whenever possible!
        /// <para>Find a localised string based on a <see cref="StringBanks"/> category for a specified <see cref="Languages"/> </para>
        /// </summary>
        /// <param name="lang"></param>
        /// <param name="category"></param>
        /// <param name="ID"></param>
        /// <param name="result"></param>
        /// <returns>True if string found, else false</returns>
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

        /// <summary>
        /// Register an English translation fallback for use in the <see cref="StringLookup"/> system
        /// </summary>
        /// <param name="category"></param>
        /// <param name="ID"></param>
        /// <param name="String"></param>
        /// <exception cref="Exception"></exception>
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

        /// <summary>
        /// Register an English translation fallback for use in the <see cref="StringLookup"/> system.
        /// Gets the ID automatically
        /// </summary>
        /// <param name="category"></param>
        /// <param name="String"></param>
        /// <exception cref="Exception"></exception>
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
        /// <summary>
        /// Resets <b>ALL</b> lookups in <see cref="LocalisationExt"/>! Do not use unless ABSOLUTELY NECESSARY!
        /// </summary>
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


        /// <summary>
        /// Convert a <see cref="LocalisedString"/> into a <see cref="LocExtStringVanillaText"/>, which loses the Glyphs function.
        /// </summary>
        /// <param name="TC"></param>
        /// <returns></returns>
        public static LocExtStringVanillaText GetLocExtStringLossy(this LocalisedString TC)
        {
            return new LocExtStringVanillaText(TC);
        }



        internal static FieldInfo textSet = typeof(TooltipComponent).GetField("m_ManuallySetText", BindingFlags.NonPublic | BindingFlags.Instance);
        internal static FieldInfo GetLoc = typeof(TooltipComponent).GetField("m_LocalisedString", BindingFlags.Instance | BindingFlags.NonPublic);
        internal static Localisation.GlyphInfo[] emptyGlyphs = new Localisation.GlyphInfo[0];
        /// <summary>
        /// Change the given <see cref="TooltipComponent"/> to display <paramref name="inString"/>
        /// </summary>
        /// <param name="inString">To replace the <see cref="TooltipComponent"/>'s description with</param>
        /// <param name="TC">The target</param>
        public static void SetTextAuto(this string inString, TooltipComponent TC)
        {
            textSet.SetValue(TC, false);
            TC.SetText(inString);
        }
        /// <summary>
        /// Creates a vanilla <see cref="LocalisedString"/> for immedeate, temporary use.  
        /// <para><b>Does not register it.</b></para>
        /// </summary>
        /// <param name="inst"></param>
        /// <param name="guiExpanded"></param>
        /// <returns></returns>
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

    /// <summary>
    /// Interface for a mod localized string for <see cref="LocalisationExt"/>
    /// </summary>
    public interface ILocExtString
    {
        /// <summary>
        /// Gets the localization of this
        /// </summary>
        string ToString();
        /// <summary>
        /// Gets the default English fallback of this.  Always exists.
        /// </summary>
        string GetEnglish();
    }
    /// <summary>
    /// A <see cref="ILocExtString"/> that has replacable contents.
    /// </summary>
    public interface ILocExtStringLSAble : ILocExtString
    {
        /// <summary>
        /// Converts it to a vanilla-style <see cref="LocalisedString"/>
        /// </summary>
        /// <param name="guiExpanded"></param>
        /// <returns></returns>
        LocalisedString CreateNewLocalisedString(bool guiExpanded = true);
        /// <summary>
        /// Set it with the contents of the given <see cref="LocalisedString"/>
        /// </summary>
        void SetLocalisedString(LocalisedString LS);
        /// <summary>
        /// Set it with the contents of the given <see cref="TooltipComponent"/>
        /// </summary>
        void SetTextAuto(TooltipComponent TC);
    }
    /// <summary>
    /// A <see cref="ILocExtString"/> for mods
    /// </summary>
    public interface ILocExtStringMod : ILocExtString
    {
        /// <summary>
        /// Returns only if EXACT language match exists, without doing fallback to english!
        /// </summary>
        /// <param name="lang"></param>
        /// <param name="output"></param>
        /// <returns>True if a valid translation was found, false for fallback English</returns>
        bool TryLookup(Languages lang, out string output);
        /// <summary>
        /// Change the default English string for this
        /// </summary>
        /// <param name="newDesc"></param>
        void ChangeEnglish(string newDesc);
        /// <summary>
        /// Change the target language string for this
        /// </summary>
        /// <param name="lang"></param>
        /// <param name="newDesc"></param>
        void Change(Languages lang, string newDesc);
        /// <summary>
        /// Iterate all of the languages this supports
        /// </summary>
        /// <returns></returns>
        IEnumerable<KeyValuePair<Languages, string>> IterateLanguages();
    }

    /// <summary>
    /// Represents a multi-lingual string instance that is managed by the base game
    /// </summary>
    public abstract class LocExtString : ILocExtString
    {
        string ILocExtString.ToString() => ToString();
        /// <summary>
        /// Gets the localization of this
        /// </summary>
        public abstract new string ToString();
        /// <inheritdoc/>
        public abstract string GetEnglish();
        /// <inheritdoc/>
        public static implicit operator string(LocExtString s) => s.ToString();
    }

    /// <summary>
    /// Localised category
    /// </summary>
    public class LocExtCategory
    {
        /// <summary>
        /// Next free ID that can be assigned to
        /// </summary>
        public int NextID;
        /// <summary>
        /// The databank that stores all related strings
        /// </summary>
        public Dictionary<int, string> bank = new Dictionary<int, string>();
        /// <summary>
        /// Creates the category
        /// </summary>
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
