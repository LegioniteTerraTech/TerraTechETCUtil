using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;

namespace TerraTechETCUtil
{
    internal class TerraTechUIPatches
    {
        internal static class ManPointerPatches
        {
            internal static Type target = typeof(ManPointer);

            /// <summary>
            /// LockMouseWhenOverSubMenu
            /// </summary>
            [HarmonyPriority(-9001)]
            internal static bool UpdateMouseEvents_Prefix(ref ManPointer __instance)
            {
                return __instance.DraggingItem != null || !ManModGUI.IsMouseOverModGUI;
            }
            /// <summary>
            /// StopBeingDumbRadial
            /// </summary>
            [HarmonyPriority(-9001)]
            internal static bool OpenMenuForTarget_Prefix(ref ManPointer __instance)
            {
                return __instance.DraggingItem != null || !ManModGUI.IsMouseOverModGUI;
            }
        }
        internal static class GameCursorPatches
        {
            internal static Type target = typeof(GameCursor);
            
            /// <summary>
            /// Make cursor not look interactable when over menu
            /// </summary>
            [HarmonyPriority(900001)] // FIRSTSSSS
            internal static void GetCursorState_Postfix(ref GameCursor.CursorState __result)
            {
                if (ManModGUI.IsMouseOverModGUI)
                    __result = GameCursor.CursorState.Default;
            }
        }

        internal static class TankControlPatches
        {
            internal static Type target = typeof(TankControl);

            /// <summary>
            /// LockMouseWhenOverSubMenu2
            /// </summary>
            [HarmonyPriority(-9001)]
            internal static bool OnManualTargetingEvent_Prefix()
            {
                return !ManModGUI.UIKickoffState;
            }
            //OnMouseZoomEvent_Prefix()
        }
        internal static class TankCameraPatches
        {
            internal static Type target = typeof(TankCamera);

            /// <summary>
            /// DontOpenModalsWhenIAmOverIMGUI
            /// </summary>
            [HarmonyPriority(-9001)]
            internal static bool ManualZoom_Prefix()
            {
                return !ManModGUI.UIKickoffState;
            }
        }
        internal static class AdvertisingPanelPatches
        {
            internal static Type target = typeof(AdvertisingPanel);

            /// <summary>
            /// DontClickThePanelWhenIAmOverIMGUI
            /// </summary>
            [HarmonyPriority(-9001)]
            internal static bool UI_OnBannerClicked_Prefix()
            {
                return !ManModGUI.UIKickoffState;
            }
        }
        internal static class ManHUDPatches
        {
            internal static Type target = typeof(ManHUD);

            /// <summary>
            /// EatEscapeKeypress
            /// </summary>
            [HarmonyPriority(-9001)]
            internal static bool HandleEscapeKey_Prefix(ref bool __result)
            {
                if (ManModGUI.CallEscapeCallbackPre())
                {
                    __result = true;
                    return false;
                }
                return true;
            }
            /// <summary>
            /// EatEscapeKeypress(2)
            /// </summary>
            [HarmonyPriority(-9001)]
            internal static void HandleEscapeKey_Postfix(ref bool __result)
            {
                if (!__result && ManModGUI.CallEscapeCallbackPost())
                    __result = true;
            }
        }
        internal static class UIMiniMapDisplayPatches
        {
            internal static Type target = typeof(UIMiniMapDisplay);
            // Allow custom UI
            [HarmonyPriority(-9001)]
            internal static void Show_Postfix(UIMiniMapDisplay __instance)
            {
                if ((bool)__instance && ManMinimapExt.Enabled)
                {
                    if (__instance.GetComponent<ManMinimapExt.MinimapExt>())
                        return;
                    var instWorld = __instance.gameObject.AddComponent<ManMinimapExt.MinimapExt>();
                    instWorld.InitInst(__instance);
                }
            }
        }
    }
}
