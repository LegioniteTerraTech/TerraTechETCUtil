using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

namespace TerraTechETCUtil
{
    /// <summary>
    /// Used to store all important data of the AudioInst for accessing within the game
    /// </summary>
    [Serializable]
    public class AudioInstFile
    {
        public const string leadingFileName = "AudioInst_";

        public float[] data;
        public uint lengthBytes;
        public int channels;
        public int frequency;

        /// <summary>
        /// SERIALIZATION ONLY
        /// </summary>
        public AudioInstFile() { }

        public AudioInstFile(AudioClip AC)
        {
            try
            {
                if (AC.loadState != AudioDataLoadState.Loaded)
                    throw new InvalidOperationException("AudioInst() - loadState is not loaded: " + AC.loadState);
                if (!AC.preloadAudioData && !AC.LoadAudioData())
                    throw new InvalidOperationException("AudioInst() - LoadAudioData() failed and returned nothing");
                if (AC.channels <= 0)
                    throw new InvalidOperationException("AudioInst() - AudioClip has no channels");
                if (AC.frequency <= 0)
                    throw new InvalidOperationException("AudioInst() - AudioClip has frequency of zero");
                int arraySize = AC.samples * AC.channels;
                Debug_TTExt.Log("AudioInstJson(AudioClip) - " + AC.name + " samples: " + AC.samples + ", channels: " + AC.channels);
                data = new float[arraySize];
                if (!AC.GetData(data, 0))
                    throw new InvalidOperationException("AudioInst() - GetData() failed and returned nothing");

                lengthBytes = (uint)(arraySize * sizeof(float));
                channels = AC.channels;
                frequency = AC.frequency;
            }
            catch (Exception e)
            {
                Debug_TTExt.Log("AudioInstJson(AudioClip) - Failed to convert sound - " + e);
                throw e;
            }
        }
    }
}
