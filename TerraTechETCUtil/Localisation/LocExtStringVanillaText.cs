using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static BlockPlacementCollector.Collection;
using static LocalisationEnums;

namespace TerraTechETCUtil
{
    public class LocExtStringVanillaText : LocExtString, ILocExtStringLSAble
    {
        private string data;
        public readonly string English;
        public readonly string Bank;
        public readonly string ID;
        /// <summary>
        /// ONLY FOR VANILLA ENTRY REFERENCING
        /// </summary>
        /// <param name="englishFallback"></param>
        /// <param name="bank"></param>
        /// <param name="stringID"></param>
        public LocExtStringVanillaText(string englishFallback, string bank, string stringID)
        {
            LocalisationExt.InsureInit();
            LocalisationExt.OnLOCChanged.Subscribe(OnLOCChange);
            English = englishFallback;
            Bank = bank;
            ID = stringID;
            OnLOCChange(Localisation.inst.CurrentLanguage);
        }
        public LocExtStringVanillaText(string englishFallback, LocalisedString LS)
        {
            LocalisationExt.InsureInit();
            LocalisationExt.OnLOCChanged.Subscribe(OnLOCChange);
            English = englishFallback;
            Bank = LS.m_Bank;
            ID = LS.m_Id;
            OnLOCChange(Localisation.inst.CurrentLanguage);
        }
        public LocExtStringVanillaText(LocalisedString LS)
        {
            LocalisationExt.InsureInit();
            LocalisationExt.OnLOCChanged.Subscribe(OnLOCChange);
            Bank = LS.m_Bank;
            ID = LS.m_Id;
            English = Localisation.inst.GetLocalisedString(Bank, ID, LocalisationExt.emptyGlyphs);
            OnLOCChange(Localisation.inst.CurrentLanguage);
        }
        private void OnLOCChange(Languages newLang)
        {
            try
            {
                data = Localisation.inst.GetLocalisedString(Bank, ID, LocalisationExt.emptyGlyphs);
                if (data.NullOrEmpty())
                    data = GetEnglish();
                //LocalisationExt.TryGetFrom(IDCategory, IDIndex, ref data);
            }
            catch
            {
                data = GetEnglish();
            }
        }
        public override string ToString() => data;
        public override string GetEnglish() => English;
        /// <summary>
        /// LOSSY - m_InlineGlyphs was not transferred!!!
        /// </summary>
        /// <param name="guiExpanded"></param>
        /// <returns></returns>
        public LocalisedString CreateNewLocalisedString(bool guiExpanded = true)
        {
            return new LocalisedString
            {
                m_Bank = Bank,
                m_Id = ID,
                m_GUIExpanded = guiExpanded,
                m_InlineGlyphs = LocalisationExt.emptyGlyphs,
            };
        }
        /// <summary>
        /// LOSSY - m_InlineGlyphs was not transferred!!!
        /// </summary>
        /// <param name="guiExpanded"></param>
        /// <returns></returns>
        public void SetLocalisedString(LocalisedString inst)
        {
            inst.m_Bank = Bank;
            inst.m_Id = ID;
            inst.m_InlineGlyphs = LocalisationExt.emptyGlyphs;
        }
        /// <summary>
        /// LOSSY - m_InlineGlyphs was not transferred!!!
        /// </summary>
        /// <param name="guiExpanded"></param>
        /// <returns></returns>
        public void SetTextAuto(TooltipComponent TC)
        {
            LocalisationExt.textSet.SetValue(TC, true);
            LocalisationExt.GetLoc.SetValue(TC, CreateNewLocalisedString());
        }
    }
}
