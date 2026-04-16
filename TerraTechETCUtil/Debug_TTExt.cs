using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TerraTechETCUtil
{
    /// <summary>
    /// Debug logging for <see cref="TerraTechETCUtil"/>
    /// </summary>
    public static class Debug_TTExt
    {
        private const string modName = "TerraTechModExt";

        /// <summary> log EVERYTHING </summary>
        public static bool LogAll = false;
        /// <summary> log basic logging </summary>
        public static bool ShouldLog = true;
        /// <summary> log for <b>ManWorldGeneratorExt</b> </summary>
        public static bool ShouldLogBiomeGen = false;
        private static bool LogDev = false;

        internal static void Info(string message)
        {
            if (!ShouldLog || !LogAll)
                return;
            Debug.Log(message);
        }
        internal static void Log(string message)
        {
            if (!ShouldLog)
                return;
            Debug.Log(message);
        }
        internal static void Log(Exception e)
        {
            if (!ShouldLog)
                return;
            Debug.Log(e);
        }
        internal static void LogGen(string message)
        {
            if (!ShouldLog || !ShouldLogBiomeGen)
                return;
            UnityEngine.Debug.Log(message);
        }
        internal static void Assert(string message)
        {
            if (!ShouldLog)
                return;
            Debug.Log(message + "\n" + StackTraceUtility.ExtractStackTrace().ToString());
        }
        internal static void Assert(bool shouldAssert, string message)
        {
            if (!ShouldLog || !shouldAssert)
                return;
            Debug.Log(message + "\n" + StackTraceUtility.ExtractStackTrace().ToString());
        }
        internal static void LogError(string message)
        {
            if (!ShouldLog)
                return;
            Debug.Log(message + "\n" + StackTraceUtility.ExtractStackTrace().ToString());
        }
        internal static void LogDevOnly(string message)
        {
            if (!LogDev)
                return;
            Debug.Log(message + "\n" + StackTraceUtility.ExtractStackTrace().ToString());
        }
        internal static void FatalError(string e)
        {
            try
            {
#if EDITOR
                Debug.LogError(modName + ": ENCOUNTERED CRITICAL ERROR: " + e);
#else
                ManUI.inst.ShowErrorPopup(modName + ": ENCOUNTERED CRITICAL ERROR: " + e);
#endif
            }
            catch { }
            Debug.Log(modName + ": ENCOUNTERED CRITICAL ERROR");
            Debug.Log(modName + ": MAY NOT WORK PROPERLY AFTER THIS ERROR, PLEASE REPORT!");
        }
    }
}
