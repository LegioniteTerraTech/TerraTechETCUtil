using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using System.Collections;
using System.Runtime.Serialization;
using static UnityEngine.TouchScreenKeyboard;



#if !EDITOR
using Steamworks;
#endif

namespace TerraTechETCUtil
{
    /// <summary>
    /// The status of a mod is determined by <see cref="ModStatusChecker"/> on mod loading via 
    /// <see cref="ModStatusChecker.EncapsulateSafeInit(string, Action, Action, bool)"/>
    /// </summary>
    public enum EModStatus : byte
    {
        /// <summary> TTSMM is not installed! </summary>
        ModManagerMissing,
        /// <summary> Crashed on startup call in <see cref="ModStatusChecker.EncapsulateSafeInit(string, Action, Action, bool)"/> </summary>
        Exception,
        /// <summary> <see cref="TerraTechETCUtil"/> crashed itself while initing the mod! <b>VERY BAD!</b> </summary>
        TTETCUtilException,
        /// <summary> <see cref="TerraTechETCUtil"/> with the mod is outdated and may pose a mod stability risk.  Please update. </summary>
        TTETCUtilOutdated,
        /// <summary> Mod was made for a newer version of TerraTech... <b>I hope you aren't <i>pirating...</i></b> </summary>
        GameOutdated,
        /// <summary> Mod loaded in just fine with no errors.  Horray! </summary>
        UpToDate,
        /// <summary> Mod loaded in just fine with no errors.  Horray! But we didn't check for outdatedness...</summary>
        SkippedOnlineCheck,
        /// <summary> Mod is still updating as the game is launching??? </summary>
        ModUpdating,
        /// <summary> Mod is outdated as there is a new version of it on Steam Workshop </summary>
        NeedsToBeUpdated,
        /// <summary> The mod installed has a ModID that does not match itself??? </summary>
        ModIDMismatch,
        /// <summary> Mod is older than the game itself.  It might be out of date and be broken... </summary>
        MightBeOutOfDate,
        /// <summary> Mod is seen, but absent from the disk??? </summary>
        MissingFromDisk,
        /// <summary> Mod exists on disk but does not exist on the Steam Workshop? </summary>
        FailedToFetch,
    }
    /// <summary>
    /// Keep track of the game's version
    /// </summary>
    public struct GameVersion
    {
        /// <summary> </summary>
        public bool Valid;
        /// <summary> </summary>
        public readonly int Major;
        /// <summary> </summary>
        public readonly int Mid;
        /// <summary> </summary>
        public readonly int Minor;
        /// <summary> </summary>
        public readonly int Unstable;
        /// <summary> </summary>
        public static GameVersion Default => new GameVersion("0");
        /// <summary>
        /// Creates a <see cref="GameVersion"/> for use in determining the state of the game from a string.
        /// <para>Usually found at <see cref="SKU.DisplayVersion"/></para>
        /// </summary>
        /// <param name="SKU_DisplayVersion"></param>
        public GameVersion(string SKU_DisplayVersion)
        {
            if (SKU_DisplayVersion.NullOrEmpty())
            {
                Valid = false;
                Major = -1;
                Mid = -1;
                Minor = -1;
                Unstable = -1;
                return;
            }
            var strings = SKU_DisplayVersion.Split('.');
            int result;
            if (strings.Length > 0 && int.TryParse(strings[0], out result))
                Major = result;
            else
                Major = 0;
            if (strings.Length > 1 && int.TryParse(strings[1], out result))
                Mid = result;
            else
                Mid = 0;
            if (strings.Length > 2 && int.TryParse(strings[2], out result))
                Minor = result;
            else
                Minor = 0;
            if (strings.Length > 3 && int.TryParse(strings[3], out result))
                Unstable = result;
            else
                Unstable = 0;
            Valid = true;
        }

        /// <inheritdoc/>
        public static bool operator ==(GameVersion a, GameVersion b) => (a.Valid == b.Valid) &&
            (a.Major == b.Major) && (a.Mid == b.Mid) && (a.Minor == b.Minor) && (a.Unstable == b.Unstable);
        /// <inheritdoc/>
        public static bool operator !=(GameVersion a, GameVersion b) => (a.Valid != b.Valid) ||
            (a.Major != b.Major) || (a.Mid != b.Mid) || (a.Minor != b.Minor) || (a.Unstable != b.Unstable);

        /// <inheritdoc/>
        public static bool operator >(GameVersion a, GameVersion b) => (a.Valid && !b.Valid) ||
            a.Major > b.Major || a.Mid > b.Mid || a.Minor > b.Minor || a.Unstable > b.Unstable;
        /// <inheritdoc/>
        public static bool operator <(GameVersion a, GameVersion b) => (!a.Valid && b.Valid) ||
            a.Major < b.Major || a.Mid < b.Mid || a.Minor < b.Minor || a.Unstable < b.Unstable;
        /// <inheritdoc/>
        public static bool operator >=(GameVersion a, GameVersion b) => a.Valid || (!a.Valid && !b.Valid) ||
            a.Major >= b.Major || a.Mid >= b.Mid || a.Minor >= b.Minor || a.Unstable >= b.Unstable;
        /// <inheritdoc/>
        public static bool operator <=(GameVersion a, GameVersion b) => b.Valid || (!a.Valid && !b.Valid) ||
            a.Major <= b.Major || a.Mid <= b.Mid || a.Minor <= b.Minor || a.Unstable <= b.Unstable;

        /// <inheritdoc/>
        public override string ToString()
        {
            return Major + "." + Mid + "." + Minor + 
                (Unstable > 0 ? "." + Unstable : "");
        }
        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is GameVersion version &&
                   Valid == version.Valid &&
                   Major == version.Major &&
                   Mid == version.Mid &&
                   Minor == version.Minor &&
                   Unstable == version.Unstable;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            int hashCode = 1391761443;
            hashCode = hashCode * -1521134295 + Valid.GetHashCode();
            hashCode = hashCode * -1521134295 + Major.GetHashCode();
            hashCode = hashCode * -1521134295 + Mid.GetHashCode();
            hashCode = hashCode * -1521134295 + Minor.GetHashCode();
            hashCode = hashCode * -1521134295 + Unstable.GetHashCode();
            return hashCode;
        }
    }
    /// <summary>
    /// The status of a mod loaded through <see cref="ModStatusChecker.EncapsulateSafeInit(string, Action, Action, bool)"/>
    /// </summary>
    public class ModStatus
    {
        /// <summary>
        /// Name of the mod, the <b>ModID</b>
        /// </summary>
        public readonly string name;
        /// <summary>
        /// The loaded assembly of the mod.
        /// </summary>
        public Assembly assembly { get; internal set; }
        /// <summary>
        /// Current status of the mod
        /// </summary>
        public EModStatus status { get; internal set; }
        /// <summary>
        /// The site of the exception if this mod failed to initiate, if applicable
        /// </summary>
        public MethodBase site { get; internal set; }
        private string _exception = "NULL";
        /// <summary>
        /// Gets a cleaned version of the exception on mod initiation if that ever happened
        /// </summary>
        public string exception {
            get => _exception;
            internal set
            {
                try
                {
                    int countMax = value.Length;
                    string cleaned = value;
                    bool junkWithin = true;
                    while (junkWithin)
                    {
                        int startIndex = cleaned.IndexOf("[0x");
                        int endIndex = cleaned.IndexOf(">:0");
                        if (startIndex != -1 && endIndex != -1)
                        {
                            cleaned = cleaned.Replace(cleaned.Substring(startIndex, endIndex - startIndex + 4), "");
                        }
                        else
                            junkWithin = false;
                    }
                    _exception = cleaned;
                }
                catch (Exception e)
                {
                    _exception = "!!!LOG IS BLOATED!!!! \n" + e;
                }
            }
        }

        internal ModStatus(string modName, EModStatus stat, string Exception = "Active")
        {
            name = modName;
            status = stat;
            exception = Exception;
        }

        internal ModStatus(string modName, string Exception)
        {
            name = modName;
            status = EModStatus.FailedToFetch;
            exception = Exception;
        }

    }

    /// <summary>
    /// An advanced mod startup checker that reports to the user if anything goes wrong
    /// </summary>
    public class ModStatusChecker : ITinySettings
    {
#if !EDITOR
        private static readonly FieldInfo SessionData = typeof(ManMods).GetField("m_CurrentSession", BindingFlags.Instance | BindingFlags.NonPublic);
#endif
        private static readonly string DataDirectory = "ModKickStartHelper";

        /// <summary>
        /// The mod status strings to display to the user according to error status
        /// </summary>
        public static Dictionary<EModStatus, string[]> ModStatusLOC = new Dictionary<EModStatus, string[]>
        {
            { EModStatus.ModManagerMissing, new string[]{"Requires TTSMM & 0ModManager", 
                "You will need to subscribe and download 0ModManager to get this mod to work properly." } },
            { EModStatus.Exception, new string[]{"Mod Startup Crash",
                "The mod (or one of it's dependancies) encountered an error on startup and has not applied itself to the game correctly.  You should disable or unsubscribe from the mod until it is fixed." } },
            { EModStatus.TTETCUtilException, new string[]{"TerraTechETCUtil Startup Crash",
                "The mod's backend encountered a serious error and failed to startup correctly. You should disable or unsubscribe from the mod until it is fixed." } },
            { EModStatus.TTETCUtilOutdated, new string[]{"Outdated TerraTechETCUtil Version",
                "The mod's backend is conflicting with backend of the same type, but different version.  There is a chance the mod may encounter serious errors!" }  },
            { EModStatus.UpToDate, new string[]{"Working",
                "The mod has started up properly with no issues." }  },
            { EModStatus.SkippedOnlineCheck, new string[]{"Working",
                "The mod has started up properly with no issues. We skipped the online check." }  },
            { EModStatus.ModUpdating, new string[]{"Mod is still updating",
                "The mod is still being downloaded." }  },
            { EModStatus.NeedsToBeUpdated, new string[]{"Mod is outdated",
                "There is a new version of the mod on Steam Workshop.  Make sure to quit the game to update the mod.  If it fails to update, restart your computer." }  },
            { EModStatus.ModIDMismatch, new string[]{"Mod ID does not match",
                "A technical issue has occurred as the mod's" }  },
            { EModStatus.MightBeOutOfDate, new string[]{"Mod might need update",
                "The game was updated but the mod has not been updated yet.  There is a chance some things broke.  Proceed at your own risk!" }  },
            { EModStatus.MissingFromDisk, new string[]{"Mod is not downloaded",
                "The mod is not downloaded.  It will not be able to launch." }  },
            { EModStatus.FailedToFetch, new string[]{"Mod is not present on Steam Workshop",
                "The mod does not exist on the Steam Workshop." }  },
        };

        private static Rect guiWindow = new Rect(20, 20, 800, 600);
        private static Vector2 guiSlider = Vector2.zero;
        private static ModStatusChecker inst;
        private static GUIInst instGUI;

        /// <summary>
        /// Ping Steam to get data on mod update statuses every time the game is started up. 
        /// <para>Turn this off via code call if you are frequently booting the game and need to save your Steam API tokens.</para>
        /// </summary>
        public bool CheckOnlineStatus = true;
        /// <summary>
        /// The last version the <see cref="ModStatusChecker"/> was launched under to warn the user every time the game is updated
        /// </summary>
        public string LastVersion = new GameVersion(SKU.DisplayVersion).ToString();
        /// <summary>
        /// 0. Show Always
        /// <list type="number">
        /// <item>Show on Warnings</item>
        /// <item>Show Errors</item>
        /// <item>Never Show</item>
        /// </list>
        /// </summary>
        public int ShowSetting = 0;
        /// <summary>
        /// What setting the user has set <see cref="ModStatusChecker"/> to display on startup
        /// </summary>
        public static bool ShowWarnings => inst.ShowSetting <= 1;

        /// <inheritdoc/>
        public string DirectoryInExtModSettings => DataDirectory;

        private static Dictionary<string, ModStatus> modStatuses = new Dictionary<string, ModStatus>();

        private const int ExtDebugID = 1002247;

        private static string[] options = new string[] { "Always", "Warnings", "Error", "Off" };
        private class GUIInst : MonoBehaviour
        {
            public void OnGUI()
            {
                try
                {
                    if (failed)
                        guiWindow = AltUI.Window(ExtDebugID, guiWindow, DoGUI, "Mod Startup Error", CloseGUI, true, true);
                    else
                        guiWindow = AltUI.Window(ExtDebugID, guiWindow, DoGUI, "Mod Startup", CloseGUI, true, true);
                }
                catch (ExitGUIException e) 
                {
                    throw e;
                }
                catch (Exception) { }
            }
        }
        private static ModStatus selected = null;
        internal static void DoGUI(int ID)
        {
            guiSlider = GUILayout.BeginScrollView(guiSlider);
            if (ETCUtilFailed)
            {
                GUILayout.Label("TerraTechETCUtil (backbone code of listed mods) failed to start!", AltUI.LabelRedTitle);
                context = "TerraTechETCUtil has failed to initialize!\n" +
                    "TerraTechETCUtil hosts dependancies for the mods below and because it is broken, the mods will be broken";
            }
            if (failed)
            {
                GUILayout.Label("One or more mods have crashed on startup.  This reporter may not support all your mods!");
                if (!Is0MMAvail)
                {
                    GUILayout.Label("  Crashes can be resolved by un-subscribing from the problem mods.");
                    GUILayout.Label("0ModManager is HIGHLY recommended if you have startup issues, which could be the result of missing or incorrect dependancies.");
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("0ModManager Workshop Page [LINK]", AltUI.ButtonOrangeLarge, GUILayout.Height(40)))
                        ManSteamworks.inst.OpenOverlayURL("https://steamcommunity.com/sharedfiles/filedetails/?id=2790161231");
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                }
                else
                    GUILayout.Label("  Crashes can be resolved by disabling the mod in TTSMM with the problems. ", AltUI.TextfieldBlackAdjusted);
            }
            GUILayout.TextField(context, AltUI.TextfieldBlackHuge, GUILayout.ExpandHeight(false), GUILayout.MaxHeight(140), GUILayout.MaxWidth(750));
            foreach (var item in modStatuses)
            {
                GUILayout.BeginHorizontal();
                if (selected == item.Value)
                    GUILayout.Label(item.Key, AltUI.LabelBlue);
                else
                    GUILayout.Label(item.Key);
                GUILayout.FlexibleSpace();
                switch (item.Value.status)
                {
                    case EModStatus.FailedToFetch:
                    case EModStatus.ModManagerMissing:
                        if (GUILayout.Button(ModStatusLOC[item.Value.status][0], AltUI.ButtonGrey))
                        {
                            selected = item.Value;
                            context = item.Value.exception;
                        }
                        AltUI.Tooltip.GUITooltip(ModStatusLOC[item.Value.status][1]);
                        break;
                    case EModStatus.MissingFromDisk:
                    case EModStatus.GameOutdated:
                    case EModStatus.TTETCUtilOutdated:
                        if (GUILayout.Button(ModStatusLOC[item.Value.status][0], AltUI.ButtonRed))
                        {
                            selected = item.Value;
                            context = item.Value.exception;
                        }
                        AltUI.Tooltip.GUITooltip(ModStatusLOC[item.Value.status][1]);
                        break;
                    case EModStatus.Exception:
                        if (TryGetSteamWorkshopID(item.Key, out ulong uID))
                        {
                            if (GUILayout.Button("Open Workshop [LINK]", AltUI.ButtonRed))
                                ManSteamworks.inst.OpenOverlayURL("https://steamcommunity.com/sharedfiles/filedetails/?id=" + uID.ToString());
                            AltUI.Tooltip.GUITooltip("Make sure this is the right mod!\nIf you would like to, you can let the mod maker know of your crash in the comments section.");
                        }
                        if (GUILayout.Button(ModStatusLOC[item.Value.status][0], AltUI.ButtonRed))
                        {
                            selected = item.Value;
                            context = item.Value.exception;
                        }
                        AltUI.Tooltip.GUITooltip(ModStatusLOC[item.Value.status][1]);
                        break;
                    case EModStatus.TTETCUtilException:
                        if (GUILayout.Button("Open Workshop [LINK]", AltUI.ButtonRed))
                            ManSteamworks.inst.OpenOverlayURL("https://steamcommunity.com/sharedfiles/filedetails/?id=2765334969");
                        AltUI.Tooltip.GUITooltip("This will lead to the RandomAdditions workshop page, which handles TerraTechETCUtil error reports.");
                        if (GUILayout.Button(ModStatusLOC[item.Value.status][0], AltUI.ButtonRed))
                        {
                            selected = item.Value;
                            context = item.Value.exception;
                        }
                        AltUI.Tooltip.GUITooltip(ModStatusLOC[item.Value.status][1]);
                        break;
                    case EModStatus.UpToDate:
                        if (GUILayout.Button(ModStatusLOC[item.Value.status][0], AltUI.ButtonGreenActive))
                        {
                            selected = item.Value;
                            context = item.Value.exception;
                        }
                        AltUI.Tooltip.GUITooltip(ModStatusLOC[item.Value.status][1]);
                        break;
                    case EModStatus.MightBeOutOfDate:
                        if (GUILayout.Button(ModStatusLOC[item.Value.status][0], AltUI.ButtonGreen))
                        {
                            selected = item.Value;
                            context = item.Value.exception;
                        }
                        AltUI.Tooltip.GUITooltip(ModStatusLOC[item.Value.status][1]);
                        break;
                    case EModStatus.NeedsToBeUpdated:
                    case EModStatus.ModUpdating:
                        if (GUILayout.Button(ModStatusLOC[item.Value.status][0], AltUI.ButtonBlue))
                        {
                            selected = item.Value;
                            context = item.Value.exception;
                        }
                        AltUI.Tooltip.GUITooltip(ModStatusLOC[item.Value.status][1]);
                        break;
                    default:
                        if (GUILayout.Button(item.Value.status.ToString(), AltUI.TextfieldBordered))
                        {
                            selected = item.Value;
                            context = item.Value.exception;
                        }
                        break;
                }
                GUILayout.EndHorizontal();
            }
            switch (inst.ShowSetting)
            {
                case 0:
                    GUILayout.Label("Always show on mod initialization");
                    break;
                case 1:
                    GUILayout.Label("Show on mod warning");
                    break;
                case 2:
                    GUILayout.Label("Show on mod error");
                    break;
                case 3:
                    GUILayout.Label("This will appear again after a game update");
                    break;
            }
            int set = GUILayout.Toolbar(inst.ShowSetting, options, AltUI.ButtonBlue);
            var curVer = new GameVersion(SKU.DisplayVersion);
            if (set != inst.ShowSetting || new GameVersion(inst.LastVersion) != curVer)
            {
                inst.LastVersion = curVer.ToString();
                inst.ShowSetting = set;
                if (!inst.TrySaveToDisk())
                    Debug_TTExt.Assert("Failed to save startup settings to file!");
            }
            GUILayout.EndScrollView();
            UIHelpersExt.ClampGUIToScreen(ref guiWindow, false);
        }
        internal static void CloseGUI()
        {
            instGUI.gameObject.SetActive(false);
        }

        private static bool TryGetSteamWorkshopID(string ModStringID, out ulong uID)
        {
            ModSessionInfo data = (ModSessionInfo)SessionData.GetValue(ManMods.inst);
            uID = 0;
            return data != null && data.Mods.TryGetValue(ModStringID, out uID);
        }

        /// <summary>
        /// Check to see if <see cref="ModManager"/> is present
        /// </summary>
        public static bool Is0MMAvail => LookForMod("NLogManager");//LookForMod("0ModManager");

        /// <summary>
        /// Check to see if <b>RandomAdditions</b> is present
        /// </summary>
        public static bool IsRandomAdditionsAvail => LookForMod("RandomAdditions");
        /// <summary>
        /// Check to see if <b>WaterMod</b> or it's other variant is present
        /// </summary>
        public static bool IsWaterModAvail => LookForMod("Water Mod") || LookForMod("Water Mod + Lava");

        /// <summary>
        /// Check to see if <b>ConfigHelper</b> is present
        /// </summary>
        public static bool IsConfigHelperPresent => LookForMod("ConfigHelper");
        /// <summary>
        /// Check to see if <b>Nuterra.NativeOptions</b> is present
        /// </summary>
        public static bool IsNativeOptionsPresent => LookForMod("0Nuterra.NativeOptions");

        private static bool failed = false;
        private static bool chainFailed = false;
        private static bool ETCUtilFailed = false;
        /// <summary>
        /// A non-repeating error has occurred in one of our mod boot attempts and this is bad
        /// </summary>
        public static bool InitFailiure => failed;
        /// <summary>
        /// A repeating error has occurred in one of our mod boot attempts and this is REALLY bad
        /// </summary>
        public static bool EpicFailiure => chainFailed;
        /// <summary>
        /// TerraTechETCUtil (this class' dll) failed to init entirely, which will probably bring down all mods that rely on it!!!
        /// </summary>
        public static bool OutstandingFailiure => ETCUtilFailed;
        private static string context = "NULL";
        private static string errorReport = "Nothing here!?!?";

        internal static void EpicFail()
        {
            if (!ETCUtilFailed)
            {
                ETCUtilFailed = true;
                ShowErrorLog(AltUI.EnemyString("TerraTechETCUtil (Backend shared code of the following mods) has failed to initialize!"));
            }
        }
        /// <summary>
        /// Look for a mod of a name in the current <see cref="AppDomain"/> of the game
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool LookForMod(string name)
        {
            if (name == "RandomAdditions")
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (assembly.FullName.StartsWith(name))
                    {
                        if (assembly.GetType("KickStart") != null)
                            return true;
                    }
                }
            }
            else
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (assembly.FullName.StartsWith(name))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Look for a <see cref="Type"/> of given type name
        /// </summary>
        /// <param name="name"></param>
        /// <returns><see cref="Type"/> if found, otherwise null</returns>
        public static Type LookForType(string name)
        {
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                var typeGet = assembly.GetType(name);
                if (typeGet != null)
                    return typeGet;
            }
            return null;
        }
        /// <summary>
        /// Check if we can actually use mod options correctly
        /// </summary>
        /// <returns>True if both <b>ConfigHelper</b> AND <b>NativeOptions</b> is present</returns>
        public static bool IsModOptionsAvailable() => IsConfigHelperPresent && IsNativeOptionsPresent;


        private static void ThrowDelayedErrorForRandAdd()
        {
            if (Singleton.Manager<ManUI>.inst != null)
            {
                InvokeHelper.CancelInvokeSingleRepeat(ThrowDelayedErrorForRandAdd);
                //throw new Exception(errorReport);
            }
        }

        private static void ShowErrorLog(string Context)
        {
            if (instGUI == null)
            {
                instGUI = new GameObject("TempError").AddComponent<GUIInst>();
            }
            context = Context;
            Debug_TTExt.Log(Context);
        }
        /// <summary>
        /// Inits a mod with enhanced error handling.
        /// </summary>
        /// <param name="ModID">The string ModID of a mod, which is given when making it in TerraTechModTool</param>
        /// <param name="init">The function that starts up the mod.  Must be a function in the mod itself.</param>
        /// <param name="onFail">Called if the function hit an exception</param>
        /// <param name="doChainFail">Stop the next mods that are to be loaded using EncapsulateSafeInit()</param>
        public static void EncapsulateSafeInit(string ModID, Action init, Action onFail = null, bool doChainFail = false)
        {
            Assembly CallerAssembly = null;
            try
            {
                if (inst == null)
                {
                    Debug_TTExt.Log("Current game version is " + SKU.DisplayVersion);
                    ResourcesHelper.CheckTTExtUtils();
                    inst = new ModStatusChecker();
                    
                    if (inst.TryLoadFromDisk(ref inst))
                    {
                        Debug_TTExt.Log("ModStatusChecker.ShowSetting is " + inst.ShowSetting);
                        if (new GameVersion(inst.LastVersion) != new GameVersion(SKU.DisplayVersion))
                            ShowErrorLog("Game Updated from [" + inst.LastVersion + "] to [" + SKU.DisplayVersion + "]\n- Be warned as some mods might be broken");
                        else if (inst.ShowSetting == 0)
                            ShowErrorLog("Active each startup - No immedeate startup issues detected");
                    }
                    else
                        ShowErrorLog("First Initialization");

                    /*
                    if (ActiveGameInterop.CheckIfInteropActiveGameOnly())
                    {
                        ShowErrorLog("Editor Detected, Hooked Up!");
                        ActiveGameInterop.Init();
                        InvokeHelper.InvokeSingleRepeat(ActiveGameInterop.UpdateNow, 1);
                    }
                    */
                }
                CallerAssembly = init.GetType().Assembly;
                var status = CheckStatusOfMod(ModID);
                if (!modStatuses.ContainsKey(ModID))
                    modStatuses.Add(ModID, status);
                status.assembly = CallerAssembly;
                if (!chainFailed)
                    init.Invoke();
                switch (inst.ShowSetting)
                {
                    case 1:
                    case 2:
                        if (ShowWarnings)
                        {
                            switch (status.status)
                            {
                                case EModStatus.ModManagerMissing:
                                    break;
                                case EModStatus.Exception:
                                    break;
                                case EModStatus.TTETCUtilException:
                                    break;
                                case EModStatus.TTETCUtilOutdated:
                                    break;
                                case EModStatus.GameOutdated:
                                    break;
                                case EModStatus.UpToDate:
                                    break;
                                case EModStatus.ModUpdating:
                                    break;
                                case EModStatus.NeedsToBeUpdated:
                                    break;
                                case EModStatus.MightBeOutOfDate:
                                    break;
                                case EModStatus.ModIDMismatch:
                                    break;
                                case EModStatus.MissingFromDisk:
                                    break;
                                case EModStatus.FailedToFetch:
                                    break;
                                default:
                                    break;
                            }
                        }
                        switch (status.status)
                        {
                            case EModStatus.ModManagerMissing:
                                ShowErrorLog(ModID + " - Mod needs 0ModManager to function.  Failed to boot correctly.");
                                break;
                            case EModStatus.TTETCUtilException:
                            case EModStatus.Exception:
                                break;
                            case EModStatus.TTETCUtilOutdated:
                                string lazyCombiner = "";

#if !EDITOR
                                foreach (var item in ResourcesHelper.GUIManaged.infoCache)
                                {
                                    lazyCombiner += item.Key.ModID + ", ";
                                }
                                ShowErrorLog(ModID + " - Mod Shared API is outdated.\nInsure the following mods are updated on Steam Workshop:\n" +
                                    lazyCombiner);
#endif
                                break;
                            case EModStatus.GameOutdated:
                                ShowErrorLog(ModID + " - TerraTech is outdated.\nInstall the latest game update");
                                break;
                            case EModStatus.UpToDate:
                                break;
                            case EModStatus.ModUpdating:
                                ShowErrorLog(ModID + " - Mod is updating!\nMake sure Steam Workshop is finished downloading as mods that aren't downloaded fully cannot be executed.");
                                break;
                            case EModStatus.NeedsToBeUpdated:
                                if (ShowWarnings)
                                    ShowErrorLog(ModID + " - Mod is outdated.\nIf it crashes, insure the mod is updated on Steam Workshop");
                                break;
                            case EModStatus.ModIDMismatch:
                                ShowErrorLog(ModID + " - Mod ID in the DLL is incorrect and ResourceHelper cannot access the data correctly. " +
                                    "If a crash happens it may be due to not being able to access it's resources!");
                                break;
                            case EModStatus.MightBeOutOfDate:
                                if (ShowWarnings)
                                    ShowErrorLog(ModID + " - Mod may not work in the Unstable.\nIf it crashes, insure the mod is updated on Steam Workshop");
                                break;
                            case EModStatus.MissingFromDisk:
                                ShowErrorLog(ModID + " - Mod is missing or corrupted.\nThis might be due to parental controls or a security program.");
                                break;
                            case EModStatus.FailedToFetch:
                                ShowErrorLog(ModID + " - Could not fetch data for mod.\nThis might be due to parental controls or a security program.");
                                break;
                            default:
                                break;
                        }
                        break;
                }
            }
            catch (Exception e)
            {
                failed = true;
                if (doChainFail)
                    chainFailed = true;
                selected = modStatuses[ModID];
                string failReport;
                Assembly problemAssembly = e.TargetSite?.DeclaringType?.Assembly;
                if (problemAssembly == Assembly.GetExecutingAssembly())
                {
                    modStatuses[ModID].status = EModStatus.TTETCUtilException;
                    failReport = "Initialization Failiure within TerraTechETCUtil, invoked by " + ModID + ": " + e;
                }
                else if (problemAssembly == CallerAssembly)
                {
                    modStatuses[ModID].status = EModStatus.Exception;
                    failReport = "Initialization Failiure within " + ModID + ": " + e;
                }
                else
                {
                    modStatuses[ModID].status = EModStatus.Exception;
                    if (problemAssembly.FullName == "Assembly-CSharp")
                        failReport = "Initialization Failiure within call to base game, invoked by " + ModID + ": " + e;
                    else
                    {
                        failReport = null;
                        foreach (var item in modStatuses)
                        {
                            if (item.Value.assembly == problemAssembly)
                                failReport = "Initialization Failiure within mod dll " + item.Key + ", invoked by " + ModID + ": " + e;
                        }
                        if (failReport == null)
                            failReport = "Initialization Failiure within dll " + problemAssembly.FullName + ", invoked by " + ModID + ": " + e;
                    }
                }
                Debug_TTExt.Log(failReport);
                try
                {
                    if (onFail != null)
                        onFail.Invoke();
                    modStatuses[ModID].exception = e.ToString();
                }
                catch (Exception e2)
                {
                    modStatuses[ModID].exception = e.ToString() + "\n\nFallback onFail Action failed to execute as well - " + e2.ToString();
                }
                if (IsRandomAdditionsAvail)
                {
                    // Delay the crash and deny loading of our other mods for now at least
                    errorReport = failReport;
                    InvokeHelper.InvokeSingleRepeat(ThrowDelayedErrorForRandAdd, 1);
                }
                modStatuses[ModID].site = e.TargetSite;
                switch (inst.ShowSetting)
                {
                    case 0:
                    case 1:
                    case 2:
                        ShowErrorLog(ModID + " - " + modStatuses[ModID].exception);
                        break;
                }
            }
        }
        /// <summary>
        /// Check the status of the mod now and return a <see cref="ModStatus"/> with detailed info on it
        /// </summary>
        /// <param name="name">has to be the EXACT name of the mod!</param>
        /// <returns><see cref="ModStatus"/> with infomration on the mod's startup</returns>
        public static ModStatus CheckStatusOfMod(string name)
        {
            try
            {
                return CheckStatusOfMod_Internal(name);
            }
            catch (Exception e)
            {
                Debug_TTExt.Log("CheckStatusOfMod: Could not fetch details of mod " + name +
                    " since an error occurred - " + e);
                return new ModStatus(name, e.ToString());
            }
        }
        private static ModStatus CheckStatusOfMod_Internal(string name)
        {
            try
            {
                if (modStatuses.TryGetValue(name, out ModStatus status))
                    return status;
                ModSessionInfo data = (ModSessionInfo)SessionData.GetValue(ManMods.inst);
                if (data == null)
                {
                    throw new NullReferenceException("Could not fetch mod session data!");
                }
                if (data.Mods == null)
                {
                    throw new NullReferenceException("Could not fetch mod session mods list!");
                }
                if (data.Mods.TryGetValue(name, out var modID))
                    return CheckStatusOfMod_Internal(name, modID);
            }
            catch (FileNotFoundException e)
            {
                if (e.Message.Contains(".dll"))
                {
                    Debug_TTExt.Log("CheckStatusOfMod_Internal: Could not fetch details of mod " + name +
                        " since a vital dependancy was missing - " + e);
                    return new ModStatus(name, EModStatus.Exception, new FileNotFoundException("CheckStatusOfMod_Internal: Could not fetch details of mod " + name +
                        " since a vital dependancy was missing - " + e).ToString());
                    /*
                    if (IsTTSMMAvail)
                    {
                    }
                    else
                    {
                        Debug_TTExt.Log("Could not fetch details of mod " + name + " since 0ModManager is not present");
                        return new ModStatus(name, EModStatus.ModManagerMissing);
                    }*/
                }
                else
                { 
                }
                return new ModStatus(name, EModStatus.Exception, e.ToString());
            }
            catch (Exception e)
            {
                Debug_TTExt.Log("CheckStatusOfMod_Internal: Could not fetch details of mod " + name + 
                    " since an error occurred - " + e);
                return new ModStatus(name, EModStatus.Exception, e.ToString());
            }
            return new ModStatus(name, "Mod does not exist");
        }

        private static ModStatus CheckStatusOfMod_Internal(string name, ulong modID)
        {
            try
            {
                if (modStatuses.TryGetValue(name, out ModStatus mStatus))
                    return mStatus;
                if (!ResourcesHelper.TryGetModContainer(name, out _))
                {
                    StringBuilder SB = new StringBuilder();
                    SB.AppendLine(">- " + name);
                    foreach (var item in ResourcesHelper.GetAllMods().Values)
                    {
                        SB.AppendLine(" - " + item.ModID);
                    }
                    Debug_TTExt.LogError("ModID " + name + " is invalid: \n" + SB.ToString());
                    return new ModStatus(name, EModStatus.ModIDMismatch);
                }
#if !EDITOR
                var ID = new PublishedFileId_t(modID);
                if (SteamUGC.GetItemInstallInfo(ID, out ulong sizeBytes, out string directed, 512U, out uint lastUpdateTime))
                {
                    DirectoryInfo directory = new DirectoryInfo(directed);
                    if (!directory.Exists)
                    {
                        Debug_TTExt.LogError("Could not find directory of item " + name);
                        return new ModStatus(name, EModStatus.MissingFromDisk);
                    }
                    FileInfo fileData = directory.GetFiles().FirstOrDefault(x => x.Name == "SteamVersion");
                    if (fileData != null)
                        return new ModStatus(name, EModStatus.UpToDate);

                    if (inst.CheckOnlineStatus)
                    {

                        EItemState state = (EItemState)SteamUGC.GetItemState(ID);
                        if ((state & EItemState.k_EItemStateInstalled) == 0)
                        {
                            Debug_TTExt.LogError("Could not load data of item " + name + " because it is not installed.");
                            return new ModStatus(name, EModStatus.MissingFromDisk);
                        }
                        EModStatus status = EModStatus.FailedToFetch;
                        if ((state & EItemState.k_EItemStateDownloadPending) > 0 ||
                            (state & EItemState.k_EItemStateDownloading) > 0)
                        {
                            status = EModStatus.ModUpdating;
                        }
                        else
                        {
                            if ((state & EItemState.k_EItemStateInstalled) > 0)
                            {
                                if ((state & EItemState.k_EItemStateNeedsUpdate) > 0)
                                    status = EModStatus.NeedsToBeUpdated;
                                else
                                {
                                    if (ResourcesHelper.TryGetModContainer(name, out var MC) &&
                                        ResourcesHelper.GUIManaged.infoCache.TryGetValue(MC, out var val))
                                    {
                                        if (!val.synced)
                                            return new ModStatus(name, EModStatus.TTETCUtilOutdated);
                                        DateTime DT = Directory.GetLastWriteTime(directory.ToString());
                                        DateTime DTM = File.GetLastWriteTime(Assembly.GetAssembly(typeof(TankBlock)).Location);
                                        //Debug_TTExt.Log("Time is " + DT.ToString() + " vs " + DTM.ToString());
                                        if (DT < DTM)
                                            return new ModStatus(name, EModStatus.MightBeOutOfDate);
                                    }
                                    status = EModStatus.UpToDate;
                                }
                            }
                            else
                                status = EModStatus.MissingFromDisk;
                        }
                        return new ModStatus(name, status);
                    }
                    else
                        return new ModStatus(name, EModStatus.SkippedOnlineCheck);
                }
#endif
                return new ModStatus(name, EModStatus.FailedToFetch);
            }
            catch (Exception e)
            {
                Debug_TTExt.Log("CheckStatusOfMod_Internal(2): Could not fetch details of mod " + name +
                    " since an error occurred - " + e);
                return new ModStatus(name, EModStatus.Exception);
            }
            //return new ModStatus(name, EModStatus.ModManagerMissing);
        }

        private static void CheckStatusOfAllMods_Internal()
        {
            try
            {
                ModSessionInfo data = (ModSessionInfo)SessionData.GetValue(ManMods.inst);
                foreach (var item in data.Mods)
                {
                    modStatuses.Add(item.Key, CheckStatusOfMod_Internal(item.Key, item.Value));
                }
            }
            catch (Exception e)
            {
                Debug_TTExt.Log("CheckStatusOfAllMods_Internal: Could not fetch details of mods " +
                    "since an error occurred - " + e);
            }
        }
    }
}
