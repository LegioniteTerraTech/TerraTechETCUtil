using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

namespace TerraTechETCUtil
{
    public interface TinySettings
    {
        string DirectoryInExtModSettings { get; }
    }
    public class LoadStaticsPls : Newtonsoft.Json.Serialization.DefaultContractResolver
    {
        private IEnumerable<MemberInfo> FilterValidMembers(PropertyInfo[] infos)
        { 
            foreach (PropertyInfo item in infos)
            {
                if (item.CanWrite && item.CanRead && !(item is object))
                    yield return item;
            }
        }
        protected override List<MemberInfo> GetSerializableMembers(Type objectType)
        {
            var baseMembers = base.GetSerializableMembers(objectType);
            var properties = objectType.GetProperties(BindingFlags.Static | BindingFlags.Public);
            baseMembers.AddRange(FilterValidMembers(properties));
            return baseMembers;
        }
    }
    public static class TinySettingsUtil
    {
        public static string ExtModSettingsDirectory => expDirect;
        private static string expDirect = Path.Combine(new DirectoryInfo(Application.dataPath).Parent.ToString(), "ExtModSettings");

        private static JsonSerializerSettings serialer = new JsonSerializerSettings() {
            ContractResolver = new LoadStaticsPls(),
        };

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
        public static bool TrySaveToDiskStatic<T>(string DirectoryInExtModSettings)
        {
            if (!Directory.Exists(expDirect))
                Directory.CreateDirectory(expDirect);
            string initDir = Path.Combine(expDirect, DirectoryInExtModSettings);
            //Debug_TTExt.Log("TinySettingsUtil[static] - Save type " + settings.GetType().ToString());
            try
            {
                File.WriteAllText(initDir, JsonConvert.SerializeObject(null, serialer));
                return true;
            }
            catch { }
            return false;
        }
        public static bool TryLoadFromDiskStatic<T>(string DirectoryInExtModSettings) where T : TinySettings
        {
            if (!Directory.Exists(expDirect))
                Directory.CreateDirectory(expDirect);
            string initDir = Path.Combine(expDirect, DirectoryInExtModSettings);
            //Debug_TTExt.Log("TinySettingsUtil[static] - Save type " + settings.GetType().ToString());
            try
            {
                if (!File.Exists(initDir))
                {
                    File.WriteAllText(initDir, JsonConvert.SerializeObject(null, serialer));
                    return false;
                }
                JsonConvert.DeserializeObject(File.ReadAllText(initDir));
                return true;
            }
            catch { }
            return false;
        }
    }
}
