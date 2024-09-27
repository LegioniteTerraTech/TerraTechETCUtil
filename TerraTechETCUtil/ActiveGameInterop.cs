using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
using System.IO.Compression;
using System.IO;
using System.Collections;

namespace TerraTechETCUtil
{
    public class ActiveGameInterop
    {
        /// <summary>
        /// The side this is working on
        /// </summary>
        public static DataSenderTransmit OurSender { get; } = Application.isPlayer? DataSenderTransmit.Game : DataSenderTransmit.Editor;

        static ActiveGameInterop()
        {
            OnRecieve.Add(DataTypeTransmit.StartupOneSide.ToString(), (string x) =>
            {
                if (x != null && x == "Marco")
                    TryTransmit(DataTypeTransmit.StartupOtherSide.ToString(), "Polo");
            });
            OnRecieve.Add(DataTypeTransmit.StartupOtherSide.ToString(), (string x) =>
            {
                if (x != null && x == "Polo")
                {
                    IsReady = true;
                    TryTransmit(DataTypeTransmit.StartupFinalizer.ToString(), "Good!");
                    IsReady = true;
                    _debug = "Ready!";
                }
            });
            OnRecieve.Add(DataTypeTransmit.StartupFinalizer.ToString(), (string x) =>
            {
                if (x != null && x == "Good!")
                {
                    IsReady = true;
                    _debug = "Ready!";
                }
            });
            OnRecieve.Add(DataTypeTransmit.String.ToString(), (string x) =>
            {
                if (OnStringRecieved != null)
                    OnStringRecieved.Invoke(x);
            });
            OnRecieve.Add(DataTypeTransmit.Disconnect.ToString(), (string x) =>
            {
                if (OurSender == DataSenderTransmit.Editor)
                    CheckIfReady();
                IsReady = false;
                _debug = "Disconnected";
            });
            OnRecieve.Add(DataTypeTransmit.Shutdown.ToString(), (string x) =>
            {
                DeInitJustThisSide();
            });
            OnRecieve.Add(DataTypeTransmit.SendGameObject.ToString(), (string x) =>
            {

                if (x == null)
                {
                    _debug = "GO Retrival Failed - null";
                    return;
                }
                if (RecentGO != null)
                {
                    if (OurSender == DataSenderTransmit.Editor)
                        UnityEngine.Object.DestroyImmediate(RecentGO, true);
                    else
                        UnityEngine.Object.Destroy(RecentGO);
                }
                _debug = "GO Retrival underway...";
                try
                {
                    List<ResourcesHelper.SerialGO> serials =
                    JsonConvert.DeserializeObject<List<ResourcesHelper.SerialGO>>(x);
                    if (serials == null || !serials.Any())
                    {
                        _debug = "GO Retrival Failed - No information recieved";
                    }
                    else
                    {
                        RecentGO = ResourcesHelper.DecompressFromSerials(serials);
                        _debug = "GO Retrival for " + serials.First().Name + " success!";
                    }
                }
                catch (Exception e)
                {
                    _debug = "GO Retrival Failed - Corrupted - " + e;
                }
            });

#if !EDITOR
            OnRecieve.Add(DataTypeTransmit.SpawnRawTechTemplate.ToString(), (string x) =>
            {

                if (x == null)
                    return;
                if (RecentTech != null)
                {
                    RecentTech.visible.RemoveFromGame();
                }
                using (MemoryStream MS = new MemoryStream())
                {
                    using (StreamWriter SR = new StreamWriter(MS))
                    {
                        SR.Write(x);
                        using (GZipStream GZS = new GZipStream(MS, CompressionMode.Decompress))
                        {
                            using (StreamReader SR2 = new StreamReader(GZS))
                            {
                                try
                                {
                                    Tank playerT = Singleton.playerTank;
                                    RawTechTemplate serials = JsonConvert.DeserializeObject<RawTechTemplate>(SR2.ReadToEnd());
                                    RecentTech = serials.SpawnRawTech(playerT.boundsCentreWorld +
                                        playerT.rootBlockTrans.forward * 64, ManPlayer.inst.PlayerTeam,
                                        -playerT.rootBlockTrans.forward);
                                }
                                catch { }
                            }
                        }
                    }
                }
            });
#endif
        }

        public static bool IsReady { get; private set; } = false;
        public static string _debug = "Disabled";
        public static Action<string> OnStringRecieved;
        public static Dictionary<string, Action<string>> OnRecieve = new Dictionary<string, Action<string>>();
        public static GameObject RecentGO = null;

        private static bool SendingNow = false;
#if !EDITOR
        public static Tank RecentTech = null;
        public static void TransmitAllBlocks(List<TankBlock> blocks)
        {
            foreach (var item in blocks)
                TransmitBlock(item);
        }
        public static void TransmitBlock(TankBlock block)
        {
            _debug = "Block " + block.name + " transmit started";
            try
            {
                SendingNow = true;
                Queued.Enqueue(new QueuedRequest(DataTypeTransmit.RebuildBlock.ToString(),
                    JsonConvert.SerializeObject(ResourcesHelper.CompressToSerials(block.gameObject)), RunTransmit));
                _debug = "Block " + block.name + " transmitting...";
            }
            catch (Exception e)
            {
                _debug = "Block " + block.name + " transmit failed - " + e;
            }
        }
#endif

        public static void TryTransmit(string type)
        {
            SendingNow = true;
            Queued.Enqueue(new QueuedRequest(type, type.ToString(), RunTransmit));
        }
        public static void TryTransmit(GameObject GO)
        {
            _debug = "GO transmit started";
            try
            {
                SendingNow = true;
                Queued.Enqueue(new QueuedRequest(DataTypeTransmit.SendGameObject.ToString(),
                    JsonConvert.SerializeObject(ResourcesHelper.CompressToSerials(GO)), RunTransmit));
                _debug = "GO transmitting...";
            }
            catch (Exception e)
            {
                _debug = "GO transmit failed - " + e;
            }
        }
        public static void TryTransmitTest(string dataString)
        {
            SendingNow = true;
            Queued.Enqueue(new QueuedRequest(DataTypeTransmit.String.ToString(),
                dataString, RunTransmit));
        }
        public static void TryTransmit(string type, string classDataJson)
        {
            SendingNow = true;
            Queued.Enqueue(new QueuedRequest(type, classDataJson, RunTransmit));
        }
        public static void TryTransmit<T>(string type, T classInst)
        {
            SendingNow = true;
            Queued.Enqueue(new QueuedRequest(type, JsonConvert.SerializeObject(classInst), RunTransmit));
        }



        // Below this point - EDIT AT YOUR OWN RISK
        public const string Name = "TerraTech_Editor_Interop";
        private const int MaxCharLim = 65536;//16384;//2048;
        private const int MemNeeded = 5 + (MaxCharLim * 2);

        public static bool inst = false;
        public static bool IsReceiving => !Queued.Any();
        private static MemoryMappedFile Main;
        private static MemoryMappedViewAccessor Access;
        private static Queue<QueuedRequest> Queued = new Queue<QueuedRequest>();
        private class QueuedRequest
        {
            public QueuedRequest(string type, string data, Func<QueuedRequest, bool> task)
            {
                Type = type;
                Data = data;
                Task = task;
            }
            public readonly string Type;
            public readonly Func<QueuedRequest, bool> Task;
            public string Data;
        }

        public enum DataSenderTransmit : byte
        {
            None,
            Editor,
            Game,
        }
        public enum DataTypeTransmit : short
        {
            None,
            StartupOneSide,
            StartupOtherSide,
            StartupFinalizer,
            String,
            Disconnect,
            Shutdown,
            SendGameObject,
            SpawnRawTechTemplate,
            RebuildBlock,
        }

        private static bool SubToQuit = false;
        public static bool CheckIfInteropActiveGameOnly()
        {
            try
            {
                if (OurSender == DataSenderTransmit.Game)
                {
                    Main = MemoryMappedFile.OpenExisting(Name);
                    return Main != null;
                }
            }
            catch { }
            return false;
        }
        public static void Init()
        {
            if (inst)
                return;
            inst = true;
            Debug_TTExt.Log(Name + " Setup");
            if (Application.isEditor)
            {
                OnStringRecieved = (string x) =>
                {
                    _debug = x;
                };
            }
            else
            {
                OnStringRecieved = (string x) =>
                {
#if !EDITOR
                    ManSFX.inst.PlayUISFX(ManSFX.UISfxType.LevelUp);
                    ManUI.inst.ShowErrorPopup(x);
#endif
                };
            }
            if (!SubToQuit)
            {
                SubToQuit = true;
                Application.quitting += DeInitJustThisSide;
            }

            _debug = "Disconnected";
            CheckIfReady();
        }

        public static void CheckIfReady()
        {
            TryTransmit(DataTypeTransmit.StartupOneSide.ToString(), "Marco");
        }

        public static void DeInitBothEnds()
        {
            if (!inst)
                return;
            TryTransmit(DataTypeTransmit.Shutdown.ToString());
            DeInitJustThisSide();
        }
        public static void DeInitJustThisSide()
        {
            if (!inst)
                return;
            if (Access != null)
            {
                Access.Dispose();
                Access = null;
                Debug_TTExt.Log(Name + " release Access");
            }
            if (Main != null)
            {
                Main.Dispose();
                Main = null;
                Debug_TTExt.Log(Name + " closed");
            }
            inst = false;
            IsReady = false;
            Queued.Clear();
            Debug_TTExt.Log(Name + " Reset");
            _debug = "Disabled";
        }

        private static int counter = 0;
        private static int counterEnd = Application.isEditor ? 0 : 80;
        public static void Update_Static()
        {
            if (counter > counterEnd)
            {
                UpdateNow();
            }
            else
                counter++;
        }
        public static void UpdateNow()
        {
            if (!Queued.Any())
            {
                HandleRecieving();
                counter = 0;
            }
            else
            {
                while (Queued.Any())
                {
                    try
                    {
                        var peek = Queued.Peek();
                        if (peek.Task.Invoke(peek))
                            return;
                    }
                    catch (Exception e)
                    {
                        Debug_TTExt.Log("Could not transmit Error - " + e);
                    }
                    Queued.Dequeue();
                    counter = 0;
                }
            }
        }

        private static bool TryStartMMF()
        {
            if (Main == null)
            {
                try
                {
                    if (OurSender == DataSenderTransmit.Editor)
                        Main = MemoryMappedFile.CreateOrOpen(Name, MemNeeded);
                    else
                        Main = MemoryMappedFile.OpenExisting(Name);
                    Debug_TTExt.Log(Name + " opened");
                    return true;
                }
                catch { }
                return false;
            }
            return true;
            /*
            if (Access == null)
            {
                Access = Main.CreateViewAccessor(0, MemNeeded);
                Debug_TTExt.Log(Name + " Accessed");
            }
            */
        }
        private static StringBuilder charCombiner = new StringBuilder();
        public static bool Observe()
        {
            try
            {
                if (!TryStartMMF())
                {
                    _debug = "Editor side is INACTIVE, waiting...";
                    return false;
                }
                Access = Main.CreateViewAccessor(0, MemNeeded);
                try
                {
                    if (Access.CanRead && Access.CanWrite)
                    {
                        DataSenderTransmit dataSender = (DataSenderTransmit)Access.ReadByte(0);
                        short typeLength = Access.ReadInt16(1);
                        for (int i = 0; i < typeLength; i++)
                        {
                            charCombiner.Append(Access.ReadChar(5 + i * 2));
                        }
                        string dataType = charCombiner.ToString();
                        charCombiner.Clear();
                        int post = 5 + (typeLength * 2);
                        short dataLength = Access.ReadInt16(3);
                        for (int i = 0; i < dataLength; i++)
                        {
                            charCombiner.Append(Access.ReadChar(post + i * 2));
                        }
                        Debug_TTExt.Log("Contents: " + dataSender.ToString() + ", " + dataType.ToString() + " -- " + charCombiner.ToString());
                        charCombiner.Clear();
                    }
                    else
                        _debug = "Waiting on other side to finish sending...";
                    return false;
                }
                finally
                {
                    Access.Dispose();
                }
            }
            catch
            {
                return false;
            }
        }

        private static bool HandleRecieving()
        {
            try
            {
                if (!TryStartMMF())
                {
                    _debug = "Editor side is INACTIVE, waiting...";
                    return false;
                }
                Access = Main.CreateViewAccessor();

                SendingNow = false;
                try
                {
                    if (Access.CanRead && Access.CanWrite)
                    {
                        short typeLength = Access.ReadInt16(1);
                        for (int i = 0; i < typeLength; i++)
                        {
                            charCombiner.Append(Access.ReadChar(5 + i * 2));
                        }
                        string dataType = charCombiner.ToString();
                        charCombiner.Clear();
                        int post = 5 + (typeLength * 2);
                        switch (OurSender)
                        {
                            case DataSenderTransmit.None:
                                break;
                            case DataSenderTransmit.Editor:
                                if ((DataSenderTransmit)Access.ReadByte(0) == DataSenderTransmit.Game)
                                {
                                    if (OnRecieve.TryGetValue(dataType, out Action<string> val))
                                    {
                                        short CharCount = Access.ReadInt16(3);
                                        for (int i = 0; i < CharCount; i++)
                                        {
                                            charCombiner.Append(Access.ReadChar(post + i * 2));
                                        }
                                        Access.Write(0, (int)DataSenderTransmit.None);
                                        //Access.Flush();
                                        val.Invoke(charCombiner.ToString());
                                        charCombiner.Clear();
                                        return true;
                                    }
                                    else
                                        _debug = "Retrival of data of type " + dataType + " FAILED...";
                                }
                                break;
                            case DataSenderTransmit.Game:
                                if ((DataSenderTransmit)Access.ReadByte(0) == DataSenderTransmit.Editor)
                                {
                                    if (OnRecieve.TryGetValue(dataType, out Action<string> val))
                                    {
                                        short CharCount = Access.ReadInt16(3);
                                        for (int i = 0; i < CharCount; i++)
                                        {
                                            charCombiner.Append(Access.ReadChar(post + i * 2));
                                        }
                                        Access.Write(0, (int)DataSenderTransmit.None);
                                        //Access.Flush();
                                        val.Invoke(charCombiner.ToString());
                                        charCombiner.Clear();
                                        return true;
                                    }
                                    else
                                        _debug = "Retrival of data of type " + dataType + " FAILED...";
                                }
                                break;
                            default:
                                break;
                        }
                    }
                    else
                        _debug = "Waiting on other side to finish sending...";
                    return false;
                }
                finally
                {
                    if (Access.CanWrite && OurSender != DataSenderTransmit.None)
                        Access.Write(0, (int)DataSenderTransmit.None);
                    Access.Dispose();
                }
            }
            catch (Exception e)
            {
                _debug = "ERROR - " + e;
                Debug_TTExt.Log("Could not recieve Error - " + e);
                return false;
            }
        }


        private static bool RunTransmit(QueuedRequest dataReq)
        {
            try
            {
                if (!TryStartMMF())
                {
                    _debug = "Editor side is INACTIVE, waiting...";
                    return false;
                }
                Access = Main.CreateViewAccessor();
                try
                {
                    if (Access.CanRead && Access.CanWrite)
                    {
                        switch ((DataSenderTransmit)Access.ReadByte(0))
                        {
                            case DataSenderTransmit.None:
                                if (Access.CanWrite)
                                {
                                    string type = dataReq.Type;
                                    string data = dataReq.Data;
                                    if (type == null)
                                        throw new ArgumentOutOfRangeException("Request type is null");
                                    if (data == null)
                                        throw new ArgumentOutOfRangeException("Request data is null");
                                    if (type.Length + data.Length > MaxCharLim)
                                        throw new ArgumentOutOfRangeException("Request is too large (exceeded " + MaxCharLim + 
                                            " characters, requested was " + (type.Length + data.Length) + ")");
                                    _debug = "Sending data of type " + dataReq.Type + "...";
                                    Access.Write(0, (byte)OurSender);
                                    Access.Write(1, (short)type.Length);
                                    Access.Write(3, (short)data.Length);

                                    for (int i = 0; type.Length > i; i++)
                                    {
                                        Access.Write(5 + i * 2, type[i]);
                                    }
                                    int post = (type.Length * 2) + 5;
                                    for (int i = 0; data.Length > i; i++)
                                    {
                                        Access.Write(post + i * 2, data[i]);
                                    }
                                    //Access.Flush();
                                    _debug = "Sent data of type " + dataReq.Type;

                                    return true;
                                }
                                _debug = "Waiting on other side...";
                                break;
                            case DataSenderTransmit.Editor:
                                if (OurSender == DataSenderTransmit.Editor)
                                    _debug = "Waiting on send";
                                else
                                    _debug = "Waiting on retreive";
                                break;
                            case DataSenderTransmit.Game:
                                if (OurSender == DataSenderTransmit.Editor)
                                    _debug = "Waiting on retreive";
                                else
                                    _debug = "Waiting on send";
                                break;
                            default:
                                _debug = "Error - invalid sender value of " + Access.ReadByte(0).ToString();
                                break;
                        }
                    }
                    else
                        _debug = "Waiting on other side to finish sending...";
                    return false;
                }
                finally
                {
                    Access.Dispose();
                }
            }
            catch (Exception e)
            {
                _debug = "ERROR - " + e;
                throw e;
            }
        }
    }
}