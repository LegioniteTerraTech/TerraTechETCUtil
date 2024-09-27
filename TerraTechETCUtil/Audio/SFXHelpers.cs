using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using FMODUnity;

namespace TerraTechETCUtil
{
    /// <summary>
    /// Handle ingame SFX here
    /// </summary>
    public class SFXHelpers
    {
        private const float SoundFalloffDelay = 1f;

        private static FieldInfo sfxTech = typeof(TechAudio).GetField("m_SimpleEvents", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo sfxTyp = typeof(AudioProvider).GetField("m_SFXType", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo attackT = typeof(AudioProvider).GetField("m_AttackTime", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo relT = typeof(AudioProvider).GetField("m_ReleaseTime", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo asdr = typeof(AudioProvider).GetField("m_Adsr01", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo state = typeof(AudioProvider).GetField("m_State", BindingFlags.NonPublic | BindingFlags.Instance);

        public static bool FetchSound = false;
        public static FMODEventInstance LastPlayed = default;
        public static Dictionary<Transform, HashSet<TechAudio.IModuleAudioProvider>> remoteSFX = new Dictionary<Transform, HashSet<TechAudio.IModuleAudioProvider>>();
        public static TechAudio.TechAudioEventSimple[] freeAudio = null;

        public static void RegisterFloatingSFX<T>(Transform trans, T module) 
            where T : TechAudio.IModuleAudioProvider
        {
            GetFloatingSFX();
            if (remoteSFX.TryGetValue(trans, out var val))
                val.Add(module);
            else
                remoteSFX.Add(trans, new HashSet<TechAudio.IModuleAudioProvider>() { module });
            module.OnAudioTickUpdate += OnModuleTickData;
        }
        public static void UnregisterFloatingSFX<T>(Transform trans, T module)
            where T : TechAudio.IModuleAudioProvider
        {
            if (remoteSFX.TryGetValue(trans, out var val))
            {
                module.OnAudioTickUpdate -= OnModuleTickData;
                val.Remove(module);
                if (val.Any())
                    remoteSFX.Remove(trans);
            }
        }
        private static void GetFloatingSFX()
        {
            if (freeAudio == null && Singleton.playerTank)
            {
                freeAudio = (TechAudio.TechAudioEventSimple[])sfxTech.GetValue(Singleton.playerTank.TechAudio);
            }
        }
        private static void OnModuleTickData(TechAudio.AudioTickData tickData, FMODEvent.FMODParams additionalParam)
        {
            int sfxtypeIndex = tickData.SFXTypeIndex;
            TechAudio.TechAudioEventSimple eventA = freeAudio[sfxtypeIndex];
            if (eventA.m_Event.IsValid() && tickData.provider is ChildModule child && tickData.numTriggered > 0)
                eventA.m_Event.PlayOneShot(child.transform, additionalParam);
        }

        public static void TankPlayOneshot(Tank tank, TechAudio.SFXType SFX)
        {
            try
            {
                var firstBlock = tank.blockman.IterateBlocks().FirstOrDefault();
                if (firstBlock)
                {
                    tank.TechAudio.PlayOneshot(TechAudio.AudioTickData.ConfigureOneshot(
                        firstBlock.GetComponent<ModuleDamage>(), SFX));
                }
            }
            catch { }
        }
        public static void TankPlayLooping(Tank tank, TechAudio.SFXType SFX, float duration, float ASDR1)
        {
            TankPlayLooping(tank, SFX, duration, ASDR1, FMODEvent.FMODParams.empty);
        }
        public static void TankPlayLooping(Tank tank, TechAudio.SFXType SFX, float duration, float ASDR1,
            FMODEvent.FMODParams additionalParams)
        {
            try
            {
                var firstBlock = tank.blockman.IterateBlocks().FirstOrDefault();
                if (firstBlock)
                {
                    var fake = firstBlock.GetComponent<ModuleFakeSound>();
                    if (!fake)
                    {
                        fake = firstBlock.gameObject.AddComponent<ModuleFakeSound>();
                        fake.Init(firstBlock);
                    }
                    fake.PlaySFXLooped(SFX, duration, ASDR1, additionalParams);
                }
            }
            catch { }
        }

        
        internal class ModuleFakeSound : MonoBehaviour
        {
            private Dictionary<TechAudio.SFXType, KeyValuePair<AudioProvider, float>> loopedSFXArticles = new Dictionary<TechAudio.SFXType, KeyValuePair<AudioProvider, float>>();
            private Dictionary<TechAudio.SFXType, KeyValuePair<AudioProvider, float>> loopedSFXArticlesEnd = new Dictionary<TechAudio.SFXType, KeyValuePair<AudioProvider, float>>();
            private Tank tank;
            private TankBlock block;

            internal void Init(TankBlock blockIn)
            {
                blockIn.DetachingEvent.Subscribe(DeInit);
                tank = blockIn.tank;
                block = blockIn;
                enabled = true;
            }
            internal void DeInit()
            {
                for (int step = 0; step < loopedSFXArticlesEnd.Count; step++)
                {
                    AudioProvider AP = loopedSFXArticlesEnd.ElementAt(step).Value.Key;
                    tank.TechAudio.RemoveModule(AP);
                }
                loopedSFXArticlesEnd.Clear();
                for (int step = 0; step < loopedSFXArticles.Count; step++)
                {
                    AudioProvider AP = loopedSFXArticles.ElementAt(step).Value.Key;
                    tank.TechAudio.RemoveModule(AP);
                }
                loopedSFXArticles.Clear();
                enabled = false;
                block.DetachingEvent.Unsubscribe(DeInit);
                DestroyImmediate(this);
            }
            internal void PlaySFXLooped(TechAudio.SFXType SFX, float duration, float ASDR1, FMODEvent.FMODParams additionalParams)
            {
                if (duration > 0)
                {
                    if (loopedSFXArticles.TryGetValue(SFX, out var value))
                    {
                        asdr.SetValue(value.Key, Mathf.Clamp01(ASDR1));
                        value.Key.AdditionalParams = additionalParams;
                        loopedSFXArticles[SFX] = new KeyValuePair<AudioProvider, float>(value.Key, Time.time + duration);
                    }
                    else
                    {
                        AudioProvider AP = new AudioProvider()
                        {
                            AdditionalParams = additionalParams,
                            NoteOn = true,
                        };
                        sfxTyp.SetValue(AP, SFX);
                        state.SetValue(AP, 0);
                        attackT.SetValue(AP, 1f);
                        relT.SetValue(AP, 1f);
                        asdr.SetValue(AP, Mathf.Clamp01(ASDR1));
                        AP.SetParent(block.GetComponent<ModuleDamage>());
                        tank.TechAudio.AddModule(AP);
                        loopedSFXArticles.Add(SFX, new KeyValuePair<AudioProvider, float>(AP, Time.time + duration));
                    }
                }
                else
                {
                    if (loopedSFXArticles.TryGetValue(SFX, out var value))
                    {
                        value.Key.NoteOn = false;
                        value.Key.Update();
                        if (loopedSFXArticlesEnd.ContainsKey(SFX))
                            loopedSFXArticlesEnd[SFX] = new KeyValuePair<AudioProvider, float>(value.Key, Time.time + SoundFalloffDelay);
                        else
                            loopedSFXArticlesEnd.Add(SFX, new KeyValuePair<AudioProvider, float>(value.Key, Time.time + SoundFalloffDelay));
                        loopedSFXArticles.Remove(SFX);
                    }
                }
            }

            private void Update()
            {
                for (int step = 0; step < loopedSFXArticlesEnd.Count;)
                {
                    var ele = loopedSFXArticlesEnd.ElementAt(step);
                    var pair = ele.Value;
                    pair.Key.Update();
                    if (pair.Value <= Time.time)
                    {
                        tank.TechAudio.RemoveModule(pair.Key);
                        loopedSFXArticlesEnd.Remove(ele.Key);
                    }
                    else
                        step++;
                }
                for (int step = 0; step < loopedSFXArticles.Count; )
                {
                    var ele = loopedSFXArticles.ElementAt(step);
                    var pair = ele.Value;
                    if (pair.Value <= Time.time)
                    {
                        pair.Key.NoteOn = false;
                        pair.Key.Update();
                        if (loopedSFXArticlesEnd.ContainsKey(ele.Key))
                            loopedSFXArticlesEnd[ele.Key] = new KeyValuePair<AudioProvider, float>(pair.Key, Time.time + SoundFalloffDelay);
                        else
                            loopedSFXArticlesEnd.Add(ele.Key, new KeyValuePair<AudioProvider, float>(pair.Key, Time.time + SoundFalloffDelay));
                        loopedSFXArticles.Remove(ele.Key);
                    }
                    else
                    {
                        pair.Key.Update();
                        step++;
                    }
                }
                if (!loopedSFXArticles.Any() && !loopedSFXArticlesEnd.Any())
                    DeInit();
            }
        }


        public class GUIManaged : GUILayoutHelpers
        {
            private static FieldInfo sfxPool = typeof(TechAudio).GetField("m_SimpleEvents", BindingFlags.NonPublic | BindingFlags.Instance);

            private static bool controlledDisp = false;
            private static HashSet<string> enabledTabs = null;
            private static string textFieldFind = "event:/SFX/IntroScene/SkyCollision";
            // event:/SFX/IntroScene/Explosion
            //event:/SFX/IntroScene/WhooshFlyby
            private static string textField = "1.4";
            private static float textFieldF = 1.4f;
            private static string textField2 = "1";
            private static float textFieldF2 = 1;
            private static HashSet<ManSFX.MiscSfxType> loopingMisc = null;
            private static HashSet<TechAudio.SFXType> loopingTech = null;
            private static FMODEventInstance eventInst = default;
            public static void GUIGetTotalManaged()
            {
                if (enabledTabs == null)
                {
                    enabledTabs = new HashSet<string>();
                    loopingMisc = new HashSet<ManSFX.MiscSfxType>
                    {
                        ManSFX.MiscSfxType.CabDetachKlaxon,
                        ManSFX.MiscSfxType.Artefact,
                    };
                    eventInst.m_EventPath = textFieldFind;
                    try
                    {
                        eventInst.m_EventInstance = RuntimeManager.CreateInstance(eventInst.m_EventPath);
                    }
                    catch (Exception) { }
                }
                if (loopingTech == null && Singleton.playerTank?.TechAudio)
                {
                    TechAudio.TechAudioEventSimple[] events = (TechAudio.TechAudioEventSimple[])sfxPool.GetValue(Singleton.playerTank.TechAudio);
                    if (events != null)
                    {
                        loopingTech = new HashSet<TechAudio.SFXType>();
                        for (int i = 0; i < events.Length; i++)
                        {
                            var item = events[i];
                            if (item != null && item.m_PlaybackType == TechAudio.SFXPlaybackType.LoopedADSR)
                                loopingTech.Add((TechAudio.SFXType)i);
                        }
                    }
                }
                GUILayout.Box("--- SFX --- ");
                if (GUILayout.Button(" Enabled Loading: " + controlledDisp))
                    controlledDisp = !controlledDisp;
                if (controlledDisp)
                {
                    try
                    {
                        if (Singleton.playerTank)
                        {
                            GUILayout.BeginHorizontal();
                            GUILayout.Label("Direct Sound Path");
                            var change0 = GUILayout.TextField(textFieldFind, 3000);
                            if (change0 != textFieldFind)
                            {
                                if (eventInst.m_EventInstance.isValid())
                                    eventInst.stop();
                                eventInst.m_EventPath = change0;
                                eventInst.m_EventInstance = default;
                                try
                                {
                                    eventInst.m_EventInstance = RuntimeManager.CreateInstance(eventInst.m_EventPath);
                                }
                                catch (Exception) { }
                                ManSFX.inst.PlayUISFX(ManSFX.UISfxType.DropDown);
                                textFieldFind = change0;
                            }
                            if (eventInst.m_EventInstance.isValid())
                            {
                                if (eventInst.CheckPlaybackState(FMOD.Studio.PLAYBACK_STATE.PLAYING))
                                {
                                    if (GUILayout.Button("Stop", AltUI.ButtonGreen))
                                        eventInst.stop();
                                }
                                else if (GUILayout.Button("Play", AltUI.ButtonBlue))
                                {
                                    eventInst.set3DAttributes(Singleton.playerPos);
                                    eventInst.start();
                                }
                            }
                            else
                                GUILayout.Button("Invalid", AltUI.ButtonGrey);
                            GUILayout.EndHorizontal();
                            GUILayout.BeginHorizontal();
                            GUILayout.Label("Play Duration");
                            var change = GUILayout.TextField(textField, 3);
                            if (change != textField)
                            {
                                if (float.TryParse(textField, out var outF))
                                {
                                    textFieldF = outF;
                                    textField = change;
                                }
                                else
                                    textField = textFieldF.ToString();
                            }
                            GUILayout.EndHorizontal();
                            GUILayout.BeginHorizontal();
                            GUILayout.Label("Play Modifier");
                            var change2 = GUILayout.TextField(textField2, 3);
                            if (change2 != textField2)
                            {
                                if (float.TryParse(textField2, out var outF))
                                {
                                    textFieldF2 = outF;
                                    textField2 = change;
                                }
                                else
                                    textField2 = textFieldF2.ToString();
                            }
                            GUILayout.EndHorizontal();
                            GUICategoryDisp<TechAudio.SFXType>(ref enabledTabs, "Tech Single", x => {
                                FetchSound = true;
                                TankPlayOneshot(
                                Singleton.playerTank, x);
                            }, x =>
                                {
                                    return loopingTech == null || !loopingTech.Contains(x);
                                });
                            GUICategoryDisp<TechAudio.SFXType>(ref enabledTabs, "Tech Looped", x =>
                            {
                                FetchSound = true;
                                switch (x)
                                {
                                    case TechAudio.SFXType.EXP_Circuits_Actuator_Ramp_Loop:
                                        TankPlayLooping(Singleton.playerTank, x, textFieldF, 1,
                                            new FMODEvent.FMODParams("Ramp", textFieldF2 > 0 ? 1 : 0));
                                        break;
                                    case TechAudio.SFXType.EXP_Circuits_Actuator_Gate_Loop:
                                        TankPlayLooping(Singleton.playerTank, x, textFieldF, 1,
                                            new FMODEvent.FMODParams("Extension", textFieldF2 > 0 ? 1 : 0));
                                        break;
                                    default:
                                        TankPlayLooping(Singleton.playerTank, x, textFieldF, 1);
                                        break;
                                }
                            });
                        }
                        GUICategoryDisp<ManSFX.UISfxType>(ref enabledTabs, "UI", x =>
                        {
                            FetchSound = true;
                            ManSFX.inst.PlayUISFX(x);
                            InvokeHelper.Invoke(ManSFX.inst.SuppressUISFX, 1);
                        });
                        GUICategoryDisp<ManSFX.MiscSfxType>(ref enabledTabs, "Misc", x =>
                        {
                            FetchSound = true;
                            ManSFX.inst.PlayMiscSFX(x);
                        }, x => !loopingMisc.Contains(x));
                        GUICategoryDisp<ManSFX.MiscSfxType>(ref enabledTabs, "Misc Looping", x =>
                        {
                            FetchSound = true;
                            ManSFX.inst.PlayMiscLoopingSFX(x);
                            InvokeHelper.Invoke(ManSFX.inst.StopMiscLoopingSFX, 1, x);
                        }, x => loopingMisc.Contains(x));
                    }
                    catch (ExitGUIException e)
                    {
                        throw e;
                    }
                    catch { }
                }
            }
        }
    }
}
