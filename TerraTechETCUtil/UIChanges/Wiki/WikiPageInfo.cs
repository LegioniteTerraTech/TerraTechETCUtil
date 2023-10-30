using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;

namespace TerraTechETCUtil
{
    public class WikiPageInfo : ManIngameWiki.WikiPage
    {
        public readonly string information;
        internal List<string> hints = new List<string>();
        public WikiPageInfo(string modID, string hintTitle, Sprite icon, string Info, ManIngameWiki.WikiPageGroup group = null) : 
            base(modID, hintTitle, icon, group)
        {
            information = Info;
        }
        public WikiPageInfo(string modID, string hintTitle, Sprite icon, Action Info, ManIngameWiki.WikiPageGroup group = null) :
            base(modID, hintTitle, icon, group)
        {
            information = "";
            infoOverride = Info;
        }
        public override void DisplaySidebar()
        {
            ButtonGUIDisp();
        }
        public override bool ReleaseAsMuchAsPossible()
        {
            return false;
        }
        public override void DisplayGUI()
        {
            GUILayout.Label(information, AltUI.LabelBlack);
        }
    }
}
