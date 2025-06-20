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
    public class LoadingHintsExt
    {
        public static List<LocExtString> ExternalHints = new List<LocExtString>();

        public static List<LocExtString> MandatoryHints = new List<LocExtString>();
        private static int curID = 0;

        public void ForceShowLoadingHint(LocExtString description)
        {
            MandatoryHints.Remove(description);
            MandatoryHints.Add(description);
        }


        public class LoadingHint
        {
            public readonly string modID;
            public readonly LocExtString desc;
            public readonly int assignedID;
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
            public void ForceShowNextTime()
            {
                MandatoryHints.Remove(desc);
                MandatoryHints.Add(desc);
            }
            public void Reregister()
            {
                if (!ExternalHints.Contains(desc))
                    ExternalHints.Add(desc);
            }
            public void Unregister()
            {
                ExternalHints.Remove(desc);
            }
        }
    }
}
