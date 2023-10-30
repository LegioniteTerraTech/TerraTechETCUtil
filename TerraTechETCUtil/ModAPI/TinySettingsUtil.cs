using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;
using UnityEngine;

namespace TerraTechETCUtil
{
    public interface TinySettings
    {
        string DirectoryInExtModSettings { get; }
    }
    public static class TinySettingsUtil
    {
        public static string ExtModSettingsDirectory => expDirect;
        private static string expDirect = Path.Combine(new DirectoryInfo(Application.dataPath).Parent.ToString(), "ExtModSettings"); 
        
        public static bool TrySaveToDisk<T>(this T settings) where T : TinySettings
        {
            if (settings == null)
                throw new NullReferenceException("TrySaveToDisk - " + settings.GetType().ToString() + " settings is null, needs a valid instance!");
            if (!Directory.Exists(expDirect))
                Directory.CreateDirectory(expDirect);
            string initDir = Path.Combine(expDirect, settings.DirectoryInExtModSettings);
            //Debug_TTExt.Log("TinySettingsUtil - Save type " + settings.GetType().ToString());
            return ManSaveGame.SaveObject(settings, initDir);
        }
        public static bool TryLoadFromDisk<T>(this T settings, ref T settingsInst) where T : TinySettings
        {
            if (settings == null)
                throw new NullReferenceException("TryLoadFromDisk - " + settings.GetType().ToString() + " settings is null, needs a valid instance!");
            string initDir = Path.Combine(expDirect, settings.DirectoryInExtModSettings);
            if (!File.Exists(initDir))
            {
                Debug_TTExt.Assert("File does not exist at " + initDir);
                return false;
            }
            //Debug_TTExt.Log("TinySettingsUtil - Load type " + settings.GetType().ToString());
            return ManSaveGame.LoadObject(ref settingsInst, initDir);
        }
    }
}
