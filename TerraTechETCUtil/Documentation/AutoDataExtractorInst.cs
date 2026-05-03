using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TerraTechETCUtil
{
    /// <summary>
    /// Extracts fine data from a target Component while tracking the active instance (but not the changes made to it after initial creation!)
    /// <para>Use <see cref="AutoDataExtractorInst"/> to keep track of inactive instances</para>
    /// </summary>
    public class AutoDataExtractorInst : AutoDataExtractor
    {
        /// <summary>
        /// Targeted Component instance
        /// </summary>
        public readonly Component inst;
        private AutoDocumentator docs;
        /// <summary>
        /// Creates a new AutoDataExtractorInst to extract and cache data about a target gameobject immedeately.
        /// <para>Also stores instance information for immedeate later access.  For targets that may be deleted, see <see cref="AutoDataExtractor"/></para>
        /// </summary>
        /// <param name="name">What to name the target in JSON</param>
        /// <param name="type">The type this is targeting</param>
        /// <param name="prefab">The actual prefab to gather context from</param>
        /// <param name="grabbedAlready">For recursive calls, what has already been extracted to prevent duplicates</param>
        public AutoDataExtractorInst(string name, Type type, Component prefab, HashSet<object> grabbedAlready) :
            base(name, type, prefab, grabbedAlready)
        {
            inst = prefab;
        }
        /// <summary>
        /// Does NOT return end comma with newline!
        /// </summary>
        internal void GetJsonFormatted(Component inst, StringBuilder SB, SlashState slash, int tabs = 0) =>
            docs.StringBuild(inst, inst.transform, SB, slash, tabs);

        /// <inheritdoc/>
        protected override void Explorer_InternalRecursePost(Type type, FieldInfo[] FIs)
        {
            docs = new AutoDocumentator(type, null, FIs);
        }

        /// <inheritdoc/>
        protected override void EndDetails()
        {
            if (inst == null)
                return;
            if (ManIngameWiki.ShowJSONExport)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Copy to system clipboard: ", AltUI.TextfieldBlue);
                if (AltUI.Button("Entire JSON", ManSFX.UISfxType.Craft))
                {
                    clipboard.Clear();
                    GetJsonFormatted(inst, clipboard, SlashState.None);
                    GUIUtility.systemCopyBuffer = clipboard.ToString();
                    clipboard.Clear();
                    ManSFX.inst.PlayUISFX(ManSFX.UISfxType.SaveGameOverwrite);
                }
                if (AltUI.Button("Reference", ManSFX.UISfxType.Craft))
                {
                    clipboard.Clear();
                    clipboard.Append("\"Reference|" + AutoDocUIElem.TryGetFoundationRefName(inst.transform) +
                        AutoDocUIElem.RecurseHierachy(inst.transform) + "." + inst.GetType().Name + "\" : ");
                    GUIUtility.systemCopyBuffer = clipboard.ToString();
                    clipboard.Clear();
                    ManSFX.inst.PlayUISFX(ManSFX.UISfxType.SaveGameOverwrite);
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
        }
    }
}
