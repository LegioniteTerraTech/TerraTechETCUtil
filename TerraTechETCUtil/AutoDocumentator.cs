using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using System.CodeDom;

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
    public class AutoDocumentator
    {
        public static bool HideUseless = true;
        public readonly Type type;
        public readonly string overrideDesc;
        internal readonly AutoDocUIElem[] lines;

        private static List<AutoDocUIElem> Collector = new List<AutoDocUIElem>();
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
        public void StringBuild(object inst, Transform trans, StringBuilder SB, int tabs = 0, bool ShowName = true)
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
                    item.GetStrings(inst, trans, SB, tabs + 1);
                }
            }
            for (int i = 0; i < tabs; i++)
                SB.Append('\t');
            SB.Append("}");
        }
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
        public void GetStrings(object inst, Transform trans, StringBuilder SB, int tabs)
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
            for (int i = 0; i < tabs; i++)
            {
                SB.Append('\t');
            }
            if (field.FieldType.IsClass)
            {
                SB.Append("// Class Ref Path: \"" + RecurseHierachy(trans) + "." + field.Name + "\"\n");
                for (int i = 0; i < tabs; i++)
                {
                    SB.Append('\t');
                }
                if (fieldVal == null)
                {
                    if (field.FieldType.IsSubclassOf(typeof(Transform)))
                    {
                        if (inst == null)
                            SB.Append("\"" + field.Name + "\": null,//\tCannot get [Transform] from a null reference instance");
                        else
                            SB.Append("\"" + field.Name + "\": null,//\t[Transform] - " + desc);
                    }
                    if (field.FieldType.IsSubclassOf(typeof(MonoBehaviour)))
                    {
                        if (inst == null)
                            SB.Append("\"" + field.Name + "\": null,//\tCannot get [" + field.FieldType.Name + "] Component from a null reference instance");
                        else
                            SB.Append("\"Instantiate|" + field.Name + "\": null,//\t[" + field.FieldType.Name + "] Component - " + desc);
                    }
                    else
                        SB.Append("\"" + field.Name + "\": null,//\t[" + field.FieldType.Name + "] Class - " + desc);
                }
                else if (fieldVal is MonoBehaviour monoV)
                {
                    SB.Append("\"" + field.Name + "\": " + DetermineTransform(trans, monoV.transform, trans, field.FieldType.Name) + " - " + desc);
                }
                else if (fieldVal is Transform transform)
                {
                    SB.Append("\"" + field.Name + "\": " + DetermineTransform(trans, transform, trans, trans.name) + " - " + desc);
                }
                else
                {
                    AutoDocumentator AD = new AutoDocumentator(fieldVal.GetType(), "Field [" + field.FieldType.Name +
                        (field.FieldType != fieldVal.GetType() ? "], Set value [" + fieldVal.GetType().Name + "]" : "]") +
                        " - " + desc);
                    //SB.Append("//\"" + field.Name + "\": null, //This field is unsupported by NuterraSteam");
                    SB.Append("\"" + field.Name + "\": ");
                    AD.StringBuild(fieldVal, trans, SB, tabs, false);
                    SB.Append(",");
                }
            }
            else
            {
                if (fieldVal == null)
                    SB.Append("\"" + field.Name + "\": null,//\t[Object] - " + desc);
                if (fieldVal is float floatV)
                    SB.Append("\"" + field.Name + "\": " + floatV.ToString("F") + ",//\t[Float] - " + desc);
                else if (fieldVal is int intV)
                    SB.Append("\"" + field.Name + "\": " + intV.ToString() + ",//\t[Integer] - " + desc);
                else if (!field.FieldType.IsPrimitive)
                {
                    if (fieldVal == default)
                    {
                        AutoDocumentator AD = new AutoDocumentator(fieldVal.GetType(), "!unset! [" + fieldVal.GetType().Name + "] - " + desc);
                        SB.Append("\"" + field.Name + "\": ");
                        AD.StringBuild(fieldVal, trans, SB, tabs, false);
                        SB.Append(",");
                    }
                    else
                    {
                        AutoDocumentator AD = new AutoDocumentator(fieldVal.GetType(), "[" + fieldVal.GetType().Name + "] - " + desc);
                        SB.Append("\"" + field.Name + "\": ");
                        AD.StringBuild(fieldVal, trans, SB, tabs, false);
                        SB.Append(",");
                    }
                }
                else
                    SB.Append("\"" + field.Name + "\": \"" + fieldVal.ToString() + "\",//\t[" + fieldVal.GetType().Name + "] - " + desc);
            }
            SB.Append("\n");
        }
        private string DetermineTransform(Transform inst, Transform trans, Transform realRoot, string endName)
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
                return "\"" + targetHierachy + ".\",//\t[" + field.FieldType.Name + "] Component in hierachy";
            }
        }
        private string RecurseHierachy(Transform inst)
        {
            if (inst.parent?.parent)
                return RecurseHierachy(inst.parent) + "/" + (inst.name.NullOrEmpty() ? "<NULL>" : inst.name);
            return string.Empty;
        }
    }
}
