
using System;
using System.Collections.Generic;
using System.IO;

#if !EDITOR
using HarmonyLib;
using Newtonsoft.Json;

#endif
using UnityEngine;
using UnityEngine.Networking;

namespace TerraTechETCUtil
{
    /// <summary>
    /// Handles mod network hooks
    /// </summary>
    public class ManModNetwork
    {
        /// <summary>
        /// The current host
        /// </summary>
        public static NetworkInstanceId Host;
        /// <summary>
        /// Our host exists
        /// </summary>
        public static bool HostExists = false;

        const int NetworkHooksStart = 6590;
        /// <summary>
        /// The next network hook id
        /// </summary>
        public static int NetworkHooks => NetworkHooksStart + hooks.Count;
        /// <summary>
        /// All assigned <see cref="NetworkHook"/>s
        /// </summary>
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

        /// <summary>
        /// Sanity check for servers to see if their clients' hooks match
        /// </summary>
        public class PlayerRequestServerCallbackBase : MessageBase
        {
            /// <summary> </summary>
            public PlayerRequestServerCallbackBase() { }
            /// <summary> </summary>
            public PlayerRequestServerCallbackBase(int senderID, int hookID)
            {
                this.senderID = senderID;
                this.hookID = hookID;
            }

            /// <summary> </summary>
            public int senderID;
            /// <summary> </summary>
            public int hookID;
        }
    }


    /// <summary>
    /// Use <see cref="NetworkHook{T}"/> instead!
    /// </summary>
    public abstract class NetworkHook
    {
        /// <summary>
        /// The string id of the hool
        /// </summary>
        public readonly string StringID;
        /// <summary>
        /// The assigned ID that <see cref="ManModNetwork"/> gives when the <see cref="NetworkHook"/> is created
        /// </summary>
        public readonly int AssignedID;
        /// <summary>
        /// The way this is handled when <see cref="TryBroadcast(MessageBase)"/> or any of it's 
        /// like-named counterparts is called
        /// </summary>
        public readonly NetMessageType Type;

        /// <summary>
        /// The full name of the <see cref="NetworkHook"/> to display when logging
        /// </summary>
        public abstract string NameFull { get; }

        /// <summary>
        /// Create a new <see cref="NetworkHook"/>
        /// </summary>
        /// <param name="ID">The string ID to reference in your mod when calling this.</param>
        /// <param name="type">The way this is handled when <see cref="TryBroadcast(MessageBase)"/> or any of it's 
        /// like-named counterparts is called</param>
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

        /// <summary>
        /// True if the client can send this
        /// </summary>
        public bool ClientSends()
        {
            return Type <= NetMessageType.RequestServerFromClient;
        }
        /// <summary>
        /// True if the server can send this
        /// </summary>
        public bool ServerSends()
        {
            return Type >= NetMessageType.FromClientToServerThenClients;
        }
        /// <summary>
        /// True if the client can receive this
        /// </summary>
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
        /// <summary>
        /// True if the server can receive this
        /// </summary>
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


        /// <summary>
        /// True if this can be broadcast across the server
        /// </summary>
        public bool CanBroadcast()
        {
            return ManNetwork.IsNetworked && ManModNetwork.HostExists;
        }
        /// <summary>
        /// True if this can be broadcast across the server
        /// </summary>
        public bool CanBroadcastTech(Tank tank)
        {
            return ManNetwork.IsNetworked && ManModNetwork.HostExists && tank?.netTech;
        }
        /// <summary>
        /// Broadcast this now
        /// </summary>
        /// <param name="message">To send</param>
        /// <returns>True if it sent correctly</returns>
        /// <exception cref="Exception"></exception>
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
        /// <summary>
        /// Broadcast this now
        /// </summary>
        /// <param name="message">To send</param>
        /// <param name="targetPlayer">Specific player to target</param>
        /// <returns>True if it sent correctly</returns>
        /// <exception cref="Exception"></exception>
        public bool TryBroadcastTarget(MessageBase message, NetPlayer targetPlayer)
        {
            return TryBroadcastToClient(targetPlayer.connectionToClient.connectionId, message);
        }
        /// <summary>
        /// Broadcast this now
        /// </summary>
        /// <param name="message">To send</param>
        /// <param name="connectionID">Specific connection to target</param>
        /// <returns>True if it sent correctly</returns>
        /// <exception cref="Exception"></exception>
        protected bool TryBroadcastToClient(int connectionID, MessageBase message)
        {
            return ManModNetwork.SendToClient(connectionID, this, message);
        }
        /// <summary>
        /// Broadcast this now to all clients
        /// </summary>
        /// <param name="message">To send</param>
        /// <returns>True if it sent correctly</returns>
        /// <exception cref="Exception"></exception>
        protected bool TryBroadcastToAllClients(MessageBase message)
        {
            return ManModNetwork.SendToAllClients(this, message);
        }
        /// <summary>
        /// Broadcast this now to the server only
        /// </summary>
        /// <param name="message">To send</param>
        /// <returns>True if it sent correctly</returns>
        /// <exception cref="Exception"></exception>
        protected bool TryBroadcastToServer(MessageBase message)
        {
            return ManModNetwork.SendToServer(this, message);
        }

        /// <summary>
        /// <see cref="NetworkHook{T}"/> is the correct hook format! DO NOT USE THIS ONE
        /// </summary>
        public virtual void OnToClientReceive_Internal(NetworkMessage netMsg)
        {
            throw new NotImplementedException("You used NetworkHook which is incorrect.  NetworkHook<T> is the correct hook format!");
        }
        /// <summary>
        /// <see cref="NetworkHook{T}"/> is the correct hook format! DO NOT USE THIS ONE
        /// </summary>
        public virtual void OnToServerReceive_Internal(NetworkMessage netMsg)
        {
            throw new NotImplementedException("You used NetworkHook which is incorrect.  NetworkHook<T> is the correct hook format!");
        }
    }
}
