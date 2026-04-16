using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

namespace TerraTechETCUtil
{
    /// <summary>
    /// <inheritdoc/>
    /// <para>This is the ACTIVE <see cref="RawTechBase"/>. For the serialized version see <see cref="RawTechTemplate"/></para>
    /// </summary>
    public class RawTechTemplate : RawTechBase
    {
        /// <summary>
        /// Lone anchor default
        /// </summary>
        public string savedTech = "{\"t\":\"GSOAnchorFixed_111\",\"p\":{\"x\":0.0,\"y\":0.0,\"z\":0.0},\"r\":0}";

#if !EDITOR
        /// <summary>
        /// Get the BB cost of this Tech
        /// </summary>
        /// <param name="BT"></param>

        public static implicit operator int(RawTechTemplate BT)
        {
            if (BT.baseCost == 0 && !BT.savedTech.NullOrEmpty())
                BT.baseCost = RawTechTemplate.GetBBCost(BT.savedTech);
            return BT.baseCost;
        }

        /// <summary>
        /// Create a empty <see cref="RawTechTemplate"/>. <b>Not recommended.</b>
        /// </summary>
        public RawTechTemplate()
        {
        }
        /// <summary>
        /// Create a <see cref="RawTechTemplate"/> based on given <see cref="TechData"/>
        /// </summary>
        /// <param name="tech"></param>
        /// <param name="MustBeExact"></param>
        public RawTechTemplate(TechData tech, bool MustBeExact = false)
        {
            techName = tech.Name;
#pragma warning disable CS0612 // Type or member is obsolete
            faction = GetTopCorp(tech);
            if (ManMods.inst.IsModdedCorp((FactionSubTypes)faction))
                factionName = ManMods.inst.FindCorpShortName((FactionSubTypes)faction);
            else
                factionName = faction.ToString();
            savedTech = MemoryToJSONExternal(TechToMemoryExternal(tech, MustBeExact));
            bool anchored = tech.CheckIsAnchored();
            purposes = GetHandler(savedTech, faction, anchored, out terrain, out IntendedGrade);
#pragma warning restore CS0612 // Type or member is obsolete
            serialDataBlock = EncodeSerialData(tech);

            this.ValidateBlocksInTech(!MustBeExact, MustBeExact);
        }
        /// <summary>
        /// Create a <see cref="RawTechTemplate"/> based on a given <see cref="RawBlockMem"/> list
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="mems"></param>
        /// <param name="anchored"></param>
        /// <param name="MustBeExact"></param>
        public RawTechTemplate(string Name, List<RawBlockMem> mems, bool anchored = false, bool MustBeExact = false)
        {
            techName = Name;
            List<RawBlockMem> mem = mems;
#pragma warning disable CS0612 // Type or member is obsolete
            faction = GetTopCorp(mem);
            savedTech = MemoryToJSONExternal(mems);
            purposes = GetHandler(mem, faction, anchored, out terrain, out IntendedGrade);
#pragma warning restore CS0612 // Type or member is obsolete
            serialDataBlock = new Dictionary<int, List<string>>();

            this.ValidateBlocksInTech(!MustBeExact, MustBeExact);
        }

        /// <summary>
        /// Create a <see cref="RawTechTemplate"/> based on a given <see cref="RawTech"/> JSON string
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="rawTech"></param>
        /// <param name="anchored"></param>
        /// <param name="MustBeExact"></param>
        public RawTechTemplate(string Name, string rawTech, bool anchored = false, bool MustBeExact = false)
        {
            techName = Name;
            List<RawBlock> mem = JSONToMemoryExternalNonAlloc(rawTech);
#pragma warning disable CS0612 // Type or member is obsolete
            faction = GetTopCorp(mem);
            savedTech = rawTech;
            purposes = GetHandler(mem, faction, anchored, out terrain, out IntendedGrade);
#pragma warning restore CS0612 // Type or member is obsolete
            serialDataBlock = new Dictionary<int, List<string>>();

            this.ValidateBlocksInTech(!MustBeExact, MustBeExact);
        }

        /// <summary>
        /// Convert a <see cref="RawTech"/> into a <see cref="RawTechTemplate"/> for quick loading
        /// </summary>
        /// <param name="tech">Copy from target</param>
        public RawTechTemplate(RawTech tech) : base(tech)
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
            savedTech = MemoryToJSONExternal(tech.savedTech);
            purposes = new HashSet<BasePurpose>(tech.purposes);
        }
        /// <summary>
        /// Convert this to a <see cref="RawTechTemplate"/> for quick loading in-game
        /// </summary>
        /// <returns></returns>
        public RawTech ToActive()
        {
            return new RawTech(this);
        }

        /// <summary>
        /// True if this is anchored and armed
        /// </summary>
        /// <returns></returns>
        public bool IsDefense()
        {
            return purposes.Contains(BasePurpose.Defense);
        }



        /// <inheritdoc/>
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
