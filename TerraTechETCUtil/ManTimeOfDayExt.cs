using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;

namespace TerraTechETCUtil
{
    /// <summary>
    /// Override ManTimeOfDay SAFELY without making a mess!
    /// </summary>
    public class ManTimeOfDayExt
    {
        public struct TOD_Ordering
        {
            public int priority;
            public Action setTheSky;
            public string ModID;
        }
        private static Dictionary<string, Action> modIDs = new Dictionary<string, Action>();
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

        /// <summary>
        /// Higher priority goes first
        /// </summary>
        /// <param name="modID"></param>
        /// <param name="priority"></param>
        /// <param name="setTheSky"></param>
        /// <returns></returns>
        public static bool SetState(string modID, int priority, Action setTheSky)
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
            if (!modIDs.ContainsKey(modID))
            {
                modIDs.Add(modID, setTheSky);
                if (!applyCommand.Any() || applyCommand.First().priority <= priority)
                    setTheSky();
                int index = applyCommand.FindIndex(x => x.priority < priority);
                if (index == -1)
                    index = applyCommand.Count;
                applyCommand.Insert(index, new TOD_Ordering
                {
                    ModID = modID,
                    priority = priority,
                    setTheSky = setTheSky,
                });
                return true;
            }
            else if (applyCommand.First().priority <= priority)
                setTheSky();
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
                applyCommand.First().setTheSky();
        }
    }
}
