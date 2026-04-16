using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

namespace TerraTechETCUtil
{
    /// <summary>
    /// A small auto-serialized settings system.
    /// <para>Adds new functions that permit easy saving and loading of settings fields</para>
    /// </summary>
    public interface ITinySettings
    {
        /// <summary>
        /// The name the TinySettings will save configuration data under
        /// </summary>
        string DirectoryInExtModSettings { get; }
    }
    /// <summary>
    /// Saves and loads static TinySettings entries 
    /// </summary>
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
        /// <summary>
        /// 
        /// </summary>
        /// <param name="objectType"></param>
        /// <returns></returns>
        protected override List<MemberInfo> GetSerializableMembers(Type objectType)
        {
            var baseMembers = base.GetSerializableMembers(objectType);
            var properties = objectType.GetProperties(BindingFlags.Static | BindingFlags.Public);
            baseMembers.AddRange(FilterValidMembers(properties));
            return baseMembers;
        }
    }
    /// <summary>
    /// Manages TinySettings
    /// </summary>
    public static class TinySettingsUtil
    {
        /// <summary>
        /// Where TinySettings configs are saved
        /// </summary>
        public static string ExtModSettingsDirectory => expDirect;
        private static string expDirect = Path.Combine(new DirectoryInfo(Application.dataPath).Parent.ToString(), "ExtModSettings");

        private static JsonSerializerSettings serialer = new JsonSerializerSettings() {
            ContractResolver = new LoadStaticsPls(),
        };

        /// <summary>
        /// Tries to save this TinySettings class to disk
        /// </summary>
        /// <typeparam name="T">The target type</typeparam>
        /// <param name="settings">The settings instance to save</param>
        /// <returns>True if it saved properly</returns>
        /// <exception cref="NullReferenceException">settings is null</exception>
        public static bool TrySaveToDisk<T>(this T settings) where T : ITinySettings
        {
            if (settings == null)
                throw new NullReferenceException("TrySaveToDisk - " + settings.GetType().ToString() + " settings is null, needs a valid instance!");
            if (!Directory.Exists(expDirect))
                Directory.CreateDirectory(expDirect);
            string initDir = Path.Combine(expDirect, settings.DirectoryInExtModSettings);
            //Debug_TTExt.Log("TinySettingsUtil - Save type " + settings.GetType().ToString());
            return ManSaveGame.SaveObject(settings, initDir);
        }

        /// <summary>
        /// Tries to load this TinySettings class from disk
        /// </summary>
        /// <typeparam name="T">The target type</typeparam>
        /// <param name="settings">The settings instance to load</param>
        /// <param name="settingsInst">The settings instance to load <b>to</b>, usually the same as <c>settings</c></param>
        /// <returns>True if it saved properly</returns>
        /// <exception cref="NullReferenceException">settings is null</exception>
        public static bool TryLoadFromDisk<T>(this T settings, ref T settingsInst) where T : ITinySettings
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

        /// <summary>
        /// Tries to save this TinySettings class to disk <b>for static data</b>
        /// </summary>
        /// <typeparam name="T">The target type</typeparam>
        /// <param name="DirectoryInExtModSettings">The name of the TinySettings file to save to.</param>
        /// <returns>True if it saved properly</returns>
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
        /// <summary>
        /// Tries to load this TinySettings class from disk <b>for static data</b>
        /// </summary>
        /// <typeparam name="T">The target type</typeparam>
        /// <param name="DirectoryInExtModSettings">The name of the TinySettings file to save to.</param>
        /// <returns>True if it saved properly</returns>
        public static bool TryLoadFromDiskStatic<T>(string DirectoryInExtModSettings) where T : ITinySettings
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
