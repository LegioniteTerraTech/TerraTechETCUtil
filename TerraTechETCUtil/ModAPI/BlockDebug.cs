using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace TerraTechETCUtil
{
    /// <summary>
    /// A small class for debugging blocks
    /// </summary>
    public class BlockDebug
    {
        /// <summary>
        /// Get the log errors and display them if needed
        /// </summary>
        public static bool DebugPopups = false;

        /// <summary>
        /// Assert!
        /// </summary>
        /// <param name="IsSeriousError">If the error can really screw the game up</param>
        /// <param name="Text">What we assert.</param>
        public static void ThrowWarning(bool IsSeriousError, string Text)
        {
            Debug_TTExt.Log(Text);
            if (DebugPopups)
                ManModGUI.ShowErrorPopup(Text, IsSeriousError);
        }
    }
}
