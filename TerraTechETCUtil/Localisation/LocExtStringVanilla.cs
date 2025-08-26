using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static LocalisationEnums;

namespace TerraTechETCUtil
{
    public class LocExtStringVanilla : LocExtString
    {
        private string data;
        public readonly string English;
        public readonly StringBanks IDCategory;
        public readonly int IDIndex;
        /// <summary>
        /// ONLY FOR VANILLA ENTRY REFERENCING
        /// </summary>
        /// <param name="englishFallback"></param>
        /// <param name="category"></param>
        /// <param name="stringID"></param>
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
        public override string ToString() => data;
        public override string GetEnglish() => English;
    }
}
