using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using System.CodeDom;
using static VectorLineRenderer;
using static CompoundExpression.EEInstance;
using static TerraTechETCUtil.WikiPageBlock;

public class DocAttribute : Attribute
{
    internal string desc;
    public DocAttribute(string description)
    {
        desc = description;
    }
}

namespace TerraTechETCUtil
{
    public static class GameObjectDocumentator
    {
        public static void GetStrings(GameObject targetBaseGameObject, StringBuilder SB,
            int tabs, SlashState slash, bool GetComponentsToo = true)
        {
            IterateRecursively(targetBaseGameObject, SB, tabs, slash, GetComponentsToo);
        }
        private static void InsertWithTabNoNewline(int tabs, string desc, StringBuilder SB)
        {
            for (int i = 0; i < tabs; i++)
                SB.Append('\t');
            SB.Append(desc);
        }
        private static void InsertVec3Fast(int tabs, string name, Vector3 vec, StringBuilder SB)
        {
            InsertWithTabNoNewline(tabs, "\"" + name + "\" : { \"x\": " + vec.x +
                ", \"y\": " + vec.y + ", \"z\": " + vec.z + " },\n", SB);
        }
        private static void IterateRecursively(GameObject gameOb, StringBuilder SB,
            int tabs, SlashState slash, bool GetComponentsToo)
        {
            for (int i = 0; i < tabs; i++)
                SB.Append('\t');
            if (tabs > 32)
            {
                SB.Append("// Depth exceeded 32!");
                return; // TOO DEEP 
            }
            if (gameOb.name.NullOrEmpty())
                SB.Append("\"GameObject|null\" : {\n");
            else if (gameOb.name.EndsWith(")"))
            {
                SB.Append("\"GameObject|");
                SB.Append(gameOb.name);
                SB.Append("\" : {// - Likely cannot be referenced due to (#)\n");
            }
            else
            {
                SB.Append("\"GameObject|");
                SB.Append(gameOb.name);
                SB.Append("\" : {\n");
            }
            Transform trans = gameOb.transform;
            InsertWithTabNoNewline(tabs + 1, "\"UnityEngine.Transform\":{\n", SB);
            InsertVec3Fast(tabs + 2, "localPosition", trans.localPosition, SB);
            InsertVec3Fast(tabs + 2, "localEulerAngles", trans.localEulerAngles, SB);
            InsertVec3Fast(tabs + 2, "localScale", trans.localScale, SB);
            InsertWithTabNoNewline(tabs + 1, "},\n", SB);
            for (int i = 0; i < trans.childCount; i++)
            {
                Transform transC = trans.GetChild(i);
                if (transC != null)
                    IterateRecursively(transC.gameObject, SB, tabs + 1, slash, GetComponentsToo);
            }
            if (GetComponentsToo)
            {
                foreach (var item in ModuleInfo.TryGetModulesBlacklisted(gameOb, ModuleInfo.BlockedTypesExporter))
                {
                    item.GetJsonFormatted(item.inst, SB, slash, tabs + 1);
                    SB.Append(",\n");
                    /*
                    AutoDocumentator AD2 = new AutoDocumentator(, item.inst, );
                    InsertWithTabNoNewline(tabs + 1, "// Module Ref Path: \"" + AutoDocUIElem.TryGetFoundationRefName(trans) +
                        AutoDocUIElem.RecurseHierachy(trans) + "/" + item.GetType().Name + ".\"\n", SB);
                    InsertWithTabNoNewline(tabs + 1, "\"" + item.GetType().Name + "\": ", SB);
                    AD2.StringBuild(item, trans, SB, slash, tabs + 1, false);
                    SB.Append(",\n");
                    */
                }
            }
            for (int i = 0; i < tabs; i++)
                SB.Append('\t');
            SB.Append("},\n");
        }
    }
    public class AutoDocumentator
    {
        public static bool LogStaticsAndBlocked = false;
        public static bool HideUseless = true;
        public readonly Type type;
        public readonly string overrideDesc;
        internal readonly AutoDocUIElem[] lines;

        private static List<AutoDocUIElem> Collector = new List<AutoDocUIElem>();
        public AutoDocumentator(Type target, string[] targetFieldNames, string overrideDesc = null)
        {
            this.overrideDesc = overrideDesc;
            type = target;
            if (targetFieldNames == null)
                throw new ArgumentNullException(nameof(targetFieldNames)); 
            for (int i = 0; i < targetFieldNames.Length; i++)
            {
                if (targetFieldNames[i].NullOrEmpty())
                    continue;
                FieldInfo item = target.GetField(targetFieldNames[i], BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (item != null)
                {
                    DocAttribute DA = item.GetCustomAttribute<DocAttribute>();
                    if (DA != null)
                        Collector.Add(new AutoDocUIElem(item, DA.desc));
                    else
                        Collector.Add(new AutoDocUIElem(item, null));
                }
            }
            lines = Collector.ToArray();
            Collector.Clear();
        }
        public AutoDocumentator(Type target, string overrideDesc = null, FieldInfo[] cachedFields = null)
        {
            this.overrideDesc = overrideDesc;
            type = target;
            if (cachedFields == null)
                cachedFields = target.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var item in cachedFields)
            {
                if (item != null)
                {
                    DocAttribute DA = item.GetCustomAttribute<DocAttribute>();
                    if (DA != null)
                        Collector.Add(new AutoDocUIElem(item, DA.desc));
                    else
                        Collector.Add(new AutoDocUIElem(item, null));
                }
            }
            lines = Collector.ToArray();
            Collector.Clear();
        }
        /// <summary>
        /// Does NOT return end comma with newline!
        /// </summary>
        /// <param name="inst"></param>
        /// <param name="trans"></param>
        /// <param name="SB"></param>
        /// <param name="slash"></param>
        /// <param name="tabs"></param>
        /// <param name="ShowName"></param>
        public void StringBuild(object inst, Transform trans, StringBuilder SB, SlashState slash, int tabs = 0, bool ShowName = true)
        {
            if (tabs > 16)
                return; // things are probably repeating
            for (int i = 0; i < tabs; i++)
                SB.Append('\t');
            if (ShowName)
            {
                SB.Append("\"");
                SB.Append(type.FullName);
                SB.Append("\": ");
            }
            SB.Append("{");
            if (overrideDesc != null)
            {
                if (slash == SlashState.Slash)
                    SB.Append("\t");
                else
                    SB.Append("//\t");
                SB.Append(overrideDesc);
            }
            else
            {
                DocAttribute DA = type.GetCustomAttribute<DocAttribute>();
                if (DA != null)
                {
                    SB.Append("//\t");
                    SB.Append(DA.desc);
                }
            }
            SB.Append("\n");
            if (trans != null)
            {
                foreach (var item in lines)
                {
                    item.GetStrings(inst, trans, SB, tabs + 1, slash);
                }
            }
            for (int i = 0; i < tabs; i++)
                SB.Append('\t');
            if (slash == SlashState.Slash)
                SB.Append("//");
            SB.Append("}");
        }


    }
    public enum SlashState
    {
        None,
        Slash,
    }
    public class AutoDocUIElem
    {
        internal readonly FieldInfo field;
        internal readonly string desc;
        public AutoDocUIElem(FieldInfo field, string desc)
        { 
            this.field = field;
            if (desc.NullOrEmpty())
                desc = string.Empty;
            this.desc = desc;
        }
        public void GetStrings(object inst, Transform trans, StringBuilder SB, int tabs, SlashState slash)
        {
            object fieldVal;
            if (inst == null)
            {
                try
                {
                    fieldVal = field.GetRawConstantValue();
                }
                catch (Exception)
                {
                    fieldVal = null;
                }
            }
            else
                fieldVal = field.GetValue(inst);
            if (!AutoDocumentator.LogStaticsAndBlocked && field.IsStatic)
                return; // DO NOT DOCUMENT STATICS
            GenerateStrings(inst, fieldVal, field.FieldType, field.Name, this, trans, SB, tabs, slash);
        }
        private static AutoDocUIElem lazyContext;
        private static string GetAdditionalInfo()
        {
            if (lazyContext != null)
            {
                if (lazyContext.desc.NullOrEmpty())
                    return string.Empty;
                return " - " + lazyContext.desc;
            }
            return string.Empty;
        }
        private static void GenerateStrings(object inst, object fieldVal, Type fieldType, string fieldName, AutoDocUIElem lazyContextSet, Transform trans, StringBuilder SB, int tabs, SlashState slash)
        {
            lazyContext = lazyContextSet;
            if (!AutoDocumentator.LogStaticsAndBlocked && (fieldName.StartsWith("_") ||
                fieldName.Contains("k__") || AutoDataExtractor.ignoreFieldTypes.Contains(fieldType) ||
                (fieldType.IsGenericType && AutoDataExtractor.ignoreFieldTypes.Contains(fieldType.GetGenericTypeDefinition())) ||
                AutoDataExtractor.ignoreFieldNames.Contains(fieldName)))
                return; // DO NOT DOCUMENT STATICS
            if (tabs - 1 <= 0 && slash == SlashState.Slash)
                    SB.Append("//");
            for (int i = 0; i < tabs; i++)
            {
                if (i == tabs - 1 && slash == SlashState.Slash)
                        SB.Append("//");
                SB.Append('\t');
            }
            if (fieldType.IsClass)
            {
                SB.Append("// Class Ref Path: \"" + TryGetFoundationRefName(trans) + RecurseHierachy(trans) + "/" + fieldName + ".\"\n");
                for (int i = 0; i < tabs; i++)
                {
                    SB.Append('\t');
                }
                if (fieldName != string.Empty)
                {
                    SB.Append("\"" + fieldName + "\": ");
                }
                if (fieldVal == null)
                {
                    if (fieldType.IsSubclassOf(typeof(Transform)))
                    {
                        if (inst == null)
                        {
                            SB.Append("null,//\tCannot get [Transform] from a null reference instance");
                        }
                        else
                        {
                            SB.Append("null,//\t[Transform]" + GetAdditionalInfo());
                        }
                    }
                    if (fieldType.IsSubclassOf(typeof(MonoBehaviour)))
                    {
                        if (inst == null)
                        {
                            SB.Append("null,//\tCannot get [" + fieldType.Name + "] Component from a null reference instance");
                        }
                        else
                        {
                            SB.Append("\"Instantiate|" + fieldName + "\": null,//\t[" + fieldType.Name + "] Component" + GetAdditionalInfo());
                        }
                    }
                    else
                        SB.Append("null,//\t[" + fieldType.Name + "] Class" + GetAdditionalInfo());
                }
                else if (fieldVal is MonoBehaviour monoV)
                {
                    SB.Append("\"" + fieldName + "\": " + DetermineTransform(fieldType.Name, trans, monoV.transform, trans, fieldType.Name) + GetAdditionalInfo());
                }
                else if (fieldVal is Transform transform)
                {
                    SB.Append("\"" + fieldName + "\": " + DetermineTransform(fieldType.Name,trans, transform, trans, trans.name) + GetAdditionalInfo());
                }
                else if (fieldVal is IList listable)
                {
                    if (listable == null)
                    {
                        SB.Append("\"" + fieldName + "\": null,//\t[" + fieldType.Name + "] Component" + GetAdditionalInfo());
                    }
                    else
                    {
                        SB.Append("\"" + fieldName + "\": {//\t[" + fieldType.Name + "] Component" + GetAdditionalInfo());
                        foreach (var item in listable)
                        {
                            GenerateStrings(null, item, item.GetType(), "", null, trans, SB, tabs + 1, slash);
                        }
                        for (int i = 0; i < tabs; i++)
                            SB.Append('\t');
                        if (slash == SlashState.Slash)
                            SB.Append("//");
                        SB.Append("}");
                        SB.Append(",");
                    }
                }
                else
                {
                    AutoDocumentator AD = new AutoDocumentator(fieldVal.GetType(), "Field [" + fieldType.Name +
                        (fieldType != fieldVal.GetType() ? "], Set value [" + fieldVal.GetType().Name + "]" : "]") +
                         GetAdditionalInfo());
                    //SB.Append("//\"" + fieldName + "\": null, //This field is unsupported by NuterraSteam");
                    SB.Append("\"" + fieldName + "\": ");
                    AD.StringBuild(fieldVal, trans, SB, slash, tabs, false);
                    SB.Append(",");
                }
            }
            else
            {
                if (fieldVal == null)
                    SB.Append("\"" + fieldName + "\": null,//\t[Object]" + GetAdditionalInfo());
                else if (fieldVal is bool boolean)
                    SB.Append("\"" + fieldName + "\": " + (boolean ? "true" : "false") + ",//\t[Bool]" + GetAdditionalInfo());
                else if (fieldVal is float floatV)
                    SB.Append("\"" + fieldName + "\": " + floatV.ToString("F") + ",//\t[Float]" + GetAdditionalInfo());
                else if (fieldVal is int intV)
                    SB.Append("\"" + fieldName + "\": " + intV.ToString() + ",//\t[Integer]" + GetAdditionalInfo());
                else if (!fieldType.IsPrimitive)
                {
                    if (fieldVal is Enum enumV)
                    {
                        SB.Append("\"" + fieldName + "\": " + (int)fieldVal + ", //\toption " + fieldVal.ToString() +
                            ", [" + fieldVal.GetType().Name + "] Enum" + GetAdditionalInfo());
                        StringBuildEnum(fieldVal.GetType(), SB, tabs);
                    }
                    else
                    {
                        if (fieldVal == default)
                        {
                            AutoDocumentator AD = new AutoDocumentator(fieldVal.GetType(), "!default! [" + fieldVal.GetType().Name + "]" + GetAdditionalInfo());
                            SB.Append("\"" + fieldName + "\": ");
                            AD.StringBuild(fieldVal, trans, SB, slash, tabs, false);
                            SB.Append(",");
                        }
                        else
                        {
                            AutoDocumentator AD = new AutoDocumentator(fieldVal.GetType(), "[" + fieldVal.GetType().Name + "]" + GetAdditionalInfo());
                            SB.Append("\"" + fieldName + "\": ");
                            AD.StringBuild(fieldVal, trans, SB, slash, tabs, false);
                            SB.Append(",");
                        }
                    }
                }
                else
                    SB.Append("\"" + fieldName + "\": \"" + fieldVal.ToString() + "\",//\t[" + fieldVal.GetType().Name + "]" + GetAdditionalInfo());
            }
            SB.Append("\n");
        }
        private static void StringBuildEnum(Type enumType, StringBuilder SB, int tabs = 0)
        {
            if (tabs > 16)
                return; // things are probably repeating
            SB.Append('\n');
            for (int i = 0; i < tabs; i++)
                SB.Append('\t');
            SB.Append("//\"");
            SB.Append(enumType.FullName);
            SB.Append("\": ");
            if (enumType.GetCustomAttribute<FlagsAttribute>() != null)
                SB.Append("[Flags] - Add the values below to combine! ");
            SB.Append("{\n");
            string[] names = Enum.GetNames(enumType);
            Array values = Enum.GetValues(enumType);
            for (int i = 0; i < names.Length; i++)
            {
                for (int i2 = 0; i2 < tabs; i2++)
                    SB.Append('\t');
                SB.Append("//\t");
                SB.Append(((int)values.GetValue(i)).ToString().PadRight(3));
                SB.Append(" = \"");
                SB.Append(names[i]);
                SB.Append("\",\n");
            }
            for (int i = 0; i < tabs; i++)
                SB.Append('\t');
            SB.Append("//}");
        }
        private static string DetermineTransform(string fieldTypeName, Transform inst, Transform trans, Transform realRoot, string endName)
        {
            var root = trans.root;
            if (root != realRoot)
            {   // try find our reference
                string targetHierachy = RecurseHierachy(inst) + "/" + endName;
                Component comp = trans.GetComponent<Projectile>();
                if (!comp)
                    comp = trans.GetComponent<BulletCasing>();
                if (comp)
                {   // it is a valid reference, proceed.
                    return "\"" + targetHierachy + "\",//\t[" + comp.GetType().Name + "] Component";
                }
                else
                {
                    if (inst)
                        return "\"" + targetHierachy + "\",//\t[Transform] in hierachy";
                    return "\"" + targetHierachy + "\",//\t[Transform] in hierachy";
                    // return "null, //Outside of Root - We cannot fetch this!!!";
                }
            }
            else
            {   // it exists in hierchy
                string targetHierachy = RecurseHierachy(trans) + "/" + endName;
                if (endName == null)
                    return "\"" + targetHierachy + "\",//\t[Transform] in hierachy";
                return "\"" + targetHierachy + ".\",//\t[" + fieldTypeName + "] Component in hierachy";
            }
        }
        internal static string RecurseHierachy(Transform inst)
        {
            if (inst.parent?.parent)
                return RecurseHierachy(inst.parent) + "/" + (inst.name.NullOrEmpty() ? "<NULL>" : inst.name);
            return string.Empty;
        }
        internal static string TryGetFoundationRefName(Transform inst)
        {
            while (inst.parent != null)
                inst = inst.parent;
            TankBlock TB = inst.GetComponent<TankBlock>();
            if (TB)
                return ((BlockTypes)inst.GetComponent<Visible>().m_ItemType.ItemType).ToString();
            return inst.name.NullOrEmpty() ? string.Empty : inst.name;
        }
    }
}
