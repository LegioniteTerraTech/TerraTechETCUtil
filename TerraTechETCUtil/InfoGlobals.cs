using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerraTechETCUtil
{
    public class InfoGlobals
    {
        public static int LocalPlayerTeam => ManPlayer.inst.PlayerTeam;

        public const int MP_NeutralTeam = 1073741828;
    }
}
