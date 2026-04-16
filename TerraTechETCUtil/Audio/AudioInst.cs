using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Text;
using UnityEngine;
using System.Drawing;
using Newtonsoft.Json;
using System.Runtime.InteropServices;
using System.Drawing.Drawing2D;

#if !EDITOR
using FMOD;
using FMODUnity;
#endif


namespace TerraTechETCUtil
{
#if !EDITOR
    /// <summary>
    /// An audio file managed by <see cref="TerraTechETCUtil"/>
    /// </summary>
    public class AudioInst
    {
        private static FMOD.System sys = RuntimeManager.LowlevelSystem;

        /// <summary>
        /// The sound is playing
        /// </summary>
        public bool IsPlaying
        {
            get
            {
                ActiveSound.isPlaying(out bool playing);
                return playing;
            }
        }
        /// <summary>
        /// the sound is paused
        /// </summary>
        public bool IsPaused
        {
            get
            {
                ActiveSound.getPaused(out bool paused);
                return paused;
            }
        }
        /// <summary>
        /// Random variance of the sound selected <b>when it starts playing</b>
        /// </summary>
        public float PitchVariance = 0;
        /// <summary>
        /// Random variance of the sound selected <b>when it starts playing</b>
        /// </summary>
        public float RangeVariance = 0;
        /// <summary>
        /// the current volume of the sound, multiplied later on by the user's <see cref="ManProfile.SoundSettings"/>
        /// </summary>
        public float Volume
        {
            get => _volume;
            set
            {
                _volume = value;
                if (IsPlaying)
                    ActiveSound.setVolume(ManAudioExt._SFXVolume * value);
            }
        }
        /// <summary>
        /// The maximum range of the sound, where it's gone entirely
        /// </summary>
        public float RangeMax
        {
            get => _rangeMax;
            set
            {
                _rangeMax = value;
                ActiveSound.isPlaying(out bool playing);
                if (playing)
                    ActiveSound.set3DMinMaxDistance(_rangeMin, _rangeMax);
            }
        }
        /// <summary>
        /// The minimum range of the sound, where it begins fading till <see cref="RangeMax"/>
        /// </summary>
        public float RangeMin
        {
            get => _rangeMin;
            set
            {
                _rangeMin = value;
                ActiveSound.isPlaying(out bool playing);
                if (playing)
                    ActiveSound.set3DMinMaxDistance(_rangeMin, _rangeMax);
            }
        }
        /// <summary>
        /// The pitch of the sound, avoid setting this while playing as it will screw with audio consistancy
        /// </summary>
        public float Pitch
        {
            get => _pitch;
            set
            {
                _pitch = value;
                if (!modeFlags.HasFlag(FMOD.MODE._3D))
                {
                    ActiveSound.isPlaying(out bool playing);
                    if (playing)
                        ActiveSound.setPitch(_pitch);
                }
                else
                    RemoteUpdate();
            }
        }

        /// <summary>
        /// The position in the Scene space the sound is playing at
        /// </summary>
        public Vector3 Position
        {
            get
            {
                if (trans)
                    return trans.position;
                else
                    return pos;
            }
            set
            {
                trans = null;
                pos = value;
                if (!modeFlags.HasFlag(FMOD.MODE._3D))
                    modeFlags.SetFlags(FMOD.MODE._3D | FMOD.MODE._3D_WORLDRELATIVE | FMOD.MODE._3D_LINEARROLLOFF, true);
                ActiveSound.isPlaying(out bool playing);
                if (playing)
                {
                    FMOD.VECTOR posD = default;
                    FMOD.VECTOR posF = value.ToFMODVector();
                    Vector3 sounder = (Camera.main.transform.position - pos).normalized;
                    FMOD.VECTOR velo;
                    if (_pitch > 1)
                        velo = (sounder * ((_pitch * _pitch * speedSound) - speedSound)).ToFMODVector();
                    else
                        velo = (sounder * ((-speedSound / Mathf.Max(0.01f, _pitch)) + speedSound)).ToFMODVector();
                    ActiveSound.set3DAttributes(ref posF, ref velo, ref posD);
                }
            }
        }
        /// <summary>
        /// The transform this sound is binded to
        /// </summary>
        public Transform transform
        {
            get => trans;
            set
            {
                if (value != null)
                {
                    trans = value;
                    FMOD.VECTOR posD = default;
                    FMOD.VECTOR posF = trans.position.ToFMODVector();
                    if (!modeFlags.HasFlag(FMOD.MODE._3D))
                        modeFlags.SetFlags(FMOD.MODE._3D | FMOD.MODE._3D_WORLDRELATIVE | FMOD.MODE._3D_LINEARROLLOFF, true);
                    ActiveSound.set3DAttributes(ref posF, ref posD, ref posD);
                }
                else
                    trans = null;
            }
        }
        /// <summary>
        /// A custom function can be inserted here to position the sound automatically
        /// </summary>
        public Func<Vector3> PositionFunc
        {
            get
            {
                return calcPos;
            }
            set
            {
                calcPos = value;
                if (calcPos != null)
                {
                    trans = null;
                    pos = calcPos.Invoke();
                    if (!modeFlags.HasFlag(FMOD.MODE._3D))
                        modeFlags.SetFlags(FMOD.MODE._3D | FMOD.MODE._3D_WORLDRELATIVE | FMOD.MODE._3D_LINEARROLLOFF, true);
                    ActiveSound.isPlaying(out bool playing);
                    if (playing)
                    {
                        FMOD.VECTOR posD = default;
                        FMOD.VECTOR posF = pos.ToFMODVector();
                        Vector3 sounder = (Camera.main.transform.position - pos).normalized;
                        FMOD.VECTOR velo;
                        if (_pitch > 1)
                            velo = (sounder * ((_pitch * _pitch * speedSound) - speedSound)).ToFMODVector();
                        else
                            velo = (sounder * ((-speedSound / Mathf.Max(0.01f, _pitch)) + speedSound)).ToFMODVector();
                        ActiveSound.set3DAttributes(ref posF, ref velo, ref posD);
                    }
                }
            }
        }
        /// <summary>
        /// If the sound is to loop endlessly
        /// </summary>
        public bool Looped
        {
            get => _loop;
            set
            {
                if (value != _loop)
                {
                    _loop = value;
                    modeFlags.SetFlags(FMOD.MODE.LOOP_NORMAL, _loop);
                    ActiveSound.isPlaying(out bool playing);
                    if (playing)
                    {
                        ActiveSound.setMode(modeFlags);
                    }
                }
            }
        }


        private Func<Vector3> calcPos;
        private Transform trans;
        private Vector3 pos = Vector3.positiveInfinity;
        private float _pitch = 1;
        private float _rangeMax = 80;
        private float _rangeMin = 20;
        private float _volume = 1;
        private bool _loop = false;
        private FMOD.MODE modeFlags;

        private FMOD.Channel ActiveSound;
        private FMOD.Sound sound;
        /// <summary>
        /// The raw <see cref="FMOD.Channel"/> data for the playing sound to edit. For advanced users only!
        /// </summary>
        public FMOD.Channel ActiveSoundAdvanced => ActiveSound;
        /// <summary>
        /// The raw <see cref="FMOD.Sound"/> data to edit. For advanced users only!
        /// </summary>
        public FMOD.Sound SoundAdvanced => sound;
        private FMOD.RESULT Callback(IntPtr channelraw, FMOD.CHANNELCONTROL_TYPE controltype,
                FMOD.CHANNELCONTROL_CALLBACK_TYPE type, IntPtr commanddata1, IntPtr commanddata2)
        {
            if (controltype == FMOD.CHANNELCONTROL_TYPE.CHANNEL)
            {
                switch (type)
                {
                    case FMOD.CHANNELCONTROL_CALLBACK_TYPE.END:
                        StopPosUpdates();
                        break;
                    case FMOD.CHANNELCONTROL_CALLBACK_TYPE.VIRTUALVOICE:
                        break;
                    case FMOD.CHANNELCONTROL_CALLBACK_TYPE.SYNCPOINT:
                        break;
                    case FMOD.CHANNELCONTROL_CALLBACK_TYPE.OCCLUSION:
                        break;
                    case FMOD.CHANNELCONTROL_CALLBACK_TYPE.MAX:
                        break;
                    default:
                        break;
                }
            }
            return FMOD.RESULT.OK;
        }

        /// <summary>
        /// Create an <see cref="AudioInst"/> from an <see cref="AudioInstFile"/> saved in a mod's AssetBundle
        /// </summary>
        /// <param name="AIJ"></param>
        public AudioInst(AudioInstFile AIJ)
        {
            ManAudioExt.InsureInit();
            try
            {
                FMOD.System sys = RuntimeManager.LowlevelSystem;

                FMOD.CREATESOUNDEXINFO soundinfo = new FMOD.CREATESOUNDEXINFO();
                soundinfo.cbsize = Marshal.SizeOf(typeof(FMOD.CREATESOUNDEXINFO));
                soundinfo.length = AIJ.lengthBytes;
                soundinfo.numchannels = AIJ.channels;
                soundinfo.defaultfrequency = AIJ.frequency;
                soundinfo.decodebuffersize = (uint)AIJ.frequency;
                soundinfo.format = FMOD.SOUND_FORMAT.PCMFLOAT;
                //wavNameWithExt
                //sys.init(128, INITFLAGS.NORMAL, IntPtr.Zero);
                FMOD.RESULT result = sys.createSound(string.Empty, FMOD.MODE.OPENUSER | FMOD.MODE.ACCURATETIME | FMOD.MODE._3D,
                    ref soundinfo, out sound);
                if (result != FMOD.RESULT.OK)
                    throw new InvalidOperationException("AudioInst(AudioInstJson) - Creation failed with code " + result);
                IntPtr ptr1, ptr2;
                uint len1, len2;
                result = sound.@lock(0, AIJ.lengthBytes, out ptr1, out ptr2, out len1, out len2);
                if (result != FMOD.RESULT.OK)
                    throw new InvalidOperationException("AudioInst(AudioInstJson) - Writing failed with code " + result);
                Marshal.Copy(AIJ.data, 0, ptr1, (int)(len1 / sizeof(float)));
                if (len2 > 0)
                    Marshal.Copy(AIJ.data, (int)(len1 / sizeof(float)), ptr2, (int)(len2 / sizeof(float)));
                result = sound.unlock(ptr1, ptr2, len1, len2);
                if (result != FMOD.RESULT.OK)
                    throw new InvalidOperationException("AudioInst(AudioInstJson) - Finalization failed with code " + result);
                if (!sound.hasHandle())
                    throw new InvalidOperationException("AudioInst(AudioInstJson) - Result failed: No output");
            }
            catch (Exception e)
            {
                Debug_TTExt.Log("AudioInst(AudioClip) - Failed to convert sound - " + e);
                throw e;
            }
        }
        /// <summary>
        /// Create an <see cref="AudioInst"/> from an a path to a sound filetype on disk.
        /// <para>Ogg is preferred.</para>
        /// </summary>
        public AudioInst(string path)
        {
            ManAudioExt.InsureInit();
            try
            {
                if (File.Exists(path))
                {
                    FMOD.System sys = RuntimeManager.LowlevelSystem;
                    sound = default;
                    sys.createSound(path, FMOD.MODE.CREATESAMPLE | FMOD.MODE.ACCURATETIME | FMOD.MODE._3D, out sound);
                    sound.setLoopCount(-1);
                    sound.getLength(out uint pos, FMOD.TIMEUNIT.MS);
                    sound.setLoopPoints(0, FMOD.TIMEUNIT.MS, pos - 1, FMOD.TIMEUNIT.MS);
                    return;
                }
                Debug_TTExt.Log("AudioInst(path) - Failed to get sound from disk");
                throw new FileNotFoundException("path");
            }
            catch (Exception e)
            {
                Debug_TTExt.Log("AudioInst(path) - Failed to convert sound - " + e);
                throw e;
            }
        }
        /// <summary>
        /// Create an <see cref="AudioInst"/> using raw <see cref="FMOD.Sound"/> data
        /// </summary>
        /// <param name="Sound">The <see cref="FMOD.Sound"/> to assign to this</param>
        /// <param name="setChannel">The channel to assign to</param>
        public AudioInst(ref FMOD.Sound Sound, FMOD.Channel setChannel = default)
        {
            ManAudioExt.InsureInit();
            try
            {
                sound.setLoopCount(-1);
                sound.getLength(out uint pos, FMOD.TIMEUNIT.MS);
                sound.setLoopPoints(0, FMOD.TIMEUNIT.MS, pos - 1, FMOD.TIMEUNIT.MS);
                ActiveSound = setChannel;
                sound = Sound;
            }
            catch (Exception e)
            {
                Debug_TTExt.Log("AudioInst(path) - Failed to convert sound - " + e);
                throw e;
            }
        }

        /// <summary>
        /// Copies the sound without copying the <see cref="FMOD.Sound"/>
        /// </summary>
        /// <returns></returns>
        public AudioInst Copy()
        {
            return new AudioInst(ref sound)
            {
                pos = pos,
                modeFlags = modeFlags,
                _pitch = _pitch,
                _loop = _loop,
                _volume = _volume,
                _rangeMax = _rangeMax,
                PitchVariance = PitchVariance,
                RangeVariance = RangeVariance,
            };
        }
        /// <summary>
        /// Start managing the positional updates for dynamic sound tracking for this
        /// </summary>
        public void StartPosUpdates()
        {
            ManAudioExt.managed.Add(this);
        }
        /// <summary>
        /// Stop managing the positional updates for dynamic sound tracking for this
        /// </summary>
        public void StopPosUpdates()
        {
            ManAudioExt.managed.Remove(this);
        }
        const float speedSound = 100f;
        internal void RemoteUpdate()
        {
            if (modeFlags.HasFlag(FMOD.MODE._3D))
            {
                if (calcPos != null)
                    pos = calcPos();
                else if (trans && trans.gameObject.activeInHierarchy)
                    pos = trans.position;
                FMOD.VECTOR posD = default;
                FMOD.VECTOR posF = pos.ToFMODVector();
                Vector3 sounder = (Camera.main.transform.position - pos).normalized;
                FMOD.VECTOR velo;
                if (_pitch > 1)
                    velo = (sounder * ((_pitch * _pitch * speedSound) - speedSound)).ToFMODVector();
                else
                    velo = (sounder * ((-speedSound / Mathf.Max(0.01f, _pitch)) + speedSound)).ToFMODVector();
                ActiveSound.set3DAttributes(ref posF, ref velo, ref posD);
            }
            /*
            ActiveMusic.getPosition(out uint pos, FMOD.TIMEUNIT.MS);
            if (pos == 1)
                Play();
            else
                InvokeHelper.Invoke(WaitForEnding, 0.01f);
            */
        }
        /// <summary>
        /// Stop the sound entirely
        /// </summary>
        public void Reset()
        {
            if (IsPlaying)
            {
                ActiveSound.stop();
                ActiveSound.setPaused(false);
                ActiveSound.setPosition(0, FMOD.TIMEUNIT.MS);
                StopPosUpdates();
            }
        }
        /// <summary>
        /// Stop the sound and play it from the start immedeately
        /// </summary>
        public void PlayFromBeginning()
        {
            if (IsPlaying)
            {
                ActiveSound.stop();
                StopPosUpdates();
            }
            Play();
        }
        /// <summary>
        /// Play the sound from the beginning
        /// </summary>
        public void Play() => Play(false);
        /// <summary>
        /// Play the sound with support for multiple instances and/or a position to play at
        /// </summary>
        /// <param name="overlap"></param>
        /// <param name="scenePos"></param>
        public void Play(bool overlap, Vector3 scenePos = default)
        {
            if (IsPlaying)
                return;
            try
            {
                if (!ManPauseGame.inst.IsPaused)
                {
                    try
                    {
                        if (!sound.hasHandle())
                            throw new InvalidOperationException("Play()[hasHandle] - FAILED: handle is NULL");
                        FMOD.RESULT result;
                        FMOD.Channel AudioActive = ActiveSound;
                        result = sound.getLength(out uint timePos, FMOD.TIMEUNIT.MS);
                        if (result != FMOD.RESULT.OK)
                            throw new InvalidOperationException("Play()[getLength] - FAILED: handle is error " + result);
                        /*
                        sound.setLoopCount(-1);
                        sound.setLoopPoints(0, FMOD.TIMEUNIT.MS, pos - 1, FMOD.TIMEUNIT.MS);
                        */
                        if (overlap)
                        {
                            result = sys.playSound(sound, ManAudioExt.ModSoundGroup, true, out AudioActive);
                            if (result != FMOD.RESULT.OK)
                                throw new InvalidOperationException("Play()[playSound] - FAILED: handle is error " + result);
                        }
                        else
                        {
                            ActiveSound.setCallback(Callback);
                            result = sys.playSound(sound, ManAudioExt.ModSoundGroup, true, out ActiveSound);
                            if (result != FMOD.RESULT.OK)
                                throw new InvalidOperationException("Play()[playSound] - FAILED(2): handle is error " + result);
                            AudioActive = ActiveSound;
                        }

                        if (modeFlags.HasFlag(FMOD.MODE.OPENUSER))
                            modeFlags = FMOD.MODE.OPENUSER;
                        else
                            modeFlags = FMOD.MODE.DEFAULT;
                        if (_loop)
                        {
                            modeFlags |= FMOD.MODE.LOOP_NORMAL;
                            AudioActive.setLoopCount(-1);
                            AudioActive.setLoopPoints(1, FMOD.TIMEUNIT.MS, timePos - 2, FMOD.TIMEUNIT.MS);
                        }
                        if (trans || pos != Vector3.positiveInfinity || scenePos != default)
                        {
                            modeFlags |= FMOD.MODE._3D | FMOD.MODE._3D_WORLDRELATIVE | FMOD.MODE._3D_LINEARROLLOFF;
                            FMOD.VECTOR posD = default;
                            FMOD.VECTOR posF;
                            if (scenePos != default)
                                posF = scenePos.ToFMODVector();
                            else if (trans)
                                posF = trans.position.ToFMODVector();
                            else
                                posF = pos.ToFMODVector();
                            AudioActive.set3DLevel(1f);
                            /*
                            float[] matrix = null;
                            AudioActive.getMixMatrix(matrix, out int numchannels, out int inchannels, 0);
                            matrix[0] = 1f;
                            matrix[1] = 1f;
                            matrix[0 + 6] = 1f;
                            matrix[1 + 6] = 1f;
                            AudioActive.setMixMatrix(matrix, numchannels, inchannels, 0);
                            */
                            AudioActive.setMixLevelsOutput(1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f);
                            AudioActive.set3DAttributes(ref posF, ref posD, ref posD);
                            AudioActive.set3DConeSettings(360f, 360f, 1f);
                            AudioActive.set3DOcclusion(0f, 0f);
                            AudioActive.set3DMinMaxDistance(_rangeMin, _rangeMax + (_rangeMax * UnityEngine.Random.Range(-RangeVariance, RangeVariance)));
                        }
                        AudioActive.setMode(modeFlags);
                        AudioActive.setVolume(ManAudioExt._SFXVolume * _volume);
                        AudioActive.setPitch(_pitch + (_pitch * UnityEngine.Random.Range(-PitchVariance, PitchVariance)));
                        AudioActive.setPosition(0, FMOD.TIMEUNIT.MS);
                        result = AudioActive.setPaused(false);
                        if (result != FMOD.RESULT.OK)
                            throw new InvalidOperationException("Play()[setPaused] - FAILED: handle is error " + result);
                        if (overlap)
                            AudioActive.setPaused(false);//InvokeHelper.Invoke(StartWrapper, 0.01f, AudioActive);
                        else
                        {
                            AudioActive.setPaused(false);
                            StartPosUpdates();
                        }
                    }
                    catch (Exception e)
                    {
                        Debug_TTExt.Log("AudioInst: Failed to Play() - " + e);
                    }
                }
            }
            catch (Exception e)
            {
                Debug_TTExt.Log("AudioInst: Failed to Play()[Outer] - " + e);
            }
        }
        private void StartWrapper(FMOD.Channel inst) => inst.setPaused(false);
        /// <summary>
        /// Pause the playing sound
        /// </summary>
        public void Pause()
        {
            if (!IsPaused)
            {
                ActiveSound.setPaused(true);
                StopPosUpdates();
            }
        }
        /// <summary>
        /// Continue the sound from where it left off
        /// </summary>
        public void Resume()
        {
            if (IsPaused)
            {
                StartPosUpdates();
                ActiveSound.setPaused(false);
            }
        }
        /// <summary>
        /// Stop the sound immedeately and set it back to the start
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public void Stop()
        {
            if (IsPlaying)
            {
                ActiveSound.stop();
                ActiveSound.setPaused(false);
                ActiveSound.setPosition(0, FMOD.TIMEUNIT.MS);
                StopPosUpdates();
                if (IsPlaying)
                    throw new InvalidOperationException("We are still playing the sound even when we Stop()!");
            }
        }

    }
#endif
}
