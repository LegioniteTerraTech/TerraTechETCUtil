using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace TerraTechETCUtil
{
    public class BlockDebug : MonoBehaviour
    {
        //Get the log errors and display them if needed
        public static bool ShowErrors = false;

        private static BlockDebug inst;

        private static StringBuilder Warnings = new StringBuilder();
        private static bool WarningQueued = false;
        private static int WarningCount = 0;
        private const int WarningCountMax = 2;

        public static void InsureInit()
        {
            if (inst)
                return;
            var logMan = new GameObject("blockDebug");
            inst = logMan.AddComponent<BlockDebug>();
        }


        /// <summary>
        /// Assert!
        /// </summary>
        /// <param name="Text">What we assert.</param>
        public static void ThrowWarning(string Text)
        {
            Debug_TTExt.Log(Text);
            if (ShowErrors)
            {
                InsureInit();
                if (WarningCount < WarningCountMax)
                {
                    if (Warnings.Length > 0)
                    {
                        Warnings.Append("\n");
                        Warnings.Append("<b>--------------------</b>\n");
                    }
                    Warnings.Append(Text);
                    if (!WarningQueued && inst.IsNotNull())
                    {
                        inst.Invoke("ActuallyThrowWarning", 0);// next Update
                        WarningQueued = true;
                    }
                }
                // Else it's MAXED
                WarningCount++;
            }
        }
        public void ActuallyThrowWarning()
        {
            if (WarningCount > WarningCountMax)
            {
                Warnings.Append("\n");
                Warnings.Append("<b>--------------------</b>\n");
                Warnings.Append("Other Errors: " + (WarningCount - WarningCountMax));
            }
            Singleton.Manager<ManUI>.inst.ShowErrorPopup(Warnings.ToString());
            Warnings.Clear(); // RESET
            WarningCount = 0;
            WarningQueued = false;
        }


    }
}
