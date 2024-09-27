using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

namespace TerraTechETCUtil
{
    public class RawTechTemplate : RawTechBase
    {
        public string savedTech = "{\"t\":\"GSOAnchorFixed_111\",\"p\":{\"x\":0.0,\"y\":0.0,\"z\":0.0},\"r\":0}";

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
        public RawTechTemplate(TechData tech, bool MustBeExact = false)
        {
            techName = tech.Name;
            faction = GetTopCorp(tech);
            if (ManMods.inst.IsModdedCorp((FactionSubTypes)faction))
                factionName = ManMods.inst.FindCorpShortName((FactionSubTypes)faction);
            else
                factionName = faction.ToString();
            savedTech = MemoryToJSONExternal(TechToMemoryExternal(tech, MustBeExact));
            bool anchored = tech.CheckIsAnchored();
            purposes = GetHandler(savedTech, faction, anchored, out terrain, out IntendedGrade);
            serialDataBlock = EncodeSerialData(tech);

            this.ValidateBlocksInTech(!MustBeExact, MustBeExact);
        }
        public RawTechTemplate(string Name, List<RawBlockMem> mems, bool anchored = false, bool MustBeExact = false)
        {
            techName = Name;
            List<RawBlockMem> mem = mems;
            faction = GetTopCorp(mem);
            savedTech = MemoryToJSONExternal(mems);
            purposes = GetHandler(mem, faction, anchored, out terrain, out IntendedGrade);
            serialDataBlock = new Dictionary<int, List<string>>();

            this.ValidateBlocksInTech(!MustBeExact, MustBeExact);
        }

        public RawTechTemplate(string Name, string rawTech, bool anchored = false, bool MustBeExact = false)
        {
            techName = Name;
            List<RawBlock> mem = JSONToMemoryExternalNonAlloc(rawTech);
            faction = GetTopCorp(mem);
            savedTech = rawTech;
            purposes = GetHandler(mem, faction, anchored, out terrain, out IntendedGrade);
            serialDataBlock = new Dictionary<int, List<string>>();

            this.ValidateBlocksInTech(!MustBeExact, MustBeExact);
        }

        public RawTechTemplate(RawTech tech) : base(tech)
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
            savedTech = MemoryToJSONExternal(tech.savedTech);
            purposes = new HashSet<BasePurpose>(tech.purposes);
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
        public override Tank SpawnRawTech(Vector3 pos, int Team, Vector3 forwards, bool snapTerrain = false, 
            bool Charged = false, bool randomSkins = false, bool CanBeIncomplete = true)
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
