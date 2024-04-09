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

#if EDITOR
using UnityEditor;
using UnityEditorInternal;
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

        public void SubToPostBlocksLoad(Action postEvent)
        {
            ResourcesHelper.PostBlocksLoadEvent.Subscribe(postEvent);
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
#endif
    public static class ResourcesHelper
    {
        public static bool ShowDebug = false;

#if !EDITOR
        private static Dictionary<string, ModContainer> modsEmpty = new Dictionary<string, ModContainer>();
        private static Dictionary<string, ModContainer> modsDirect = null;
        public static EventNoParams PostBlocksLoadEvent = new EventNoParams();


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
            if (modsDirect == null)
                modsDirect = modsEmpty;
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


#if !EDITOR
        public static T GetObjectFromModContainer<T>(this ModContainer MC, Func<T, bool> searchIterator) where T : UnityEngine.Object
        {
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
            Mesh mesh = null;
            return (T)MC.Contents.m_AdditionalAssets.Find(delegate (UnityEngine.Object cand)
            { return cand is T && cand.name.Equals(nameNoExt); });
        }
        public static IEnumerable<T> IterateAssetsInModContainer<T>(this ModContainer MC, Func<T, bool> searchIterator) where T : UnityEngine.Object
        {
            foreach (var cand in MC.Contents.m_AdditionalAssets)
            {
                var result = AssetIteratorNested(cand, searchIterator);
                if (result != null)
                    yield return result;
            }
        }
        public static IEnumerable<T> IterateAssetsInModContainer<T>(this ModContainer MC) where T : UnityEngine.Object
        {
            foreach (var cand in MC.Contents.m_AdditionalAssets)
            {
                var result = AssetIterator<T>(cand);
                if (result != null)
                    yield return result;
            }
        }
        public static IEnumerable<T> IterateAssetsInModContainer<T>(this ModContainer MC, string nameStartsWith) where T : UnityEngine.Object
        {
            foreach (var cand in MC.Contents.m_AdditionalAssets)
            {
                var result = AssetIteratorPrefix<T>(cand, nameStartsWith);
                if (result != null)
                    yield return result;
            }
        }
        public static IEnumerable<T> IterateAssetsInModContainerPostfix<T>(this ModContainer MC, string nameEndsWith) where T : UnityEngine.Object
        {
            foreach (var cand in MC.Contents.m_AdditionalAssets)
            {
                var result = AssetIteratorPostfix<T>(cand, nameEndsWith);
                if (result != null)
                    yield return result;
            }
        }
        public static IEnumerable<T> IterateAssetsInModContainerPostfix<T>(this ModContainer MC, string nameEndsWith, Func<T, bool> searchIterator) where T : UnityEngine.Object
        {
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
                var testMaterial = new Material(GetMaterialFromBaseGame("GSO_Main"));
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
        public static Texture2D GetTexture2DFromBaseGame(string MaterialName, bool complainWhenFail = true)
        {
            var res = UnityEngine.Object.FindObjectsOfType<Texture2D>();
            Texture2D mat = res.FirstOrDefault(delegate (Texture2D cand) { return cand.name.Equals(MaterialName); });
            if (complainWhenFail)
                Debug_TTExt.Assert(mat == null, MaterialName + " Texture2D in base game could not be found!");
            return mat;
        }
        public static Texture2D GetTexture2DFromBaseGamePrecise(string MaterialName, bool complainWhenFail = true)
        {
            HashSet<Texture2D> Textures = new HashSet<Texture2D>();
            foreach (var item in UnityEngine.Object.FindObjectsOfType<Texture2D>())
            {
                if (item.IsNotNull() && !item.name.NullOrEmpty())
                {
                    Textures.Add(item);
                }
            }
            HashSet<string> names = new HashSet<string>();
            foreach (var item in Textures)
            {
                if (names.Add(item.name) && MaterialName.Equals(item.name))
                {
                    return item;
                }
                int stepName = 0;
                bool nameAdd = false;
                while (!nameAdd)
                {
                    stepName++;
                    string nameCase = item.name + "(" + stepName + ")";
                    nameAdd = names.Add(item.name);
                    if (nameAdd && MaterialName.Equals(nameCase))
                    {
                        return item;
                    }
                }
            }
            Debug_TTExt.Assert(complainWhenFail, MaterialName + " Texture2D in base game could not be found!");
            return null;
        }


        public static Material GetMaterialFromBaseGame(string MaterialName, bool complainWhenFail = true)
        {
            var res = UnityEngine.Object.FindObjectsOfType<Material>();
            Material mat = res.FirstOrDefault(delegate (Material cand) { return cand.name.Equals(MaterialName); });
            if (complainWhenFail)
                Debug_TTExt.Assert(mat == null, MaterialName + " Material in base game could not be found!");
            return mat;
        }
        public static Material GetMaterialFromBaseGamePrecise(string MaterialName, bool complainWhenFail = true)
        {
            HashSet<Material> Materials = new HashSet<Material>();
            foreach (var item in UnityEngine.Object.FindObjectsOfType<Material>())
            {
                if (item.IsNotNull() && !item.name.NullOrEmpty())
                {
                    Materials.Add(item);
                }
            }
            HashSet<string> names = new HashSet<string>();
            foreach (var item in Materials)
            {
                if (names.Add(item.name) && MaterialName.Equals(item.name))
                {
                    return item;
                }
                bool added = false;
                int stepName = 0;
                while (!added)
                {
                    stepName++;
                    string nameCase = item.name + "(" + stepName + ")";
                    added = names.Add(nameCase);
                    if (added && MaterialName.Equals(nameCase))
                    {
                        return item;
                    }
                }
            }
            Debug_TTExt.Assert(complainWhenFail, MaterialName + " Material in base game could not be found!");
            return null;
        }
        public static AsyncLoader<T> GetResourceFromBaseGamePreciseAsync<T>(string ResourceName, Action<T> callback, int processIterations = 16) where T : UnityEngine.Object
        {
            return new AsyncLoader<T>(UnityEngine.Object.FindObjectsOfType<T>(), ResourceName, callback, processIterations);
        }
        public static AsyncLoaderUnstable GetResourceFromBaseGamePreciseAsync(Type type, string ResourceName, Action<UnityEngine.Object> callback, int processIterations = 16)
        {
            return new AsyncLoaderUnstable(UnityEngine.Object.FindObjectsOfType(type), ResourceName, callback, processIterations);
        }


        public static Texture2D FetchTexture(ModContainer MC, string pngName, string DLLDirectory)
        {
            Texture2D tex = null;
            try
            {
                //ResourcesHelper.LookIntoModContents(MC);
                if (MC != null)
                    tex = GetTextureFromModAssetBundle(MC, pngName.Replace(".png", ""), false);
                else if (ShowDebug)
                    Debug_TTExt.Log("ModContainer for " + MC.ModID + " DOES NOT EXIST");
                if (!tex)
                {
                    if (ShowDebug)
                        Debug_TTExt.Log("Icon " + pngName.Replace(".png", "") + " did not exist in AssetBundle, using external...");
                    string destination = Path.Combine(DLLDirectory , pngName);
                    tex = FileUtils.LoadTexture(destination);
                }
                if (tex)
                    return tex;
            }
            catch { }
            if (ShowDebug)
                Debug_TTExt.Log("Could not load Icon " + pngName + "!  \n   File is missing!");
            return null;
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
#endif

        public static Camera Snapshotter;
        public static Texture2D GeneratePreviewForGameObject(GameObject GO, Bounds bounds)
        {   // The block preview is dirty, so we need to re-render a preview icon
            if (!GO)
                return null;
            if (Snapshotter == null)
            {
                Snapshotter = UnityEngine.Object.Instantiate(Camera.main);
                Snapshotter.enabled = false;
            }
            Transform trans = GO.transform;
            Vector3 lookAngle = Snapshotter.transform.position - (trans.position + (trans.rotation * bounds.center));
            lookAngle *= 0.6f;
            trans.position = new Vector3(0, 0, 2500);

            Snapshotter.transform.position = lookAngle + trans.position;
            Snapshotter.transform.LookAt((trans.rotation * bounds.center) + trans.position, Vector3.up);

            Snapshotter.farClipPlane = 1000f;
            Snapshotter.nearClipPlane = 0.1f;


            // Give the camera a render texture of fixed size
            RenderTexture rendTex = RenderTexture.GetTemporary(1024, 1024, 24, RenderTextureFormat.ARGB32);
            RenderTexture.active = rendTex;

            // Render the block
            Snapshotter.targetTexture = rendTex;
            Snapshotter.Render();

            // Copy it into our target texture
            Texture2D preview = new Texture2D(1024, 1024);
            preview.ReadPixels(new Rect(0, 0, 1024, 1024), 0, 0);

            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rendTex);
            return preview;
        }

        public static Texture2D GeneratePreviewForGameObject(GameObject GO, Bounds bounds, Vector3 offsetCamPos)
        {   // The block preview is dirty, so we need to re-render a preview icon
            if (!GO)
                return null;
            if (Snapshotter == null)
            {
                Snapshotter = UnityEngine.Object.Instantiate(Camera.main);
                Snapshotter.enabled = false;
            }
            Transform trans = GO.transform;
            trans.position = new Vector3(0, 0, 2500);

            Snapshotter.transform.position = offsetCamPos + trans.position;
            Snapshotter.transform.LookAt((trans.rotation * bounds.center) + trans.position, Vector3.up);

            Snapshotter.farClipPlane = 1000f;
            Snapshotter.nearClipPlane = 0.1f;


            // Give the camera a render texture of fixed size
            RenderTexture rendTex = RenderTexture.GetTemporary(1024, 1024, 24, RenderTextureFormat.ARGB32);
            RenderTexture.active = rendTex;

            // Render the block
            Snapshotter.targetTexture = rendTex;
            Snapshotter.Render();

            // Copy it into our target texture
            Texture2D preview = new Texture2D(1024, 1024);
            preview.ReadPixels(new Rect(0, 0, 1024, 1024), 0, 0);

            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rendTex);
            return preview;
        }

        public static Texture2D GeneratePreviewForGameObjectOnSite(GameObject GO, Bounds bounds)
        {   // The block preview is dirty, so we need to re-render a preview icon
            if (!GO)
                return null;
            if (Snapshotter == null)
            {
                Snapshotter = UnityEngine.Object.Instantiate(Camera.main);
                Snapshotter.enabled = false;
            }
            Transform trans = GO.transform;
            Vector3 lookAngle = Snapshotter.transform.position - (trans.position + (trans.rotation * bounds.center));
            lookAngle *= 0.6f;

            Snapshotter.transform.position = lookAngle + trans.position;
            Snapshotter.transform.LookAt((trans.rotation * bounds.center) + trans.position, Vector3.up);

            Snapshotter.farClipPlane = 1000f;
            Snapshotter.nearClipPlane = 0.1f;


            // Give the camera a render texture of fixed size
            RenderTexture rendTex = RenderTexture.GetTemporary(1024, 1024, 24, RenderTextureFormat.ARGB32);
            RenderTexture.active = rendTex;

            // Render the block
            Snapshotter.targetTexture = rendTex;
            Snapshotter.Render();

            // Copy it into our target texture
            Texture2D preview = new Texture2D(1024, 1024);
            preview.ReadPixels(new Rect(0, 0, 1024, 1024), 0, 0);

            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rendTex);
            return preview;
        }


        public static List<SerialGO> CompressToSerials(GameObject GO)
        {
            List<SerialGO> GOL = new List<SerialGO>
            {
                new SerialGO(GO)
            };
            CompressToSerials_Recurse(GO, GOL);
            return GOL;
        }
        private static void CompressToSerials_Recurse(GameObject GO, List<SerialGO> GOL)
        {
            for (int i = 0; i < GO.transform.childCount; i++)
            {
                Transform ch = GO.transform.GetChild(i);
                GOL.Add(new SerialGO(ch.gameObject));
                CompressToSerials_Recurse(ch.gameObject, GOL);
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


        public class SerialGO
        {
            //private static StringBuilder SB = new StringBuilder();
            public string Name;
            public Vector3 Position;
            public Quaternion Rotation;
            public Vector3 Scale;
            public string Hierarchy;
            public string Mesh;
            public string Texture;
            public Dictionary<string, string> MonoData;
            public SerialGO(GameObject GO, bool saveMonoData = false)
            {
                if (GO == null)
                    throw new ArgumentNullException("GameObject \"GO\"");
                if (GO.name == null || GO.name.Length == 0)
                    throw new NullReferenceException("GameObject \"GO\" has no name");
                Transform trans = GO.transform;
                Name = GO.name;
                Position = trans.localPosition;
                Rotation = trans.localRotation;
                Scale = trans.localScale;
                Hierarchy = RecurseHierachyStacker(GO.transform.parent);
                if (GO.activeInHierarchy)
                {
                    Mesh = GO.GetComponent<MeshFilter>()?.sharedMesh?.name;
                    Texture = GO.GetComponent<MeshRenderer>()?.sharedMaterial?.mainTexture?.name;
                }
                else
                {
                    Mesh = null;
                    Texture = null;
                }
                if (saveMonoData) 
                {
                    foreach (var item in GO.GetComponents<MonoBehaviour>())
                    {
                        try
                        {
                            MonoData.Add(item.GetType().FullName, JsonConvert.SerializeObject(item, Formatting.None));
                        }
                        catch (Exception)
                        {
                            MonoData.Add(item.GetType().FullName, string.Empty);
                        }
                    }
                }
            }
            public GameObject RebuildAndAssignToHierarchy(GameObject root)
            {
                if (root == null && Hierarchy != null && Hierarchy.Length > 0)
                    throw new InvalidOperationException("GameObject \"" + Name + "\" is part of a hierarchy, but root is null!");
                string pathUnwinder = Hierarchy;
                Transform rootNavi = root.transform;
                while (pathUnwinder.Length != 0)
                {
                    int NextSlashIndex = pathUnwinder.IndexOf(Path.DirectorySeparatorChar);
                    string subS = pathUnwinder.Substring(0, NextSlashIndex - 1);
                    rootNavi = rootNavi.Find(subS);
                    if (rootNavi == null)
                        throw new InvalidOperationException("GameObject \"" + Name + 
                            "\" is part of a hierarchy, but it appears to be incomplete at GameObject\"" +
                            subS + "\"");
                    pathUnwinder.Remove(0, NextSlashIndex);
                }
                GameObject GO = new GameObject(Name);
                Transform trans = GO.transform;
                trans.SetParent(rootNavi);
                trans.localPosition = Position;
                trans.localRotation = Rotation;
                trans.localScale = Scale;
                if (Mesh != null && Mesh.Length > 0)
                {
                    var case1 = GO.AddComponent<MeshFilter>();
#if EDITOR
                    case1.sharedMesh = GetMeshFromModAssetBundle(Mesh);
#else
                    case1.sharedMesh = IterateAllModAssetsBundle<Mesh>(Mesh).First().Value;
#endif
                }
                if (Texture != null && Texture.Length > 0)
                {
                    var case1 = GO.AddComponent<MeshRenderer>();
#if EDITOR
                    GameObject preview = GameObject.Find("GSO_ExampleTech");
                    if (preview == null)
                        preview = GameObject.Find("SkinPreview_GSO");
                    var testMaterial = new Material(preview.GetComponent<MeshRenderer>().sharedMaterial);
                    testMaterial.SetTexture("_MainTex", GetTextureFromModAssetBundle(Texture));
#else
                    var testMaterial = new Material(GetMaterialFromBaseGame("GSO_Main"));
                    testMaterial.SetTexture("_MainTex", IterateAllModAssetsBundle<Texture2D>(Texture).First().Value);
#endif
                    case1.sharedMaterial = testMaterial;
                }
                return GO;
            }
            private string RecurseHierachyStacker(Transform trans)
            {
                if (trans == null)
                    return string.Empty;
                string next = RecurseHierachyStacker(trans);
                if ( next != string.Empty)
                    return trans.name + Path.DirectorySeparatorChar + next;
                else 
                    return trans.name;
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
                GUILayout.Box("--- Resources --- ");
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
