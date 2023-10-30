using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace TerraTechETCUtil
{
    public static class CursorChangeHelper
    {
        public static bool ShowDebug = false;

        /// <summary>
        /// Index, (Cursor base texture, Overwritten texture)
        /// </summary>
        private static Dictionary<int, KeyValuePair<Texture2D, Texture2D>> CursorTextureSwapBackup = new Dictionary<int, KeyValuePair<Texture2D, Texture2D>>();



        /// <summary>
        /// Get the CursorChangeCache registered
        /// </summary>
        /// <param name="DLLDirectory">DLL Directory in disk.  Should be gathered on game startup and not preset.</param>
        /// <param name="MC">ModContainer of the mod to look into for assets.  Use ResourcesHelper.TryGetModContainer() to get the mod container.</param>
        /// <param name="cursorsByNameInOrder">Name of each cursor to add. Will be registered in a lookup in CursorIndexCache</param>
        public static CursorChangeCache GetCursorChangeCache(string DLLDirectory, string IconsFolderName, ModContainer MC, params string[] cursorsByNameInOrder)
        {
            return new CursorChangeCache(DLLDirectory, IconsFolderName, MC, cursorsByNameInOrder);
        }

        public static CursorChangeCache GetCursorChangeCache<T>(string DLLDirectory, string IconsFolderName, ModContainer MC) where T : Enum
        {
            return new CursorChangeCache<T>(DLLDirectory, IconsFolderName, MC);
        }

        public class CursorChangeCache<T> : CursorChangeCache where T : Enum
        {
            private Dictionary<T, int> CursorIndexCacheEnumLookup;

            internal CursorChangeCache(string DLLDirectory, string IconsFolderName, ModContainer MC) :
                base(DLLDirectory, IconsFolderName, MC, Enum.GetNames(typeof(T))) 
            {
                if (Enum.GetUnderlyingType(typeof(T)) != typeof(int))
                    throw new InvalidCastException("CursorChangeCache cannot be created for enums that aren't of base type int");

                CursorIndexCacheEnumLookup = new Dictionary<T, int>();
                foreach (var item in Enum.GetValues(typeof(T)))
                {
                    CursorIndexCacheEnumLookup.Add((T)item, (int)item);
                }
            }
            public GameCursor.CursorState GetCursor(T index)
            {
                try
                {
                    return _CursorIndexCache[CursorIndexCacheEnumLookup[index]];
                }
                catch (Exception e)
                {
                    throw e;
                }
            }
        }


        public class CursorChangeCache
        {
            /// <summary>
            /// Look up the index in this to get the actual index of the cursor you are going to use from memory
            /// </summary>
            protected GameCursor.CursorState[] _CursorIndexCache;

            public GameCursor.CursorState this[int i]
            {
                get { return _CursorIndexCache[i]; }
            }
            public CursorChangeCache CursorIndexCache => this;
            private List<Texture2D> CursorTextureCache;

            /// <summary>
            /// Index, (Cursor base texture, Overwritten texture)
            /// </summary>
            /// <summary>
            /// Get the CursorChangeCache registered
            /// </summary>
            /// <param name=""></param>
            internal CursorChangeCache(string DLLDirectory, string IconsFolderName, ModContainer MC, string[] cursorsByNameInOrder)
            {
                _CursorIndexCache = new GameCursor.CursorState[cursorsByNameInOrder.Length];
                CursorTextureCache = new List<Texture2D>(cursorsByNameInOrder.Length);
                AddNewCursors(MC, DLLDirectory, IconsFolderName, cursorsByNameInOrder);
            }
            public GameCursor.CursorState GetCursor(int index)
            {
                try
                {
                    return _CursorIndexCache[index];
                }
                catch (Exception e)
                {
                    throw e;
                }
            }


            protected void AddNewCursors(ModContainer MC, string DLLDirectory, string IconsFolderName, string[] cursorsByNameOrder)
            {
                MousePointer MP = UnityEngine.Object.FindObjectOfType<MousePointer>();
                if (!MP)
                {
                    if (ShowDebug)
                        Debug_TTExt.Assert("CursorChangeCache: AddNewCursors - THE CURSOR DOES NOT EXIST!");
                    else
                        return;
                }
                string DirectoryTarget = Path.Combine(DLLDirectory, IconsFolderName);
                Debug_TTExt.LogDevOnly("CursorChangeCache: AddNewCursors - Path: " + DirectoryTarget);
                try
                {
                    int LODLevel = 0;
                    CursorDataTable.CursorDataSet[] cursorLODs = ManUI.inst.CursorDataTable.
                    PlatformSets[CursorDataTable.PlatformSetTypes.PC].m_DataSets;
                    foreach (var item in cursorLODs)
                    {
                        List<CursorDataTable.CursorData> cursorTypes = item.m_CursorData.ToList();

                        int cursorIndex = 0;
                        foreach (var item2 in cursorsByNameOrder)
                        {
                            TryAddNewCursor(MC, cursorTypes, DirectoryTarget, item2, LODLevel, Vector2.zero, cursorIndex);
                            cursorIndex++;
                        }

                        item.m_CursorData = cursorTypes.ToArray();
                    }
                }
                catch (Exception e) { Debug_TTExt.Log("CursorChangeCache: AddNewCursors - failed to fetch rest of cursor textures " + e); }
            }

            protected void TryAddNewCursor(ModContainer MC, List<CursorDataTable.CursorData> lodInst, string DLLDirectory, string name, int lodLevel, Vector2 center, int cacheIndex)
            {
                if (ShowDebug)
                    Debug_TTExt.Log("CursorChangeCache: AddNewCursors - " + DLLDirectory + " for " + name + " " + lodLevel + " " + center);
                try
                {
                    Texture2D tex;
                    try
                    {
                        tex = ResourcesHelper.FetchTexture(MC, name + lodLevel + ".png", DLLDirectory);
                        if (tex == null)
                            throw new NullReferenceException();
                    }
                    catch
                    {
                        Debug_TTExt.Info("CursorChangeCache: AddNewCursors - failed to fetch cursor texture LOD " + lodLevel + " for " + name + "\n" +
                            "Attempting to load from files instead");
                        tex = FileUtils.LoadTexture(new DirectoryInfo(DLLDirectory).GetFiles().FirstOrDefault(delegate (FileInfo cand)
                        { return cand.Name == name + "1.png"; }).ToString());
                        CursorTextureCache.Add(tex);
                    }
                    CursorDataTable.CursorData CD = new CursorDataTable.CursorData
                    {
                        m_Hotspot = center * tex.width,
                        m_Texture = tex,
                    };
                    lodInst.Add(CD);
                    _CursorIndexCache[cacheIndex] = (GameCursor.CursorState)lodInst.IndexOf(CD);
                    Debug_TTExt.Info(name + " center: " + CD.m_Hotspot.x + "|" + CD.m_Hotspot.y);
                }
                catch { Debug_TTExt.Assert(true, "CursorChangeCache: AddNewCursors - failed to fetch cursor texture " + name); }
            }


            public void ChangeMiniIcon(int cacheIndex, IntVector2 toAddOffset, Texture2D toAdd)
            {
                try
                {
                    if (CursorTextureSwapBackup.TryGetValue(cacheIndex, out KeyValuePair<Texture2D, Texture2D> tex))
                    {
                        Texture2D oldTex = tex.Key;
                        if (toAdd != tex.Value)
                        {
                            ApplyTextureDeltaAdditive(cacheIndex, toAddOffset, oldTex, toAdd);
                            CursorTextureSwapBackup.Remove(cacheIndex);
                            CursorTextureSwapBackup.Add(cacheIndex, new KeyValuePair<Texture2D, Texture2D>(oldTex, toAdd));
                        }
                    }
                    else
                    {
                        Texture2D oldTex = CursorTextureCache[cacheIndex];
                        Texture2D backupTex = new Texture2D(oldTex.width, oldTex.height, oldTex.format, oldTex.mipmapCount > 1);
                        Graphics.CopyTexture(oldTex, backupTex);
                        ApplyTextureDeltaAdditive(cacheIndex, toAddOffset, backupTex, toAdd);
                        CursorTextureSwapBackup.Add(cacheIndex, new KeyValuePair<Texture2D, Texture2D>(backupTex, toAdd));
                    }
                }
                catch (Exception e) { Debug_TTExt.Log("CursorChangeCache: ChangeMiniIcon - failed to change " + e); }
            }
            protected void ApplyTextureDeltaAdditive(int cacheIndex, IntVector2 toAddOffset, Texture2D baseTex, Texture2D toAdd)
            {
                Texture2D toAddTo = CursorTextureCache[cacheIndex];
                if (toAddTo.width != baseTex.width || toAddTo.height != baseTex.height)
                    Debug_TTExt.FatalError("CursorChangeCache: ApplyTextureDeltaAdditive - Mismatch in toAddTo and baseTex dimensions!");
                for (int xStep = 0; xStep < toAddTo.width; xStep++)
                {
                    for (int yStep = 0; yStep < toAddTo.height; yStep++)
                    {
                        Color applyColor = baseTex.GetPixel(xStep, yStep);
                        Color applyAdd = toAdd.GetPixel(xStep + toAddOffset.x, yStep + toAddOffset.y);
                        if (applyAdd.a > 0.05f)
                        {
                            applyColor.r = Mathf.Clamp01(applyColor.r + applyAdd.r);
                            applyColor.g = Mathf.Clamp01(applyColor.g + applyAdd.g);
                            applyColor.b = Mathf.Clamp01(applyColor.b + applyAdd.b);
                            applyColor.a = Mathf.Clamp01(applyColor.a + applyAdd.a);
                        }
                        CursorTextureCache[cacheIndex].SetPixel(xStep, yStep, applyColor);
                    }
                }
                CursorTextureCache[cacheIndex].Apply();
            }

        }
    }
}
