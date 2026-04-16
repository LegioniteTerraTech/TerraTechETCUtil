using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Nuterra.NativeOptions;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// NativeOptions with a bit more UI to it
/// </summary>
public static class SuperNativeOptions
{
    private static StringBuilder spacer = new StringBuilder();
    private static FieldInfo NameSetter
    {
        get
        {
            if (_nameSetter == null)
                _nameSetter = typeof(Option).GetField("name", BindingFlags.Instance | BindingFlags.NonPublic);
            return _nameSetter;
        }
    }
    private static FieldInfo _nameSetter = null;
    /// <summary>
    /// Set the UI text for the <see cref="Option"/>
    /// </summary>
    /// <param name="Op"></param>
    /// <param name="name"></param>
    public static void SetExtraTextUIOnly(this Option Op, string name)
    {
        Op.UIElement.transform.Find("Text").GetComponent<Text>().text =
        ((string)NameSetter.GetValue(Op)) + " (" + name + ")";
    }
    /// <summary>
    /// Set the UI text for the <see cref="OptionRange"/>
    /// <para>For numbers displayed with <b>no decimal places</b></para>
    /// </summary>
    /// <param name="Op"></param>
    /// <param name="name"></param>
    public static void SetExtraTextUIOnlyD0(this OptionRange Op, float name)
    {
        Op.UIElement.transform.Find("Text").GetComponent<Text>().text =
        ((string)NameSetter.GetValue(Op)) + " (" + name.ToString("0") + ")";
    }
    /// <summary>
    /// Set the UI text for the <see cref="OptionRange"/>
    /// <para>For numbers displayed with <b>two decimal places</b></para>
    /// </summary>
    /// <param name="Op"></param>
    /// <param name="name"></param>
    public static void SetExtraTextUIOnlyD2(this OptionRange Op, float name)
    {
        Op.UIElement.transform.Find("Text").GetComponent<Text>().text =
        ((string)NameSetter.GetValue(Op)) + " (" + name.ToString("0.00") + ")";
    }
    /// <summary>
    /// Set the UI text for the <see cref="OptionRange"/>
    /// <para>For numbers displayed with <b>three decimal places</b></para>
    /// </summary>
    /// <param name="Op"></param>
    /// <param name="name"></param>
    public static void SetExtraTextUIOnlyD3(this OptionRange Op, float name)
    {
        Op.UIElement.transform.Find("Text").GetComponent<Text>().text =
        ((string)NameSetter.GetValue(Op)) + " (" + name.ToString("0.000") + ")";
    }
    /// <summary>
    /// Advanced <see cref="OptionRange"/> with dynamically changing UI connected to it
    /// </summary>
    /// <param name="name">Name of the range</param>
    /// <param name="modname">Category to display this under</param>
    /// <param name="defaultval">The starting number this begins with</param>
    /// <param name="minval">The minimum value in the range</param>
    /// <param name="maxval">The maximum value in the range</param>
    /// <param name="roundto">The step clamping in the range</param>
    /// <param name="uiReturnFunc">The special function to display the result of the option range value</param>
    /// <returns>The special <see cref="OptionRange"/> with added functionality</returns>
    public static OptionRange OptionRangeAutoDisplay(string name, string modname, float defaultval = 0,
        float minval = 0, float maxval = 100, float roundto = 1, Func<float, string> uiReturnFunc = null)
    {
        string finalName;
        if (uiReturnFunc == null)
        {
            spacer.Append("0.");
            for (int i = 0; i < roundto.GetDecimalPlaceCount(); i++)
                spacer.Append('0');
            finalName = name + " (" + (Mathf.RoundToInt(defaultval / roundto) * roundto).ToString(spacer.ToString()) + ")";
            spacer.Clear();
        }
        else
        {
            finalName = name + " (" + uiReturnFunc(Mathf.RoundToInt(defaultval / roundto) * roundto) + ")";
        }
        OptionRange OR = new OptionRange(finalName, modname, defaultval, minval, maxval, roundto);
        ((Slider.SliderEvent)OR.onValueChanged).AddListener((float value) =>
        {
            value = Mathf.RoundToInt(value / roundto) * roundto;
            if (uiReturnFunc == null)
            {
                spacer.Append("0.");
                for (int i = 0; i < roundto.GetDecimalPlaceCount(); i++)
                    spacer.Append('0');
                OR.UIElement.transform.Find("Text").GetComponent<Text>().text =
                name + " (" + value.ToString(spacer.ToString()) + ")";
                spacer.Clear();
            }
            else
            {
                OR.UIElement.transform.Find("Text").GetComponent<Text>().text =
                name + " (" + uiReturnFunc(value) + ")";
            }
        });
        return OR;
    }
    /// <summary>
    /// Advanced <see cref="OptionRange"/> with dynamically changing UI connected to it
    /// </summary>
    /// <param name="name">Name of the range</param>
    /// <param name="modname">Category to display this under</param>
    /// <param name="defaultval">The starting number this begins with</param>
    /// <param name="minval">The minimum value in the range</param>
    /// <param name="maxval">The maximum value in the range</param>
    /// <param name="roundto">The step clamping in the range</param>
    /// <param name="uiReturnFunc">The special function to display the result of the option range value</param>
    /// <returns>The special <see cref="OptionRange"/> with added functionality</returns>
    public static OptionRange OptionRangeAutoDisplay(string name, string modname, float defaultval = 0,
        float minval = 0, float maxval = 100, float roundto = 1, Func<float, float> uiReturnFunc = null)
    {
        string finalName;
        if (uiReturnFunc == null)
        {
            spacer.Append("0.");
            for (int i = 0; i < roundto.GetDecimalPlaceCount(); i++)
                spacer.Append('0');
            finalName = name + " (" + (Mathf.RoundToInt(defaultval / roundto) * roundto).ToString(spacer.ToString()) + ")";
            spacer.Clear();
        }
        else
        {
            spacer.Append("0.");
            for (int i = 0; i < roundto.GetDecimalPlaceCount(); i++)
                spacer.Append('0');
            finalName = name + " (" + uiReturnFunc(Mathf.RoundToInt(defaultval / roundto) * roundto).ToString(spacer.ToString()) + ")";
            spacer.Clear();
        }
        OptionRange OR = new OptionRange(finalName, modname, defaultval, minval, maxval, roundto);
        ((Slider.SliderEvent)OR.onValueChanged).AddListener((float value) =>
        {
            value = Mathf.RoundToInt(value / roundto) * roundto;
            if (uiReturnFunc == null)
            {
                spacer.Append("0.");
                for (int i = 0; i < roundto.GetDecimalPlaceCount(); i++)
                    spacer.Append('0');
                OR.UIElement.transform.Find("Text").GetComponent<Text>().text =
                name + " (" + value.ToString(spacer.ToString()) + ")";
                spacer.Clear();
            }
            else
            {
                spacer.Append("0.");
                for (int i = 0; i < roundto.GetDecimalPlaceCount(); i++)
                    spacer.Append('0');
                OR.UIElement.transform.Find("Text").GetComponent<Text>().text =
                name + " (" + uiReturnFunc(value).ToString(spacer.ToString()) + ")";
                spacer.Clear();
            }
        });
        return OR;
    }
    /// <summary>
    /// Change the <see cref="OptionRange"/> text number at the end
    /// </summary>
    /// <param name="OR">To change</param>
    /// <param name="name">Name of the <see cref="OptionRange"/></param>
    /// <param name="value">The value to display</param>
    /// <param name="stringFormat">The string format mode</param>
    public static void UpdateTextNumber(this OptionRange OR, string name, float value, string stringFormat) =>
        OR.UIElement.transform.Find("Text").GetComponent<Text>().text = name + " (" + value.ToString(stringFormat) + ")";
}
