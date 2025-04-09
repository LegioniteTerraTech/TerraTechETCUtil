using System;
using UnityEngine;
using UnityEngine.Networking;

namespace TerraTechETCUtil
{
    public enum NetMessageType
    {
        ToClientsOnly,
        FromClientToServerThenClients,
        RequestServerFromClient,
        ToServerOnly,
    }

    public class NetworkHook<T> : NetworkHook where T : MessageBase
    {
        /// <summary>
        /// MessageBase, IsServer
        /// </summary>
        protected Func<T, bool, bool> receiveAction;
        public override string NameFull => typeof(T).ToString() +" [" + AssignedID + "]";

        public NetworkHook(Func<T, bool, bool> onReceive, NetMessageType type)
        {
            receiveAction = onReceive;
            Type = type;
        }
        public override void OnToClientReceive_Internal(NetworkMessage netMsg)
        {
            T decoded;
            switch (Type)
            {
                case NetMessageType.ToClientsOnly:
                    decoded = (T)Activator.CreateInstance(typeof(T));
                    decoded.Deserialize(netMsg.reader);
                    Debug_TTExt.Info("NetworkHook.OnClientReceive_Internal(ToClientsOnly) - Client-side trigger for " + AssignedID + ", type " + Type);
                    receiveAction.Invoke(decoded, false);
                    break;
                case NetMessageType.ToServerOnly:
                    throw new Exception("NetworkHook.OnClientReceive_Internal(ToServerOnly) - ServerOnly sent to client for " + AssignedID + ", type " + Type);
                case NetMessageType.FromClientToServerThenClients:
                    try
                    {
                        decoded = (T)Activator.CreateInstance(typeof(T));
                        decoded.Deserialize(netMsg.reader);
                        Debug_TTExt.Info("NetworkHook.OnClientReceive_Internal(FromClientToServerThenClients) - Client-side trigger for " + AssignedID + ", type " + Type);
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
                        Debug_TTExt.Log("NetworkHook.OnClientReceive_Internal(RequestServerFromClient) - Client-side trigger for " + AssignedID + ", type " + Type);
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
        public override void OnToServerReceive_Internal(NetworkMessage netMsg)
        {
            T decoded;
            switch (Type)
            {
                case NetMessageType.ToClientsOnly:
                    throw new Exception("NetworkHook.OnClientReceive_Internal(ToClientsOnly) - ClientsOnly sent to server for " + AssignedID + ", type " + Type);
                case NetMessageType.ToServerOnly:
                    decoded = (T)Activator.CreateInstance(typeof(T));
                    decoded.Deserialize(netMsg.reader);
                    Debug_TTExt.Log("NetworkHook.OnClientReceive_Internal(ToServerOnly) - Server-side trigger for " + AssignedID + ", type " + Type);
                    receiveAction.Invoke(decoded, true);
                    break;
                case NetMessageType.FromClientToServerThenClients:
                    try
                    {
                        decoded = (T)Activator.CreateInstance(typeof(T));
                        decoded.Deserialize(netMsg.reader);
                        Debug_TTExt.Info("NetworkHook.OnClientReceive_Internal(FromClientToServerThenClients) - Server-side trigger for " + AssignedID + ", type " + Type);
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
                        Debug_TTExt.Info("NetworkHook.OnClientReceive_Internal(RequestServerFromClient) - Server-side trigger for " + AssignedID + ", type " + Type);
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
