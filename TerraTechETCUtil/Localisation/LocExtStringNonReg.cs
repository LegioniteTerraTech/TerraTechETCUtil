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
    /// <para><b>Non-registered variant.</b> Does not reserve an index or get looked up at all in <see cref="LocalisationExt"/>.</para>
    /// <para>Good for just filling LocExtStringBase fields when there are no overrides for a standard string for some reason</para>
    /// </summary>
    public class LocExtStringNonReg : LocExtString, ILocExtStringMod
    {
        private string data;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="english"></param>
        public LocExtStringNonReg(string english)
        {
            data = english;
        }
        /// <inheritdoc/>
        public override string ToString() => data;
        /// <inheritdoc/>
        public override string GetEnglish() => data;
        /// <inheritdoc/>
        public void ChangeEnglish(string newDesc) => data = newDesc;
        /// <inheritdoc/>
        public void Change(Languages lang, string newDesc) => data = newDesc;
        /// <inheritdoc/>
        public bool TryLookup(Languages lang, out string output)
        {
            output = data;
            if (lang == LocalisationExt.defaultLanguage)
                return true;
            return false;
        }

        /// <inheritdoc/>
        public IEnumerable<KeyValuePair<Languages, string>> IterateLanguages()
        {
            yield return new KeyValuePair<Languages, string>(LocalisationExt.defaultLanguage, data);
        }
    }
}
