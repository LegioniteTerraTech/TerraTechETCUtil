
using System;
using System.Collections.Generic;
using System.IO;

#if !EDITOR
using HarmonyLib;
using Newtonsoft.Json;

#endif
using UnityEngine;
using UnityEngine.Networking;
using static TerraTechETCUtil.ManWorldTileExt;

namespace TerraTechETCUtil
{
    /*
     * Handles network hooks
     */
    public class ManModNetwork
    {
        public static NetworkInstanceId Host;
        public static bool HostExists = false;

        const int NetworkHooksStart = 6590;
        public static int NetworkHooks => NetworkHooksStart + hooks.Count;
        public static Dictionary<int, NetworkHook> hooks = new Dictionary<int, NetworkHook>();

        internal static int GetAssignVal(string ID)
        {
            int Value = ID.GetHashCode();
            if (Value < NetworkHooksStart)
            {
                Value = Mathf.Abs(Value) % (int.MaxValue - NetworkHooksStart);
            }
            return Value;
        }
        internal static void Enable(NetworkHook hook)
        {
            if (hooks.TryGetValue(hook.AssignedID, out NetworkHook hookGet))
            {
                if (hookGet != hook)
                {
                    throw new InvalidOperationException("Cannot register hook of ID \"" + hook.StringID + "\" as the ID hash [" +
                        hook.AssignedID + "] is already taken by  \"" + hookGet.StringID + "\" of ID  hash [" +
                        hookGet.AssignedID + "]");
                }
            }
            else
            {
                hooks.Add(hook.AssignedID, hook);
            }
        }
        internal static void Disable(NetworkHook hook)
        {
            //throw new InvalidOperationException("Cannot unregister hooks!");
            hooks.Remove(hook.AssignedID);
        }

        internal static bool SendToClient(int connectionID, NetworkHook hook, MessageBase message)
        {
            if (hooks.ContainsKey(hook.AssignedID))
            {
                try
                {
                    Singleton.Manager<ManNetwork>.inst.SendToClient(connectionID, (TTMsgType)hook.AssignedID, message);
                    Debug_TTExt.Log("TTExtUtil: SendToClient - Sent new network update for " + hook.NameFull + ", type " + hook.Type);
                    return true;
                }
                catch { Debug_TTExt.Log("TTExtUtil: SendToClient - Failed to send new network update for " + hook.NameFull + ", type " + hook.Type); }
                return false;
            }
            else
                throw new Exception("SendToClient - The given NetworkHook is not registered in ManModNetwork");
        }
        internal static bool SendToAllClients(NetworkHook hook, MessageBase message)
        {
            if (hooks.ContainsKey(hook.AssignedID))
            {
                try
                {
                    Singleton.Manager<ManNetwork>.inst.SendToAllClients((TTMsgType)hook.AssignedID, message, Host);
                    Debug_TTExt.Log("TTExtUtil: SendToAllClients - Sent new network update for " + hook.NameFull + ", type " + hook.Type);
                    return true;
                }
                catch { Debug_TTExt.Log("TTExtUtil: SendToAllClients - Failed to send new network update for " + hook.NameFull + ", type " + hook.Type); }
                return false;
            }
            else
                throw new Exception("SendToAllClients - The given NetworkHook is not registered in ManModNetwork");
        }
        internal static bool SendToServer(NetworkHook hook, MessageBase message)
        {
            if (hooks.ContainsKey(hook.AssignedID))
            {
                try
                {
                    Singleton.Manager<ManNetwork>.inst.SendToServer((TTMsgType)hook.AssignedID, message, Host);
                    Debug_TTExt.Log("TTExtUtil: SendToServer - Sent new network update for " + hook.NameFull + ", type " + hook.Type);
                    return true;
                }
                catch { Debug_TTExt.Log("TTExtUtil: SendToServer - Failed to send new network update for " + hook.NameFull + ", type " + hook.Type); }
                return false;
            }
            else
                throw new Exception("SendToServer - The given NetworkHook is not registered in ManModNetwork");
        }


        public class PlayerRequestServerCallbackBase : MessageBase
        {
            public PlayerRequestServerCallbackBase() { }
            public PlayerRequestServerCallbackBase(int senderID, int hookID)
            {
                this.senderID = senderID;
                this.hookID = hookID;
            }

            public int senderID;
            public int hookID;
        }
    }


    /// <summary>
    /// Use NetworkHook<T> instead!
    /// </summary>
    public abstract class NetworkHook
    {
        public readonly string StringID;
        public readonly int AssignedID;
        public readonly NetMessageType Type;

        public abstract string NameFull { get; }

        public NetworkHook(string ID, NetMessageType type) 
        {
            StringID = ID;
            AssignedID = ManModNetwork.GetAssignVal(StringID);
            Enable();
        }

        /// <summary>
        /// Must be called before game scene fully loads.  Do not unhook unless we are certain the hook is no longer needed!
        /// </summary>
        /// <returns>true if it worked, false if failed</returns>
        public void Enable()
        {
            ManModNetwork.Enable(this);
        }
        /// <summary>
        /// Must be called before game scene fully unloads.  Do not unhook unless we are certain the hook is no longer needed!
        /// </summary>
        /// <returns>true if it worked, false if failed</returns>
        public void Disable()
        {
            ManModNetwork.Disable(this);
        }

        public bool ClientSends()
        {
            return Type <= NetMessageType.RequestServerFromClient;
        }
        public bool ServerSends()
        {
            return Type >= NetMessageType.FromClientToServerThenClients;
        }
        public bool ClientRecieves()
        {
            switch (Type)
            {
                case NetMessageType.ToClientsOnly:
                case NetMessageType.FromClientToServerThenClients:
                case NetMessageType.RequestServerFromClient:
                    return true;
                case NetMessageType.ToServerOnly:
                default:
                    return false;
            }
        }
        public bool ServerRecieves()
        {
            switch (Type)
            {
                case NetMessageType.ToServerOnly:
                case NetMessageType.FromClientToServerThenClients:
                case NetMessageType.RequestServerFromClient:
                    return true;
                case NetMessageType.ToClientsOnly:
                default:
                    return false;
            }
        }



        public bool CanBroadcast()
        {
            return ManNetwork.IsNetworked && ManModNetwork.HostExists;
        }
        public bool CanBroadcastTech(Tank tank)
        {
            return ManNetwork.IsNetworked && ManModNetwork.HostExists && tank?.netTech;
        }
        public bool TryBroadcast(MessageBase message)
        {
            switch (Type)
            {
                case NetMessageType.ToClientsOnly:
                    return TryBroadcastToAllClients(message);
                case NetMessageType.ToServerOnly:
                    return TryBroadcastToServer(message);
                case NetMessageType.FromClientToServerThenClients:
                    return TryBroadcastToServer(message);
                case NetMessageType.RequestServerFromClient:
                    return TryBroadcastToServer(message);
                default:
                    throw new Exception("TryBroadcast - Invalid NetMessageType");
            }
        }
        public bool TryBroadcastTarget(MessageBase message, NetPlayer targetPlayer)
        {
            return TryBroadcastToClient(targetPlayer.connectionToClient.connectionId, message);
        }
        protected bool TryBroadcastToClient(int connectionID, MessageBase message)
        {
            return ManModNetwork.SendToClient(connectionID, this, message);
        }
        protected bool TryBroadcastToAllClients(MessageBase message)
        {
            return ManModNetwork.SendToAllClients(this, message);
        }
        protected bool TryBroadcastToServer(MessageBase message)
        {
            return ManModNetwork.SendToServer(this, message);
        }

        /// <summary>
        /// NetworkHook<T> is the correct hook format! DO NOT USE THIS ONE
        /// </summary>
        public virtual void OnToClientReceive_Internal(NetworkMessage netMsg)
        {
            throw new NotImplementedException("You used NetworkHook which is incorrect.  NetworkHook<T> is the correct hook format!");
        }
        /// <summary>
        /// NetworkHook<T> is the correct hook format! DO NOT USE THIS ONE
        /// </summary>
        public virtual void OnToServerReceive_Internal(NetworkMessage netMsg)
        {
            throw new NotImplementedException("You used NetworkHook which is incorrect.  NetworkHook<T> is the correct hook format!");
        }
    }
}
