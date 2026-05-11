using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;

namespace TerraTechETCUtil
{
    internal class AllNetworkPatches
    {
        internal static class NetPlayerPatches
        {
            internal static Type target = typeof(NetPlayer);
            [HarmonyPriority(-9001)]
            internal static void OnStartClient_Postfix(NetPlayer __instance)
            {
                ManModNetwork.OnStartClient(__instance);
            }
            [HarmonyPriority(-9001)]
            internal static void OnStartServer_Postfix(NetPlayer __instance)
            {
                ManModNetwork.OnStartServer(__instance);
            }
        }

    }
}
