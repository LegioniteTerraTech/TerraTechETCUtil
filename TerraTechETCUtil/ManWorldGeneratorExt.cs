using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using TerraTechETCUtil;
using MonoMod.Utils;
using Newtonsoft.Json;
using System.IO;

namespace TerraTechETCUtil
{
    public class ManWorldGeneratorExt
    {
        /// <summary>
        /// The terrain has been vertically expanded by TerrainOperations.RescaleFactor = 4x
        /// </summary>
        public static bool AmplifiedTerrain => preInit;
        public static float CurrentHeightMultiplier => AmplifiedTerrain ? TerrainOperations.RescaleFactor : 1f;
        public static float CurrentTotalHeight => AmplifiedTerrain ? TerrainOperations.TileHeightRescaled : TerrainOperations.TileHeightDefault;
        public static float CurrentMinHeight => AmplifiedTerrain ? TerrainOperations.TileYOffsetRescaled : TerrainOperations.TileYOffsetDefault;
        public static float CurrentMaxHeight => AmplifiedTerrain ? (TerrainOperations.TileHeightRescaled + TerrainOperations.TileYOffsetRescaled) : 
            (TerrainOperations.TileHeightDefault + TerrainOperations.TileYOffsetDefault);

        private static bool preInit = false;
        private static bool mainInit = false;
        public static void InsurePreInit()
        {
            if (preInit)
                return;
            preInit = true;
            Debug_TTExt.Log("PreInit WorldTerraformer");
            TerrainModifier.TileHeight = TerrainOperations.TileHeightRescaled;
            ManGameMode.inst.ModeStartEvent.Subscribe(OnModeStart);
            MassPatcher.MassPatchAllWithin(LegModExt.harmonyInstance, typeof(WorldVerticalExtender), "TerraTechModExt", true);
        }
        public static void InsureInit()
        {
            if (mainInit)
                return;
            mainInit = true;
            InsurePreInit();
            LegModExt.InsurePatches();
            Debug_TTExt.Log("Init WorldTerraformer");
            InsureLowerTerrainClamps();
        }
        public static void DeInit()
        {
            if (preInit)
            {
                LegModExt.InsurePatches();
                ManGameMode.inst.ModeStartEvent.Unsubscribe(OnModeStart);
                MassPatcher.MassUnPatchAllWithin(LegModExt.harmonyInstance, typeof(WorldVerticalExtender), "TerraTechModExt", true);
                preInit = false;
            }
            if (mainInit)
            {
                mainInit = false;
                Debug_TTExt.Log("De-Init WorldTerraformer");
            }
        }
        protected static EventNoParams OnClampTerrain = new EventNoParams();

        protected static FieldInfo layers = typeof(MapGenerator).GetField("m_Layers", BindingFlags.NonPublic | BindingFlags.Instance);
        protected static FieldInfo generatorsDetails = typeof(Biome).GetField("layers", BindingFlags.NonPublic | BindingFlags.Instance);
        public static Dictionary<MapGenerator, float> LowerTerrainHeightClamped = new Dictionary<MapGenerator, float>();
        private static void InsureLowerTerrainClamps()
        {
            if (LowerTerrainHeightClamped.Count == 0)
            {
                foreach (var item in ManWorld.inst.CurrentBiomeMap.IterateBiomes())
                {

                    if (item.DetailLayers != null)
                    {
                        foreach (var item2 in item.DetailLayers)
                        {
                            var HMG2 = item2.generator;
                            if (HMG2 != null)
                            {
                                //BlockBiomeSceneryInWater(HMG2);
                                //LowerTerrainHeightClamped.Add(HMG2, TerrainOperations.TileYOffsetScalarSeaLevel);
                            }
                        }
                    }

                    var HMG = item.HeightMapGenerator;
                    if (HMG != null)
                    {
                        Debug_TTExt.Log("Limited Vanilla Biome Terrain " + item.name +
                            " to " + TerrainOperations.TileYOffsetDelta +
                            " so that deep oceans below sea level can exist");
                        ClampBiome(HMG);
                        LowerTerrainHeightClamped.Add(HMG, TerrainOperations.TileYOffsetScalarSeaLevel);
                        /*
                        MapGenerator.Layer[] Layers = (MapGenerator.Layer[])layers.GetValue(HMG);
                        if (layers != null)
                        {
                            foreach (var item2 in Layers)
                            {
                                if (item2.applyOperation.code != MapGenerator.Operation.Code.Null)
                                {
                                    Array.Resize(ref item2.operations, item2.operations.Length + 1);
                                    item2.applyOperation.index = -3;// Setup clamp to be triggered on index -3
                                    Debug_TTExt.Log("Limited Vanilla Biome Terrain " + item.name +
                                        " to " + TerrainOperations.TileYOffsetDelta +
                                        " so that deep oceans below sea level can exist");
                                }
                            }
                        }
                        */
                    }
                }
                OnClampTerrain.Send();
            }
        }

        protected static Type biomesDataType = typeof(BiomeMap).GetNestedType("BiomeGroupDatabase", BindingFlags.NonPublic);
        protected static FieldInfo biomesData = typeof(BiomeMap).GetField("m_BiomeGroupDatabase", BindingFlags.NonPublic | BindingFlags.Instance);

        protected static FieldInfo biomesAll = biomesDataType.GetField("m_Biomes", BindingFlags.NonPublic | BindingFlags.Instance);
        protected static FieldInfo biomesBatched = biomesDataType.GetField("m_BiomeGroups", BindingFlags.Instance | BindingFlags.NonPublic);

        //private static FieldInfo biomesAll2 = typeof(BiomeMap).GetField("biomes", BindingFlags.NonPublic | BindingFlags.Instance);
        protected static FieldInfo biomesBatched2 = typeof(BiomeMap).GetField("m_BiomeGroups", BindingFlags.Instance | BindingFlags.NonPublic);

        protected static FieldInfo biomesInside = typeof(BiomeGroup).GetField("m_Biomes", BindingFlags.Instance | BindingFlags.NonPublic),
            biomesWeights = typeof(BiomeGroup).GetField("m_BiomeWeights", BindingFlags.Instance | BindingFlags.NonPublic);

        protected static FieldInfo[] MassCopy = null;
        protected static FieldInfo[] MassCopy2 = null;
        protected static FieldInfo[] MassCopy3 = null;
        protected static FieldInfo generatorHeights = typeof(Biome).GetField("heightMapGenerator",
            BindingFlags.Instance | BindingFlags.NonPublic);
        protected static FieldInfo funcIDK = typeof(MapGenerator.Layer).GetField("function",
            BindingFlags.Instance | BindingFlags.NonPublic);

        public static void BlockBiomeSceneryInWater(MapGenerator generator, bool invert = false)
        {
            if (invert && generator.m_UseLegacy)
            {
                generator.EditorInitFromLegacyParams();
                generator.m_UseLegacy = false;
            }
            MapGenerator.Layer[] ogLayers = (MapGenerator.Layer[])layers.GetValue(generator);
            if (ogLayers != null)
            {
                var ogLayerEnd = ogLayers[ogLayers.Length - 1];
                float totalWeight = 1;
                foreach (var item in ogLayers)
                {
                    totalWeight += item.weight;
                }

                var layerEnd = CopyLayer(ogLayerEnd, -1);
                //newLayer.operations = new MapGenerator.Operation[ogLayer.operations.Length + 1];
                if (invert)
                {
                    layerEnd.operations = new MapGenerator.Operation[]
                        {
                    // CLAMP
                    MapGenerator.Operation.NewBuffered(MapGenerator.Operation.Code.Store, 0),
                    MapGenerator.Operation.New(MapGenerator.Operation.Code.Add,
                        -TerrainOperations.TileYOffsetScalarSeaLevelScenerySea * TerrainOperations.tileScaleToMapGen),
                    MapGenerator.Operation.New(MapGenerator.Operation.Code.Sign, 0),
                    MapGenerator.Operation.New(MapGenerator.Operation.Code.Mul, -1),
                    MapGenerator.Operation.New(MapGenerator.Operation.Code.Max, 0),
                    MapGenerator.Operation.NewBuffered(MapGenerator.Operation.Code.Mul, 0),
                        };
                }
                else
                {
                    layerEnd.operations = new MapGenerator.Operation[]
                        {
                    // CLAMP
                    MapGenerator.Operation.NewBuffered(MapGenerator.Operation.Code.Store, 0),
                    MapGenerator.Operation.New(MapGenerator.Operation.Code.Add,
                        -TerrainOperations.TileYOffsetScalarSeaLevelSceneryLand * TerrainOperations.tileScaleToMapGen),
                    MapGenerator.Operation.New(MapGenerator.Operation.Code.Sign, 0),
                    MapGenerator.Operation.New(MapGenerator.Operation.Code.Max, 0),
                    MapGenerator.Operation.NewBuffered(MapGenerator.Operation.Code.Mul, 0),
                        };
                }
                layerEnd.applyOperation = MapGenerator.Operation.New(MapGenerator.Operation.Code.Modify, 0);


                Array.Resize(ref ogLayers, ogLayers.Length + 1);
                ogLayers[ogLayers.Length - 1] = layerEnd;
                layers.SetValue(generator, ogLayers);
            }
        }
        protected static void ClampBiome(MapGenerator generator)
        {
            return;
            MapGenerator.Layer[] ogLayers = (MapGenerator.Layer[])layers.GetValue(generator);
            if (ogLayers != null)
            {
                var ogLayerEnd = ogLayers[ogLayers.Length - 1];
                var layerEnd = new MapGenerator.Layer()
                {
                    generator = ogLayerEnd.generator,
                    weight = ogLayerEnd.weight,
                    offset = ogLayerEnd.offset,
                    amplitude = ogLayerEnd.amplitude,
                    applyOperation = ogLayerEnd.applyOperation,
                    invert = ogLayerEnd.invert,
                    scale = ogLayerEnd.scale,
                    scaleX = ogLayerEnd.scaleX,
                    scaleY = ogLayerEnd.scaleY,
                    rotation = ogLayerEnd.rotation,
                    bias = ogLayerEnd.bias,
                };
                funcIDK.SetValue(layerEnd, funcIDK.GetValue(ogLayerEnd));
                //newLayer.operations = new MapGenerator.Operation[ogLayer.operations.Length + 1];
                layerEnd.operations = new MapGenerator.Operation[1]
                    {
                    MapGenerator.Operation.New(MapGenerator.Operation.Code.Modify,
                        TerrainOperations.TileYOffsetScalarSeaLevel),
                    };
                layerEnd.applyOperation = MapGenerator.Operation.New(MapGenerator.Operation.Code.Max, 0);

                Array.Resize(ref ogLayers, ogLayers.Length + 1);
                ogLayers[ogLayers.Length - 1] = layerEnd;
                layers.SetValue(generator, ogLayers);
            }
        }

        protected static MapGenerator.Layer CopyLayer(MapGenerator.Layer ogLayer, int addOps = 0)
        {
            MapGenerator.Layer newLayer = new MapGenerator.Layer
            {
                generator = ogLayer.generator,
                weight = ogLayer.weight,
                offset = ogLayer.offset,
                amplitude = ogLayer.amplitude,
                applyOperation = ogLayer.applyOperation,
                invert = ogLayer.invert,
                scale = ogLayer.scale,
                scaleX = ogLayer.scaleX,
                scaleY = ogLayer.scaleY,
                rotation = ogLayer.rotation,
                bias = ogLayer.bias,
            };
            funcIDK.SetValue(newLayer, funcIDK.GetValue(ogLayer));
            if (addOps >= 0)
            {
                newLayer.operations = new MapGenerator.Operation[ogLayer.operations.Length + addOps];
                Array.Copy(ogLayer.operations, newLayer.operations, ogLayer.operations.Length);
            }

            /*
            newLayer.operations[newLayer.operations.Length - 1] = new MapGenerator.Operation()
            {
                buffered = false,
                code = MapGenerator.Operation.Code.Add,
                index = ogLayer.operations.Length - 1,
                param = SeaHeightDeltaMeters / TerrainOperations.TileHeightRescaled
            };
            Debug_TTExt.LogGen("- Layer " + newLayer.applyOperation.code);
            foreach (var item in newLayer.operations)
            {
                Debug_TTExt.LogGen("- - Op " + item.code + " - " + item.param);
            }
            */
            return newLayer;
        }
        protected static MapGenerator.Layer[] CopyLayers(MapGenerator.Layer[] ogLayers, int addExtra = 0)
        {
            MapGenerator.Layer[] NewLayers = new MapGenerator.Layer[ogLayers.Length + addExtra];
            for (int i = 0; i < ogLayers.Length; i++)
            {
                var item = ogLayers[i];
                Debug_TTExt.LogGen("Layer - " + item.applyOperation.code);
                foreach (var item2 in item.operations)
                {
                    Debug_TTExt.LogGen("- " + item2.code + (item2.buffered ?
                        (", bufferIndex " + item2.index) : (", val " + item2.param)));
                }
                NewLayers[i] = CopyLayer(ogLayers[i]);
            }
            return NewLayers;
        }

        protected static Biome CopyBiome(Biome from, string name)
        {
            if (MassCopy == null)
                MassCopy = typeof(Biome).GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Biome copyBiome = ScriptableObject.CreateInstance<Biome>();
            copyBiome.name = name;
            foreach (var item in MassCopy)
            {
                try
                {
                    item.SetValue(copyBiome, item.GetValue(from));
                    //Debug_TTExt.Log("Set " + item.Name);
                }
                catch (Exception e)
                {
                    Debug_TTExt.Log("Error on " + item.Name + " - " + e);
                }
            }
            Biome.DetailLayer[] dLayers = new Biome.DetailLayer[copyBiome.DetailLayers.Length];
            for (int step = 0; step < copyBiome.DetailLayers.Length; step++)
            {
                Biome.DetailLayer og = copyBiome.DetailLayers[step];
                MapGenerator detailGen = DeepCopyGenerator(og.generator, name + "_DETAIL(" + step + ")");
                dLayers[step] = new Biome.DetailLayer
                {
                    distributor = og.distributor,
                    generator = detailGen,
                };
            }
            generatorsDetails.SetValue(copyBiome, dLayers);
            generatorHeights.SetValue(copyBiome, DeepCopyGenerator(copyBiome.HeightMapGenerator, name));
            Debug_TTExt.LogGen("Made biome " + name);
            return copyBiome;
        }

        protected static BiomeGroup CopyBiomeGroup(BiomeGroup from, string name, Biome[] biomes, float[] weights)
        {
            if (MassCopy3 == null)
                MassCopy3 = typeof(BiomeGroup).GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            BiomeGroup copyGroup = ScriptableObject.CreateInstance<BiomeGroup>();
            copyGroup.name = name;
            foreach (var item in MassCopy3)
            {
                try
                {
                    item.SetValue(copyGroup, item.GetValue(from));
                    //Debug_TTExt.Log("Set " + item.Name);
                }
                catch (Exception e)
                {
                    Debug_TTExt.Log("Error on " + item.Name + " - " + e);
                }
            }
            biomesInside.SetValue(copyGroup, biomes);
            biomesWeights.SetValue(copyGroup, weights);

            Debug_TTExt.LogGen("Made biomeGroup " + name);
            return copyGroup;
        }
        protected static MapGenerator ShallowCopyGenerator(MapGenerator ogGen, string name)
        {
            if (MassCopy2 == null)
                MassCopy2 = typeof(MapGenerator).GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
            Debug_TTExt.LogGen("Making mapGen " + name);
            MapGenerator MG = new GameObject(name).AddComponent<MapGenerator>();
            foreach (var item in MassCopy2)
            {
                try
                {
                    item.SetValue(MG, item.GetValue(ogGen));
                    //Debug_TTExt.Log("Set " + item.Name);
                }
                catch (Exception e)
                {
                    Debug_TTExt.Log("Error on " + item.Name + " - " + e);
                }
            }
            return MG;
        }
        protected static MapGenerator DeepCopyGenerator(MapGenerator ogGen, string name, int addLayers = 0)
        {
            MapGenerator MG = ShallowCopyGenerator(ogGen, name);
            Debug_TTExt.LogGen("Generator - " + name + ", isLegacy " + MG.m_UseLegacy.ToString());
            layers.SetValue(MG, CopyLayers((MapGenerator.Layer[])layers.GetValue(MG), addLayers));
            return MG;
        }


        protected static float GenDelegateNone(float x, float y)
        {
            return 1;
        }
        protected static FieldInfo allowTS = typeof(Biome).GetField("m_AllowVendors",
            BindingFlags.Instance | BindingFlags.NonPublic);
        protected static FieldInfo allowMarks = typeof(Biome).GetField("m_AllowLandmarks",
            BindingFlags.Instance | BindingFlags.NonPublic);
        protected static FieldInfo allowStunts = typeof(Biome).GetField("m_AllowStuntRamps",
            BindingFlags.Instance | BindingFlags.NonPublic);



        /// <summary>
        /// This is LEGACY
        /// </summary>
        protected static FieldInfo biomesAllOld = typeof(BiomeMap).GetField("biomes", BindingFlags.NonPublic | BindingFlags.Instance);


        private static void OnModeStart(Mode mode)
        {
            if (!ManWorldDeformerExt.inst)
                return;
            switch (mode.GetGameType())
            {
                case ManGameMode.GameType.Attract:
                case ManGameMode.GameType.MainGame:
#if DEBUG
                    WorldDeformer.inst.enabled = true;
#else
                    ManWorldDeformerExt.inst.enabled = false;
#endif
                    break;
                case ManGameMode.GameType.RaD:
                case ManGameMode.GameType.Creative:
                    ManWorldDeformerExt.inst.enabled = true;
                    break;
                default:
                    ManWorldDeformerExt.inst.enabled = false;
                    break;
            }
        }


    }
}
