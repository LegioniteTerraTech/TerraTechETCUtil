using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Reflection;
using UnityEngine;
#if !EDITOR
using ModManager;
using Steamworks;
using Payload.UI.Commands.Steam;
#endif

namespace TerraTechETCUtil
{
    public enum EModStatus : byte
    {
        ModManagerMissing,
        Exception, 
        TTExtUtilOutdated,
        GameOutdated,
        UpToDate,
        ModUpdating,
        NeedsToBeUpdated,
        ModIDMismatch,
        MightBeOutOfDate,
        MissingFromDisk,
        FailedToFetch,
    }

    public struct GameVersion
    {
        public bool Valid;
        public readonly int Major;
        public readonly int Mid;
        public readonly int Minor;
        public readonly int Unstable;
        public static GameVersion Default => new GameVersion("0");

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

        public static bool operator ==(GameVersion a, GameVersion b) => (a.Valid == b.Valid) &&
            (a.Major == b.Major) && (a.Mid == b.Mid) && (a.Minor == b.Minor) && (a.Unstable == b.Unstable);
        public static bool operator !=(GameVersion a, GameVersion b) => (a.Valid != b.Valid) ||
            (a.Major != b.Major) || (a.Mid != b.Mid) || (a.Minor != b.Minor) || (a.Unstable != b.Unstable);

        public static bool operator >(GameVersion a, GameVersion b) => (a.Valid && !b.Valid) ||
            a.Major > b.Major || a.Mid > b.Mid || a.Minor > b.Minor || a.Unstable > b.Unstable;
        public static bool operator <(GameVersion a, GameVersion b) => (!a.Valid && b.Valid) ||
            a.Major < b.Major || a.Mid < b.Mid || a.Minor < b.Minor || a.Unstable < b.Unstable;
        public static bool operator >=(GameVersion a, GameVersion b) => a.Valid || (!a.Valid && !b.Valid) ||
            a.Major >= b.Major || a.Mid >= b.Mid || a.Minor >= b.Minor || a.Unstable >= b.Unstable;
        public static bool operator <=(GameVersion a, GameVersion b) => b.Valid || (!a.Valid && !b.Valid) ||
            a.Major <= b.Major || a.Mid <= b.Mid || a.Minor <= b.Minor || a.Unstable <= b.Unstable;

        public override string ToString()
        {
            return Major + "." + Mid + "." + Minor + 
                (Unstable > 0 ? "." + Unstable : "");
        }
    }
    public class ModStatus
    {
        public readonly string name;
        public EModStatus status { get; internal set; }
        private string _exception = "NULL";
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

    public class ModStatusChecker : TinySettings
    {
#if !EDITOR
        private static readonly FieldInfo SessionData = typeof(ManMods).GetField("m_CurrentSession", BindingFlags.Instance | BindingFlags.NonPublic);
#endif
        private static readonly string DataDirectory = "ModKickStartHelper";

        private static Rect guiWindow = new Rect(20, 20, 800, 600);
        private static Vector2 guiSlider = Vector2.zero;
        private static ModStatusChecker inst;
        private static GUIInst instGUI;

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
        public static bool ShowWarnings => inst.ShowSetting <= 1;

        public string DirectoryInExtModSettings => DataDirectory;

        private static Dictionary<string, ModStatus> modStatuses = new Dictionary<string, ModStatus>();

        private const int ExtDebugID = 1002247;

        private static string[] options = new string[] { "Always", "Warnings", "Error", "Off" };
        public class GUIInst : MonoBehaviour
        {
            public void OnGUI()
            {
                try
                {
                    if (failed)
                        guiWindow = AltUI.Window(ExtDebugID, guiWindow, DoGUI, "Mod Startup Error", CloseGUI);
                    else
                        guiWindow = AltUI.Window(ExtDebugID, guiWindow, DoGUI, "Mod Startup", CloseGUI);
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
                        if (GUILayout.Button(item.Value.status.ToString(), AltUI.ButtonGrey))
                        {
                            selected = item.Value;
                            context = item.Value.exception;
                        }
                        break;
                    case EModStatus.MissingFromDisk:
                    case EModStatus.GameOutdated:
                    case EModStatus.TTExtUtilOutdated:
                    case EModStatus.Exception:
                        if (GUILayout.Button(item.Value.status.ToString(), AltUI.ButtonRed))
                        {
                            selected = item.Value;
                            context = item.Value.exception;
                        }
                        break;
                    case EModStatus.UpToDate:
                        if (GUILayout.Button(item.Value.status.ToString(), AltUI.ButtonGreenActive))
                        {
                            selected = item.Value;
                            context = item.Value.exception;
                        }
                        break;
                    case EModStatus.MightBeOutOfDate:
                        if (GUILayout.Button(item.Value.status.ToString(), AltUI.ButtonGreen))
                        {
                            selected = item.Value;
                            context = item.Value.exception;
                        }
                        break;
                    case EModStatus.NeedsToBeUpdated:
                    case EModStatus.ModUpdating:
                        if (GUILayout.Button(item.Value.status.ToString(), AltUI.ButtonBlue))
                        {
                            selected = item.Value;
                            context = item.Value.exception;
                        }
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
            UIHelpersExt.ClampMenuToScreen(ref guiWindow, false);
            GUI.DragWindow(guiWindow);
        }
        internal static void CloseGUI()
        {
            instGUI.gameObject.SetActive(false);
        }


        public static bool Is0MMAvail => LookForMod("NLogManager");//LookForMod("0ModManager");

        public static bool IsRandomAdditionsAvail => LookForMod("RandomAdditions");
        public static bool IsWaterModAvail => LookForMod("Water Mod") || LookForMod("Water Mod + Lava");


        private static bool failed = false;
        private static bool chainFailed = false;
        private static bool ETCUtilFailed = false;
        public static bool InitFailiure => failed;
        public static bool EpicFailiure => chainFailed;
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
        public static void EncapsulateSafeInit(string ModID, Action init, Action onFail = null, bool doChainFail = false)
        {
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
                var status = CheckStatusOfMod(ModID);
                if (!modStatuses.ContainsKey(ModID))
                    modStatuses.Add(ModID, status);
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
                                case EModStatus.TTExtUtilOutdated:
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
                            case EModStatus.Exception:
                                break;
                            case EModStatus.TTExtUtilOutdated:
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
                string failReport = "Initialization Failiure within " + ModID + ": " + e;
                Debug_TTExt.Log(failReport);
                if (onFail != null)
                    onFail.Invoke();
                if (IsRandomAdditionsAvail)
                {
                    // Delay the crash and deny loading of our other mods for now at least
                    errorReport = failReport;
                    InvokeHelper.InvokeSingleRepeat(ThrowDelayedErrorForRandAdd, 1);
                }
                modStatuses[ModID].status = EModStatus.Exception;
                modStatuses[ModID].exception = e.ToString();
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
        /// 
        /// </summary>
        /// <param name="name">has to be the EXACT name of the mod!</param>
        /// <returns></returns>
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
                                        return new ModStatus(name, EModStatus.TTExtUtilOutdated);
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
#endif
                return new ModStatus(name, EModStatus.FailedToFetch);
            }
            catch (Exception e)
            {
                Debug_TTExt.Log("CheckStatusOfMod_Internal(2): Could not fetch details of mod " + name +
                    " since an error occurred - " + e);
                return new ModStatus(name, EModStatus.Exception);
            }
            return new ModStatus(name, EModStatus.ModManagerMissing);
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
