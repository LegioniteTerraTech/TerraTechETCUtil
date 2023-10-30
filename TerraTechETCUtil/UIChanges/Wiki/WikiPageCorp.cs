using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;

namespace TerraTechETCUtil
{
    public class WikiPageCorp : ManIngameWiki.WikiPage
    {
        public int corpID;
        public string corpShortName;
        internal GatheredInfo info = null;

        public WikiPageCorp(string modID, int corpID) : 
            base(modID, StringLookup.GetCorporationName((FactionSubTypes)corpID),
            ManUI.inst.GetModernCorpIcon((FactionSubTypes)corpID), "Corporations", ManIngameWiki.CorpsSprite)
        {
            this.corpID = corpID;
            if (ManMods.inst.IsModdedCorp((FactionSubTypes)corpID))
                corpShortName = ManMods.inst.FindCorpShortName((FactionSubTypes)corpID);
            else
                corpShortName = ((FactionSubTypes)corpID).ToString();
        }
        public WikiPageCorp(string modID, int corpID, ManIngameWiki.WikiPageGroup grouper) :
            base(modID, StringLookup.GetCorporationName((FactionSubTypes)corpID),
            ManUI.inst.GetModernCorpIcon((FactionSubTypes)corpID), grouper)
        {
            this.corpID = corpID;
            if (ManMods.inst.IsModdedCorp((FactionSubTypes)corpID))
                corpShortName = ManMods.inst.FindCorpShortName((FactionSubTypes)corpID);
            else
                corpShortName = ((FactionSubTypes)corpID).ToString();
        }
        public override void DisplaySidebar()
        {
            ButtonGUIDisp();
        }
        public override void DisplayGUI()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("AKA: ", AltUI.LabelBlackTitle);
            GUILayout.Label(corpShortName, AltUI.LabelBlackTitle);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            if (icon)
                AltUI.Sprite(icon, AltUI.TextfieldBorderedBlue, GUILayout.Height(128), GUILayout.Width(128));
            if (GUILayout.Button("Generate Report", AltUI.ButtonOrangeLarge, GUILayout.Height(40)))
            {
                info = new GatheredInfo(corpID);
            }
            if (info != null)
            {
                info.DisplayGUI();
            }
        }
        public override bool ReleaseAsMuchAsPossible()
        {
            if (info != null)
            {
                info = null;
                return true;
            }
            return false;
        }

        internal class GatheredInfo
        {
            private const int iterationsPerFrame = 48;
            private readonly FactionSubTypes corpID;

            private BlockTypes iterating = 0;

            private List<WikiPageBlock> blocks = new List<WikiPageBlock>();

            public double CombinedHealth = 0;
            public long CombinedCost = 0;
            public long CombinedVolume = 0;
            public double CombinedMass = 0;
            public double CombinedFragility = 0;

            public double HPToCostDensity = -1;
            public double BlockToHPDensity = -1;
            public double BlockToMassDensity = -1;
            public double AvgFragility = -1;


            /// <summary>
            /// Gather ALL the details by mathmatical, computational BRUTE-FORCE
            /// </summary>
            public GatheredInfo(int corpID)
            {
                this.corpID = (FactionSubTypes)corpID;

                if (ManMods.inst.IsModdedCorp(this.corpID))
                {
                    iterating = (BlockTypes)ManMods.k_FIRST_MODDED_BLOCK_ID;
                }

                ManGameMode.inst.ModeCleanUpEvent.Subscribe(ABORT);
                InvokeHelper.Invoke(Calc,0.1f);
            }
            internal void Calc()
            {
                if (iterating == (BlockTypes)(-1))
                    return;
                if ((int)iterating < Enum.GetValues(typeof(BlockTypes)).Length)
                {
                    for (int i = 0; i < iterationsPerFrame; i++)
                    {
                        if (CalcStep())
                        {
                            OnDone();
                            return;
                        }
                    }
                }
                else if (iterating > (BlockTypes)ManMods.k_FIRST_MODDED_BLOCK_ID)
                {
                    for (int i = 0; i < iterationsPerFrame; i++)
                    {
                        if (CalcStepModded())
                        {
                            OnDone();
                            return;
                        }
                    }
                }
                InvokeHelper.Invoke(Calc, 0.1f);
            }
            internal void OnDone()
            {
                ManGameMode.inst.ModeCleanUpEvent.Unsubscribe(ABORT);
                try
                {
                    HPToCostDensity = CombinedHealth / CombinedCost;
                }
                catch 
                {
                }
                try
                {
                    BlockToHPDensity = CombinedHealth / CombinedVolume;
                }
                catch
                {
                }
                try
                {
                    BlockToMassDensity = CombinedMass / CombinedVolume;
                }
                catch
                {
                }
                try
                {
                    AvgFragility = CombinedFragility / blocks.Count;
                }
                catch
                {
                }
            }
            internal void ABORT(Mode unused)
            {
                iterating = (BlockTypes)(-1);
                ManGameMode.inst.ModeCleanUpEvent.Unsubscribe(ABORT);
            }
            internal void TryGatherPrefab(BlockTypes toTry)
            {
                var tryMe = ManSpawn.inst.GetBlockPrefab(toTry);
                if (tryMe)
                {
                    CombinedCost += RecipeManager.inst.GetBlockBuyPrice(toTry);
                    CombinedVolume += tryMe.filledCells.Length;
                    CombinedMass += tryMe.m_DefaultMass;
                    var MD = tryMe.GetComponent<ModuleDamage>();
                    CombinedHealth += MD.maxHealth;
                    CombinedFragility += MD.m_DamageDetachFragility;
                }
            }
            internal bool CalcStep()
            {
                if ((int)iterating < Enum.GetValues(typeof(BlockTypes)).Length)
                {
                    if (ManSpawn.inst.GetCorporation(iterating) == corpID)
                    {
                        blocks.Add(ManIngameWiki.GetBlockPage(ManMods.inst.FindBlockName((int)iterating)));
                        TryGatherPrefab(iterating);
                    }
                    iterating++;
                    return false;
                }
                return true;
            }
            internal bool CalcStepModded()
            {
                if (ManMods.inst.IsModdedBlock(iterating))
                {
                    if (ManSpawn.inst.GetCorporation(iterating) == corpID)
                    {
                        blocks.Add(ManIngameWiki.GetBlockPage(ManMods.inst.FindBlockName((int)iterating)));
                        TryGatherPrefab(iterating);
                    }
                    iterating++;
                    return false;
                }
                return true;
            }

            private void TinyStringDisp(string desc, double val)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(desc, AltUI.LabelBlack);
                GUILayout.Label(val.ToString(), AltUI.TextfieldBlackAdjusted);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }


            internal void DisplayGUI()
            {
                if (BlockToHPDensity != -1)
                {
                    TinyStringDisp("Total Blocks: ", blocks.Count);
                    TinyStringDisp("Total Health: ", CombinedHealth);
                    TinyStringDisp("Total Volume: ", CombinedVolume);
                    TinyStringDisp("Total Cost: ", CombinedCost);
                    TinyStringDisp("Health per Cost Average: ", HPToCostDensity);
                    TinyStringDisp("Health per Volume Average: ", BlockToHPDensity);
                    TinyStringDisp("Averaged Mass: ", BlockToMassDensity);
                    TinyStringDisp("Averaged Fragility: ", AvgFragility);
                }
                else
                {
                    TinyStringDisp("Total Blocks: ", blocks.Count);
                    TinyStringDisp("Total Health: ", CombinedHealth);
                    TinyStringDisp("Total Volume: ", CombinedVolume);
                    TinyStringDisp("Total Mass: ", CombinedMass);
                    TinyStringDisp("Total Fragility: ", CombinedFragility);
                }
            }
        }
    }

}
