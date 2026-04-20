using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TerraTechETCUtil
{
    /// <summary>
    /// Documents <see cref="GameObject"/>s.
    /// <para>Also see <seealso cref="AutoDocumentator"/> and <seealso cref="Utilities"/></para>
    /// </summary>
    public static class GameObjectDocumentator
    {
        /// <summary>
        /// Logs the target <see cref="GameObject"/>'s hierarchy to <b>output_log.txt</b> at <see cref="Application.consoleLogPath"/>
        /// <para>For a the <see cref="Component"/> version, see <seealso cref="Utilities.PrintAllComponentsGameObjectDepth{T}(GameObject)"/></para>
        /// <para>For a more <b>precise</b> version, see <seealso cref="AutoDocumentator"/></para>
        /// </summary>
        /// <param name="targetBaseGameObject">The <see cref="GameObject"/> to get from</param>
        /// <param name="tabs">Offset number of tabs to apply to the extracted data</param>
        /// <param name="slash">set to <b><see cref="SlashState.Slash"/></b> if we should add slashes to the ends</param>
        /// <param name="GetComponentsToo">Set this to true to also get the attached <see cref="Component"/>s</param>
        public static void GetHierachyAndPrintToLog(GameObject targetBaseGameObject, int tabs, SlashState slash = SlashState.None, bool GetComponentsToo = true)
        {
            StringBuilder sb = new StringBuilder();
            GetStrings(targetBaseGameObject, sb, tabs, slash, GetComponentsToo);
            Debug_TTExt.Log(sb.ToString());
        }

        /// <summary>
        /// Logs the target <see cref="GameObject"/>'s hierarchy into the given <see cref="StringBuilder"/>
        /// <para>For the <see cref="Component"/> version, see <seealso cref="Utilities.PrintAllComponentsGameObjectDepth{T}(GameObject)"/></para>
        /// <para>For a more <b>precise</b> version, see <seealso cref="AutoDocumentator"/></para>
        /// </summary>
        /// <param name="targetBaseGameObject">The <see cref="GameObject"/> to get from</param>
        /// <param name="SB">Inserts the data into this</param>
        /// <param name="tabs">Offset number of tabs to apply to the extracted data</param>
        /// <param name="slash">set to <b><see cref="SlashState.Slash"/></b> if we should add slashes to the ends</param>
        /// <param name="GetComponentsToo">Set this to true to also get the attached <see cref="Component"/>s</param>
        public static void GetStrings(GameObject targetBaseGameObject, StringBuilder SB,
            int tabs, SlashState slash, bool GetComponentsToo = true)
        {
            IterateRecursively(targetBaseGameObject, SB, tabs, slash, GetComponentsToo);
        }
        private static void InsertWithTabNoNewline(int tabs, string desc, StringBuilder SB)
        {
            for (int i = 0; i < tabs; i++)
                SB.Append('\t');
            SB.Append(desc);
        }
        private static void InsertVec3Fast(int tabs, string name, Vector3 vec, StringBuilder SB)
        {
            InsertWithTabNoNewline(tabs, "\"" + name + "\" : { \"x\": " + vec.x +
                ", \"y\": " + vec.y + ", \"z\": " + vec.z + " },\n", SB);
        }
        private static void IterateRecursively(GameObject gameOb, StringBuilder SB,
            int tabs, SlashState slash, bool GetComponentsToo)
        {
            for (int i = 0; i < tabs; i++)
                SB.Append('\t');
            if (tabs > 32)
            {
                SB.Append("// Depth exceeded 32!");
                return; // TOO DEEP 
            }
            if (gameOb.name.NullOrEmpty())
                SB.Append("\"GameObject|null\" : {\n");
            else if (gameOb.name.EndsWith(")"))
            {
                SB.Append("\"GameObject|");
                SB.Append(gameOb.name);
                SB.Append("\" : {// - Likely cannot be referenced due to (#)\n");
            }
            else
            {
                SB.Append("\"GameObject|");
                SB.Append(gameOb.name);
                SB.Append("\" : {\n");
            }
            Transform trans = gameOb.transform;
            InsertWithTabNoNewline(tabs + 1, "\"UnityEngine.Transform\":{\n", SB);
            InsertVec3Fast(tabs + 2, "localPosition", trans.localPosition, SB);
            InsertVec3Fast(tabs + 2, "localEulerAngles", trans.localEulerAngles, SB);
            InsertVec3Fast(tabs + 2, "localScale", trans.localScale, SB);
            InsertWithTabNoNewline(tabs + 1, "},\n", SB);
            for (int i = 0; i < trans.childCount; i++)
            {
                Transform transC = trans.GetChild(i);
                if (transC != null)
                    IterateRecursively(transC.gameObject, SB, tabs + 1, slash, GetComponentsToo);
            }
            if (GetComponentsToo)
            {
                foreach (var item in ModuleInfo.TryGetModulesBlacklisted(gameOb, ModuleInfo.BlockedTypesExporter))
                {
                    item.GetJsonFormatted(item.inst, SB, slash, tabs + 1);
                    SB.Append(",\n");
                    /*
                    AutoDocumentator AD2 = new AutoDocumentator(, item.inst, );
                    InsertWithTabNoNewline(tabs + 1, "// Module Ref Path: \"" + AutoDocUIElem.TryGetFoundationRefName(trans) +
                        AutoDocUIElem.RecurseHierachy(trans) + "/" + item.GetType().Name + ".\"\n", SB);
                    InsertWithTabNoNewline(tabs + 1, "\"" + item.GetType().Name + "\": ", SB);
                    AD2.StringBuild(item, trans, SB, slash, tabs + 1, false);
                    SB.Append(",\n");
                    */
                }
            }
            for (int i = 0; i < tabs; i++)
                SB.Append('\t');
            SB.Append("},\n");
        }
    }
}
