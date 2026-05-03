using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static BlockPlacementCollector.Collection;
using static LocalisationEnums;

namespace TerraTechETCUtil
{
    /// <summary>
    /// <inheritdoc/>
    /// <para>Class to convert vanilla <see cref="LocalisedString"/>s to the mod <see cref="LocExtString"/> format</para>
    /// <para>For entries that are looked up using <see cref="Localisation.GetLocalisedString(string, string, Localisation.GlyphInfo[])"/></para>
    /// <para>For other cases, see <see cref="LocExtStringVanilla"/></para>
    /// </summary>
    public class LocExtStringVanillaText : LocExtString, ILocExtStringLSAble
    {
        private string data;
        /// <summary>
        /// English fallback
        /// </summary>
        public readonly string English;
        /// <summary>
        /// Lookup category string ID
        /// </summary>
        public readonly string Bank;
        /// <summary>
        /// The auto-assigned ID Index for <see cref="LocalisationExt"/> to access this
        /// </summary>
        public readonly string ID;
        /// <summary>
        /// Creates a <see cref="LocExtStringVanillaText"/> for displaying vanilla localized text on modded UI interfaces.
        /// <para>Registered automatically</para>
        /// <para><b>ONLY FOR VANILLA ENTRY REFERENCING</b></para>
        /// </summary>
        /// <param name="englishFallback"></param>
        /// <param name="bank"></param>
        /// <param name="stringID"></param>
        public LocExtStringVanillaText(string englishFallback, string bank, string stringID)
        {
            if (englishFallback == null)
                throw new ArgumentNullException(nameof(englishFallback));
            if (englishFallback == string.Empty)
                throw new ArgumentException(nameof(englishFallback) + " is empty.  Cannot leave empty.");
            if (bank == null)
                throw new ArgumentNullException(nameof(bank));
            if (bank == string.Empty)
                throw new ArgumentException(nameof(bank) + " is empty.  Cannot leave empty.");
            if (stringID == null)
                throw new ArgumentNullException(nameof(stringID));
            if (stringID == string.Empty)
                throw new ArgumentException(nameof(stringID) + " is empty.  Cannot leave empty.");
            LocalisationExt.InsureInit();
            LocalisationExt.OnLOCChanged.Subscribe(OnLOCChange);
            English = englishFallback;
            Bank = bank;
            ID = stringID;
            OnLOCChange(Localisation.inst.CurrentLanguage);
        }
        /// <summary>
        /// Creates a <see cref="LocExtStringVanillaText"/> for displaying vanilla localized text on modded UI interfaces.
        /// <para>Registered automatically</para>
        /// </summary>
        /// <param name="englishFallback">The english lookup that will be used for all lookups. Cannot leave null or empty</param>
        /// <param name="LS"><see cref="LocalisedString"/> to convert. Cannot leave null.</param>
        public LocExtStringVanillaText(string englishFallback, LocalisedString LS)
        {
            if (englishFallback == null)
                throw new ArgumentNullException(nameof(englishFallback));
            if (englishFallback == string.Empty)
                throw new ArgumentException(nameof(englishFallback) + " is empty.  Cannot leave empty.");
            if (LS == null)
                throw new ArgumentNullException(nameof(LS));
            LocalisationExt.InsureInit();
            LocalisationExt.OnLOCChanged.Subscribe(OnLOCChange);
            English = englishFallback;
            Bank = LS.m_Bank;
            ID = LS.m_Id;
            OnLOCChange(Localisation.inst.CurrentLanguage);
        }
        /// <summary>
        /// Creates a <see cref="LocExtStringVanillaText"/> for displaying vanilla localized text on modded UI interfaces.
        /// <para>Registered automatically</para>
        /// </summary>
        /// <param name="LS"><see cref="LocalisedString"/> to convert. Cannot leave null.</param>
        public LocExtStringVanillaText(LocalisedString LS)
        {
            if (LS == null)
                throw new ArgumentNullException(nameof(LS));
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
        /// <inheritdoc/>
        public override string ToString() => data;
        /// <inheritdoc/>
        public override string GetEnglish() => English;
        /// <summary>
        /// Convert this to a <see cref="LocalisedString"/>
        /// <para>LOSSY - m_InlineGlyphs is not transferred!!!</para>
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
        /// Set this to the contents of the given <see cref="LocalisedString"/>
        /// <para>LOSSY - m_InlineGlyphs is not transferred!!!</para>
        /// </summary>
        /// <param name="inst"></param>
        public void SetLocalisedString(LocalisedString inst)
        {
            inst.m_Bank = Bank;
            inst.m_Id = ID;
            inst.m_InlineGlyphs = LocalisationExt.emptyGlyphs;
        }
        /// <summary>
        /// Set this to the contents of the given <see cref="TooltipComponent"/>
        /// <para>LOSSY - m_InlineGlyphs is not transferred!!!</para>
        /// </summary>
        /// <param name="TC"></param>
        public void SetTextAuto(TooltipComponent TC)
        {
            LocalisationExt.textSet.SetValue(TC, true);
            LocalisationExt.GetLoc.SetValue(TC, CreateNewLocalisedString());
        }
    }
}
