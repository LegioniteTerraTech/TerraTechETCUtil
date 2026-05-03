using System;
using UnityEngine;
using UnityEngine.Networking;

namespace TerraTechETCUtil
{
    /// <summary>
    /// The networking path
    /// </summary>
    public enum NetMessageType
    {
        /// <summary> self-explanitory </summary>
        ToClientsOnly,
        /// <summary> self-explanitory </summary>
        FromClientToServerThenClients,
        /// <summary> self-explanitory </summary>
        RequestServerFromClient,
        /// <summary> self-explanitory </summary>
        ToServerOnly,
    }

    /// <summary>
    /// A simple network hook to send mod networking information
    /// <para><b>INSURE YOU CALL <see cref="LegModExt.InsurePatches()"/> BEFORE USING</b></para>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class NetworkHook<T> : NetworkHook where T : MessageBase
    {
        /// <summary>
        /// MessageBase, IsServer
        /// </summary>
        protected Func<T, bool, bool> receiveAction;
        /// <summary>
        /// The logging name of this
        /// </summary>
        public override string NameFull => typeof(T).ToString() +" [" + StringID + "]";

        /// <summary>
        /// Create a new <see cref="NetworkHook"/>
        /// <para><b>INSURE YOU CALL <see cref="LegModExt.InsurePatches()"/> BEFORE USING</b></para>
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="onReceive"></param>
        /// <param name="type"></param>
        public NetworkHook(string ID, Func<T, bool, bool> onReceive, NetMessageType type)
            : base(ID, type)
        {
            receiveAction = onReceive;
        }
        /// <summary>
        /// Sent from someone on our server to our client
        /// </summary>
        /// <param name="netMsg"></param>
        /// <exception cref="Exception"></exception>
        public override void OnToClientReceive_Internal(NetworkMessage netMsg)
        {
            T decoded;
            switch (Type)
            {
                case NetMessageType.ToClientsOnly:
                    decoded = (T)Activator.CreateInstance(typeof(T));
                    decoded.Deserialize(netMsg.reader);
                    Debug_TTExt.Info("NetworkHook.OnClientReceive_Internal(ToClientsOnly) - Client-side trigger for " + StringID + ", type " + Type);
                    receiveAction.Invoke(decoded, false);
                    break;
                case NetMessageType.ToServerOnly:
                    throw new Exception("NetworkHook.OnClientReceive_Internal(ToServerOnly) - ServerOnly sent to client for " + StringID + ", type " + Type);
                case NetMessageType.FromClientToServerThenClients:
                    try
                    {
                        decoded = (T)Activator.CreateInstance(typeof(T));
                        decoded.Deserialize(netMsg.reader);
                        Debug_TTExt.Info("NetworkHook.OnClientReceive_Internal(FromClientToServerThenClients) - Client-side trigger for " + StringID + ", type " + Type);
                        receiveAction.Invoke(decoded, false);
                    }
                    catch (Exception e)
                    {
                        Debug_TTExt.Log("NetworkHook.OnClientReceive_Internal(FromClientToServerThenClients) - ERROR: isServer " + ManNetwork.inst.IsServer + " | " + e);
                    }
                    break;
                case NetMessageType.RequestServerFromClient:
                    try
                    {
                        decoded = (T)Activator.CreateInstance(typeof(T));
                        decoded.Deserialize(netMsg.reader);
                        Debug_TTExt.Log("NetworkHook.OnClientReceive_Internal(RequestServerFromClient) - Client-side trigger for " + StringID + ", type " + Type);
                        receiveAction.Invoke(decoded, false);
                    }
                    catch (Exception e)
                    {
                        Debug_TTExt.Log("NetworkHook.OnClientReceive_Internal(RequestServerFromClient) - ERROR: isServer " + ManNetwork.inst.IsServer + " | " + e);
                    }
                    break;
                default:
                    break;
            }
        }
        /// <summary>
        /// Sent from someone on our server to our server
        /// </summary>
        /// <param name="netMsg"></param>
        /// <exception cref="Exception"></exception>
        public override void OnToServerReceive_Internal(NetworkMessage netMsg)
        {
            T decoded;
            switch (Type)
            {
                case NetMessageType.ToClientsOnly:
                    throw new Exception("NetworkHook.OnClientReceive_Internal(ToClientsOnly) - ClientsOnly sent to server for " + StringID + ", type " + Type);
                case NetMessageType.ToServerOnly:
                    decoded = (T)Activator.CreateInstance(typeof(T));
                    decoded.Deserialize(netMsg.reader);
                    Debug_TTExt.Log("NetworkHook.OnClientReceive_Internal(ToServerOnly) - Server-side trigger for " + StringID + ", type " + Type);
                    receiveAction.Invoke(decoded, true);
                    break;
                case NetMessageType.FromClientToServerThenClients:
                    try
                    {
                        decoded = (T)Activator.CreateInstance(typeof(T));
                        decoded.Deserialize(netMsg.reader);
                        Debug_TTExt.Info("NetworkHook.OnClientReceive_Internal(FromClientToServerThenClients) - Server-side trigger for " + StringID + ", type " + Type);
                        if (receiveAction.Invoke(decoded, true))
                        {
                            try
                            {
                                TryBroadcastToAllClients(decoded);
                            }
                            catch (Exception e)
                            {
                                throw new Exception("NetworkHook.OnClientReceive_Internal(FromClientToServerThenClients) -> TryBroadcastToAllClients FAILED - ", e);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug_TTExt.Log("NetworkHook.OnClientReceive_Internal(FromClientToServerThenClients) - ERROR: isServer " + ManNetwork.inst.IsServer + " | " + e);
                    }
                    break;
                case NetMessageType.RequestServerFromClient:
                    try
                    {
                        decoded = (T)Activator.CreateInstance(typeof(T));
                        decoded.Deserialize(netMsg.reader);
                        Debug_TTExt.Info("NetworkHook.OnClientReceive_Internal(RequestServerFromClient) - Server-side trigger for " + StringID + ", type " + Type);
                        if (receiveAction.Invoke(decoded, true))
                        {
                            try
                            {
                                TryBroadcastToClient(netMsg.conn.connectionId, decoded);
                            }
                            catch (Exception e)
                            {
                                throw new Exception("NetworkHook.OnClientReceive_Internal(RequestServerFromClient) -> TryBroadcastToClient FAILED - ", e);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug_TTExt.Log("NetworkHook.OnClientReceive_Internal(RequestServerFromClient) - ERROR: isServer " + ManNetwork.inst.IsServer + " | " + e);
                    }
                    break;
                default:
                    break;
            }
        }
    }

}
