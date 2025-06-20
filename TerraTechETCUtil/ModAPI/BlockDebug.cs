using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace TerraTechETCUtil
{
    public class BlockDebug
    {
        //Get the log errors and display them if needed
        public static bool DebugPopups = false;

        /// <summary>
        /// Assert!
        /// </summary>
        /// <param name="Text">What we assert.</param>
        public static void ThrowWarning(bool IsSeriousError, string Text)
        {
            Debug_TTExt.Log(Text);
            if (DebugPopups)
                ManModGUI.ShowErrorPopup(Text, IsSeriousError);
        }
    }
}
