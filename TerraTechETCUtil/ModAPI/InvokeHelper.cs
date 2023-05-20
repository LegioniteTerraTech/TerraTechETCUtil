﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace TerraTechETCUtil
{
    /// <summary>
    /// Do not use unless absolutely nesseary - MonoBehaviour.Invoke is safer
    /// </summary>
    public class InvokeHelper : MonoBehaviour
    {
        private static InvokeHelper inst;
        internal struct InvokeRepeater
        {
            internal float nextTime;
            internal float delay;
            internal Action toInvoke;
        }
        private Dictionary<MethodInfo, InvokeRepeater> invokeSingleRepeat = new Dictionary<MethodInfo, InvokeRepeater>();
        private Dictionary<MethodInfo, IInvokeable> invokeSingles = new Dictionary<MethodInfo, IInvokeable>();
        private Dictionary<MethodInfo, List<IInvokeable>> invokes = new Dictionary<MethodInfo, List<IInvokeable>>();
        private static void InsureInit()
        {
            if (inst)
                return;
            Debug_TTExt.Log("Debug_TTExt: InvokeHelper.InsureInit()");
            var logMan = new GameObject("invokeHelper");
            inst = logMan.AddComponent<InvokeHelper>();
            logMan.SetActive(true);
            inst.enabled = true;
        }

        /// <summary>
        /// Invoke a MethodInfo-level instance repeatedly of the given Action after a set delay. 
        /// Will overwrite previous MethodInfo invoke requests.
        /// </summary>
        /// <param name="act">The Method to invoke after delay time.</param>
        /// <param name="delay">The delay before we invoke.  
        /// Note this will not invoke immedeately when the time is passed.</param>
        public static void InvokeSingleRepeat(Action act, float delay)
        {
            InsureInit();
            if (inst.invokeSingleRepeat.TryGetValue(act.Method, out _))
            {
                inst.invokeSingleRepeat.Remove(act.Method);
            }
            inst.invokeSingleRepeat.Add(act.Method, new InvokeRepeater
            {
                nextTime = Time.time + delay,
                delay = delay,
                toInvoke = act,

            });
        }
        /// <summary>
        /// Cancels any MethodInfo-level of invoke request set.
        /// </summary>
        /// <param name="act">The Method to cancel the MethodInfo-level invoke for.</param>
        public static void CancelInvokeSingleRepeat(Action act)
        {
            if (!inst)
                return;
            inst.invokeSingleRepeat.Remove(act.Method);
        }


        /// <summary>
        /// Invoke only ONE MethodInfo-level instance of the given Action after a set delay. 
        /// Will overwrite previous MethodInfo invoke requests.
        /// </summary>
        /// <param name="act">The Method to invoke after delay time.</param>
        /// <param name="delay">The delay before we invoke.  
        /// Note this will not invoke immedeately when the time is passed.</param>
        /// <param name="ForceSet">To force set the Single's delay again.</param>
        public static void InvokeSingle(Action act, float delay, bool ForceSet = false)
        {
            InsureInit();
            if (inst.invokeSingles.TryGetValue(act.Method, out _))
            {
                if (ForceSet)
                {
                    inst.invokeSingles.Remove(act.Method);
                    inst.invokeSingles.Add(act.Method, new Invokeable(act, Time.time + delay));
                }
            }
            else
                inst.invokeSingles.Add(act.Method, new Invokeable(act, Time.time + delay));
        }
        /// <summary>
        /// Invoke only ONE MethodInfo-level instance of the given Action after a set delay. 
        /// Will overwrite previous MethodInfo invoke requests.
        /// </summary>
        /// <param name="act">The Method to invoke after delay time.</param>
        /// <param name="delay">The delay before we invoke.  
        /// Note this will not invoke immedeately when the time is passed.</param>
        /// <param name="in1">Invoke param 1.</param>
        /// <param name="ForceSet">To force set the Single's delay again.</param>
        public static void InvokeSingle<T>(Action<T> act, float delay, T in1, bool ForceSet = false)
        {
            InsureInit();
            if (inst.invokeSingles.TryGetValue(act.Method, out _))
            {
                if (ForceSet)
                {
                    inst.invokeSingles.Remove(act.Method);
                    inst.invokeSingles.Add(act.Method,
                        new Invokeable<T>(act, Time.time + delay, in1));
                }
            }
            else
                inst.invokeSingles.Add(act.Method,
                    new Invokeable<T>(act, Time.time + delay, in1));
        }
        /// <summary>
         /// Invoke only ONE MethodInfo-level instance of the given Action after a set delay. 
         /// Will overwrite previous MethodInfo invoke requests.
         /// </summary>
         /// <param name="act">The Method to invoke after delay time.</param>
         /// <param name="delay">The delay before we invoke.  
         /// Note this will not invoke immedeately when the time is passed.</param>
         /// <param name="in1">Invoke param 1.</param>
         /// <param name="in2">Invoke param 2.</param>
         /// <param name="ForceSet">To force set the Single's delay again.</param>
        public static void InvokeSingle<T, V>(Action<T, V> act, float delay, T in1, V in2, bool ForceSet = false)
        {
            InsureInit();
            if (inst.invokeSingles.TryGetValue(act.Method, out _))
            {
                if (ForceSet)
                {
                    inst.invokeSingles.Remove(act.Method);
                    inst.invokeSingles.Add(act.Method,
                        new Invokeable<T, V>(act, Time.time + delay, in1, in2));
                }
            }
            else
                inst.invokeSingles.Add(act.Method,
                    new Invokeable<T, V>(act, Time.time + delay, in1, in2));
        }
        /// <summary>
        /// Invoke only ONE MethodInfo-level instance of the given Action after a set delay. 
        /// Will overwrite previous MethodInfo invoke requests.
        /// </summary>
        /// <param name="act">The Method to invoke after delay time.</param>
        /// <param name="delay">The delay before we invoke.  
        /// Note this will not invoke immedeately when the time is passed.</param>
        /// <param name="in1">Invoke param 1.</param>
        /// <param name="in2">Invoke param 2.</param>
        /// <param name="in3">Invoke param 3.</param>
        /// <param name="ForceSet">To force set the Single's delay again.</param>
        public static void InvokeSingle<T, V, R>(Action<T, V, R> act, float delay, T in1, V in2, R in3, bool ForceSet = false)
        {
            InsureInit();
            if (inst.invokeSingles.TryGetValue(act.Method, out _))
            {
                if (ForceSet)
                {
                    inst.invokeSingles.Remove(act.Method);
                    inst.invokeSingles.Add(act.Method,
                        new Invokeable<T, V, R>(act, Time.time + delay, in1, in2, in3));
                }
            }
            else
                inst.invokeSingles.Add(act.Method,
                    new Invokeable<T, V, R>(act, Time.time + delay, in1, in2, in3));
        }

        /// <summary>
        /// Cancels any MethodInfo-level of invoke request set.
        /// </summary>
        /// <param name="act">The Method to cancel the MethodInfo-level invoke for.</param>
        public static void CancelInvokeSingle(Action act)
        {
            if (!inst)
                return;
            inst.invokeSingles.Remove(act.Method);
        }
        /// <summary>
        /// Cancels any MethodInfo-level of invoke request set.
        /// </summary>
        /// <param name="act">The Method to cancel the MethodInfo-level invoke for.</param>
        public static void CancelInvokeSingle<T>(Action<T> act)
        {
            if (!inst)
                return;
            inst.invokeSingles.Remove(act.Method);
        }
        /// <summary>
        /// Cancels any MethodInfo-level of invoke request set.
        /// </summary>
        /// <param name="act">The Method to cancel the MethodInfo-level invoke for.</param>
        public static void CancelInvokeSingle<T, V>(Action<T, V> act)
        {
            if (!inst)
                return;
            inst.invokeSingles.Remove(act.Method);
        }
        /// <summary>
        /// Cancels any MethodInfo-level of invoke request set.
        /// </summary>
        /// <param name="act">The Method to cancel the MethodInfo-level invoke for.</param>
        public static void CancelInvokeSingle<T, V, R>(Action<T, V, R> act)
        {
            if (!inst)
                return;
            inst.invokeSingles.Remove(act.Method);
        }

        /// <summary>
        /// Invoke the given instance of the given Action after a set delay.
        /// </summary>
        /// <param name="act">The Method to invoke after delay time.</param>
        /// <param name="delay">The delay before we invoke.  
        /// Note this will not invoke immedeately when the time is passed.</param>
        public static void Invoke(Action act, float delay)
        {
            InsureInit();
            inst.invokes.AddInlined(act.Method, (IInvokeable)new Invokeable(act, Time.time + delay));
        }
        /// <summary>
        /// Invoke the given instance of the given Action after a set delay.
        /// </summary>
        /// <param name="act">The Method to invoke after delay time.</param>
        /// <param name="delay">The delay before we invoke.  
        /// Note this will not invoke immedeately when the time is passed.</param>
        /// <param name="in1">Invoke param 1.</param>
        public static void Invoke<T>(Action<T> act, float delay, T in1)
        {
            InsureInit();
            inst.invokes.AddInlined(act.Method,
                (IInvokeable)new Invokeable<T>(act, Time.time + delay, in1));
        }
        /// <summary>
        /// Invoke the given instance of the given Action after a set delay.
        /// </summary>
        /// <param name="act">The Method to invoke after delay time.</param>
        /// <param name="delay">The delay before we invoke.  
        /// Note this will not invoke immedeately when the time is passed.</param>
        /// <param name="in1">Invoke param 1.</param>
        /// <param name="in2">Invoke param 2.</param>
        public static void Invoke<T, V>(Action<T, V> act, float delay, T in1, V in2)
        {
            InsureInit();
            inst.invokes.AddInlined(act.Method,
                (IInvokeable)new Invokeable<T, V>(act, Time.time + delay, in1, in2));
        }
        /// <summary>
        /// Invoke the given instance of the given Action after a set delay.
        /// </summary>
        /// <param name="act">The Method to invoke after delay time.</param>
        /// <param name="delay">The delay before we invoke.  
        /// Note this will not invoke immedeately when the time is passed.</param>
        /// <param name="in1">Invoke param 1.</param>
        /// <param name="in2">Invoke param 2.</param>
        /// <param name="in3">Invoke param 3.</param>
        public static void Invoke<T,V,R>(Action<T, V, R> act, float delay, T in1, V in2, R in3)
        {
            InsureInit();
            inst.invokes.AddInlined(act.Method, 
                (IInvokeable)new Invokeable<T, V, R>(act, Time.time + delay, in1, in2, in3));
        }
        
        /// <summary>
        /// Cancels the instance of invoke request set.
        /// </summary>
        /// <param name="act">The Method to cancel the invoke for.</param>
        public static void CancelInvoke(Action act) 
        {
            if (!inst)
                return;
            if (inst.invokes.TryGetValue(act.Method, out var val))
            {
                int valF = val.FindIndex(x => x.IsSameMethod(act.GetType(),act));
                if (valF != -1)
                    val.RemoveAt(valF);
            }
        }
        /// <summary>
        /// Cancels the instance of invoke request set.
        /// </summary>
        /// <param name="act">The Method to cancel the invoke for.</param>
        public static void CancelInvoke<T>(Action<T> act)
        {
            if (!inst)
                return;
            if (inst.invokes.TryGetValue(act.Method, out var val))
            {
                int valF = val.FindIndex(x => x.IsSameMethod(act.GetType(), act));
                if (valF != -1)
                    val.RemoveAt(valF);
            }
        }
        /// <summary>
        /// Cancels the instance of invoke request set.
        /// </summary>
        /// <param name="act">The Method to cancel the invoke for.</param>
        public static void CancelInvoke<T, V>(Action<T, V> act)
        {
            if (!inst)
                return;
            if (inst.invokes.TryGetValue(act.Method, out var val))
            {
                int valF = val.FindIndex(x => x.IsSameMethod(act.GetType(), act));
                if (valF != -1)
                    val.RemoveAt(valF);
            }
        }
        /// <summary>
        /// Cancels the instance of invoke request set.
        /// </summary>
        /// <param name="act">The Method to cancel the invoke for.</param>
        public static void CancelInvoke<T, V, R>(Action<T, V, R> act)
        {
            if (!inst)
                return;
            if (inst.invokes.TryGetValue(act.Method, out var val))
            {
                int valF = val.FindIndex(x => x.IsSameMethod(act.GetType(), act));
                if (valF != -1)
                    val.RemoveAt(valF);
            }
        }
        /// <summary>
        /// Print all of the currently active Invokes being run by InvokeHelper.
        /// </summary>
        public static void PrintAllActiveInvokes()
        {
            if (inst == null)
            {
                Debug_TTExt.Log("-------- InvokeHelper INACTIVE --------");
                return;
            }
            Debug_TTExt.Log("-------- InvokeHelper --------");
            Debug_TTExt.Log("TIME: " + Time.time);
            Debug_TTExt.Log("InvokeRepeating Single");
            foreach (var item in inst.invokeSingleRepeat)
            {
                Debug_TTExt.Log(item.Key + " | Delay: " + item.Value.delay + " | NextTime: " + item.Value.nextTime);
            }
            Debug_TTExt.Log("Invoke Single");
            foreach (var item in inst.invokeSingles)
            {
                Debug_TTExt.Log(item.Key + " | NextTime: " + item.Value);
            }
            Debug_TTExt.Log("Invokes");
            foreach (var item in inst.invokes)
            {
                Debug_TTExt.Log(item.Key + " | Times: ");
                foreach (var item2 in item.Value)
                {
                    Debug_TTExt.Log(" - " + item2.nextTime);
                }
            }
            Debug_TTExt.Log("-------- END --------");
        }

        public static void CancelALL()
        {
            if (!inst)
                return;
            Debug_TTExt.Log("-------- InvokeHelper.CancelALL() --------");
            inst.invokes.Clear();
            inst.invokeSingles.Clear();
            inst.invokeSingleRepeat.Clear();
        }

        private void Update()
        {
            for (int step = 0; step < invokeSingleRepeat.Count;)
            {
                var ele = invokeSingleRepeat.ElementAt(step);
                try
                {
                    if (ele.Value.nextTime > Time.time)
                    {
                        step++;
                        continue;
                    }
                    else
                    {
                        ele.Value.toInvoke.Invoke();
                        InvokeRepeater prev = invokeSingleRepeat[ele.Key];
                        invokeSingleRepeat[ele.Key] = new InvokeRepeater
                        {
                            nextTime = Time.time + prev.delay,
                            delay = prev.delay,
                            toInvoke = prev.toInvoke,
                        };
                        step++;
                    }
                }
                catch
                {
                    Debug_TTExt.Log("Debug_TTExt: InvokeHelper.invokeSingleRepeat - Error on " + ele.Key.Name + " init, aborting...");
                    invokeSingleRepeat.Remove(ele.Key);
                }
            }
            for (int step = 0; step < invokeSingles.Count;)
            {
                var ele = invokeSingles.ElementAt(step);
                try
                {
                    if (ele.Value.nextTime > Time.time)
                    {
                        step++;
                        continue;
                    }
                    else
                        ele.Value.Invoke();
                }
                catch
                {
                    Debug_TTExt.Log("Debug_TTExt: InvokeHelper.invokeSingles - Error on " + ele.Key.Name + " init, aborting...");
                }
                invokeSingles.Remove(ele.Key);
            }
            for (int step = 0; step < invokes.Count;)
            {
                var ele = invokes.ElementAt(step);
                try
                {
                    for (int step2 = 0; step2 < ele.Value.Count;)
                    {
                        var ele2 = ele.Value.ElementAt(step2);
                        try
                        {
                            if (ele2.nextTime > Time.time)
                            {
                                step2++;
                                continue;
                            }
                            else
                                ele2.Invoke();
                        }
                        catch { }
                        ele.Value.RemoveAt(step2);
                    }
                    if (ele.Value.Any())
                    {
                        step++;
                        continue;
                    }
                }
                catch
                {
                    Debug_TTExt.Log("Debug_TTExt: InvokeHelper.invokes - Error on " + ele.Key.Name + " init, aborting...");
                }
                invokes.Remove(ele.Key);
            }
        }

        internal interface IInvokeable
        {
            float nextTime { get; set; }
            void Invoke();
            bool IsSameMethod(Type type, object obj);
        }
        internal struct Invokeable : IInvokeable
        {
            public float nextTime { get; set; }
            private Action toInvoke;
            public Invokeable(Action invoke, float timeSet)
            {
                nextTime = timeSet;
                toInvoke = invoke;
            }
            public void Invoke()
            {
                toInvoke.Invoke();
            }
            public bool IsSameMethod(Type type, object obj)
            {
                return type == typeof(Action) && (Action)obj == toInvoke;
            }
        }
        internal struct Invokeable<T> : IInvokeable
        {
            public float nextTime { get; set; }
            private T cachedVar;
            private Action<T> toInvoke;
            public Invokeable(Action<T> invoke, float timeSet, T var)
            {
                nextTime = timeSet;
                cachedVar = var;
                toInvoke = invoke;
            }
            public void Invoke()
            {
                toInvoke.Invoke(cachedVar);
            }
            public bool IsSameMethod(Type type, object obj)
            {
                return type == typeof(Action<T>) && (Action<T>)obj == toInvoke;
            }
        }
        internal struct Invokeable<T, V> : IInvokeable
        {
            public float nextTime { get; set; }
            private T cachedVar;
            private V cachedVar2;
            private Action<T, V> toInvoke;
            public Invokeable(Action<T, V> invoke, float timeSet, T var, V var2)
            {
                nextTime = timeSet;
                cachedVar = var;
                cachedVar2 = var2;
                toInvoke = invoke;
            }
            public void Invoke()
            {
                toInvoke.Invoke(cachedVar, cachedVar2);
            }
            public bool IsSameMethod(Type type, object obj)
            {
                return type == typeof(Action<T, V>) && (Action<T, V>)obj == toInvoke;
            }
        }
        internal struct Invokeable<T, V, R> : IInvokeable
        {
            public float nextTime { get; set; }
            private T cachedVar;
            private V cachedVar2;
            private R cachedVar3;
            private Action<T, V, R> toInvoke;
            public Invokeable(Action<T, V, R> invoke, float timeSet, T var, V var2, R var3)
            {
                nextTime = timeSet;
                cachedVar = var;
                cachedVar2 = var2;
                cachedVar3 = var3;
                toInvoke = invoke;
            }
            public void Invoke()
            {
                toInvoke.Invoke(cachedVar, cachedVar2, cachedVar3);
            }
            public bool IsSameMethod(Type type, object obj)
            {
                return type == typeof(Action<T, V, R>) && (Action<T, V, R>)obj == toInvoke;
            }
        }

        internal class GUIManaged : GUILayoutHelpers
        {
            private static bool controlledDisp = false;
            private static HashSet<string> enabledTabs = null;
            public static void GUIGetTotalManaged()
            {
                if (enabledTabs == null)
                {
                    enabledTabs = new HashSet<string>();
                }
                GUILayout.Box("--- Invoke Helper --- ");
                if (GUILayout.Button(" Enabled Loading: " + controlledDisp))
                    controlledDisp = !controlledDisp;
                if (controlledDisp)
                {
                    if (inst != null)
                    {
                        try
                        {
                            GUILabelDispFast("TIME.time: ", Time.time);
                            GUILabelDispFast("TIME.deltaTime: ", Time.deltaTime);
                            GUILabelDispFast("TIME.fixedTime: ", Time.fixedTime);
                            GUILabelDispFast("TIME.fixedDeltaTime: ", Time.fixedDeltaTime);
                            if (GUITabDisp(ref enabledTabs, "Repeating Single Invokes"))
                            {
                                foreach (var item in inst.invokeSingleRepeat)
                                {
                                    GUILayout.Label(item.Key.DeclaringType.FullName + "." + item.Key.Name + "()");
                                }
                            }
                            if (GUITabDisp(ref enabledTabs, "Invoke Singles"))
                            {
                                foreach (var item in inst.invokeSingles)
                                {
                                    GUILabelDispFast(item.Key.DeclaringType.FullName + "." + item.Key.Name + "()", item.Value.nextTime);
                                }
                            }
                            if (GUITabDisp(ref enabledTabs, "Invokes"))
                            {
                                foreach (var item in inst.invokes)
                                {
                                    GUILabelDispFast(item.Key.DeclaringType.FullName + "." + item.Key.Name + "()", item.Value.Count);
                                }
                            }
                        }
                        catch (ExitGUIException e)
                        {
                            throw e;
                        }
                        catch { }
                    }
                    else
                        GUILayout.Label("  None Active!");
                }
            }
        }
    }
}
