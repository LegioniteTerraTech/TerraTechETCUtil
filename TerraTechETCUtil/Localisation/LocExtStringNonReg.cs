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
    /// Non-registered variant. Does not reserve an index of get looked up at all.
    ///   Good for just filling LocExtStringBase fields when there are no overrides for a standard string for some reason
    /// </summary>
    public class LocExtStringNonReg : LocExtString, ILocExtStringMod
    {
        private string data;
        public LocExtStringNonReg(string english)
        {
            data = english;
        }
        public override string ToString() => data;
        public override string GetEnglish() => data;
        public void ChangeEnglish(string newDesc) => data = newDesc;
        public void Change(Languages lang, string newDesc) => data = newDesc;
        public bool TryLookup(Languages lang, out string output)
        {
            output = data;
            if (lang == LocalisationExt.defaultLanguage)
                return true;
            return false;
        }

        public IEnumerable<KeyValuePair<Languages, string>> IterateLanguages()
        {
            yield return new KeyValuePair<Languages, string>(LocalisationExt.defaultLanguage, data);
        }
    }
}
