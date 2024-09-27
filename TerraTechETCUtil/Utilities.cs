using System;
using System.Collections.Generic;
using System.Linq;
#if !EDITOR
using HarmonyLib;
#endif
using UnityEngine;

namespace TerraTechETCUtil
{
    public struct ColorBytes
    {
        public byte r;
        public byte g;
        public byte b;
        public byte a;

        public ColorBytes(byte R, byte G, byte B)
        {
            r = R;
            g = G;
            b = B;
            a = byte.MaxValue;
        }
        public ColorBytes(byte R, byte G, byte B, byte A)
        {
            r = R;
            g = G;
            b = B;
            a = A;
        }
        public ColorBytes(Color color)
        {
            r = (byte)Mathf.RoundToInt(Mathf.Clamp01(color.r * byte.MaxValue));
            g = (byte)Mathf.RoundToInt(Mathf.Clamp01(color.g * byte.MaxValue));
            b = (byte)Mathf.RoundToInt(Mathf.Clamp01(color.b * byte.MaxValue));
            a = (byte)Mathf.RoundToInt(Mathf.Clamp01(color.a * byte.MaxValue));
        }
        public ColorBytes(float R, float G, float B)
        {
            r = (byte)Mathf.RoundToInt(Mathf.Clamp01(R * byte.MaxValue));
            g = (byte)Mathf.RoundToInt(Mathf.Clamp01(G * byte.MaxValue));
            b = (byte)Mathf.RoundToInt(Mathf.Clamp01(B * byte.MaxValue));
            a = byte.MaxValue;
        }
        public ColorBytes(float R, float G, float B, float A)
        {
            r = (byte)Mathf.RoundToInt(Mathf.Clamp01(R * byte.MaxValue));
            g = (byte)Mathf.RoundToInt(Mathf.Clamp01(G * byte.MaxValue));
            b = (byte)Mathf.RoundToInt(Mathf.Clamp01(B * byte.MaxValue));
            a = (byte)Mathf.RoundToInt(Mathf.Clamp01(A * byte.MaxValue));
        }
        public Color ToRGBAFloat()
        {
            return new Color(
                Mathf.Clamp01(r / (float)byte.MaxValue),
                Mathf.Clamp01(g / (float)byte.MaxValue),
                Mathf.Clamp01(b / (float)byte.MaxValue),
                Mathf.Clamp01(a / (float)byte.MaxValue)
            );
        }
        public override string ToString()
        {
            return r.ToString("X2") + g.ToString("X2") + b.ToString("X2") + a.ToString("X2");
        }
        public string ColorString(string ToColor)
        {
            return "<color=" + r.ToString("X2") + g.ToString("X2") + b.ToString("X2") + a.ToString("X2") + ">" + ToColor + "</color>";
        }
    }

    /// <summary>
    /// Manages extension methods for various classes.
    /// </summary>
    public static class Utilities
    {

        public static ColorBytes ToRGBA255(this Color toNum)
        {
            return new ColorBytes(toNum);
        }
        public static string ToHex(this Color toHex)
        {
            return new ColorBytes(toHex).ToString();
        }

        /// <summary>
        /// Get and set a flag QUICKLY!
        /// </summary>
        /// <typeparam name="T">Enum (MUST BE INT32)</typeparam>
        /// <param name="inst">The enum to adjust</param>
        /// <param name="flagBit">The flag to adjust</param>
        /// <param name="trueState">The state to set</param>
        /// <returns>True if it changed the enum value</returns>
        public static bool GetSetFlagBitShift<T>(this ref T inst, T flagBit, bool trueState) where T : struct, Enum
        {
            int valM = (int)(object)inst;
            int valF = 1 << (int)(object)flagBit;
            bool curState = (valM & valF) != 0;
            if (curState != trueState)
            {
                inst = (T)(object)((valM & ~valF) | (trueState ? valF : 0));
                return true;
            }
            return false;
        }
        /// <summary>
        /// Get and set a flag QUICKLY!
        /// </summary>
        /// <typeparam name="T">Enum (MUST BE INT32)</typeparam>
        /// <param name="inst">The enum to adjust</param>
        /// <param name="flagBit">The flag to adjust</param>
        /// <param name="trueState">The state to set</param>
        /// <returns>True if it changed the enum value</returns>
        public static bool GetSetFlag<T>(this ref T inst, T flagBit, bool trueState) where T : struct, Enum
        {
            int valM = (int)(object)inst;
            int valF = (int)(object)flagBit;
            bool curState = (valM & valF) != 0;
            if (curState != trueState)
            {
                inst = (T)(object)((valM & ~valF) | (trueState ? valF : 0));
                return true;
            }
            return false;
        }
        /// <summary>
        /// Gets flags in enum
        /// </summary>
        /// <typeparam name="T">Enum</typeparam>
        /// <param name="inst">The enum to check</param>
        /// <param name="flag">The flag to test for</param>
        public static bool HasAnyFlag<T>(ref this T inst, T flag) where T : struct, Enum
        {
            switch (Convert.GetTypeCode(inst))
            {
                case TypeCode.SByte:
                    return ((sbyte)(object)inst & (sbyte)(object)flag) != 0;
                case TypeCode.Byte:
                    return ((byte)(object)inst & (byte)(object)flag) != 0;
                case TypeCode.Int16:
                    return ((short)(object)inst & (short)(object)flag) != 0;
                case TypeCode.UInt16:
                    return ((ushort)(object)inst & (ushort)(object)flag) != 0;
                case TypeCode.Int32:
                    return ((int)(object)inst & (int)(object)flag) != 0;
                case TypeCode.UInt32:
                    return ((uint)(object)inst & (uint)(object)flag) != 0;
                default:
                    throw new NotImplementedException(Convert.GetTypeCode(inst).ToString());
            }
        }
        /// <summary>
        /// Gets flags that aren't bit-shifted in enum
        /// </summary>
        /// <typeparam name="T">Enum</typeparam>
        /// <param name="inst">The enum to check</param>
        /// <param name="flag">The flag to test for</param>
        public static bool HasFlagBitShift<T>(ref this T inst, T flag) where T : struct, Enum
        {
            switch (Convert.GetTypeCode(inst))
            {
                case TypeCode.SByte:
                    return ((sbyte)(object)inst & (1 << (sbyte)(object)flag)) != 0;
                case TypeCode.Byte:
                    return ((byte)(object)inst & (1U << (byte)(object)flag)) != 0;
                case TypeCode.Int16:
                    return ((short)(object)inst & (1 << (short)(object)flag)) != 0;
                case TypeCode.UInt16:
                    return ((ushort)(object)inst & (1U << (ushort)(object)flag)) != 0;
                case TypeCode.Int32:
                    return ((int)(object)inst & (1 << (int)(object)flag)) != 0;
                case TypeCode.UInt32:
                    return ((uint)(object)inst & (1U << (int)(uint)(object)flag)) != 0;
                default:
                    throw new NotImplementedException(Convert.GetTypeCode(inst).ToString());
            }
        }

        /// <summary>
        /// Sets flags that are already bit-shifted in enum
        /// </summary>
        /// <typeparam name="T">Enum</typeparam>
        /// <param name="inst">The enum to set</param>
        /// <param name="flags">The flags to set</param>
        /// <param name="state">The state to set</param>
        public static void SetFlags<T>(ref this T inst, T flags, bool state) where T : struct, Enum
        {
            switch (Convert.GetTypeCode(inst))
            {
                case TypeCode.SByte:
                    inst = (T)(object)(((sbyte)(object)inst) & (~(sbyte)(object)flags) | (state ? (sbyte)(object)flags : 0));
                    break;
                case TypeCode.Byte:
                    inst = (T)(object)(((byte)(object)inst) & (~(byte)(object)flags) | (state ? (byte)(object)flags : 0));
                    break;
                case TypeCode.Int16:
                    inst = (T)(object)(((short)(object)inst) & (~(short)(object)flags) | (state ? (short)(object)flags : 0));
                    break;
                case TypeCode.UInt16:
                    inst = (T)(object)(((ushort)(object)inst) & (~(ushort)(object)flags) | (state ? (ushort)(object)flags : 0));
                    break;
                case TypeCode.Int32:
                    inst = (T)(object)(((int)(object)inst) & (~(int)(object)flags) | (state ? (int)(object)flags : 0));
                    break;
                case TypeCode.UInt32:
                    inst = (T)(object)(((uint)(object)inst) & (~(uint)(object)flags) | (state ? (uint)(object)flags : 0));
                    break;
                default:
                    throw new NotImplementedException(Convert.GetTypeCode(inst).ToString());
            }
        }
        /// <summary>
        /// Sets flags that aren't bit-shifted in enum
        /// </summary>
        /// <typeparam name="T">Enum</typeparam>
        /// <param name="inst"></param>
        /// <param name="state">The state to set</param>
        /// <param name="flags">All targeted flags</param>
        public static void SetFlagsBitShift<T>(ref this T inst, bool state, params T[] flags) where T : struct, Enum
        {
            int combinedFlag = 0;
            switch (Convert.GetTypeCode(inst))
            {
                case TypeCode.SByte:
                    foreach (var flag in flags)
                        combinedFlag |= 1 << (sbyte)(object)flag;
                    inst.SetFlags((T)(object)(sbyte)combinedFlag, state);
                    break;
                case TypeCode.Byte:
                    foreach (var flag in flags)
                        combinedFlag |= 1 << (int)(object)flag;
                    inst.SetFlags((T)(object)(byte)combinedFlag, state);
                    break;
                case TypeCode.Int16:
                    foreach (var flag in flags)
                        combinedFlag |= 1 << (int)(object)flag;
                    inst.SetFlags((T)(object)(short)combinedFlag, state);
                    break;
                case TypeCode.UInt16:
                    foreach (var flag in flags)
                        combinedFlag |= 1 << (int)(object)flag;
                    inst.SetFlags((T)(object)(ushort)combinedFlag, state);
                    break;
                case TypeCode.Int32:
                    foreach (var flag in flags)
                        combinedFlag |= 1 << (int)(object)flag;
                    inst.SetFlags((T)(object)combinedFlag, state);
                    break;
                case TypeCode.UInt32:
                    uint combinedFlags32U = 0;
                    foreach (var flag in flags)
                        combinedFlags32U |= 1U << (int)(uint)(object)flag;
                    inst.SetFlags((T)(object)combinedFlags32U, state);
                    break;
                default:
                    throw new NotImplementedException(Convert.GetTypeCode(inst).ToString());
            }
        }



        public static IEnumerable<IntVector2> IterateCircleVolume(this IntVector2 iVec2, float radius)
        {
            //tan = sin/cos
            yield return iVec2;
            int outerLim = Mathf.RoundToInt(radius);
            for (int step = 1; step < outerLim; step++)
            {
                yield return new IntVector2(step, 0) + iVec2;
                yield return new IntVector2(0, step) + iVec2;
                yield return new IntVector2(-step, 0) + iVec2;
                yield return new IntVector2(0, -step) + iVec2;
            }
            //Debug_TTExt.Log("IterateCircleVolume(1) iterated " + circleElements.Count.ToString() + " entries for radius " + radius);
            // does one QUADRANT and then re-uses that
            // a^2 = b^2 + c^2
            for (int x = 1; x < outerLim; x++)
            {
                int widthOffset = x - iVec2.x;
                int yMax = Mathf.RoundToInt(Mathf.Sqrt((radius * radius) - (widthOffset * widthOffset)) + iVec2.y);
                for (int y = 1; y < yMax; y++)
                {
                    yield return new IntVector2(x, y) + iVec2;
                    yield return new IntVector2(-x, y) + iVec2;
                    yield return new IntVector2(x, -y) + iVec2;
                    yield return new IntVector2(-x, -y) + iVec2;
                }
            }
            /*Debug_TTExt.Log("IterateCircleVolume(2) iterated " + circleElements.Count.ToString() + " entries for radius " + radius);
            foreach (var item in circleElements)
            {
                Debug_TTExt.Log(item.ToString());
            }*/
        }
        private static List<IntVector2> circleElements = new List<IntVector2>();
        public static IEnumerable<IntVector2> IterateCircleVolume_LEGACY(this IntVector2 iVec2, float radius)
        {
            //tan = sin/cos
            circleElements.Clear();
            circleElements.Add(iVec2);
            int outerLim = Mathf.RoundToInt(radius);
            for (int step = 1; step < outerLim; step++)
            {
                circleElements.Add(new IntVector2(step, 0) + iVec2);
                circleElements.Add(new IntVector2(0, step) + iVec2);
                circleElements.Add(new IntVector2(-step, 0) + iVec2);
                circleElements.Add(new IntVector2(0, -step) + iVec2);
            }
            //Debug_TTExt.Log("IterateCircleVolume(1) iterated " + circleElements.Count.ToString() + " entries for radius " + radius);
            // does one QUADRANT and then re-uses that
            // a^2 = b^2 + c^2
            for (int x = 1; x < outerLim; x++)
            {
                int widthOffset = x - iVec2.x;
                int yMax = Mathf.RoundToInt(Mathf.Sqrt((radius * radius) - (widthOffset * widthOffset)) + iVec2.y);
                for (int y = 1; y < yMax; y++)
                {
                    circleElements.Add(new IntVector2(x, y) + iVec2);
                    circleElements.Add(new IntVector2(-x, y) + iVec2);
                    circleElements.Add(new IntVector2(x, -y) + iVec2);
                    circleElements.Add(new IntVector2(-x, -y) + iVec2);
                }
            }
            /*Debug_TTExt.Log("IterateCircleVolume(2) iterated " + circleElements.Count.ToString() + " entries for radius " + radius);
            foreach (var item in circleElements)
            {
                Debug_TTExt.Log(item.ToString());
            }*/
            return circleElements;
        }
        public static IEnumerable<IntVector2> IterateRectVolume(this IntVector2 iVec2, IntVector2 Dimensions)
        {
            for (int x = 0; x < Dimensions.x; x++)
            {
                for (int y = 0; y < Dimensions.y; y++)
                {
                    yield return new IntVector2(x, y) + iVec2;
                }
            }
        }
        public static IEnumerable<IntVector2> IterateRectVolumeCentered(this IntVector2 iVec2, IntVector2 Dimensions)
        {
            return IterateRectVolume(new IntVector2(iVec2.x + (Dimensions.x / 2), iVec2.y + (Dimensions.y / 2)), Dimensions);
        }

        public static void LogGameObjectHierachy(GameObject GO, int Maxdepth = 16)
        {
            try
            {
                ExtractGameObjectHierachy_Internal(GO, 0, Maxdepth);
            }
            catch (Exception e)
            {
                throw new Exception("LogGameObjectHierachy FAILED - ", e);
            }
        }
        private static void ExtractGameObjectHierachy_Internal(GameObject GO, int depth, int leftoverDepth)
        {
            leftoverDepth--;
            string depthParse = "";
            for (int i = 0; i < depth; i++)
                depthParse += "  ";
            Debug_TTExt.Log(depthParse + "{");
            Transform trans = GO.transform;
            Debug_TTExt.Log(depthParse + "Name:     " + (GO.name.NullOrEmpty() ? "<NULL_NAME>" : GO.name));
            Debug_TTExt.Log(depthParse + "Pos:      " + trans.position.ToString());
            Debug_TTExt.Log(depthParse + "Rot:      " + trans.eulerAngles.ToString());
            if (trans.parent != null)
                Debug_TTExt.Log(depthParse + "Parent:   " + (trans.parent.name.NullOrEmpty() ? "<NULL_NAME>" : trans.parent.name));
            else
                Debug_TTExt.Log(depthParse + "Parent:   <NONE>");
            foreach (var item in GO.GetComponents<Component>())
            {
                Debug_TTExt.Log(depthParse + "Component: " + item.GetType());
            }
            if (leftoverDepth > 0)
            {
                depth++;
                for (int i = 0; i < trans.childCount; i++)
                {
                    ExtractGameObjectHierachy_Internal(trans.GetChild(i).gameObject, depth, leftoverDepth);
                }
            }
            Debug_TTExt.Log(depthParse + "}");
        }



        /// <summary>
        /// When a ExtModule must respond to block attach updates, use this.
        /// For block attachment updates when Tank is set to a valid reference.
        /// </summary>
        /// <param name="TB">TankBlock instance</param>
        /// <param name="attachEvent">Action to call on block attach.</param>
        /// <param name="detachEvent">Action to call on block detach.</param>
        public static void SubToBlockAttachConnected(this TankBlock TB, Action attachEvent, Action detachEvent)
        {
            if (attachEvent != null)
                TB.AttachedEvent.Subscribe(attachEvent);
            if (detachEvent != null)
                TB.DetachingEvent.Subscribe(detachEvent);
        }

        /// <summary>
        /// When a ExtModule no longer needs to respond to block attach updates, use this.
        /// For block attachment updates when Tank is set to a valid reference.
        /// </summary>
        /// <param name="TB">TankBlock instance</param>
        /// <param name="attachEvent">Action to call on block attach.</param>
        /// <param name="detachEvent">Action to call on block detach.</param>
        public static void UnSubToBlockAttachConnected(this TankBlock TB, Action attachEvent, Action detachEvent)
        {
            if (attachEvent != null)
                TB.AttachedEvent.Unsubscribe(attachEvent);
            if (detachEvent != null)
                TB.DetachingEvent.Unsubscribe(detachEvent);
        }

        /// <summary>
        /// Finds the first Transform that matches the given name.
        /// Case sensitive.
        /// </summary>
        /// <param name="trans">The parent Transform to search through.</param>
        /// <param name="name">The name of the Transform to find. Case sensitive.</param>
        /// <returns>The found Transform if any, otherwise returns null.</returns>
        public static Transform HeavyTransformSearch(this Transform trans, string name)
        {
            if (name.NullOrEmpty())
                return null;
            return trans.gameObject.GetComponentsInChildren<Transform>().FirstOrDefault(delegate (Transform cand)
            {
                if (cand.name.NullOrEmpty())
                    return false;
                return cand.name.CompareTo(name) == 0;
            });
        }

        /// <summary>
        /// Finds all Components of type T within a given GameObject's children.
        /// </summary>
        /// <typeparam name="T">The type to search for.</typeparam>
        /// <param name="GO">The GameObject to search.</param>
        public static void PrintAllComponentsGameObjectDepth<T>(GameObject GO) where T : Component
        {
            Debug_TTExt.Log("-------------------------------------------");
            Debug_TTExt.Log("PrintAllComponentsGameObjectDepth - For " + typeof(T).Name);
            Debug_TTExt.Log(" -- " + GO.name);
            foreach (var item in GO.GetComponentsInChildren<T>(true))
            {
                Debug_TTExt.Log("-------------------------------------------");
                Debug_TTExt.Log(" - " + item.gameObject.name);
                Transform trans = item.transform.parent;
                while (trans != null)
                {
                    Debug_TTExt.Log("  " + trans.gameObject.name);
                    trans = trans.parent;
                }
            }
            Debug_TTExt.Log("-------------------------------------------");
        }


        /// <summary>
        /// Add to a ICollection within a IDictionary.
        /// </summary>
        /// <typeparam name="T">IDictionary</typeparam>
        /// <typeparam name="V">ICollection</typeparam>
        /// <typeparam name="E">Within ICollection</typeparam>
        /// <param name="dict">IDictionary instance</param>
        /// <param name="key">Key to look up in the IDictionary</param>
        /// <param name="typeToAdd">The element to add to the list nested in the dictionary.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void AddInlined<T, V, E>(this IDictionary<T,V> dict, T key, E typeToAdd) where V : ICollection<E>
        {
            if (dict.TryGetValue(key, out V val))
            {
                val.Add(typeToAdd);
            }
            else
            {
                V newIEnumerable = ((V)Activator.CreateInstance(typeof(V)));
                newIEnumerable.Add(typeToAdd);
                dict.Add(key, newIEnumerable);
            }
        }

        /// <summary>
        /// Add to a ICollection within a IDictionary.
        /// </summary>
        /// <typeparam name="T">IDictionary</typeparam>
        /// <typeparam name="V">ICollection</typeparam>
        /// <typeparam name="E">Within ICollection</typeparam>
        /// <param name="dict">IDictionary instance</param>
        /// <param name="key">Key to look up in the IDictionary</param>
        /// <param name="typeToAdd">The element to add to the list nested in the dictionary.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void GetInlined<T, V, E>(this IDictionary<T, V> dict, T key, E typeToAdd) where V : ICollection<E>
        {
            if (dict.TryGetValue(key, out V val))
            {
                val.Add(typeToAdd);
            }
            else
            {
                V newIEnumerable = ((V)Activator.CreateInstance(typeof(V)));
                newIEnumerable.Add(typeToAdd);
                dict.Add(key, newIEnumerable);
            }
        }
        /// <summary>
        /// Remove from an ICollection within a IDictionary.
        /// </summary>
        /// <typeparam name="T">IDictionary</typeparam>
        /// <typeparam name="V">ICollection</typeparam>
        /// <typeparam name="E">Within ICollection</typeparam>
        /// <param name="dict">IDictionary instance</param>
        /// <param name="key">Key to look up in the IDictionary.</param>
        /// <param name="typeToRemove">The element to remove from the list nested in the dictionary.</param>
        /// <returns>true if the element was successfully removed.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static bool RemoveInlined<T, V, E>(this IDictionary<T, V> dict, T key, E typeToRemove) where V : ICollection<E>
        {
            if (dict.TryGetValue(key, out V val))
            {
                bool worked = val.Remove(typeToRemove);
                dict[key] = val;
                return worked;
            }
            return false;
        }

        /// <summary>
        /// Add to a IDictionary within a IDictionary.
        /// </summary>
        /// <typeparam name="T">IDictionary key</typeparam>
        /// <typeparam name="V">IDictionary nested within T</typeparam>
        /// <typeparam name="E">Nested IDictionary key</typeparam>
        /// <typeparam name="A">Within IDictionary</typeparam>
        /// <param name="dict">Dictionary instance</param>
        /// <param name="key">Key to look up in the Dictionary</param>
        /// <param name="typeToAdd">The element to add to the dictionary nested in the dictionary.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void AddInlined<T, V, E, A>(this IDictionary<T, V> dict, T key, E keyNested, A typeToAdd) where V : IDictionary<E,A>
        {
            if (dict.TryGetValue(key, out V val))
            {
                val.Add(keyNested, typeToAdd);
            }
            else
            {
                V newIDictionary = ((V)Activator.CreateInstance(typeof(V)));
                newIDictionary.Add(keyNested, typeToAdd);
                dict.Add(key, newIDictionary);
            }
        }
        /// <summary>
        /// Remove from an IDictionary within a IDictionary.
        /// </summary>
        /// <typeparam name="T">IDictionary key</typeparam>
        /// <typeparam name="V">IDictionary nested within T</typeparam>
        /// <typeparam name="E">Nested IDictionary key</typeparam>
        /// <typeparam name="A">Within IDictionary</typeparam>
        /// <param name="dict">Dictionary instance</param>
        /// <param name="key">Key to look up in the IDictionary.</param>
        /// <param name="typeToRemove">The key to remove from the dictionary nested in the dictionary.</param>
        /// <returns>true if the element was successfully removed.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static bool RemoveInlined<T, V, E, A>(this IDictionary<T, V> dict, T key, E typeToRemove) where V : IDictionary<E, A>
        {
            if (dict.TryGetValue(key, out V val))
            {
                bool worked = val.Remove(typeToRemove);
                dict[key] = val;
                return worked;
            }
            return false;
        }
    }
}
