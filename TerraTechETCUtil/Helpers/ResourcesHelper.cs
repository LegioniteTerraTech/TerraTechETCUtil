using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Text;
using UnityEngine;
using Newtonsoft.Json;
using System.Collections;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;




#if EDITOR
using UnityEditor;
using UnityEditorInternal;
#else
using FMODUnity;
using FMOD;
#endif

namespace TerraTechETCUtil
{
#if !EDITOR
    public struct ModDataHandle
    {
        public override int GetHashCode()
        {
            if (ModID == null)
                return 0;
            return ModID.GetHashCode();
        }
        public static bool operator ==(ModDataHandle script1, ModDataHandle script2)
        {
            return script1.ModID == script2.ModID;
        }
        public static bool operator !=(ModDataHandle script1, ModDataHandle script2)
        {
            return script1.ModID != script2.ModID;
        }
        public ModDataHandle(string modID)
        {
            if (!ManMods.inst.ModExists(modID))
                throw new NullReferenceException("ModResourceHandle - ModID " + modID + " does not exists");
            ModID = modID;
        }
        public readonly string ModID;

        public ModContainer GetModContainer()
        {
            return ResourcesHelper.GetModContainerFromScript(this);
        }

        public void SubToModsPreLoad(Action preEvent)
        {
            ResourcesHelper.ModsPreLoadEvent.Subscribe(preEvent);
        }
        public void SubToModsPostLoad(Action postEvent)
        {
            ResourcesHelper.ModsPostLoadEvent.Subscribe(postEvent);
        }
        public void SubToBlocksPostChange(Action postEvent)
        {
            ResourcesHelper.BlocksPostChangeEvent.Subscribe(postEvent);
        }

        public string GetModName()
        {
            return ModID;
        }
        public void DebugLogModContents()
        {
            ResourcesHelper.LookIntoModContents(GetModContainer());
        }
        public Texture2D GetModTexture(string nameNoExt)
        {
            return ResourcesHelper.GetTextureFromModAssetBundle(GetModContainer(), nameNoExt);
        }
        public T GetModObject<T>(string nameNoExt) where T : UnityEngine.Object
        {
            return ResourcesHelper.GetObjectFromModContainer<T>(GetModContainer(), nameNoExt);
        }
        public IEnumerable<T> GetModObjects<T>() where T : UnityEngine.Object
        {
            return ResourcesHelper.IterateAssetsInModContainer<T>(GetModContainer());
        }
        public IEnumerable<T> GetModObjects<T>(string nameNoExt) where T : UnityEngine.Object
        {
            return ResourcesHelper.IterateAssetsInModContainer<T>(GetModContainer(), nameNoExt);
        }
    }
    internal static class HelperHooks
    {
        internal class ManModsPatches
        {
            internal static Type target = typeof(ManMods);

            internal static void RequestReloadAllMods_Prefix()
            {
                ResourcesHelper.ModsPreLoadEvent.Send();
            }
            internal static void RequestReparseAllJsons_Prefix()
            {
                ResourcesHelper.ModsPreLoadEvent.Send();
            }
            internal static void UpdateModScripts_Postfix()
            {
                //Debug_TTExt.Log("UpdateModScripts_Postfix");
                ResourcesHelper.ModsUpdateEvent.Send();
            }
        }
    }
#endif
    public static class ResourcesHelper
    {

        public static bool ShowDebug = false;
        /// <summary>Add as a post or prefix to flag it as an additional asset item</summary>
        public const string BlockFolderJsonFlag = "%";
#if !EDITOR
        public class RelayEvent
        {
            private EventNoParams eventHook = new EventNoParams();
            public void Send()
            {
                if (startupHook)
                {
                    startupHook = false;
                    InitHooks();
                }
                eventHook.Send();
            }
            public void Subscribe(Action act)
            {
                if (startupHook)
                {
                    startupHook = false;
                    InitHooks();
                }
                eventHook.Subscribe(act);
            }
            public void Unsubscribe(Action act)
            {
                if (startupHook)
                {
                    startupHook = false;
                    InitHooks();
                }
                eventHook.Unsubscribe(act);
            }
        }

        private static Dictionary<string, ModContainer> modsEmpty = new Dictionary<string, ModContainer>();
        private static Dictionary<string, ModContainer> modsDirect = null;
        private static bool startupHook = true;
        public static RelayEvent ModsPreLoadEvent = new RelayEvent();
        public static EventNoParams BlocksPostChangeEvent => ManMods.inst.BlocksModifiedEvent;
        public static EventNoParams ModsPostLoadEvent => ManMods.inst.ModSessionLoadCompleteEvent;
        public static RelayEvent ModsUpdateEvent = new RelayEvent();

        private static void InitHooks()
        {
            LegModExt.harmonyInstance.MassPatchAllWithin(typeof(HelperHooks), "TerraTechModExt", true);
            Debug_TTExt.Log("Attached to mods updater");
        }

        private static void OnModsChanged(Mode unused)
        {
            ManGameMode.inst.ModeCleanUpEvent.Unsubscribe(OnModsChanged);
            modsDirect = modsEmpty;
        }
        public static Dictionary<string, ModContainer> GetAllMods()
        {
            if (modsDirect == null)
            {
                ManGameMode.inst.ModeCleanUpEvent.Subscribe(OnModsChanged);
                modsDirect = (Dictionary<string, ModContainer>)ModContainerGet.GetValue(ManMods.inst);
            }
            else if (modsDirect == modsEmpty)
                modsDirect = (Dictionary<string, ModContainer>)ModContainerGet.GetValue(ManMods.inst);
            
            if (modsDirect == null)
            {
                Debug_TTExt.Assert("For some reason ManMods returned a NULL mod list?  This is unexpected!");
                modsDirect = modsEmpty;
            }
            return modsDirect;
        }
        public static IEnumerable<KeyValuePair<string, ModContainer>> IterateAllMods() => GetAllMods();
#endif

        private static T AssetIterator<T>(UnityEngine.Object cand) where T : UnityEngine.Object
        {
            if (cand is T)
                return (T)cand;
            else if (cand is GameObject GO)
            {
                if (typeof(Mesh) == typeof(T))
                {
                    var mesher = GO.GetComponent<MeshFilter>();
                    if (mesher?.sharedMesh != null)
                        return mesher.sharedMesh as T;
                }
                else if (typeof(Texture2D) == typeof(T))
                {
                    var tex = GO.GetComponent<MeshRenderer>();
                    if (tex?.sharedMaterial != null)
                        return tex.sharedMaterial.mainTexture as T;
                }
            }
            return null;
        }
        private static T AssetIteratorNested<T>(UnityEngine.Object cand, Func<T, bool> searchIterator) where T : UnityEngine.Object
        {
            if (cand is T outp)
            {
                if (searchIterator(outp))
                    return (T)cand;
            }
            else if (cand is GameObject GO)
            {
                if (typeof(Mesh) == typeof(T))
                {
                    var mesher = GO.GetComponent<MeshFilter>();
                    if (mesher?.sharedMesh != null && mesher.sharedMesh is T mesh && searchIterator(mesh))
                        return mesh;
                }
                else if (typeof(Texture2D) == typeof(T))
                {
                    var tex = GO.GetComponent<MeshRenderer>();
                    if (tex?.sharedMaterial != null && tex.sharedMaterial.mainTexture is T mat && searchIterator(mat))
                        return mat;
                }
            }
            return null;
        }
        private static T AssetIteratorPrefix<T>(UnityEngine.Object cand, string nameStartsWith) where T : UnityEngine.Object
        {
            if (cand is T)
            {
                if (cand.name.StartsWith(nameStartsWith))
                    return (T)cand;
            }
            else if (cand is GameObject GO)
            {
                if (typeof(Mesh) == typeof(T))
                {
                    var mesher = GO.GetComponent<MeshFilter>();
                    if (mesher?.sharedMesh != null && cand.name.StartsWith(nameStartsWith))
                        return mesher.sharedMesh as T;
                }
                else if (typeof(Texture2D) == typeof(T))
                {
                    var tex = GO.GetComponent<MeshRenderer>();
                    if (tex?.sharedMaterial != null && cand.name.StartsWith(nameStartsWith))
                        return tex.sharedMaterial.mainTexture as T;
                }
            }
            return null;
        }
        private static T AssetIteratorPostfix<T>(UnityEngine.Object cand, string nameEndsWith) where T : UnityEngine.Object
        {
            if (cand is T)
            {
                if (cand.name.EndsWith(nameEndsWith))
                    return (T)cand;
            }
            else if (cand is GameObject GO)
            {
                if (typeof(Mesh) == typeof(T))
                {
                    var mesher = GO.GetComponent<MeshFilter>();
                    if (mesher?.sharedMesh != null && cand.name.EndsWith(nameEndsWith))
                        return mesher.sharedMesh as T;
                }
                else if (typeof(Texture2D) == typeof(T))
                {
                    var tex = GO.GetComponent<MeshRenderer>();
                    if (tex?.sharedMaterial != null && cand.name.EndsWith(nameEndsWith))
                        return tex.sharedMaterial.mainTexture as T;
                }
            }
            return null;
        }
        private static T AssetIteratorPostfix<T>(UnityEngine.Object cand, string nameEndsWith, Func<T, bool> searchIterator) where T : UnityEngine.Object
        {
            if (cand is T cand2)
            {
                if (cand.name.EndsWith(nameEndsWith) && searchIterator(cand2))
                    return cand2;
            }
            else if (cand is GameObject GO)
            {
                if (typeof(Mesh) == typeof(T))
                {
                    var mesher = GO.GetComponent<MeshFilter>();
                    if (mesher?.sharedMesh != null && mesher.sharedMesh is T cand3 && 
                        cand.name.EndsWith(nameEndsWith) && searchIterator(cand3))
                        return cand3;
                }
                else if (typeof(Texture2D) == typeof(T))
                {
                    var tex = GO.GetComponent<MeshRenderer>();
                    if (tex?.sharedMaterial != null && tex.sharedMaterial is T cand3 && 
                        cand.name.EndsWith(nameEndsWith) && searchIterator(cand3))
                        return cand3;
                }
            }
            return null;
        }


#if !EDITOR
        public static IEnumerable<KeyValuePair<ModDataHandle, T>> IterateAllModAssetsBundle<T>()
            where T : UnityEngine.Object
        {
            StopInvalidTypes<T>();
            foreach (var item in GetAllMods())
            {
                foreach (var TA in IterateAssetsInModContainer<T>(item.Value))
                {
                    yield return new KeyValuePair<ModDataHandle, T>(new ModDataHandle(item.Key), TA);
                }
            }
        }
        public static IEnumerable<KeyValuePair<ModDataHandle, T>> IterateAllModAssetsBundle<T>(string nameEndsWith) 
            where T : UnityEngine.Object
        {
            StopInvalidTypes<T>();
            foreach (var item in GetAllMods())
            {
                foreach (var TA in IterateAssetsInModContainerPostfix<T>(item.Value, nameEndsWith))
                {
                    yield return new KeyValuePair<ModDataHandle, T>(new ModDataHandle(item.Key), TA);
                }
            }
        }
        public static IEnumerable<KeyValuePair<ModDataHandle, T>> IterateAllModAssetsBundle<T>(Func<T, bool> searchIterator)
            where T : UnityEngine.Object
        {
            StopInvalidTypes<T>();
            foreach (var item in GetAllMods())
            {
                foreach (var TA in IterateAssetsInModContainer(item.Value, searchIterator))
                {
                    yield return new KeyValuePair<ModDataHandle, T>(new ModDataHandle(item.Key), TA);
                }
            }
        }
        public static IEnumerable<KeyValuePair<ModDataHandle, T>> IterateAllModAssetsBundle<T>(string nameEndsWith, Func<T, bool> searchIterator)
            where T : UnityEngine.Object
        {
            StopInvalidTypes<T>();
            foreach (var item in GetAllMods())
            {
                foreach (var TA in IterateAssetsInModContainerPostfix(item.Value, nameEndsWith, searchIterator))
                {
                    yield return new KeyValuePair<ModDataHandle, T>(new ModDataHandle(item.Key), TA);
                }
            }
        }
        public struct ModExtPath
        {
            public ModDataHandle Mod;
            public string LocationDisk;
        }
        public static IEnumerable<ModExtPath> IterateAllModAssetsExternal(string fileExtension)
        {
            foreach (var item in GetAllMods())
            {
                foreach (var item2 in item.Value.AssetBundlePath)
                {
                    foreach (var item3 in new DirectoryInfo(item.Value.AssetBundlePath).Parent.EnumerateFiles())
                    {
                        if (item3.Extension == fileExtension)
                        {
                            yield return new ModExtPath
                            {
                                Mod = new ModDataHandle(item.Key),
                                LocationDisk = item3.FullName,
                            };
                        }
                    }
                }
            }
        }
        public static IEnumerable<ModExtPath> IterateAllModAssetsExternal(string fileExtension, string namePostfix)
        {
            foreach (var item in GetAllMods())
            {
                foreach (var item2 in item.Value.AssetBundlePath)
                {
                    foreach (var item3 in new DirectoryInfo(item.Value.AssetBundlePath).Parent.EnumerateFiles())
                    {
                        if (item3.Extension == fileExtension && item3.Name.EndsWith(namePostfix))
                        {
                            yield return new ModExtPath
                            {
                                Mod = new ModDataHandle(item.Key),
                                LocationDisk = item3.FullName,
                            };
                        }
                    }
                }
            }
        }


        public static ModContainer GetModContainerFromScript(ModDataHandle script)
        {
            if (script.ModID.NullOrEmpty())
                throw new NullReferenceException("GetModContainerFromScript was given NULL ModDataHandle.  " +
                    "Make sure you have a ModDataHandle with a ModID that is valid!");
            if (GetAllMods().TryGetValue(script.ModID, out var item))
            {
                return item;
            }
            throw new NullReferenceException("GetModContainerFromScript could not find the mod with the given script");
        }
        public static bool TryGetModContainerFromScript(ModDataHandle script, out ModContainer MC)
        {
            if (script.ModID.NullOrEmpty())
                throw new NullReferenceException("GetModContainerFromScript was given NULL ModDataHandle.  " +
                    "Make sure you have a ModDataHandle with a ModID that is valid!");
            return GetAllMods().TryGetValue(script.ModID, out MC);
        }
        public static bool TryGetModContainer(string modName, out ModContainer MC)
        {
            if (modName.NullOrEmpty())
                throw new NullReferenceException("TryGetModContainer was given NULL modName.  " +
                    "Make sure you have a modName that is valid!");
            MC = ManMods.inst.FindMod(modName);
            return MC != null;
        }
        public static ModContainer GetModContainer(string modName, out ModContainer MC)
        {
            if (modName.NullOrEmpty())
                throw new NullReferenceException("GetModContainer was given NULL modName.  " +
                    "Make sure you have a modName that is valid!");
            MC = ManMods.inst.FindMod(modName);
            if (MC == null)
            {
                StringBuilder SB = new StringBuilder();
                SB.AppendLine(">- " + modName);
                foreach (var item in GetAllMods().Values)
                {
                    SB.AppendLine(" - " + item.ModID);
                }
                throw new NullReferenceException("GetModContainer FAILED to get mod container: \n" + SB.ToString());
            }
            return MC;
        }
        public static void LookIntoModContents(ModContainer MC)
        {
            if (MC == null)
                throw new NullReferenceException("LookIntoModContents was given NULL ModContainer.  " +
                    "Make sure you have a ModContainer that is valid!");
            try
            {
                Debug_TTExt.Log("----- CHECKING IN " + MC.ModID + " -----");
                FieldInfo FI = typeof(ModContainer).GetField("m_AssetLookup", BindingFlags.NonPublic | BindingFlags.Instance);
                Dictionary<string, ModdedAsset> content = (Dictionary<string, ModdedAsset>)FI.GetValue(MC);
                if (content != null)
                {
                    Debug_TTExt.Log("----- GETTING ALL CONTENT KEYS -----");
                    foreach (var item in content)
                    {
                        Debug_TTExt.Log(item.Key + " | " + item.Value.GetType());
                    }
                }
                if (MC.Contents.m_AdditionalAssets != null)
                {
                    Debug_TTExt.Log("----- GETTING ALL EXTRA CONTENT KEYS -----");
                    foreach (var item in MC.Contents.m_AdditionalAssets)
                    {
                        Debug_TTExt.Log(item.name + " | " + item.GetType());
                    }
                }
                Debug_TTExt.Log("-------- END --------");
            }
            catch (Exception e)
            {
                Debug_TTExt.Log("LookIntoModContents ~ Error on checking ModContainer - " + e);
            }
        }
#endif

        public static void StopInvalidTypes<T>()
        {
            if (typeof(T) == typeof(AudioClip))
                throw new InvalidOperationException("ResourcesHelper does not support AudioClips.  " +
                    "This is because FMOD disables loading them entirely.  Use AudioInstJson instead.");
        }

#if !EDITOR
        public static T GetObjectFromModContainer<T>(this ModContainer MC, Func<T, bool> searchIterator) where T : UnityEngine.Object
        {
            StopInvalidTypes<T>();
            foreach (var cand in MC.Contents.m_AdditionalAssets)
            {
                var result = AssetIteratorNested(cand, searchIterator);
                if (result != null)
                    return result;
            }
            return null;
        }
        public static T GetObjectFromModContainer<T>(this ModContainer MC, string nameNoExt) where T : UnityEngine.Object
        {
            StopInvalidTypes<T>();
            return (T)MC.Contents.m_AdditionalAssets.Find(delegate (UnityEngine.Object cand)
            { return cand is T && cand.name.Equals(nameNoExt); });
        }
        public static IEnumerable<T> IterateAssetsInModContainer<T>(this ModContainer MC, Func<T, bool> searchIterator) where T : UnityEngine.Object
        {
            StopInvalidTypes<T>();
            foreach (var cand in MC.Contents.m_AdditionalAssets)
            {
                var result = AssetIteratorNested(cand, searchIterator);
                if (result != null)
                    yield return result;
            }
        }
        public static IEnumerable<T> IterateAssetsInModContainer<T>(this ModContainer MC) where T : UnityEngine.Object
        {
            StopInvalidTypes<T>();
            foreach (var cand in MC.Contents.m_AdditionalAssets)
            {
                var result = AssetIterator<T>(cand);
                if (result != null)
                    yield return result;
            }
        }
        public static IEnumerable<T> IterateAssetsInModContainer<T>(this ModContainer MC, string nameStartsWith) where T : UnityEngine.Object
        {
            StopInvalidTypes<T>();
            foreach (var cand in MC.Contents.m_AdditionalAssets)
            {
                var result = AssetIteratorPrefix<T>(cand, nameStartsWith);
                if (result != null)
                    yield return result;
            }
        }
        public static IEnumerable<T> IterateAssetsInModContainerPostfix<T>(this ModContainer MC, string nameEndsWith) where T : UnityEngine.Object
        {
            StopInvalidTypes<T>();
            foreach (var cand in MC.Contents.m_AdditionalAssets)
            {
                var result = AssetIteratorPostfix<T>(cand, nameEndsWith);
                if (result != null)
                    yield return result;
            }
        }
        public static IEnumerable<T> IterateAssetsInModContainerPostfix<T>(this ModContainer MC, string nameEndsWith, Func<T, bool> searchIterator) where T : UnityEngine.Object
        {
            StopInvalidTypes<T>();
            foreach (var cand in MC.Contents.m_AdditionalAssets)
            {
                var result = AssetIteratorPostfix(cand, nameEndsWith, searchIterator);
                if (result != null)
                    yield return result;
            }
        }
#else

#endif


#if !EDITOR
        /// <summary>
        /// Make sure the Mod AssetBundle is loaded first!
        /// </summary>
        public static Mesh GetMeshFromModAssetBundle(this ModContainer MC, string nameNoExt, bool complainWhenFail = true)
        {
            Mesh mesh = null;
            UnityEngine.Object obj = MC.Contents.m_AdditionalAssets.Find(delegate (UnityEngine.Object cand)
            { return cand.name.Equals(nameNoExt); });
            if (obj is Mesh mesh1)
                mesh = mesh1;
            else if (obj is GameObject objGO)
                mesh = objGO.GetComponentInChildren<MeshFilter>(true).sharedMesh;
            if (complainWhenFail)
                Debug_TTExt.Assert(mesh == null, nameNoExt + ".obj could not be found!");
            return mesh;
        }
        private static string tempDirect = new DirectoryInfo(Application.dataPath).Parent.ToString() + "\\testIcons";

        /// <summary>
        /// Make sure the Mod AssetBundle is loaded first!
        /// </summary>
        public static string GetTextFromModAssetBundle(this ModContainer MC, string nameNoExt, bool complainWhenFail = true)
        {
            TextAsset TA = null;
            string tex = string.Empty;
            UnityEngine.Object obj = MC.Contents.m_AdditionalAssets.Find(delegate (UnityEngine.Object cand)
            { return cand.name.Equals(nameNoExt); });
            if (obj is TextAsset tex1)
                TA = tex1;
            else if (obj is GameObject objGO)
                TA = objGO.GetComponentInChildren<TextAsset>(true);

            if (TA != null)
            {
                tex = TA.text;
                if (tex == null || !tex.Any())
                    tex = Encoding.UTF8.GetString(TA.bytes);
                if ((tex == null || !tex.Any()) && complainWhenFail)
                    Debug_TTExt.Assert(tex == null, nameNoExt + ".txt could not be found!");
                return tex;
            }
            if (complainWhenFail)
                Debug_TTExt.Assert(tex == null, nameNoExt + ".txt could not be found!");
            return null;
        }
        /// <summary>
        /// Make sure the Mod AssetBundle is loaded first!
        /// </summary>
        public static byte[] GetBinaryFromModAssetBundle(this ModContainer MC, string nameNoExt, bool complainWhenFail = true)
        {
            TextAsset TA = null;
            byte[] tex = null;
            UnityEngine.Object obj = MC.Contents.m_AdditionalAssets.Find(delegate (UnityEngine.Object cand)
            { return cand.name.Equals(nameNoExt); });
            if (obj is TextAsset tex1)
                TA = tex1;
            else if (obj is GameObject objGO)
                TA = objGO.GetComponentInChildren<TextAsset>(true);

            if (TA != null)
            {
                tex = TA.bytes;
                if ((tex == null || !tex.Any()) && complainWhenFail)
                    Debug_TTExt.Assert(tex == null, nameNoExt + ".txt could not be found!");
                return tex;
            }
            if (complainWhenFail)
                Debug_TTExt.Assert(tex == null, nameNoExt + ".txt could not be found!");
            return null;
        }
        /// <summary>
        /// Make sure the Mod AssetBundle is loaded first!
        /// </summary>
        public static AudioInst GetAudioFromModAssetBundle(this ModContainer MC, string nameNoExt, bool complainWhenFail = true)
        {
            string name = AudioInstFile.leadingFileName + nameNoExt.Replace(AudioInstFile.leadingFileName, string.Empty);
            byte[] data = GetBinaryFromModAssetBundle(MC, name, complainWhenFail);
            if (data != null)
            {
                using (MemoryStream MS = new MemoryStream(data))
                {
                    AudioInstFile AC = (AudioInstFile)new BinaryFormatter().Deserialize(MS);
                    if (AC != null)
                        return new AudioInst(AC);
                }
            }
            return null;
        }
        /// <summary>
        /// Make sure the Mod AssetBundle is loaded first!
        /// </summary>
        public static AudioInst GetAudioFromModAssetBundleCached(this ModContainer MC, string nameNoExt, bool complainWhenFail = true)
        {
            string name = AudioInstFile.leadingFileName + nameNoExt.Replace(AudioInstFile.leadingFileName, string.Empty);
            if (MC != null && ManAudioExt.AllSounds.TryGetValue(MC, out var group) &&
                group != null && group.TryGetValue(name + ".wav", out var group2))
                return group2.main[0];
            return GetAudioFromModAssetBundle(MC, name, complainWhenFail);
        }
        /// <summary>
        /// Make sure the Mod AssetBundle is loaded first!
        /// </summary>
        public static Texture2D GetTextureFromModAssetBundle(this ModContainer MC, string nameNoExt, bool complainWhenFail = true, bool testIconsFolder = false)
        {
            Texture2D tex = null;
            UnityEngine.Object obj = MC.Contents.m_AdditionalAssets.Find(delegate (UnityEngine.Object cand)
            { return cand.name.Equals(nameNoExt); });
            if (obj is Texture2D tex1)
                tex = tex1;
            else if (obj is GameObject objGO)
                tex = (Texture2D)objGO.GetComponentInChildren<MeshRenderer>(true).sharedMaterial.mainTexture;
            if (tex == null && testIconsFolder)
            {
                if (!Directory.Exists(tempDirect))
                    Directory.CreateDirectory(tempDirect);
                string targ = Path.Combine(tempDirect, nameNoExt + ".png");
                if (File.Exists(targ))
                    tex = FileUtils.LoadTexture(targ);
            }
            if (complainWhenFail)
                Debug_TTExt.Assert(tex == null, nameNoExt + ".png could not be found!");
            return tex;
        }
        /// <summary>
        /// Make sure the Mod AssetBundle is loaded first!
        /// </summary>
        public static Material GetMaterialFromModAssetBundle(this ModContainer MC, string nameNoExt, bool complainWhenFail = true, bool testIconsFolder = false)
        {
            Texture2D tex = null;
            UnityEngine.Object obj = MC.Contents.m_AdditionalAssets.Find(delegate (UnityEngine.Object cand)
            { return cand.name.Equals(nameNoExt); });
            if (obj is Texture2D tex1)
                tex = tex1;
            else if (obj is GameObject objGO)
                tex = (Texture2D)objGO.GetComponentInChildren<MeshRenderer>(true).sharedMaterial.mainTexture;
            if (tex == null && testIconsFolder)
            {
                if (!Directory.Exists(tempDirect))
                    Directory.CreateDirectory(tempDirect);
                string targ = Path.Combine(tempDirect, nameNoExt + ".png");
                if (File.Exists(targ))
                    tex = FileUtils.LoadTexture(targ);
            }
            if (complainWhenFail)
                Debug_TTExt.Assert(tex == null, nameNoExt + ".png could not be found!");
            if (tex != null)
            {
                var testMaterial = new Material(GetMaterialFromBaseGameActive("GSO_Main"));
                testMaterial.SetTexture("_MainTex", tex);
                return testMaterial;
            }
            return null;
        }

#else
        /// <summary>
        /// Make sure the Mod AssetBundle is loaded first!
        /// </summary>
        public static Mesh GetMeshFromModAssetBundle(string nameNoExt, bool complainWhenFail = true)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:mesh"))
            {
                string aPath = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(aPath) == nameNoExt)
                {
                    Mesh obj = AssetDatabase.LoadAssetAtPath<Mesh>(aPath);
                    if (obj)
                        return obj;
                }
            }
            if (complainWhenFail)
                throw new NullReferenceException("Mesh \"" + nameNoExt + "\" does not exists");
            return null;
        }
        private static string tempDirect = new DirectoryInfo(Application.dataPath).Parent.ToString() + "\\testIcons";
        /// <summary>
        /// Make sure the Mod AssetBundle is loaded first!
        /// </summary>
        public static Texture2D GetTextureFromModAssetBundle(string nameStart, bool complainWhenFail = true)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:texture2D"))
            {
                string aPath = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(aPath).StartsWith(nameStart))
                {
                    Texture2D obj = AssetDatabase.LoadAssetAtPath<Texture2D>(aPath);
                    if (obj)
                        return obj;
                }
            }
            if (complainWhenFail)
                throw new NullReferenceException("Mesh \"" + nameStart + "\" does not exists");
            return null;
        }

#endif



#if !EDITOR

        private static HashSet<string> names = new HashSet<string>();
        public static void LogObjectsFromBaseGameAll<T>() where T : UnityEngine.Object
        {
            StopInvalidTypes<T>();
            try
            {
                Debug_TTExt.Log(typeof(T).Name + " - Results are:");
                foreach (var item in Resources.FindObjectsOfTypeAll<T>())
                {
                    if (item.IsNull() || item.name.NullOrEmpty())
                        continue;
                    if (names.Add(item.name))
                    {
                        Debug_TTExt.Log("- " + item.name);
                        continue;
                    }
                    int stepName = 0;
                    while (true)
                    {
                        stepName++;
                        string nameCase = item.name + "(" + stepName + ")";
                        if (names.Add(nameCase))
                        {
                            Debug_TTExt.Log("- " + nameCase);
                            break;
                        }
                    }
                }
            }
            finally
            {
                names.Clear();
            }
        }
        public static T GetObjectFromBaseGameActive<T>(string objectName, bool complainWhenFail = true) 
            where T : UnityEngine.Object
        {
            StopInvalidTypes<T>();
            var res = UnityEngine.Object.FindObjectsOfType<T>();
            T mat = res.FirstOrDefault(delegate (T cand) { return cand.name.Equals(objectName); });
            if (mat == null)
                Debug_TTExt.Assert(complainWhenFail, objectName + " " + typeof(T).Name + " in base game could not be found!");
            return mat;
        }
        public static T GetObjectFromBaseGameAllFast<T>(string objectName, bool complainWhenFail = true) 
            where T : UnityEngine.Object
        {
            StopInvalidTypes<T>();
            try
            {
                foreach (var item in Resources.FindObjectsOfTypeAll<T>())
                {
                    if (item.IsNotNull() && !item.name.NullOrEmpty() && 
                        names.Add(item.name) && objectName.Equals(item.name))
                        return item;
                }
                Debug_TTExt.Assert(complainWhenFail, objectName + " " + typeof(T).Name + " in base game could not be found!");
                return null;
            }
            finally
            {
                names.Clear();
            }
        }
        public static T GetObjectFromBaseGameAllDeep<T>(string objectName, bool complainWhenFail = true) where T : UnityEngine.Object
        {
            StopInvalidTypes<T>();
            try
            {
                foreach (var item in Resources.FindObjectsOfTypeAll<T>())
                {
                    if (item.IsNotNull() && !item.name.NullOrEmpty())
                    {
                        if (names.Add(item.name) && objectName.Equals(item.name))
                            return item;
                        int stepName = 0;
                        while (true)
                        {
                            stepName++;
                            string nameCase = item.name + "(" + stepName + ")";
                            if (names.Add(nameCase))
                            {
                                if (objectName.Equals(item.name))
                                    return item;
                                break;
                            }
                        }
                    }
                }
                Debug_TTExt.Assert(complainWhenFail, objectName + " " + typeof(T).Name + " in base game could not be found!");
                return null;
            }
            finally
            {
                names.Clear();
            }
        }

        public static Texture2D GetTexture2DFromBaseGameActive(string MaterialName, bool complainWhenFail = true) =>
            GetObjectFromBaseGameActive<Texture2D>(MaterialName, complainWhenFail);
        public static Texture2D GetTexture2DFromBaseGameAllFast(string MaterialName, bool complainWhenFail = true) =>
            GetObjectFromBaseGameAllFast<Texture2D>(MaterialName, complainWhenFail);
        public static Texture2D GetTexture2DFromBaseGameAllDeep(string MaterialName, bool complainWhenFail = true) =>
            GetObjectFromBaseGameAllDeep<Texture2D>(MaterialName, complainWhenFail);


        public static Material GetMaterialFromBaseGameActive(string MaterialName, bool complainWhenFail = true) =>
            GetObjectFromBaseGameActive<Material>(MaterialName, complainWhenFail);
        public static Material GetMaterialFromBaseGameAllFast(string MaterialName, bool complainWhenFail = true) =>
            GetObjectFromBaseGameAllFast<Material>(MaterialName, complainWhenFail);
        public static Material GetMaterialFromBaseGameAllDeep(string MaterialName, bool complainWhenFail = true) =>
            GetObjectFromBaseGameAllDeep<Material>(MaterialName, complainWhenFail);

        public static AsyncLoader<T> GetResourceFromBaseGamePreciseAsync<T>(string ResourceName, Action<T> callback, int processIterations = 16) where T : UnityEngine.Object
        {
            StopInvalidTypes<T>();
            return new AsyncLoader<T>(Resources.FindObjectsOfTypeAll<T>(), ResourceName, callback, processIterations);
        }
        public static AsyncLoaderUnstable GetResourceFromBaseGamePreciseAsync(Type type, string ResourceName, Action<UnityEngine.Object> callback, int processIterations = 16)
        {
            return new AsyncLoaderUnstable(Resources.FindObjectsOfTypeAll(type), ResourceName, callback, processIterations);
        }

        public static Sprite ConvertToSprite(this Texture2D texture)
        {
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero, 1, 0, SpriteMeshType.FullRect);
        }
        public static Sprite ConvertToSprite(this Texture2D texture, Sprite refSprite)
        {
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero, refSprite.pixelsPerUnit, 0, SpriteMeshType.FullRect, refSprite.border);
        }

        public static byte[] FetchBinaryData(ModContainer MC, string textNameWithExt, string DLLDirectory = null)
        {
            byte[] tex = null;
            try
            {
                //ResourcesHelper.LookIntoModContents(MC);
                if (MC != null)
                    tex = GetBinaryFromModAssetBundle(MC, Path.GetFileNameWithoutExtension(textNameWithExt), ShowDebug);
                else if (ShowDebug)
                    Debug_TTExt.Log("ModContainer for " + MC.ModID + " DOES NOT EXIST");
                if (tex == null)
                {
                    if (ShowDebug)
                        Debug_TTExt.Log("Binary " + Path.GetFileNameWithoutExtension(textNameWithExt) + " did not exist in AssetBundle, using external...");
                    if (DLLDirectory == null)
                        DLLDirectory = new DirectoryInfo(MC.AssetBundlePath).Parent.ToString();
                    string destination = Path.Combine(DLLDirectory, textNameWithExt);
                    if (File.Exists(destination))
                        tex = File.ReadAllBytes(destination);
                }
                if (tex != null)
                    return tex;
            }
            catch { }
            if (ShowDebug)
                Debug_TTExt.Log("Could not load Binary " + textNameWithExt + "!  \n   File is missing!");
            return null;
        }
        public static string FetchTextData(ModContainer MC, string textNameWithExt, string DLLDirectory = null)
        {
            string tex = null;
            try
            {
                //ResourcesHelper.LookIntoModContents(MC);
                if (MC != null)
                    tex = GetTextFromModAssetBundle(MC, Path.GetFileNameWithoutExtension(textNameWithExt), ShowDebug);
                else if (ShowDebug)
                    Debug_TTExt.Log("ModContainer for " + MC.ModID + " DOES NOT EXIST");
                if (tex == null)
                {
                    if (ShowDebug)
                        Debug_TTExt.Log("Text " + Path.GetFileNameWithoutExtension(textNameWithExt) + " did not exist in AssetBundle, using external...");
                    if (DLLDirectory == null)
                        DLLDirectory = new DirectoryInfo(MC.AssetBundlePath).Parent.ToString();
                    string destination = Path.Combine(DLLDirectory, textNameWithExt);
                    if (File.Exists(destination))
                        tex = File.ReadAllText(destination);
                }
                if (tex != null)
                    return tex;
            }
            catch { }
            if (ShowDebug)
                Debug_TTExt.Log("Could not load Text " + textNameWithExt + "!  \n   File is missing!");
            return string.Empty;
        }
        public static Texture2D FetchTexture(ModContainer MC, string pngName, string DLLDirectory = null)
        {
            Texture2D tex = null;
            try
            {
                //ResourcesHelper.LookIntoModContents(MC);
                if (MC != null)
                    tex = GetTextureFromModAssetBundle(MC, pngName.Replace(".png", ""), ShowDebug);
                else if (ShowDebug)
                    Debug_TTExt.Log("ModContainer for " + MC.ModID + " DOES NOT EXIST");
                if (!tex)
                {
                    if (ShowDebug)
                        Debug_TTExt.Log("Texture2D " + pngName.Replace(".png", "") + " did not exist in AssetBundle, using external...");
                    if (DLLDirectory == null)
                        DLLDirectory = new DirectoryInfo(MC.AssetBundlePath).Parent.ToString();
                    string destination = Path.Combine(DLLDirectory , pngName);
                    if (File.Exists(destination))
                        tex = FileUtils.LoadTexture(destination);
                }
                if (tex)
                    return tex;
            }
            catch { }
            if (ShowDebug)
                Debug_TTExt.Log("Could not load Texture2D " + pngName + "!  \n   File is missing!");
            return null;
        }
        /// <summary>
        /// Searches in the respective ModContainer.  
        ///   If that fails to find anything then it looks in the mod's directory
        /// </summary>
        /// <param name="MC">Mod to search</param>
        /// <param name="wavNameWithExt">The name of the sound</param>
        /// <param name="DLLDirectory">The fallback location to find the sound in. 
        /// Leave null to use the ModContainer's DLL</param>
        /// <returns></returns>
        public static AudioInst FetchSound(ModContainer MC, string wavNameWithExt, string DLLDirectory = null)
        {
            if (MC != null && ManAudioExt.AllSounds.TryGetValue(MC, out var group) &&
                group != null && group.TryGetValue(wavNameWithExt, out var group2))
                return group2.main[0];
            return FetchSoundDirect(wavNameWithExt, DLLDirectory, MC);
        }
        public static AudioInst FetchSoundDirect(string wavNameWithExt, string DLLDirectory, ModContainer MC = null)
        {
            if (MC != null)
            {
                AudioInst inst = GetAudioFromModAssetBundle(MC, Path.GetFileNameWithoutExtension(wavNameWithExt), ShowDebug);
                if (inst != null)
                    return inst;
                if (DLLDirectory == null)
                    DLLDirectory = new DirectoryInfo(MC.AssetBundlePath).Parent.ToString();
            }
            string GO = Path.Combine(DLLDirectory, wavNameWithExt);
            if (File.Exists(GO))
                return new AudioInst(GO);
            if (ShowDebug)
            {
                Debug_TTExt.Log("Could not load wav " + wavNameWithExt + "!  \n   File is missing!");
                Debug_TTExt.Log("The files exist:");
                foreach (var item in Directory.EnumerateFiles(DLLDirectory))
                {
                    Debug_TTExt.Log(Path.GetFileName(item));
                }
            }
            return default;
        }
        private static AudioClip ACPrev;
        internal static void PlayLastCached()
        {
            AudioClipToFMODSound(ACPrev, "", out var channel);
        }

        internal static float[] sampleCache;
        /// <summary>
        /// Source for the AudioClip to FMOD.Sound: https://qa.fmod.com/t/load-an-audioclip-as-fmod-sound/11741/2
        /// </summary>
        internal static FMOD.Sound AudioClipToFMODSound(AudioClip AC, string wavNameWithExt, out Channel channelSet)
        {
            try
            {
                AC.LoadAudioData();
                ACPrev = AC;
                if (AC.channels <= 0)
                    throw new InvalidOperationException("AudioClipToFMODSound() - AudioClip has no channels");
                if (AC.frequency <= 0)
                    throw new InvalidOperationException("AudioClipToFMODSound() - AudioClip has frequency of zero");
                int arraySize = AC.samples * AC.channels;
                var samplesCache = new float[arraySize];
                if (!AC.GetData(samplesCache, 0))
                    throw new InvalidOperationException("AudioClipToFMODSound() - GetData failed and returned nothing");

                FMOD.System sys = RuntimeManager.LowlevelSystem;

                uint lenbytes = (uint)(arraySize * sizeof(float));

                FMOD.CREATESOUNDEXINFO soundinfo = new FMOD.CREATESOUNDEXINFO();
                soundinfo.cbsize = Marshal.SizeOf(typeof(FMOD.CREATESOUNDEXINFO));
                soundinfo.length = lenbytes;
                soundinfo.numchannels = AC.channels;
                soundinfo.defaultfrequency = AC.frequency;
                soundinfo.decodebuffersize = (uint)AC.frequency;
                soundinfo.format = FMOD.SOUND_FORMAT.PCMFLOAT;
                //wavNameWithExt
                    //sys.init(128, INITFLAGS.NORMAL, IntPtr.Zero);
                FMOD.RESULT result = sys.createSound(string.Empty, FMOD.MODE.OPENUSER | FMOD.MODE.ACCURATETIME | FMOD.MODE._3D, 
                    ref soundinfo, out FMOD.Sound newSound);
                if (result != FMOD.RESULT.OK)
                    throw new InvalidOperationException("AudioClipToFMODSound() - Creation failed with code " + result);
                IntPtr ptr1, ptr2;
                uint len1, len2;
                result = newSound.@lock(0, lenbytes, out ptr1, out ptr2, out len1, out len2);
                if (result != FMOD.RESULT.OK)
                    throw new InvalidOperationException("AudioClipToFMODSound() - Writing failed with code " + result);
                Marshal.Copy(samplesCache, 0, ptr1, (int)(len1 / sizeof(float)));
                if (len2 > 0)
                    Marshal.Copy(samplesCache, (int)(len1 / sizeof(float)), ptr2, (int)(len2 / sizeof(float)));
                result = newSound.unlock(ptr1, ptr2, len1, len2);
                if (result != FMOD.RESULT.OK)
                    throw new InvalidOperationException("AudioClipToFMODSound() - Finalization failed with code " + result);
                if (!newSound.hasHandle())
                    throw new InvalidOperationException("AudioClipToFMODSound() - Result failed: No output");
                sys.playSound(newSound, ManAudioExt.ModSoundGroup, false, out channelSet);
                channelSet.setPaused(false);
                return newSound;
            }
            catch (Exception e)
            {
                Debug_TTExt.Log("AudioClipToFMODSound() - Failed to convert sound - " + e);
                throw e;
            }
        }
        internal static void SetupAudioClipToFMODSound(AudioClip AC, ref FMOD.Sound newSound)
        {
            try
            {
                AC.LoadAudioData();
                ACPrev = AC;
                if (AC.channels <= 0)
                    throw new InvalidOperationException("AudioClipToFMODSound() - AudioClip has no channels");
                if (AC.frequency <= 0)
                    throw new InvalidOperationException("AudioClipToFMODSound() - AudioClip has frequency of zero");
                int arraySize = AC.samples * AC.channels;
                var samplesCache = new float[arraySize];
                if (!AC.GetData(samplesCache, 0))
                    throw new InvalidOperationException("AudioClipToFMODSound() - GetData failed and returned nothing");

                FMOD.System sys = RuntimeManager.LowlevelSystem;

                uint lenbytes = (uint)(arraySize * sizeof(float));

                IntPtr ptr1, ptr2;
                uint len1, len2;
                FMOD.RESULT result = newSound.@lock(0, lenbytes, out ptr1, out ptr2, out len1, out len2);
                if (result != FMOD.RESULT.OK)
                    throw new InvalidOperationException("AudioClipToFMODSound() - Writing failed with code " + result);
                Marshal.Copy(samplesCache, 0, ptr1, (int)(len1 / sizeof(float)));
                if (len2 > 0)
                    Marshal.Copy(samplesCache, (int)(len1 / sizeof(float)), ptr2, (int)(len2 / sizeof(float)));
                result = newSound.unlock(ptr1, ptr2, len1, len2);
                if (result != FMOD.RESULT.OK)
                    throw new InvalidOperationException("AudioClipToFMODSound() - Finalization failed with code " + result);
                if (!newSound.hasHandle())
                    throw new InvalidOperationException("AudioClipToFMODSound() - Result failed: No output");
            }
            catch (Exception e)
            {
                Debug_TTExt.Log("AudioClipToFMODSound() - Failed to convert sound - " + e);
                throw e;
            }
        }


        private static FieldInfo ModContainerGet = typeof(ManMods).GetField("m_Mods", BindingFlags.NonPublic | BindingFlags.Instance);
        
        internal static bool CheckTTExtUtils()
        {
            bool success = true;
            if (GetAllMods().Any())
            {
                FileVersionInfo FVIC = null;
                GameVersion compVal = default;
                foreach (var item in GetAllMods().Values)
                {
                    string filePos;
                    if (FVIC == null)
                    {
                        if (GUIManaged.infoCache.TryGetValue(item, out var val))
                        {
                            GUIManaged.CurVersion = val.FileVer;
                        }
                        else
                        {
                            filePos = Path.Combine(new DirectoryInfo(item.AssetBundlePath).Parent.ToString(), "TerraTechETCUtil.dll");
                            if (File.Exists(filePos))
                            {
                                FVIC = FileVersionInfo.GetVersionInfo(filePos);
                                if (FVIC != null)
                                {
                                    GUIManaged.CurVersion = FVIC.FileVersion;
                                    compVal = new GameVersion(GUIManaged.CurVersion);
                                    GUIManaged.infoCache.Add(item, new ExtInfo()
                                    {
                                        synced = true,
                                        FilePos = filePos,
                                        FileVer = GUIManaged.CurVersion,
                                    }
                                    );
                                }
                            }
                        }
                    }
                    else
                    {
                        string FVI;
                        if (GUIManaged.infoCache.TryGetValue(item, out var val))
                        {
                            filePos = val.FilePos;
                            FVI = val.FileVer;
                            if (!val.synced)
                                success = false;
                        }
                        else
                        {
                            filePos = Path.Combine(new DirectoryInfo(item.AssetBundlePath).Parent.ToString(), "TerraTechETCUtil.dll");
                            if (File.Exists(filePos))
                            {
                                FVIC = FileVersionInfo.GetVersionInfo(filePos);
                                if (FVIC != null)
                                {
                                    FVI = FVIC.FileVersion;
                                    GUIManaged.infoCache.Add(item, new ExtInfo()
                                    {
                                        synced = true,
                                        FilePos = filePos,
                                        FileVer = FVI,
                                    }
                                    );
                                    break;
                                }
                            }
                            success = false;
                            GUIManaged.infoCache.Add(item, new ExtInfo()
                            {
                                synced = false,
                                FilePos = filePos,
                                FileVer = new GameVersion().ToString(),
                            }
                            );
                        }
                    }
                }
            }
            return success;
        }

        internal struct RenderQueueItem
        {
            internal GameObject GO;
            internal Bounds bounds;
            internal bool onSite;
            internal Vector3 lookVec;
            internal Action<Texture2D> Callback;
            internal List<Globals.ObjectLayer> permittedlayers;
        }

        private static Vector3 OffsetSnapVec => new Vector3(0, 350, 0);
        private static Camera Snapshotter;
        private static Coroutine updater;
        private static Queue<RenderQueueItem> queueRend = null;
        private static RenderQueueItem RQI = default;
        private static List<Globals.ObjectLayer> AllowedLayers = new List<Globals.ObjectLayer>
        {
            new Globals.ObjectLayer("UI"),
            Globals.inst.layerTank,
            Globals.inst.layerTankIgnoreTerrain,
            Globals.inst.layerScenery,
            Globals.inst.layerSceneryCoarse,
            Globals.inst.layerSceneryFader,
            Globals.inst.layerLandmark,
            Globals.inst.layerPickup,
        };

        public static void AllocPreviewer()
        {
            if (Snapshotter == null)
            {
                queueRend = new Queue<RenderQueueItem>();
                //Snapshotter = UnityEngine.Object.Instantiate(ManScreenshot.inst.m_TechPresetCameraPrefab, null).Camera;
                Snapshotter = UnityEngine.Object.Instantiate(Singleton.camera, null);
                /*
                foreach (var item in Snapshotter.GetComponents<MonoBehaviour>())
                {
                    if (item != null && item.enabled)
                    {
                        item.enabled = false;
                        Debug_TTExt.Log("Destroyed useless " + item.GetType());
                        UnityEngine.Object.Destroy(item);
                    }
                }*/
                //Snapshotter = new GameObject("SnapCam").AddComponent<Camera>();
                //Snapshotter.CopyFrom(Singleton.camera);
                float dist = 80f;
                Snapshotter.allowHDR = false;
                Snapshotter.backgroundColor = new UnityEngine.Color(0,0,0,0);
                //Snapshotter.renderingPath = ManScreenshot.inst.m_TechPresetCameraPrefab.Camera.renderingPath;
                Snapshotter.farClipPlane = dist;
                Snapshotter.nearClipPlane = 0.1f;
                Snapshotter.forceIntoRenderTexture = true;
                Snapshotter.fieldOfView = Singleton.camera.fieldOfView;
                /*
                Snapshotter.projectionMatrix = Singleton.camera.projectionMatrix;
                Snapshotter.cullingMatrix = Singleton.camera.cullingMatrix;
                */
                var layers = Snapshotter.layerCullDistances;
                for (int i = 0; i < layers.Length; i++)
                {
                    layers[i] = dist;
                    /*
                    switch (i)
                    {
                        case 13:
                            layers[i] = 0;
                            break;
                        default:
                            layers[i] = dist;
                            break;
                    }*/
                }
                Snapshotter.layerCullDistances = layers;

                SyncLayers(null);

                Snapshotter.enabled = false;
                Snapshotter.gameObject.SetActive(false);
            }
        }
        internal static Vector3 returnPos;
        internal static Transform returnParent;
        public static IEnumerator StaticUpdate()
        {
            yield return new WaitForEndOfFrame();
            while (queueRend.Any())
            {
                RQI = queueRend.Dequeue();
                if (!RQI.onSite)
                {
                    returnPos = RQI.GO.transform.localPosition;
                    returnParent = RQI.GO.transform.parent;
                    RQI.GO.transform.SetParent(null);
                    RQI.GO.transform.localPosition = OffsetSnapVec;
                }
                yield return new WaitForEndOfFrame();
                var outcome =  PreviewGO_Internal(RQI.GO, RQI.bounds, RQI.lookVec, RQI.permittedlayers);
                yield return new WaitForEndOfFrame();
                RQI.Callback(outcome);
                if (!RQI.onSite)
                {
                    RQI.GO.transform.SetParent(returnParent);
                    RQI.GO.transform.localPosition = returnPos;
                }
            }
            yield break;
            /*
            if (RQI.GO && RQI.Callback != null)
            {
                if (RQI.onSite)
                {
                    RQI.Callback(PreviewGOOnSite_Internal(RQI.GO, RQI.bounds, RQI.lookVec, RQI.permittedlayers));
                }
                else
                {
                    RQI.Callback(PreviewGO_Internal(RQI.GO, RQI.bounds, RQI.returnPos, RQI.lookVec, RQI.permittedlayers));
                }
                RQI = default;
            }
            if (queueRend.Any() && RQI.Callback == null)
            {
                RQI = queueRend.Dequeue();
                if (!RQI.onSite)
                {
                    RQI.returnPos = RQI.GO.transform.localPosition;
                    RQI.GO.transform.position = OffsetSnapVec;
                }
            }
            */
        }
        public static void SyncLayers(List<Globals.ObjectLayer> permittedlayers)
        {
            int layerMask = 0;
            if (permittedlayers == null)
            {
                permittedlayers = AllowedLayers;
                foreach (var item in permittedlayers)
                {
                    layerMask |= item.mask;
                    //Debug_TTExt.Log("Permitted: " + ((int)item).ToString());
                }
                //layerMask |= Globals.inst.layerGroups[Globals.ObjectLayer.Group.Type.PhysicalScenery].LayerMask;
                Snapshotter.cullingMask = layerMask;
            }
            else if (!permittedlayers.Any())
            {
                Snapshotter.cullingMask = int.MaxValue;
                return;
            }

            //int layerMask = int.MaxValue;
            foreach (var item in permittedlayers)
            {
                layerMask |= item.mask;
                //Debug_TTExt.Log("Permitted: " + ((int)item).ToString());
            }
            /*
            for (int i = 0; i < 32; i++)
            {
                int index = permittedlayers.FindIndex(x => x == i);
                if (index != -1)
                    layerMask &= ~permittedlayers[i].mask;
                //bool trueCase = ((layerMask >> i) & 1) == 1;
                layerMask |= i;
            }*/
            //layerMask = ~layerMask;
            /*
            var layers = Snapshotter.layerCullDistances;
            for (int i = 0; i < layers.Length; i++)
            {
                if (permittedlayers.Contains(i))
                    layers[i] = 1000f;
                else
                    layers[i] = 0f;
            }
            Snapshotter.layerCullDistances = layers;
            */
            Snapshotter.cullingMask = layerMask;
        }
        public static void DeallocPreviewer()
        {
            if (Snapshotter)
            {
                InvokeHelper.CancelCoroutine(StaticUpdate());
                queueRend = null;
                UnityEngine.Object.Destroy(Snapshotter.gameObject);
                Snapshotter = null;
            }
        }
      
        public static void GeneratePreviewForGameObject(Action<Texture2D> Callback, GameObject GO, Bounds bounds, List<Globals.ObjectLayer> LayersToShow = null)
        {   // The block preview is dirty, so we need to re-render a preview icon
            if (!GO)
                return;
            AllocPreviewer();
            Transform trans = GO.transform;
            Vector3 posOffsetCenter = trans.rotation * bounds.center;
            Vector3 posSet = (Singleton.cameraTrans.position - trans.position + posOffsetCenter) * 0.6f;
            Debug_TTExt.Log("Previewer at " + posSet.ToString("F") + ", offset center " + posOffsetCenter.ToString("F"));
            queueRend.Enqueue(new RenderQueueItem()
            {
                GO = GO,
                onSite = false,
                bounds = bounds,
                lookVec = posSet,
                Callback = Callback,
                permittedlayers = LayersToShow,
            });
            if (queueRend.Count == 1)
                InvokeHelper.InvokeCoroutine(StaticUpdate());
        }
        public static void GeneratePreviewForGameObject(Action<Texture2D> Callback, GameObject GO, Bounds bounds, Vector3 offset, List<Globals.ObjectLayer> LayersToShow = null)
        {   // The block preview is dirty, so we need to re-render a preview icon
            if (!GO)
                return;
            AllocPreviewer();
            queueRend.Enqueue(new RenderQueueItem()
            {
                GO = GO,
                onSite = false,
                bounds = bounds,
                lookVec = offset,
                Callback = Callback,
                permittedlayers = LayersToShow,
            });
            if (queueRend.Count == 1)
                InvokeHelper.InvokeCoroutine(StaticUpdate());
        }
        public static void GeneratePreviewForGameObjectOnSite(Action<Texture2D> Callback, GameObject GO, Bounds bounds, List<Globals.ObjectLayer> LayersToShow = null)
        {   // The block preview is dirty, so we need to re-render a preview icon
            if (!GO)
                return;
            AllocPreviewer();
            Transform trans = GO.transform;
            Vector3 posOffsetCenter = trans.rotation * bounds.center;
            Vector3 posSet = (Singleton.cameraTrans.position - trans.position + posOffsetCenter) * 0.6f;
            Debug_TTExt.Log("Previewer at " + posSet.ToString("F") + ", offset center " + posOffsetCenter.ToString("F"));
            queueRend.Enqueue(new RenderQueueItem()
            {
                GO = GO,
                onSite = true,
                bounds = bounds,
                lookVec = posSet,
                Callback = Callback,
                permittedlayers = LayersToShow,
            });
            if (queueRend.Count == 1)
                InvokeHelper.InvokeCoroutine(StaticUpdate());
        }
        public static void GeneratePreviewForGameObjectOnSite(Action<Texture2D> Callback, GameObject GO, Bounds bounds, Vector3 offset, List<Globals.ObjectLayer> LayersToShow = null)
        {   // The block preview is dirty, so we need to re-render a preview icon
            if (!GO)
                return;
            AllocPreviewer();
            queueRend.Enqueue(new RenderQueueItem()
            {
                GO = GO,
                onSite = true,
                bounds = bounds,
                lookVec = offset,
                Callback = Callback,
                permittedlayers = LayersToShow,
            });
            if (queueRend.Count == 1)
                InvokeHelper.InvokeCoroutine(StaticUpdate());
        }

        private static Texture2D DoTheSnap_Internal()
        {
            Snapshotter.gameObject.SetActive(true);
            // Give the camera a render texture of fixed size
            RenderTexture rendTex = RenderTexture.GetTemporary(1024, 1024, 24, RenderTextureFormat.ARGB32);
            RenderTexture.active = rendTex;

            // Render the gameobject
            Snapshotter.targetTexture = rendTex;
            Snapshotter.Render();

            // Copy it into our target texture
            Texture2D preview = new Texture2D(1024, 1024, TextureFormat.ARGB32, false);
            preview.ReadPixels(new Rect(0, 0, 1024, 1024), 0, 0);
            preview.Apply();

            // Clean up
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rendTex);
            Snapshotter.targetTexture = Singleton.camera.targetTexture;

            Snapshotter.gameObject.SetActive(false);
            if (Camera.current == Snapshotter)
                throw new InvalidOperationException("The current camera should NOT be the texture rendering camera!!!");
            return preview;
        }
        private static Texture2D PreviewGO_Internal(GameObject GO, Bounds bounds, Vector3 offsetCamPos, List<Globals.ObjectLayer> permittedlayers)
        {   // The block preview is dirty, so we need to re-render a preview icon
            if (!GO)
                return null;
            Transform trans = GO.transform;
            Vector3 posOffsetCenter = trans.rotation * bounds.center;
            Vector3 posOffsetTarget = posOffsetCenter + trans.position;
            //DebugExtUtilities.DrawDirIndicatorRecPriz(posOffsetTarget, bounds.size, Color.yellow, 1);

            SyncLayers(permittedlayers);

            Snapshotter.transform.position = offsetCamPos + trans.position;
            Snapshotter.transform.rotation = Quaternion.LookRotation(posOffsetTarget - Snapshotter.transform.position, Vector3.up);
            //DebugExtUtilities.DrawDirIndicatorCircle(Snapshotter.transform.position, Snapshotter.transform.rotation * Vector3.forward, Snapshotter.transform.up, 1, Color.gray, 1);
            //DebugExtUtilities.DrawDirIndicator(Snapshotter.transform.position, Snapshotter.transform.position + (Snapshotter.transform.rotation * Vector3.forward * 2), Color.gray, 1);

            Debug_TTExt.Log("Snapping GeneratePreviewForGameObject_Internal");
            Debug_TTExt.Log("Transform at " + trans.position.ToString("F") + ", camera " + Snapshotter.transform.position.ToString("F"));
            var pic = DoTheSnap_Internal();

            return pic;
        }
#endif



        public static List<SerialGO> CompressToSerials(GameObject GO)
        {
            List<SerialGO> GOL = new List<SerialGO>
            {
                new SerialGO(GO, GO)
            };
            CompressToSerials_Recurse(GO, GO, GOL);
            return GOL;
        }
        private static void CompressToSerials_Recurse(GameObject GO, GameObject GORoot, List<SerialGO> GOL)
        {
            for (int i = 0; i < GO.transform.childCount; i++)
            {
                Transform child = GO.transform.GetChild(i);
                GOL.Add(new SerialGO(child.gameObject, GORoot));
                CompressToSerials_Recurse(child.gameObject, GORoot, GOL);
            }
        }
        public static GameObject DecompressFromSerials(List<SerialGO> GOL)
        {
            GameObject GOR = null;
            foreach (var item in GOL)
            {
                GameObject GO = item.RebuildAndAssignToHierarchy(GOR);
                if (GOR == null)
                    GOR = GO;
            }
            return GOR;
        }
        public static GameObject DecompressFromSerials(GameObject toApplyTo, List<SerialGO> GOL)
        {
            if (!GOL.Any())
                throw new InvalidOperationException("GOL has NO entries!");
            GOL.First().OverrideInternalLayout(toApplyTo);
            for (int i = 1; i < GOL.Count; i++)
                GOL[i].RebuildAndAssignToHierarchy(toApplyTo);
            return toApplyTo;
        }


        public class SerialGO
        {
            //private static StringBuilder SB = new StringBuilder();
            public bool Active;
            public string Name;
            public int Layer;
            public Vector3 Position;
            public Vector4 Rotation;
            public Vector3 Scale;
            public string Hierarchy;
            public string Mesh;
            public string Texture;
            public Dictionary<string, string> ComponentData = new Dictionary<string, string>();
            /// <summary>
            /// EDITOR ONLY
            /// </summary>
            public SerialGO()
            { 
            }
            public SerialGO(GameObject GO, GameObject lowestGO, bool saveMonoData = false)
            {
                if (GO == null)
                    throw new ArgumentNullException("GameObject \"GO\"");
                if (GO.name == null || GO.name.Length == 0)
                    throw new NullReferenceException("GameObject \"GO\" has no name");
                Transform trans = GO.transform;
                Active = GO.activeSelf;
                Name = GO.name;
                Layer = GO.layer;
                Position = trans.localPosition;
                Rotation = new Vector4(trans.localRotation.x, trans.localRotation.y, trans.localRotation.z, trans.localRotation.w);
                Scale = trans.localScale;
                Hierarchy = RecurseHierachyStacker(GO.transform.parent, lowestGO.transform);
                if (GO.GetComponent<MeshFilter>()?.sharedMesh)
                    Mesh = GO.GetComponent<MeshFilter>().sharedMesh.name;
                else if(GO.GetComponent<MeshFilter>()?.mesh)
                    Mesh = GO.GetComponent<MeshFilter>().mesh.name;
                else
                    Mesh = null;
                var Renderer = GO.GetComponent<Renderer>();
                if (Renderer != null)
                {
                    if (Renderer.sharedMaterial?.mainTexture)
                        Texture = Renderer.sharedMaterial.mainTexture.name;
                    else if (Renderer.material?.mainTexture)
                        Texture = Renderer.material.mainTexture.name;
                    else
                        Texture = null;
                }
                else
                    Texture = null;
                try
                {
                    foreach (var item in GO.GetComponents<Collider>())
                    {
                        try
                        {
                            ComponentData.Add(item.GetType().FullName, JsonConvert.SerializeObject(item, Formatting.None));
                        }
                        catch (Exception)
                        {
                            ComponentData.Add(item.GetType().FullName, string.Empty);
                        }
                    }
                }
                catch { }
                if (saveMonoData)
                {
                    try
                    {
                        foreach (var item in GO.GetComponents<MonoBehaviour>())
                        {
                            try
                            {
                                ComponentData.Add(item.GetType().FullName, JsonConvert.SerializeObject(item, Formatting.None));
                            }
                            catch (Exception)
                            {
                                ComponentData.Add(item.GetType().FullName, string.Empty);
                            }
                        }
                    }
                    catch { }
                }
            }
            public GameObject RebuildAndAssignToHierarchy(GameObject root)
            {
                GameObject GO = null;
                try
                {
                    Transform rootNavi = null;
                    if (Name == null)
                        Debug_TTExt.Log("NULL NAME ENCOUNTERED");
                    if (Hierarchy != null)
                    {
                        Debug_TTExt.Log("Part of hierachy " + Hierarchy);
                        if (root == null)
                            throw new InvalidOperationException("GameObject \"" + Name + "\" is part of a hierarchy, but root is null!");
                        string pathUnwinder = Hierarchy;
                        rootNavi = root.transform;
                        int depth = 0;
                        while (pathUnwinder.Length != 0)
                        {
                            string subS;
                            int NextSlashIndex = pathUnwinder.LastIndexOf(Path.DirectorySeparatorChar);
                            if (NextSlashIndex < 1)
                            {
                                NextSlashIndex = pathUnwinder.Length;
                                subS = pathUnwinder;
                            }
                            else
                            {
                                subS = pathUnwinder.Substring(NextSlashIndex + 1);
                                pathUnwinder = pathUnwinder.Remove(NextSlashIndex);
                                if (subS == null || !subS.Any())
                                    break;
                            }
                            if (depth == 0 && (subS == root.name || subS + "_Prefab" == root.name))
                            {
                                try
                                {
                                    /*
                                    var rootNext = rootNavi.Find(subS);
                                    if (!rootNext)
                                        rootNext = rootNavi.Find(subS + "_Prefab");
                                    if (!rootNext)
                                        throw new InvalidOperationException("GameObject \"" + Name +
                                            "\" is part of a hierarchy, but it appears to be incomplete at ROOT GameObject\"" +
                                            subS + "\" - Layout is " + Hierarchy);
                                    Debug_TTExt.Log("Hierachy - " + subS + "(ROOT)");
                                    rootNavi = rootNext;
                                    */
                                }/*
                            catch (InvalidOperationException e)
                            {
                                throw e;
                            }*/
                                catch (Exception e)
                                {
                                    Debug_TTExt.Log("Hierachy - ABORT(ROOT) on exception - " + e);
                                    break;
                                }
                            }
                            else
                            {
                                try
                                {
                                    var rootNext = rootNavi.Find(subS);
                                    if (!rootNext)
                                        rootNext = rootNavi.Find(subS + "_Prefab");
                                    if (rootNext == null)
                                        throw new InvalidOperationException("GameObject \"" + Name +
                                            "\" is part of a hierarchy, but it appears to be incomplete at GameObject\"" +
                                            subS + "\" - Layout is " + Hierarchy);
                                    Debug_TTExt.Log("Hierachy - " + subS);
                                    rootNavi = rootNext;
                                }/*
                            catch (InvalidOperationException e)
                            {
                                throw e;
                            }*/
                                catch (Exception e)
                                {
                                    Debug_TTExt.Log("Hierachy - ABORT on exception - " + e);
                                    break;
                                }
                            }
                            depth++;
                        }
                    }
                    Transform trans = null;
                    if (rootNavi != null)
                    {
                        Transform child = rootNavi.Find(Name);
                        if (child)
                        {
                            Debug_TTExt.Log("Adjusting " + Name);
                            GO = child.gameObject;
                            trans = child;
                        }
                    }
                    if (GO == null)
                    {
                        if (rootNavi == null)
                            Debug_TTExt.Log("Making " + Name);
                        else
                            Debug_TTExt.Log("Making " + Name + " - Attached " + Name + " to " + rootNavi.name);
                        GO = new GameObject(Name);
                        trans = GO.transform;
                        if (rootNavi != null)
                            trans.SetParent(rootNavi, false);
                    }
                    if (root == null)
                    {
                        trans.localPosition = Vector3.zero;
                        trans.localRotation = Quaternion.identity;
                    }
                    else
                    {
                        trans.localPosition = Position;
                        trans.localRotation = new Quaternion(Rotation.x, Rotation.y, Rotation.z, Rotation.w);
                    }
                    trans.localScale = Scale;
                    if (Mesh != null && Mesh.Length > 0)
                    {
                        try
                        {
                            var case1 = GO.GetComponent<MeshFilter>();
                            if (case1 == null)
                                case1 = GO.AddComponent<MeshFilter>();
#if EDITOR
                            case1.sharedMesh = GetMeshFromModAssetBundle(Mesh);
#else
                        case1.sharedMesh = IterateAllModAssetsBundle<Mesh>(Mesh).First().Value;
#endif
                        }
                        catch (Exception e)
                        {
                            throw new NullReferenceException("Mesh", e);
                        }
                        if (Texture != null && Texture.Length > 0)
                        {
                            try
                            {
                                var case1 = GO.GetComponent<MeshRenderer>();
                                if (case1 == null)
                                    case1 = GO.AddComponent<MeshRenderer>();
#if EDITOR
                                GameObject preview = GameObject.Find("GSO_ExampleTech");
                                if (preview == null)
                                    preview = GameObject.Find("SkinPreview_GSO");
                                if (preview == null)
                                    Debug_TTExt.Log("Could not locate SkinPreview object");
                                var testMaterial = new Material(preview.GetComponentInChildren<MeshRenderer>().sharedMaterial);
                                testMaterial.SetTexture("_MainTex", GetTextureFromModAssetBundle(Texture));
#else
                            var testMaterial = new Material(GetMaterialFromBaseGameActive("GSO_Main"));
                            testMaterial.SetTexture("_MainTex", IterateAllModAssetsBundle<Texture2D>(Texture).First().Value);
#endif
                                case1.sharedMaterial = testMaterial;
                                Debug_TTExt.Log("Added texture");
                            }
                            catch (Exception e)
                            {
                                throw new NullReferenceException("Texture", e);
                            }
                        }
                        else
                        {
                            try
                            {
                                var case1 = GO.GetComponent<MeshRenderer>();
                                if (case1 == null)
                                    case1 = GO.AddComponent<MeshRenderer>();
#if EDITOR
                                GameObject preview = GameObject.Find("GSO_ExampleTech");
                                if (preview == null)
                                    preview = GameObject.Find("SkinPreview_GSO");
                                if (preview == null)
                                    Debug_TTExt.Log("Could not locate SkinPreview object");

                                var testMaterial = preview.GetComponentInChildren<MeshRenderer>().sharedMaterial;
#else
                            var testMaterial = GetMaterialFromBaseGameActive("GSO_Main");
#endif
                                case1.sharedMaterial = testMaterial;
                                Debug_TTExt.Log("Added texture(2)");
                            }
                            catch (Exception e)
                            {
                                throw new NullReferenceException("Texture (Fallback)", e);
                            }
                        }
                    }
                    foreach (var item in ComponentData)
                    {
                        try
                        {
                            if (item.Value == null)
                                throw new NullReferenceException("item.Value");
                            Type type = GetTypeDeep(item.Key);
                            Component comp = GO.GetComponent(type);
                            if (comp == null)
                                comp = GO.AddComponent(type);
                            JsonConvert.PopulateObject(item.Value, comp);
                        }
                        catch (Exception e)
                        {
                            UnityEngine.Debug.LogWarning("MonoBehavior " + ((item.Key == null) ? "<NULL>" : item.Key) + " deserialize failed - " + e);
                        }
                    }
                    GO.layer = Layer;
                    if (root == null)
                        GO.SetActive(true);
                    else
                        GO.SetActive(Active);

                    Debug_TTExt.Log("Setup " + Name);
                    return GO;
                }
                catch (Exception e)
                {
                    if (GO != null)
                    {
#if EDITOR
                        UnityEngine.Object.DestroyImmediate(GO, true);
#else
                        UnityEngine.Object.Destroy(GO);
#endif
                    }
                    Debug_TTExt.LogError("Failed on " + Name + ", skipping... - " + e);
                    return GO;
                }
            }
            public GameObject OverrideInternalLayout(GameObject target)
            {
                Transform rootNavi = null;
                if (Name == null)
                    Debug_TTExt.Log("NULL NAME ENCOUNTERED");
                Debug_TTExt.Log("Overriding " + Name);
                if (Hierarchy != null && Hierarchy.Contains(Path.DirectorySeparatorChar))
                    throw new InvalidOperationException("GameObject \"" + Name +
                        "\" is part of a hierarchy, but OverrideAndAssignToHierarchy does not support hierarchies!");
                Transform trans = target.transform;
                if (rootNavi != null)
                {
                    trans.SetParent(rootNavi, false);
                    Debug_TTExt.Log("Hierachy - Attached " + Name + " to " + rootNavi.name);
                }
                trans.localPosition = Vector3.zero;
                trans.localRotation = Quaternion.identity;
                trans.localScale = Vector3.one;
                if (Mesh != null && Mesh.Length > 0)
                {
                    try
                    {
                        var case1 = target.GetComponent<MeshFilter>();
                        if (case1 == null)
                            case1 = target.AddComponent<MeshFilter>();
                        try
                        {
#if EDITOR
                            case1.sharedMesh = GetMeshFromModAssetBundle(Mesh);
#else
                            case1.sharedMesh = IterateAllModAssetsBundle<Mesh>(Mesh).First().Value;
#endif
                        }
                        catch { }
                    }
                    catch (Exception e)
                    {
                        throw new NullReferenceException("Mesh", e);
                    }
                    if (Texture != null && Texture.Length > 0)
                    {
                        try
                        {
                            var case1 = target.GetComponent<MeshRenderer>();
                            if (case1 == null)
                            {
                                case1 = target.AddComponent<MeshRenderer>();
                                try
                                {
#if EDITOR
                                    GameObject preview = GameObject.Find("GSO_ExampleTech");
                                    if (preview == null)
                                        preview = GameObject.Find("SkinPreview_GSO");
                                    var testMaterial = new Material(preview.GetComponentInChildren<MeshRenderer>().sharedMaterial);
                                    testMaterial.SetTexture("_MainTex", GetTextureFromModAssetBundle(Texture));
#else
                                var testMaterial = new Material(GetMaterialFromBaseGameActive("GSO_Main"));
                                testMaterial.SetTexture("_MainTex", IterateAllModAssetsBundle<Texture2D>(Texture).First().Value);
#endif
                                    case1.sharedMaterial = testMaterial;
                                    Debug_TTExt.Log("Added texture");
                                }
                                catch { }
                            }
                        }
                        catch (Exception e)
                        {
                            throw new NullReferenceException("Texture", e);
                        }
                    }
                    else
                    {
                        try
                        {
                            var case1 = target.GetComponent<MeshRenderer>();
                            if (case1 == null)
                            {
                                case1 = target.AddComponent<MeshRenderer>();
                                try
                                {
#if EDITOR
                                    GameObject preview = GameObject.Find("GSO_ExampleTech");
                                    if (preview == null)
                                        preview = GameObject.Find("SkinPreview_GSO");
                                    var testMaterial = preview.GetComponentInChildren<MeshRenderer>().sharedMaterial;
#else
                                var testMaterial = new Material(GetMaterialFromBaseGameActive("GSO_Main"));
#endif
                                    case1.sharedMaterial = testMaterial;
                                    Debug_TTExt.Log("Added texture(2)");
                                }
                                catch { }
                            }
                        }
                        catch (Exception e)
                        {
                            throw new NullReferenceException("Texture (Fallback)", e);
                        }
                    }
                }
                foreach (var item in ComponentData)
                {
                    try
                    {
                        Type type = GetTypeDeep(item.Key);
                        Component comp = target.GetComponent(type);
                        if (comp == null)
                            comp = target.AddComponent(type);
                        JsonConvert.PopulateObject(item.Value, comp);
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogWarning("MonoBehavior " + item.Key + " deserialize failed - " + e);
                    }
                }
                target.layer = Layer;

                Debug_TTExt.Log("Setup " + Name);
                return target;
            }
            private string RecurseHierachyStacker(Transform trans, Transform transLowest)
            {
                if (trans == null)
                    return string.Empty;
                if (trans == transLowest)
                    return trans.name;
                string next = RecurseHierachyStacker(trans.parent, transLowest);
                if (next != string.Empty)
                    return trans.name + Path.DirectorySeparatorChar + next;
                else 
                    return trans.name;
            }
        }

        public static Type GetTypeDeep(string name)
        {
            if (name == null)
                throw new NullReferenceException("value " + name + " is NULL");
            foreach (var item in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var type = item.GetType(name);
                    if (type != null)
                        return type;
                }
                catch { }
            }
            throw new NullReferenceException("type " + name + " does not exists");
        }
        public static Type[] FORCE_GET_TYPES(Assembly AEM)
        {
            try
            {
                return AEM.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types;
            }
        }




#if !EDITOR
        private static List<AsyncLoader> asyncLoaders = new List<AsyncLoader>();
        public interface AsyncLoader
        {
            bool IsBusy { get; }
            void Update();
            void Abort();
        }
        public class AsyncLoader<T> : AsyncLoader where T : UnityEngine.Object
        {
            public static readonly AsyncLoader<T> Default = new AsyncLoader<T>("Default");
            public bool IsBusy { get => step != arrayCache.Length; }
            private T[] arrayCache;
            private string name;
            private int step;
            private int stepRate;
            private HashSet<T> Iterated;
            private Dictionary<string, int> names;
            private Action<T> callback;
            public AsyncLoader(string name)
            {
                stepRate = 1;
                arrayCache = new T[0];
                Iterated = new HashSet<T>();
                names = new Dictionary<string, int>();
                callback = null;
                this.name = name;
                step = 0;
            }
            public AsyncLoader(T[] array, string name, Action<T> Callback, int stepRateSet)
            {
                stepRate = stepRateSet;
                arrayCache = (T[])array.Clone();
                Iterated = new HashSet<T>();
                names = new Dictionary<string, int>();
                callback = Callback;
                this.name = name;
                step = 0;
                asyncLoaders.Add(this);
                Update();
            }

            public void Abort()
            {
                InvokeHelper.CancelInvoke(Update);
                asyncLoaders.Remove(this);
            }
            public void Update()
            {
                int deltaStep = Mathf.Min(step + stepRate, arrayCache.Length);
                while (step < deltaStep)
                {
                    var item = arrayCache[step];
                    if (Iterated.Add(item))
                    {
                        if (names.TryGetValue(item.name, out int stepName))
                        {
                            string nameCase = item.name + "(" + stepName + ")";
                            if (name.Equals(nameCase))
                            {
                                callback.Invoke(item);
                                asyncLoaders.Remove(this);
                                step = arrayCache.Length;
                                return;
                            }
                            names[item.name] = stepName + 1;
                        }
                        else
                        {
                            names.Add(item.name, 1);
                            if (name.Equals(item.name))
                            {
                                callback.Invoke(item);
                                asyncLoaders.Remove(this);
                                step = arrayCache.Length;
                                return;
                            }
                        }
                    }
                    step++;
                }
                if (step == arrayCache.Length)
                {
                    asyncLoaders.Remove(this);
                }
                else
                    InvokeHelper.Invoke(Update, 0.04f);
            }
        }
        public class AsyncLoaderUnstable : AsyncLoader
        {
            public static readonly AsyncLoaderUnstable Default = new AsyncLoaderUnstable("Default");
            public bool IsBusy { get => step != arrayCache.Length; }
            private UnityEngine.Object[] arrayCache;
            private string name;
            private int step;
            private int stepRate;
            private HashSet<UnityEngine.Object> Iterated;
            private Dictionary<string, int> names;
            private Action<UnityEngine.Object> callback;
            public AsyncLoaderUnstable(string name)
            {
                stepRate = 1;
                arrayCache = new UnityEngine.Object[0];
                Iterated = new HashSet<UnityEngine.Object>();
                names = new Dictionary<string, int>();
                callback = null;
                this.name = name;
                step = 0;
            }
            public AsyncLoaderUnstable(UnityEngine.Object[] array, string name, Action<UnityEngine.Object> Callback, int stepRateSet)
            {
                stepRate = stepRateSet;
                arrayCache = (UnityEngine.Object[])array.Clone();
                Iterated = new HashSet<UnityEngine.Object>();
                names = new Dictionary<string, int>();
                callback = Callback;
                this.name = name;
                step = 0;
                asyncLoaders.Add(this);
                Update();
            }

            public void Abort()
            {
                InvokeHelper.CancelInvoke(Update);
                asyncLoaders.Remove(this);
            }
            public void Update()
            {
                int deltaStep = Mathf.Min(step + stepRate, arrayCache.Length);
                while (step < deltaStep)
                {
                    var item = arrayCache[step];
                    if (Iterated.Add(item))
                    {
                        if (names.TryGetValue(item.name, out int stepName))
                        {
                            string nameCase = item.name + "(" + stepName + ")";
                            if (name.Equals(nameCase))
                            {
                                callback.Invoke(item);
                                asyncLoaders.Remove(this);
                                step = arrayCache.Length;
                                return;
                            }
                            names[item.name] = stepName + 1;
                        }
                        else
                        {
                            names.Add(item.name, 1);
                            if (name.Equals(item.name))
                            {
                                callback.Invoke(item);
                                asyncLoaders.Remove(this);
                                step = arrayCache.Length;
                                return;
                            }
                        }
                    }
                    step++;
                }
                if (step == arrayCache.Length)
                {
                    asyncLoaders.Remove(this);
                }
                else
                    InvokeHelper.Invoke(Update, 0.04f);
            }
        }

        internal enum ResourceHelperGUITypes
        {
            IngameTexture,
            ModTexture,
            ModContainer,
            ETC_Utils
        }
        internal class ExtInfo
        {
            internal bool synced;
            internal string FilePos;
            internal string FileVer;
        }
        internal class GUIManaged : GUILayoutHelpers
        {
            private static bool controlledDisp = false;
            private static bool controlledDisp2 = false;
            private static HashSet<string> enabledTabs = null;
            private static ResourceHelperGUITypes guiType = ResourceHelperGUITypes.ModTexture;
            public static Dictionary<ModContainer, ExtInfo> infoCache = new Dictionary<ModContainer, ExtInfo>();
            public static void GUIGetTotalManaged()
            {
                if (enabledTabs == null)
                {
                    enabledTabs = new HashSet<string>();
                    contentNONE = new Dictionary<string, ModdedAsset>();
                    content = contentNONE;
                }
                GUIResources();
                GUIExtSFX();
            }
            public static void GUIResources()
            {
                GUILayout.Box("--- Mod Resources --- ");
                bool show = controlledDisp && Singleton.playerTank;
                if (GUILayout.Button("Enabled Loading: " + show))
                    controlledDisp = !controlledDisp;
                if (controlledDisp)
                {
                    try
                    {
                        bool deltaed = false;
                        guiType = (ResourceHelperGUITypes)GUIToolbarCategoryDisp<ResourceHelperGUITypes>((int)guiType);
                        switch (guiType)
                        {
                            case ResourceHelperGUITypes.ModTexture:
                                GUIModTextures(ref deltaed);
                                break;
                            case ResourceHelperGUITypes.IngameTexture:
                                GUIIngameMaterials(ref deltaed);
                                break;
                            case ResourceHelperGUITypes.ModContainer:
                                GUIModContents(ref deltaed);
                                break;
                            case ResourceHelperGUITypes.ETC_Utils:
                                CheckTTExtUtils();
                                GUILayout.Box("TerraTechETCUtils within mods");
                                GUILayout.BeginHorizontal();
                                GUILayout.Box("Current Version: ");
                                GUILayout.Box(CurVersion);
                                GUILayout.FlexibleSpace();
                                GUILayout.EndHorizontal();
                                foreach (var item in GetAllMods().Values)
                                {
                                    if (infoCache.TryGetValue(item, out var val))
                                    {
                                        string filePos = val.FilePos;
                                        string FVI = val.FileVer;
                                        if (File.Exists(filePos))
                                        {
                                            GUILayout.BeginHorizontal();
                                            GUILayout.Label(item.Contents.ModName);
                                            GUILayout.FlexibleSpace();
                                            if (FVI != null)
                                            {
                                                GUILayout.Label("Version: ");
                                                GUILayout.Label(FVI);
                                            }
                                            else
                                                GUILayout.Label("Version: NULL");
                                            GUILayout.EndHorizontal();
                                        }
                                    }
                                }
                                break;
                            default:
                                break;
                        }
                    }
                    catch (ExitGUIException e)
                    {
                        throw e;
                    }
                    catch (Exception e)
                    {
                        Debug_TTExt.Log("ResourcesHelper UI Debug errored - " + e);
                    }
                }
            }


            private static FieldInfo ModContainerGet = typeof(ManMods).GetField("m_Mods", BindingFlags.NonPublic | BindingFlags.Instance);
            internal static string CurVersion = "error";
            private static string MCName = "";
            private static ModContainer ModContainerInst = null;
            private static bool showModContainers = false;
            private static Dictionary<string, ModdedAsset> content = null;
            private static Dictionary<string, ModdedAsset> contentNONE = null;
            private static void GUIModContainers(ref bool deltaed)
            {
                if (GUITextFieldDisp("ModContainer Name:", ref MCName))
                    deltaed = true;
                showModContainers = AltUI.Toggle(showModContainers, "Get Active ModContainers");
                if (showModContainers)
                {
                    int select = -1;
                    select = GUIVertToolbar(select, GetAllMods().Values.Select(x => x.Contents.ModName).ToArray());
                    if (select != -1)
                    {
                        MCName = GetAllMods().ElementAt(select).Value.Contents.ModName;
                        deltaed = true;
                        showModContainers = false;
                    }
                }
                if (deltaed)
                {
                    if (TryGetModContainer(MCName, out ModContainerInst))
                    {
                        FieldInfo FI = typeof(ModContainer).GetField("m_AssetLookup", BindingFlags.NonPublic | BindingFlags.Instance);
                        content = (Dictionary<string, ModdedAsset>)FI.GetValue(ModContainerInst);
                    }
                    else
                    {
                        ModContainerInst = null;
                        content = contentNONE;
                    }
                }
            }


            private static bool showModTexs = false;
            private static string TexName = "";
            private static Texture2D cachedTex = null;
            private static SlowSorter<Texture> slowTex = SlowSorter<Texture>.Default;
            private static void GUIModTextures(ref bool deltaed)
            {
                GUILayout.Box("Mod Textures");
                GUIModContainers(ref deltaed);
                if (ModContainerInst != null)
                {
                    if (GUITextFieldDisp("Texture Name:", ref TexName))
                        deltaed = true;
                    showModTexs = AltUI.Toggle(showModTexs, "Show Active Mod Textures");
                    if (showModTexs)
                    {
                        if (deltaed)
                        {
                            List<Texture> texs = new List<Texture>();
                            foreach (var item in ModContainerInst.Contents.m_AdditionalAssets)
                            {
                                if (item.IsNotNull())
                                {
                                    if (item is Texture2D tex)
                                        texs.Add(tex);
                                    else if (item is GameObject GO)
                                    {
                                        var MR = GO.GetComponentInChildren<MeshRenderer>();
                                        if (MR?.sharedMaterial?.mainTexture)
                                            texs.Add(MR.sharedMaterial.mainTexture);
                                    }
                                }
                            }
                            slowTex.SetSearchArrayAndSearchQuery(texs.ToArray(), TexName, false);
                        }
                        if (slowTex.namesValid.Count > 0)
                        {
                            int select = -1;
                            select = GUIVertToolbar(select, slowTex.namesValid);
                            if (select != -1)
                            {
                                TexName = slowTex.namesValid[select];
                                deltaed = true;
                                if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
                                    showModTexs = false;
                            }
                        }
                    }
                    else
                        slowTex.Abort();
                    if (deltaed)
                    {
                        cachedTex = GetTextureFromModAssetBundle(ModContainerInst, TexName, false);
                    }
                    if (cachedTex != null)
                    {
                        GUILayout.Label("Success");
                        float maxDim = DebugExtUtilities.HotWindow.width / 2;
                        GUILayout.Button(cachedTex, GUILayout.MaxWidth(maxDim), GUILayout.MaxHeight(maxDim));
                    }
                    else
                        GUILayout.Label("ModContainer \"" + MCName + "\" exists but no such texture was found");
                }
                else
                    GUILayout.Label("No Valid ModContainer Selected");
            }

            private static string MatName = "";
            private static Material cachedMat = null;
            private static Texture cachedMatTex = null;
            private static AsyncLoader<Material> asyncMatFinder = AsyncLoader<Material>.Default;
            private static Material[] cachedMats = null;
            private static SlowSorter<Material> slowSort = new SlowSorter<Material>(64);
            private static bool showAllMats = false;
            private static bool hideDuplicates = true;
            private static void GUIIngameMaterials(ref bool deltaed)
            {
                GUILayout.Box("Ingame Materials");
                if (GUITextFieldDisp("Material Name:", ref MatName))
                    deltaed = true;
                showAllMats = AltUI.Toggle(showAllMats, "Show Active Materials");
                if (showAllMats)
                {
                    bool prevHideDupes = hideDuplicates;
                    hideDuplicates = AltUI.Toggle(hideDuplicates, "[Filter Out Duplicates]");
                    if (hideDuplicates != prevHideDupes)
                        deltaed = true;
                    if (cachedMats == null)
                    {
                        cachedMats = UnityEngine.Object.FindObjectsOfType<Material>();
                        slowSort.Abort();
                        slowSort.SetSearchArrayAndSearchQuery(cachedMats, MatName, !hideDuplicates);
                    }
                    else if (deltaed)
                        slowSort.SetNewSearchQueryIfNeeded(MatName, !hideDuplicates);
                    if (slowSort.namesValid.Count > 0)
                    {
                        int select = -1;
                        select = GUIVertToolbar(select, slowSort.namesValid);
                        if (select != -1)
                        {
                            MatName = slowSort.namesValid[select];
                            deltaed = true;
                            if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
                                showAllMats = false;
                        }
                    }
                }
                else
                {
                    cachedMats = null;
                    slowSort.Abort();
                }
                if (deltaed)
                {
                    asyncMatFinder.Abort();
                    GetResourceFromBaseGamePreciseAsync(MatName, (Material outp) =>
                    {
                        cachedMat = outp;
                        if (cachedMat)
                            cachedMatTex = cachedMat.mainTexture;
                    }, 128);
                }
                if (cachedMat != null)
                {
                    if (cachedMatTex != null)
                    {
                        GUILayout.Label("Success");
                        float maxDim = DebugExtUtilities.HotWindow.width / 2;
                        GUILayout.Button(cachedMat.mainTexture, GUILayout.MaxWidth(maxDim), GUILayout.MaxHeight(maxDim));
                    }
                    else
                        GUILayout.Label("Material has no mainTexture!");
                }
                else
                    GUILayout.Label("No Material Selected");
            }


            private static bool showExtSFX = false;
            public static void GUIExtSFX()
            {
                GUILayout.Box("--- Mod SFX Data --- ");
                bool show = showExtSFX && Singleton.playerTank;
                if (GUILayout.Button("Enabled Loading: " + show))
                    showExtSFX = !showExtSFX;
                if (showExtSFX)
                {
                    try
                    {
                        int count = 0;
                        foreach (var item in ManAudioExt.AllSounds)
                        {
                            count += item.Value.Count;
                        }
                        GUILabelDispFast("Count: ", count);
                        if (GUILayout.Button("Play Last", AltUI.ButtonRed))
                            ResourcesHelper.PlayLastCached();
                        if (GUILayout.Button("Reload ALL", AltUI.ButtonRed))
                            ManAudioExt.RebuildAllSounds();
                        GUILayout.Label("Sounds", AltUI.LabelBlackTitle);
                        foreach (var item in ManAudioExt.AllSounds)
                        {
                            GUILayout.BeginVertical(AltUI.TextfieldBlackHuge);
                            GUILayout.Label(item.Key.ModID, AltUI.LabelBlueTitle);
                            foreach (var item1 in item.Value)
                            {
                                GUILayout.BeginVertical(AltUI.TextfieldBlackHuge);
                                GUILayout.Label(item1.Key, AltUI.LabelBlue);
                                if (GUILayout.Button("Main", AltUI.ButtonBlue))
                                    item1.Value.main.GetRandomEntry().Play();
                                if (item1.Value.startup != null && GUILayout.Button("Startup", AltUI.ButtonBlue))
                                    item1.Value.startup.Play();
                                if (item1.Value.engage != null && GUILayout.Button("Engage", AltUI.ButtonBlue))
                                    item1.Value.engage.Play();
                                if (item1.Value.stop != null && GUILayout.Button("Stop", AltUI.ButtonBlue))
                                    item1.Value.stop.Play();
                                if (GUILayout.Button("Stop All", AltUI.ButtonRed))
                                {
                                    foreach (var item2 in item1.Value.main)
                                        item2.Stop();
                                    if (item1.Value.startup != null)
                                        item1.Value.startup.Stop();
                                    if (item1.Value.engage != null)
                                        item1.Value.engage.Stop();
                                    if (item1.Value.stop != null)
                                        item1.Value.stop.Stop();
                                }
                                GUILayout.EndVertical();
                            }
                            GUILayout.EndVertical();
                        }
                    }
                    catch (ExitGUIException e)
                    {
                        throw e;
                    }
                    catch (Exception e)
                    {
                        Debug_TTExt.Log("ResourcesHelper UI Debug errored - " + e);
                    }
                }
            }



            private static ModBundleGUITypes guiMBType = ModBundleGUITypes.ALL;
            internal enum ModBundleGUITypes
            {
                ALL,
                Blocks,
                Skins,
                Corps,
                Additional,
            }
            private static void GUIModContents(ref bool deltaed)
            {
                GUILayout.Box("Mod Packed Resources");
                GUIModContainers(ref deltaed);
                if (ModContainerInst != null)
                {
                    guiMBType = (ModBundleGUITypes)GUIToolbarCategoryDisp<ModBundleGUITypes>((int)guiMBType);
                    switch (guiMBType)
                    {
                        case ModBundleGUITypes.ALL:
                            GUILayout.Box("Mod ALL Assets");
                            GUILabelDispFast("Count: ", content.Count);
                            foreach (var item in content)
                            {
                                try
                                {
                                    GUILayout.BeginHorizontal();
                                    GUILayout.Label(item.Key);
                                    GUILayout.FlexibleSpace();
                                    GUILayout.Button(item.Value.GetType().ToString());
                                    GUILayout.EndHorizontal();
                                }
                                catch (ExitGUIException e)
                                {
                                    throw e;
                                }
                                catch { }
                            }
                            break;
                        case ModBundleGUITypes.Blocks:
                            GUILayout.Box("Mod Block Assets");
                            GUILabelDispFast("Count: ", ModContainerInst.Contents.m_Blocks.Count);
                            foreach (var item in ModContainerInst.Contents.m_Blocks)
                            {
                                try
                                {
                                    GUILayout.BeginHorizontal();
                                    GUILayout.Label(item.m_BlockDisplayName);
                                    GUILayout.FlexibleSpace();
                                    GUILayout.Label(item.m_Corporation);
                                    GUILayout.Label("|");
                                    GUILayout.Label(item.m_Grade.ToString());
                                    GUILayout.EndHorizontal();
                                }
                                catch (ExitGUIException e)
                                {
                                    throw e;
                                }
                                catch { }
                            }
                            break;
                        case ModBundleGUITypes.Skins:
                            GUILayout.Box("Mod Skin Assets");
                            GUILabelDispFast("Count: ", ModContainerInst.Contents.m_Skins.Count);
                            foreach (var item in ModContainerInst.Contents.m_Skins)
                            {
                                try
                                {
                                    GUILayout.BeginHorizontal();
                                    GUILayout.Label(item.m_SkinDisplayName);
                                    GUILayout.FlexibleSpace();
                                    GUILayout.Label(item.m_Corporation);
                                    GUILayout.EndHorizontal();
                                }
                                catch (ExitGUIException e)
                                {
                                    throw e;
                                }
                                catch { }
                            }
                            break;
                        case ModBundleGUITypes.Corps:
                            GUILayout.Box("Mod Corp Assets");
                            GUILabelDispFast("Count: ", ModContainerInst.Contents.m_Corps.Count);
                            foreach (var item in ModContainerInst.Contents.m_Corps)
                            {
                                try
                                {
                                    GUILayout.BeginHorizontal();
                                    GUILayout.Label(item.m_DisplayName);
                                    GUILayout.FlexibleSpace();
                                    GUILayout.Label(item.m_ShortName);
                                    GUILayout.EndHorizontal();
                                }
                                catch (ExitGUIException e)
                                {
                                    throw e;
                                }
                                catch { }
                            }
                            break;
                        case ModBundleGUITypes.Additional:
                            GUILayout.Box("Mod Additional Assets");
                            GUILabelDispFast("Count: ", ModContainerInst.Contents.m_AdditionalAssets.Count);
                            foreach (var item in ModContainerInst.Contents.m_AdditionalAssets)
                            {
                                try
                                {
                                    GUILayout.BeginHorizontal();
                                    GUILayout.Label(item.name);
                                    GUILayout.FlexibleSpace();
                                    GUILayout.Label(item.GetType().ToString());
                                    GUILayout.EndHorizontal();
                                }
                                catch (ExitGUIException e)
                                {
                                    throw e;
                                }
                                catch { }
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
        }
#endif
    }
}
