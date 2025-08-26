using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FMOD.Studio;
using UnityEngine;
using static LocalisationEnums;

namespace TerraTechETCUtil
{
    /// <summary>
    /// Does NOT support vanilla referencing
    /// </summary>
    public class LocExtStringMod : LocExtString, ILocExtStringMod, ILocExtStringLSAble
    {
        private string data;
        public readonly int IDIndex;
        public readonly StringBanks IDCategory;
        public LocExtStringMod(Dictionary<Languages, string> stringByLang, StringBanks category = LocalisationExt.LOC_ExtGeneralID)
        {
            LocalisationExt.InsureInit();
            LocalisationExt.OnLOCChanged.Subscribe(OnLOCChange);
            IDCategory = category;
            IDIndex = LocalisationExt.RegisterStringDirect_Internal(stringByLang, category);
            LocalisationExt.TryGetFrom(category, IDIndex, ref data);
        }
        public LocExtStringMod(string englishTranslation, StringBanks category = LocalisationExt.LOC_ExtGeneralID)
        {
            LocalisationExt.InsureInit();
            LocalisationExt.OnLOCChanged.Subscribe(OnLOCChange);
            IDCategory = category;
            IDIndex = LocalisationExt.RegisterEnglishGetID(category, englishTranslation);
            LocalisationExt.TryGetFrom(category, IDIndex, ref data);
        }
        /// <summary>
        /// ONLY FOR VANILLA ENTRY REFERENCING
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
        public override string ToString() => data;
        /// <summary>
        /// DOES NOT WORK FOR VANILLA ENTRY REFERENCING
        /// </summary>
        public override string GetEnglish()
        {
            string outp = string.Empty;
            if (!LocalisationExt.TryGetFromLang(LocalisationExt.defaultLanguage, IDCategory, IDIndex, ref outp))
                throw new ArgumentException("Could not get defaults " + LocalisationExt.defaultLanguage + " for target LocExtString of ID" + IDIndex +
                    ", category " + IDCategory);
            return outp;
        }
        public bool TryLookup(Languages lang, out string output)
        {
            output = string.Empty;
            return LocalisationExt.TryGetFromLang(lang, IDCategory, IDIndex, ref output);
        }
        public void ChangeEnglish(string newDesc)
        {
            Change(LocalisationExt.defaultLanguage, newDesc);
        }
        public void Change(Languages lang, string newDesc)
        {
            LocalisationExt.DoReplace(lang, IDCategory, IDIndex, newDesc);
        }
        public IEnumerable<KeyValuePair<Languages, string>> IterateLanguages()
        {
            string result = string.Empty;
            foreach (var item in LocalisationExt.IterateRegisteredLanguages())
            {
                if (LocalisationExt.TryGetFromLang(item, IDCategory, IDIndex, ref result))
                    yield return new KeyValuePair<Languages, string>(item, result);
            }
        }
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
        public void SetLocalisedString(LocalisedString inst)
        {
            inst.m_Bank = LocalisationExt.ModTag + IDIndex;
            inst.m_Id = ToString();
            inst.m_InlineGlyphs = LocalisationExt.emptyGlyphs;
        }
        public void SetTextAuto(TooltipComponent TC)
        {
            LocalisationExt.textSet.SetValue(TC, true);
            LocalisationExt.GetLoc.SetValue(TC, CreateNewLocalisedString());
        }
    }
}
