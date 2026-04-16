using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static LocalisationEnums;

namespace TerraTechETCUtil
{
    /// <summary>
    /// <inheritdoc/>
    /// <para>Class to convert vanilla <see cref="LocalisedString"/>s to the mod <see cref="LocExtString"/> format</para>
    /// <para>For entries that are looked up using <see cref="Localisation.GetLocalisedString(StringBanks, int, Localisation.GlyphInfo[])"/></para>
    /// <para>For other cases, see <see cref="LocExtStringVanillaText"/></para>
    /// </summary>
    public class LocExtStringVanilla : LocExtString
    {
        private string data;
        /// <summary>
        /// English fallback
        /// </summary>
        public readonly string English;
        /// <summary>
        /// Lookup category, sometimes converted to <see cref="int"/> in base-game
        /// </summary>
        public readonly StringBanks IDCategory;
        /// <summary>
        /// The auto-assigned ID Index for <see cref="LocalisationExt"/> to access this
        /// </summary>
        public readonly int IDIndex;
        /// <summary>
        /// ONLY FOR VANILLA ENTRY REFERENCING
        /// </summary>
        public LocExtStringVanilla(string englishFallback, StringBanks category, int stringID)
        {
            LocalisationExt.InsureInit();
            LocalisationExt.OnLOCChanged.Subscribe(OnLOCChange);
            English = englishFallback;
            IDCategory = category;
            IDIndex = stringID;
            OnLOCChange(Localisation.inst.CurrentLanguage);
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
        /// <inheritdoc/>
        public override string GetEnglish() => English;
    }
    /// <summary>
    /// <inheritdoc/>
    /// <para>Class to convert vanilla <see cref="LocalisedString"/>s to the mod <see cref="LocExtString"/> format</para>
    /// <para>For entries that are looked up using <see cref="StringLookup.GetItemName(ObjectTypes, int)"/></para>
    /// <para>For other cases, see <see cref="LocExtStringVanilla"/></para>
    /// </summary>
    public class LocExtStringVanillaOT : LocExtString
    {
        private string data;
        /// <summary>
        /// English fallback
        /// </summary>
        public readonly string English;
        /// <summary>
        /// Lookup type
        /// </summary>
        public readonly ObjectTypes IDType;
        /// <summary>
        /// The auto-assigned ID Index for <see cref="LocalisationExt"/> to access this
        /// </summary>
        public readonly int IDIndex;
        /// <summary>
        /// ONLY FOR VANILLA ENTRY REFERENCING
        /// </summary>
        public LocExtStringVanillaOT(ObjectTypes objectType, int itemType)
        {
            LocalisationExt.InsureInit();
            LocalisationExt.OnLOCChanged.Subscribe(OnLOCChange);
            English = StringLookup.GetItemName(objectType, itemType);
            IDType = objectType;
            IDIndex = itemType;
            OnLOCChange(Localisation.inst.CurrentLanguage);
        }
        private void OnLOCChange(Languages newLang)
        {
            try
            {
                data = StringLookup.GetItemName(IDType, IDIndex);
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
    }
}
