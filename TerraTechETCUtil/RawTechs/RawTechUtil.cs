using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Newtonsoft.Json;

namespace TerraTechETCUtil
{
    public static class RawTechUtil
    {
        public const int FrameImpactingTechBlockCount = 256;
#if !EDITOR
        /// <summary>
        /// Only call for cases where we want only vanilla corps!
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static FactionTypesExt GetCorpExtended(BlockTypes type)
        {
            return (FactionTypesExt)Singleton.Manager<ManSpawn>.inst.GetCorporation(type);
        }
        public static bool IsFactionExtension(FactionTypesExt ext)
        {
            return ext >= FactionTypesExt.AER && ext <= FactionTypesExt.LOL;
        }
        public static FactionSubTypes CorpExtToCorp(FactionTypesExt corpExt)
        {
            switch (corpExt)
            {
                case FactionTypesExt.SPE:
                //case FactionTypesExt.GSO:
                case FactionTypesExt.GT:
                case FactionTypesExt.IEC:
                    return FactionSubTypes.GSO;
                //case FactionTypesExt.GC:
                case FactionTypesExt.EFF:
                case FactionTypesExt.LK:
                    return FactionSubTypes.GC;
                //case FactionTypesExt.VEN:
                case FactionTypesExt.OS:
                    return FactionSubTypes.VEN;
                //case FactionTypesExt.HE:
                case FactionTypesExt.BL:
                case FactionTypesExt.TAC:
                    return FactionSubTypes.HE;
                //case FactionTypesExt.BF:
                case FactionTypesExt.DL:
                case FactionTypesExt.EYM:
                case FactionTypesExt.HS:
                    return FactionSubTypes.BF;
                case FactionTypesExt.EXP:
                    return FactionSubTypes.EXP;
            }
            return (FactionSubTypes)corpExt;
        }
        public static FactionTypesExt CorpExtToVanilla(FactionTypesExt corpExt)
        {
            switch (corpExt)
            {
                case FactionTypesExt.SPE:
                //case FactionTypesExt.GSO:
                case FactionTypesExt.GT:
                case FactionTypesExt.IEC:
                    return FactionTypesExt.GSO;
                //case FactionTypesExt.GC:
                case FactionTypesExt.EFF:
                case FactionTypesExt.LK:
                    return FactionTypesExt.GC;
                //case FactionTypesExt.VEN:
                case FactionTypesExt.OS:
                    return FactionTypesExt.VEN;
                //case FactionTypesExt.HE:
                case FactionTypesExt.BL:
                case FactionTypesExt.TAC:
                    return FactionTypesExt.HE;
                //case FactionTypesExt.BF:
                case FactionTypesExt.DL:
                case FactionTypesExt.EYM:
                case FactionTypesExt.HS:
                    return FactionTypesExt.BF;
                case FactionTypesExt.EXP:
                    return FactionTypesExt.EXP;
            }
            return corpExt;
        }

        public static FactionLevel GetFactionLevel(FactionSubTypes FST)
        {
            switch (FST)
            {
                case FactionSubTypes.NULL:
                case FactionSubTypes.GSO:
                case FactionSubTypes.SPE:
                    return FactionLevel.GSO;
                case FactionSubTypes.GC:
                    return FactionLevel.GC;
                case FactionSubTypes.EXP:
                    return FactionLevel.EXP;
                case FactionSubTypes.VEN:
                    return FactionLevel.VEN;
                case FactionSubTypes.HE:
                    return FactionLevel.HE;
                case FactionSubTypes.BF:
                    return FactionLevel.BF;
                default:
                    return FactionLevel.MOD;
            }
        }

        private static List<FactionTypesExt> SortCorps(List<FactionTypesExt> unsorted)
        {
            List<FactionTypesExt> distinct = unsorted.Distinct().ToList();
            List<KeyValuePair<int, FactionTypesExt>> sorted = new List<KeyValuePair<int, FactionTypesExt>>();
            foreach (FactionTypesExt FTE in distinct)
            {
                int countOut = unsorted.FindAll(delegate (FactionTypesExt cand) { return cand == FTE; }).Count();
                sorted.Add(new KeyValuePair<int, FactionTypesExt>(countOut, FTE));
            }
            sorted = sorted.OrderByDescending(x => x.Key).ToList();
            distinct.Clear();
            foreach (KeyValuePair<int, FactionTypesExt> intBT in sorted)
            {
                distinct.Add(intBT.Value);
            }
            return distinct;
        }
        public static FactionTypesExt GetMainCorpExt(this Tank tank)
        {
            List<FactionTypesExt> toSort = new List<FactionTypesExt>();
            foreach (TankBlock BlocS in tank.blockman.IterateBlocks())
            {
                toSort.Add(GetBlockCorpExt(BlocS.BlockType));
            }
            toSort = SortCorps(toSort);
            FactionTypesExt final = toSort.FirstOrDefault();

            //Debug_TTExt.Log("RawTech: GetMainCorpExt - Selected " + final + " for main corp");
            return final;
        }
        public static FactionTypesExt GetMainCorpExt(this TechData tank)
        {
            List<FactionTypesExt> toSort = new List<FactionTypesExt>();
            foreach (TankPreset.BlockSpec BlocS in tank.m_BlockSpecs)
            {
                toSort.Add(GetBlockCorpExt(BlocS.m_BlockType));
            }
            toSort = SortCorps(toSort);
            return toSort.FirstOrDefault();//(FactionTypesExt)tank.GetMainCorporations().FirstOrDefault();
        }
        public static FactionTypesExt GetMainCorpExt(this List<RawBlockMem> tank)
        {
            List<FactionTypesExt> toSort = new List<FactionTypesExt>();
            foreach (RawBlockMem BlocS in tank)
            {
                toSort.Add(GetBlockCorpExt(BlockIndexer.StringToBlockType(BlocS.t)));
            }
            toSort = SortCorps(toSort);
            return toSort.FirstOrDefault();//(FactionTypesExt)tank.GetMainCorporations().FirstOrDefault();
        }
        public static FactionTypesExt GetMainCorpExt(this List<RawBlock> tank)
        {
            List<FactionTypesExt> toSort = new List<FactionTypesExt>();
            foreach (RawBlock BlocS in tank)
            {
                toSort.Add(GetBlockCorpExt(BlockIndexer.StringToBlockType(BlocS.t)));
            }
            toSort = SortCorps(toSort);
            return toSort.FirstOrDefault();//(FactionTypesExt)tank.GetMainCorporations().FirstOrDefault();
        }
        internal static FactionTypesExt GetBlockCorpExt(BlockTypes BT)
        {
            int BTval = (int)BT;
            if (BTval < 5000)// Payload's range
                return (FactionTypesExt)Singleton.Manager<ManSpawn>.inst.GetCorporation(BT);

            if (BTval >= 300000 && BTval <= 303999) // Old Star
            {   // This should work until Pachu makes a VEN Block
                if (Singleton.Manager<ManSpawn>.inst.GetCorporation(BT) == FactionSubTypes.VEN)
                    return FactionTypesExt.OS;
            }
            if (BTval >= 419000 && BTval <= 419999) // Lemon Kingdom - Mobile Kingdom
                return FactionTypesExt.LK;
            if (BTval == 584147)
                return FactionTypesExt.TAC;
            if (BTval >= 584200 && BTval <= 584599) // Technocratic AI Colony - Power Density
                return FactionTypesExt.TAC;
            if (BTval >= 584600 && BTval <= 584750) // Emperical Forge Fabrication - Unit Count
                return FactionTypesExt.EFF;
            if (BTval >= 911000 && BTval <= 912000) // GreenTech - Eco Rangers
                return FactionTypesExt.GT;

            return (FactionTypesExt)Singleton.Manager<ManSpawn>.inst.GetCorporation(BT);
        }


        private static StringBuilder RAW = new StringBuilder();
        private static StringBuilder RAWCase = new StringBuilder();
        private static List<RawBlockMem> nonAlloc = new List<RawBlockMem>();
        /// <summary>
        /// Checks all of the blocks in a BaseTemplate Tech to make sure it's safe to spawn as well as calculate other requirements for it.
        /// </summary>
        public static bool ValidateBlocksInTech(this RawTechTemplate templateToCheck)
        {
            try
            {
                foreach (char ch in templateToCheck.savedTech)
                {
                    if (ch != '\\')
                    {
                        RAW.Append(ch);
                    }
                }
                FactionLevel greatestFaction = FactionLevel.GSO;
                try
                {
                    foreach (char ch in RAW.ToString())
                    {
                        if (ch == '|')//new block
                        {
                            nonAlloc.Add(JsonUtility.FromJson<RawBlockMem>(RAWCase.ToString()));
                            RAWCase.Clear();
                        }
                        else
                            RAWCase.Append(ch);
                    }
                    nonAlloc.Add(JsonUtility.FromJson<RawBlockMem>(RAWCase.ToString()));
                }
                catch
                {
                    Debug_TTExt.Assert(true, "RawTech: ValidateBlocksInTech - Loading error - File was edited or corrupted!");
                    greatestFaction = FactionLevel.GSO;
                    return false;
                }
                finally
                {
                    RAWCase.Clear();
                }
                bool valid = true;
                if (!nonAlloc.Any())
                {
                    greatestFaction = FactionLevel.GSO;
                    Debug_TTExt.Log("RawTech: ValidateBlocksInTech - FAILED as no blocks were present!");
                    return false;
                }
                int basePrice = 0;
                foreach (RawBlockMem bloc in nonAlloc)
                {
                    BlockTypes type = BlockIndexer.StringToBlockType(bloc.t);
                    if (!Singleton.Manager<ManSpawn>.inst.IsTankBlockLoaded(type))
                    {
                        valid = false;
                        continue;
                    }

                    FactionSubTypes FST = Singleton.Manager<ManSpawn>.inst.GetCorporation(type);
                    FactionLevel FL = GetFactionLevel(FST);
                    if (FL >= FactionLevel.ALL)
                    {
                        if (ManMods.inst.IsModdedCorp(FST))
                        {
                            ModdedCorpDefinition MCD = ManMods.inst.GetCorpDefinition(FST);
                            if (Enum.TryParse(MCD.m_RewardCorp, out FactionSubTypes FST2))
                            {
                                FST = FST2;
                            }
                            else
                                throw new Exception("There's a block given that has an invalid corp \nBlockType: " + type);
                        }
                        else
                            throw new Exception("There's a block given that has an invalid corp \nCorp: " + FL + " \nBlockType: " + type);
                    }
                    if (greatestFaction < FL)
                        greatestFaction = FL;
                    basePrice += Singleton.Manager<RecipeManager>.inst.GetBlockBuyPrice(type);
                    bloc.t = Singleton.Manager<ManSpawn>.inst.GetBlockPrefab(type).name;
                }
                templateToCheck.baseCost = basePrice;
                templateToCheck.factionLim = greatestFaction;
                templateToCheck.blockCount = nonAlloc.Count;

                // Rebuild in workable format
                templateToCheck.savedTech = RawTechTemplate.MemoryToJSONExternal(nonAlloc);

                return valid;
            }
            catch
            {
                Debug_TTExt.Log("RawTech: ValidateBlocksInTech - Tech was corrupted via unexpected mod changes!");
                return false;
            }
            finally
            {
                nonAlloc.Clear();
                RAW.Clear();
            }
        }
#endif
    }
#if !EDITOR
    public interface RawBlockBase
    {
        string tg { get; }
        Vector3 pg { get; }
        OrthoRotation.r rg { get; }

        void CopyFrom(RawBlockBase blockBase);
    }

    [Serializable]
    public class RawBlockMem : RawBlockBase
    {   // Save the blocks!
        public string t = BlockTypes.GSOAIController_111.ToString(); //blocktype
        public Vector3 p = Vector3.zero;
        public OrthoRotation.r r = OrthoRotation.r.u000;
        public string tg => t;
        public Vector3 pg => p;
        public OrthoRotation.r rg => r;

        public void CopyFrom(RawBlockBase RBB)
        {
            t = RBB.tg;
            p = RBB.pg;
            r = RBB.rg;
        }

        public BlockTypes typeSlow => BlockIndexer.StringToBlockType(t);
        public TankBlock instSlow => ManSpawn.inst.GetBlockPrefab(typeSlow);

        /// <summary>
        /// get rid of floating point errors
        /// </summary>
        public void TidyUp()
        {
            p.x = Mathf.RoundToInt(p.x);
            p.y = Mathf.RoundToInt(p.y);
            p.z = Mathf.RoundToInt(p.z);
        }
        public bool IsValidException()
        {
            if (!Singleton.Manager<ManSpawn>.inst.IsTankBlockLoaded(typeSlow))
                throw new NullReferenceException();
            return true;
        }
        public bool IsValid()
        {
            return Singleton.Manager<ManSpawn>.inst.IsTankBlockLoaded(typeSlow);
        }


        public RawBlockMem()
        {
        }
        public RawBlockMem(BlockTypes BT)
        {
            var inst = ManSpawn.inst.GetBlockPrefab(BT);
            if (inst == null)
                throw new NullReferenceException("Block type " + BT.ToString() + " does not exist");
            t = inst.name;
        }
        public RawBlockMem(BlockTypes BT, Vector3 position) : this(BT)
        {
            p = position;
            TidyUp();
        }
        public RawBlockMem(BlockTypes BT, Vector3 position, OrthoRotation.r rotation) : this(BT)
        {
            p = position;
            r = rotation;
            TidyUp();
        }
        public RawBlockMem(BlockTypes BT, Vector3 position, OrthoRotation rotation) : this(BT)
        {
            p = position;
            r = rotation.rot;
            TidyUp();
        }
        public RawBlockMem(BlockTypes BT, Vector3 position, Quaternion rotation) : this(BT)
        {
            p = position;
            r = new OrthoRotation(rotation).rot;
            TidyUp();
        }
    }

    /// <summary>
    /// STRUCT! USE WITH CARE
    /// </summary>
    public struct RawBlock : RawBlockBase
    {   // Save the blocks!
        public string t; //blocktype
        public Vector3 p;
        public OrthoRotation.r r;
        public string tg => t;
        public Vector3 pg => p;
        public OrthoRotation.r rg => r;

        public void CopyFrom(RawBlockBase RBB)
        {
            t = RBB.tg;
            p = RBB.pg;
            r = RBB.rg;
        }

        public BlockTypes typeSlow => BlockIndexer.StringToBlockType(t);
        public TankBlock instSlow => ManSpawn.inst.GetBlockPrefab(typeSlow);

        /// <summary>
        /// get rid of floating point errors
        /// </summary>
        public void TidyUp()
        {
            p.x = Mathf.RoundToInt(p.x);
            p.y = Mathf.RoundToInt(p.y);
            p.z = Mathf.RoundToInt(p.z);
        }
        public bool IsValid()
        {
            return Singleton.Manager<ManSpawn>.inst.IsTankBlockLoaded(typeSlow);
        }

        public static RawBlock empty => new RawBlock()
        {
            t = BlockTypes.GSOAIController_111.ToString(),
            p = Vector3.zero,
            r = OrthoRotation.r.u000,
        };
        public RawBlock(BlockTypes BT)
        {
            var inst = ManSpawn.inst.GetBlockPrefab(BT);
            if (inst == null)
                throw new NullReferenceException("Block type " + BT.ToString() + " does not exist");
            t = inst.name;
            p = Vector3.zero;
            r = OrthoRotation.r.u000;
        }
        public RawBlock(BlockTypes BT, Vector3 position)
        {
            var inst = ManSpawn.inst.GetBlockPrefab(BT);
            if (inst == null)
                throw new NullReferenceException("Block type " + BT.ToString() + " does not exist");
            t = inst.name;
            p = position;
            r = OrthoRotation.r.u000;
            TidyUp();
        }
        public RawBlock(BlockTypes BT, Vector3 position, OrthoRotation.r rotation)
        {
            var inst = ManSpawn.inst.GetBlockPrefab(BT);
            if (inst == null)
                throw new NullReferenceException("Block type " + BT.ToString() + " does not exist");
            t = inst.name;
            p = position;
            r = rotation;
            TidyUp();
        }
        public RawBlock(BlockTypes BT, Vector3 position, OrthoRotation rotation)
        {
            var inst = ManSpawn.inst.GetBlockPrefab(BT);
            if (inst == null)
                throw new NullReferenceException("Block type " + BT.ToString() + " does not exist");
            t = inst.name;
            p = position;
            r = rotation.rot;
            TidyUp();
        }
        public RawBlock(BlockTypes BT, Vector3 position, Quaternion rotation)
        {
            var inst = ManSpawn.inst.GetBlockPrefab(BT);
            if (inst == null)
                throw new NullReferenceException("Block type " + BT.ToString() + " does not exist");
            t = inst.name;
            p = position;
            r = RawTechBase.SetCorrectRotation(rotation).rot;
            TidyUp();
        }
    }
    public class RequestAnchored : MonoBehaviour
    {
        sbyte delay = 4;
        private void Update()
        {
            delay--;
            if (delay == 0)
                Destroy(this);
        }
    }
#endif

    public enum FactionTypesExt
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
        LEG,    // Legion

        // Below is currently mostly unused as Custom Corps already address this.
        // Community
        AER = 256,    // Aerion
        BL = 257,     // Black Labs (EXT OF HE)
        CC = 258,     // CrystalCorp
        DC = 259,     // DEVCorp
        DL = 260,     // DarkLight
        EYM = 261,    // Ellydium
        GT = 262,     // GreenTech
        HS = 263,     // Hyperion Systems
        IEC = 264,    // Independant Earthern Colonies
        LK = 265,     // Lemon Kingdom
        OS = 266,     // Old Stars
        TC = 267,     // Tofuu Corp
        TAC = 268,    // Technocratic AI Colony

        // idk
        EFF = 269,    // Emperical Forge Fabrication
        MCC = 270,    // Mechaniccoid Cooperative Confederacy 
        BLN = 271,    // BuLwark Nation (Bulin)
        CNC = 272,    // ClaNg Clads (ChanClas)
        LOL = 273,    // Larry's Overlord Laser
    }

    /// <summary>
    /// ONLY SUPPORTS RANKING OF FactionSubTypes (No modded corps!)
    /// </summary>
    public enum FactionLevel
    {
        NULL,
        GSO,
        GC,
        VEN,
        HE,
        BF,
        EXP,
        ALL,
        MOD,
    }
    public enum BaseTypeLevel
    {
        Basic,      //basic base
        Advanced,
        Headquarters,
        Overkill,
        InvaderSpecific,
    }
    public enum BaseTerrain
    {
        Any,
        AnyNonSea,
        Land,   // is anchored
        Sea,    // floats on water
        Air,    // doubles as airplane
        Chopper,// relies on props to stay airborne
        Space   // mobile base that flies beyond
    }
    public enum BasePurpose
    {
        AnyNonHQ,       // Any base that's not an HQ
        HarvestingNoHQ, // Any harvesting base that's not an HQ
        Defense,        // Strictly defensive base element
        Harvesting,     // Has Delivery cannons
        Autominer,      // Can mine unlimited BB (DO NOT ATTACH THIS TAG TO HQs!!!)
        TechProduction, // Base with Explosive Bolts attached
                        //   As of Update 1.8.3, this is now considered a Garrison
        Headquarters,   // Calls in techs from orbit using funds
        MPUnsafe,       // MP blocked crafting blocks
        HasReceivers,   // Has receivers
        NotStationary,  // Mobile Tech
        AttractTech,    // Reserved for Attract (or an endgame spawn)
        NoWeapons,      // unarmed
        Fallback,       // run out of other options
        Sniper,          // Lock to harder difficulties
        NANI,           // Incomprehensibly powerful Tech spawn
    }
}
