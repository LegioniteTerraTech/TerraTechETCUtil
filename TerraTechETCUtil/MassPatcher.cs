using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using System.Text;
using static CompoundExpression.EEInstance;



#if !EDITOR
using HarmonyLib;
#endif
using System.Reflection;

#if !EDITOR
namespace TerraTechETCUtil
{
    /// <summary>
    /// use this attached to a <see cref="MassPatcher"/> function to target specific overrides, like 
    /// <see cref="HarmonyPatch"/> <b>but not automatically added</b> on calling <see cref="Harmony.PatchAll()"/>
    /// </summary>
    public class MassPatchTypesAttribute : Attribute
    {
        /// <summary>
        /// <inheritdoc cref="MassPatchTypesAttribute"/>
        /// </summary>
        /// <param name="parameters">Types to target in override</param>
        /// <param name="generics">Any generics to specify in target</param>
        public MassPatchTypesAttribute(Type[] parameters = null, Type[] generics = null)
        {
            Types = parameters;
            Generics = generics;
        }
        internal Type[] Types;
        internal Type[] Generics;
    }
    /// <summary>
    /// A general-use patcher for Harmony with segmented error-checking to insure at least some patches get through if some fail
    /// </summary>
    public static class MassPatcher
    {
        private static bool IsUnstable => CheckIfUnstable();

        /// <summary>
        /// Check if game is on Unstable version
        /// </summary>
        /// <returns></returns>
        public static bool CheckIfUnstable()
        {
            return SKU.DisplayVersion.Count(x => x == '.') > 2;
        }
        /// <summary>
        /// Throw a quick and dirty <see cref="Exception"/>
        /// </summary>
        /// <param name="throwOnFail"></param>
        /// <param name="stringIn"></param>
        /// <exception cref="Exception"></exception>
        public static void Thrower(bool throwOnFail, string stringIn)
        {
            if (throwOnFail)
                throw new Exception(stringIn);
            else
                Debug_TTExt.Log(stringIn);
        }

        private static HarmonyMethod TryCreatePatcherMethod(Type typeCase, string targetMethodName)
        {
            if (typeCase == null)
                throw new ArgumentNullException(nameof(typeCase));
            if (targetMethodName == null)
                throw new ArgumentNullException(nameof(targetMethodName));
            MethodInfo targetMethod = AccessTools.Method(typeCase, targetMethodName);
            if (targetMethod == null)
                throw new NullReferenceException("Targeted method of name " + targetMethodName + " does not exist");
            HarmonyMethod HM = new HarmonyMethod(targetMethod);
            if (HM == null)
                throw new NullReferenceException("Targeted method of name " + targetMethodName + " failed to create HarmonyMethod");
            return HM;
        }
        private static MethodInfo TryCreateTargeterMethod(Type typeCase, string targetMethodName,
            Type[] typesAttribute, Type[] generics)
        {
            if (typeCase == null)
                throw new ArgumentNullException(nameof(typeCase));
            if (targetMethodName == null)
                throw new ArgumentNullException(nameof(targetMethodName));
            try
            {
                MethodInfo targetMethod = AccessTools.Method(typeCase, targetMethodName, typesAttribute, generics);
                if (targetMethod == null)
                    throw new NullReferenceException(nameof(targetMethod));
                return targetMethod;
            }
            catch (Exception e)
            {
                StringBuilder SB = new StringBuilder();
                if (typesAttribute != null)
                {
                    foreach (Type t in typesAttribute)
                        SB.Append(t.Name);
                }
                throw new NullReferenceException("Targeted method of name " + targetMethodName + " does not exist with params " + SB.ToString(), e);
            }
        }

        /// <summary>
        /// Mass-patch all in a target class. See <see cref="AllProjectilePatches"/> as an example of how to layout the class
        /// </summary>
        /// <param name="inst"></param>
        /// <param name="ToPatch"></param>
        /// <param name="modName"></param>
        /// <param name="throwOnFail"></param>
        /// <returns></returns>
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
                foreach (Type patcherClass in types)
                {
                    try
                    {
                        if (patcherClass?.Name != null && patcherClass.Name.StartsWith("<>"))
                            continue; // ignore auto-generated classes
                        Type patcherType;
                        try
                        {
                            patcherType = (Type)patcherClass.GetField("target", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
                        }
                        catch
                        {
                            Thrower(throwOnFail, modName + ": FAILED TO patch " + patcherClass.Name + " of " + ToPatch.Name + " - There must be a declared target type in a field \"target\"");
                            continue;
                        }
                        MethodInfo[] patcherMethods = patcherClass.GetMethods(BindingFlags.Static | BindingFlags.NonPublic);
                        if (patcherMethods == null)
                        {
                            Thrower(throwOnFail, modName + ": FAILED TO patch " + patcherClass.Name + " of " + ToPatch.Name + " - There are no methods to patch?");
                            continue;
                        }
                        //Debug_TTExt.Log("MethodCount: " + methods.Length);
                        Dictionary<MassPatcherTarget, MassPatcherTemplate> targetMethodsToPatch = new Dictionary<MassPatcherTarget, MassPatcherTemplate>();
                        foreach (var patcherMethod in patcherMethods)
                        {
                            int underscore = patcherMethod.Name.LastIndexOf('_');
                            if (underscore == -1)
                            {
                                //Debug_TTExt.Log("No Underscore");
                                continue;
                            }
                            bool StableOnly = patcherMethod.Name.EndsWith("1");
                            bool UnstableOnly = patcherMethod.Name.EndsWith("0");
                            string nameNoDivider = UnstableOnly || StableOnly ? patcherMethod.Name.Substring(0, patcherMethod.Name.Length - 1) : patcherMethod.Name;
                            string targetMethodName = nameNoDivider.Substring(0, underscore);
                            Type[] targetTypes = null;
                            Type[] targetGenerics = null;
                            var attribute = Attribute.GetCustomAttribute(patcherMethod, typeof(MassPatchTypesAttribute));
                            if (attribute != null && attribute is MassPatchTypesAttribute MPTA)
                            {
                                targetTypes = MPTA.Types;
                                targetGenerics = MPTA.Generics;
                            }
                            var targeter = new MassPatcherTarget()
                            {
                                fullName = targetMethodName,
                                paramTypes = targetTypes,
                                genericTypes = targetGenerics,
                            };
                            string patchingExecution = nameNoDivider.Substring(underscore + 1, nameNoDivider.Length - 1 - underscore);
                            if (!targetMethodsToPatch.TryGetValue(targeter, out MassPatcherTemplate MPT))
                            {
                                //Debug_TTExt.Log("Patching " + patcherMethod);
                                MPT = new MassPatcherTemplate
                                {
                                    target = targeter,
                                };
                                targetMethodsToPatch.Add(targeter, MPT);
                            }
                            //Debug_TTExt.Log("patchingExecution " + patchingExecution);
                            if (UnstableOnly)
                            {   // It's clearly an unstable handler
                                if (IsUnstable)
                                {
                                    switch (patchingExecution)
                                    {
                                        case "Prefix":
                                            MPT.prefix = TryCreatePatcherMethod(patcherClass, patcherMethod.Name);
                                            break;
                                        case "Postfix":
                                            MPT.postfix = TryCreatePatcherMethod(patcherClass, patcherMethod.Name);
                                            break;
                                        case "Transpiler":
                                            MPT.transpiler = TryCreatePatcherMethod(patcherClass, patcherMethod.Name);
                                            break;
                                        case "Finalizer":
                                            MPT.finalizer = TryCreatePatcherMethod(patcherClass, patcherMethod.Name);
                                            break;
                                        default:
                                            Debug_TTExt.Log("MassPatcher is unsure on how to proceed with type " + 
                                                (patchingExecution.NullOrEmpty() ? "<NULL>" : patchingExecution));
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
                                            MPT.prefix = TryCreatePatcherMethod(patcherClass, patcherMethod.Name);
                                            break;
                                        case "Postfix":
                                            MPT.postfix = TryCreatePatcherMethod(patcherClass, patcherMethod.Name);
                                            break;
                                        case "Transpiler":
                                            MPT.transpiler = TryCreatePatcherMethod(patcherClass, patcherMethod.Name);
                                            break;
                                        case "Finalizer":
                                            MPT.finalizer = TryCreatePatcherMethod(patcherClass, patcherMethod.Name);
                                            break;
                                        default:
                                            Debug_TTExt.Log("MassPatcher is unsure on how to proceed with type " +
                                                (patchingExecution.NullOrEmpty() ? "<NULL>" : patchingExecution));
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
                                            MPT.prefix = TryCreatePatcherMethod(patcherClass, patcherMethod.Name);
                                        break;
                                    case "Postfix":
                                        if (MPT.postfix == null)
                                            MPT.postfix = TryCreatePatcherMethod(patcherClass, patcherMethod.Name);
                                        break;
                                    case "Transpiler":
                                        if (MPT.transpiler == null)
                                            MPT.transpiler = TryCreatePatcherMethod(patcherClass, patcherMethod.Name);
                                        break;
                                    case "Finalizer":
                                        if (MPT.finalizer == null)
                                            MPT.finalizer = TryCreatePatcherMethod(patcherClass, patcherMethod.Name);
                                        break;
                                    default:
                                        Debug_TTExt.Log("MassPatcher is unsure on how to proceed with type " +
                                            (patchingExecution.NullOrEmpty() ? "<NULL>" : patchingExecution));
                                        break;
                                }
                            }
                        }

                        foreach (var item in targetMethodsToPatch)
                        {
                            try
                            {
                                if (item.Value.prefix != null || item.Value.postfix != null || item.Value.transpiler != null || item.Value.finalizer != null)
                                {
                                    MethodInfo targetMethodCase = TryCreateTargeterMethod(patcherType, item.Key.fullName, item.Key.paramTypes, item.Key.genericTypes);
                                    inst.Patch(targetMethodCase, item.Value.prefix, item.Value.postfix, item.Value.transpiler, item.Value.finalizer);
                                    Debug_TTExt.Info(modName + ": (" + item.Key.fullName + ") Patched " + item.Key.fullName + " of " + ToPatch.Name);//+ "  prefix: " + (item.Value.prefix != null) + "  postfix: " + (item.Value.postfix != null)
                                }
                            }
                            catch (Exception e)
                            {
                                Thrower(throwOnFail, modName + ": FAILED (" + item.Key.fullName + ") on patch of " + ToPatch.Name + " in type - " + patcherClass.Name + " - " + e.Message);
                                errorless = false;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug_TTExt.Log(modName + ": Failed to handle patch of " + ToPatch.Name + " in type - " + patcherClass.Name + " - " + e.Message);
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

        /// <summary>
        /// Mass-unpatch all in a target class.
        /// </summary>
        /// <param name="inst"></param>
        /// <param name="ToPatch"></param>
        /// <param name="modName"></param>
        /// <param name="throwOnFail"></param>
        /// <returns></returns>
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
                        List<MassPatcherTarget> methodsToUnpatch = new List<MassPatcherTarget>();
                        foreach (var item in methods)
                        {
                            int underscore = item.Name.LastIndexOf('_');
                            if (underscore == -1)
                                continue;
                            bool divider = item.Name.EndsWith("0");
                            string nameNoDivider = divider ? item.Name.Substring(0, item.Name.Length - 1) : item.Name;
                            Type[] targetTypes = null;
                            Type[] targetGenerics = null;
                            var attribute = Attribute.GetCustomAttribute(item, typeof(MassPatchTypesAttribute));
                            if (attribute != null && attribute is MassPatchTypesAttribute MPTA)
                            {
                                targetTypes = MPTA.Types;
                                targetGenerics = MPTA.Generics;
                            }
                            MassPatcherTarget patcherMethod = new MassPatcherTarget
                            {
                                fullName = nameNoDivider.Substring(0, underscore),
                                paramTypes = targetTypes,
                                genericTypes = targetGenerics,
                            };
                            string patchingExecution = nameNoDivider.Substring(underscore + 1, nameNoDivider.Length - 1 - underscore);
                            if (!methodsToUnpatch.Contains(patcherMethod))
                            {
                                methodsToUnpatch.Add(patcherMethod);
                            }
                        }

                        foreach (var item in methodsToUnpatch)
                        {
                            MethodInfo methodCase = TryCreateTargeterMethod(patcherType, item.fullName, item.paramTypes, item.genericTypes);
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

        internal class MassPatcherTemplate
        {
            internal MassPatcherTarget target = default;
            internal HarmonyMethod prefix = null;
            internal HarmonyMethod postfix = null;
            internal HarmonyMethod transpiler = null;
            internal HarmonyMethod finalizer = null;
        }
        internal struct MassPatcherTarget
        {
            internal string fullName;
            internal Type[] paramTypes;
            internal Type[] genericTypes;
        }
    }
}
#endif