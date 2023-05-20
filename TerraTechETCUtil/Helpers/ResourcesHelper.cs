using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Text;
using UnityEngine;

namespace TerraTechETCUtil
{
    public class ResourcesHelper
    {
        public static bool ShowDebug = false;
        public static string Up
        {
            get
            {
                if (up == null)
                {
                    if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.MacOSX)
                        up = "/";
                    else
                        up = "\\";
                }
                return up;
            }
        }
        private static string up = null;

        private static void Init()
        {
            if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.MacOSX)
            {
                up = "/";
            }
        }

        public static bool TryGetModContainer(string modName, out ModContainer MC)
        {
            MC = ManMods.inst.FindMod(modName);
            return MC != null;
        }
        public static void LookIntoModContents(ModContainer MC)
        {
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
                Debug_TTExt.Log(e);
            }
        }

        /// <summary>
        /// Make sure the Mod AssetBundle is loaded first!
        /// </summary>
        public static Mesh GetMeshFromModAssetBundle(ModContainer MC, string nameNoExt, bool complainWhenFail = true)
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
        public static Texture2D GetTextureFromModAssetBundle(ModContainer MC, string nameNoExt, bool complainWhenFail = true, bool testIconsFolder = false)
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
                string targ = tempDirect + "\\" + nameNoExt + ".png";
                if (File.Exists(targ))
                    tex = FileUtils.LoadTexture(targ);
            }
            if (complainWhenFail)
                Debug_TTExt.Assert(tex == null, nameNoExt + ".png could not be found!");
            return tex;
        }

        public static Texture2D GetTexture2DFromBaseGame(string MaterialName, bool complainWhenFail = true)
        {
            var res = UnityEngine.Object.FindObjectsOfType<Texture2D>();
            Texture2D mat = res.ToList().Find(delegate (Texture2D cand) { return cand.name.Equals(MaterialName); });
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
            Material mat = res.ToList().Find(delegate (Material cand) { return cand.name.Equals(MaterialName); });
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
                    string destination = DLLDirectory + "\\" + "AI_Icons" + "\\" + pngName;
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
            ModTexture,
            IngameTexture,
            ModContainer,
            ETC_Utils
        }
        internal class GUIManaged : GUILayoutHelpers
        {
            private static bool controlledDisp = false;
            private static HashSet<string> enabledTabs = null;
            private static ResourceHelperGUITypes guiType = ResourceHelperGUITypes.ModTexture;
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
                                GUILayout.Box("TerraTechETCUtils within mods");
                                CurMods = (Dictionary<string, ModContainer>)ModContainerGet.GetValue(ManMods.inst);
                                if (CurMods != null)
                                {
                                    FileVersionInfo FVIC = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
                                    if (FVIC == null)
                                        throw new NullReferenceException("TerraTechETCUtil does NOT have version info for some reason!");

                                    GUILayout.Box("Current Version: " + FVIC.FileVersion);
                                    foreach (var item in CurMods.Values)
                                    {
                                        string filePos = (new DirectoryInfo(item.AssetBundlePath).Parent.ToString()) + Up + "TerraTechETCUtil.dll";
                                        if (File.Exists(filePos))
                                        {
                                            GUILayout.BeginHorizontal();
                                            GUILayout.Label(item.Contents.ModName);
                                            GUILayout.FlexibleSpace();
                                            FileVersionInfo FVI = FileVersionInfo.GetVersionInfo(filePos);
                                            if (FVI != null)
                                                GUILayout.Label("Version: " + FVI.FileVersion);
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
                    catch { }
                }
            }


            private static FieldInfo ModContainerGet = typeof(ManMods).GetField("m_Mods", BindingFlags.NonPublic | BindingFlags.Instance);
            private static Dictionary<string, ModContainer> CurMods = null;
            private static string MCName = "";
            private static ModContainer ModContainerInst = null;
            private static bool showModContainers = false;
            private static Dictionary<string, ModdedAsset> content = null;
            private static Dictionary<string, ModdedAsset> contentNONE = null;
            private static void GUIModContainers(ref bool deltaed)
            {
                if (GUITextFieldDisp("ModContainer Name:", ref MCName))
                    deltaed = true;
                showModContainers = GUILayout.Toggle(showModContainers, "Get Active ModContainers");
                if (showModContainers)
                {
                    int select = -1;
                    CurMods = (Dictionary<string, ModContainer>)ModContainerGet.GetValue(ManMods.inst);
                    if (CurMods != null)
                    {
                        select = GUIVertToolbar(select, CurMods.Values.Select(x => x.Contents.ModName).ToArray());
                        if (select != -1)
                        {
                            MCName = CurMods.ElementAt(select).Value.Contents.ModName;
                            deltaed = true;
                            showModContainers = false;
                        }
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
                    showModTexs = GUILayout.Toggle(showModTexs, "Show Active Mod Textures");
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
            private static SlowSorter<Material> slowSort = new  SlowSorter<Material>(64);
            private static bool showAllMats = false;
            private static bool hideDuplicates = true;
            private static void GUIIngameMaterials(ref bool deltaed)
            {
                GUILayout.Box("Ingame Materials");
                if (GUITextFieldDisp("Material Name:", ref MatName))
                    deltaed = true;
                showAllMats = GUILayout.Toggle(showAllMats, "Show Active Materials");
                if (showAllMats)
                {
                    bool prevHideDupes = hideDuplicates;
                    hideDuplicates = GUILayout.Toggle(hideDuplicates, "[Filter Out Duplicates]");
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
    }
}
