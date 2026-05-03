using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using FMOD.Studio;
using UnityEngine;
using static LocalisationEnums;

namespace TerraTechETCUtil
{
    /// <summary>
    /// <inheritdoc/>
    /// <para><b>Does NOT support vanilla referencing</b></para>
    /// <para>For vanilla's <see cref="LocalisedString"/> for use in modded interfaces, see <see cref="LocExtStringVanilla"/></para>
    /// </summary>
    public class LocExtStringMod : LocExtString, ILocExtStringMod, ILocExtStringLSAble
    {
        private string data;
        /// <summary>
        /// The auto-assigned ID Index for <see cref="LocalisationExt"/> to access this
        /// </summary>
        public readonly int IDIndex;
        /// <summary>
        /// The <see cref="StringBanks"/> category this was created with
        /// </summary>
        public readonly StringBanks IDCategory;
        /// <summary>
        /// Create a <see cref="LocExtStringMod"/> for localisation text in a mod
        /// </summary>
        /// <param name="stringByLang">Languages to string. 
        /// <para><b>MUST contain <see cref="Languages.English"/> or <see cref="Languages.US_English"/></b></para></param>
        /// <param name="category">Specific category to inject this into.  Leave unset to use like normal</param>
        public LocExtStringMod(Dictionary<Languages, string> stringByLang, StringBanks category = LocalisationExt.LOC_ExtGeneralID)
        {
            if (stringByLang == null)
                throw new ArgumentNullException(nameof(stringByLang));
            if (stringByLang.Count == 0)
                throw new ArgumentException(nameof(stringByLang) + " is empty.  Cannot leave empty.");
            if (!stringByLang.ContainsKey(Languages.US_English) && !stringByLang.ContainsKey(Languages.US_English))
                throw new ArgumentException(nameof(stringByLang) + " must contain a string for " + 
                    nameof(Languages.English) + " or " + nameof(Languages.US_English) + ".");
            LocalisationExt.InsureInit();
            LocalisationExt.OnLOCChanged.Subscribe(OnLOCChange);
            IDCategory = category;
            IDIndex = LocalisationExt.RegisterStringDirect_Internal(stringByLang, category);
            LocalisationExt.TryGetFrom(category, IDIndex, ref data);
        }
        /// <summary>
        /// Create a <see cref="LocExtStringMod"/> for localisation text in a mod
        /// <para>English-only quick version with no actual localisation</para>
        /// </summary>
        /// <param name="englishTranslation">The english lookup that will be used for all lookups. Cannot leave null or empty</param>
        /// <param name="category">Specific category to inject this into.  Leave unset to use like normal</param>
        public LocExtStringMod(string englishTranslation, StringBanks category = LocalisationExt.LOC_ExtGeneralID)
        {
            if (englishTranslation == null)
                throw new ArgumentNullException(nameof(englishTranslation));
            if (englishTranslation == string.Empty)
                throw new ArgumentException(nameof(englishTranslation) + " is empty.  Cannot leave empty.");
            LocalisationExt.InsureInit();
            LocalisationExt.OnLOCChanged.Subscribe(OnLOCChange);
            IDCategory = category;
            IDIndex = LocalisationExt.RegisterEnglishGetID(category, englishTranslation);
            LocalisationExt.TryGetFrom(category, IDIndex, ref data);
        }
        /// <summary>
        /// Create a <see cref="LocExtStringMod"/> for localisation text in a mod
        /// <para><b>ONLY FOR VANILLA ENTRY REFERENCING</b></para>
        /// </summary>
        /// <param name="category"></param>
        /// <param name="stringID"></param>
        protected LocExtStringMod(StringBanks category, int stringID)
        {
            LocalisationExt.InsureInit();
            LocalisationExt.OnLOCChanged.Subscribe(OnLOCChange);
            IDCategory = category;
            IDIndex = stringID;
            if (Localisation.inst != null)
                OnLOCChange(Localisation.inst.CurrentLanguage);
            else
                data = LocalisationExt.StringFailed;
        }

        private void OnLOCChange(Languages newLang)
        {
            try
            {
                data = Localisation.inst.GetLocalisedString(IDCategory, IDIndex, LocalisationExt.emptyGlyphs);
                //LocalisationExt.TryGetFrom(IDCategory, IDIndex, ref data);
            }
            catch
            {
                data = GetEnglish();
            }
        }
        /// <inheritdoc/>
        public override string ToString() => data;

        /// <summary>
        /// <inheritdoc/>
        /// <para><b>DOES NOT WORK FOR VANILLA ENTRY REFERENCING</b></para>
        /// </summary>
        public override string GetEnglish()
        {
            string outp = string.Empty;
            if (!LocalisationExt.TryGetFromLang(LocalisationExt.defaultLanguage, IDCategory, IDIndex, ref outp))
                throw new ArgumentException("Could not get defaults " + LocalisationExt.defaultLanguage + " for target LocExtString of ID" + IDIndex +
                    ", category " + IDCategory);
            return outp;
        }
        /// <summary>
        /// To test this for the output localised string
        /// </summary>
        /// <param name="lang"></param>
        /// <param name="output"></param>
        /// <returns>True if a valid translation was found, false for fallback English</returns>
        public bool TryLookup(Languages lang, out string output)
        {
            output = string.Empty;
            return LocalisationExt.TryGetFromLang(lang, IDCategory, IDIndex, ref output);
        }
        /// <inheritdoc/>
        public void ChangeEnglish(string newDesc)
        {
            Change(LocalisationExt.defaultLanguage, newDesc);
        }
        /// <inheritdoc/>
        public void Change(Languages lang, string newDesc)
        {
            LocalisationExt.DoReplace(lang, IDCategory, IDIndex, newDesc);
        }
        /// <inheritdoc/>
        public IEnumerable<KeyValuePair<Languages, string>> IterateLanguages()
        {
            string result = string.Empty;
            foreach (var item in LocalisationExt.IterateRegisteredLanguages())
            {
                if (LocalisationExt.TryGetFromLang(item, IDCategory, IDIndex, ref result))
                    yield return new KeyValuePair<Languages, string>(item, result);
            }
        }
        /// <inheritdoc/>
        public LocalisedString CreateNewLocalisedString(bool guiExpanded = true)
        {
            return new LocalisedString
            {
                m_Bank = LocalisationExt.ModTag + IDIndex,
                m_Id = ToString(),
                m_GUIExpanded = guiExpanded,
                m_InlineGlyphs = LocalisationExt.emptyGlyphs,
            };
        }
        /// <inheritdoc/>
        public void SetLocalisedString(LocalisedString inst)
        {
            inst.m_Bank = LocalisationExt.ModTag + IDIndex;
            inst.m_Id = ToString();
            inst.m_InlineGlyphs = LocalisationExt.emptyGlyphs;
        }
        /// <inheritdoc/>
        public void SetTextAuto(TooltipComponent TC)
        {
            LocalisationExt.textSet.SetValue(TC, true);
            LocalisationExt.GetLoc.SetValue(TC, CreateNewLocalisedString());
        }
    }
}
