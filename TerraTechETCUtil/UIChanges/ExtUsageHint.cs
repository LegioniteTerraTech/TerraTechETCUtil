using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;

namespace TerraTechETCUtil
{

    public class ExtUsageHint : TinySettings
    {
        public class UsageHint
        {
            public readonly string modID;
            public readonly string stringID;
            public readonly float displayDuration;
            public readonly bool repeatable;
            private string descCache;
            public string desc
            {
                get => descCache;
                set {
                    descCache = value;
                    UpdateHint(this);
                }
            }
            public int assignedID { get; internal set; }
            public UsageHint(string ModID, string StringID, string desc, float duration = defaultHintDisplayTime, bool repeat = false)
            {
                modID = ModID;
                stringID = StringID;
                descCache = desc;
                displayDuration = duration;
                repeatable = repeat;
                RegisterHint(this);
            }
            public bool Show()
            {
                return ShowExistingHint(this);
            }
        }
        public string DirectoryInExtModSettings => "UsageHints";

        internal static FieldInfo
        defD = typeof(EnumString).GetField("m_EnumValueInt", BindingFlags.NonPublic | BindingFlags.Instance),
        locFA = typeof(Localisation).GetField("m_HashLookup", BindingFlags.NonPublic | BindingFlags.Instance);

        private static bool init = false;
        private static ExtUsageHint inst = new ExtUsageHint();
        private static List<ManHints.HintDefinition> extHints = new List<ManHints.HintDefinition>();
        private static Dictionary<GameHints.HintID, Action> extHintsActive = new Dictionary<GameHints.HintID, Action>();
        private static HashSet<string> extHintsAuto = new HashSet<string>();
        private static Dictionary<string, int> extHintsIDLookup = new Dictionary<string, int>();
        private static Dictionary<int, string> extHintsIDLookupInv = new Dictionary<int, string>();

        private static HashSet<string> HintsSeen = new HashSet<string>();
        public static bool HintSeen(string hintID)
        {
            return HintsSeen.Contains(hintID);
        }
        public static void ResetHints()
        {
            HintsSeen.Clear();
            HintsSeenToSave();
        }

        public string HintsSeenSAV = "";
        private static int HintsSeenETCIndex = 5000;

        private static void InsureInit()
        {
            if (!init)
            {
                init = true;
                SaveToHintsSeen();
            }
        }

        internal static void RegisterHint(UsageHint UH)
        {
            try
            {
                InsureInit();
                string subjectID = UH.stringID;
                if (extHintsAuto.Contains(subjectID))
                    throw new Exception("Hint of ID " + subjectID + " is already registered");
                ManIngameWiki.InjectHint(UH.modID, "General", UH.desc);
                int intID = HintsSeenETCIndex;
                HintsSeenETCIndex++;
                UH.assignedID = intID;
                ManHints.HintDefinition HD = new ManHints.HintDefinition();
                HD.m_HintIcon = UIHints.IconType.None;
                LocalisedString LS = new LocalisedString();
                string HintDescription = UH.desc;
                LS.m_Bank = subjectID;
                LS.m_Id = "ⁿ_" + subjectID;
                HD.m_HintMessage = LS;
                extHintsAuto.Add(subjectID);
                extHints.Add(HD);
                extHintsIDLookup.Add(subjectID, intID);
                extHintsIDLookupInv.Add(intID, subjectID);
                EnumString ES = new EnumString(typeof(GameHints.HintID), 1);
                defD.SetValue(ES, intID);
                HD.m_HintId = ES;
                try
                {
                    Dictionary<int, string> lingo = (Dictionary<int, string>)locFA.GetValue(Localisation.inst);
                    int lingoSearch = (LS.m_Bank + LS.m_Id).GetHashCode();
                    if (lingo.TryGetValue(lingoSearch, out _))
                    {
                        lingo.Remove(lingoSearch);
                        lingo.Add(lingoSearch, HintDescription);
                        //Debug_TTExt.Log("TerraTechModExt: Refreshed the loc desc for gameObject " + subjectName);
                    }
                    else
                        lingo.Add(lingoSearch, HintDescription);
                    locFA.SetValue(Localisation.inst, lingo);
                    //Debug_TTExt.Log("TerraTechModExt: Saved the loc desc for gameObject " + subjectName);
                }
                catch (Exception e)
                {
                    BlockDebug.ThrowWarning("TerraTechModExt: \nModuleUsageHint TECHINCAL ERROR - loc fail \n" + e);
                }
            }
            catch (Exception e)
            {
                BlockDebug.ThrowWarning("TerraTechModExt: \nModuleUsageHint TECHINCAL ERROR - " + e);
            }
        }
        internal static void UpdateHint(UsageHint UH)
        {
            try
            {
                ManHints.HintDefinition HD = new ManHints.HintDefinition();
                HD.m_HintIcon = UIHints.IconType.None;
                LocalisedString LS = new LocalisedString();
                string subjectID = UH.stringID;
                string HintDescription = UH.desc;
                LS.m_Bank = subjectID;
                LS.m_Id = "ⁿ_" + subjectID;
                HD.m_HintMessage = LS;
                EnumString ES = new EnumString(typeof(GameHints.HintID), 1);
                defD.SetValue(ES, UH.assignedID);
                HD.m_HintId = ES;
                if (!extHints.Contains(HD))
                    extHints.Add(HD);
                try
                {
                    Dictionary<int, string> lingo = (Dictionary<int, string>)locFA.GetValue(Localisation.inst);
                    int lingoSearch = (LS.m_Bank + LS.m_Id).GetHashCode();
                    if (lingo.TryGetValue(lingoSearch, out _))
                    {
                        lingo.Remove(lingoSearch);
                        lingo.Add(lingoSearch, HintDescription);
                        //Debug_TTExt.Log("TerraTechModExt: Refreshed the loc desc for gameObject " + subjectName);
                    }
                    else
                        lingo.Add(lingoSearch, HintDescription);
                    locFA.SetValue(Localisation.inst, lingo);
                    //Debug_TTExt.Log("TerraTechModExt: Saved the loc desc for gameObject " + subjectName);
                }
                catch (Exception e)
                {
                    BlockDebug.ThrowWarning("TerraTechModExt: \nModuleUsageHint TECHINCAL ERROR - loc fail \n" + e);
                }
            }
            catch
            {
                BlockDebug.ThrowWarning("TerraTechModExt: \nModuleUsageHint TECHINCAL ERROR \nCause of error - HintID " + UH.stringID);
            }
        }

        public static void EditHint(string subjectName, int blockID, string HintDescription)
        {
            try
            {
                InsureInit();
                string bank = "ⁿ_" + ManMods.inst.FindBlockName(blockID);
                ManHints.HintDefinition HD = new ManHints.HintDefinition();
                HD.m_HintIcon = UIHints.IconType.None;
                LocalisedString LS = new LocalisedString();
                LS.m_Bank = bank;
                LS.m_Id = "ⁿ_" + blockID.ToString();
                HD.m_HintMessage = LS;
                if (!extHintsAuto.Contains(bank))
                {
                    int intID = HintsSeenETCIndex;
                    HintsSeenETCIndex++;
                    extHintsAuto.Add(bank);
                    extHints.Add(HD);
                    extHintsIDLookup.Add(bank, intID);
                    extHintsIDLookupInv.Add(intID, bank);
                }
                EnumString ES = new EnumString(typeof(GameHints.HintID), 1);
                defD.SetValue(ES, blockID);
                HD.m_HintId = ES;
                try
                {
                    Dictionary<int, string> lingo = (Dictionary<int, string>)locFA.GetValue(Localisation.inst);
                    int lingoSearch = (LS.m_Bank + LS.m_Id).GetHashCode();
                    if (lingo.TryGetValue(lingoSearch, out _))
                    {
                        lingo.Remove(lingoSearch);
                        lingo.Add(lingoSearch, HintDescription);
                        //Debug_TTExt.Log("TerraTechModExt: Refreshed the loc desc for gameObject " + subjectName);
                    }
                    else
                        lingo.Add(lingoSearch, HintDescription);
                    locFA.SetValue(Localisation.inst, lingo);
                    //Debug_TTExt.Log("TerraTechModExt: Saved the loc desc for gameObject " + subjectName);
                }
                catch (Exception e)
                {
                    BlockDebug.ThrowWarning("TerraTechModExt: \nModuleUsageHint TECHINCAL ERROR - loc fail \n" + e);
                }
            }
            catch
            {
                BlockDebug.ThrowWarning("TerraTechModExt: \nModuleUsageHint TECHINCAL ERROR \nCause of error - Block " + subjectName);
            }
        }

        public static void ShowBlockHint(int blockID)
        {
            try
            {
                string bank = "ⁿ_" + ManMods.inst.FindBlockName(blockID);
                if (!HintsSeen.Contains(bank) && extHintsIDLookup.TryGetValue(bank, out var val))
                    ShowHint((GameHints.HintID)val, defaultHintDisplayTime, false);
            }
            catch (Exception e)
            {
                Debug_TTExt.Assert(true, "ShowExistingHint failed on blockID "  + blockID + " - " + e.Message);
            }
        }
        public static bool ShowExistingHint(UsageHint UH)
        {
            try
            {
                if (!HintsSeen.Contains(UH.stringID))
                    return ShowHint((GameHints.HintID)UH.assignedID, UH.displayDuration, UH.repeatable);
                else
                    return true;
            }
            catch (Exception e)
            {
                Debug_TTExt.Assert(true, e.Message);
            }
            return false;
        }
        private static bool ShowHint(GameHints.HintID hintID, float displayTime, bool allowRepeat)
        {
            /*
            Debug_TTExt.Log("ShowHint called for hintID " + hintID + " - time " + displayTime + ", manHints " + ManHints.inst.HintsEnabled + 
                ", hintHud " + ManHUD.inst.IsHudElementVisible(ManHUD.HUDElementType.HintFloating) + ", registered " + extHints.Exists(x => x.m_HintId.Value == (int)hintID) +
                ", active " + extHintsActive.ContainsKey(hintID));
            */
            if (ManHints.inst.HintsEnabled && ManHUD.inst.IsVisible && ManGameMode.inst.GetIsInPlayableMode())
            {
                ManHints.HintDefinition hintDef = extHints.Find(x => x.m_HintId.Value == (int)hintID);
                if (hintDef != null)
                {
                    if (extHintsActive.TryGetValue(hintID, out var hintAction))
                    {
                        InvokeHelper.CancelInvoke(hintAction);
                    }
                    else
                    {
                        Singleton.Manager<ManSFX>.inst.PlayUISFX(ManSFX.UISfxType.Hint);
                        UIHints.ShowContext showContext = new UIHints.ShowContext
                        {
                            hintID = hintID,
                            hintDef = hintDef
                        };
                        Singleton.Manager<ManHUD>.inst.ShowHudElement(ManHUD.HUDElementType.Hint, showContext);
                    }
                    extHintsActive[hintID] = () => {
                        try
                        {
                            Singleton.Manager<ManHUD>.inst.HideHudElement(ManHUD.HUDElementType.Hint, hintID);
                            extHintsActive.Remove(hintID);
                        }
                        catch (Exception e)
                        {
                            Debug_TTExt.Assert(true, e.Message);
                        }
                    };
                    InvokeHelper.Invoke(extHintsActive[hintID], displayTime);
                    if (!allowRepeat)
                    {
                        HintsSeen.Add(extHintsIDLookupInv[(int)hintID]);
                        InvokeHelper.InvokeSingle(HintsSeenToSave, 0.3f);
                    }
                    return true;
                }
                else
                    Debug_TTExt.Log("ShowHint FAILED call for hintID " + hintID + " - time " + displayTime + ", manHints " + ManHints.inst.HintsEnabled +
                        ", hintHud " + ManHUD.inst.IsHudElementVisible(ManHUD.HUDElementType.HintFloating) + ", registered " + extHints.Exists(x => x.m_HintId.Value == (int)hintID) +
                        ", active " + extHintsActive.ContainsKey(hintID));
            }
            return false;
        }

        public const float defaultHintDisplayTime = 8;

        private static StringBuilder SB = new StringBuilder();
        // Saving to File
        private static void HintsSeenToSave()
        {   // Saving a Tech from the BlockMemory
            try
            {
                if (HintsSeen.Any())
                {
                    SB.Append(HintsSeen.ElementAt(0));
                    for (int step = 1; step < HintsSeen.Count; step++)
                    {
                        SB.Append("|");
                        SB.Append(HintsSeen.ElementAt(step));
                    }
                }
                inst.HintsSeenSAV = SB.ToString();
                inst.TrySaveToDisk();
            }
            finally
            {
                SB.Clear();
            }
        }

        private static void SaveToHintsSeen()
        {   // Loading from memory
            try
            {
                HintsSeen.Clear();
                if (inst.TryLoadFromDisk(ref inst) && !inst.HintsSeenSAV.NullOrEmpty())
                {
                    foreach (char ch in inst.HintsSeenSAV)
                    {
                        if (ch == '|')//new string
                        {
                            HintsSeen.Add(SB.ToString());
                            SB.Clear();
                        }
                        else
                            SB.Append(ch);
                    }
                    HintsSeen.Add(SB.ToString());
                }
            }
            catch (Exception e)
            {
                Debug_TTExt.Assert(true, e.Message);
            }
            finally
            {
                SB.Clear();
            }
        }



        // Default-set ones for this mod
        private static readonly string
        legsDesc = "This block is a leg block.  While it's slow, it has considerable grip and could even climb walls!";
    }
}
