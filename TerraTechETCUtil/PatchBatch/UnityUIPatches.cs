using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;

namespace TerraTechETCUtil.PatchBatch
{
    internal class UnityUIPatches
    {// --------------------------  UNITY  --------------------------

        internal static class UnitySelectablePatches
        {
            internal static Type target = typeof(UnityEngine.UI.Selectable);

            /// <summary>
            /// DontClickTheSelectableWhenIAmOverIMGUI
            /// </summary>
            [HarmonyPriority(-9001)]
            internal static bool OnPointerDown_Prefix()
            {
                return !ManModGUI.UIKickoffState;
            }
            /*
            /// <summary>
            /// DontBeInteractableWhenIAmOverIMGUI
            /// </summary>
            [HarmonyPriority(-9001)]
            internal static bool IsInteractable_Prefix(ref bool __result)
            {
                if (ManModGUI.UIKickoffState)
                {
                    __result = false;
                    return false;
                }
                return true;
            }//*/
            /// <summary>
            /// DontBeInteractableWhenIAmOverIMGUI2
            /// </summary>
            [HarmonyPriority(-9001)]
            internal static bool Select_Prefix()
            {
                return !ManModGUI.UIKickoffState;
            }
            /// <summary>
            /// DontBeInteractableWhenIAmOverIMGUI3
            /// </summary>
            [HarmonyPriority(-9001)]
            internal static bool IsHighlighted_Prefix(ref bool __result)
            {
                if (ManModGUI.UIKickoffState)
                {
                    __result = false;
                    return false;
                }
                return true;
            }
        }
        internal static class UnityButtonPatches
        {
            internal static Type target = typeof(UnityEngine.UI.Button);

            /// <summary>
            /// DontClickTheButtonWhenIAmOverIMGUI
            /// </summary>
            [HarmonyPriority(-9001)]
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
            [HarmonyPriority(-9001)]
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
            [HarmonyPriority(-9001)]
            internal static bool OnPointerClick_Prefix()
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
            [HarmonyPriority(-9001)]
            internal static bool OnPointerDown_Prefix()
            {
                return !ManModGUI.UIKickoffState;
            }
            /// <summary>
            /// DontMoveTheScrollbarWhenIAmOverIMGUI
            /// </summary>
            [HarmonyPriority(-9001)]
            internal static bool UpdateDrag_Prefix()
            {
                return !ManModGUI.UIKickoffState;
            }
            /// <summary>
            /// DontMoveTheScrollbarWhenIAmOverIMGUI2
            /// </summary>
            [HarmonyPriority(-9001)]
            internal static bool ClickRepeat_Prefix()
            {
                return !ManModGUI.UIKickoffState;
            }
        }
        internal static class UnityScrollRectPatches
        {
            internal static Type target = typeof(UnityEngine.UI.ScrollRect);

            /// <summary>
            /// DontClickTheScrollRectWhenIAmOverIMGUI
            /// </summary>
            [HarmonyPriority(-9001)]
            internal static bool OnScroll_Prefix()
            {
                return !ManModGUI.UIKickoffState;
            }
            /// <summary>
            /// DontMoveTheScrollRectWhenIAmOverIMGUI
            /// </summary>
            [HarmonyPriority(-9001)]
            internal static bool OnBeginDrag_Prefix()
            {
                return !ManModGUI.UIKickoffState;
            }
            /// <summary>
            /// DontMoveTheScrollRectWhenIAmOverIMGUI2
            /// </summary>
            [HarmonyPriority(-9001)]
            internal static bool OnDrag_Prefix()
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
            [HarmonyPriority(-9001)]
            internal static bool OnPointerDown_Prefix()
            {
                return !ManModGUI.UIKickoffState;
            }
            /// <summary>
            /// DontClickTheSliderWhenIAmOverIMGUI2
            /// </summary>
            [HarmonyPriority(-9001)]
            internal static bool UpdateDrag_Prefix()
            {
                return !ManModGUI.UIKickoffState;
            }
            /// <summary>
            /// DontClickTheSliderWhenIAmOverIMGUI3
            /// </summary>
            [HarmonyPriority(-9001)]
            internal static bool MayDrag_Prefix(ref bool __result)
            {
                if (ManModGUI.UIKickoffState)
                {
                    __result = false;
                    return false;
                }
                return true;
            }
        }
        internal static class UnityTogglePatches
        {
            internal static Type target = typeof(UnityEngine.UI.Toggle);

            /// <summary>
            /// DontClickTheToggleWhenIAmOverIMGUI
            /// </summary>
            [HarmonyPriority(-9001)]
            internal static bool OnPointerClick_Prefix()
            {
                return !ManModGUI.UIKickoffState;
            }
        }
    }
}
