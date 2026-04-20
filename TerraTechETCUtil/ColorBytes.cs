using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TerraTechETCUtil
{
    /// <summary>
    /// A quick-access low memory use struct for coloring things
    /// </summary>
    public struct ColorBytes
    {
        /// <summary> Red </summary>
        public byte r;
        /// <summary> Green </summary>
        public byte g;
        /// <summary> Blue </summary>
        public byte b;
        /// <summary> Alpha </summary>
        public byte a;

        /// <inheritdoc cref="ColorBytes.ColorBytes(byte, byte, byte, byte)"/>
        public ColorBytes(byte R, byte G, byte B)
        {
            r = R;
            g = G;
            b = B;
            a = byte.MaxValue;
        }
        /// <summary>
        /// Setup <see cref="ColorBytes"/> to use
        /// </summary>
        /// <param name="R">Red</param>
        /// <param name="G">Green</param>
        /// <param name="B">Blue</param>
        /// <param name="A">Alpha</param>
        public ColorBytes(byte R, byte G, byte B, byte A)
        {
            r = R;
            g = G;
            b = B;
            a = A;
        }

        /// <summary>
        /// <inheritdoc cref="ColorBytes.ColorBytes(byte, byte, byte, byte)"/>
        /// <para><b>Lossy</b></para>
        /// </summary>
        /// <param name="color">The color to copy from.</param>
        public ColorBytes(Color color)
        {
            r = (byte)Mathf.RoundToInt(Mathf.Clamp01(color.r) * byte.MaxValue);
            g = (byte)Mathf.RoundToInt(Mathf.Clamp01(color.g) * byte.MaxValue);
            b = (byte)Mathf.RoundToInt(Mathf.Clamp01(color.b) * byte.MaxValue);
            a = (byte)Mathf.RoundToInt(Mathf.Clamp01(color.a) * byte.MaxValue);
        }
        /// <inheritdoc cref="ColorBytes.ColorBytes(byte, byte, byte, byte)"/>
        public ColorBytes(float R, float G, float B)
        {
            r = (byte)Mathf.RoundToInt(Mathf.Clamp01(R) * byte.MaxValue);
            g = (byte)Mathf.RoundToInt(Mathf.Clamp01(G) * byte.MaxValue);
            b = (byte)Mathf.RoundToInt(Mathf.Clamp01(B) * byte.MaxValue);
            a = byte.MaxValue;
        }
        /// <inheritdoc cref="ColorBytes.ColorBytes(byte, byte, byte, byte)"/>
        public ColorBytes(float R, float G, float B, float A)
        {
            r = (byte)Mathf.RoundToInt(Mathf.Clamp01(R) * byte.MaxValue);
            g = (byte)Mathf.RoundToInt(Mathf.Clamp01(G) * byte.MaxValue);
            b = (byte)Mathf.RoundToInt(Mathf.Clamp01(B) * byte.MaxValue);
            a = (byte)Mathf.RoundToInt(Mathf.Clamp01(A) * byte.MaxValue);
        }
        /// <summary>
        /// To <see cref="Color"/> type. Still lossy
        /// </summary>
        /// <returns></returns>
        public Color ToRGBAFloat()
        {
            return new Color(
                Mathf.Clamp01(r / (float)byte.MaxValue),
                Mathf.Clamp01(g / (float)byte.MaxValue),
                Mathf.Clamp01(b / (float)byte.MaxValue),
                Mathf.Clamp01(a / (float)byte.MaxValue)
            );
        }
        /// <inheritdoc/>
        public override string ToString()
        {
            return r.ToString("X2") + g.ToString("X2") + b.ToString("X2") + a.ToString("X2");
        }
        /// <summary>
        /// Color the given string
        /// </summary>
        /// <param name="ToColor">string to color</param>
        /// <returns>the given string with the color formatting around it</returns>
        public string ColorString(string ToColor)
        {
            return "<color=#" + r.ToString("X2") + g.ToString("X2") + b.ToString("X2") + a.ToString("X2") + ">" + ToColor + "</color>";
        }
    }

}
