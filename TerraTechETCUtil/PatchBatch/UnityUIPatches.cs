using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerraTechETCUtil.PatchBatch
{
    internal class UnityUIPatches
    {// --------------------------  UNITY  --------------------------

        internal static class UnityButtonPatches
        {
            internal static Type target = typeof(UnityEngine.UI.Button);

            /// <summary>
            /// DontClickTheButtonWhenIAmOverIMGUI
            /// </summary>
            internal static bool OnPointerClick_Prefix()
            {
                return !ManModGUI.UIKickoffState;
            }
        }
        internal static class UnityDropdownPatches
        {
            internal static Type target = typeof(UnityEngine.UI.Dropdown);

            /// <summary>
            /// DontClickTheDropdownWhenIAmOverIMGUI
            /// </summary>
            internal static bool OnPointerClick_Prefix()
            {
                return !ManModGUI.UIKickoffState;
            }
        }
        internal static class UnityInputFieldPatches
        {
            internal static Type target = typeof(UnityEngine.UI.InputField);

            /// <summary>
            /// DontClickTheInputFieldWhenIAmOverIMGUI
            /// </summary>
            internal static bool OnPointerClick_Prefix()
            {
                return !ManModGUI.UIKickoffState;
            }
        }

        internal static class UnitySelectablePatches
        {
            internal static Type target = typeof(UnityEngine.UI.Selectable);

            /// <summary>
            /// DontClickTheSelectableWhenIAmOverIMGUI
            /// </summary>
            internal static bool OnPointerDown_Prefix()
            {
                return !ManModGUI.UIKickoffState;
            }
        }
        internal static class UnityScrollbarPatches
        {
            internal static Type target = typeof(UnityEngine.UI.Scrollbar);

            /// <summary>
            /// DontClickTheScrollbarWhenIAmOverIMGUI
            /// </summary>
            internal static bool OnPointerDown_Prefix()
            {
                return !ManModGUI.UIKickoffState;
            }
        }
        internal static class UnitySliderPatches
        {
            internal static Type target = typeof(UnityEngine.UI.Slider);

            /// <summary>
            /// DontClickTheSliderWhenIAmOverIMGUI
            /// </summary>
            internal static bool OnPointerDown_Prefix()
            {
                return !ManModGUI.UIKickoffState;
            }
        }
        internal static class UnityTogglePatches
        {
            internal static Type target = typeof(UnityEngine.UI.Toggle);

            /// <summary>
            /// DontClickTheToggleWhenIAmOverIMGUI
            /// </summary>
            internal static bool OnPointerClick_Prefix()
            {
                return !ManModGUI.UIKickoffState;
            }
        }
    }
}
