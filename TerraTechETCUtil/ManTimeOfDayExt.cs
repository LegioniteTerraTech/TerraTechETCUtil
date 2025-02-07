using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using static Biome;

namespace TerraTechETCUtil
{
    /// <summary>
    /// Override ManTimeOfDay SAFELY without making a mess!
    /// </summary>
    public class ManTimeOfDayExt
    {
        public class TOD_Ordering
        {
            public TOD_Ordering(string ModID, int priority, Func<Color, Color> ColorForSetSkyInOrder, Action<DayNightColours, DayNightColours> KeepTheSkyInOrder)
            {
                this.ModID = ModID;
                this.priority = priority;
                setTheSky = ColorForSetSkyInOrder;
                keepTheSky = KeepTheSkyInOrder;
            }
            public readonly int priority;
            public readonly Func<Color,Color> setTheSky;
            public readonly Action<DayNightColours,DayNightColours> keepTheSky;
            public readonly string ModID;
        }
        private static Dictionary<string, TOD_Ordering> modIDs = new Dictionary<string, TOD_Ordering>();
        private static List<TOD_Ordering> applyCommand = new List<TOD_Ordering>();
        private static FieldInfo m_Sky = typeof(ManTimeOfDay).GetField("m_Sky", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FogMode fM = RenderSettings.fogMode;
        private static TOD_FogType todFt;
        private static TOD_AmbientType todAt;
        private static bool Fog = RenderSettings.fog;
        private static float fDens = RenderSettings.fogDensity;
        private static Color fogColor = RenderSettings.fogColor;
        private static Color ambientLight = RenderSettings.ambientLight;
        private static Color ambientGroundColor = RenderSettings.ambientGroundColor;
        private static float ambientIntensity = RenderSettings.ambientIntensity;

        private static float fogStartDistance = RenderSettings.fogStartDistance;
        private static float fogEndDistance = RenderSettings.fogEndDistance;

        private static Gradient dayFogColors;
        private static Gradient nightFogColors;
        private static Gradient dayLightColors;
        private static Gradient nightLightColors;
        private static Gradient daySkyColors;
        private static Gradient nightSkyColors;
        private static Gradient dayAmbColors;
        private static Gradient nightAmbColors;
        private static float updateFrame = Time.time;

        /// <summary>
        /// Higher priority goes first
        /// </summary>
        /// <param name="modID"></param>
        /// <param name="priority"></param>
        /// <param name="setTheSky"></param>
        /// <returns></returns>
        public static bool SetState(TOD_Ordering order)
        {
            if (modIDs.Count == 0)
            {
                var sky = m_Sky.GetValue(ManTimeOfDay.inst) as TOD_Sky;

                fogStartDistance = RenderSettings.fogStartDistance;
                fogEndDistance = RenderSettings.fogEndDistance;

                fogColor = RenderSettings.fogColor;
                ambientLight = RenderSettings.ambientLight;
                ambientGroundColor = RenderSettings.ambientGroundColor;
                ambientIntensity = RenderSettings.ambientIntensity;

                todFt = sky.Fog.Mode;
                todAt = sky.Ambient.Mode;
                dayFogColors = sky.Day.FogColor;
                nightFogColors = sky.Night.FogColor;
                daySkyColors = sky.Day.SkyColor;
                nightSkyColors = sky.Night.SkyColor;
                dayLightColors = sky.Day.LightColor;
                nightLightColors = sky.Night.LightColor;
                dayAmbColors = sky.Day.AmbientColor;
                nightAmbColors = sky.Night.AmbientColor;
                ManTimeOfDay.inst.DayNightChangedEvent.Subscribe(ReinforceState);
            }
            if (!modIDs.ContainsKey(order.ModID))
            {
                modIDs.Add(order.ModID, order);
                if (!applyCommand.Any() || applyCommand.First().priority <= order.priority)
                    if (updateFrame != Time.time)
                    {
                        ReinforceState();
                        updateFrame = Time.time;
                    }
                int index = applyCommand.FindIndex(x => x.priority < order.priority);
                if (index == -1)
                    index = applyCommand.Count;
                applyCommand.Insert(index, order);
                return true;
            }
            else if (applyCommand.First().priority <= order.priority)
                if (updateFrame != Time.time)
                {
                    ReinforceState();
                    updateFrame = Time.time;
                }
            return false;

        }
        public static void RemoveState(string modID)
        {
            if (modIDs.TryGetValue(modID, out var val))
            {
                applyCommand.RemoveAll(x => x.ModID == modID);
                modIDs.Remove(modID);
                ReinforceState();
            }

            if (modIDs.Count == 0)
            {
                ManTimeOfDay.inst.DayNightChangedEvent.Unsubscribe(ReinforceState);
                var sky = m_Sky.GetValue(ManTimeOfDay.inst) as TOD_Sky;
                RenderSettings.fog = Fog;
                RenderSettings.fogMode = fM;
                RenderSettings.fogDensity = fDens;
                sky.Fog.Mode = todFt;

                RenderSettings.fogStartDistance = fogStartDistance;
                RenderSettings.fogEndDistance = fogEndDistance;

                sky.Ambient.Mode = todAt;
                RenderSettings.fogColor = fogColor;
                RenderSettings.ambientLight = ambientLight;
                RenderSettings.ambientGroundColor = ambientGroundColor;
                RenderSettings.ambientIntensity = ambientIntensity;

                sky.Day.FogColor = dayFogColors;
                sky.Night.FogColor = nightFogColors;
                sky.Day.SkyColor = daySkyColors;
                sky.Night.SkyColor = nightSkyColors;
                sky.Day.LightColor = dayLightColors;
                sky.Night.LightColor = nightLightColors;
                sky.Day.AmbientColor = dayAmbColors;
                sky.Night.AmbientColor = nightAmbColors;
                sky.m_UseTerraTechBiomeData = true;
            }
        }
        public static void ReinforceState(bool ignored = false)
        {
            if (applyCommand.Any())
            {
                Color transColor = new Color(1f, 1f, 1f, 1f);
                foreach (var item in applyCommand)
                {
                    transColor = item.setTheSky(transColor);
                }
            }
        }
        public static void ReinforceStateActive(ref DayNightColours dayColours, ref DayNightColours nightColours)
        {
            if (applyCommand.Any())
            {
                foreach (var item in applyCommand)
                {
                    item.keepTheSky(dayColours, nightColours);
                }
            }
        }
    }
}
