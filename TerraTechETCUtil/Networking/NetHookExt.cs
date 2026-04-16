using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace TerraTechETCUtil
{
    /// <summary>
    /// External extensions for <see cref="NetworkHook"/>s
    /// </summary>
    public static class NetHookExt
    {
        /// <summary>
        /// See if the given tech is managed by the server
        /// </summary>
        /// <param name="tank"></param>
        /// <returns>True if the Tech is server managed</returns>
        public static bool IsServerManaged(this Tank tank)
        {
            return ManNetwork.IsNetworked && tank.netTech && tank.netTech.isServer;
        }
        /// <summary>
        /// See if the given tech is run on physics on our immedeate client
        /// </summary>
        /// <param name="tank"></param>
        /// <returns>True if it is run on our client</returns>
        public static bool CanRunPhysics(this Tank tank)
        {
            if (!ManNetwork.IsNetworked || tank.netTech == null)
                return true;
            return tank.netTech.hasAuthority;
        }

        /// <summary>
        /// Quick and dirty way to get the Tech's netId
        /// </summary>
        /// <param name="tank"></param>
        /// <returns>Tech NetID</returns>
        public static uint GetTechNetID(this Tank tank)
        {
            if (tank?.netTech)
                return tank.netTech.netId.Value;
            return 0;
        }

        /// <summary>
        /// Quick and dirty way to get the Tech's block index of a block mounted on itself
        /// </summary>
        /// <param name="block"></param>
        /// <returns>Block index</returns>
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


        /// <summary>
        /// Quick and dirty way to get the Tech's block index of a block mounted on itself
        /// as well as the tech's own <paramref name="netID"/>
        /// </summary>
        /// <param name="block"></param>
        /// <param name="netID">Tank netID</param>
        /// <returns>Block index</returns>
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


        /// <summary>
        /// Quick and dirty way to get a Tech's block module with given details on a tech
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="val"></param>
        /// <param name="localTechID"></param>
        /// <param name="blockIndex"></param>
        /// <param name="Module"></param>
        /// <returns>True if found</returns>
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
        /// <summary>
        /// Quick and dirty way to get a Tech's block with given details on a tech
        /// </summary>
        /// <param name="val"></param>
        /// <param name="localTechID"></param>
        /// <param name="blockIndex"></param>
        /// <param name="block"></param>
        /// <returns>True if found</returns>
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
        /// <summary>
        /// Quick and dirty way to get a Tech from given details
        /// </summary>
        /// <param name="val"></param>
        /// <param name="localTechID"></param>
        /// <param name="tech"></param>
        /// <returns>True if found</returns>
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
