﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TerraTechETCUtil
{
    public class GUILayoutHelpers
    {
        private interface SlowSorter { }
        public class SlowSorter<T> : SlowSorter where T : UnityEngine.Object
        {
            public static readonly SlowSorter<T> Default = new SlowSorter<T>(16);
            private bool updating;
            private bool showDuplicates;
            private T[] arrayCache;
            private string name;
            private int step;
            private int stepRate;
            private HashSet<T> Iterated;
            private Dictionary<string, int> names;
            public List<string> namesValid;
            public SlowSorter(int iterations)
            {
                stepRate = iterations;
                updating = false;
                arrayCache = null;
                Iterated = new HashSet<T>();
                names = new Dictionary<string, int>();
                namesValid = new List<string>();
                this.name = "";
                step = 0;
            }

            public void SetNewSearchQueryIfNeeded(string query, bool allowDupes)
            {
                if (query.ToLower().Equals(name) && showDuplicates == allowDupes)
                    return;
                showDuplicates = allowDupes;
                name = query.ToLower();
                Abort(false);
                StartUpdate();
            }
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
                    InvokeHelper.Invoke(Update, 0.04f);
                    updating = true;
                }
            }

            public void Abort(bool clearArrayCache = true)
            {
                step = 0;
                Iterated.Clear();
                names.Clear();
                namesValid.Clear();
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
                    if (Iterated.Add(item))
                    {
                        if (names.TryGetValue(item.name, out int stepName))
                        {
                            if (showDuplicates)
                            {
                                if (item.name.ToLower().Contains(name))
                                {
                                    namesValid.Add(item.name + "(" + stepName + ")");
                                    step++;
                                    names[item.name] = stepName + 1;
                                    continue;
                                }
                                names[item.name] = stepName + 1;
                            }
                        }
                        else
                        {
                            names.Add(item.name, 1);
                            if (item.name.ToLower().Contains(name))
                            {
                                namesValid.Add(item.name);
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
                    InvokeHelper.Invoke(Update, 0.04f);
            }
        }
        public struct SlowSorterString : SlowSorter
        {
            private bool updating;
            private string searchQuery;
            private int curStep;
            private string[] names;
            public List<string> namesSorted;
            private readonly int stepSpeed;

            public SlowSorterString(int searchIterationsPerUpdate = 12)
            {
                curStep = 0;
                stepSpeed = searchIterationsPerUpdate;
                namesSorted = new List<string>();
                searchQuery = "";
                names = new string[0];
                updating = false;
            }
            public void SetNewSearchQueryIfNeeded(string query)
            {
                if (query.ToLower() == searchQuery)
                    return;
                searchQuery = query.ToLower();
                curStep = 0;
                namesSorted.Clear();
                StartUpdate();
            }
            public void SetNames(string[] namesArray)
            {
                names = namesArray;
                curStep = 0;
                namesSorted.Clear(); 
                StartUpdate();
            }
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
        public static void GUILabelDispFast(string fieldName, int num)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(fieldName);
            GUILayout.Label(num.ToString());
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
        public static void GUILabelDispFast(string fieldName, float num)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(fieldName);
            GUILayout.Label(num.ToString());
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
        public static void GUITextFieldDispFast(string fieldName, ref string input)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(fieldName);
            input = GUILayout.TextField(input, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();
        }
        public static bool GUITextFieldDisp(string fieldName, ref string input)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(fieldName);
            string inputC = input;
            input = GUILayout.TextField(input, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();
            return inputC.CompareTo(input) != 0;
        }
        public static int GUIVertToolbar(int enabledOption, string[] strings)
        {
            int posStep = 0;
            foreach (var item in strings)
            {
                if (GUILayout.Toggle(posStep == enabledOption, item))
                {
                    enabledOption = posStep;
                }
                posStep++;
            }
            return enabledOption;
        }
        public static int GUIVertToolbar(int enabledOption, List<string> strings)
        {
            int posStep = 0;
            foreach (var item in strings)
            {
                if (GUILayout.Toggle(posStep == enabledOption, item))
                {
                    enabledOption = posStep;
                }
                posStep++;
            }
            return enabledOption;
        }
        public static int GUIToolbarCategoryDisp<T>(int enabledOption) where T : Enum
        {
            return GUILayout.Toolbar(enabledOption, Enum.GetNames(typeof(T)), GUI.skin.button, buttonSize: GUI.ToolbarButtonSize.FitToContents);
        }
        public static int GUIVertToolbarCategoryDisp<T>(int enabledOption) where T : Enum
        {
            GUILayout.BeginHorizontal();
            int outp = GUILayout.Toolbar(enabledOption, Enum.GetNames(typeof(T)), GUI.skin.button, buttonSize: GUI.ToolbarButtonSize.FitToContents);
            GUILayout.EndHorizontal();
            return outp;
        }
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
        public static void GUICategoryDisp<T>(ref HashSet<string> enabledTabs, string name, Action<T> enumTypeAct) where T : Enum
        {
            if (GUILayout.Button(name + " | Total: " + Enum.GetValues(typeof(ManSFX.MiscSfxType)).Length))
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
    }
}
