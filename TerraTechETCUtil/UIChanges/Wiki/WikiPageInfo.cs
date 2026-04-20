using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;

namespace TerraTechETCUtil
{
    /// <inheritdoc cref="ManIngameWiki.WikiPage"/>
    /// <summary>
    /// <para>Wiki page for general information</para>
    /// </summary>
    public class WikiPageInfo : ManIngameWiki.WikiPage
    {
        /// <inheritdoc />
        protected override void OnBeforeDataRequested(bool getFullData)
        {
        }
        /// <summary>
        /// Information string to display on the page
        /// </summary>
        public readonly string information;
        internal List<string> hints = new List<string>();

        /// <inheritdoc cref="ManIngameWiki.WikiPage.WikiPage(string, LocExtString, Sprite, ManIngameWiki.WikiPageGroup)"/>
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ModID"></param>
        /// <param name="Title"></param>
        /// <param name="Icon"></param>
        /// <param name="Info">Info to display on the page</param>
        /// <param name="group"></param>
        public WikiPageInfo(string ModID, LocExtString Title, Sprite Icon, string Info, ManIngameWiki.WikiPageGroup group = null) :
            base(ModID, Title, Icon, group)
        {
            information = Info;
        }
        /// <inheritdoc cref="WikiPageInfo.WikiPageInfo(string, LocExtString, Sprite, string, ManIngameWiki.WikiPageGroup)"/>
        public WikiPageInfo(string ModID, LocExtString Title, Sprite Icon, Action Info, ManIngameWiki.WikiPageGroup group = null) :
            base(ModID, Title, Icon, group)
        {
            information = "";
            infoOverride = Info;
        }

        /// <inheritdoc cref="WikiPageInfo.WikiPageInfo(string, LocExtString, Sprite, string, ManIngameWiki.WikiPageGroup)"/>
        public WikiPageInfo(string ModID, string Title, Sprite Icon, string Info, ManIngameWiki.WikiPageGroup group = null) : 
            base(ModID, Title, Icon, group)
        {
            information = Info;
        }
        /// <inheritdoc cref="WikiPageInfo.WikiPageInfo(string, LocExtString, Sprite, string, ManIngameWiki.WikiPageGroup)"/>
        public WikiPageInfo(string ModID, string Title, Sprite Icon, Action Info, ManIngameWiki.WikiPageGroup group = null) :
            base(ModID, Title, Icon, group)
        {
            information = "";
            infoOverride = Info;
        }


        /// <inheritdoc/>
        public override void GetIcon() { }
        /// <inheritdoc/>
        public override void DisplaySidebar() => ButtonGUIDisp();
        /// <inheritdoc/>
        public override bool OnWikiClosedOrDeallocateMemory()
        {
            return false;
        }
        /// <inheritdoc/>
        protected override void DisplayGUI()
        {
            GUILayout.Label(information, AltUI.LabelBlack);
        }
    }
}
