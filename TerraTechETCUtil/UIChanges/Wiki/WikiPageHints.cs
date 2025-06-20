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
        private List<LocExtString> hints = new List<LocExtString>();

        internal WikiPageHints(string modID, LocExtString hintTitle, LocExtString firstHint) :
            base(modID, hintTitle, null, ManIngameWiki.LOC_Hints, ManIngameWiki.InfoSprite)
        {
            AddHint(firstHint);
        }
        internal WikiPageHints(string modID, string hintTitle, LocExtString firstHint) :
            base(modID, hintTitle, null, (LocExtString)ManIngameWiki.LOC_Hints, ManIngameWiki.InfoSprite)
        {
            AddHint(firstHint);
        }
        internal WikiPageHints(string modID, LocExtString hintTitle, string firstHint) :
            base(modID, hintTitle, null, (LocExtString)ManIngameWiki.LOC_Hints, ManIngameWiki.InfoSprite)
        {
            AddHint(firstHint);
        }
        internal WikiPageHints(string modID, string hintTitle, string firstHint) :
            base(modID, hintTitle, null, (LocExtString)ManIngameWiki.LOC_Hints, ManIngameWiki.InfoSprite)
        {
            AddHint(firstHint);
        }
        public bool AddHint(LocExtString hint)
        {
            if (!hints.Contains(hint))
            {
                hints.Add(hint);
                return true;
            }
            return false;
        }
        public bool AddHint(string hintEnglish)
        {
            if (!hints.Exists(x => x.GetEnglish() == hintEnglish))
            {
                hints.Add(new LocExtStringNonReg(hintEnglish));
                return true;
            }
            return false;
        }
        public bool RemoveHint(LocExtString hint)
        {
            return hints.Remove(hint);
        }
        public bool RemoveHint(string hintEnglish)
        {
            return hints.RemoveAll(x => x.GetEnglish() == hintEnglish) > 0;
        }
        public void RemoveALLHints()
        {
            hints.Clear();
        }
        public override void GetIcon() { }
        public override void DisplaySidebar() => ButtonGUIDisp();
        public override bool OnWikiClosed()
        {
            return false;
        }
        protected override void DisplayGUI()
        {
            if (hints != null && hints.Any())
            {
                foreach (var item in hints)
                {
                    GUILayout.Label(item.ToString(), AltUI.TextfieldBlackHuge);
                    GUILayout.Space(10);
                }
            }
            else
                GUILayout.Label("Hints: None", AltUI.LabelBlackTitle);
            GUILayout.FlexibleSpace();
        }
    }
}
