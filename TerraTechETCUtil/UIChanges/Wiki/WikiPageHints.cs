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
    /// <para>Wiki page for all hint-related information</para>
    /// </summary>
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
        /// <summary>
        /// Register a hint with <see cref="WikiPageHints"/>
        /// <para>This is done automatically for <see cref="ExtUsageHint.UsageHint"/> and 
        /// <see cref="LoadingHintsExt.LoadingHint"/></para>
        /// </summary>
        /// <param name="hint">The hint to add</param>
        /// <returns>True if it added new successfully</returns>
        public bool AddHint(LocExtString hint)
        {
            if (!hints.Contains(hint))
            {
                hints.Add(hint);
                return true;
            }
            return false;
        }
        /// <inheritdoc cref="AddHint(LocExtString)"/>
        /// <summary>
        /// 
        /// </summary>
        /// <param name="hintEnglish">The hint to add in ENGLISH</param>
        /// <returns></returns>
        public bool AddHint(string hintEnglish)
        {
            if (!hints.Exists(x => x.GetEnglish() == hintEnglish))
            {
                hints.Add(new LocExtStringNonReg(hintEnglish));
                return true;
            }
            return false;
        }
        /// <summary>
        /// Remove a hint assigned to this
        /// </summary>
        /// <param name="hint">The hint to remove</param>
        /// <returns>True if it removed it successfully</returns>
        public bool RemoveHint(LocExtString hint)
        {
            return hints.Remove(hint);
        }
        /// <inheritdoc cref="RemoveHint(LocExtString)"/>
        /// <summary>
        /// 
        /// </summary>
        /// <param name="hintEnglish">The hint to remove in ENGLISH</param>
        /// <returns></returns>
        public bool RemoveHint(string hintEnglish)
        {
            return hints.RemoveAll(x => x.GetEnglish() == hintEnglish) > 0;
        }
        /// <summary>
        /// Clear out all hints assigned to <see cref="WikiPageHints"/>.
        /// <para><b>Only use this if absolutely necessary!</b></para>
        /// </summary>
        public void RemoveALLHints()
        {
            hints.Clear();
        }
        /// <inheritdoc/>
        public override void GetIcon() { }
        /// <inheritdoc/>
        public override void DisplaySidebar() => ButtonGUIDisp();
        /// <inheritdoc/>
        public override bool OnWikiClosed()
        {
            return false;
        }
        /// <inheritdoc/>
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
