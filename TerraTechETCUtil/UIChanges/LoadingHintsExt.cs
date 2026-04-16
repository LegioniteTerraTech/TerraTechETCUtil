using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using static LocalisationEnums;
using System.Security.Cryptography;

namespace TerraTechETCUtil
{
    /// <summary>
    /// Manager for modded loading hints shown during load tranzition screens like <see cref="UILoadingScreenHints"/>
    /// </summary>
    public class LoadingHintsExt
    {
        /// <summary>
        /// Normal mod-added hints
        /// </summary>
        public static List<LocExtString> ExternalHints = new List<LocExtString>();
        /// <summary>
        /// Mod-added hints that must be shown on the next loading screen
        /// </summary>

        public static List<LocExtString> MandatoryHints = new List<LocExtString>();
        private static int curID = 0;

        /// <summary>
        /// Test showing a loading hint on next loading screen
        /// </summary>
        /// <param name="description"></param>
        public void ForceShowLoadingHint(LocExtString description)
        {
            MandatoryHints.Remove(description);
            MandatoryHints.Add(description);
        }

        /// <summary>
        /// A loading hint to show during a loading screen
        /// </summary>
        public class LoadingHint
        {
            /// <summary>
            /// Mod ID that added this
            /// </summary>
            public readonly string modID;
            /// <summary>
            /// Text to display for the hint
            /// </summary>
            public readonly LocExtString desc;
            /// <summary>
            /// The auto-assigned ID for this hint
            /// </summary>
            public readonly int assignedID;
            /// <summary>
            /// Create a new loading hint to display during a load screen.
            /// <para>Has a random chance of being shown</para>
            /// </summary>
            /// <param name="ModID">ModID of the mod that is adding this</param>
            /// <param name="Title">Title of the loading hint</param>
            /// <param name="Description">Description of the hint</param>
            /// <exception cref="InvalidOperationException"></exception>
            public LoadingHint(string ModID, LocExtStringMod Title, LocExtStringMod Description)
            {
                modID = ModID;
                assignedID = curID;
                curID++;
                var hintsLoader = Localisation.inst;
                if (ExternalHints.Exists(x => x.Equals(assignedID)))
                    throw new InvalidOperationException("Description with wording: \"" + Description.GetEnglish() + "\" already exists in External Hints!  Don't create LoadingHints twice!");
                Dictionary<Languages, string> NewDesc = new Dictionary<Languages, string>();
                foreach (var hintPair in Description.IterateLanguages())
                {
                    if (Title.TryLookup(hintPair.Key, out string val))
                    {
                        NewDesc.Add(hintPair.Key, AltUI.UIHintPopupTitle(val) + "\n" + hintPair.Value);
                    }
                }
                desc = new LocExtStringMod(NewDesc);
                ExternalHints.Add(desc);
                ManIngameWiki.InjectHint(ModID, Title, Description);
            }
            /// <inheritdoc cref="LoadingHint.LoadingHint(string, LocExtStringMod, LocExtStringMod)"/>
            public LoadingHint(string ModID, string Title, string Description)
            {
                modID = ModID;
                assignedID = curID;
                curID++;
                var hintsLoader = Localisation.inst;
                if (ExternalHints.Exists(x => x.Equals(assignedID)))
                    throw new InvalidOperationException("Description with wording: \"" + Description + "\" already exists in External Hints!  Don't create LoadingHints twice!");
                desc = new LocExtStringNonReg(AltUI.UIHintPopupTitle(Title) + "\n" + Description);
                ExternalHints.Add(desc);
                ManIngameWiki.InjectHint(ModID, Title, new LocExtStringNonReg(Description));
            }
            /// <summary>
            /// Make this show up next loading screen
            /// </summary>
            public void ForceShowNextTime()
            {
                MandatoryHints.Remove(desc);
                MandatoryHints.Add(desc);
            }
            /// <summary>
            /// Re-register this hint if you <see cref="Unregister"/> it earlier.
            /// <para>The first creation of this class automatically registers it.</para>
            /// </summary>
            public void Reregister()
            {
                if (!ExternalHints.Contains(desc))
                    ExternalHints.Add(desc);
            }
            /// <summary>
            /// Unregister this hint, which means it will never appear on the loading screen until it is <see cref="Reregister"/>
            /// </summary>
            public void Unregister()
            {
                ExternalHints.Remove(desc);
            }
        }
    }
}
