using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevCommands;
using TerraTechETCUtil;

namespace TerraTechETCUtil
{
    internal class ETCCommands
    {
        [DevCommand(Name = "TTETCUtil.EnableAdvancedDebug", Access = Access.Public, Users = User.Host)]
        public static CommandReturn OpenDebugMenuAdvanced()
        {
            if (ManGameMode.inst.IsCurrentModeMultiplayer())
            {
                return new CommandReturn
                {
                    message = "Cannot use AdvancedDebug in Multiplayer!",
                    success = false,
                };
            }
            else
            {
                if (ManDLC.inst.HasAnyDLCOfType(ManDLC.DLCType.RandD))
                {
                    DebugExtUtilities.AllowEnableDebugGUIMenu_KeypadEnter = true;
                    DebugExtUtilities.Open();
                    return new CommandReturn
                    {
                        message = "Enabled!",
                        success = true,
                    };
                }
                else
                {
                    return new CommandReturn
                    {
                        message = "Cannot open AdvancedDebug as this will cause a crash without the R&D DLC!",
                        success = true,
                    };
                }
            }
        }
    }
}
