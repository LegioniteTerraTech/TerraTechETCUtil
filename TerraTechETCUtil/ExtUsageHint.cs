using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;

namespace TerraTechETCUtil
{
    public class ExtUsageHint : MonoBehaviour
    {
        private static FieldInfo
        defD = typeof(EnumString).GetField("m_EnumValueInt", BindingFlags.NonPublic | BindingFlags.Instance),
        locFA = typeof(Localisation).GetField("m_HashLookup", BindingFlags.NonPublic | BindingFlags.Instance);

        private static ExtUsageHint inst;
        private static List<ManHints.HintDefinition> extHints = new List<ManHints.HintDefinition>();
        public static List<int> HintsSeen = new List<int>();
        public static string HintsSeenSAV;

        internal static void Init()
        {
            if (inst)
                return;
            inst = new GameObject("ModUsageHintExt").AddComponent<ExtUsageHint>();
            EditHintInternal("ModuleClock", "ⁿMC", 4001, clockDesc);
            EditHintInternal("ModulePointDefense", "ⁿMPD", 4002, interceptDesc);
            EditHintInternal("ModuleRepairAimer", "ⁿMRA", 4003, repairDesc);
            EditHintInternal("ModuleModeSwitch", "ⁿMMS", 4004, modeSwitchDesc);
            EditHintInternal("ModuleItemSilo", "ⁿMIS", 4005, siloDesc);
            EditHintInternal("ModuleLudicrousSpeedButton", "ⁿMLSB", 4006, speeedDesc);
            EditHintInternal("ModuleFuelEnergyGenerator", "ⁿMFEG", 4007, fuelGenDesc);
            EditHintInternal("ModuleReinforced", "ⁿMR", 4008, eraDesc);
            EditHintInternal("ModuleHangar", "ⁿMH", 4009, hangarDesc);
            EditHintInternal("ModuleTractorBeam", "ⁿMTB", 4010, tracBeamDesc);
            EditHintInternal("ModuleOmniCore", "ⁿMOC", 4011, omniCoreDesc);
            EditHintInternal("ModuleJumpDrive", "ⁿMTD", 4012, jumpDesc);
        }

        private const float SlowUpdateTime = 1f;
        private float SlowUpdate = 0;
        internal void Update()
        {
            UpdateHintTimers();
        }

        public static void EditHint(string subjectName, string prefix, int blockID, string HintDescription)
        {
            try
            {
                ManHints.HintDefinition HD = new ManHints.HintDefinition();
                HD.m_HintIcon = UIHints.IconType.None;
                LocalisedString LS = new LocalisedString();
                LS.m_Bank = prefix + blockID.ToString();
                LS.m_Id = "ⁿ_" + blockID.ToString();
                HD.m_HintMessage = LS;
                EnumString ES = new EnumString(typeof(GameHints.HintID), 1);
                defD.SetValue(ES, blockID);
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
                BlockDebug.ThrowWarning("TerraTechModExt: \nModuleUsageHint TECHINCAL ERROR \nCause of error - Block " + subjectName);
            }
        }
         
        internal static void EditHintInternal(string subjectName, string prefix, int hintID, string HintDescription)
        {
            try
            {
                ManHints.HintDefinition HD = new ManHints.HintDefinition();
                HD.m_HintIcon = UIHints.IconType.None;
                LocalisedString LS = new LocalisedString();
                LS.m_Bank = prefix;
                LS.m_Id = prefix;
                HD.m_HintMessage = LS;
                EnumString ES = new EnumString(typeof(GameHints.HintID), 1);
                defD.SetValue(ES, hintID);
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
                        //Debug_TTExt.Log("TerraTechModExt: Refreshed the loc desc for module " + subjectName);
                    }
                    else
                        lingo.Add(lingoSearch, HintDescription);
                    locFA.SetValue(Localisation.inst, lingo);
                    //Debug_TTExt.Log("TerraTechModExt: Saved the loc desc for module " + subjectName);
                }
                catch (Exception e)
                {
                    BlockDebug.ThrowWarning("TerraTechModExt: \nExtUsageHint TECHINCAL ERROR - loc fail \n" + e);
                }
            }
            catch
            {
                BlockDebug.ThrowWarning("TerraTechModExt: \nModuleUsageHint TECHINCAL ERROR \nCause of error - Block " + subjectName);
            }
        }
        public static void ShowExistingHint(int hintID)
        {
            try
            {
                Init();
                GameHints.HintID hintNum = (GameHints.HintID)hintID;
                if (!HintsSeen.Contains(hintID))
                    ShowHint(hintNum);
            }
            catch (Exception e)
            {
                Debug_TTExt.Assert(true, e.Message);
            }
        }
        private static void ShowHint(GameHints.HintID hintID)
        {
            if (!ManNetwork.IsNetworked && ManHints.inst.HintsEnabled)
            {
                ManHints.HintDefinition hintDef = null;
                for (int step = 0; step < extHints.Count; step++)
                {
                    if (extHints.ElementAt(step).m_HintId.Value == (int)hintID)
                    {
                        hintDef = extHints[step];
                        break;
                    }
                }
                if (hintDef != null)
                {
                    UIHints.ShowContext showContext = new UIHints.ShowContext
                    {
                        hintID = hintID,
                        hintDef = hintDef
                    };
                    Singleton.Manager<ManHUD>.inst.ShowHudElement(ManHUD.HUDElementType.Hint, showContext);
                    toHide.Add(new KeyValuePair<float, UIHints.ShowContext>(Time.time + hintDisplayTime, showContext));
                    HintsSeen.Add((int)hintID);
                    HintsSeenToSave();

                    try
                    {
                        ConfigConnect.TrySaveConfigData();
                    }
                    catch
                    {
                        Debug_TTExt.Log("TerraTechModExt: Could not save to Config since ConfigHelper is absent.");
                    }

                    Singleton.Manager<ManSFX>.inst.PlayUISFX(ManSFX.UISfxType.Hint);
                }
            }
        }

        private static readonly float hintDisplayTime = 8;
        public static readonly List<KeyValuePair<float, UIHints.ShowContext>> toHide = new List<KeyValuePair<float, UIHints.ShowContext>>();
        public static void UpdateHintTimers()
        {   // Saving a Tech from the BlockMemory
            int iterating = 0;
            while (toHide.Count > iterating)
            {
                var item = toHide.ElementAt(iterating);
                if (item.Key <= Time.time)
                {
                    try
                    {
                        Singleton.Manager<ManHUD>.inst.HideHudElement(ManHUD.HUDElementType.Hint, item.Value.hintID);
                    }
                    catch (Exception e)
                    {
                        Debug_TTExt.Assert(true, e.Message);
                    }
                    toHide.RemoveAt(iterating);
                }
                else
                    iterating++;
            }
        }

        // Saving to File
        public static void HintsSeenToSave()
        {   // Saving a Tech from the BlockMemory
            List<int> mem = HintsSeen;
            if (mem.Count == 0)
                return;
            StringBuilder hintsSeenSav = new StringBuilder();
            hintsSeenSav.Append(mem[0]);
            for (int step = 1; step < mem.Count; step++)
            {
                hintsSeenSav.Append("|");
                hintsSeenSav.Append(mem[step]);
            }
            HintsSeenSAV = hintsSeenSav.ToString();
        }

        public static void SaveToHintsSeen()
        {   // Loading from memory
            HintsSeen.Clear();
            try
            {
                List<int> mem = new List<int>();
                StringBuilder blockCase = new StringBuilder();
                foreach (char ch in HintsSeenSAV)
                {
                    if (ch == '|')//new Int
                    {
                        mem.Add(int.Parse(blockCase.ToString()));
                        blockCase.Clear();
                    }
                    else
                        blockCase.Append(ch);
                }
                mem.Add(int.Parse(blockCase.ToString()));
                HintsSeen = mem;
            }
            catch (Exception e)
            {
                Debug_TTExt.Assert(true, e.Message);
            }
        }



        // Default-set ones for this mod
        private static readonly string
        eraDesc = "This block will greatly reduce the damage caused by explosive rounds on direct contact.",
        clockDesc = "This block keeps track of the world time.  Never be late again prospector!",
        interceptDesc = "This block can intercept some types of incoming projectiles.",
        repairDesc = "This block consumes power and can repair blocks slowly from very long distances.",
        modeSwitchDesc = "This weapon block automatically switches ammunition types based on the target.",
        siloDesc = "This base block can store stacks of chunks or blocks inside.",
        speeedDesc = "This base block can overclock all your base components by clicking on it.",
        fuelGenDesc = "This block burns fuel from your Fuel Tanks in exchange for some Battery Power.",
        omniCoreDesc = "This block can apply thrust in all directions to the center of your Tech.  It limits movement so be careful!",
        tracBeamDesc = "This block can carry other Techs through the power of tractor beams.",
        hangarDesc = "This block can store and deploy Techs! H + right-click on an allied Tech to store or drive close and right-click from another.",
        legsDesc = "This block is a leg block.  While it's slow, it has considerable grip and could even climb walls!",
        jumpDesc = "This block will let you jump to any tech you have in the world!";
    }
}
