using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace TerraTechETCUtil 
{
    /// <summary>
    /// Handle ingame SFX here
    /// </summary>
    public class SFXHelpers
    {
        private const float SoundFalloffDelay = 1f;

        private static FieldInfo sfxTyp = typeof(AudioProvider).GetField("m_SFXType", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo attackT = typeof(AudioProvider).GetField("m_AttackTime", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo relT = typeof(AudioProvider).GetField("m_ReleaseTime", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo asdr = typeof(AudioProvider).GetField("m_Adsr01", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo state = typeof(AudioProvider).GetField("m_State", BindingFlags.NonPublic | BindingFlags.Instance);

        public static FMODEvent LastPlayed = default;

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

            private static bool controlledDisp = false;
            private static HashSet<string> enabledTabs = null;
            private static string textField = "";
            private static float textFieldF = 1.4f;
            private static string textField2 = "";
            private static float textFieldF2 = 1;
            private static HashSet<ManSFX.MiscSfxType> looping = null;
            public static void GUIGetTotalManaged()
            {
                if (enabledTabs == null)
                {
                    enabledTabs = new HashSet<string>();
                    looping = new HashSet<ManSFX.MiscSfxType>
                    {
                        ManSFX.MiscSfxType.CabDetachKlaxon,
                        ManSFX.MiscSfxType.Artefact,
                    };
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
                            GUICategoryDisp<TechAudio.SFXType>(ref enabledTabs, "Tech Single", x => TankPlayOneshot(
                                Singleton.playerTank, x));
                            GUICategoryDisp<TechAudio.SFXType>(ref enabledTabs, "Tech Looped", x =>
                            {
                                if (x == TechAudio.SFXType.EXP_Circuits_Actuator_Ramp_Loop)
                                {
                                    TankPlayLooping(Singleton.playerTank, x, textFieldF, 1,
                                        new FMODEvent.FMODParams("Ramp", textFieldF2 > 0 ? 1 : 0));
                                }
                                else if (x == TechAudio.SFXType.EXP_Circuits_Actuator_Gate_Loop)
                                {
                                    TankPlayLooping(Singleton.playerTank, x, textFieldF, 1,
                                        new FMODEvent.FMODParams("Extension", textFieldF2 > 0 ? 1 : 0));
                                }
                                else
                                {
                                    TankPlayLooping(Singleton.playerTank, x, textFieldF, 1);
                                }
                            });
                        }
                        GUICategoryDisp<ManSFX.UISfxType>(ref enabledTabs, "UI", x => ManSFX.inst.PlayUISFX(x));
                        GUICategoryDisp<ManSFX.MiscSfxType>(ref enabledTabs, "Misc", x =>
                        {
                            if (!looping.Contains(x))
                                ManSFX.inst.PlayMiscSFX(x);
                        });
                        GUICategoryDisp<ManSFX.MiscSfxType>(ref enabledTabs, "Misc Looping", x =>
                        { 
                            ManSFX.inst.PlayMiscSFX(x);
                            InvokeHelper.Invoke(ManSFX.inst.StopMiscLoopingSFX, 1, x);
                        }
                        );
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
