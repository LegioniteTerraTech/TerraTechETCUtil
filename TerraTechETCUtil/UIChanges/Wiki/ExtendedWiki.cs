using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static LocalisationEnums;
using static TerraTechETCUtil.ManIngameWiki;

namespace TerraTechETCUtil
{
    /// <summary>
    /// Use this as a simple hook to extend the vanilla-side wiki!
    /// </summary>
    public static class ExtendedWiki
    {
        /// <summary>
        /// Called when a Wiki is created incase mods want to add more data to the end of every wiki
        /// </summary>
        public static Event<Wiki> OnExtendWikiCall = new Event<Wiki>();
        internal static void AutoPopulateWikiExtras(Wiki wiki)
        {
            OnExtendWikiCall.Send(wiki);
        }
    }
}
