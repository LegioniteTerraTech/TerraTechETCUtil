using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using FMODUnity;
using FMOD;

namespace TerraTechETCUtil
{
#if !EDITOR
    /// <summary>
    /// The modded audio player manager for TerraTech
    /// </summary>
    public class ManAudioExt : MonoBehaviour
    {
        private static ManAudioExt inst;
        /// <summary>
        /// The SFX volume of the game
        /// </summary>
        public static float SFXVolume
        {
            get => _SFXVolume;
            private set
            {
                if (_SFXVolume != value)
                {
                    _SFXVolume = value;
                    UpdateCheck();
                }
            }
        }

        internal static float _SFXVolume = 0;

        private static FMOD.System sys = RuntimeManager.LowlevelSystem;
        internal static FMOD.ChannelGroup ModSoundGroup = default;
        /// <summary>
        /// All registered non-vanilla sounds.  
        /// <para>For vanilla sounds see:
        /// <list type="bullet">
        /// <item><see cref="ManMusic"/> for all vanilla music</item>
        /// <item><see cref="ManSFX"/> for vanilla non-Tech specific SFX</item>
        /// <item><see cref="TechAudio"/> for vanilla Tech specific SFX</item>
        /// </list></para>
        /// </summary>
        public static Dictionary<ModContainer, Dictionary<string, AudioGroup>> AllSounds =
            new Dictionary<ModContainer, Dictionary<string, AudioGroup>>();
        /// <summary>
        /// Called after <see cref="ManAudioExt"/> rebuilds all sounds
        /// </summary>
        public static EventNoParams OnRebuildSounds = new EventNoParams();

        /// <summary>
        /// Force <see cref="ManAudioExt"/> to rebuild sounds immedeately.  <b>Very slow.</b>
        /// </summary>
        public static void RebuildAllSounds()
        {
            ClearAllSounds();
            GC.Collect();
            RegisterAllSounds();
            OnRebuildSounds.Send();
        }
        internal static void ClearAllSounds()
        {
            AllSounds.Clear();
        }
        internal static void RegisterAllSounds()
        {
            List<AudioInst> soundsMain = new List<AudioInst>();
            foreach (var item in ResourcesHelper.IterateAllMods())
            {
                if (item.Value != null && !AllSounds.ContainsKey(item.Value))
                {
                    if (item.Value.AssetBundlePath.NullOrEmpty())
                        throw new NullReferenceException("ManSFXExt.RegisterAllSounds() EXPECTS assetbundles to have paths, but they are absent!?");
                    Dictionary<string, AudioGroup> audioLib = new Dictionary<string, AudioGroup>();
                    AllSounds.Add(item.Value, audioLib);
                    string DIR = new DirectoryInfo(item.Value.AssetBundlePath).Parent.ToString();

                    try
                    {
                        foreach (var item2 in ResourcesHelper.IterateAssetsInModContainer<TextAsset>(item.Value, AudioInstFile.leadingFileName))
                        {
                            string nameNoExt = item2.name;
                            if (nameNoExt.EndsWith("_Start") || nameNoExt.EndsWith("_Stop") || nameNoExt.EndsWith("_Engage"))
                                continue;
                            string name = nameNoExt + ".wav";
                            if (audioLib.ContainsKey(name))
                            {
                                //Debug_TTExt.LogError("Internal Sound " + nameNoExt + " for " + item.Key + " could not be added as there is already a conflict!");
                            }
                            else
                            {
                                AudioInst sound = ResourcesHelper.GetAudioFromModAssetBundle(item.Value, nameNoExt, false);
                                if (sound != null)
                                {
                                    while (sound != null)
                                    {
                                        soundsMain.Add(sound);
                                        sound = ResourcesHelper.GetAudioFromModAssetBundle(item.Value, nameNoExt + (soundsMain.Count + 1), false);
                                    }
                                    if (soundsMain.Count > 1)
                                        Debug_TTExt.Log("Found AssetBundle sound " + name + " with (" + soundsMain.Count + ") alternates");
                                    else
                                        Debug_TTExt.Log("Found AssetBundle sound " + name);
                                    AudioGroup audioGroup = new AudioGroup()
                                    {
                                        main = soundsMain.ToArray(),
                                        startup = ResourcesHelper.GetAudioFromModAssetBundle(item.Value, nameNoExt + "_Start", false),
                                        stop = ResourcesHelper.GetAudioFromModAssetBundle(item.Value, nameNoExt + "_Stop", false),
                                        engage = ResourcesHelper.GetAudioFromModAssetBundle(item.Value, nameNoExt + "_Engage", false),
                                    };
                                    soundsMain.Clear();
                                    audioLib.Add(name.Replace(AudioInstFile.leadingFileName, string.Empty), audioGroup);
                                }
                                else
                                    Debug_TTExt.LogError("AssetBundle Sound " + name + " for " + item.Key + " could not be added as it was corrupted!");
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug_TTExt.Log("Error on ManSFXExt.RegisterAllSounds() while trying to collect sounds from AssetBundle for mod ID " + item.Key + " - " + e);
                    }

                    try
                    {
                        foreach (var dir in Directory.EnumerateFiles(DIR, "*.wav", SearchOption.AllDirectories))
                        {
                            string nameNoExt = Path.GetFileNameWithoutExtension(dir);
                            if (nameNoExt.EndsWith("_Start") || nameNoExt.EndsWith("_Stop") || nameNoExt.EndsWith("_Engage"))
                                continue;
                            string name = Path.GetFileName(dir);

                            if (audioLib.ContainsKey(name))
                            {
                                //Debug_TTExt.LogError("Sound " + name + " for " + item.Key + " could not be added as there is already a conflict!");
                            }
                            else
                            {
                                var sound = ResourcesHelper.FetchSoundDirect(name, DIR, item.Value);
                                if (sound != null)
                                {
                                    while (sound != null)
                                    {
                                        soundsMain.Add(sound);
                                        sound = ResourcesHelper.FetchSoundDirect(nameNoExt + (soundsMain.Count + 1) + ".wav", DIR, item.Value);
                                    }
                                    if (soundsMain.Count > 1)
                                        Debug_TTExt.Log("Found External sound " + name + " with (" + soundsMain.Count + ") alternates");
                                    else
                                        Debug_TTExt.Log("Found External sound " + name);
                                    AudioGroup audioGroup = new AudioGroup()
                                    {
                                        main = soundsMain.ToArray(),
                                        startup = ResourcesHelper.FetchSoundDirect(nameNoExt + "_Start.wav", DIR, item.Value),
                                        stop = ResourcesHelper.FetchSoundDirect(nameNoExt + "_Stop.wav", DIR, item.Value),
                                        engage = ResourcesHelper.FetchSoundDirect(nameNoExt + "_Engage.wav", DIR, item.Value),
                                    };
                                    soundsMain.Clear();
                                    audioLib.Add(name, audioGroup);
                                }
                                else
                                    Debug_TTExt.LogError("External Sound " + name + " for " + item.Key + " could not be added as it was corrupted!");
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug_TTExt.Log("Error on ManSFXExt.RegisterAllSounds() while trying to collect sounds from files for mod ID " + item.Key + " - " + e);
                    }
                }
            }
        }


        internal static HashSet<AudioInst> managed;
        internal static void InsureInit()
        {
            if (inst != null)
                return;
            inst = new GameObject("ManAudioExt").AddComponent<ManAudioExt>();
            managed = new HashSet<AudioInst>();
            ManWorldTreadmill.inst.OnAfterWorldOriginMoved.Subscribe(OnWorldMove);
            ManProfile.inst.OnProfileSaved.Subscribe(OnProfileDelta);
            ResourcesHelper.ModsPostLoadEvent.Subscribe(OnBlocksSet);
            ManPauseGame.inst.PauseEvent.Subscribe(OnPaused);
            sys.set3DSettings(1, 1, 1);
            sys.set3DNumListeners(1);
            sys.createChannelGroup("ExternalMods", out ModSoundGroup);
            ADVANCEDSETTINGS settings = default;
            sys.getAdvancedSettings(ref settings);
            ManUpdate.inst.AddAction(ManUpdate.Type.Update, ManUpdate.Order.Last, inst.OnUpdate, 109001);
        }
        private static void OnWorldMove(IntVector3 move)
        {
            foreach (AudioInst inst in managed)
            {
                if (inst.PositionFunc == null && !inst.transform && !inst.Position.IsNaN())
                    inst.Position = move;
            }
        }
        private static void OnProfileDelta(ManProfile.Profile prof)
        {
            SFXVolume = prof.m_SoundSettings.m_SFXVolume;
        }
        private static void OnPaused(bool paused)
        {
            ModSoundGroup.setPaused(paused);
        }
        private static void OnBlocksSet()
        {
            SFXVolume = ManProfile.inst.GetCurrentUser().m_SoundSettings.m_SFXVolume;
            UpdateCheck();
        }
        private static void UpdateCheck()
        {
            foreach (AudioInst inst in managed)
                inst.Volume = inst.Volume;
        }
        /*
        private void Update()
        {
            foreach (AudioInst inst in managed)
            {
                inst.RemoteUpdate();
            }
            FMOD.VECTOR vecP = Singleton.playerPos.ToFMODVector();
            FMOD.VECTOR vecD = default;
            FMOD.VECTOR vecF = Singleton.cameraTrans.forward.ToFMODVector();
            FMOD.VECTOR vecU = Singleton.cameraTrans.up.ToFMODVector();
            sys.set3DListenerAttributes(0, ref vecP, ref vecD, ref vecF, ref vecU);
        }*/

        /// <summary>
        /// Needed to deal with desync
        /// </summary>
        private static Vector3 latentPosVec;
        private static Vector3 latentFwdVec;
        private static Vector3 latentUpVec;
        private void OnUpdate()
        {
            try
            {
                foreach (AudioInst inst in managed)
                {
                    inst.RemoteUpdate();
                }
            }
            catch // IDK what the heck FMOD is doing but somehow it is tampering with ManAudioExt.managed.
                  // We catch the dumb exception to silence it.
            { }
            try
            {
                FMOD.VECTOR vecP = latentPosVec.ToFMODVector();
                FMOD.VECTOR vecD = default;
                FMOD.VECTOR vecF = latentFwdVec.ToFMODVector();
                FMOD.VECTOR vecU = latentUpVec.ToFMODVector();
                sys.set3DListenerAttributes(0, ref vecP, ref vecD, ref vecF, ref vecU);
                sys.update();
                latentPosVec = Singleton.cameraTrans.position;
                latentFwdVec = Singleton.cameraTrans.forward;
                latentUpVec = Singleton.cameraTrans.up;
            }
            catch // extra for safety
            { }
        }

        /// <summary>
        /// Audio grouped together based on name
        /// </summary>
        public struct AudioGroup
        {
            /// <summary>
            /// The main audio to play
            /// </summary>
            public AudioInst[] main;
            /// <summary>
            /// Played when the group starts
            /// </summary>
            public AudioInst startup;
            /// <summary>
            /// Played when the group started
            /// </summary>
            public AudioInst engage;
            /// <summary>
            /// Played when the group stops playing
            /// </summary>
            public AudioInst stop;
        }
    }
    /// <summary>
    /// External extnesion class for <see cref="FMOD.Sound"/>
    /// </summary>
    public static class FMODExt
    {
        /// <summary>
        /// Convert the <see cref="FMOD.Sound"/> to a compatable <see cref="AudioInst"/>
        /// </summary>
        /// <param name="sound"></param>
        /// <param name="setChannel"></param>
        /// <returns></returns>
        public static AudioInst GetAudio(this FMOD.Sound sound, Channel setChannel = default)
            => new AudioInst(ref sound, setChannel);
    }
#endif
}
