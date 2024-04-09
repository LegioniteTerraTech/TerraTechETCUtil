using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Newtonsoft.Json;

namespace TerraTechETCUtil
{
    public class RawTechTemplate : RawTechBase
    {
        public FactionTypesExt faction = FactionTypesExt.GSO;
        public string factionName = null;
        [JsonIgnore]
        public string FactionActual => 
            (factionName != null && factionName.Length > 0) ? factionName : faction.ToString(); 
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
        public bool environ = false; // are we not a miner?
        public bool deployBoltsASAP = false; // press X on spawn

#if !EDITOR

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
            if (ManMods.inst.IsModdedCorp((FactionSubTypes)faction))
                factionName = ManMods.inst.FindCorpShortName((FactionSubTypes)faction);
            else
                factionName = faction.ToString();
            savedTech = MemoryToJSONExternal(TechToMemoryExternal(tech, ExceptionIfFail));
            bool anchored = tech.CheckIsAnchored();
            purposes = GetHandler(savedTech, faction, anchored, out terrain, out IntendedGrade);
            serialDataBlock = EncodeSerialData(tech);

            this.ValidateBlocksInTech();
        }
        public RawTechTemplate(string Name, List<RawBlockMem> mems, bool anchored = false)
        {
            techName = Name;
            List<RawBlockMem> mem = mems;
            faction = GetTopCorp(mem);
            savedTech = MemoryToJSONExternal(mems);
            purposes = GetHandler(mem, faction, anchored, out terrain, out IntendedGrade);
            serialDataBlock = new Dictionary<int, List<string>>();

            this.ValidateBlocksInTech();
        }

        public RawTechTemplate(string Name, string rawTech, bool anchored = false)
        {
            techName = Name;
            List<RawBlock> mem = JSONToMemoryExternalNonAlloc(rawTech);
            faction = GetTopCorp(mem);
            savedTech = rawTech;
            purposes = GetHandler(mem, faction, anchored, out terrain, out IntendedGrade);
            serialDataBlock = new Dictionary<int, List<string>>();

            this.ValidateBlocksInTech();
        }

        public RawTechTemplate(RawTech tech, bool anchored = false) : base(tech)
        {
            techName = tech.techName;
            faction = GetTopCorp(tech.savedTech);
            savedTech = MemoryToJSONExternal(tech.savedTech);
            purposes = GetHandler(tech.savedTech, faction, anchored, out terrain, out IntendedGrade);
            
            this.ValidateBlocksInTech();
        }
        public RawTech ToActive()
        {
            return new RawTech(this);
        }

        public bool IsDefense()
        {
            return purposes.Contains(BasePurpose.Defense);
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
        public override Tank SpawnRawTech(Vector3 pos, int Team, Vector3 forwards, bool snapTerrain = false, bool Charged = false, bool randomSkins = false)
        {
            if (savedTech == null)
                throw new NullReferenceException("TTExtUtil: SpawnTechExternal - Was handed a NULL Blueprint!");
            string baseBlueprint = savedTech;

            try
            {
                Tank theTech = InstantTech(pos, forwards, Team, techName, baseBlueprint, snapTerrain, 
                    ForceAnchor: purposes != null ? !purposes.Contains(BasePurpose.NotStationary) : false, Team == -1, randomSkins);
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
#endif

    }
}
