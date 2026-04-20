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
        /// Called once for every wiki when a Wiki is created incase mods want to add more data to the end of every wiki
        /// </summary>
        private static Event<Wiki> OnExtendWikiCall = new Event<Wiki>();
        /// <summary>
        /// Subscribe to extended wiki
        /// </summary>
        public static void SubToExtendWiki(Action<Wiki> subRequest)
        {
            foreach (var item in AllWikis.Where(x => x.Value != null))
                subRequest.Invoke(item.Value);
            OnExtendWikiCall.Subscribe(subRequest);
        }
        /// <summary>
        /// Unsub to extended wiki
        /// </summary>
        public static void UnsubToExtendWiki(Action<Wiki> subRequest)
        {
            OnExtendWikiCall.Unsubscribe(subRequest);
        }
        internal static void AutoPopulateWikiExtras(Wiki wiki)
        {
            OnExtendWikiCall.Send(wiki);
        }
    }
}
