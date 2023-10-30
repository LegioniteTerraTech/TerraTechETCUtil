using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if !STEAM
using ModHelper.Config;
#else
using ModHelper;
#endif
//using Nuterra.NativeOptions;

namespace TerraTechETCUtil
{
    internal class ConfigConnect
    {
        internal static ModConfig config;

        private static bool launched = false;

        public static void TryInitOptionAndConfig()
        {
            if (launched)
                return;
            launched = true;
            //Initiate the madness
            try
            {
            }
            catch (Exception e)
            {
                Debug_TTExt.Log("TerraTechModExt: Error on Option & Config setup");
                Debug_TTExt.Log(e);
            }

        }

        public static void TrySaveConfigData()
        {
            try
            {
                config.WriteConfigJsonFile();
            }
            catch (Exception e)
            {
                Debug_TTExt.Log("TerraTechModExt: Error on Config Saving");
                Debug_TTExt.Log(e);
            }

        }
    }
}
