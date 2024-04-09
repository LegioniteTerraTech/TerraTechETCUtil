using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;

namespace TerraTechETCUtil
{
    public class WikiPageHints : ManIngameWiki.WikiPage
    {
        internal List<string> hints = new List<string>();
        public WikiPageHints(string modID, string hintTitle, string firstHint) :
            base(modID, hintTitle, null, "Hints", ManIngameWiki.InfoSprite)
        {
            hints.Add(firstHint);
        }
        public WikiPageHints(string modID, string hintTitle, string firstHint, ManIngameWiki.WikiPageGroup group) : 
            base(modID, hintTitle, null, group)
        {
            hints.Add(firstHint);
        }
        public override void DisplaySidebar() => ButtonGUIDisp();
        public override bool ReleaseAsMuchAsPossible()
        {
            return false;
        }
        public override void DisplayGUI()
        {
            if (hints != null && hints.Any())
            {
                foreach (var item in hints)
                {
                    GUILayout.Label(item, AltUI.TextfieldBlackHuge);
                    GUILayout.Space(10);
                }
            }
            else
                GUILayout.Label("Hints: None", AltUI.LabelBlackTitle);
            GUILayout.FlexibleSpace();
        }
    }
}
