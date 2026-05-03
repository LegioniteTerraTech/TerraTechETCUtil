using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerraTechETCUtil
{
    /// <summary>
    /// Stores all of the localized text
    /// </summary>
    public class LocHelper
    {
        /// <summary>
        /// Localisation
        /// </summary>
        public static LocExtStringMod LOC_GENERAL_HINT = new LocExtStringMod(new Dictionary<LocalisationEnums.Languages, string>()
        {
            { LocalisationEnums.Languages.US_English, "GENERAL HINT" },
            { LocalisationEnums.Languages.Japanese, "一般的なヒント" },
        });
        /// <summary>
        /// Localisation
        /// </summary>
        public static LocExtStringMod LOC_TRAIN_HINT = new LocExtStringMod(new Dictionary<LocalisationEnums.Languages, string>()
        {
            { LocalisationEnums.Languages.US_English, "TRAIN HINT" },
            { LocalisationEnums.Languages.Japanese, "電車のヒント" },
        });
    }
}
