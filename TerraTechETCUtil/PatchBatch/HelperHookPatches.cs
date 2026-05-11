using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;

namespace TerraTechETCUtil.PatchBatch
{
    internal static class HelperHookPatches
    {
        internal static class UILoadingScreenModProgressPatches
        {
            internal static Type target = typeof(UILoadingScreenModProgress);

            /// <summary>
            /// OverrideTheScreenToShowWeAreLoadingBiomesRN
            /// </summary>
            /// <param name="__instance"></param>
            /// <returns></returns>
            [HarmonyPriority(1000)]
            internal static bool Update_Prefix(UILoadingScreenModProgress __instance)
            {
                var loading = InvokeHelper.CurrentlyLoading;
                if (loading != null)
                {   // Show ManModBiomes status here 
                    __instance.loadingProgressText.text = loading.Subject +
                        ((int)(loading.EstPercentDone * 100f)).ToString() + "%\n" +
                        (loading.EstNumSteps == 0 ? string.Empty : 
                        (loading.EstNumStepsIterator + " of " + loading.EstNumSteps + "\n")) +
                        loading.InProgress;
                    __instance.loadingProgressImage.fillAmount = loading.EstPercentDone;
                    return false;
                }
                return true;
            }
        }

        internal class ManModsPatches
        {
            internal static Type target = typeof(ManMods);


            [HarmonyPriority(9001)]
            internal static bool UpdateModSession_Prefix()
            {
                return InvokeHelper.GetNextToLoadIfAny();
            }
            [HarmonyPriority(1000)]
            internal static void RequestReloadAllMods_Prefix()
            {
                InvokeHelper.ModsPreLoadEvent.Send();
            }
            [HarmonyPriority(1000)]
            internal static void RequestReparseAllJsons_Prefix()
            {
                InvokeHelper.ModsPreLoadEvent.Send();
            }
            [HarmonyPriority(1000)]
            internal static void UpdateModScripts_Postfix()
            {   //Debug_TTExt.Log("UpdateModScripts_Postfix");
                InvokeHelper.ModsUpdateEvent.Send();
            }
        }
    }
}
