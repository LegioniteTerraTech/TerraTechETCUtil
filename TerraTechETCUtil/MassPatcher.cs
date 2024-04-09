using System;
using System.Collections.Generic;
using System.Linq;
#if !EDITOR
using HarmonyLib;
#endif
using UnityEngine;
using System.Reflection;

#if !EDITOR
namespace TerraTechETCUtil
{
    public static class MassPatcher
    {
        private static bool IsUnstable => CheckIfUnstable();

        public static bool CheckIfUnstable()
        {
            return SKU.DisplayVersion.Count(x => x == '.') > 2;
        }
        public static void Thrower(bool throwOnFail, string stringIn)
        {
            if (throwOnFail)
                throw new Exception(stringIn);
            else
                Debug_TTExt.Log(stringIn);
        }

        public static bool MassPatchAllWithin(this Harmony inst, Type ToPatch, string modName, bool throwOnFail = false)
        {
            bool errorless = true;
            try
            {
                Debug_TTExt.Info(modName + ": MassPatchAllWithin - Target " + ToPatch.ToString());
                Type[] types = ToPatch.GetNestedTypes(BindingFlags.Static | BindingFlags.NonPublic);
                if (types == null)
                {
                    Debug_TTExt.Log(modName + ": FAILED TO patch " + ToPatch.Name + " - There's no nested classes?");
                    return false;
                }
                foreach (var typeCase in types)
                {
                    try
                    {
                        Type patcherType;
                        try
                        {
                            patcherType = (Type)typeCase.GetField("target", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
                        }
                        catch
                        {
                            Thrower(throwOnFail, modName + ": FAILED TO patch " + typeCase.Name + " of " + ToPatch.Name + " - There must be a declared target type in a field \"target\"");
                            continue;
                        }
                        MethodInfo[] methods = typeCase.GetMethods(BindingFlags.Static | BindingFlags.NonPublic);
                        if (methods == null)
                        {
                            Thrower(throwOnFail, modName + ": FAILED TO patch " + typeCase.Name + " of " + ToPatch.Name + " - There are no methods to patch?");
                            continue;
                        }
                        //Debug_TTExt.Log("MethodCount: " + methods.Length);
                        Dictionary<string, MassPatcherTemplate> methodsToPatch = new Dictionary<string, MassPatcherTemplate>();
                        foreach (var item in methods)
                        {
                            int underscore = item.Name.LastIndexOf('_');
                            if (underscore == -1)
                            {
                                //Debug_TTExt.Log("No Underscore");
                                continue;
                            }
                            bool StableOnly = item.Name.EndsWith("1");
                            bool UnstableOnly = item.Name.EndsWith("0");
                            string nameNoDivider = UnstableOnly || StableOnly ? item.Name.Substring(0, item.Name.Length - 1) : item.Name;
                            string patcherMethod = nameNoDivider.Substring(0, underscore);
                            string patchingExecution = nameNoDivider.Substring(underscore + 1, nameNoDivider.Length - 1 - underscore);
                            if (!methodsToPatch.TryGetValue(patcherMethod, out MassPatcherTemplate MPT))
                            {
                                //Debug_TTExt.Log("Patching " + patcherMethod);
                                MPT = new MassPatcherTemplate
                                {
                                    fullName = item.Name,
                                };
                                methodsToPatch.Add(patcherMethod, MPT);
                            }
                            //Debug_TTExt.Log("patchingExecution " + patchingExecution);
                            if (UnstableOnly)
                            {   // It's clearly an unstable handler
                                if (IsUnstable)
                                {
                                    switch (patchingExecution)
                                    {
                                        case "Prefix":
                                            MPT.prefix = new HarmonyMethod(AccessTools.Method(typeCase, item.Name));
                                            break;
                                        case "Postfix":
                                            MPT.postfix = new HarmonyMethod(AccessTools.Method(typeCase, item.Name));
                                            break;
                                        case "Transpiler":
                                            MPT.transpiler = new HarmonyMethod(AccessTools.Method(typeCase, item.Name));
                                            break;
                                        default:
                                            break;
                                    }
                                }
                            }
                            else if (StableOnly)
                            {
                                if (!IsUnstable)
                                {
                                    switch (patchingExecution)
                                    {
                                        case "Prefix":
                                            MPT.prefix = new HarmonyMethod(AccessTools.Method(typeCase, item.Name));
                                            break;
                                        case "Postfix":
                                            MPT.postfix = new HarmonyMethod(AccessTools.Method(typeCase, item.Name));
                                            break;
                                        case "Transpiler":
                                            MPT.transpiler = new HarmonyMethod(AccessTools.Method(typeCase, item.Name));
                                            break;
                                        default:
                                            break;
                                    }
                                }
                            }
                            else
                            {
                                switch (patchingExecution)
                                {
                                    case "Prefix":
                                        if (MPT.prefix == null)
                                            MPT.prefix = new HarmonyMethod(AccessTools.Method(typeCase, item.Name));
                                        break;
                                    case "Postfix":
                                        if (MPT.postfix == null)
                                            MPT.postfix = new HarmonyMethod(AccessTools.Method(typeCase, item.Name));
                                        break;
                                    case "Transpiler":
                                        if (MPT.transpiler == null)
                                            MPT.transpiler = new HarmonyMethod(AccessTools.Method(typeCase, item.Name));
                                        break;
                                    default:
                                        break;
                                }
                            }
                        }

                        foreach (var item in methodsToPatch)
                        {
                            try
                            {
                                if (item.Value.prefix != null || item.Value.postfix != null || item.Value.transpiler != null)
                                {
                                    MethodInfo methodCase = AccessTools.Method(patcherType, item.Key);
                                    inst.Patch(methodCase, item.Value.prefix, item.Value.postfix, item.Value.transpiler);
                                    Debug_TTExt.Info(modName + ": (" + item.Value.fullName + ") Patched " + item.Key + " of " + ToPatch.Name);//+ "  prefix: " + (item.Value.prefix != null) + "  postfix: " + (item.Value.postfix != null)
                                }
                            }
                            catch (Exception e)
                            {
                                Thrower(throwOnFail, modName + ": FAILED (" + item.Value.fullName + ") on patch of " + ToPatch.Name + " in type - " + typeCase.Name + " - " + e.Message);
                                errorless = false;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug_TTExt.Log(modName + ": Failed to handle patch of " + ToPatch.Name + " in type - " + typeCase.Name + " - " + e.Message);
                        errorless = false;
                    }
                }
            }
            catch (Exception e)
            {
                Thrower(throwOnFail, modName + ": FAILED TO patch " + ToPatch.Name + " - " + e);
                errorless = false;
            }
            Debug_TTExt.Log(modName + ": Mass patched " + ToPatch.Name);
            return errorless;
        }
        public static bool MassUnPatchAllWithin(this Harmony inst, Type ToPatch, string modName, bool throwOnFail = false)
        {
            try
            {
                Type[] types = ToPatch.GetNestedTypes(BindingFlags.Static | BindingFlags.NonPublic);
                if (types == null)
                {
                    Thrower(throwOnFail, modName + ": FAILED TO patch " + ToPatch.Name + " - There's no nested classes?");
                    return false;
                }
                foreach (var typeCase in types)
                {
                    try
                    {
                        Type patcherType;
                        try
                        {
                            patcherType = (Type)typeCase.GetField("target", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
                        }
                        catch
                        {
                            Thrower(throwOnFail, modName + ": FAILED TO un-patch " + typeCase.Name + " of " + ToPatch.Name + " - There must be a declared target type in a field \"target\"");
                            continue;
                        }
                        MethodInfo[] methods = typeCase.GetMethods(BindingFlags.Static | BindingFlags.NonPublic);
                        if (methods == null)
                        {
                            Thrower(throwOnFail, modName + ": FAILED TO un-patch " + typeCase.Name + " of " + ToPatch.Name + " - There are no methods to patch?");
                            continue;
                        }
                        List<string> methodsToUnpatch = new List<string>();
                        foreach (var item in methods)
                        {
                            int underscore = item.Name.LastIndexOf('_');
                            if (underscore == -1)
                                continue;
                            bool divider = item.Name.EndsWith("0");
                            string nameNoDivider = divider ? item.Name.Substring(0, item.Name.Length - 1) : item.Name;
                            string patcherMethod = nameNoDivider.Substring(0, underscore);
                            string patchingExecution = nameNoDivider.Substring(underscore + 1, nameNoDivider.Length - 1 - underscore);
                            if (!methodsToUnpatch.Contains(patcherMethod))
                            {
                                methodsToUnpatch.Add(patcherMethod);
                            }
                        }

                        foreach (var item in methodsToUnpatch)
                        {
                            MethodInfo methodCase = AccessTools.Method(patcherType, item);
                            inst.Unpatch(methodCase, HarmonyPatchType.All, inst.Id);
                        }
                    }
                    catch (Exception e)
                    {
                        Thrower(throwOnFail, modName + ": Failed to handle un-patch of " + ToPatch.Name + " in type - " + typeCase.Name + " - " + e.Message);
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                Thrower(throwOnFail, modName + ": FAILED TO un-patch " + ToPatch.Name + " - " + e);
            }
            Debug_TTExt.Log(modName + ": Mass un-patched " + ToPatch.Name);
            return true;
        }

        public class MassPatcherTemplate
        {
            internal string fullName = null;
            internal HarmonyMethod prefix = null;
            internal HarmonyMethod postfix = null;
            internal HarmonyMethod transpiler = null;
        }
    }
}
#endif