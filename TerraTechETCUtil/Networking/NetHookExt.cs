using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace TerraTechETCUtil
{
    public static class NetHookExt
    {
        public static bool IsServerManaged(this Tank tank)
        {
            return ManNetwork.IsNetworked && tank.netTech && tank.netTech.isServer;
        }
        public static bool CanRunPhysics(this Tank tank)
        {
            if (!ManNetwork.IsNetworked || tank.netTech == null)
                return true;
            return tank.netTech.hasAuthority;
        }


        public static uint GetTechNetID(this Tank tank)
        {
            if (tank?.netTech)
                return tank.netTech.netId.Value;
            return 0;
        }

        public static int GetBlockIndexOnTank(this TankBlock block)
        {
            if (block == null || block.tank == null)
                return -1;
            int index = 0;
            foreach (var item in block.tank.blockman.IterateBlocks())
            {
                if (item == block)
                    return index;
                index++;
            }
            return -1;
        }

        public static int GetBlockIndexAndTechNetID(this TankBlock block, out uint netID)
        {
            netID = 0;
            if (block == null || block.tank == null)
                return -1;
            int index = 0;
            foreach (var item in block.tank.blockman.IterateBlocks())
            {
                if (item == block)
                {
                    netID = block.tank.netTech.netId.Value;
                    return index;
                }
                index++;
            }
            return -1;
        }


        public static bool GetBlockModuleOnTech<T>(this MessageBase val, uint localTechID, int blockIndex, out T Module) where T : ExtModule
        {
            NetTech target = ManNetTechs.inst.FindTech(localTechID);
            if (target?.tech)
            {
                var block = target.tech.blockman.GetBlockWithIndex(blockIndex);
                if (block)
                {
                    Module = block.GetComponent<T>();
                    if (Module)
                        return true;
                }
            }
            Module = null;
            return false;
        }
        public static bool GetBlockOnTech(this MessageBase val, uint localTechID, int blockIndex, out TankBlock block)
        {
            NetTech target = ManNetTechs.inst.FindTech(localTechID);
            if (target?.tech)
            {
                block = target.tech.blockman.GetBlockWithIndex(blockIndex);
                if (block)
                    return true;
            }
            block = null;
            return false;
        }
        public static bool GetTech(this MessageBase val, uint localTechID, out Tank tech)
        {
            NetTech target = ManNetTechs.inst.FindTech(localTechID);
            if (target?.tech)
            {
                tech = target.tech;
                return true;
            }
            tech = null;
            return false;
        }

    }
}
