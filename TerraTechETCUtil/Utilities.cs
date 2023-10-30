using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

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


        private static List<IntVector2> circleElements = new List<IntVector2>();
        public static IEnumerable<IntVector2> IterateCircleVolume(this IntVector2 iVec2, float radius)
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
        /// Add to a ICollection within a Dictionary.
        /// </summary>
        /// <typeparam name="T">Dictionary</typeparam>
        /// <typeparam name="V">ICollection</typeparam>
        /// <typeparam name="E">Within ICollection</typeparam>
        /// <param name="dict">Dictionary instance</param>
        /// <param name="key">Key to look up in the Dictionary</param>
        /// <param name="typeToAdd">The element to add to the list nested in the dictionary.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void AddInlined<T, V, E>(this Dictionary<T,V> dict, T key, E typeToAdd) where V : ICollection<E>
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
        /// Remove from an ICollection within a Dictionary.
        /// </summary>
        /// <typeparam name="T">Dictionary</typeparam>
        /// <typeparam name="V">ICollection</typeparam>
        /// <typeparam name="E">Within ICollection</typeparam>
        /// <param name="dict">Dictionary instance</param>
        /// <param name="key">Key to look up in the Dictionary.</param>
        /// <param name="typeToRemove">The element to remove from the list nested in the dictionary.</param>
        /// <returns>true if the element was successfully removed.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static bool RemoveInlined<T, V, E>(this Dictionary<T, V> dict, T key, E typeToRemove) where V : ICollection<E>
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
