using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TerraTechETCUtil
{
    /// <summary>
    /// The static utility for <see cref="RawTech"/>s
    /// <para>More static functions can be found in <seealso cref="RawTechBase"/></para>
    /// </summary>
    public static class RawTechUtil
    {
        /// <summary>
        /// Block counts equal to and above this are flagged as <b>Eradicator</b> with <see cref="BasePurpose.NANI"/>
        /// </summary>
        public const int FrameImpactingTechBlockCount = 256;
#if !EDITOR

        /// <summary>
        /// get the CORRECT <see cref="OrthoRotation.r"/>
        /// </summary>
        /// <param name="spec"></param>
        /// <returns></returns>
        public static OrthoRotation.r GetOrthoR(this TankPreset.BlockSpec spec) => new OrthoRotation(spec.orthoRotation).rot;

        /// <summary>
        /// Get the <see cref="FactionTypesExt"/> from the <see cref="FactionSubTypes"/>. Lossy!!!!
        /// <para><b>DO NOT STORE THIS VALUE ANYWHERE, USE THE STRING FORMAT INSTEAD</b></para>
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static FactionTypesExt GetCorpExtendedFromVanilla(FactionSubTypes type)
        {
            return (FactionTypesExt)type;
        }

        /// <summary>
        /// Returns if this is a custom faction
        /// </summary>
        /// <param name="ext"></param>
        /// <returns></returns>
        public static bool IsFactionExtension(FactionTypesExt ext)
        {
            return ext >= FactionTypesExt.AER && ext <= FactionTypesExt.LOL;
        }
        /// <summary>
        /// Converter
        /// </summary>
        /// <param name="corpExt"></param>
        /// <returns></returns>
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
        /// <summary>
        /// Converter
        /// </summary>
        /// <param name="corpExt"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Get the highest faction level based on corp progression
        /// </summary>
        /// <param name="FST"></param>
        /// <returns></returns>
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
                case FactionSubTypes.SJ:
                    return FactionLevel.SJ;
                case FactionSubTypes.BF:
                    return FactionLevel.BF;
                default:
                    return FactionLevel.MOD;
            }
        }

        /// <inheritdoc cref="RawTechBase.GetTopCorp(Tank)"/>
        /// <param name="tank"></param>
        /// <returns></returns>
        public static FactionTypesExt GetMainCorpExt(this Tank tank) =>
            RawTechBase.GetTopCorp(tank);
        /// <inheritdoc cref="RawTechBase.GetTopCorp(TechData)"/>
        /// <param name="tank"></param>
        /// <returns></returns>
        public static FactionTypesExt GetMainCorpExt(this TechData tank) =>
            RawTechBase.GetTopCorp(tank);
        /// <inheritdoc cref="RawTechBase.GetTopCorp(List{RawBlockMem})"/>
        /// <param name="tank"></param>
        /// <returns></returns>
        public static FactionTypesExt GetMainCorpExt(this List<RawBlockMem> tank) =>
            RawTechBase.GetTopCorp(tank);
        /// <inheritdoc cref="RawTechBase.GetTopCorp(List{RawBlock})"/>
        /// <param name="tank"></param>
        /// <returns></returns>
        public static FactionTypesExt GetMainCorpExt(this List<RawBlock> tank) =>
            RawTechBase.GetTopCorp(tank);
        /// <summary>
        /// Get the <see cref="FactionTypesExt"/> based on the <see cref="BlockTypes"/> given
        /// </summary>
        /// <param name="BT"></param>
        /// <returns></returns>
        internal static FactionTypesExt GetBlockCorpExt(BlockTypes BT)
        {
            int BTval = (int)BT;
            if (BTval < 5000)// Payload's + official modding range
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
        /// <inheritdoc cref="GetBlockCorpExt(BlockTypes)"/>
        public static FactionTypesExt GetCorpExtended(BlockTypes type) =>
            GetBlockCorpExt(type);

        /// <summary>
        /// Get the first block in the given <see cref="List{RawBlockMem}"/>
        /// </summary>
        /// <returns>The first block if found, otherwise fallback invalid <see cref="BlockTypes.GSOAIController_111"/></returns>
        public static BlockTypes GetFirstBlock(this List<RawBlockMem> tank)
        {
            if (tank == null || !tank.Any())
                return BlockTypes.GSOAIController_111;
            return tank.First().typeSlow;
        }
        /// <summary>
        /// Get the first block in the given <see cref="List{RawBlock}"/>
        /// </summary>
        /// <returns>The first block if found, otherwise fallback invalid <see cref="BlockTypes.GSOAIController_111"/></returns>
        public static BlockTypes GetFirstBlock(this List<RawBlock> tank)
        {
            if (tank == null || !tank.Any())
                return BlockTypes.GSOAIController_111;
            return tank.First().typeSlow;
        }


        private static List<RawBlockMem> nonAlloc = new List<RawBlockMem>();

        /// <summary>
        /// Checks all of the blocks in a <see cref="RawTechTemplate"/> to make sure it's safe to spawn as well as calculate other requirements for it.
        /// </summary>
        /// <param name="templateToCheck">The target to validate</param>
        /// <param name="removeInvalid">Set this to true to remove invalid blocks, 
        /// otherwise it will stop loading the tech as soon as an invalid block is encountered</param>
        /// <param name="throwOnFail">Make this throw an <see cref="Exception"/> if it fails</param>
        /// <returns></returns>
        /// <exception cref="Exception">If <paramref name="throwOnFail"/> is true, this will throw an exception for you to catch later</exception>
        public static bool ValidateBlocksInTech(this RawTechTemplate templateToCheck, 
            bool removeInvalid, bool throwOnFail)
        {
            try
            {
                RawTechBase.JSONToMemoryExternal(nonAlloc, templateToCheck.savedTech);

                bool valid = ValidateBlocksInTech_Internal(nonAlloc, templateToCheck, removeInvalid, throwOnFail);

                // Rebuild in workable format
                templateToCheck.savedTech = RawTechBase.MemoryToJSONExternal(nonAlloc);

                return valid;
            }
            catch (Exception e)
            {
                ThrowOnFail_Internal(templateToCheck, e, throwOnFail);
                return false;
            }
            finally
            {
                nonAlloc.Clear();
            }
        }


        internal static void ThrowOnFail_Internal(RawTechBase templateToCheck, Exception e, bool throwOnFail)
        {
            if (throwOnFail)
            {
                try
                {
                    throw new Exception("RawTech: ValidateBlocksInTech - Tech " + templateToCheck.techName + " is invalid!", e);
                }
                catch (Exception)
                {
                    throw new Exception("RawTech: ValidateBlocksInTech - Tech <?NULL?> is invalid!", e);
                }
            }
            else
            {
                try
                {
                    Debug_TTExt.Log("RawTech: ValidateBlocksInTech - Tech " + templateToCheck.techName + " is invalid! - " + e);
                }
                catch (Exception)
                {
                    Debug_TTExt.Log("RawTech: ValidateBlocksInTech - Tech <?NULL?> is invalid! - " + e);
                }
            }
        }
        internal static bool ValidateBlocksInTech_Internal(List<RawBlockMem> nonAlloc, RawTechBase templateToCheck, 
            bool removeInvalid, bool throwOnFail)
        {
            ManMods MM = Singleton.Manager<ManMods>.inst;
            RecipeManager RM = Singleton.Manager<RecipeManager>.inst;
            ManSpawn MS = Singleton.Manager<ManSpawn>.inst;
            bool valid = true;
            FactionLevel greatestFaction = FactionLevel.GSO;
            if (!nonAlloc.Any())
            {
                Debug_TTExt.Log("RawTech: ValidateBlocksInTech - FAILED as no blocks were present!");
                return false;
            }
            int basePrice = 0;
            for (int step = 0; step < nonAlloc.Count;)
            {
                RawBlockMem bloc = nonAlloc[step];
                try
                {
                    if (!BlockIndexer.StringToBlockType(bloc.t, out BlockTypes type))
                        throw new NullReferenceException("Block does not exists - \nBlockName: " +
                            (bloc.t.NullOrEmpty() ? "<NULL>" : bloc.t));
                    if (!MM.IsModdedBlock(type) && !MS.IsTankBlockLoaded(type))
                        throw new NullReferenceException("Block is not loaded - \nBlockName: " +
                            (bloc.t.NullOrEmpty() ? "<NULL>" : bloc.t));

                    FactionSubTypes FST = MS.GetCorporation(type);
                    FactionLevel FL = GetFactionLevel(FST);
                    if (FL >= FactionLevel.ALL)
                    {
                        try
                        {
                            if (MM.IsModdedCorp(FST))
                            {
                                ModdedCorpDefinition MCD = MM.GetCorpDefinition(FST);
                                if (Enum.TryParse(MCD.m_RewardCorp, out FactionSubTypes FST2))
                                    FST = FST2;
                                else
                                    throw new InvalidOperationException("Block with invalid m_RewardCorp - \nBlockType: " + type.ToString());
                            }
                            else
                                throw new InvalidOperationException("Block with invalid corp - \nCorp Level: " + FL.ToString() + " \nBlockType: " + type.ToString());
                        }
                        catch (InvalidOperationException e)
                        {
                            throw e;
                        }
                        catch (Exception)
                        {
                            throw new Exception("Block with invalid data - \nBlockType: <?NULL?>");
                        }
                    }
                    if (greatestFaction < FL)
                        greatestFaction = FL;
                    basePrice += RM.GetBlockBuyPrice(type);
                    bloc.t = MS.GetBlockPrefab(type).name;
                    step++;
                }
                catch (Exception e)
                {
                    if (removeInvalid)
                        nonAlloc.RemoveAt(step);
                    else
                        throw e;
                }
            }
            templateToCheck.baseCost = basePrice;
            templateToCheck.factionLim = greatestFaction;
            templateToCheck.blockCount = nonAlloc.Count;

            return valid;
        }

#endif
    }
#if !EDITOR
    /// <summary>
    /// A datatype to represent blocks at their rawest form - just positioning and type
    /// </summary>
    public interface RawBlockBase
    {
        /// <summary>
        /// <see cref="BlockTypes"/> in string convention form.
        /// <para>See <see cref="BlockIndexer.StringToBlockType(string)"/> for the conversion.</para>
        /// </summary>
        string tg { get; }
        /// <summary>
        /// Position of the block on the Tech
        /// </summary>
        Vector3 pg { get; }
        /// <summary>
        /// Rotation of the block on the Tech
        /// </summary>
        OrthoRotation.r rg { get; }

        /// <summary>
        /// Copy from another <see cref="RawBlockBase"/>
        /// </summary>
        /// <param name="blockBase">Copy from target</param>
        void CopyFrom(RawBlockBase blockBase);

        /// <summary>
        /// Get the <see cref="BlockTypes"/> from this
        /// </summary>
        BlockTypes typeSlow { get; }
        /// <summary>
        /// Get the <see cref="TankBlock"/> <b>prefab</b> from this
        /// </summary>
        TankBlock instSlow { get; }

        /// <summary>
        /// Gets rid of floating point errors which may mess with block loading
        /// </summary>
        void TidyUp();

        /// <summary>
        /// True if the block is loaded.  Does not check for other matters like if it is visible in inventory
        /// </summary>
        /// <returns>True if the block is loaded in <see cref="ManSpawn"/>.</returns>
        bool IsValid();

        /// <summary>
        /// True if the block is loaded.  Does not check for other matters like if it is visible in inventory.
        /// <para><b>Throws <see cref="NullReferenceException"/> if the block isn't present!</b></para>
        /// </summary>
        /// <returns>True if the block is loaded in <see cref="ManSpawn"/>.</returns>
        /// <exception cref="NullReferenceException"></exception>
        bool IsValidException();
    }

    /// <summary>
    /// <inheritdoc cref="RawBlockBase"/>
    /// <para>This version stores it in a class, which is more memory heavy but transferrable and serializable.</para>
    /// <para><b>For the struct, see <see cref="RawBlock"/></b></para>
    /// </summary>
    [Serializable]
    public class RawBlockMem : RawBlockBase
    {   // Save the blocks!
        /// <summary>
        /// Get an empty variant of this quickly.
        /// <para>DO NOT ALTER THIS</para>
        /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
        public static readonly RawBlockMem empty = new RawBlockMem()
        {
            t = BlockTypes.GSOAIController_111.ToString(),
            p = Vector3.zero,
            r = OrthoRotation.r.u000,
        };
#pragma warning restore CS0618 // Type or member is obsolete

        /// <inheritdoc cref="tg"/>
        public string t = BlockTypes.GSOAIController_111.ToString(); //blocktype
        /// <inheritdoc cref="pg"/>
        public Vector3 p = Vector3.zero;
        /// <inheritdoc cref="rg"/>
        public OrthoRotation.r r = OrthoRotation.r.u000;
        /// <inheritdoc/>
        public string tg => t;
        /// <inheritdoc/>
        public Vector3 pg => p;
        /// <inheritdoc/>
        public OrthoRotation.r rg => r;

        /// <inheritdoc/>
        public void CopyFrom(RawBlockBase RBB)
        {
            t = RBB.tg;
            p = RBB.pg;
            r = RBB.rg;
        }

        /// <inheritdoc/>
        public BlockTypes typeSlow => BlockIndexer.StringToBlockType(t);
        /// <inheritdoc/>
        public TankBlock instSlow => ManSpawn.inst.GetBlockPrefab(typeSlow);

        /// <inheritdoc/>
        public void TidyUp()
        {
            p.x = Mathf.RoundToInt(p.x);
            p.y = Mathf.RoundToInt(p.y);
            p.z = Mathf.RoundToInt(p.z);
        }
        /// <inheritdoc/>
        public bool IsValid()
        {
            return Singleton.Manager<ManSpawn>.inst.IsTankBlockLoaded(typeSlow);
        }
        /// <inheritdoc/>
        public bool IsValidException()
        {
            if (!Singleton.Manager<ManSpawn>.inst.IsTankBlockLoaded(typeSlow))
                throw new NullReferenceException();
            return true;
        }


        /// <summary>
        /// <b>SERIALIZATION ONLY</b>
        /// </summary>
        [Obsolete("Only for SERIALIZATION")]
        public RawBlockMem()
        {
        }
        /// <summary>
        /// Creates a <see cref="RawBlockMem"/>
        /// </summary>
        /// <param name="BT">The block type of this session.  
        /// <para>Will be automatically converted to cross-session compatable serial</para></param>
        /// <exception cref="NullReferenceException"></exception>
        public RawBlockMem(BlockTypes BT)
        {
            var inst = ManSpawn.inst.GetBlockPrefab(BT);
            if (inst == null)
                throw new NullReferenceException("Block type " + BT.ToString() + " does not exist");
            t = inst.name;
        }
        /// <summary>
        /// Creates a <see cref="RawBlockMem"/> relative to a Tech's space
        /// </summary>
        /// <param name="BT">The block type of this session.  
        /// <para>Will be automatically converted to cross-session compatable serial</para></param>
        /// <param name="position">Position relative to Tech space</param>
        /// <exception cref="NullReferenceException"></exception>
        public RawBlockMem(BlockTypes BT, Vector3 position) : this(BT)
        {
            p = position;
            TidyUp();
        }
        /// <summary>
        /// Creates a <see cref="RawBlockMem"/> relative to a Tech's space with rotation
        /// <para><b>DOES NOT CHECK FOR BLOCK ON CREATION</b></para>
        /// </summary>
        /// <param name="blockString">The cross-session compatable block type serial</param>
        /// <param name="position">Position relative to Tech space</param>
        /// <param name="rotation">Rotation relative to Tech space</param>
        /// <exception cref="NullReferenceException"></exception>
        public RawBlockMem(string blockString, Vector3 position, OrthoRotation.r rotation)
        {
            t = blockString;
            p = position;
            r = rotation;
            TidyUp();
        }
        /// <summary>
        /// Creates a <see cref="RawBlockMem"/> relative to a Tech's space with rotation
        /// </summary>
        /// <param name="BT">The block type of this session.  
        /// <para>Will be automatically converted to cross-session compatable serial</para></param>
        /// <param name="position">Position relative to Tech space</param>
        /// <param name="rotation">Rotation relative to Tech space</param>
        /// <exception cref="NullReferenceException"></exception>
        public RawBlockMem(BlockTypes BT, Vector3 position, OrthoRotation.r rotation) : this(BT)
        {
            p = position;
            r = rotation;
            TidyUp();
        }
        /// <summary>
        /// Creates a <see cref="RawBlockMem"/> relative to a Tech's space with rotation
        /// </summary>
        /// <param name="BT">The block type of this session.  
        /// <para>Will be automatically converted to cross-session compatable serial</para></param>
        /// <param name="position">Position relative to Tech space</param>
        /// <param name="rotation">Rotation relative to Tech space</param>
        /// <exception cref="NullReferenceException"></exception>
        public RawBlockMem(BlockTypes BT, Vector3 position, OrthoRotation rotation) : this(BT)
        {
            p = position;
            r = rotation.rot;
            TidyUp();
        }
        /// <summary>
        /// Creates a <see cref="RawBlockMem"/> relative to a Tech's space with rotation
        /// </summary>
        /// <param name="BT">The block type of this session.  
        /// <para>Will be automatically converted to cross-session compatable serial</para></param>
        /// <param name="position">Position relative to Tech space</param>
        /// <param name="rotation">Rotation relative to Tech space</param>
        /// <exception cref="NullReferenceException"></exception>
        public RawBlockMem(BlockTypes BT, Vector3 position, Quaternion rotation) : this(BT)
        {
            p = position;
            r = new OrthoRotation(rotation).rot;
            TidyUp();
        }
    }

    /// <summary>
    /// <inheritdoc cref="RawBlockBase"/>
    /// <para>This version stores it in a struct, which is less memory heavy but not transferrable and serializable.</para>
    /// <para><b>For the class, see <see cref="RawBlockMem"/></b></para>
    /// <para><b>STRUCT! <i>USE WITH CARE</i></b></para>
    /// </summary>
    public struct RawBlock : RawBlockBase
    {
        /// <summary>
        /// Get an empty variant of this quickly
        /// </summary>
        public static RawBlock empty => new RawBlock(BlockTypes.GSOAIController_111,
            Vector3.zero, OrthoRotation.r.u000);

        /// <inheritdoc cref="tg"/>
        public string t; //blocktype
        /// <inheritdoc cref="pg"/>
        public Vector3 p;
        /// <inheritdoc cref="rg"/>
        public OrthoRotation.r r;
        /// <inheritdoc/>
        public string tg => t;
        /// <inheritdoc/>
        public Vector3 pg => p;
        /// <inheritdoc/>
        public OrthoRotation.r rg => r;

        /// <inheritdoc/>
        public void CopyFrom(RawBlockBase RBB)
        {
            t = RBB.tg;
            p = RBB.pg;
            r = RBB.rg;
        }

        /// <inheritdoc/>
        public BlockTypes typeSlow => BlockIndexer.StringToBlockType(t);
        /// <inheritdoc/>
        public TankBlock instSlow => ManSpawn.inst.GetBlockPrefab(typeSlow);

        /// <inheritdoc/>
        public void TidyUp()
        {
            p.x = Mathf.RoundToInt(p.x);
            p.y = Mathf.RoundToInt(p.y);
            p.z = Mathf.RoundToInt(p.z);
        }
        /// <inheritdoc/>
        public bool IsValid()
        {
            return Singleton.Manager<ManSpawn>.inst.IsTankBlockLoaded(typeSlow);
        }
        /// <inheritdoc/>
        public bool IsValidException()
        {
            if (!Singleton.Manager<ManSpawn>.inst.IsTankBlockLoaded(typeSlow))
                throw new NullReferenceException();
            return true;
        }

        /// <summary>
        /// Creates a <see cref="RawBlock"/>
        /// </summary>
        /// <param name="BT"></param>
        /// <exception cref="NullReferenceException"></exception>
        public RawBlock(BlockTypes BT)
        {
            var inst = ManSpawn.inst.GetBlockPrefab(BT);
            if (inst == null)
                throw new NullReferenceException("Block type " + BT.ToString() + " does not exist");
            t = inst.name;
            p = Vector3.zero;
            r = OrthoRotation.r.u000;
        }
        /// <summary>
        /// Creates a <see cref="RawBlock"/> relative to a Tech's space
        /// </summary>
        /// <param name="BT">The block type of this session.  
        /// <para>Will be automatically converted to cross-session compatable serial</para></param>
        /// <param name="position">Position relative to Tech space</param>
        /// <exception cref="NullReferenceException"></exception>
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
        /// <summary>
        /// Creates a <see cref="RawBlock"/> relative to a Tech's space with rotation
        /// </summary>
        /// <param name="BT">The block type of this session.  
        /// <para>Will be automatically converted to cross-session compatable serial</para></param>
        /// <param name="position">Position relative to Tech space</param>
        /// <param name="rotation">Rotation relative to Tech space</param>
        /// <exception cref="NullReferenceException"></exception>
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
        /// <summary>
        /// Creates a <see cref="RawBlock"/> relative to a Tech's space with rotation
        /// </summary>
        /// <param name="BT">The block type of this session.  
        /// <para>Will be automatically converted to cross-session compatable serial</para></param>
        /// <param name="position">Position relative to Tech space</param>
        /// <param name="rotation">Rotation relative to Tech space</param>
        /// <exception cref="NullReferenceException"></exception>
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
        /// <summary>
        /// Creates a <see cref="RawBlock"/> relative to a Tech's space with rotation
        /// </summary>
        /// <param name="BT">The block type of this session.  
        /// <para>Will be automatically converted to cross-session compatable serial</para></param>
        /// <param name="position">Position relative to Tech space</param>
        /// <param name="rotation">Rotation relative to Tech space</param>
        /// <exception cref="NullReferenceException"></exception>
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
    /// <summary>
    /// Request a Tech to anchor
    /// </summary>
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

    /// <summary>
    /// Extended <b>FactionSubTypes</b>.  Mostly obsolete.
    /// </summary>
    public enum FactionTypesExt
    {
        // No I do not make any of the corps below (exclusing some of TAC and EFF) 
        //  - but these are needed to allow the AI to spawn the right bases with 
        //    the right block ranges
        // OFFICIAL
        /// <summary> not a corp, really, probably the most unique of all lol </summary>
        NULL,
        /// <summary> Galactic Survey Organization </summary>
        GSO,
        /// <summary> GeoCorp </summary>
        GC,
        /// <summary> Reticule Research </summary>
        EXP,
        /// <summary> VENture </summary>
        VEN,
        /// <summary> HawkEye </summary>
        HE,
        /// <summary> Special </summary>
        SPE,
        /// <summary> Better Future </summary>
        BF,
        /// <summary> Space Junkers </summary>
        SJ,
        /// <summary> Legion/Enclave </summary>
        LEG,

        // Below is currently mostly unused as Custom Corps already address this.
        // Community
        /// <summary> Aerion </summary>
        AER = 256,
        /// <summary> Black Labs (EXT OF HE) </summary>
        BL = 257,
        /// <summary> CrystalCorp </summary>
        CC = 258,
        /// <summary> DEVCorp </summary>
        DC = 259,
        /// <summary> DarkLight </summary>
        DL = 260,
        /// <summary> Ellydium </summary>
        EYM = 261,
        /// <summary> GreenTech </summary>
        GT = 262,
        /// <summary> Hyperion Systems </summary>
        HS = 263,
        /// <summary> Independant Earthern Colonies </summary>
        IEC = 264,
        /// <summary> Lemon Kingdom </summary>
        LK = 265,
        /// <summary> Old Stars </summary>
        OS = 266,
        /// <summary> Tofuu Corp </summary>
        TC = 267,
        /// <summary> Technocratic AI Colony </summary>
        TAC = 268,

        // idk
        /// <summary> Emperical Forge Fabrication </summary>
        EFF = 269,
        /// <summary> Mechaniccoid Cooperative Confederacy </summary>
        MCC = 270,
        /// <summary> BuLwark Nation (Bulin) </summary>
        BLN = 271,
        /// <summary> ClaNg Clads (ChanClas) </summary>
        CNC = 272,
        /// <summary> Larry's Overlord Laser </summary>
        LOL = 273,
    }

    /// <summary>
    /// Ordered enum in relation to corp progression to sort spawns
    /// <para>ONLY SUPPORTS RANKING OF FactionSubTypes (No modded corps!)</para>
    /// </summary>
    public enum FactionLevel
    {
        /// <summary> </summary>
        NULL,
        /// <summary> </summary>
        GSO,
        /// <summary> </summary>
        GC,
        /// <summary> </summary>
        VEN,
        /// <summary> </summary>
        HE,
        /// <summary> </summary>
        BF,
        /// <summary> </summary>
        SJ,
        /// <summary> </summary>
        EXP,
        /// <summary> </summary>
        ALL,
        /// <summary> All modded corporations </summary>
        MOD,
    }
    /// <summary>
    /// Unused!
    /// </summary>
    public enum BaseTypeLevel
    {
        /// <summary> basic base </summary>
        Basic,
        /// <summary> </summary>
        Advanced,
        /// <summary> </summary>
        Headquarters,
        /// <summary> </summary>
        Overkill,
        /// <summary> </summary>
        InvaderSpecific,
    }
    /// <summary>
    /// the terrain restriction Techs should only spawn on
    /// </summary>
    public enum BaseTerrain
    {
        /// <summary> anywhere </summary>
        Any,
        /// <summary> anywhere but not the sea </summary>
        AnyNonSea,
        /// <summary> On land </summary>
        Land,   // is anchored
        /// <summary> In the sea </summary>
        Sea,    // floats on water
        /// <summary> In the air </summary>
        Air,    // doubles as airplane
        /// <summary> Needs extra clearance overhead </summary>
        Chopper,// relies on props to stay airborne
        /// <summary> Needs access to space </summary>
        Space   // mobile base that flies beyond
    }
    /// <summary>
    /// special tag(s) that tells their use for spawning Techs
    /// </summary>
    public enum BasePurpose
    {
        /// <summary> Any base that's not an HQ </summary>
        AnyNonHQ,
        /// <summary> Any harvesting base that's not an HQ </summary>
        HarvestingNoHQ,
        /// <summary> Strictly defensive base element </summary>
        Defense,
        /// <summary> Has Delivery cannons  </summary>
        Harvesting,
        /// <summary> Can mine unlimited BB (DO NOT ATTACH THIS TAG TO HQs!!!) </summary>
        Autominer,
        /// <summary> Base with Explosive Bolts attached 
        /// <para>As of Update 1.8.3, this is now considered a Garrison</para></summary>
        TechProduction,

        /// <summary> Calls in techs from orbit using funds </summary>
        Headquarters,
        /// <summary> MP blocked crafting blocks </summary>
        MPUnsafe,
        /// <summary> Has receivers </summary>
        HasReceivers,
        /// <summary> Mobile Tech </summary>
        NotStationary,
        /// <summary> Reserved for Attract (or an endgame spawn) </summary>
        AttractTech,
        /// <summary> unarmed </summary>
        NoWeapons,
        /// <summary> ran out of other options to spawn </summary>
        Fallback,
        /// <summary> Lock to harder difficulties </summary>
        Sniper,
        /// <summary> Incomprehensibly powerful Tech spawn </summary>
        NANI,
    }
}
