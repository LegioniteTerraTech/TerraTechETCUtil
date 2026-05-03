using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using FMODUnity;
using HarmonyLib;

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


        internal static bool FetchSound = false;
        /// <summary>  </summary>
        public static FMODEventInstance LastPlayed = default;
        /// <summary>  </summary>
        public static Dictionary<Transform, HashSet<TechAudio.IModuleAudioProvider>> remoteSFX =
            new Dictionary<Transform, HashSet<TechAudio.IModuleAudioProvider>>();
        /// <summary>  </summary>
        public static TechAudio.TechAudioEventSimple[] freeAudio = null;

        /// <summary>
        /// Not advised.  Allows playing of SFX BUT it's incorrectly tied to the position 
        ///  of the first player Tech that was active the first time this function is called!!!
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="trans"></param>
        /// <param name="module"></param>
        [Obsolete]
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
        /// <summary>
        /// Not advised.  Allows playing of SFX BUT it's incorrectly tied to the position 
        ///  of the first player Tech that was active the first time this function is called!!!
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="trans"></param>
        /// <param name="module"></param>
        [Obsolete]
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
            if (freeAudio != null)
            {
                TechAudio.TechAudioEventSimple eventA = freeAudio[sfxtypeIndex];
                if (eventA.m_Event.IsValid() && tickData.provider is ChildModule child && tickData.numTriggered > 0)
                    eventA.m_Event.PlayOneShot(child.transform, additionalParam);
            }
        }

        /// <summary>
        /// Like <see cref="TechAudio.PlayOneshot(TankBlock, TechAudio.SFXType)"/>, this plays the sounds for modded content
        /// </summary>
        /// <param name="tank"></param>
        /// <param name="SFX"></param>
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
        /// <summary>
        /// Plays looping audio for modded SFX.
        /// </summary>
        /// <param name="tank"></param>
        /// <param name="SFX"></param>
        /// <param name="duration"></param>
        /// <param name="ASDR1"></param>
        public static void TankPlayLooping(Tank tank, TechAudio.SFXType SFX, float duration, float ASDR1)
        {
            TankPlayLooping(tank, SFX, duration, ASDR1, FMODEvent.FMODParams.empty);
        }
        /// <summary>
        /// Plays looping audio for modded SFX.
        /// </summary>
        /// <param name="tank"></param>
        /// <param name="SFX"></param>
        /// <param name="duration"></param>
        /// <param name="ASDR1"></param>
        /// <param name="additionalParams"></param>
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


        internal class GUIManaged : GUILayoutHelpers
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
                GUIManSFX();
            }
            private static Collider FirstPlayerCollider => Singleton.playerTank.trans.GetComponentInChildren<Collider>(true);
            private static Collision CreateFraudCollision(float sped)
            {
                Collision faker = new Collision();
                var collodo = FirstPlayerCollider;
                AccessTools.Field(typeof(Collision), "m_Collider").SetValue(faker, collodo);
                AccessTools.Field(typeof(Collision), "m_Impulse").SetValue(faker,
                    new Vector3(0, 0, sped));
                AccessTools.Field(typeof(Collision), "m_RelativeVelocity").SetValue(faker,
                    new Vector3(0, 0, sped));
                AccessTools.Field(typeof(Collision), "m_Rigidbody").SetValue(faker,
                    Singleton.playerTank.rbody);
                AccessTools.Field(typeof(Collision), "m_ContactCount").SetValue(faker, 1);

                ContactPoint CoP = new ContactPoint();
                AccessTools.Field(typeof(ContactPoint), "m_ThisColliderInstanceID").SetValue(CoP, collodo.GetInstanceID());
                AccessTools.Field(typeof(ContactPoint), "m_OtherColliderInstanceID").SetValue(CoP, collodo.GetInstanceID());

                AccessTools.Field(typeof(Collision), "m_LegacyContacts").SetValue(faker,
                    new ContactPoint[] { CoP });
                return faker;
            }
            public static void GUIManSFX()
            {
                GUILayout.Box("--- Vanilla SFX ---", AltUI.BoxBlackTextBlueTitle);
                if (GUILayout.Button(" Enabled Loading: " + controlledDisp))
                    controlledDisp = !controlledDisp;
                if (controlledDisp)
                {
                    try
                    {
                        if (Singleton.playerTank)
                        {
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
                                TankPlayOneshot(Singleton.playerTank, x);
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

                            GUILayout.BeginHorizontal();
                            GUILayout.Label("Collision");
                            if (GUILayout.Button("Boop"))
                            {
                                FetchSound = true;
                                var col = new Tank.CollisionInfo();
                                col.Init(CreateFraudCollision(0.1f));
                                col.DealImpactDamage = false;
                                ManSFX.inst.PlayTechImpactSFX(Singleton.playerTank, col);
                            }
                            if (GUILayout.Button("Light"))
                            {
                                FetchSound = true;
                                var col = new Tank.CollisionInfo();
                                col.Init(CreateFraudCollision(5f));
                                col.DealImpactDamage = false;
                                ManSFX.inst.PlayTechImpactSFX(Singleton.playerTank, col);
                            }
                            if (GUILayout.Button("Med"))
                            {
                                FetchSound = true;
                                var col = new Tank.CollisionInfo();
                                col.Init(CreateFraudCollision(12f));
                                col.DealImpactDamage = false;
                                ManSFX.inst.PlayTechImpactSFX(Singleton.playerTank, col);
                            }
                            if (GUILayout.Button("Fast"))
                            {
                                FetchSound = true;
                                var col = new Tank.CollisionInfo();
                                col.Init(CreateFraudCollision(50f));
                                col.DealImpactDamage = false;
                                ManSFX.inst.PlayTechImpactSFX(Singleton.playerTank, col);
                            }
                            if (GUILayout.Button("Ex"))
                            {
                                FetchSound = true;
                                var col = new Tank.CollisionInfo();
                                col.Init(CreateFraudCollision(125f));
                                col.DealImpactDamage = false;
                                ManSFX.inst.PlayTechImpactSFX(Singleton.playerTank, col);
                            }
                            GUILayout.EndHorizontal();

                            GUICategoryDisp<ManSFX.WeaponImpactSfxType>(ref enabledTabs, "Weapon Impact", x =>
                            {
                                FetchSound = true;
                                ManSFX.inst.PlayImpactSFX(Singleton.playerTank, x,
                                    Singleton.playerTank.trans.GetComponentInChildren<Damageable>(true),
                                    Singleton.playerPos, FirstPlayerCollider);
                            });
                            GUICategoryDisp<ManSFX.TransformOneshotSFXTypes>(ref enabledTabs, "Transform Anim", x =>
                            {
                                FetchSound = true;
                                ManSFX.inst.PlayTransformOneshotSFX(x, Singleton.cameraTrans);
                            });
                            GUICategoryDisp<SceneryTypes>(ref enabledTabs, "Scenery Damage", x =>
                            {
                                FetchSound = true;
                                ManSFX.inst.PlaySceneryDebrisSFX(SpawnHelper.GetSceneryByType(x).Values.First().First().
                                    GetComponent<ResourceDispenser>(), 1);
                            });
                            GUICategoryDisp<SceneryTypes>(ref enabledTabs, "Scenery Death", x =>
                            {
                                FetchSound = true;
                                ManSFX.inst.PlaySceneryDestroyedSFX(SpawnHelper.GetSceneryByType(x).Values.First().First().
                                    GetComponent<ResourceDispenser>());
                            });
                            GUICategoryDisp<ManSFX.ExplosionSize>(ref enabledTabs, "Block hit", x =>
                            {
                                FetchSound = true;
                                
                                var fraubBlock = Singleton.playerTank.CentralBlock;
                                var grabber = AccessTools.Field(typeof(TankBlock), "m_CurrentMass");
                                float curMass = (float)grabber.GetValue(fraubBlock);
                                switch (x)
                                {
                                    case ManSFX.ExplosionSize.Small:
                                        grabber.SetValue(fraubBlock, 0.5f);
                                        break;
                                    case ManSFX.ExplosionSize.Medium:
                                        grabber.SetValue(fraubBlock, 1f);
                                        break;
                                    case ManSFX.ExplosionSize.Large:
                                    default:
                                        grabber.SetValue(fraubBlock, 8f);
                                        break;
                                }
                                ManSFX.inst.PlayBlockImpactSFX(fraubBlock, CreateFraudCollision(50));
                                grabber.SetValue(fraubBlock, curMass);
                            });
                            GUICategoryDisp<ManSFX.ExplosionSize>(ref enabledTabs, "Chunk hit", x =>
                            {
                                FetchSound = true;

                                float sped = 0;
                                switch (x)
                                {
                                    case ManSFX.ExplosionSize.Small:
                                        sped = 0.5f;
                                        break;
                                    case ManSFX.ExplosionSize.Medium:
                                        sped = 5f;
                                        break;
                                    case ManSFX.ExplosionSize.Large:
                                    default:
                                        sped = 50f;
                                        break;
                                }
                                ManSFX.inst.PlayChunkImpactSFX(ResourceManager.inst.resourceTable.
                                    resources[0].basePrefab.GetComponent<ResourcePickup>(), sped);
                            });
                        }
                        if (GUILayout.Button("Explosions"))
                        {
                            if (enabledTabs.Contains("Explosions"))
                                enabledTabs.Remove("Explosions");
                            else
                                enabledTabs.Add("Explosions");
                        }
                        if (enabledTabs.Contains("Explosions"))
                        {
                            foreach (ManSFX.ExplosionSize size in Enum.GetValues(typeof(ManSFX.ExplosionSize)))
                            {
                                if (GUILayout.Button(" Type: " + size.ToString()))
                                {
                                    if (enabledTabs.Contains("Explosions" + size.ToString()))
                                        enabledTabs.Remove("Explosions" + size.ToString());
                                    else
                                        enabledTabs.Add("Explosions" + size.ToString());
                                }
                                if (enabledTabs.Contains("Explosions" + size.ToString()))
                                {
                                    foreach (ManSFX.ExplosionType eType in Enum.GetValues(typeof(ManSFX.ExplosionType)))
                                    {
                                        foreach (FactionSubTypes fType in Enum.GetValues(typeof(FactionSubTypes)))
                                        {
                                            if (GUILayout.Button(fType + " | " + eType.ToString()))
                                            {
                                                FetchSound = true;
                                                ManSFX.inst.PlayExplosionSFX(Singleton.playerPos, eType, size, fType);
                                            }
                                        }
                                    }
                                }
                            }
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
