using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using Newtonsoft.Json;
using System.IO;

#if !STEAM
using Nuterra.BlockInjector;
#endif

namespace TerraTechETCUtil
{
    /// <summary>
    /// A temporary way of doing reverse block lookups for the time being
    /// </summary>
    public class BlockIndexer : MonoBehaviour
    {
        public static bool isBlockInjectorPresent = false;
        public static string FolderDivider = "/";
        public static bool UseVanillaFallbackSnapUtility = true;

        private static bool spamLog = false;
        private static BlockIndexer inst;
        private static bool Compiled = false;
        private static int types = Enum.GetValues(typeof(BlockTypes)).Length;


        /// <summary>
        /// Searches Block Injector for the block based on root GameObject name.
        /// </summary>
        /// <param name="mem">The name of the block's root GameObject.  This is also set in the Official Mod Tool by the Name ID (filename of the .json), not the name you give it.</param>
        /// <returns>The Block Type to use if it found it, otherwise returns BlockTypes.GSOCockpit_111</returns>
        public static BlockTypes StringToBlockType(string mem)
        {
            if (!Enum.TryParse(mem, out BlockTypes type) && (int)type < types)
                if (!TryGetMismatchNames(mem, ref type))
                    if (!StringToBIBlockType(mem, out type))
                        type = GetBlockIDLogFree(mem);
            return type;
        }
        /// <summary>
        /// Searches the ENTIRE GAME for the block based on root GameObject name.
        /// </summary>
        /// <param name="mem">The name of the block's root GameObject.  This is also set in the Official Mod Tool by the Name ID (filename of the .json), not the name you give it.</param>
        /// <param name="BT">The Block Type to use if it found it</param>
        /// <returns>True if it found it in Block Injector</returns>
        public static bool StringToBlockType(string mem, out BlockTypes BT) =>
            (Enum.TryParse(mem, out BT) && (int)BT < types) || TryGetMismatchNames(mem, ref BT) ||
            StringToBIBlockType(mem, out BT) || GetBlockIDLogFree(mem, out BT);
        /// <summary>
        /// Searches Block Injector for the block based on root GameObject name.
        /// </summary>
        /// <param name="mem">The name of the block's root GameObject.  This is also set in the Official Mod Tool by the Name ID (filename of the .json), not the name you give it.</param>
        /// <param name="BT">The Block Type to use if it found it</param>
        /// <returns>True if it found it in Block Injector</returns>
        public static bool StringToBIBlockType(string mem, out BlockTypes BT) // BLOCK INJECTOR
        {
            BT = BlockTypes.GSOAIController_111;

#if !STEAM
            if (!isBlockInjectorPresent)
                return false;
#endif
            if (TryGetIDSwap(mem.GetHashCode(), out BlockTypes BTC))
            {
                BT = BTC;
                return true;
            }
            Debug_TTExt.Info("StringToBIBlockType Failed attempting " + mem + " -> " + mem.GetHashCode());
            return false;
        }

        public static BlockTypes GetBlockIDLogFree(string name)
        {
            PrepareModdedBlocksFetch();
            if (ModdedBlocksGrabbed != null && ModdedBlocksGrabbed.TryGetValue(name, out int blockType))
                return (BlockTypes)blockType;
            else if (name == "GSO_Exploder_A1_111")
                return (BlockTypes)622;
            return BlockTypes.GSOCockpit_111;
        }

        public static bool GetBlockIDLogFree(string name, out BlockTypes BT)
        {
            PrepareModdedBlocksFetch();
            if (ModdedBlocksGrabbed != null && ModdedBlocksGrabbed.TryGetValue(name, out int blockType))
            {
                BT = (BlockTypes)blockType;
                return true;
            }
            else if (name == "GSO_Exploder_A1_111")
            {
                BT = (BlockTypes)622;
                return true;
            }
            BT = BlockTypes.GSOCockpit_111;
            return false;
        }

        // Block Details Cache 
        private static readonly Dictionary<int, BlockDetails.Flags> vanillaDetails = new Dictionary<int, BlockDetails.Flags>();
        private static readonly Dictionary<int, BlockDetails.Flags> moddedDetails = new Dictionary<int, BlockDetails.Flags>();
        public static BlockDetails GetBlockDetails(BlockTypes type) => new BlockDetails(type);
        internal static void GetBlockDetails_Internal(BlockTypes type, ref BlockDetails cache)
        {
            var intT = (int)type;
            cache.attributesHash = (BlockAttributes)ManSpawn.inst.VisibleTypeInfo.GetDescriptorFlags<BlockAttributes>(
                new ItemTypeInfo(ObjectTypes.Block, intT).GetHashCode());
            if (!vanillaDetails.TryGetValue(intT, out cache.flags))
                moddedDetails.TryGetValue(intT, out cache.flags);
        }


        // Logless block loader
        private static FieldInfo generator = typeof(ModuleEnergy).GetField("m_OutputConditions", BindingFlags.NonPublic | BindingFlags.Instance);

        private static Dictionary<string, int> ModdedBlocksGrabbed;
        private static readonly FieldInfo allModdedBlocks = typeof(ManMods).GetField("m_BlockIDReverseLookup", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly Dictionary<int, BlockTypes> errorNamesVanilla = new Dictionary<int, BlockTypes>();
        private static readonly Dictionary<int, BlockTypes> errorNames = new Dictionary<int, BlockTypes>();
        private static bool subbed = false;
        public static void ResetBlockLookupList()
        {
            if (subbed)
            {
                ManGameMode.inst.ModeStartEvent.Unsubscribe(ConstructBlockLookupListTrigger);
                subbed = false;
            }
            if (!Compiled)
                return;
            errorNames.Clear();
            moddedDetails.Clear();
            Compiled = false;
            LegModExt.harmonyInstance.MassUnPatchAllWithin(typeof(SnapshotsPatchBatch), "TerraTechModExt", false);
        }
        /// <summary>
        /// Builds the lookup to use when using block names to find BlockTypes
        /// </summary>
        public static void ConstructBlockLookupListDelayed()
        {
            if (Compiled)
                return;
            if (inst == null)
            {
                inst = new GameObject("BlockIndexerUtil").AddComponent<BlockIndexer>();
            }
            if (!subbed)
            {
                ManGameMode.inst.ModeStartEvent.Subscribe(ConstructBlockLookupListTrigger);
                subbed = true;
            }
        }
        public static void ConstructBlockLookupListTrigger(Mode mode)
        {
            ConstructBlockLookupList();
        }

        private static void CollectBlockDetails(BlockTypes type,
            Dictionary<int, BlockTypes> errorNames, Dictionary<int, BlockDetails.Flags> details, bool isModded, bool moddedLog)
        {
            TankBlock prefab = Singleton.Manager<ManSpawn>.inst.GetBlockPrefab(type);
            if (prefab?.GetComponent<Damageable>() == null)
                return;
            string name = prefab.name;
            int hash = name.GetHashCode();
            if (!errorNames.Keys.Contains(hash))
            {
                errorNames.Add(hash, type);

                int blockDetailFlags = 0;

                var booster = prefab.GetComponent<ModuleBooster>();
                if (booster)
                {
                    //Get the slowest spooling one
                    if (booster.transform.GetComponentInChildren<FanJet>(true))
                        blockDetailFlags |= (int)BlockDetails.Flags.Fans;
                    if (booster.transform.GetComponentInChildren<BoosterJet>(true))
                        blockDetailFlags |= (int)BlockDetails.Flags.Boosters;
                }

                if (prefab.GetComponent<ModuleBalloon>())
                    blockDetailFlags |= (int)BlockDetails.Flags.AirFloats;
                if (prefab.GetComponent<ModuleWing>())
                    blockDetailFlags |= (int)BlockDetails.Flags.Wings;
                if (prefab.GetComponent<ModuleHover>())
                    blockDetailFlags |= (int)BlockDetails.Flags.Hovers;
                if (prefab.GetComponent<ModuleGyro>())
                    blockDetailFlags |= (int)BlockDetails.Flags.Gyro;
                if (prefab.GetComponent<ModuleWheels>())
                    blockDetailFlags |= (int)BlockDetails.Flags.Wheels;
                if (prefab.GetComponent<ModuleAntiGravityEngine>())
                    blockDetailFlags |= (int)BlockDetails.Flags.AntiGrav;
                if (prefab.GetComponent<ModuleShieldGenerator>())
                    blockDetailFlags |= (int)BlockDetails.Flags.Bubble;

                if (prefab.GetComponent<IModuleDamager>() != null)
                    blockDetailFlags |= (int)BlockDetails.Flags.Weapon;
                if (prefab.GetComponent<ModuleMeleeWeapon>())
                    blockDetailFlags |= (int)BlockDetails.Flags.Melee;
                if (prefab.GetComponent<ModuleWeaponFlamethrower>() ||
                    prefab.GetComponent<ModuleWeaponTeslaCoil>())
                    blockDetailFlags |= (int)BlockDetails.Flags.Short;
                if (prefab.GetComponent<ModuleDetachableLink>() ||
                    prefab.GetComponent<ModuleFasteningLink>())
                    blockDetailFlags |= (int)BlockDetails.Flags.ControlsBlockman;
                if (prefab.m_DefaultMass < prefab.filledCells.Length)
                    blockDetailFlags |= (int)BlockDetails.Flags.WaterFloats;

                foreach (var item in prefab.GetComponents<ExtModule>())
                    blockDetailFlags |= (int)item.BlockDetailFlags;

                int typeI = (int)type;
                if (!details.ContainsKey(typeI))
                    details.Add(typeI, (BlockDetails.Flags)blockDetailFlags);
                else
                    details[typeI] = (BlockDetails.Flags)blockDetailFlags;

                if (moddedLog)
                    Debug.Log("TTETCUtil: ConstructErrorBlocksList - Added Modded Block " + name + " | " + type.ToString());
            }
        }
        private static void ConstructBlockLookupListVanilla()
        {
            if (!errorNamesVanilla.Any())
            {
                int enumMax = Enum.GetValues(typeof(BlockTypes)).Length;
                foreach (BlockTypes type in Singleton.Manager<ManSpawn>.inst.GetLoadedTankBlockNames())
                {
                    if ((int)type > enumMax)
                        return;
                    CollectBlockDetails(type, errorNamesVanilla, vanillaDetails, false, false);
                }
            }
        }
        /// <summary>
        /// Builds the lookup to use when using block names to find BlockTypes
        /// </summary>
        public static void ConstructBlockLookupList()
        {
            if (Compiled)
                return;
            Debug.Log("TerraTechETCUtil: Rebuilding block lookup...");
            try
            {
                LegModExt.harmonyInstance.MassPatchAllWithin(typeof(SnapshotsPatchBatch), "TerraTechModExt", true);
                ConstructBlockLookupListVanilla();
                int enumMax = Enum.GetValues(typeof(BlockTypes)).Length;
                foreach (BlockTypes type in Singleton.Manager<ManSpawn>.inst.GetLoadedTankBlockNames())
                {
                    if ((int)type <= enumMax)
                        continue;
                    CollectBlockDetails(type, errorNames, moddedDetails, true, spamLog);
                }
#if !STEAM
                if (isBlockInjectorPresent)
#endif
                ConstructModdedIDList();
            }
            catch (Exception e)
            {
                Debug.Log("BlockUtils: ConstructErrorBlocksList - CRITICAL ERROR - " + e);
            }

            Debug.Log("BlockUtils: ConstructErrorBlocksList - There are " + errorNames.Count + " blocks with names not equal to their type");
            Compiled = true;
        }

        /// <summary>
        /// Delay this until AFTER Block Injector to setup the lookups
        /// </summary>
        /// <summary>
        /// Call at least once to hook up to modding
        /// </summary>
        public static void PrepareModdedBlocksFetch()
        {
            if (ModdedBlocksGrabbed == null)
                ModdedBlocksGrabbed = (Dictionary<string, int>)allModdedBlocks.GetValue(Singleton.Manager<ManMods>.inst);
        }

        public static void ConstructModdedIDList()
        {
#if STEAM
            ModSessionInfo session = (ModSessionInfo)access.GetValue(ManMods.inst);
            UnOf_Offi.Clear();
            try
            {
                Dictionary<int, string> blocc = session.BlockIDs;
                foreach (KeyValuePair<int, string> pair in blocc)
                {
                    ModdedBlockDefinition MBD = ManMods.inst.FindModdedAsset<ModdedBlockDefinition>(pair.Value);

                    string SCAN = MBD.m_Json.text;

                    if (SCAN.Contains("NuterraBlock"))
                    {
                        int num = 0;
                        string name = "";
                        if (FindInt(SCAN, "\"ID\":", ref num)) //&& FindText(SCAN, "\"Name\" :", ref name))
                        {
                            BlockTypes BT = (BlockTypes)ManMods.inst.GetBlockID(MBD.name);
                            int hasher = num.ToString().GetHashCode();
                            if (!UnOf_Offi.ContainsKey(hasher))
                                UnOf_Offi.Add(hasher, BT);
                            hasher = ("_C_BLOCK:" + num.ToString()).GetHashCode();
                            if (!UnOf_Offi.ContainsKey(hasher))
                                UnOf_Offi.Add(hasher, BT);
                            Debug_TTExt.Info("BlockUtils: ConstructModdedIDList - " + num.ToString() +
                                " | _C_BLOCK:" + num.ToString() + " | " + MBD.name + " | " + BT +
                                " | -> " + num.ToString().GetHashCode());
                        }
                    }
                }
            }
            catch { Debug.Log("BlockUtils: ConstructModdedIDList - Error on compile"); };
            Debug.Log("BlockUtils: ConstructModdedIDList - compiled " + UnOf_Offi.Count());
#else
            try
            {
                foreach (KeyValuePair<int, CustomBlock> pair in BlockLoader.CustomBlocks)
                {
                    CustomBlock CB = pair.Value;
                    if (CB != null)
                    {
                        var MCB = CB.Prefab.GetComponent<ModuleCustomBlock>();
                        if (GetNameJSON(MCB.FilePath, out string outp, true))
                            Offi_UnOf.Add(outp.GetHashCode(), (BlockTypes)pair.Key);
                    }
                }
            }
            catch { Debug.Log("TTETCUtil: ConstructModdedIDList - Error on compile"); };
            Debug.Log("TTETCUtil: ConstructModdedIDList - compiled " + Offi_UnOf.Count());
#endif
        }


        private static bool TryGetMismatchNames(string name, ref BlockTypes type)
        {
            if (errorNames.TryGetValue(name.GetHashCode(), out BlockTypes val) || errorNamesVanilla.TryGetValue(name.GetHashCode(), out val))
            {
                type = val;
                return true;
            }
            return false;
        }
        private static bool GetNameJSON(string FolderDirectory, out string output, bool excludeJSON)
        {
            StringBuilder final = new StringBuilder();
            foreach (char ch in FolderDirectory)
            {
                if (ch == FolderDivider.ToCharArray()[0])
                {
                    final.Clear();
                }
                else
                    final.Append(ch);
            }
            if (!final.ToString().Contains(".RAWTECH"))
            {
                if (!final.ToString().Contains(".JSON") && !excludeJSON)
                {
                    output = null;
                    return false;
                }
                else
                    final.Remove(final.Length - 5, 5);// remove ".JSON"
            }
            else
                final.Remove(final.Length - 8, 8);// remove ".RAWTECH"

            output = final.ToString();
            return true;
        }

        private static bool TryRepairSnapshot(string path)
        {
            try
            {
                Texture2D tex = FileUtils.LoadTexture(path);
                if (tex == null)
                    return false;
                if (ManScreenshot.TryDecodeSnapshotRender(tex, out var data, path, false))
                {
                    TechData TD = data.CreateTechData();
                    bool delta = false;
                    for (int i = 0; TD.m_BlockSpecs.Count > i; i++)
                    {
                        TankPreset.BlockSpec BS = TD.m_BlockSpecs[i];
                        if (ManMods.inst.IsModdedBlock(BS.m_BlockType, true))
                        {
                            if (StringToBlockType(BS.block, out BlockTypes BT))
                            {
                                TankBlock prefab = ManSpawn.inst.GetBlockPrefab(BT);
                                if (prefab.name != BS.block)
                                {
                                    delta = true;
                                    BS.block = prefab.name;
                                    TD.m_BlockSpecs[i] = BS;
                                    Debug_TTExt.Log("BlockIndexer: Fixed reference for " + TD.Name + " for block " +
                                        BS.block + " -> " + prefab.name);
                                }
                            }
                            else
                                Debug_TTExt.Log("BlockIndexer: Unable to fix reference for " + TD.Name + " for block " + 
                                    (BS.block.NullOrEmpty() ? "<NULL>" : BS.block));
                        }
                    }
                    if (delta)
                    {
                        ManScreenshot.EncodeSnapshotRender(TD, tex);
                        FileUtils.SaveTexture(tex, path);
                        Debug_TTExt.Log("BlockIndexer: Fixed " + TD.Name);
                    }
                    return true;
                }
            }
            catch (Exception e)
            {
                Debug_TTExt.Log("BlockIndexer: Failed to repair Snapshot " + Path.GetFileNameWithoutExtension(path) +
                    " with modded data - " + e);
            }
            return false;
        }


#if STEAM
        private static Dictionary<int, BlockTypes> UnOf_Offi = new Dictionary<int, BlockTypes>();
#else
        private static readonly Dictionary<int, BlockTypes> Offi_UnOf = new Dictionary<int, BlockTypes>();
#endif
        private static FieldInfo access = typeof(ManMods).GetField("m_CurrentSession", BindingFlags.NonPublic | BindingFlags.Instance);

        private static bool TryGetIDSwap(int hash, out BlockTypes blockType)
        {
#if STEAM
            return UnOf_Offi.TryGetValue(hash, out blockType);
#else
            return Offi_UnOf.TryGetValue(hash, out blockType);
#endif
        }

        private static bool FindInt(string text, string searchBase, ref int intCase)
        {
            int indexFind = text.IndexOf(searchBase);
            if (indexFind >= 0)
            {
                int searchEnd = 0;
                int searchLength = 0;
                string output = "";
                try
                {
                    searchEnd = indexFind + searchBase.Length;
                    searchLength = text.Substring(searchEnd).IndexOf(",");
                    if (searchLength != -1)
                    {
                        output = text.Substring(searchEnd, searchLength).Replace(" ", "");
                        intCase = (int)float.Parse(output);
                        return true;
                    }
                    //Debug_TTExt.Log(searchEnd + " | " + searchLength + " | " + output + " | ");
                }
                catch (Exception e) { Debug_TTExt.LogError(searchEnd + " | " + searchLength + " | " + output + " | " + e); }
            }
            return false;
        }
        private static bool FindText(string text, string searchBase, ref string name)
        {
            int indexFind = text.IndexOf(searchBase);
            if (indexFind >= 0)
            {
                int searchEnd = 0;
                int searchLength = 0;
                string output = "";
                try
                {
                    searchEnd = indexFind + searchBase.Length;
                    searchLength = text.Substring(searchEnd).IndexOf(",");
                    output = text.Substring(searchEnd, searchLength);
                    name = output;
                    return true;
                }
                catch (Exception e) { Debug.LogError(searchEnd + " | " + searchLength + " | " + output + " | " + e); }
            }
            return false;
        }


        // RawTech Support

        private static StringBuilder blockCase = new StringBuilder();
        private static List<BlockMemory> JSONToMemory(string toLoad)
        {   // Loading a Tech from the BlockMemory
            List<BlockMemory> mem = new List<BlockMemory>();
            try
            {
                foreach (char ch in toLoad)
                {
                    if (ch != '\\')
                    {
                        if (ch == '|')//new block
                        {
                            mem.Add(JsonUtility.FromJson<BlockMemory>(blockCase.ToString()));
                            blockCase.Clear();
                        }
                        else
                            blockCase.Append(ch);
                    }
                }
                mem.Add(JsonUtility.FromJson<BlockMemory>(blockCase.ToString()));
                //Debug.Log("TTETCUtil:  DesignMemory: saved " + mem.Count);
                return mem;
            }
            finally
            {
                blockCase.Clear();
            }
        }
        private static FactionTypesExt GetCorpExtended(BlockTypes type)
        {
            return (FactionTypesExt)Singleton.Manager<ManSpawn>.inst.GetCorporation(type);
        }
        private static FactionSubTypes CorpExtToCorp(FactionTypesExt corpExt)
        {
            switch (corpExt)
            {
                case FactionTypesExt.GT:
                case FactionTypesExt.IEC:
                    return FactionSubTypes.GSO;
                case FactionTypesExt.EFF:
                case FactionTypesExt.LK:
                    return FactionSubTypes.GC;
                case FactionTypesExt.OS:
                    return FactionSubTypes.VEN;
                case FactionTypesExt.BL:
                case FactionTypesExt.TAC:
                    return FactionSubTypes.HE;
                case FactionTypesExt.DL:
                case FactionTypesExt.EYM:
                case FactionTypesExt.HS:
                    return FactionSubTypes.BF;
            }
            return (FactionSubTypes)corpExt;
        }
        public static TechData RawTechToTechData(string name, string blueprint, out int[] blockIDs)
        {
            if (!Compiled)
            {
                ConstructBlockLookupList();
            }
            TechData data = new TechData();
            data.Name = name;
            data.m_Bounds = new IntVector3(new Vector3(18, 18, 18));
            data.m_SkinMapping = new Dictionary<uint, string>();
            data.m_TechSaveState = new Dictionary<int, TechComponent.SerialData>();
            data.m_CreationData = new TechData.CreationData();
            data.m_BlockSpecs = new List<TankPreset.BlockSpec>();
            List<BlockMemory> mems = JSONToMemory(blueprint);
            if (mems == null)
                throw new NullReferenceException("RawTechToTechData was given an INVALID blueprint!");
            List<int> BTs = new List<int>();

            bool skinChaotic = UnityEngine.Random.Range(0, 100) < 2;
            byte skinset = (byte)UnityEngine.Random.Range(0, 2);
            byte skinset2 = (byte)UnityEngine.Random.Range(0, 1);
            foreach (BlockMemory mem in mems)
            {
                if (StringToBlockType(mem.t, out BlockTypes type))
                {
                    if (!Singleton.Manager<ManSpawn>.inst.IsBlockAllowedInCurrentGameMode(type) || 
                        Singleton.Manager<ManSpawn>.inst.IsBlockUsageRestrictedInGameMode(type))
                    {
                        Debug.Log("TTETCUtil: InstantTech - Removed " + mem.t + " as it was invalidated");
                        continue;
                    }
                    if (!BTs.Contains((int)type))
                    {
                        BTs.Add((int)type);
                    }

                    TankPreset.BlockSpec spec = default;
                    spec.block = mem.t;
                    spec.m_BlockType = type;
                    spec.orthoRotation = new OrthoRotation(mem.r);
                    spec.position = mem.p;
                    spec.saveState = new Dictionary<int, Module.SerialData>();
                    spec.textSerialData = new List<string>();
                    FactionTypesExt factType = GetCorpExtended(type);
                    if (skinChaotic)
                    {
                        byte rand = (byte)UnityEngine.Random.Range(0, 2);
                        if (factType == FactionTypesExt.GSO && rand != 0)
                            rand += 3;
                        if (!ManDLC.inst.IsSkinDLC(rand, CorpExtToCorp(factType)))
                            spec.m_SkinID = rand;
                        else
                            spec.m_SkinID = 0;
                    }
                    else
                    {
                        if (factType == FactionTypesExt.GSO)
                        {
                            if (!ManDLC.inst.IsSkinDLC(skinset + (skinset != 0 ? 3 : 0), CorpExtToCorp(factType)))
                                spec.m_SkinID = (byte)(skinset + (skinset != 0 ? 3 : 0));
                            else if (!ManDLC.inst.IsSkinDLC(skinset2 + (skinset2 != 0 ? 3 : 0), CorpExtToCorp(factType)))
                                spec.m_SkinID = (byte)(skinset2 + (skinset2 != 0 ? 3 : 0));
                            else
                                spec.m_SkinID = 0;
                        }
                        else
                        {
                            if (!ManDLC.inst.IsSkinDLC(skinset, CorpExtToCorp(factType)))
                                spec.m_SkinID = skinset;
                            else if (!ManDLC.inst.IsSkinDLC(skinset2, CorpExtToCorp(factType)))
                                spec.m_SkinID = skinset2;
                            else
                                spec.m_SkinID = 0;
                        }
                    }

                    if (spamLog)
                        Debug.Log("TTETCUtil: InstantTech - Added " + mem.t);
                    data.m_BlockSpecs.Add(spec);
                }
                else
                {
                    if (spamLog)
                        Debug.Log("TTETCUtil: InstantTech - Removed " + mem.t + " as it was not loaded");
                }
            }
            //Debug.Log("TTETCUtil: ExportRawTechToTechData - Exported " + name);

            blockIDs = BTs.ToArray();
            return data;
        }

        public static string SaveTechToRawJSON(Tank tank)
        {
            return TechToJSONExternal(tank);
        }

        /// <summary>
        /// Mandatory for techs that have plans to be built over time by the building AI.
        /// The first block being an anchored block will determine if the entire techs live or not
        ///   on spawning. If this fails, there's a good chance the AI could have wasted money on it.
        /// </summary>
        /// <param name="ToSearch">The list of blocks to find the new root in</param>
        /// <returns>The new Root block</returns>
        public static TankBlock FindProperRootBlockExternal(List<TankBlock> ToSearch)
        {
            bool IsAnchoredAnchorPresent = false;
            float close = 128 * 128;
            TankBlock newRoot = ToSearch.FirstOrDefault();
            foreach (TankBlock bloc in ToSearch)
            {
                Vector3 blockPos = bloc.CalcFirstFilledCellLocalPos();
                float sqrMag = blockPos.sqrMagnitude;
                if (bloc.GetComponent<ModuleAnchor>() && bloc.GetComponent<ModuleAnchor>().IsAnchored)
                {   // If there's an anchored anchor, then we base the root off of that
                    //  It's probably a base
                    IsAnchoredAnchorPresent = true;
                    break;
                }
                if (sqrMag < close && (bloc.GetComponent<ModuleTechController>() || bloc.GetComponent<ModuleAIBot>()))
                {
                    close = sqrMag;
                    newRoot = bloc;
                }
            }
            if (IsAnchoredAnchorPresent)
            {
                close = 128 * 128;
                foreach (TankBlock bloc in ToSearch)
                {
                    Vector3 blockPos = bloc.CalcFirstFilledCellLocalPos();
                    float sqrMag = blockPos.sqrMagnitude;
                    if (sqrMag < close && bloc.GetComponent<ModuleAnchor>() && bloc.GetComponent<ModuleAnchor>().IsAnchored)
                    {
                        close = sqrMag;
                        newRoot = bloc;
                    }
                }
            }
            return newRoot;
        }
        private static List<BlockMemory> TechToMemoryExternal(Tank tank)
        {
            // This resaves the whole tech cab-forwards regardless of original rotation
            //   It's because any solutions that involve the cab in a funny direction will demand unholy workarounds.
            //   I seriously don't know why the devs didn't try it this way, perhaps due to lag reasons.
            //   or the blocks that don't allow upright placement (just detach those lmao)
            List<BlockMemory> output = new List<BlockMemory>();
            List<TankBlock> ToSave = tank.blockman.IterateBlocks().ToList();
            Vector3 coreOffset = Vector3.zero;
            Quaternion coreRot;
            TankBlock rootBlock = FindProperRootBlockExternal(ToSave);
            if (rootBlock != null)
            {
                if (rootBlock != ToSave.FirstOrDefault())
                {
                    ToSave.Remove(rootBlock);
                    ToSave.Insert(0, rootBlock);
                }
                coreOffset = rootBlock.trans.localPosition;
                coreRot = rootBlock.trans.localRotation;
                tank.blockman.SetRootBlock(rootBlock);
            }
            else
                coreRot = new OrthoRotation(OrthoRotation.r.u000);

            foreach (TankBlock bloc in ToSave)
            {
                if (!Singleton.Manager<ManSpawn>.inst.IsTankBlockLoaded(bloc.BlockType))
                    continue;
                Quaternion deltaRot = Quaternion.Inverse(coreRot);
                BlockMemory mem = new BlockMemory
                {
                    t = bloc.name,
                    p = deltaRot * (bloc.trans.localPosition - coreOffset)
                };
                // get rid of floating point errors
                mem.TidyUp();
                //Get the rotation
                mem.r = SetCorrectRotation(bloc.trans.localRotation, deltaRot).rot;
                if (!IsValidRotation(bloc, mem.r))
                {   // block cannot be saved - illegal rotation.
                    Debug.Log("TTETCUtil:  DesignMemory - " + tank.name + ": could not save " + bloc.name + " in blueprint due to illegal rotation.");
                    continue;
                }
                output.Add(mem);
            }

            return output;
        }
        public static string TechToJSONExternal(Tank tank)
        {   // Saving a Tech from the BlockMemory
            return MemoryToJSONExternal(TechToMemoryExternal(tank));
        }

        private static StringBuilder TechRAW = new StringBuilder();
        private static StringBuilder TechRAWCase = new StringBuilder();
        private static string MemoryToJSONExternal(List<BlockMemory> mem)
        {   // Saving a Tech from the BlockMemory
            if (mem.Count == 0)
                return null;
            try
            {
                TechRAW.Append(JsonUtility.ToJson(mem.FirstOrDefault()));
                for (int step = 1; step < mem.Count; step++)
                {
                    TechRAW.Append("|");
                    TechRAW.Append(JsonUtility.ToJson(mem.ElementAt(step)));
                }
            }
            finally
            {
                TechRAW.Clear();
            }
            try
            {
                foreach (char ch in TechRAW.ToString())
                {
                    if (ch == '"')
                    {
                        //TechRAWCase.Append("\\");
                        TechRAWCase.Append(ch);
                    }
                    else
                        TechRAWCase.Append(ch);
                }
                //Debug.Log("TTETCUtil: " + JSONTech.ToString());
                return TechRAWCase.ToString();
            }
            finally
            {
                TechRAWCase.Clear();
            }
        }

        public static bool IsValidRotation(TankBlock TB, OrthoRotation.r r)
        {
            return true; // can't fetch proper context for some reason
        }

        public static OrthoRotation SetCorrectRotation(Quaternion blockRot, Quaternion changeRot)
        {
            Quaternion qRot2 = Quaternion.identity;
            Vector3 endRotF = blockRot * Vector3.forward;
            Vector3 endRotU = blockRot * Vector3.up;
            Vector3 foA = changeRot * endRotF;
            Vector3 upA = changeRot * endRotU;
            qRot2.SetLookRotation(foA, upA);
            OrthoRotation rot = new OrthoRotation(qRot2);
            if (rot != qRot2)
            {
                bool worked = false;
                for (int step = 0; step < OrthoRotation.NumDistinctRotations; step++)
                {
                    OrthoRotation rotT = new OrthoRotation(OrthoRotation.AllRotations[step]);
                    bool isForeMatch = rotT * Vector3.forward == foA;
                    bool isUpMatch = rotT * Vector3.up == upA;
                    if (isForeMatch && isUpMatch)
                    {
                        rot = rotT;
                        worked = true;
                        break;
                    }
                }
                if (!worked)
                {
                    Debug.Log("TerraTechETCUtil: ReplaceBlock - Matching failed - OrthoRotation is missing edge case");
                }
            }
            return rot;
        }


#if !EDITOR
        internal class GUIManaged : GUILayoutHelpers
        {
            private static bool controlledDisp = false;
            private static bool controlledDisp2 = false;
            private static bool controlledDisp3 = false;
            private static bool controlledDisp4 = false;
            private static HashSet<string> enabledTabs = null;
            private static BiomeTypes curBiome = BiomeTypes.Grassland;
            private static bool showUtils = false;
            private static Visible Vis = null;
            private static GameObject GrabbedObject;
            private static Biome biome;
            private static Tank tank => Vis ? Vis.isActive ? Vis?.tank : null : null;
            private static TankBlock block => Vis ? Vis.isActive ? Vis?.block : null : null;
            private static ResourceDispenser node => Vis ? Vis.isActive ? Vis?.resdisp : null : null;
            private static ResourcePickup chunk => Vis ? Vis.isActive ? Vis?.pickup : null : null;
            private static Visible visible => Vis ? Vis.isActive ? Vis : null : null;
            private static int ItemID;
            private static ManIngameWiki.WikiPage Page;



            private static bool snapTerrain = false;
            private static string Tech = "";
            private static int mask = Globals.inst.layerTank.mask | Globals.inst.layerTerrain.mask | Globals.inst.layerScenery.mask | 
                Globals.inst.layerPickup.mask | Globals.inst.layerLandmark.mask;
            public static void OnGrab(ManPointer.Event eventC, bool down, bool clicked)
            {
                if (clicked && !ManPointer.inst.IsInteractionBlocked &&
                    Physics.Raycast(ManUI.inst.ScreenPointToRay(ManPointer.inst.DragPositionOnScreen),
                    out var hit, float.MaxValue, mask, QueryTriggerInteraction.Ignore))
                {
                    GrabbedObject = hit.collider.gameObject;
                    Vis = Visible.FindVisibleUpwards(hit.collider);
                    biome = null;
                    Page = null;
                    if (tank)
                    {
                    }
                    else if (node)
                    {
                        ItemID = Vis.m_ItemType.ItemType;
                        Page = ManIngameWiki.GetPage(StringLookup.GetItemName(ObjectTypes.Scenery, ItemID));
                    }
                    else if (block)
                    {
                        ItemID = Vis.m_ItemType.ItemType;
                        Page = ManIngameWiki.GetPage(StringLookup.GetItemName(ObjectTypes.Block, ItemID));
                    }
                    else if (chunk)
                    {
                        ItemID = Vis.m_ItemType.ItemType;
                        Page = ManIngameWiki.GetPage(StringLookup.GetItemName(ObjectTypes.Chunk, ItemID));
                    }
                    else
                    {
                        if (hit.collider.GetComponent<Terrain>() && 
                            ManWorld.inst.TileManager.LookupTile(hit.point) != null)
                        {
                            biome = ManWorld.inst.GetBiomeWeightsAtScenePosition(hit.point).Biome(0);
                            if (biome)
                            {
                                string name = WikiPageBiome.CleanupName(biome.name);
                                ItemID = (int)biome.BiomeType;
                                Page = ManIngameWiki.GetPage(name);
                            }
                        }
                        else if (GrabbedObject)
                        {
                        }
                    }
                }
            }
            public static void GUIInfoExtractor()
            {
                GUILayout.Box("--- Info Extractor --- ");
                if (GUILayout.Button(" Enabled Loading: " + controlledDisp))
                {
                    controlledDisp = !controlledDisp;
                    if (controlledDisp)
                        ManPointer.inst.MouseEvent.Subscribe(OnGrab);
                    else
                        ManPointer.inst.MouseEvent.Unsubscribe(OnGrab);
                }
                if (controlledDisp)
                {
                    try
                    {
                        if (visible)
                        {
                            GUILayout.BeginHorizontal();
                            if (tank)
                            {
                                GUILayout.Label("Selected Tech: ");
                                GUILayout.Label(tank.name);
                            }
                            else if (node)
                            {
                                GUILayout.Label("Selected Scenery: ");
                                GUILayout.Label(node.name);
                            }
                            else if (block)
                            {
                                GUILayout.Label("Selected Block: ");
                                GUILayout.Label(block.name);
                            }
                            else if (chunk)
                            {
                                GUILayout.Label("Selected Chunk: ");
                                GUILayout.Label(chunk.name);
                            }
                            else
                            {
                                GUILayout.Label("Selected Visible: ");
                                GUILayout.Label(visible.name);
                            }
                            GUILayout.FlexibleSpace();
                            GUILayout.EndHorizontal();

                            GUILayout.BeginHorizontal();
                            GUILayout.Label("Type: ");
                            GUILayout.Label(visible.type.ToString());
                            GUILayout.FlexibleSpace();
                            GUILayout.EndHorizontal();

                            if (tank)
                            {
                                GUILayout.BeginHorizontal();
                                GUILayout.Label("Block Count: ");
                                GUILayout.Label(tank.blockman.blockCount.ToString());
                                GUILayout.FlexibleSpace();
                                GUILayout.EndHorizontal();
                            }
                            else if (node)
                            {
                                GUILayout.FlexibleSpace();
                                if (GUILayout.Button("Open In Wiki", AltUI.ButtonOrangeLarge))
                                {
                                    ManIngameWiki.GetBlockPage(StringLookup.GetItemName(ObjectTypes.Scenery, ItemID)).GoHere();
                                    ManIngameWiki.SetGUI(true);
                                    DebugExtUtilities.Close();
                                }
                            }
                            else if (block)
                            {
                                GUILayout.FlexibleSpace();
                                if (GUILayout.Button("Open In Wiki", AltUI.ButtonOrangeLarge))
                                {
                                    ManIngameWiki.GetBlockPage(StringLookup.GetItemName(ObjectTypes.Block, ItemID)).GoHere();
                                    ManIngameWiki.SetGUI(true);
                                    DebugExtUtilities.Close();
                                }
                            }
                            else if (chunk)
                            {
                                GUILayout.FlexibleSpace();
                                if (GUILayout.Button("Open In Wiki", AltUI.ButtonOrangeLarge))
                                {
                                    ManIngameWiki.GetBlockPage(StringLookup.GetItemName(ObjectTypes.Chunk, ItemID)).GoHere();
                                    ManIngameWiki.SetGUI(true);
                                    DebugExtUtilities.Close();
                                }
                            }
                            else
                            {
                                GUILayout.BeginHorizontal();
                                GUILayout.Label("Selected Visible: ");
                                GUILayout.Label(visible.name);
                                GUILayout.FlexibleSpace();
                                GUILayout.EndHorizontal();
                            }
                            if (ActiveGameInterop.IsReady)
                            {
                                if (GUILayout.Button("Try rebuild in editor", AltUI.ButtonGreen))
                                    ActiveGameInterop.TransmitBlock(block);
                                GUILayout.Label("Note the editor only supports loading in YOUR own assets!");
                            }
                            else
                            {
                                GUILayout.Button("Try rebuild in editor", AltUI.ButtonGrey);
                                GUILayout.Label("Need to hook up to UnityEditor first!");
                            }

                            if (Page != null)
                                Page.DisplayGUI();

                        }
                        else if (biome != null)
                        {
                            GUILayout.BeginHorizontal();
                            GUILayout.Label("Selected Biome: ");
                            string name = WikiPageBiome.CleanupName(biome.name);
                            GUILayout.Label(name);
                            GUILayout.FlexibleSpace();
                            GUILayout.EndHorizontal();

                            if (Page != null)
                                Page.DisplayGUI();
                            if (GUILayout.Button("Open In Wiki", AltUI.ButtonOrangeLarge))
                            {
                                Page.GoHere();
                                ManIngameWiki.SetGUI(true);
                                DebugExtUtilities.Close();
                            }
                        }
                        else if (GrabbedObject)
                        {
                            GUILayout.BeginHorizontal();
                            GUILayout.Label("Selected Object: ");
                            GUILayout.Label(GrabbedObject.name.NullOrEmpty() ? "<NULL>" : GrabbedObject.name);
                            GUILayout.FlexibleSpace();
                            GUILayout.EndHorizontal();
                        }
                        else
                        {
                            GUILayout.Label("Click on something to scan it!");
                        }
                    }
                    catch (ExitGUIException e)
                    {
                        throw e;
                    }
                    catch { controlledDisp = false; }
                }
            }

            public static void GUIBlockIndexer()
            {
                GUILayout.Box("--- Blocks Indexing --- ");
                if (GUILayout.Button(" Enabled Loading: " + controlledDisp2))
                {
                    controlledDisp2 = !controlledDisp2;
                }
                if (controlledDisp2)
                {
                    try
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Blocks Registered In Lookup:");
                        GUILayout.Label(errorNames.Count.ToString());
                        GUILayout.FlexibleSpace();
                        GUILayout.EndHorizontal();
                    }
                    catch (ExitGUIException e)
                    {
                        throw e;
                    }
                    catch { controlledDisp2 = false; }
                }
            }

            public static void GUIRawTechs()
            {
                GUILayout.Box("--- RawTechs --- ");
                bool show = controlledDisp3 && Singleton.playerTank;
                if (GUILayout.Button(" Enabled Loading: " + show))
                    controlledDisp3 = !controlledDisp3;
                if (controlledDisp3)
                {
                    try
                    {
                        if (GUILayout.Button("Tech Utils"))
                        {
                            showUtils = !showUtils;
                            if (!showUtils)
                                Tech = null;
                        }
                        if (showUtils)
                        {
                            var playerT = Singleton.playerTank;
                            if (playerT)
                            {
                                if (GUILayout.Button("Export RawTech"))
                                {
                                    Tech = "\"" + RawTechBase.TechToJSONExternal(playerT).Replace("\"", "\\\"") + "\"";
                                }
                                if (GUILayout.Button("Export RawTechTemplate"))
                                {
                                    TechData TD = RawTechBase.CreateNewTechData();
                                    TD.SaveTech(Singleton.playerTank);
                                    Tech = JsonConvert.SerializeObject(new RawTechTemplate(TD));
                                }
                                if (!Tech.NullOrEmpty())
                                {
                                    var type = ManGameMode.inst.GetCurrentGameType();
                                    if (type == ManGameMode.GameType.Creative || type == ManGameMode.GameType.RaD ||
                                        type == ManGameMode.GameType.Misc || type == ManGameMode.GameType.Attract)
                                    {
                                        if (GUILayout.Button("Spawn RawTech"))
                                        {
                                            try
                                            {
                                                string cleaned = Tech.Replace("\\\"", "\"");
                                                if (cleaned.StartsWith("\""))
                                                    cleaned = cleaned.Remove(0, 1);
                                                if (cleaned.EndsWith("\""))
                                                    cleaned = cleaned.Remove(cleaned.Length - 1, 1);
                                                new RawTechTemplate(RawTechToTechData("Raw Tech", cleaned, out _)).SpawnRawTech(
                                                    playerT.boundsCentreWorld + playerT.rootBlockTrans.forward * 100, ManPlayer.inst.PlayerTeam,
                                                    -playerT.rootBlockTrans.forward);
                                                ManSFX.inst.PlayUISFX(ManSFX.UISfxType.AIFollow);
                                            }
                                            catch
                                            {
                                                ManSFX.inst.PlayUISFX(ManSFX.UISfxType.AnchorFailed);
                                            }
                                        }
                                        if (GUILayout.Button("Spawn RawTechTemplate"))
                                        {
                                            try
                                            {
                                                JsonConvert.DeserializeObject<RawTechTemplate>(Tech).SpawnRawTech(
                                                    playerT.boundsCentreWorld + playerT.rootBlockTrans.forward * 100, ManPlayer.inst.PlayerTeam,
                                                    -playerT.rootBlockTrans.forward);
                                                ManSFX.inst.PlayUISFX(ManSFX.UISfxType.AIFollow);
                                            }
                                            catch
                                            {
                                                ManSFX.inst.PlayUISFX(ManSFX.UISfxType.AnchorFailed);
                                            }
                                        }
                                    }
                                    else
                                        GUILayout.Button("Spawn RawTech needs Creative");
                                    Tech = GUILayout.TextArea(Tech, AltUI.TextfieldBlackHuge,
                                        GUILayout.MaxWidth(DebugExtUtilities.HotWindow.width));
                                }
                            }
                            else
                                GUILayout.Button("You must be\ncontrolling\na Tech!");
                        }
                    }
                    catch (ExitGUIException e)
                    {
                        throw e;
                    }
                    catch { controlledDisp3 = false; }
                }
            }

            public static void GUIExtHints()
            {
                GUILayout.Box("--- External Hints --- ");
                bool show = controlledDisp4 && Singleton.playerTank;
                if (GUILayout.Button(" Enabled Loading: " + show))
                    controlledDisp4 = !controlledDisp4;
                if (controlledDisp4)
                {
                    if (GUILayout.Button("Random Mod Hint", AltUI.ButtonBlueLarge))
                        ExtUsageHint.ShowRandomExternalHint();
                }
            }

            public static void GUIGetTotalManaged()
            {
                if (enabledTabs == null)
                {
                    enabledTabs = new HashSet<string>();
                }
                GUIInfoExtractor();
                GUIBlockIndexer();
                GUIRawTechs();
                GUIExtHints();
            }
        }
#endif

        internal class SnapshotsPatchBatch
        {
            internal static class ManScreenshotPatches
            {
                internal static Type target = typeof(ManScreenshot);
                //BlockBuggedConverter
                private static bool RunSnapshotConversionTool_Prefix(ref string snapshotPath, ref bool __result)
                {
                    if (TryRepairSnapshot(snapshotPath))
                        __result = true;
                    return UseVanillaFallbackSnapUtility;
                }
            }
        }

        [Serializable]
        private class BlockMemory
        {   // Save the blocks!
            public string t = BlockTypes.GSOAIController_111.ToString(); //blocktype
            public Vector3 p = Vector3.zero;
            public OrthoRotation.r r = OrthoRotation.r.u000;

            /// <summary>
            /// get rid of floating point errors
            /// </summary>
            public void TidyUp()
            {
                p.x = Mathf.RoundToInt(p.x);
                p.y = Mathf.RoundToInt(p.y);
                p.z = Mathf.RoundToInt(p.z);
            }
        }

        /// <summary>
        /// Obsolete - Will phase out.
        /// </summary>
        private enum FactionTypesExt
        {
            // No I do not make any of the corps below (exclusing some of TAC and EFF) 
            //  - but these are needed to allow the AI to spawn the right bases with 
            //    the right block ranges
            // OFFICIAL
            NULL,   // not a corp, really, probably the most unique of all lol
            GSO,    // Galactic Survey Organization
            GC,     // GeoCorp
            EXP,    // Reticule Research
            VEN,    // VENture
            HE,     // HawkEye
            SPE,    // Special
            BF,     // Better Future
            SJ,     // Space Junkers
            LEG,    // LEGION!!1!

            // Community
            AER,    // Aerion
            BL,     // Black Labs (EXT OF HE)
            CC,     // CrystalCorp
            DC,     // DEVCorp
            DL,     // DarkLight
            EYM,    // Ellydium
            GT,     // GreenTech
            HS,     // Hyperion Systems
            IEC,    // Independant Earthern Colonies
            LK,     // Lemon Kingdom
            OS,     // Old Stars
            TC,     // Tofuu Corp
            TAC,    // Technocratic AI Colony

            // idk
            EFF,    // Emperical Forge Fabrication
            MCC,    // Mechaniccoid Cooperative Confederacy 
            BLN,    // BuLwark Nation (Bulin)
            CNC,    // ClaNg Clads (ChanClas)
            LOL,    // Larry's Overlord Laser
        }
    }
}
