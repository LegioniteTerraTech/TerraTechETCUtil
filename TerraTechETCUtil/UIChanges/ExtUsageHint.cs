using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;

namespace TerraTechETCUtil
{
    /// <summary>
    /// The manager for <see cref="UsageHint"/>s that adds modded hints to <see cref="ManHints"/>
    /// </summary>
    public class ExtUsageHint : ITinySettings
    {
        /// <summary>
        /// Special tag to flag hints as modded in <see cref="ManHints"/>
        /// </summary>
        public const string prefix = "ⁿ_";
        /// <summary>
        /// A usage hint automatically integrated with <see cref="ManHints"/> managed by <see cref="ExtUsageHint"/>
        /// </summary>
        public class UsageHint
        {
            /// <summary>
            /// Mod that added this
            /// </summary>
            public readonly string modID;
            /// <summary>
            /// Specific ID used to store and identify the hint in serialization save and load
            /// </summary>
            public readonly string stringID;
            /// <summary>
            /// How long to display this hint for
            /// </summary>
            public readonly float displayDuration;
            /// <summary>
            /// If calling this usage hint again permits it to be displayed more than once
            /// </summary>
            public readonly bool repeat;
            internal LocExtStringMod descAuto;
            /// <summary>
            /// The description to display for this hint
            /// </summary>
            public string desc
            {
                get
                {
                    if (descAuto != null)
                        return descAuto.ToString();
                    return string.Empty;
                }
            }
            /// <summary>
            /// The auto assigned ID for this game session given by <see cref="ExtUsageHint"/>
            /// </summary>
            public int assignedID { get; internal set; }
            /// <summary>
            /// Create a new hint for use on the UI system
            /// </summary>
            /// <param name="ModID">ModID from the mod adding this</param>
            /// <param name="StringID">Specific ID used to store and identify the hint in serialization save and load.
            /// <para><b>AVOID CHANGING THIS AFTER RELEASE</b></para></param>
            /// <param name="desc">The description to display when showing the hint</param>
            /// <param name="duration">How long to display this hint for</param>
            /// <param name="repeat">If calling this usage hint again permits it to be displayed more than once</param>
            public UsageHint(string ModID, string StringID, string desc, float duration = defaultHintDisplayTime, bool repeat = false)
            {
                modID = ModID;
                stringID = StringID;
                descAuto = new LocExtStringMod(desc);
                displayDuration = duration;
                this.repeat = repeat;
                RegisterHint(this);
            }
            /// <inheritdoc cref="UsageHint.UsageHint(string, string, string, float, bool)"/>
            public UsageHint(string ModID, string StringID, LocExtStringMod desc, float duration = defaultHintDisplayTime, bool repeat = false)
            {
                modID = ModID;
                stringID = StringID;
                descAuto = desc;
                displayDuration = duration;
                this.repeat = repeat;
                RegisterHint(this);
            }
            /// <summary>
            /// Show this hint.
            /// <para>If it isn't working the second time, make sure to init this with <see cref="repeat"/> set to true!</para>
            /// </summary>
            /// <returns></returns>
            public bool Show()
            {
                return ShowExistingHint(this);
            }
            /// <summary>
            /// Check to see if this hint was already seen
            /// </summary>
            /// <returns>True if already seen</returns>
            public bool HintSeen() => ExtUsageHint.HintSeen(stringID);
        }
        /// <inheritdoc/>
        public string DirectoryInExtModSettings => "UsageHints";

        internal static FieldInfo defD = typeof(EnumString).GetField("m_EnumValueInt", BindingFlags.NonPublic | BindingFlags.Instance);
        //internal static FieldInfo locFA = typeof(Localisation).GetField("m_HashLookup", BindingFlags.NonPublic | BindingFlags.Instance);

        private static bool init = false;
        private static ExtUsageHint inst = new ExtUsageHint();
        private static List<ManHints.HintDefinition> extHints = new List<ManHints.HintDefinition>();
        private static Dictionary<GameHints.HintID, Action> extHintsActive = new Dictionary<GameHints.HintID, Action>();
        private static Dictionary<string, int> extHintsIDLookup = new Dictionary<string, int>();
        private static Dictionary<int, string> extHintsIDLookupInv = new Dictionary<int, string>();

        private static HashSet<string> HintsSeen = new HashSet<string>();
        /// <summary>
        /// Check to see if the hint was already seen
        /// </summary>
        /// <param name="hintID"></param>
        /// <returns>True if already seen</returns>
        public static bool HintSeen(string hintID)
        {
            return HintsSeen.Contains(hintID);
        }
        /// <summary>
        /// Reset ALL hints seen by the player.
        /// <para><b>AVOID USING THIS IN RELEASE</b></para>
        /// </summary>
        public static void ResetHints()
        {
            HintsSeen.Clear();
            HintsSeenToSave();
        }
        /// <summary>
        /// Serialized hints the player has seen thus far.
        /// </summary>
        public string HintsSeenSAV = "";
        /// <summary>
        /// Where the hint indexes for the play session start at
        /// </summary>
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
                LocalisedString LS;
                LS = UH.descAuto.CreateNewLocalisedString();
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
                ManIngameWiki.InjectHint(UH.modID, ManIngameWiki.LOC_General, UH.descAuto);
                string HintDescription = UH.desc;
                extHints.Add(HD);
                extHintsIDLookup.Add(subjectID, intID);
                extHintsIDLookupInv.Add(intID, subjectID);
                /*
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
                    BlockDebug.ThrowWarning(false, "TerraTechModExt: \nModuleUsageHint.RegisterHint TECHNICAL ERROR - loc fail \n" + e);
                }*/
            }
            catch (Exception e)
            {
                BlockDebug.ThrowWarning(false, "TerraTechModExt: \nModuleUsageHint.RegisterHint TECHNICAL ERROR - " + e);
            }
        }
        /*
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
                    BlockDebug.ThrowWarning(false, "TerraTechModExt: \nModuleUsageHint.UpdateHint TECHINCAL ERROR - loc fail \n" + e);
                }
            }
            catch
            {
                BlockDebug.ThrowWarning(false, "TerraTechModExt: \nModuleUsageHint.UpdateHint TECHINCAL ERROR \nCause of error - HintID " + UH.stringID);
            }
        }*/

        /// <summary>
        /// Edit a hint that already exists <b>for a block type</b>
        /// </summary>
        /// <param name="subjectName"></param>
        /// <param name="blockID">Target <see cref="BlockTypes"/></param>
        /// <param name="HintDescription">The description to display for this hint</param>
        public static void EditHint(string subjectName, int blockID, string HintDescription)
        {
            try
            {
                InsureInit();
                string bank = prefix + subjectName;
                EnumString ES;
                LocalisedString LS = new LocalisedString()
                {
                    m_Bank = LocalisationExt.ModTag,
                    m_Id = HintDescription,
                };
                if (extHintsIDLookup.TryGetValue(bank, out int val))
                {
                    ES = new EnumString(typeof(GameHints.HintID), 1);
                    defD.SetValue(ES, val);
                    extHints.Find(x => x.m_HintId.Value == val).m_HintMessage.m_Id = HintDescription;
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
                /*
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
                    BlockDebug.ThrowWarning(false, "TerraTechModExt: \nModuleUsageHint.EditHint TECHINCAL ERROR - loc fail \n" + e);
                }*/
            }
            catch
            {
                BlockDebug.ThrowWarning(false, "TerraTechModExt: \nModuleUsageHint.EditHint TECHINCAL ERROR \nCause of error - Block " + subjectName);
            }
        }

        /// <summary>
        /// To test the <see cref="ExtUsageHint"/> system with. Can also be done in the in-game wiki tools
        /// </summary>
        public static void ShowRandomExternalHint()
        {
            if (extHintsIDLookupInv.Any())
            {
                var entry = extHintsIDLookupInv.ToList().GetRandomEntry();
                ShowHint((GameHints.HintID)entry.Key, 3, true);
            }
        }
        /// <summary>
        /// Show a block hint
        /// </summary>
        /// <param name="subjectName"></param>
        /// <param name="blockID"></param>
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
        /// <summary>
        /// Show a specific hint
        /// </summary>
        /// <param name="UH"></param>
        /// <returns>True if it showed</returns>
        public static bool ShowExistingHint(UsageHint UH)
        {
            try
            {
                if (!HintsSeen.Contains(UH.stringID))
                    return ShowHint((GameHints.HintID)UH.assignedID, UH.displayDuration, UH.repeat);
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

        /// <summary>
        /// The default time a <see cref="UsageHint"/> will be displayed for
        /// </summary>
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
