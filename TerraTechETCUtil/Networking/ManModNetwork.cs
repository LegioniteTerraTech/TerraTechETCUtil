
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;



#if !EDITOR
using HarmonyLib;
using Newtonsoft.Json;

#endif
using UnityEngine;
using UnityEngine.Networking;
using static CompoundExpression;

namespace TerraTechETCUtil
{
    /// <summary>
    /// Handles mod network hooks
    /// <para><b>INSURE YOU CALL <see cref="LegModExt.InsurePatches()"/> BEFORE USING</b></para>
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
        public static bool subbed = false;

        const int NetworkHooksMaster = 6590;
        const int NetworkHooksStart = 6591;
        /// <summary>
        /// The next network hook id
        /// </summary>
        public static int NetworkHooks => NetworkHooksStart + hooks.Count;
        /// <summary>
        /// All assigned <see cref="NetworkHook"/>s
        /// </summary>
        public static Dictionary<int, NetworkHook> hooks = new Dictionary<int, NetworkHook>();

        internal static void OnStartClient(NetPlayer __instance)
        {
            ManNetwork.inst.SubscribeToClientMessage(__instance.netId, (TTMsgType)NetworkHooksMaster, 
                new ManNetwork.MessageHandler(OnClientRecHookSetter));
            Debug_TTExt.Log("Client Subscribed main to network under ID " + NetworkHooksMaster);
            int counter = 0;
            foreach (var item in hooks)
            {
                if (item.Value.ClientRecieves())
                {
                    ManNetwork.inst.SubscribeToClientMessage(__instance.netId, (TTMsgType)item.Key,
                        item.Value.OnToClientReceive_Internal);
                    Debug_TTExt.Log("Client Subscribed " + item.Value.ToString() + " to network under ID " + item.Key);
                    counter++;
                }
            }
            Debug_TTExt.Log("Client subscribed " + counter + " hooks.");
        }
        private static void OnClientRecHookSetter(NetworkMessage netM)
        {
            try
            {
                NetSyncMessage NSM = new NetSyncMessage();
                NSM.Deserialize(netM.reader);
                var hook = hooks.FirstOrDefault(x => x.Value.StringID == NSM.stringID);
                if (hook.Value != null)
                {
                    if (hook.Key != NSM.hookID)
                    {
                        Debug_TTExt.Log("Client changed net hook \"" + NSM.stringID + "\" to match server [" +
                            hook.Key + " => " + NSM.hookID + "].");
                        hooks.Remove(hook.Key);
                        for (int i = 0; i < ManNetwork.inst.GetNumPlayers(); i++)
                        {
                            var play = ManNetwork.inst.GetPlayer(i);
                            if (play != null)
                            {
                                ManNetwork.inst.UnsubscribeFromClientMessage(play.netId, (TTMsgType)hook.Key,
                                    hook.Value.OnToClientReceive_Internal);
                                ManNetwork.inst.SubscribeToClientMessage(play.netId, (TTMsgType)NSM.hookID,
                                    hook.Value.OnToClientReceive_Internal);
                            }
                        }
                        hooks.Add(NSM.hookID, hook.Value);
                        hook.Value.AssignedID = NSM.hookID;
                    }
                    else
                        Debug_TTExt.Info("Client net hook \"" + NSM.stringID + "\" is synced.");
                }
                else
                    Debug_TTExt.FatalError("Client failed to find matching network hook of name \"" + NSM.stringID + "\"!!!");
            }
            catch (Exception e)
            {
                Debug_TTExt.FatalError("Client failed to recieve nethook update!!! - " + e);
            }
        }
        private static void OnPlayerAddedSentHooksSanityCheck(NetPlayer __instance)
        {
            if (ManNetwork.IsHostOrWillBe)
            {
                foreach (var item in hooks)
                    ManNetwork.inst.SendToAllExceptHost((TTMsgType)NetworkHooksMaster,
                        new NetSyncMessage(item.Value.StringID, item.Key));
            }
        }
        internal static void OnStartServer(NetPlayer __instance)
        {
            if (!HostExists)
            {
                Debug_TTExt.Log("Host started, hooked ManModNetwork update broadcasting to " + __instance.netId.ToString());
                Host = __instance.netId;
                HostExists = true;
                if (!subbed)
                {
                    subbed = true;
                    ManNetwork.inst.OnPlayerAdded.Subscribe(OnPlayerAddedSentHooksSanityCheck);
                }

                int counter = 0;
                foreach (var item in hooks)
                {
                    if (item.Value.ServerRecieves())
                    {
                        ManNetwork.inst.SubscribeToServerMessage(__instance.netId, (TTMsgType)item.Key, 
                            new ManNetwork.MessageHandler(item.Value.OnToServerReceive_Internal));
                        Debug_TTExt.Log("Server Subscribed " + item.Value.ToString() + " to network under ID " + item.Key);
                        counter++;
                    }
                }
                Debug_TTExt.Log("Server subscribed " + counter + " hooks.");
            }
        }

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
        public class NetSyncMessage : MessageBase
        {
            /// <summary> </summary>
            public NetSyncMessage() { }
            /// <summary> </summary>
            public NetSyncMessage(string stringID, int hookID)
            {
                this.stringID = stringID;
                this.hookID = hookID;
            }

            /// <summary> </summary>
            public string stringID;
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
        public int AssignedID { get; internal set; }
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
