using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TerraTechETCUtil
{
#if !EDITOR
    public class RawTechCollisionMatrix
    {
        private int cells = 0;
        private Vector3 largestBlock = Vector3.one;
        private Vector3 CollisionOverlay = Vector3.one;
        private RawTech baseInst = null;

        private List<RawBlockMem> baseArray => baseInst.savedTech;
        public Dictionary<IntVector3, RawBlockMem> blockTable = new Dictionary<IntVector3, RawBlockMem>();

        public int Cells => cells;


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
        public static Dictionary<IntVector3, RawBlockMem> posCached = new Dictionary<IntVector3, RawBlockMem>();
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
        public bool TryAdd(BlockTypes item, Vector3 pos, Quaternion rot, bool actuallyAdd = false)
        {
            return TryAdd(new RawBlockMem(item, pos, rot), actuallyAdd);
        }
    }
#endif

    /// <summary>
    /// the ACTIVE rawtech
    /// </summary>
    public class RawTech : RawTechBase
    {
#if !EDITOR
        public bool IgnoreChecks = false;
        private bool dirty = true;
        private List<RawBlockMem> _savedTech = null;
        private RawTechCollisionMatrix matrix = null;
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

        public static implicit operator int(RawTech BT)
        {
            if (BT.baseCost == 0 && BT.savedTech.Any())
                BT.baseCost = GetBBCost(BT.savedTech);
            return BT.baseCost;
        }

        public RawTech()
        {
            _savedTech = new List<RawBlockMem>();
            serialDataBlock = new Dictionary<int, List<string>>();
            //matrix = new RawTechCollisionMatrix(this);
        }
        public RawTech(TechData tech, bool MustBeExact = false)
        {
            techName = tech.Name;
            _savedTech = new List<RawBlockMem>();
            _savedTech = TechToMemoryExternal(tech, MustBeExact);
            serialDataBlock = EncodeSerialData(tech);
            //matrix = new RawTechCollisionMatrix(this);
            InsureValid(!MustBeExact, MustBeExact);
        }
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

        public RawTech(string Name, string rawTech, bool MustBeExact = false)
        {
            techName = Name;
            _savedTech = JSONToMemoryExternal(rawTech);
            serialDataBlock = new Dictionary<int, List<string>>();
            //matrix = new RawTechCollisionMatrix(this);

            InsureValid(!MustBeExact, MustBeExact);
        }
        public RawTech(RawTechTemplate tech) : base(tech)
        {
            techName = tech.techName;
            faction = tech.faction;
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
        public RawTechTemplate ToTemplate()
        {
            return new RawTechTemplate(this);
        }

        public BlockTypes GetFirstBlock()
        {
            if (savedTech.Any())
                return savedTech.First().typeSlow;
            return BlockTypes.GSOAIController_111;
        }
        public void Re_Referencing(BlockTypes prev, BlockTypes newSet)
        {
            for (int step = 0; step < _savedTech.Count; step++)
            {
                var item = _savedTech[step];
                if (item.typeSlow == prev)
                    item.t = new RawBlock(newSet).t;
            }
        }
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

        public void InsureValid(bool removeInvalid, bool throwOnFail)
        {
            if (dirty)
            {
                if (!ValidateBlocksInTech(removeInvalid, throwOnFail))
                    throw new InvalidOperationException("SpawnRawTech failed because tech was invalid!");
            }
        }

        /// <summary>
        /// Checks all of the blocks in a Raw Tech to make sure it's safe to spawn as well as calculate other requirements for it.
        /// </summary>
        public bool ValidateBlocksInTech(bool removeInvalid, bool throwOnFail)
        {
            try
            {
                dirty = false;
                if (IgnoreChecks)
                    return true;
                if (!_savedTech.Any())
                {
                    Debug_TTExt.Log("RawTech: ValidateBlocksInTech - FAILED as no blocks were present!");
                    return false;
                }
                for (int step = 0; step < _savedTech.Count;)
                {
                    RawBlockMem bloc = _savedTech[step];
                    try
                    {
                        if (!BlockIndexer.StringToBlockType(bloc.t, out BlockTypes type))
                            throw new NullReferenceException("Block does not exists - \nBlockName: " +
                                (bloc.t.NullOrEmpty() ? "<NULL>" : bloc.t));
                        if (!ManMods.inst.IsModdedBlock(type) && !ManSpawn.inst.IsTankBlockLoaded(type))
                            throw new NullReferenceException("Block is not loaded - \nBlockName: " +
                                (bloc.t.NullOrEmpty() ? "<NULL>" : bloc.t));

                        FactionSubTypes FST = Singleton.Manager<ManSpawn>.inst.GetCorporation(type);
                        FactionLevel FL = RawTechUtil.GetFactionLevel(FST);
                        if (FL >= FactionLevel.ALL)
                        {
                            try
                            {
                                if (ManMods.inst.IsModdedCorp(FST))
                                {
                                    ModdedCorpDefinition MCD = ManMods.inst.GetCorpDefinition(FST);
                                    if (Enum.TryParse(MCD.m_RewardCorp, out FactionSubTypes FST2))
                                    {
                                        FST = FST2;
                                    }
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
                        bloc.t = Singleton.Manager<ManSpawn>.inst.GetBlockPrefab(type).name;
                        step++;
                    }
                    catch (Exception e)
                    {
                        if (removeInvalid)
                            _savedTech.RemoveAt(step);
                        else
                            throw e;
                    }
                }
                if (!_savedTech.Any())
                {
                    Debug_TTExt.Log("RawTech: ValidateBlocksInTech - FAILED as no blocks were present afterwards!");
                    return false;
                }
                if (matrix != null)
                    matrix.RegenerateCollisionOverlay();
            }
            catch (Exception e)
            {
                if (throwOnFail)
                {
                    try
                    {
                        throw new Exception("RawTech: ValidateBlocksInTech - Tech " + techName + " is invalid!", e);
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
                        Debug_TTExt.Log("RawTech: ValidateBlocksInTech - Tech " + techName + " is invalid! - " + e);
                    }
                    catch (Exception)
                    {
                        Debug_TTExt.Log("RawTech: ValidateBlocksInTech - Tech <?NULL?> is invalid! - " + e);
                    }
                }
                return false;
            }
            return true;
        }
#endif
    }
}
