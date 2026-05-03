using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerraTechETCUtil
{
    internal class AllNetworkPatches
    {
        internal static class NetPlayerPatches
        {
            internal static Type target = typeof(NetPlayer);
            internal static void OnStartClient_Postfix(NetPlayer __instance)
            {
                ManModNetwork.OnStartClient(__instance);
            }
            internal static void OnStartServer_Postfix(NetPlayer __instance)
            {
                ManModNetwork.OnStartServer(__instance);
            }
        }

    }
}
