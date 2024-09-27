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
        public const string prefix = "ⁿ_";
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
        public const int HintsSeenETCIndexStart = 5000;
        private static int HintsSeenETCIndex = HintsSeenETCIndexStart;

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
                if (extHintsIDLookup.ContainsKey(subjectID))
                    throw new Exception("Hint of ID " + subjectID + " is already registered");
                LocalisedString LS = new LocalisedString()
                {
                    m_Bank = subjectID,
                    m_Id = prefix + subjectID,
                };
                EnumString ES = new EnumString(typeof(GameHints.HintID), 1);
                int intID = HintsSeenETCIndex;
                HintsSeenETCIndex++;
                UH.assignedID = intID;
                defD.SetValue(ES, intID);
                ManHints.HintDefinition HD = new ManHints.HintDefinition()
                {
                    m_HintMessage = LS,
                    m_HintIcon = UIHints.IconType.None,
                    m_HintId = ES,
                    m_UseDifferentHintMessageForPad = false
                };
                ManIngameWiki.InjectHint(UH.modID, "General", UH.desc);
                string HintDescription = UH.desc;
                extHints.Add(HD);
                extHintsIDLookup.Add(subjectID, intID);
                extHintsIDLookupInv.Add(intID, subjectID);
                try
                {
                    Dictionary<int, string> lingo = (Dictionary<int, string>)locFA.GetValue(Localisation.inst);
                    int lingoSearch = (LS.m_Bank + LS.m_Id).GetHashCode();
                    if (lingo.TryGetValue(lingoSearch, out _))
                    {
                        lingo[lingoSearch] = HintDescription;
                        Debug_TTExt.Info("TerraTechModExt: ModuleUsageHint.RegisterHint Refreshed the loc desc for UsageHint " + UH.assignedID);
                    }
                    else
                        lingo.Add(lingoSearch, HintDescription);
                    locFA.SetValue(Localisation.inst, lingo);
                    Debug_TTExt.Info("TerraTechModExt: ModuleUsageHint.RegisterHint Saved the loc desc for UsageHint " + UH.assignedID);
                }
                catch (Exception e)
                {
                    BlockDebug.ThrowWarning("TerraTechModExt: \nModuleUsageHint.RegisterHint TECHINCAL ERROR - loc fail \n" + e);
                }
            }
            catch (Exception e)
            {
                BlockDebug.ThrowWarning("TerraTechModExt: \nModuleUsageHint.RegisterHint TECHINCAL ERROR - " + e);
            }
        }
        internal static void UpdateHint(UsageHint UH)
        {
            try
            {
                string subjectID = UH.stringID;
                string HintDescription = UH.desc;
                LocalisedString LS = new LocalisedString()
                {
                    m_Bank = subjectID,
                    m_Id = prefix + subjectID,
                };
                EnumString ES = new EnumString(typeof(GameHints.HintID), 1);
                defD.SetValue(ES, UH.assignedID);
                ManHints.HintDefinition HD = new ManHints.HintDefinition()
                {
                    m_HintMessage = LS,
                    m_HintIcon = UIHints.IconType.None,
                    m_HintId = ES,
                    m_UseDifferentHintMessageForPad = false
                };
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
                        Debug_TTExt.Info("TerraTechModExt: ModuleUsageHint.UpdateHint Refreshed the loc desc for UsageHint " + UH.assignedID);
                    }
                    else
                        lingo.Add(lingoSearch, HintDescription);
                    locFA.SetValue(Localisation.inst, lingo);
                    Debug_TTExt.Info("TerraTechModExt: ModuleUsageHint.UpdateHint Saved the loc desc for UsageHint " + UH.assignedID);
                }
                catch (Exception e)
                {
                    BlockDebug.ThrowWarning("TerraTechModExt: \nModuleUsageHint.UpdateHint TECHINCAL ERROR - loc fail \n" + e);
                }
            }
            catch
            {
                BlockDebug.ThrowWarning("TerraTechModExt: \nModuleUsageHint.UpdateHint TECHINCAL ERROR \nCause of error - HintID " + UH.stringID);
            }
        }

        public static void EditHint(string subjectName, int blockID, string HintDescription)
        {
            try
            {
                InsureInit();
                string bank = prefix + subjectName;
                EnumString ES;
                LocalisedString LS = new LocalisedString()
                {
                    m_Bank = bank,
                    m_Id = ManMods.inst.FindBlockName(blockID),//prefix + blockID.ToString();
                };
                if (extHintsIDLookup.TryGetValue(bank, out int val))
                {
                    ES = new EnumString(typeof(GameHints.HintID), 1);
                    defD.SetValue(ES, val);
                }
                else
                {
                    //Debug_TTExt.Log("TerraTechModExt: ModuleUsageHint.EditHint add new for gameObject " + subjectName);
                    int intID = HintsSeenETCIndex;
                    HintsSeenETCIndex++;
                    ES = new EnumString(typeof(GameHints.HintID), 1);
                    defD.SetValue(ES, intID);
                    ManHints.HintDefinition HD = new ManHints.HintDefinition()
                    {
                        m_HintMessage = LS,
                        m_HintIcon = UIHints.IconType.None,
                        m_HintId = ES,
                        m_UseDifferentHintMessageForPad = false
                    };
                    extHints.Add(HD);
                    extHintsIDLookup.Add(bank, intID);
                    extHintsIDLookupInv.Add(intID, bank);
                }
                try
                {
                    Dictionary<int, string> lingo = (Dictionary<int, string>)locFA.GetValue(Localisation.inst);
                    int lingoSearch = (LS.m_Bank + LS.m_Id).GetHashCode();
                    if (lingo.TryGetValue(lingoSearch, out _))
                    {
                        lingo[lingoSearch] = HintDescription;
                        Debug_TTExt.Info("TerraTechModExt: ModuleUsageHint.EditHint Refreshed the loc desc for gameObject " + subjectName);
                    }
                    else
                        lingo.Add(lingoSearch, HintDescription);
                    locFA.SetValue(Localisation.inst, lingo);
                    Debug_TTExt.Info("TerraTechModExt: ModuleUsageHint.EditHint Saved the loc desc for gameObject " + subjectName);
                }
                catch (Exception e)
                {
                    BlockDebug.ThrowWarning("TerraTechModExt: \nModuleUsageHint.EditHint TECHINCAL ERROR - loc fail \n" + e);
                }
            }
            catch
            {
                BlockDebug.ThrowWarning("TerraTechModExt: \nModuleUsageHint.EditHint TECHINCAL ERROR \nCause of error - Block " + subjectName);
            }
        }

        public static void ShowRandomExternalHint()
        {
            if (extHintsIDLookupInv.Any())
            {
                var entry = extHintsIDLookupInv.ToList().GetRandomEntry();
                ShowHint((GameHints.HintID)entry.Key, 3, true);
            }
        }
        public static void ShowBlockHint(string subjectName, int blockID)
        {
            try
            {
                string bank = prefix + subjectName;
                if (!HintsSeen.Contains(bank))
                {
                    if (extHintsIDLookup.TryGetValue(bank, out var val))
                        ShowHint((GameHints.HintID)val, defaultHintDisplayTime, false);
                    else
                        Debug_TTExt.Assert(true, "ShowBlockHint failed on blockID " + blockID +
                            " - For some reason the block \"" + ManMods.inst.FindBlockName(blockID) + 
                            "\" is not registered but was called for a hint!?");
                }
                else
                    Debug_TTExt.Info("ShowBlockHint show on blockID " + blockID +
                        " - Hint already shown");
            }
            catch (Exception e)
            {
                Debug_TTExt.Assert(true, "ShowBlockHint crashed on blockID " + blockID + " - " + e.Message);
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
                    Debug_TTExt.Log("ShowHint FAILED call for hintID " + hintID + " - time " +
                        displayTime + ", manHints " + ManHints.inst.HintsEnabled +
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
