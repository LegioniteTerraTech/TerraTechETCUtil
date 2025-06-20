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
    /// DO NOT RELY ON THIS - LAGGY AF
    /// </summary>
    public class LocExtStringFunc : LocExtString
    {
        private readonly string English;
        private readonly Func<string> GetString;
        private string data;
        public LocExtStringFunc(string english, Func<string> getString)
        {
            LocalisationExt.InsureInit();
            LocalisationExt.OnLOCChanged.Subscribe(OnLOCChange);
            English = english;
            GetString = getString;
            data = GetString();
        }

        private void OnLOCChange(Languages newLang)
        {
            data = GetString();
        }
        public override string ToString() => data;
        /// <summary>
        /// DOES NOT WORK FOR VANILLA ENTRY REFERENCING
        /// </summary>
        public override string GetEnglish() => English;
    }
}
