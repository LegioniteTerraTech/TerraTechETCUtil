using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;

namespace TerraTechETCUtil
{
    public class LoadingHintsExt
    {
        public static List<string> ExternalHints = new List<string>();

        public static List<string> MandatoryHints = new List<string>();
        private static int curID = 0;

        public void ForceShowLoadingHint(string description)
        {
            MandatoryHints.Remove(description);
            MandatoryHints.Add(description);
        }


        public class LoadingHint
        {
            public readonly string modID;
            public readonly string desc;
            public int assignedID { get; internal set; } = int.MinValue;
            public LoadingHint(string ModID, string Title, string Description)
            {
                modID = ModID;
                assignedID = curID;
                curID++;
                var hintsLoader = Localisation.inst;
                if (ExternalHints.Exists(x => x.Equals(assignedID)))
                    throw new InvalidOperationException("Description with wording: \"" + Description + "\" already exists in External Hints!  Don't create LoadingHints twice!");
                desc = AltUI.UIHintPopupTitle(Title) + "\n" + Description;
                ExternalHints.Add(desc);
                ManIngameWiki.InjectHint(ModID, Title, Description);
            }
            public void ForceShowNextTime()
            {
                MandatoryHints.Remove(desc);
                MandatoryHints.Add(desc);
            }
            public void Unregister()
            {
                ExternalHints.Remove(desc);
                curID = int.MinValue;
            }
        }
    }
}
