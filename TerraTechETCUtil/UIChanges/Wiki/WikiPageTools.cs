using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TerraTechETCUtil
{
    /// <inheritdoc cref="ManIngameWiki.WikiPage"/>
    /// <summary>
    /// <para>Wiki page for tools and helpers</para>
    /// <para>Note: <b>There can only be one per mod!</b></para>
    /// </summary>
    public class WikiPageTools : WikiPageInfo
    {
        /// <summary>
        /// Create a wiki page for tools and helpers
        /// </summary>
        /// <param name="ModID">Mod ID to make under.</param>
        /// <param name="pageGUI">Contents to display the the GUI</param>
        public WikiPageTools(string ModID, Action pageGUI) : 
            base(ModID, ManIngameWiki.LOC_Tools, ManIngameWiki.ToolsSprite, pageGUI)
        {
        }
    }
}
