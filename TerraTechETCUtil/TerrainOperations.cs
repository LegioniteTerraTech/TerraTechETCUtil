using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TerraTechETCUtil
{
    public class TerrainOperations
    {
        public const float RescaleFactor = 4;
        public const float DownwardsOffset = 0;
        private static HashSet<Terrain> amped = new HashSet<Terrain>();
        public static float RescaledFactor = 4;

        internal static void AmplifyTerrain(Terrain Terra)
        {
            /*
            if (amped.Contains(Terra))
                return;
            amped.Add(Terra);
            */
            Terra.transform.position = Terra.transform.position - new Vector3(0, DownwardsOffset, 0);
            Debug_TTExt.Info("TerrainOperations: Amplifying Terrain....");
            TerrainData TD = Terra.terrainData;
            RescaledFactor = TD.size.y * RescaleFactor;
            TD.size = new Vector3(TD.size.x, RescaledFactor, TD.size.z);
            float[,] floats = TD.GetHeights(0, 0, 129, 129);
            for (int stepX = 0; stepX < 129; stepX++)
            {
                for (int stepY = 0; stepY < 129; stepY++)
                {
                    //floats.SetValue(floats[stepX, stepY] / RescaleFactor, stepX, stepY);
                    //floats.SetValue(floats[stepX, stepY] + (DownwardsOffset / maxH), stepX, stepY);
                    floats.SetValue(floats[stepX, stepY] / RescaleFactor, stepX, stepY);
                }
            }
            TD.SetHeights(0, 0, floats);
            Terra.terrainData = TD;
            Terra.Flush();
            Debug_TTExt.Info("TerrainOperations: Amplifying Terrain complete!");
        }

        internal static void LevelTerrain(WorldTile WT)
        {
            Debug_TTExt.Log("TerrainOperations: Leveling terrain....");
            TerrainData TD = WT.Terrain.terrainData;
            TD.size = new Vector3(TD.size.x, TD.size.y * RescaleFactor, TD.size.z);
            float[,] floats = TD.GetHeights(0, 0, 129, 129);
            double totalheight = 0;
            foreach (float flo in floats)
                totalheight += flo;
            totalheight /= floats.Length;
            float th = (float)totalheight;
            for (int stepX = 1; stepX < 129; stepX++)
                for (int stepY = 1; stepY < 129; stepY++)
                    floats.SetValue(th, stepX, stepY);
            TD.SetHeights(0, 0, floats);
            WT.Terrain.terrainData = TD;
            WT.Terrain.Flush();
            Debug_TTExt.Log("TerrainOperations: Leveling terrain complete!");
        }
    }
}
