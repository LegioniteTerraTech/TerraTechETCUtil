using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Steamworks;
using UnityEngine;

namespace TerraTechETCUtil
{
    /// <summary>
    /// Helpers for <see cref="GUILayout"/>
    /// </summary>
    public class GUILayoutHelpers
    {
        /// <summary>
        /// Interface for slow sortable entries managed by <see cref="ISlowSorter"/>
        /// </summary>
        public interface ISlowSortable 
        { 
            /// <summary>
            /// The display name to sort by
            /// </summary>
            string displayName { get; }
        }
        private interface ISlowSorter { }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public class SlowSorter<T> : ISlowSorter
        {
            /// <summary>
            /// The default <see cref="SlowSorter{T}"/>
            /// </summary>
            public static readonly SlowSorter<T> Default = new SlowSorter<T>(16);
            private bool updating;
            private bool showDuplicates;
            private T[] arrayCache;
            private string name;
            private int step;
            private int stepRate;
            private Func<T, string, string, bool> selector;
            private Func<T, string, string, bool> selectorFinal;
            /// <summary>
            /// Default <see cref="SlowSorter{T}"/> search query
            /// </summary>
            /// <param name="type"></param>
            /// <param name="nameGet"></param>
            /// <param name="curName"></param>
            /// <returns></returns>
            public bool DefaultSearcher(T type, string nameGet, string curName)
            {
                return nameGet.ToLower().Contains(curName);
            }
            private HashSet<T> Iterated;
            private Dictionary<string, int> names;
            /// <summary> </summary>
            public List<string> namesValid;
            /// <summary> </summary>
            public List<T> valid;
            /// <summary>
            /// Creates a new <see cref="SlowSorter{T}"/> that sorts
            /// </summary>
            /// <param name="iterations">entries to search through with each <see cref="Update"/></param>
            public SlowSorter(int iterations)
            {
                stepRate = iterations;
                updating = false;
                arrayCache = null;
                Iterated = new HashSet<T>();
                names = new Dictionary<string, int>();
                namesValid = new List<string>();
                valid = new List<T>();
                name = "";
                step = 0;
                selector = null;
                selectorFinal = DefaultSearcher;
            }
            /// <summary>
            /// Creates a new <see cref="SlowSorter{T}"/> that sorts
            /// </summary>
            /// <param name="iterations">entries to search through with each <see cref="Update"/></param>
            /// <param name="acceptor"></param>
            /// <param name="filter"></param>
            public SlowSorter(int iterations, Func<T, string, string, bool> acceptor = null,
                Func<T, string, string, bool> filter = null)
            {
                stepRate = iterations;
                updating = false;
                arrayCache = null;
                Iterated = new HashSet<T>();
                names = new Dictionary<string, int>();
                namesValid = new List<string>();
                valid = new List<T>();
                name = "";
                step = 0;
                selector = acceptor;
                if (filter == null)
                    selectorFinal = DefaultSearcher;
                else 
                    selectorFinal = filter;
            }
            /// <summary>
            /// 
            /// </summary>
            /// <param name="query"></param>
            /// <param name="allowDupes"></param>
            public void SetNewSearchQueryIfNeeded(string query, bool allowDupes)
            {
                if (query.ToLower().Equals(name) && showDuplicates == allowDupes)
                    return;
                showDuplicates = allowDupes;
                name = query.ToLower();
                Abort(false);
                StartUpdate();
            }
            /// <summary>
            /// 
            /// </summary>
            /// <param name="namesArray"></param>
            /// <param name="query"></param>
            /// <param name="allowDupes"></param>
            /// <exception cref="NullReferenceException"></exception>
            public void SetSearchArrayAndSearchQuery(T[] namesArray, string query, bool allowDupes)
            {
                if (namesArray == null)
                    throw new NullReferenceException("SetSearchArray - namesArray cannot be null");
                showDuplicates = allowDupes;
                name = query.ToLower();
                Abort();
                arrayCache = namesArray;
                StartUpdate();
            }

            private void StartUpdate()
            {
                if (!updating)
                {
                    InvokeHelper.Invoke(Update, 0);
                    updating = true;
                }
            }

            /// <summary>
            /// Cancels the search
            /// </summary>
            /// <param name="clearArrayCache"></param>
            public void Abort(bool clearArrayCache = true)
            {
                step = 0;
                Iterated.Clear();
                names.Clear();
                namesValid.Clear();
                valid.Clear();
                if (clearArrayCache)
                    arrayCache = null;
                InvokeHelper.CancelInvoke(Update);
                updating = false;
            }
            private void Update()
            {
                int deltaStep = Mathf.Min(step + stepRate, arrayCache.Length);
                while (step < deltaStep)
                {
                    var item = arrayCache[step];
                    string nameGet;
                    if (item is UnityEngine.Object obj)
                        nameGet = obj.name;
                    else if (item is ISlowSortable sortable)
                        nameGet = sortable.displayName;
                    else
                        throw new InvalidCastException("SlowSorter must have a type that is either UnityEngine.Object or SlowSortable");
                    if (Iterated.Add(item) && (selector == null || selector(item, nameGet, name)))
                    {
                        if (names.TryGetValue(nameGet, out int stepName) && showDuplicates)
                        {
                            if (selectorFinal(item, nameGet, name))
                            {
                                namesValid.Add(nameGet + "(" + stepName + ")");
                                valid.Add(item);
                                step++;
                                names[nameGet] = stepName + 1;
                                continue;
                            }
                            names[nameGet] = stepName + 1;
                        }
                        else
                        {
                            names.Add(nameGet, 1);
                            if (selectorFinal(item, nameGet, name))
                            {
                                namesValid.Add(nameGet);
                                valid.Add(item);
                                step++;
                                continue;
                            }
                        }
                    }
                    step++;
                }
                if (step >= arrayCache.Length)
                {
                    updating = false;
                }
                else
                    InvokeHelper.Invoke(Update, 0);
            }
        }
        /// <summary>
        /// <inheritdoc/>
        /// Slow sorter for strings
        /// </summary>
        public struct SlowSorterString : ISlowSorter
        {
            private bool updating;
            private string searchQuery;
            private int curStep;
            private string[] names;
            /// <summary>
            /// All names that this has sorted
            /// </summary>
            public List<string> namesSorted;
            private readonly int stepSpeed;

            /// <summary>
            /// Creates a new <see cref="SlowSorterString"/> that sorts
            /// </summary>
            /// <param name="searchIterationsPerUpdate">entries to search through with each <see cref="Update"/></param>
            public SlowSorterString(int searchIterationsPerUpdate = 12)
            {
                curStep = 0;
                stepSpeed = searchIterationsPerUpdate;
                namesSorted = new List<string>();
                searchQuery = "";
                names = new string[0];
                updating = false;
            }
            /// <summary>
            /// 
            /// </summary>
            /// <param name="query"></param>
            public void SetNewSearchQueryIfNeeded(string query)
            {
                if (query.ToLower() == searchQuery)
                    return;
                searchQuery = query.ToLower();
                curStep = 0;
                namesSorted.Clear();
                StartUpdate();
            }
            /// <summary>
            /// 
            /// </summary>
            /// <param name="namesArray"></param>
            public void SetNames(string[] namesArray)
            {
                names = namesArray;
                curStep = 0;
                namesSorted.Clear(); 
                StartUpdate();
            }
            /// <summary>
            /// 
            /// </summary>
            public void Reset()
            {
                searchQuery = "";
                curStep = 0;
                namesSorted.Clear();
                updating = false;
            }
            private void StartUpdate()
            {
                if (!updating)
                {
                    InvokeHelper.Invoke(Update, 0.04f);
                    updating = true;
                }
            }

            private void Update()
            {
                if (!updating)
                    return;
                int endStep = Mathf.Min(stepSpeed + curStep, names.Length);
                while (curStep < endStep)
                {
                    if (names[curStep].ToLower().Contains(searchQuery))
                    {
                        namesSorted.Add(names[curStep]);
                    }
                    curStep++;
                }
                if (curStep == endStep)
                    updating = false;
                else
                    InvokeHelper.Invoke(Update, 0.04f);
            }
        }
        /// <summary>
        /// To display in IMGUI
        /// </summary>
        public static void GUILabelDispFast(string fieldName, int num)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(fieldName);
            GUILayout.Label(num.ToString());
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
        /// <summary>
        /// To display in IMGUI
        /// </summary>
        public static void GUILabelDispFast(string fieldName, float num)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(fieldName);
            GUILayout.Label(num.ToString());
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
        /// <summary>
        /// To display in IMGUI
        /// </summary>
        public static void GUITextFieldDispFast(string fieldName, ref string input)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(fieldName);
            input = GUILayout.TextField(input, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();
        }
        /// <summary>
        /// To display in IMGUI
        /// </summary>
        public static bool GUITextFieldDisp(string fieldName, ref string input)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(fieldName);
            string inputC = input;
            input = GUILayout.TextField(input, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();
            return inputC.CompareTo(input) != 0;
        }
        /// <summary>
        /// To display in IMGUI
        /// </summary>
        public static int GUIVertToolbar(int enabledOption, string[] strings)
        {
            int posStep = 0;
            foreach (var item in strings)
            {
                if (AltUI.Toggle(posStep == enabledOption, item))
                {
                    enabledOption = posStep;
                }
                posStep++;
            }
            return enabledOption;
        }
        /// <summary>
        /// To display in IMGUI
        /// </summary>
        public static int GUIVertToolbar(int enabledOption, List<string> strings)
        {
            int posStep = 0;
            foreach (var item in strings)
            {
                if (AltUI.Toggle(posStep == enabledOption, item))
                {
                    enabledOption = posStep;
                }
                posStep++;
            }
            return enabledOption;
        }
        /// <summary>
        /// To display in IMGUI
        /// </summary>
        public static int GUIToolbarCategoryDisp<T>(int enabledOption) where T : Enum
        {
            return GUILayout.Toolbar(enabledOption, Enum.GetNames(typeof(T)), GUI.skin.button, buttonSize: GUI.ToolbarButtonSize.FitToContents);
        }
        /// <summary>
        /// To display in IMGUI
        /// </summary>
        public static int GUIVertToolbarCategoryDisp<T>(int enabledOption) where T : Enum
        {
            GUILayout.BeginHorizontal();
            int outp = GUILayout.Toolbar(enabledOption, Enum.GetNames(typeof(T)), GUI.skin.button, buttonSize: GUI.ToolbarButtonSize.FitToContents);
            GUILayout.EndHorizontal();
            return outp;
        }
        /// <summary>
        /// To display in IMGUI
        /// </summary>
        public static bool GUITabDisp(ref HashSet<string> enabledTabs, string name)
        {
            if (GUILayout.Button(name))
            {
                if (enabledTabs.Contains(name))
                    enabledTabs.Remove(name);
                else
                    enabledTabs.Add(name);
            }
            return enabledTabs.Contains(name);
        }
        /// <summary>
        /// To display in IMGUI
        /// </summary>
        public static void GUICategoryDisp<T>(ref HashSet<string> enabledTabs, string name, Action<T> enumTypeAct) where T : Enum
        {
            if (GUILayout.Button(name + " | Total: " + Enum.GetValues(typeof(T)).Length))
            {
                if (enabledTabs.Contains(name))
                    enabledTabs.Remove(name);
                else
                    enabledTabs.Add(name);
            }
            if (enabledTabs.Contains(name))
            {
                foreach (T item in Enum.GetValues(typeof(T)))
                {
                    if (GUILayout.Button(" Type: " + item.ToString()))
                    {
                        enumTypeAct.Invoke(item);
                    }
                }
            }
        }
        /// <summary>
        /// To display in IMGUI
        /// </summary>
        public static void GUICategoryDisp<T>(ref HashSet<string> enabledTabs, string name, Action<T> enumTypeAct, Func<T, bool> selector)
            where T : Enum
        {
            if (GUILayout.Button(name + " | Total: " + Enum.GetValues(typeof(T)).Length))
            {
                if (enabledTabs.Contains(name))
                    enabledTabs.Remove(name);
                else
                    enabledTabs.Add(name);
            }
            if (enabledTabs.Contains(name))
            {
                foreach (T item in Enum.GetValues(typeof(T)))
                {
                    if (selector(item) && GUILayout.Button(" Type: " + item.ToString()))
                    {
                        enumTypeAct.Invoke(item);
                    }
                }
            }
        }
    }
}
