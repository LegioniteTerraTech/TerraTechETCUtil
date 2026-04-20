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
    /// Advanced <see cref="Option"/> with dynamically changing UI connected to it
    /// </summary>
    /// <param name="name">Name of the range</param>
    /// <param name="modname">Category to display this under</param>
    /// <param name="defaultval">The starting boolean state this begins with</param>
    /// <param name="uiReturnFunc">The special function to display the result of the option toggle value</param>
    /// <returns>The special <see cref="OptionToggle"/> with added functionality</returns>
    public static OptionToggle OptionToggleAutoDisplay(string name, string modname, bool defaultval,
        Func<bool, string> uiReturnFunc)
    {
        string finalName;
        if (uiReturnFunc == null)
            throw new ArgumentNullException("OptionToggleAutoDisplay expects a valid " + nameof(uiReturnFunc) + 
                " to use for the toggle.  Otherwise just use " + nameof(OptionToggle));
        else
            finalName = name + " (" + uiReturnFunc(defaultval) + ")";
        OptionToggle OT = new OptionToggle(finalName, modname, defaultval);
        ((Toggle.ToggleEvent)OT.onValueChanged).AddListener((bool value) =>
        {
            OT.UIElement.transform.Find("Text").GetComponent<Text>().text =
            name + " (" + uiReturnFunc(value) + ")";
        });
        return OT;
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
    /// <param name="uiReturnFunc">The special function to display the result of the option range value.</param>
    /// <returns>The special <see cref="OptionRange"/> with added functionality</returns>
    public static OptionRange OptionRangeAutoDisplay(string name, string modname, float defaultval = 0,
        float minval = 0, float maxval = 100, float roundto = 1, Func<float, float> uiReturnFunc = null)
    {
        string finalName;
        if (uiReturnFunc == null)
            uiReturnFunc = (inVal) => inVal;
        spacer.Append("0.");
        for (int i = 0; i < roundto.GetDecimalPlaceCount(); i++)
            spacer.Append('0');
        string spacerGet = spacer.ToString();
        spacer.Clear();
        finalName = name + " (" + uiReturnFunc(Mathf.RoundToInt(defaultval / roundto) * roundto).ToString(spacerGet) + ")";
        OptionRange OR = new OptionRange(finalName, modname, defaultval, minval, maxval, roundto);
        ((Slider.SliderEvent)OR.onValueChanged).AddListener((float value) =>
        {
            value = Mathf.RoundToInt(value / roundto) * roundto;
            OR.UIElement.transform.Find("Text").GetComponent<Text>().text =
            name + " (" + uiReturnFunc(value).ToString(spacerGet) + ")";
        });
        return OR;
    }
    /// <inheritdoc cref="OptionRangeAutoDisplay(string, string, float, float, float, float, Func{float, float})"/>
    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <param name="modname"></param>
    /// <param name="defaultval"></param>
    /// <param name="minval"></param>
    /// <param name="maxval"></param>
    /// <param name="roundto"></param>
    /// <param name="uiReturnFuncString">The special function to display the result of the option range value.
    /// <para>Return null to display nothing</para></param>
    /// <returns></returns>
    public static OptionRange OptionRangeAutoDisplay(string name, string modname, float defaultval = 0,
        float minval = 0, float maxval = 100, float roundto = 1, Func<float, string> uiReturnFuncString = null)
    {
        string finalName;
        if (uiReturnFuncString == null)
        {
            spacer.Append("0.");
            for (int i = 0; i < roundto.GetDecimalPlaceCount(); i++)
                spacer.Append('0');
            string spacerGet = spacer.ToString();
            spacer.Clear();
            uiReturnFuncString = (inVal) => inVal.ToString(spacerGet);
        }
        finalName = uiReturnFuncString(Mathf.RoundToInt(defaultval / roundto) * roundto);
        if (finalName != null)
            finalName = name + " (" + finalName + ")";
        else
            finalName = string.Empty;
        OptionRange OR = new OptionRange(finalName, modname, defaultval, minval, maxval, roundto);
        ((Slider.SliderEvent)OR.onValueChanged).AddListener((float value) =>
        {
            value = Mathf.RoundToInt(value / roundto) * roundto;
            OR.UIElement.transform.Find("Text").GetComponent<Text>().text =
            name + " (" + uiReturnFuncString(value) + ")";
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
