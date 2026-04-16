using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TerraTechETCUtil
{
#if !EDITOR
    /// <summary>
    /// Collision checker for RawTechs for adding blocks without actually loading them into the world
    /// </summary>
    public class RawTechCollisionMatrix
    {
        private int cells = 0;
        private Vector3 largestBlock = Vector3.one;
        private Vector3 CollisionOverlay = Vector3.one;
        private RawTech baseInst = null;

        private List<RawBlockMem> baseArray => baseInst.savedTech;
        /// <summary>
        /// The blocktable for building the tech
        /// </summary>
        public Dictionary<IntVector3, RawBlockMem> blockTable = new Dictionary<IntVector3, RawBlockMem>();

        /// <summary>
        /// Cell count
        /// </summary>
        public int Cells => cells;

        /// <summary>
        /// Generate a collision matrix from a <see cref="RawTech"/>
        /// </summary>
        /// <param name="blocks"></param>
        public RawTechCollisionMatrix(RawTech blocks)
        {
            baseInst = blocks;
            foreach (var item in baseArray)
            {
                var inst = item.instSlow;
                largestBlock = Vector3.Max(largestBlock, inst.BlockCellBounds.size);
                cells += inst.filledCells.Length;
            }
            largestBlock.x = Mathf.Max(largestBlock.x, largestBlock.y, largestBlock.x);
            largestBlock.y = largestBlock.x;
            largestBlock.z = largestBlock.x;
        }
        /// <summary>
        /// 
        /// </summary>
        public static Dictionary<IntVector3, RawBlockMem> posCached = new Dictionary<IntVector3, RawBlockMem>();

        /// <summary>
        /// Rebuild the collision overlay
        /// </summary>
        public void RegenerateCollisionOverlay()
        {
            blockTable.Clear();

            for (int step = 0; step < baseArray.Count;)
            {
                RawBlockMem item = baseArray[step];
                foreach (var item2 in item.instSlow.filledCells)
                {
                    IntVector3 posVec = item.p + new OrthoRotation(item.r) * item2;
                    if (blockTable.ContainsKey(posVec))
                    {
                        posCached.Clear();
                        baseArray.RemoveAt(step);
                        break;
                    }
                    posCached.Add(posVec, item);
                }
                foreach (var item2 in posCached)
                {
                    blockTable.Add(item2.Key, item2.Value);
                }
                posCached.Clear();
                step++;
            }
        }

        /// <summary>
        /// Add a block to this. Does not check for AP connections!
        /// </summary>
        /// <param name="item"></param>
        /// <param name="actuallyAdd"></param>
        /// <returns>True if it fits</returns>
        public bool TryAdd(RawBlockMem item, bool actuallyAdd = true)
        {
            try
            {
                foreach (var item2 in item.instSlow.filledCells)
                {
                    IntVector3 posVec = item.p + new OrthoRotation(item.r) * item2;
                    if (blockTable.ContainsKey(posVec))
                    {
                        posCached.Clear();
                        break;
                    }
                    posCached.Add(posVec, item);
                }
                if (actuallyAdd && posCached.Any())
                {
                    foreach (var item2 in posCached)
                    {
                        blockTable.Add(item2.Key, item2.Value);
                    }
                    return true;
                }
                return false;
            }
            finally
            {
                posCached.Clear();
            }
        }
        /// <summary>
        /// Add a block to this. Does not check for AP connections!
        /// </summary>
        /// <param name="item"></param>
        /// <param name="actuallyAdd"></param>
        /// <param name="pos"></param>
        /// <param name="rot"></param>
        /// <returns>True if it fits</returns>
        public bool TryAdd(BlockTypes item, Vector3 pos, Quaternion rot, bool actuallyAdd = false)
        {
            return TryAdd(new RawBlockMem(item, pos, rot), actuallyAdd);
        }
    }
#endif

    /// <summary>
    /// <inheritdoc/>
    /// <para>This is the ACTIVE <see cref="RawTechBase"/>. For the serialized version see <see cref="RawTechTemplate"/></para>
    /// </summary>
    public class RawTech : RawTechBase
    {
#if !EDITOR
        /// <summary>
        /// Ignore validations checks.  Does not work in all cases
        /// </summary>
        public bool IgnoreChecks = false;
        private bool dirty = true;
        private List<RawBlockMem> _savedTech = null;
        /// <summary>
        /// Optional for bounds checks
        /// </summary>
        private RawTechCollisionMatrix matrix = null;
        /// <summary>
        /// The saved tech in <see cref="RawBlockMem"/> format
        /// </summary>
        public List<RawBlockMem> savedTech
        {
            get
            {
                dirty = true;
                return _savedTech;
            }
            set
            {
                if (value == null)
                    throw new NullReferenceException("RawTech set with null RawBlockMems");
                if (!value.Any())
                    throw new NullReferenceException("RawTech set with empty RawBlockMems");
                dirty = true;
                _savedTech = value;
            }
        }
        /// <summary>
        /// Get the BB cost of this
        /// </summary>
        /// <param name="BT"></param>
        public static implicit operator int(RawTech BT)
        {
            if (BT.baseCost == 0 && BT.savedTech.Any())
                BT.baseCost = GetBBCost(BT.savedTech);
            return BT.baseCost;
        }

        /// <summary>
        /// Creates empty
        /// </summary>
        public RawTech()
        {
            _savedTech = new List<RawBlockMem>();
            serialDataBlock = new Dictionary<int, List<string>>();
            //matrix = new RawTechCollisionMatrix(this);
        }
        /// <summary>
        /// Create from <see cref="TechData"/>
        /// </summary>
        /// <param name="tech"></param>
        /// <param name="MustBeExact"></param>
        public RawTech(TechData tech, bool MustBeExact = false)
        {
            techName = tech.Name;
            _savedTech = new List<RawBlockMem>();
            _savedTech = TechToMemoryExternal(tech, MustBeExact);
            serialDataBlock = EncodeSerialData(tech);
            //matrix = new RawTechCollisionMatrix(this);
            InsureValid(!MustBeExact, MustBeExact);
        }
        /// <summary>
        /// Create from <see cref="RawBlockMem"/> list
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="mems"></param>
        /// <param name="MustBeExact"></param>
        /// <exception cref="NullReferenceException"></exception>
        public RawTech(string Name, List<RawBlockMem> mems, bool MustBeExact = false)
        {
            techName = Name;
            _savedTech = mems;
            serialDataBlock = new Dictionary<int, List<string>>();
            if (_savedTech == null)
                throw new NullReferenceException("RawTech created with null mems parameter");
            if (!_savedTech.Any())
                throw new NullReferenceException("RawTech created with empty mems parameter");
            //matrix = new RawTechCollisionMatrix(this);
            InsureValid(!MustBeExact, MustBeExact);
        }

        /// <summary>
        /// Create from <see cref="RawTech"/> JSON string
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="rawTech"></param>
        /// <param name="MustBeExact"></param>
        public RawTech(string Name, string rawTech, bool MustBeExact = false)
        {
            techName = Name;
            _savedTech = JSONToMemoryExternal(rawTech);
            serialDataBlock = new Dictionary<int, List<string>>();
            //matrix = new RawTechCollisionMatrix(this);

            InsureValid(!MustBeExact, MustBeExact);
        }
        /// <summary>
        /// Create from <see cref="RawTechTemplate"/>
        /// </summary>
        /// <param name="tech">Copy from target</param>
        public RawTech(RawTechTemplate tech) : base(tech)
        {
            techName = tech.techName;
            //faction = tech.faction;
            factionName = tech.FactionActual;
            baseCost = tech.baseCost;
            blockCount = tech.blockCount;
            deployBoltsASAP = tech.deployBoltsASAP;
            factionName = tech.factionName;
            factionLim = tech.factionLim;
            environ = tech.environ;
            IntendedGrade = tech.IntendedGrade;
            serialDataBlock = tech.serialDataBlock;
            serialDataTech = tech.serialDataTech;
            terrain = tech.terrain;
            purposes = new HashSet<BasePurpose>(tech.purposes);
            _savedTech = JSONToMemoryExternal(tech.savedTech);
            //matrix = new RawTechCollisionMatrix(this);
        }
        /// <summary>
        /// Convert this to a template for community spawning
        /// </summary>
        /// <returns></returns>
        public RawTechTemplate ToTemplate()
        {
            return new RawTechTemplate(this);
        }

        /// <summary>
        /// Get the first block on this
        /// </summary>
        /// <returns>The first block if found, otherwise fallback invalid <see cref="BlockTypes.GSOAIController_111"/></returns>
        public BlockTypes GetFirstBlock() => RawTechUtil.GetFirstBlock(savedTech);
        /// <summary>
        /// Replace a type with another
        /// </summary>
        /// <param name="prev"></param>
        /// <param name="newSet"></param>
        public void Re_Referencing(BlockTypes prev, BlockTypes newSet)
        {
            for (int step = 0; step < _savedTech.Count; step++)
            {
                var item = _savedTech[step];
                if (item.typeSlow == prev)
                    item.t = new RawBlock(newSet).t;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="posOnTech"></param>
        /// <param name="rotation"></param>
        /// <param name="SpamLog"></param>
        /// <returns></returns>
        public bool TryAddBlock(BlockTypes type, Vector3 posOnTech, Quaternion rotation, bool SpamLog = false)
        {
            RawBlockMem RB = new RawBlockMem(type, posOnTech, rotation);

            if (SpamLog)
            {
                if (!RB.IsValid())
                {
                    Debug_TTExt.Log("Failed to attach " + RB.typeSlow.ToString() + " at " + RB.p + " because block invalid");
                    return false;
                }
                if (matrix == null)
                    matrix = new RawTechCollisionMatrix(this);
                if (!matrix.TryAdd(RB))
                {
                    Debug_TTExt.Log("Failed to attach " + RB.typeSlow.ToString() + " at " + RB.p + " because other block occupies cell");
                    return false;
                }
                _savedTech.Add(RB);
                return true;
            }
            else
            {
                if (matrix == null)
                    matrix = new RawTechCollisionMatrix(this);
                if (RB.IsValid() && matrix.TryAdd(RB))
                {
                    _savedTech.Add(RB);
                    return true;
                }
                else
                    return false;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="posOnTech"></param>
        /// <param name="rotation"></param>
        /// <returns></returns>
        public bool AddBlock(BlockTypes type, Vector3 posOnTech, Quaternion rotation)
        {
            RawBlockMem RB= new RawBlockMem(type, posOnTech, rotation);
            if (RB.IsValid())
            {
                _savedTech.Add(RB);
                dirty = true;
                return true;
            }
            else
                return false;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="posOnTech"></param>
        /// <param name="rotation"></param>
        /// <param name="RB"></param>
        /// <returns></returns>
        public bool AddBlock(BlockTypes type, Vector3 posOnTech, Quaternion rotation, out RawBlockMem RB)
        {
            RB = new RawBlockMem(type, posOnTech, rotation);
            if (RB.IsValid())
            {
                _savedTech.Add(RB);
                dirty = true;
                return true;
            }
            else
                return false;
        }

        /// <inheritdoc/>
        public override Tank SpawnRawTech(Vector3 pos, int Team, Vector3 forwards, bool snapTerrain = false, 
            bool Charged = false, bool randomSkins = false, bool CanBeIncomplete = true)
        {
            if (savedTech == null)
                throw new NullReferenceException("TTExtUtil: SpawnTechExternal - Was handed a NULL Blueprint!");

            try
            {
                InsureValid(CanBeIncomplete, true);

                Tank theTech = InstantTech(pos, forwards, Team, techName, savedTech, serialDataBlock, snapTerrain, ForceAnchor: false, Team == -1, randomSkins);
                Debug_TTExt.Log("TTExtUtil: SpawnTechExternal - Spawned " + techName + " at " + pos + ". Snapped to terrain " + snapTerrain);

                if (Team == -2)//neutral - be crafty mike and face the player
                    theTech.AI.SetBehaviorType(AITreeType.AITypes.FacePlayer);
                if (Charged)
                    ChargeAndClean(theTech);

                //InvokeHelper.Invoke(() => { DecodeSerialData(theTech); }, 0.1f);

                return theTech;
            }
            catch (Exception e)
            {
                throw new Exception("Tech " + techName + " failed to spawn", e);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="removeInvalid"></param>
        /// <param name="throwOnFail"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public void InsureValid(bool removeInvalid, bool throwOnFail)
        {
            if (dirty)
            {
                if (!ValidateBlocksInTech(removeInvalid, throwOnFail))
                    throw new InvalidOperationException("SpawnRawTech failed because tech was invalid!");
            }
        }

        /// <summary>
        /// Checks all of the blocks in a <see cref="RawTech"/> to make sure it's safe to spawn as well as calculate other requirements for it.
        /// </summary>
        /// <param name="removeInvalid">Set this to true to remove invalid blocks, 
        /// otherwise it will stop loading the tech as soon as an invalid block is encountered</param>
        /// <param name="throwOnFail">Make this throw an <see cref="Exception"/> if it fails</param>
        /// <returns></returns>
        /// <exception cref="Exception">If <paramref name="throwOnFail"/> is true, this will throw an exception for you to catch later</exception>
        public bool ValidateBlocksInTech(bool removeInvalid, bool throwOnFail)
        {
            try
            {
                dirty = false;
                if (IgnoreChecks)
                    return true;

                RawTechUtil.ValidateBlocksInTech_Internal(_savedTech, this, removeInvalid, throwOnFail);

                if (!_savedTech.Any())
                {
                    Debug_TTExt.Log("RawTech: ValidateBlocksInTech - FAILED as no blocks were present afterwards!");
                    return false;
                }
                matrix?.RegenerateCollisionOverlay();
            }
            catch (Exception e)
            {
                RawTechUtil.ThrowOnFail_Internal(this, e, throwOnFail);
                return false;
            }
            return true;
        }
#endif
    }
}
