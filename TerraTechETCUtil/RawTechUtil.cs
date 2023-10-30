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
        public static string up = "\\";

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
        /// <param name="toLoad"></param>
        /// <param name="templateToCheck"></param>
        /// <param name="basePrice"></param>
        /// <param name="greatestFaction"></param>
        /// <returns></returns>
        public static bool ValidateBlocksInTech(this RawTechTemplate templateToCheck)
        {
            try
            {
                foreach (char ch in templateToCheck.savedTech)
                {
                    if (ch != up.ToCharArray()[0])
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
    }
    [Serializable]
    public class RawBlockMem
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
    public class RawTechTemplate
    {
        private static readonly FieldInfo forceVal = typeof(BoosterJet).GetField("m_Force", BindingFlags.NonPublic | BindingFlags.Instance);

        public string techName = "!!!!null!!!!";
        public FactionTypesExt faction = FactionTypesExt.GSO;
        /// <summary>
        /// Mostly here to restrict spawns based on best corp in the spawns
        /// </summary>
        public FactionLevel factionLim = FactionLevel.NULL;
        public HashSet<BasePurpose> purposes;
        public int IntendedGrade = -1;
        public BaseTerrain terrain = BaseTerrain.Land;
        public int startingFunds = 5000;
        public int baseCost = 0;
        public int blockCount = 0;
        public string savedTech = "{\"t\":\"GSOAnchorFixed_111\",\"p\":{\"x\":0.0,\"y\":0.0,\"z\":0.0},\"r\":0}";
        public Dictionary<string, string> serialDataTech = null;
        public Dictionary<int, List<string>> serialDataBlock = null;
        public bool environ = false; // are we not a miner?
        public bool deployBoltsASAP = false; // press X on spawn

        public static implicit operator int(RawTechTemplate BT)
        {
            if (BT.baseCost == 0 && !BT.savedTech.NullOrEmpty())
                BT.baseCost = RawTechTemplate.GetBBCost(BT.savedTech);
            return BT.baseCost;
        }

        public RawTechTemplate()
        {
        }
        public RawTechTemplate(TechData tech, bool ExceptionIfFail = false)
        {
            techName = tech.Name;
            faction = GetTopCorp(tech);
            savedTech = MemoryToJSONExternal(TechToMemoryExternal(tech, ExceptionIfFail));
            bool anchored = tech.CheckIsAnchored();
            purposes = GetHandler(savedTech, faction, anchored, out terrain, out IntendedGrade);
            serialDataBlock = EncodeSerialData(tech);

            this.ValidateBlocksInTech();
        }

        public RawTechTemplate(string Name, string rawTech, bool anchored = false)
        {
            techName = Name;
            List<RawBlockMem> mem = JSONToMemoryExternalNonAlloc(rawTech);
            faction = GetTopCorp(mem);
            savedTech = rawTech;
            purposes = GetHandler(mem, faction, anchored, out terrain, out IntendedGrade);
            serialDataBlock = new Dictionary<int, List<string>>();

            this.ValidateBlocksInTech();
        }


        public void SetState(string param, string serial)
        {
            if (serialDataTech == null)
                serialDataTech = new Dictionary<string, string>();
            serialDataTech[param] = serial;
        }
        public string GetState(string param)
        {
            if (serialDataTech != null && serialDataTech.TryGetValue(param, out string val))
                return val;
            return null;
        }
        public void PurgeStates()
        {
            serialDataTech.Clear();
        }


        public static HashSet<BasePurpose> GetHandler(string blueprint, FactionTypesExt factionType, bool Anchored, out BaseTerrain terra, out int minCorpGrade)
        {
            return GetHandler(JSONToMemoryExternalNonAlloc(blueprint), factionType, Anchored, out terra, out minCorpGrade);
        }
        public static HashSet<BasePurpose> GetHandler(List<RawBlockMem> mems, FactionTypesExt factionType, bool Anchored, out BaseTerrain terra, out int minCorpGrade)
        {
            List<TankBlock> blocs = new List<TankBlock>();
            if (mems.Count < 1)
            {
                Debug_TTExt.Log("RawTech: TECH IS NULL!  SKIPPING!");
                minCorpGrade = 99;
                terra = BaseTerrain.AnyNonSea;
                return new HashSet<BasePurpose>();
            }

            HashSet<BasePurpose> purposes = new HashSet<BasePurpose>();
            //foreach (BlockMemory mem in mems)
            //{
            //    blocs.Add(Singleton.Manager<ManSpawn>.inst.GetBlockPrefab((BlockTypes)Enum.Parse(typeof(BlockTypes), mem.t)));
            //}

            bool isFlying = false;
            bool isFlyingDirectionForwards = true;

            Vector3 biasDirection = Vector3.zero;
            Vector3 boostBiasDirection = Vector3.zero;

            int FoilCount = 0;
            int MovingFoilCount = 0;

            int modControlCount = 0;
            int modCollectCount = 0;
            int modEXPLODECount = 0;
            int modBoostCount = 0;
            int modHoverCount = 0;
            int modGyroCount = 0;
            int modWheelCount = 0;
            int modAGCount = 0;
            int modDangerCount = 0;
            int modGunCount = 0;
            int modDrillCount = 0;
            minCorpGrade = 0;
            bool NotMP = false;
            bool hasAutominer = false;
            bool hasReceiver = false;
            bool hasBaseFunction = false;

            BlockUnlockTable blockList = Singleton.Manager<ManLicenses>.inst.GetBlockUnlockTable();
            int gradeM = blockList.GetMaxGrade(RawTechUtil.CorpExtToCorp(factionType));
            //Debug_TTExt.Log("RawTech: GetHandler - " + Singleton.Manager<ManLicenses>.inst.m_UnlockTable.GetAllBlocksInTier(1, factionType, false).Count());
            foreach (RawBlockMem blocRaw in mems)
            {
                BlockTypes type = BlockIndexer.StringToBlockType(blocRaw.t);
                TankBlock bloc = Singleton.Manager<ManSpawn>.inst.GetBlockPrefab(type);
                if (bloc.IsNull())
                    continue;
                ModuleItemPickup rec = bloc.GetComponent<ModuleItemPickup>();
                ModuleItemHolder conv = bloc.GetComponent<ModuleItemHolder>();
                if ((bool)rec && conv && conv.Acceptance.HasFlag(ModuleItemHolder.AcceptFlags.Chunks) && conv.IsFlag(ModuleItemHolder.Flags.Receiver))
                {
                    hasReceiver = true;
                }
                if (bloc.GetComponent<ModuleItemProducer>())
                {
                    hasAutominer = true;
                    NotMP = true;
                }
                if (bloc.GetComponent<ModuleItemConveyor>())
                {
                    NotMP = true;
                }
                if (bloc.GetComponent<ModuleItemConsume>())
                {
                    hasBaseFunction = true;
                    switch (type)
                    {
                        case BlockTypes.GSODeliCannon_221:
                        case BlockTypes.GSODeliCannon_222:
                        case BlockTypes.GCDeliveryCannon_464:
                        case BlockTypes.VENDeliCannon_221:
                        case BlockTypes.HE_DeliveryCannon_353:
                        case BlockTypes.BF_DeliveryCannon_122:
                            break;
                        default:
                            var recipeCase = bloc.GetComponent<ModuleRecipeProvider>();
                            if ((bool)recipeCase)
                            {
                                using (IEnumerator<RecipeTable.RecipeList> matters = recipeCase.GetEnumerator())
                                {
                                    while (matters.MoveNext())
                                    {
                                        RecipeTable.RecipeList matter = matters.Current;
                                        if (matter.m_Name != "rawresources" && matter.m_Name != "gsodelicannon")
                                        {
                                            NotMP = true;
                                        }
                                    }
                                }
                            }
                            break;
                    }
                }


                if (bloc.GetComponent<ModuleItemHolder>())
                    modCollectCount++;
                if (bloc.GetComponent<ModuleDetachableLink>())
                    modEXPLODECount++;
                if (bloc.GetComponent<ModuleBooster>())
                {
                    var module = bloc.GetComponent<ModuleBooster>();
                    foreach (FanJet jet in module.GetComponentsInChildren<FanJet>())
                    {
                        if (jet.spinDelta <= 10)
                        {
                            Quaternion quat = new OrthoRotation(blocRaw.r);
                            biasDirection -= quat * (jet.EffectorForwards * jet.force);
                        }
                    }
                    foreach (BoosterJet boost in module.GetComponentsInChildren<BoosterJet>())
                    {
                        //We have to get the total thrust in here accounted for as well because the only way we CAN boost is ALL boosters firing!
                        boostBiasDirection -= boost.LocalBoostDirection * (float)forceVal.GetValue(boost);
                    }
                    modBoostCount++;
                }
                if (bloc.GetComponent<ModuleHover>())
                    modHoverCount++;
                if (bloc.GetComponent<ModuleGyro>())
                    modGyroCount++;
                if (bloc.GetComponent<ModuleWheels>())
                    modWheelCount++;
                if (bloc.GetComponent<ModuleAntiGravityEngine>())
                    modAGCount++;

                if (bloc.GetComponent<ModuleTechController>())
                    modControlCount++;
                else
                {
                    if (bloc.GetComponent<ModuleWeapon>() || bloc.GetComponent<ModuleWeaponTeslaCoil>())
                        modDangerCount++;
                    if (bloc.GetComponent<ModuleWeaponGun>())
                        modGunCount++;
                    if (bloc.GetComponent<ModuleDrill>())
                        modDrillCount++;
                }

                try
                {
                    int tier = Singleton.Manager<ManLicenses>.inst.m_UnlockTable.GetBlockTier(type, true);
                    if (RawTechUtil.GetCorpExtended(type) == factionType)
                    {
                        if (tier > minCorpGrade)
                        {
                            minCorpGrade = tier;
                        }
                    }
                    else
                    {
                        if (tier - 1 > minCorpGrade)
                        {
                            if (tier > gradeM)
                                minCorpGrade = gradeM - 1;
                            else
                                minCorpGrade = tier - 1;
                        }
                    }
                }
                catch
                {
                    //Debug_TTExt.Log("RawTech: GetHandler - error");
                }

                if (bloc.GetComponent<ModuleWing>())
                {
                    //Get the slowest spooling one
                    foreach (ModuleWing.Aerofoil Afoil in bloc.GetComponent<ModuleWing>().m_Aerofoils)
                    {
                        if (Afoil.flapAngleRangeActual > 0 && Afoil.flapTurnSpeed > 0)
                            MovingFoilCount++;
                        FoilCount++;
                    }
                }
                blocs.Add(bloc);
            }
            bool isDef = true;
            if (modEXPLODECount > 0)
            {
                purposes.Add(BasePurpose.TechProduction);
                isDef = false;
            }
            if (modCollectCount > 0 || hasBaseFunction)
            {
                purposes.Add(BasePurpose.Harvesting);
                isDef = false;
            }
            if (NotMP)
                purposes.Add(BasePurpose.MPUnsafe);
            if (Anchored)
            {
                if (hasReceiver)
                {
                    purposes.Add(BasePurpose.HasReceivers);
                    isDef = false;
                }
                if (hasAutominer)
                {
                    purposes.Add(BasePurpose.Autominer);
                    isDef = false;
                }
                if (isDef)
                    purposes.Add(BasePurpose.Defense);
            }


            boostBiasDirection.Normalize();
            biasDirection.Normalize();

            if (biasDirection == Vector3.zero && boostBiasDirection != Vector3.zero)
            {
                isFlying = true;
                if (boostBiasDirection.y > 0.6)
                    isFlyingDirectionForwards = false;
            }
            else if (biasDirection != Vector3.zero)
            {
                isFlying = true;
                if (biasDirection.y > 0.6)
                    isFlyingDirectionForwards = false;
            }

            if (modDangerCount == 0)
                purposes.Add(BasePurpose.NoWeapons);

            terra = BaseTerrain.Land;
            string purposesList = "None.";
            if (Singleton.Manager<ManSpawn>.inst.GetBlockPrefab(BlockIndexer.StringToBlockType(mems.ElementAt(0).t)).GetComponent<ModuleAnchor>())
            {
                purposesList = "";
                foreach (BasePurpose purp in purposes)
                {
                    purposesList += purp.ToString() + "|";
                }
                Debug_TTExt.Info("RawTech: Terrain: " + terra.ToString() + " - Purposes: " + purposesList + "Anchored (static)");

                //Debug_TTExt.Log("RawTech: Purposes: Anchored (static)");
                return purposes;
            }
            else if (modBoostCount > 2 && (modHoverCount > 2 || modAGCount > 0))
            {   //Starship
                terra = BaseTerrain.Space;
            }
            else if (MovingFoilCount > 4 && isFlying && isFlyingDirectionForwards)
            {   // Airplane
                terra = BaseTerrain.Air;
            }
            else if (modGyroCount > 0 && isFlying && !isFlyingDirectionForwards)
            {   // Chopper
                terra = BaseTerrain.Air;
            }
            else if (FoilCount > 0 && modGyroCount > 0 && modBoostCount > 0 && (modWheelCount < 4 || modHoverCount > 1))
            {   // Naval
                terra = BaseTerrain.Sea;
            }
            else if (modGunCount < 2 && modDrillCount < 2 && modBoostCount > 0)
            {   // Melee
                terra = BaseTerrain.AnyNonSea;
            }

            if (!Anchored)
                purposes.Add(BasePurpose.NotStationary);

            if (mems.Count >= RawTechUtil.FrameImpactingTechBlockCount || modGunCount > 48 || modHoverCount > 18)
            {
                purposes.Add(BasePurpose.NANI);
            }

            if (purposes.Count > 0)
            {
                purposesList = "";
                foreach (BasePurpose purp in purposes)
                {
                    purposesList += purp.ToString() + "|";
                }
            }

            Debug_TTExt.Info("RawTech: Terrain: " + terra.ToString() + " - Purposes: " + purposesList);

            return purposes;
        }
        public static BaseTerrain GetBaseTerrain(TechData tech, bool Anchored)
        {
            List<TankBlock> blocs = new List<TankBlock>();
            List<RawBlockMem> mems = JSONToMemoryExternalNonAlloc(BlockSpecToJSONExternal(tech.m_BlockSpecs, out _, out _, out _, out _));
            if (mems.Count < 1)
            {
                Debug_TTExt.Log("RawTech: TECH IS NULL!  SKIPPING!");
                return BaseTerrain.Land;
            }

            HashSet<BasePurpose> purposes = new HashSet<BasePurpose>();
            //foreach (BlockMemory mem in mems)
            //{
            //    blocs.Add(Singleton.Manager<ManSpawn>.inst.GetBlockPrefab((BlockTypes)Enum.Parse(typeof(BlockTypes), mem.t)));
            //}

            bool isFlying = false;
            bool isFlyingDirectionForwards = true;

            Vector3 biasDirection = Vector3.zero;
            Vector3 boostBiasDirection = Vector3.zero;

            int FoilCount = 0;
            int MovingFoilCount = 0;

            int modControlCount = 0;
            int modCollectCount = 0;
            int modEXPLODECount = 0;
            int modBoostCount = 0;
            int modHoverCount = 0;
            int modGyroCount = 0;
            int modWheelCount = 0;
            int modAGCount = 0;
            int modDangerCount = 0;
            int modGunCount = 0;
            int modDrillCount = 0;
            bool NotMP = false;
            bool hasAutominer = false;
            bool hasReceiver = false;
            bool hasBaseFunction = false;

            //BlockUnlockTable blockList = Singleton.Manager<ManLicenses>.inst.GetBlockUnlockTable();
            //Debug_TTExt.Log("RawTech: GetHandler - " + Singleton.Manager<ManLicenses>.inst.m_UnlockTable.GetAllBlocksInTier(1, factionType, false).Count());
            foreach (RawBlockMem blocRaw in mems)
            {
                BlockTypes type = BlockIndexer.StringToBlockType(blocRaw.t);
                TankBlock bloc = Singleton.Manager<ManSpawn>.inst.GetBlockPrefab(type);
                if (bloc.IsNull())
                    continue;
                ModuleItemPickup rec = bloc.GetComponent<ModuleItemPickup>();
                if ((bool)rec)
                {
                    hasReceiver = true;
                }
                if (bloc.GetComponent<ModuleItemProducer>())
                {
                    hasAutominer = true;
                    NotMP = true;
                }
                if (bloc.GetComponent<ModuleItemConveyor>())
                {
                    NotMP = true;
                }
                if (bloc.GetComponent<ModuleItemConsume>())
                {
                    hasBaseFunction = true;
                    switch (type)
                    {
                        case BlockTypes.GSODeliCannon_221:
                        case BlockTypes.GSODeliCannon_222:
                        case BlockTypes.GCDeliveryCannon_464:
                        case BlockTypes.VENDeliCannon_221:
                        case BlockTypes.HE_DeliveryCannon_353:
                        case BlockTypes.BF_DeliveryCannon_122:
                            break;
                        default:
                            var recipeCase = bloc.GetComponent<ModuleRecipeProvider>();
                            if ((bool)recipeCase)
                            {
                                List<RecipeTable.RecipeList> matters = (List<RecipeTable.RecipeList>)recipeCase.GetEnumerator();
                                foreach (RecipeTable.RecipeList matter in matters)
                                {
                                    if (matter.m_Name != "rawresources" && matter.m_Name != "gsodelicannon")
                                    {
                                        NotMP = true;
                                    }
                                }
                            }
                            break;
                    }
                }


                if (bloc.GetComponent<ModuleTechController>())
                    modControlCount++;
                if (bloc.GetComponent<ModuleTechController>())
                    modControlCount++;
                if (bloc.GetComponent<ModuleItemHolder>())
                    modCollectCount++;
                if (bloc.GetComponent<ModuleDetachableLink>())
                    modEXPLODECount++;
                if (bloc.GetComponent<ModuleBooster>())
                {
                    var module = bloc.GetComponent<ModuleBooster>();
                    foreach (FanJet jet in module.GetComponentsInChildren<FanJet>())
                    {
                        if (jet.spinDelta <= 10)
                        {
                            Quaternion quat = new OrthoRotation(blocRaw.r);
                            biasDirection -= quat * (jet.EffectorForwards * jet.force);
                        }
                    }
                    foreach (BoosterJet boost in module.GetComponentsInChildren<BoosterJet>())
                    {
                        //We have to get the total thrust in here accounted for as well because the only way we CAN boost is ALL boosters firing!
                        boostBiasDirection -= boost.LocalBoostDirection * (float)forceVal.GetValue(boost);
                    }
                    modBoostCount++;
                }
                if (bloc.GetComponent<ModuleHover>())
                    modHoverCount++;
                if (bloc.GetComponent<ModuleGyro>())
                    modGyroCount++;
                if (bloc.GetComponent<ModuleWheels>())
                    modWheelCount++;
                if (bloc.GetComponent<ModuleAntiGravityEngine>())
                    modAGCount++;
                if (bloc.GetComponent<ModuleWeapon>())
                    modDangerCount++;
                if (bloc.GetComponent<ModuleWeaponGun>())
                    modGunCount++;
                if (bloc.GetComponent<ModuleDrill>())
                    modDrillCount++;


                if (bloc.GetComponent<ModuleWing>())
                {
                    //Get the slowest spooling one
                    foreach (ModuleWing.Aerofoil Afoil in bloc.GetComponent<ModuleWing>().m_Aerofoils)
                    {
                        FoilCount++;
                        if (Afoil.flapAngleRangeActual > 0 && Afoil.flapTurnSpeed > 0)
                            MovingFoilCount++;
                    }
                }
                blocs.Add(bloc);
            }
            bool isDef = true;
            if (modEXPLODECount > 0)
            {
                purposes.Add(BasePurpose.TechProduction);
                isDef = false;
            }
            if (modCollectCount > 0 || hasBaseFunction)
            {
                purposes.Add(BasePurpose.Harvesting);
                isDef = false;
            }
            if (NotMP)
                purposes.Add(BasePurpose.MPUnsafe);
            if (Anchored)
            {
                if (hasReceiver)
                {
                    purposes.Add(BasePurpose.HasReceivers);
                    isDef = false;
                }
                if (hasAutominer)
                {
                    purposes.Add(BasePurpose.Autominer);
                    isDef = false;
                }
                if (isDef)
                    purposes.Add(BasePurpose.Defense);
            }


            boostBiasDirection.Normalize();
            biasDirection.Normalize();

            if (biasDirection == Vector3.zero && boostBiasDirection != Vector3.zero)
            {
                isFlying = true;
                if (boostBiasDirection.y > 0.6)
                    isFlyingDirectionForwards = false;
            }
            else if (biasDirection != Vector3.zero)
            {
                isFlying = true;
                if (biasDirection.y > 0.6)
                    isFlyingDirectionForwards = false;
            }

            if (modDangerCount <= modControlCount)
                purposes.Add(BasePurpose.NoWeapons);

            BaseTerrain terra = BaseTerrain.Land;
            string purposesList;
            if (Singleton.Manager<ManSpawn>.inst.GetBlockPrefab(BlockIndexer.StringToBlockType(mems.ElementAt(0).t)).GetComponent<ModuleAnchor>())
            {
                purposesList = "";
                foreach (BasePurpose purp in purposes)
                {
                    purposesList += purp.ToString() + "|";
                }
                Debug_TTExt.Info("RawTech: Terrain: " + terra.ToString() + " - Purposes: " + purposesList + "Anchored (static)");

                return BaseTerrain.Land;
            }
            else if (modBoostCount > 2 && (modHoverCount > 2 || modAGCount > 0))
            {   //Starship
                terra = BaseTerrain.Space;
            }
            else if (MovingFoilCount > 4 && isFlying && isFlyingDirectionForwards)
            {   // Airplane
                terra = BaseTerrain.Air;
            }
            else if (modGyroCount > 0 && isFlying && !isFlyingDirectionForwards)
            {   // Chopper
                terra = BaseTerrain.Air;
            }
            else if (FoilCount > 0 && modGyroCount > 0 && modBoostCount > 0 && (modWheelCount < 4 || modHoverCount > 1))
            {   // Naval
                terra = BaseTerrain.Sea;
            }
            else if (modGunCount < 2 && modDrillCount < 2 && modBoostCount > 0)
            {   // Melee
                terra = BaseTerrain.AnyNonSea;
            }

            if (!Anchored)
                purposes.Add(BasePurpose.NotStationary);

            if (mems.Count >= RawTechUtil.FrameImpactingTechBlockCount || modGunCount > 48 || modHoverCount > 18)
            {
                purposes.Add(BasePurpose.NANI);
            }

            if (purposes.Count > 0)
            {
                purposesList = "";
                foreach (BasePurpose purp in purposes)
                {
                    purposesList += purp.ToString() + "|";
                }
            }

            Debug_TTExt.Info("RawTech: Terrain: " + terra.ToString());

            return terra;
        }

        public static FactionTypesExt GetTopCorp(Tank tank)
        {   // 
            FactionTypesExt final = tank.GetMainCorpExt();
            if (!(bool)Singleton.Manager<ManLicenses>.inst)
                return final;
            int corps = Enum.GetNames(typeof(FactionTypesExt)).Length;
            int[] corpCounts = new int[corps];

            foreach (TankBlock block in tank.blockman.IterateBlocks())
            {
                corpCounts[(int)RawTechUtil.GetBlockCorpExt(block.BlockType)]++;
            }
            int blockCounts = 0;
            int bestCorpIndex = 0;
            for (int step = 0; step < corps; step++)
            {
                int num = corpCounts[step];
                if (num > blockCounts)
                {
                    bestCorpIndex = step;
                    blockCounts = num;
                }
            }
            final = (FactionTypesExt)bestCorpIndex;
            return final;
        }
        public static FactionTypesExt GetTopCorp(TechData tank)
        {   // 
            FactionTypesExt final = tank.GetMainCorpExt();
            if (!(bool)Singleton.Manager<ManLicenses>.inst)
                return final;
            int corps = Enum.GetNames(typeof(FactionTypesExt)).Length;
            int[] corpCounts = new int[corps];

            foreach (var block in tank.m_BlockSpecs)
            {
                corpCounts[(int)RawTechUtil.GetBlockCorpExt(block.m_BlockType)]++;
            }
            int blockCounts = 0;
            int bestCorpIndex = 0;
            for (int step = 0; step < corps; step++)
            {
                int num = corpCounts[step];
                if (num > blockCounts)
                {
                    bestCorpIndex = step;
                    blockCounts = num;
                }
            }
            final = (FactionTypesExt)bestCorpIndex;
            return final;
        }
        public static FactionTypesExt GetTopCorp(List<RawBlockMem> tank)
        {   // 
            FactionTypesExt final = tank.GetMainCorpExt();
            if (!(bool)Singleton.Manager<ManLicenses>.inst)
                return final;
            int corps = Enum.GetNames(typeof(FactionTypesExt)).Length;
            int[] corpCounts = new int[corps];

            foreach (var block in tank)
            {
                corpCounts[(int)RawTechUtil.GetBlockCorpExt(BlockIndexer.StringToBlockType(block.t))]++;
            }
            int blockCounts = 0;
            int bestCorpIndex = 0;
            for (int step = 0; step < corps; step++)
            {
                int num = corpCounts[step];
                if (num > blockCounts)
                {
                    bestCorpIndex = step;
                    blockCounts = num;
                }
            }
            final = (FactionTypesExt)bestCorpIndex;
            return final;
        }

        public static Dictionary<int, List<string>> EncodeSerialData(TechData tech)
        {
            Dictionary<int, List<string>> blockSerial = new Dictionary<int, List<string>>();
            if (tech.m_BlockSpecs.Count < 1)
            {
                Debug_TTExt.Log("RawTech: TECH IS NULL!");
                return null;
            }
            int blockIndex = 0;
            for (int step = 0; step < tech.m_BlockSpecs.Count; step++)
            {
                var blocRaw = tech.m_BlockSpecs[step];
                if (blocRaw.textSerialData != null && blocRaw.textSerialData.Count > 0)
                {
                    var blockInfo = new List<string>();
                    foreach (var item in blocRaw.textSerialData)
                    {
                        blockInfo.Add(item);
                    }
                    if (blockInfo.Count > 0)
                        blockSerial.Add(step, blockInfo);
                }
                blockIndex++;
            }
            if (blockSerial.Count == 0)
                return null;
            return blockSerial;
        }
        public void DecodeSerialData(TechData tech)
        {
            if (serialDataBlock != null)
            {
                foreach (var item in serialDataBlock)
                {
                    try
                    {
                        var blockSpec = tech.m_BlockSpecs[item.Key];
                        blockSpec.textSerialData = item.Value;
                        tech.m_BlockSpecs[item.Key] = blockSpec;
                    }
                    catch { }
                }
            }
        }



        public bool IsDefense()
        {
            return purposes.Contains(BasePurpose.Defense);
        }

        public static int GetBBCost(List<RawBlockMem> mem)
        {
            int output = 0;
            foreach (RawBlockMem block in mem)
            {
                try
                {
                    output += Singleton.Manager<RecipeManager>.inst.GetBlockBuyPrice((BlockTypes)Enum.Parse(typeof(BlockTypes), block.t));
                }
                catch { }
            }
            return output;
        }
        public static int GetBBCost(ManSaveGame.StoredTech tech)
        {
            return tech.m_TechData.GetValue();
        }
        public static int GetBBCost(Tank tank)
        {
            int output = 0;
            foreach (TankBlock block in tank.blockman.IterateBlocks())
            {
                output += Singleton.Manager<RecipeManager>.inst.GetBlockBuyPrice(block.BlockType);
            }
            return output;
        }
        public static int GetBBCost(string JSONTechBlueprint)
        {
            int output = 0;
            List<RawBlockMem> mem = JSONToMemoryExternalNonAlloc(JSONTechBlueprint);
            foreach (RawBlockMem block in mem)
            {
                output += Singleton.Manager<RecipeManager>.inst.GetBlockBuyPrice(BlockIndexer.StringToBlockType(block.t), true);
            }
            return output;
        }



        //External
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

        /// <summary>
        /// Mandatory for techs that have plans to be built over time by the building AI.
        /// The first block being an anchored block will determine if the entire techs live or not
        ///   on spawning. If this fails, there's a good chance the AI could have wasted money on it.
        /// </summary>
        /// <param name="ToSearch">The list of saved blocks to find the new root in</param>
        /// <returns>The new Root block</returns>
        public static TankPreset.BlockSpec FindProperRootBlockExternal(List<TankPreset.BlockSpec> ToSearch)
        {
            bool IsAnchoredAnchorPresent = false;
            float close = 128 * 128;
            TankPreset.BlockSpec newRoot = ToSearch.FirstOrDefault();
            foreach (TankPreset.BlockSpec blocS in ToSearch)
            {
                TankBlock bloc = ManSpawn.inst.GetBlockPrefab(BlockIndexer.StringToBlockType(blocS.block));
                Vector3 blockPos = blocS.position + new OrthoRotation((OrthoRotation.r)blocS.orthoRotation) * bloc.filledCells[0];
                if (bloc.GetComponent<ModuleAnchor>() && blocS.CheckIsAnchored())
                {
                    IsAnchoredAnchorPresent = true;
                    break;
                }
                float sqrMag = blockPos.sqrMagnitude;
                if (sqrMag < close && (bloc.GetComponent<ModuleTechController>() || bloc.GetComponent<ModuleAIBot>()))
                {
                    close = sqrMag;
                    newRoot = blocS;
                }
            }
            if (IsAnchoredAnchorPresent)
            {
                close = 128 * 128;
                foreach (TankPreset.BlockSpec blocS in ToSearch)
                {
                    TankBlock bloc = ManSpawn.inst.GetBlockPrefab(BlockIndexer.StringToBlockType(blocS.block));
                    Vector3 blockPos = blocS.position + new OrthoRotation((OrthoRotation.r)blocS.orthoRotation) * bloc.filledCells[0];
                    float sqrMag = blockPos.sqrMagnitude;
                    if (sqrMag < close && bloc.GetComponent<ModuleAnchor>() && blocS.CheckIsAnchored())
                    {
                        close = sqrMag;
                        newRoot = blocS;
                    }
                }
            }
            return newRoot;
        }

        /// <summary>
        /// Mandatory for techs that have plans to be built over time by the building AI.
        /// Since the first block placed ultimately determines the base rotation of the Tech
        ///  (Arrow shown on Radar/minimap) we must be ABSOLUTELY SURE to build teh Tech in relation
        ///   to that first block.
        ///   Any alteration on the first block's rotation will have severe consequences in the long run.
        ///   
        /// Split techs on the other hand are mostly free from this issue.
        /// </summary>
        /// <param name="ToSearch">The list of blocks to find the new foot in</param>
        /// <returns></returns>
        public static List<RawBlockMem> TechToMemoryExternal(Tank tank)
        {
            // This resaves the whole tech cab-forwards regardless of original rotation
            //   It's because any solutions that involve the cab in a funny direction will demand unholy workarounds.
            //   I seriously don't know why the devs didn't try it this way, perhaps due to lag reasons.
            //   or the blocks that don't allow upright placement (just detach those lmao)
            List<RawBlockMem> output = new List<RawBlockMem>();
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
                RawBlockMem mem = new RawBlockMem
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
                    Debug_TTExt.Log("RawTech:  DesignMemory - " + tank.name + ": could not save " + bloc.name + " in blueprint due to illegal rotation.");
                    continue;
                }
                output.Add(mem);
            }
            Debug_TTExt.Info("RawTech:  DesignMemory - Saved " + tank.name + " to memory format");

            return output;
        }

        /// <summary>
        /// Mandatory for techs that have plans to be built over time by the building AI.
        /// Since the first block placed ultimately determines the base rotation of the Tech
        ///  (Arrow shown on Radar/minimap) we must be ABSOLUTELY SURE to build teh Tech in relation
        ///   to that first block.
        ///   Any alteration on the first block's rotation will have severe consequences in the long run.
        ///   
        /// Split techs on the other hand are mostly free from this issue.
        /// </summary>
        /// <param name="ToSearch">The list of blocks to find the new foot in</param>
        /// <returns></returns>
        public static List<RawBlockMem> TechToMemoryExternal(TechData tank, bool ExceptionIfFail = false)
        {
            // This resaves the whole tech cab-forwards regardless of original rotation
            //   It's because any solutions that involve the cab in a funny direction will demand unholy workarounds.
            //   I seriously don't know why the devs didn't try it this way, perhaps due to lag reasons.
            //   or the blocks that don't allow upright placement (just detach those lmao)
            List<RawBlockMem> output = new List<RawBlockMem>();
            List<TankPreset.BlockSpec> ToSave = tank.m_BlockSpecs;
            TankPreset.BlockSpec rootBlock = FindProperRootBlockExternal(ToSave);
            if (rootBlock.m_BlockType != ToSave.FirstOrDefault().m_BlockType)
            {
                ToSave.Remove(rootBlock);
                ToSave.Insert(0, rootBlock);
            }

            Vector3 coreOffset = rootBlock.position;
            Quaternion coreRot = Quaternion.Euler(new OrthoRotation(rootBlock.orthoRotation).ToEulers());

            foreach (TankPreset.BlockSpec blocS in ToSave)
            {
                BlockTypes BT = BlockIndexer.StringToBlockType(blocS.block);
                TankBlock bloc = ManSpawn.inst.GetBlockPrefab(BT);
                if (bloc == null || BT == BlockTypes.GSOAIController_111 || !Singleton.Manager<ManSpawn>.inst.IsTankBlockLoaded(BT))
                {
                    if (ExceptionIfFail)
                        throw new NullReferenceException("TechToMemoryExternal - Block " + 
                            StringLookup.GetItemName(ObjectTypes.Block, (int)BT) + " does not exists. Entry " + 
                            blocS.block + " could not be found");
                    continue;
                }
                Quaternion deltaRot = Quaternion.Inverse(coreRot);
                Quaternion savRot = Quaternion.Euler(new OrthoRotation(blocS.orthoRotation).ToEulers());
                RawBlockMem mem = new RawBlockMem
                {
                    t = blocS.block,
                    p = deltaRot * (blocS.position - coreOffset),
                    r = SetCorrectRotation(savRot, deltaRot).rot,
                };
                //Get the rotation
                if (!IsValidRotation(bloc, mem.r))
                {   // block cannot be saved - illegal rotation.
                    continue;
                }
                output.Add(mem);
            }
            if (!output.Any())
                throw new NullReferenceException("RawTechTemplate.ctor - Tech is invalid! No blocks could be loaded!!!");
            return output;
        }
        public static string TechToJSONExternal(Tank tank)
        {   // Saving a Tech from the BlockMemory
            return MemoryToJSONExternal(TechToMemoryExternal(tank));
        }


        private static StringBuilder TechRAW = new StringBuilder();
        private static StringBuilder TechRAWCase = new StringBuilder();
        public static string MemoryToJSONExternal(List<RawBlockMem> mem, bool ExceptionIfFail = false)
        {   // Saving a Tech from the BlockMemory
            if (!mem.Any())
            {
                if (ExceptionIfFail)
                    throw new NullReferenceException("MemoryToJSONExternal expects mem contain at least one instance in it.  Got none.");
                return null;
            }
            try
            {
                TechRAW.Append(JsonUtility.ToJson(mem.First()));
                for (int step = 1; step < mem.Count; step++)
                {
                    TechRAW.Append("|");
                    TechRAW.Append(JsonUtility.ToJson(mem.ElementAt(step)));
                }
                try
                {
                    foreach (char ch in TechRAW.ToString())
                    {
                        if (ch == '"')
                        {
                            //JSONTech.Append("\\");
                            TechRAWCase.Append(ch);
                        }
                        else
                            TechRAWCase.Append(ch);
                    }
                    //Debug_TTExt.Log("RawTech: " + JSONTech.ToString());
                    return TechRAWCase.ToString();
                }
                finally
                {
                    TechRAWCase.Clear();
                }
            }
            finally
            {
                TechRAW.Clear();
            }
        }

        private static StringBuilder RAWCase = new StringBuilder();
        public static List<RawBlockMem> JSONToMemoryExternal(string toLoad)
        {   // Loading a Tech from the BlockMemory
            try
            {
                List<RawBlockMem> mem = new List<RawBlockMem>();
                foreach (char ch in toLoad)
                {
                    if (ch != '\\')
                    {
                        if (ch == '|')//new block
                        {
                            mem.Add(JsonUtility.FromJson<RawBlockMem>(RAWCase.ToString()));
                            RAWCase.Clear();
                        }
                        else
                            RAWCase.Append(ch);
                    }
                }
                mem.Add(JsonUtility.FromJson<RawBlockMem>(RAWCase.ToString()));
                //Debug_TTExt.Log("RawTech:  DesignMemory: saved " + mem.Count);
                return mem;
            }
            finally
            {
                RAWCase.Clear();
            }
        }

        private static List<RawBlockMem> nonAllocL = new List<RawBlockMem>();
        public static List<RawBlockMem> JSONToMemoryExternalNonAlloc(string toLoad)
        {   // Loading a Tech from the BlockMemory
            try
            {
                nonAllocL.Clear();
                foreach (char ch in toLoad)
                {
                    if (ch != '\\')
                    {
                        if (ch == '|')//new block
                        {
                            nonAllocL.Add(JsonUtility.FromJson<RawBlockMem>(RAWCase.ToString()));
                            RAWCase.Clear();
                        }
                        else
                            RAWCase.Append(ch);
                    }
                }
                nonAllocL.Add(JsonUtility.FromJson<RawBlockMem>(RAWCase.ToString()));
                //Debug_TTExt.Log("RawTech:  DesignMemory: saved " + mem.Count);
                return nonAllocL;
            }
            finally
            {
                RAWCase.Clear();
            }
        }

        public static bool IsValidRotation(TankBlock TB, OrthoRotation.r r)
        {

            return true; // can't fetch proper context for some reason
            Singleton.Manager<ManTechBuilder>.inst.ClearBlockRotationOverride(TB);
            OrthoRotation.r[] rots = Singleton.Manager<ManTechBuilder>.inst.GetBlockRotationOrder(TB);
            Singleton.Manager<ManTechBuilder>.inst.ClearBlockRotationOverride(TB);
            if (rots != null && rots.Length > 0 && !rots.Contains(r))
            {   // block cannot be saved - illegal rotation.
                return false;
            }
            return true;
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
                    Debug_TTExt.Log("RawTech: ReplaceBlock - Matching failed - OrthoRotation is missing edge case");
                }
            }
            return rot;
        }


        public static BlockTypes JSONToFirstBlock(string toLoad, bool exceptionOnFail = false)
        {   // Loading a Tech from the BlockMemory
            RawBlockMem mem = null;
            try
            {

                foreach (char ch in toLoad)
                {
                    if (ch != '\\')
                    {
                        if (ch == '|')//new block
                        {
                            mem = JsonUtility.FromJson<RawBlockMem>(RAWCase.ToString());
                            break;
                        }
                        else
                            RAWCase.Append(ch);
                    }
                }
                if (mem == null)
                    mem = JsonUtility.FromJson<RawBlockMem>(RAWCase.ToString());
            }
            finally
            {
                RAWCase.Clear();
            }
            if (mem == null)
            {
                if (exceptionOnFail)
                    throw new NullReferenceException("JSONToFirstBlock could not find the first block from the following json: \n" + toLoad);
                mem = new RawBlockMem();
            }

            return BlockIndexer.StringToBlockType(mem.t);
        }

        // Utilities
        public static string BlockSpecToJSONExternal(List<TankPreset.BlockSpec> specs, out int blockCount, out bool lethal, out int hoveCount, out int weapGCount)
        {   // Saving a Tech from the BlockMemory
            blockCount = 0;
            int ctrlCount = 0;
            int weapCount = 0;
            weapGCount = 0;
            hoveCount = 0;
            lethal = false;
            if (specs.Count == 0)
                return null;
            bool invalidBlocks = false;
            List<RawBlockMem> mem = new List<RawBlockMem>();
            foreach (TankPreset.BlockSpec spec in specs)
            {
                RawBlockMem mem1 = new RawBlockMem
                {
                    t = spec.block,
                    p = spec.position,
                    r = new OrthoRotation(spec.orthoRotation).rot
                };

                try
                {
                    TankBlock block = Singleton.Manager<ManSpawn>.inst.GetBlockPrefab(spec.GetBlockType());
                    if (block != null)
                    {
                        if (block.GetComponent<ModuleHover>())
                            hoveCount++;
                        if (block.GetComponent<ModuleWeapon>())
                            weapCount++;
                        if (block.GetComponent<ModuleWeaponGun>())
                            weapGCount++;
                        if (block.GetComponent<ModuleTechController>())
                            ctrlCount++;
                    }
                    else
                        invalidBlocks = true;
                }
                catch
                {
                    invalidBlocks = true;
                }
                mem.Add(mem1);
            }

            lethal = weapCount > ctrlCount;
            blockCount = mem.Count;


            if (invalidBlocks)
                Debug_TTExt.Log("RawTech: Invalid blocks in TechData");


            //Debug_TTExt.Log("RawTech: " + MemoryToJSONExternal(mem).ToString());
            return MemoryToJSONExternal(mem);
        }

        private static readonly Dictionary<int, List<byte>> valid = new Dictionary<int, List<byte>>();
        private static readonly Dictionary<int, byte> valid2 = new Dictionary<int, byte>();
        internal static void ResetSkinIDSet()
        {
            valid.Clear();
            valid2.Clear();
        }
        internal static byte GetSkinIDSet(int faction)
        {
            if (valid2.TryGetValue(faction, out byte num))
            {
                return num;
            }
            else
            {
                try
                {
                    byte pick = GetSkinIDRand(faction);
                    valid2.Add(faction, pick);
                    return pick;
                }
                catch { }// corp has no skins!
            }
            return 0;
        }
        internal static byte GetSkinIDCase(int team, int faction)
        {
            if (valid.TryGetValue(faction, out List<byte> num))
            {
                return num[team % num.Count];
            }
            else
            {
                try
                {
                    num2.Clear();
                    FactionSubTypes FST = (FactionSubTypes)faction;
                    int count = ManCustomSkins.inst.GetNumSkinsInCorp(FST);
                    for (int step = 0; step < count; step++)
                    {
                        byte skin = ManCustomSkins.inst.SkinIndexToID((byte)step, FST);
                        if (!ManDLC.inst.IsSkinLocked(skin, FST))
                        {
                            num2.Add(skin);
                        }
                    }
                    valid.Add(faction, num2);
                    return num2[team % num2.Count];
                }
                catch { }// corp has no skins!
            }
            return 0;
        }
        internal static byte GetSkinIDRand(int faction)
        {
            if (valid.TryGetValue(faction, out List<byte> num))
            {
                return num.GetRandomEntry();
            }
            else
            {
                try
                {
                    num2.Clear();
                    FactionSubTypes FST = (FactionSubTypes)faction;
                    int count = ManCustomSkins.inst.GetNumSkinsInCorp(FST);
                    for (int step = 0; step < count; step++)
                    {
                        byte skin = ManCustomSkins.inst.SkinIndexToID((byte)step, FST);
                        if (!ManDLC.inst.IsSkinLocked(skin, FST))
                        {
                            num2.Add(skin);
                            //Debug_TTExt.Log("SKINSSSSSS " + ManCustomSkins.inst.GetSkinNameForSnapshot(FST, skin));
                        }
                    }
                    valid.Add(faction, num2);
                    return num2.GetRandomEntry();
                }
                catch { }// corp has no skins!
            }
            return 0;
        }

        internal static byte GetSkinIDSetForTeam(int team, int faction)
        {
            if (valid2.TryGetValue(faction, out byte num))
            {
                return num;
            }
            else
            {
                try
                {
                    byte pick = GetSkinIDCase(team, faction);
                    valid2.Add(faction, pick);
                    return pick;
                }
                catch { }// corp has no skins!
            }
            return 0;
        }
        private static List<byte> num2 = new List<byte>();
        private static Tank InstantTech(Vector3 pos, Vector3 forward, int Team, string name, string blueprint, bool grounded, bool ForceAnchor = false, bool population = false, bool randomSkins = true, bool UseTeam = false)
        {
            TechData data = new TechData
            {
                Name = name,
                m_Bounds = new IntVector3(new Vector3(18, 18, 18)),
                m_SkinMapping = new Dictionary<uint, string>(),
                m_TechSaveState = new Dictionary<int, TechComponent.SerialData>(),
                m_CreationData = new TechData.CreationData(),
                m_BlockSpecs = new List<TankPreset.BlockSpec>()
            };

            bool skinChaotic = false;
            ResetSkinIDSet();
            if (randomSkins)
            {
                skinChaotic = UnityEngine.Random.Range(0, 100) < 2;
            }
            foreach (RawBlockMem mem in JSONToMemoryExternalNonAlloc(blueprint))
            {
                BlockTypes type = BlockIndexer.StringToBlockType(mem.t);
                if (!Singleton.Manager<ManSpawn>.inst.IsBlockAllowedInCurrentGameMode(type) ||
                        Singleton.Manager<ManSpawn>.inst.IsBlockUsageRestrictedInGameMode(type))
                {
                    Debug_TTExt.Log("TTExtUtil: InstantTech - Removed " + mem.t + " as it was invalidated");
                    continue;
                }
                TankPreset.BlockSpec spec = default;
                spec.block = mem.t;
                spec.m_BlockType = type;
                spec.orthoRotation = new OrthoRotation(mem.r);
                spec.position = mem.p;
                spec.saveState = new Dictionary<int, Module.SerialData>();
                spec.textSerialData = new List<string>();

                if (UseTeam)
                {
                    FactionTypesExt factType = RawTechUtil.GetCorpExtended(type);
                    FactionSubTypes FST = RawTechUtil.CorpExtToCorp(factType);
                    spec.m_SkinID = GetSkinIDSetForTeam(Team, (int)FST);
                }
                else if (randomSkins)
                {
                    FactionTypesExt factType = RawTechUtil.GetCorpExtended(type);
                    FactionSubTypes FST = RawTechUtil.CorpExtToCorp(factType);
                    if (skinChaotic)
                    {
                        spec.m_SkinID = GetSkinIDRand((int)FST);
                    }
                    else
                    {
                        spec.m_SkinID = GetSkinIDSet((int)FST);
                    }
                }
                else
                    spec.m_SkinID = 0;

                data.m_BlockSpecs.Add(spec);
            }

            Tank theTech;
            if (ManNetwork.IsNetworked)
            {
                uint[] BS = new uint[data.m_BlockSpecs.Count];
                for (int step = 0; step < data.m_BlockSpecs.Count; step++)
                {
                    BS[step] = Singleton.Manager<ManNetwork>.inst.GetNextHostBlockPoolID();
                }
                TrackedVisible TV = ManNetwork.inst.SpawnNetworkedNonPlayerTech(data, BS,
                    WorldPosition.FromScenePosition(pos).ScenePosition,
                    Quaternion.LookRotation(forward, Vector3.up), grounded);
                if (TV == null)
                {
                    Debug_TTExt.FatalError("TTExtUtil: InstantTech(TrackedVisible)[MP] - error on SpawnTank");
                    return null;
                }
                if (TV.visible == null)
                {
                    Debug_TTExt.FatalError("TTExtUtil: InstantTech(Visible)[MP] - error on SpawnTank");
                    return null;
                }
                theTech = TV.visible.tank;
                if (theTech.IsNull())
                {
                    throw new NullReferenceException("TTExtUtil: InstantTech[MP] - error on SpawnTank" +
                        " - Resulting tech is null");
                }
                else
                    TryForceIntoPop(theTech);
            }
            else
            {
                ManSpawn.TankSpawnParams tankSpawn = new ManSpawn.TankSpawnParams
                {
                    techData = data,
                    blockIDs = null,
                    teamID = Team,
                    position = pos,
                    rotation = Quaternion.LookRotation(forward, Vector3.up),//Singleton.cameraTrans.position - pos
                    ignoreSceneryOnSpawnProjection = false,
                    forceSpawn = true,
                    isPopulation = population
                };
                if (ForceAnchor)
                    tankSpawn.grounded = true;
                else
                    tankSpawn.grounded = grounded;
                theTech = Singleton.Manager<ManSpawn>.inst.SpawnTank(tankSpawn, true);
                if (theTech.IsNull())
                {
                    throw new NullReferenceException("TTExtUtil: InstantTech - error on SpawnTank" +
                        " - Resulting tech is null");
                }
                else
                    TryForceIntoPop(theTech);
            }

            ForceAllBubblesUp(theTech);
            if (ForceAnchor)
            {
                theTech.gameObject.AddComponent<RequestAnchored>();
                theTech.trans.position = theTech.trans.position + new Vector3(0, -0.5f, 0);
                //theTech.visible.MoveAboveGround();
            }

            Debug_TTExt.Log("TTExtUtil: InstantTech - Built " + name);

            return theTech;
        }

        private static Tank InstantTech(Vector3 pos, Vector3 forward, int Team, string name, RawTechTemplate blueprint, bool grounded, bool ForceAnchor = false, bool population = false, bool randomSkins = true, bool UseTeam = false)
        {
            TechData data = new TechData
            {
                Name = name,
                m_Bounds = new IntVector3(new Vector3(18, 18, 18)),
                m_SkinMapping = new Dictionary<uint, string>(),
                m_TechSaveState = new Dictionary<int, TechComponent.SerialData>(),
                m_CreationData = new TechData.CreationData(),
                m_BlockSpecs = new List<TankPreset.BlockSpec>()
            };

            bool skinChaotic = false;
            ResetSkinIDSet();
            if (randomSkins)
            {
                skinChaotic = UnityEngine.Random.Range(0, 100) < 2;
            }
            foreach (RawBlockMem mem in JSONToMemoryExternalNonAlloc(blueprint.savedTech))
            {
                BlockTypes type = BlockIndexer.StringToBlockType(mem.t);
                if (!Singleton.Manager<ManSpawn>.inst.IsBlockAllowedInCurrentGameMode(type) ||
                        Singleton.Manager<ManSpawn>.inst.IsBlockUsageRestrictedInGameMode(type))
                {
                    Debug_TTExt.Log("TTExtUtil: InstantTech - Removed " + mem.t + " as it was invalidated");
                    continue;
                }
                TankPreset.BlockSpec spec = default;
                spec.block = mem.t;
                spec.m_BlockType = type;
                spec.orthoRotation = new OrthoRotation(mem.r);
                spec.position = mem.p;
                spec.saveState = new Dictionary<int, Module.SerialData>();
                spec.textSerialData = new List<string>();

                if (UseTeam)
                {
                    FactionTypesExt factType = RawTechUtil.GetCorpExtended(type);
                    FactionSubTypes FST = RawTechUtil.CorpExtToCorp(factType);
                    spec.m_SkinID = GetSkinIDSetForTeam(Team, (int)FST);
                }
                else if (randomSkins)
                {
                    FactionTypesExt factType = RawTechUtil.GetCorpExtended(type);
                    FactionSubTypes FST = RawTechUtil.CorpExtToCorp(factType);
                    if (skinChaotic)
                    {
                        spec.m_SkinID = GetSkinIDRand((int)FST);
                    }
                    else
                    {
                        spec.m_SkinID = GetSkinIDSet((int)FST);
                    }
                }
                else
                    spec.m_SkinID = 0;

                data.m_BlockSpecs.Add(spec);
            }

            Tank theTech;
            if (ManNetwork.IsNetworked)
            {
                uint[] BS = new uint[data.m_BlockSpecs.Count];
                for (int step = 0; step < data.m_BlockSpecs.Count; step++)
                {
                    BS[step] = Singleton.Manager<ManNetwork>.inst.GetNextHostBlockPoolID();
                }
                TrackedVisible TV = ManNetwork.inst.SpawnNetworkedNonPlayerTech(data, BS,
                    WorldPosition.FromScenePosition(pos).ScenePosition,
                    Quaternion.LookRotation(forward, Vector3.up), grounded);
                if (TV == null)
                {
                    Debug_TTExt.FatalError("TTExtUtil: InstantTech(TrackedVisible)[MP] - error on SpawnTank");
                    return null;
                }
                if (TV.visible == null)
                {
                    Debug_TTExt.FatalError("TTExtUtil: InstantTech(Visible)[MP] - error on SpawnTank");
                    return null;
                }
                theTech = TV.visible.tank;
                if (theTech.IsNull())
                {
                    throw new NullReferenceException("TTExtUtil: InstantTech[MP] - error on SpawnTank" +
                        " - Resulting tech is null");
                }
                else
                    TryForceIntoPop(theTech);
            }
            else
            {
                ManSpawn.TankSpawnParams tankSpawn = new ManSpawn.TankSpawnParams
                {
                    techData = data,
                    blockIDs = null,
                    teamID = Team,
                    position = pos,
                    rotation = Quaternion.LookRotation(forward, Vector3.up),//Singleton.cameraTrans.position - pos
                    ignoreSceneryOnSpawnProjection = false,
                    forceSpawn = true,
                    isPopulation = population
                };
                if (ForceAnchor)
                    tankSpawn.grounded = true;
                else
                    tankSpawn.grounded = grounded;
                theTech = Singleton.Manager<ManSpawn>.inst.SpawnTank(tankSpawn, true);
                if (theTech.IsNull())
                {
                    throw new NullReferenceException("TTExtUtil: InstantTech - error on SpawnTank" +
                        " - Resulting tech is null");
                }
                else
                    TryForceIntoPop(theTech);
            }

            ForceAllBubblesUp(theTech);
            if (ForceAnchor)
            {
                theTech.gameObject.AddComponent<RequestAnchored>();
                theTech.trans.position = theTech.trans.position + new Vector3(0, -0.5f, 0);
                //theTech.visible.MoveAboveGround();
            }

            Debug_TTExt.Log("TTExtUtil: InstantTech - Built " + name);

            return theTech;
        }

        /// <summary>
        /// Spawns a RawTech IMMEDEATELY.  Do NOT Call while calling BlockMan or spawner blocks or the game will break!
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="Team"></param>
        /// <param name="forwards"></param>
        /// <param name="Blueprint"></param>
        /// <param name="snapTerrain"></param>
        /// <param name="Charged"></param>
        /// <param name="ForceInstant"></param>
        /// <returns></returns>
        public Tank SpawnRawTech(Vector3 pos, int Team, Vector3 forwards, bool snapTerrain = false, bool Charged = false, bool randomSkins = false)
        {
            if (savedTech == null)
                throw new NullReferenceException("TTExtUtil: SpawnTechExternal - Was handed a NULL Blueprint!");
            string baseBlueprint = savedTech;

            try
            {
                Tank theTech = InstantTech(pos, forwards, Team, techName, baseBlueprint, snapTerrain, ForceAnchor: !purposes.Contains(BasePurpose.NotStationary), Team == -1, randomSkins);
                Debug_TTExt.Log("TTExtUtil: SpawnTechExternal - Spawned " + techName + " at " + pos + ". Snapped to terrain " + snapTerrain);

                if (Team == -2)//neutral - be crafty mike and face the player
                    theTech.AI.SetBehaviorType(AITreeType.AITypes.FacePlayer);
                if (Charged)
                    ChargeAndClean(theTech);

                return theTech;
            }
            catch (Exception e)
            {
                throw new Exception("Tech " + techName + " failed to spawn", e);
            }
        }

        internal static TrackedVisible TrackTank(Tank tank, bool anchored = false)
        {
            if (ManNetwork.IsNetworked)
            {
                //Debug_TTExt.Log("TTExtUtil: RawTechLoader(MP) - No such tracking function is finished yet - " + tank.name);
            }
            TrackedVisible tracked = ManVisible.inst.GetTrackedVisible(tank.visible.ID);
            if (tracked != null)
            {
                //Debug_TTExt.Log("TTExtUtil: RawTechLoader - Updating Tracked " + tank.name);
                tracked.SetPos(tank.boundsCentreWorldNoCheck);
                return tracked;
            }

            tracked = new TrackedVisible(tank.visible.ID, tank.visible, ObjectTypes.Vehicle, anchored ? RadarTypes.Base : RadarTypes.Vehicle);
            tracked.SetPos(tank.boundsCentreWorldNoCheck);
            tracked.TeamID = tank.Team;
            ManVisible.inst.TrackVisible(tracked);
            //Debug_TTExt.Log("TTExtUtil: RawTechLoader - Tracking " + tank.name);
            return tracked;
        }
        private static readonly FieldInfo forceInsert = typeof(ManPop).GetField("m_SpawnedTechs", BindingFlags.NonPublic | BindingFlags.Instance);
        private static void TryForceIntoPop(Tank tank)
        {
            if (tank.Team == -1) // the wild tech pop number
            {
                List<TrackedVisible> visT = (List<TrackedVisible>)forceInsert.GetValue(ManPop.inst);
                visT.Add(TrackTank(tank, tank.IsAnchored));
                forceInsert.SetValue(ManPop.inst, visT);
                //Debug_TTExt.Log("TTExtUtil: RawTechLoader - Forced " + tank.name + " into population");
            }
        }


        internal static FieldInfo charge = typeof(ModuleShieldGenerator).GetField("m_EnergyDeficit", BindingFlags.NonPublic | BindingFlags.Instance);
        internal static FieldInfo charge2 = typeof(ModuleShieldGenerator).GetField("m_State", BindingFlags.NonPublic | BindingFlags.Instance);
        internal static FieldInfo charge3 = typeof(ModuleShieldGenerator).GetField("m_Shield", BindingFlags.NonPublic | BindingFlags.Instance);
        private static void ForceAllBubblesUp(Tank tank)
        {
            try
            {
                if (ManNetwork.IsNetworked && !ManNetwork.IsHost)
                    return;

                foreach (ModuleShieldGenerator buubles in tank.blockman.IterateBlockComponents<ModuleShieldGenerator>())
                {
                    if ((bool)buubles)
                    {
                        charge.SetValue(buubles, 0);
                        charge2.SetValue(buubles, 2);
                        BubbleShield shield = (BubbleShield)charge3.GetValue(buubles);
                        shield.SetTargetScale(buubles.m_Radius);
                    }
                }
            }
            catch
            {
                Debug_TTExt.Log("TTExtUtil: ForceAllBubblesUp - error");
            }
        }
        public static void ChargeAndClean(Tank tank, float fullPercent = 1)
        {
            try
            {
                tank.EnergyRegulator.SetAllStoresAmount(fullPercent);
                ForceAllBubblesUp(tank);
            }
            catch
            {
                Debug_TTExt.Log("TTExtUtil: ChargeAndClean - error");
            }
        }
    }
    internal class RequestAnchored : MonoBehaviour
    {
        sbyte delay = 4;
        private void Update()
        {
            delay--;
            if (delay == 0)
                Destroy(this);
        }
    }


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
