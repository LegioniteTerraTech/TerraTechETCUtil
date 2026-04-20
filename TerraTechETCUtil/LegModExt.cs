using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using FMOD.Studio;
using UnityEngine;
using TerraTechETCUtil.PatchBatch;

#if !EDITOR
using HarmonyLib;
#endif

#if !EDITOR
namespace TerraTechETCUtil
{
    /// <summary>
    /// The foundation for <see cref="TerraTechETCUtil"/>. Call <see cref="LegModExt.InsurePatches()"/> 
    /// to fully init <see cref="TerraTechETCUtil"/> and all of it's main functions.
    /// <para>Most of the functions start on demand</para>
    /// </summary>
    public class LegModExt
    {
        internal static string modID = "TerraTechModExt";
        internal static Harmony harmonyInstance = new Harmony("legionite." + modID.ToLower());

        private static bool patched = false;
        internal static bool BypassSetPieceChecks = false;
        /// <summary>
        /// Make FMOD spill beans for the sake of <i><b>SCIENCE</b></i>
        /// </summary>
        public static void GetSFX()
        {
            // Make FMOD spill beans
            if (Singleton.playerTank)
            {
                List<FMODEvent> batched = new List<FMODEvent>();
                TechAudio.TechAudioEventSimple[] simp = (TechAudio.TechAudioEventSimple[])typeof(TechAudio).GetField("m_SimpleEvents", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Singleton.playerTank.TechAudio);
                foreach (var item in simp)
                {
                    batched.Add(item.m_Event);
                }
                Type sekret = typeof(TechAudio).GetNestedType("TechAudioEvent",
                    BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.CreateInstance);
                FieldInfo sekret2 = sekret.GetField("m_Event",
                    BindingFlags.Public | BindingFlags.Instance);
                Array sek = (Array)typeof(TechAudio).GetField("m_Events", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Singleton.playerTank.TechAudio);
                for (int step = 0; step < sek.Length; step++)
                {
                    batched.Add((FMODEvent)sekret2.GetValue(sek.GetValue(new int[1] { step })));
                }
                Debug_TTExt.Log("Collected " + batched.Count + " entries: ");
                foreach (var item in batched)
                {
                    Debug_TTExt.Log(" - " + item.EventPath);
                }
                return;
            }
            InvokeHelper.Invoke(GetSFX, 0.5f);
        }

        private static int actionCount = 0;
        private static int actionCountCached = 0;
        internal static bool PreventDuplicates()
        {
            actionCount++;
            if (actionCountCached < actionCount)
            {
                actionCountCached++;
                return true;
            }
            return false;
        }

        private static string exportDir = Path.Combine(new DirectoryInfo(Application.dataPath).Parent.ToString(), "DataExtracts");
        internal static void TryExportTexture(Sprite tex)
        {
            if (PreventDuplicates())
            {
                if (tex)
                {
                    if (tex.texture)
                    {
                        if (!Directory.Exists(exportDir))
                            Directory.CreateDirectory(exportDir);
                        FileUtils.SaveTexture(tex.texture, Path.Combine(exportDir, tex.texture.name) + ".png");
                    }
                    else
                        Debug_TTExt.Log("Failed to export sprite - texture null");
                }
                else
                    Debug_TTExt.Log("Failed to export sprite - sprite null");
            }
        }
        internal static void TryExportTexture(Texture2D tex)
        {
            if (PreventDuplicates())
            {
                if (tex)
                {
                    if (!Directory.Exists(exportDir))
                        Directory.CreateDirectory(exportDir);
                    FileUtils.SaveTexture(tex, Path.Combine(exportDir, tex.name) + ".png");
                }
                else
                    Debug_TTExt.Log("Failed to export texture - texture null");
            }
        }


        private static FieldInfo FIDT = typeof(SpriteFetcher).GetField("m_DamageTypeIcons", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo FIDAT = typeof(SpriteFetcher).GetField("m_DamageableTypeIcons", BindingFlags.NonPublic | BindingFlags.Instance);
        private static void ExtractOnce()
        {
            actionCount = 0;
            try
            {
                /*
                if (PreventDuplicates())
                    InvokeHelper.Invoke(GetSFX, 5);
                */
                Sprite[] batcher = (Sprite[])FIDT.GetValue(ManUI.inst.m_SpriteFetcher);
                foreach (var item in batcher)
                {
                    TryExportTexture(item);
                }
                batcher = (Sprite[])FIDAT.GetValue(ManUI.inst.m_SpriteFetcher);
                foreach (var item in batcher)
                {
                    TryExportTexture(item);
                }
                Debug_TTExt.Log("ExtractOnce succeeded");
            }
            catch (Exception e)
            {
                Debug_TTExt.Log("ExtractOnce failed, trying again in 2 ingame seconds - " + e);
                InvokeHelper.Invoke(ExtractOnce, 2f);
            }
        }
        
        private static void DoUIAbilityTestCall()
        {
            UIHelpersExt.BigF5broningBanner("Ability - Test", false);
            ManSFX.inst.PlayUISFX(ManSFX.UISfxType.Craft);
        }
        private static void TryInitEnclosedLegModExtOptions()
        {
            LegModExtOptions.InitOptionsAndConfig();
            Debug_TTExt.Info("TerraTechETCUtil: Found NativeOptions & ConfigHelper");
        }

        /// <summary>
        /// Fully init <see cref="TerraTechETCUtil"/> and all of it's main functions.
        /// <para><b>Avoid catching the exception since TerraTechETCUtil will not try again later!</b></para>
        /// </summary>
        /// <exception cref="Exception">If TerraTechETCUtil completely failed to launch</exception>
        public static void InsurePatches()
        {
            if (patched)
                return;
            patched = true;
            try
            {
                //InvokeHelper.Invoke(ExtractOnce, 3f);
                UIHelpersExt.InsureNetHooks();

                ResourcesHelper.ModsPostLoadEvent.Subscribe(ManAudioExt.RegisterAllSounds);

                //new ManAbilities.AbilityButton("DebugPower", ManIngameWiki.BlocksSprite, DoUIAbilityTestCall, 1.5f);
                //ManAbilities.InitAbilityBar();
                try
                {
                    harmonyInstance.MassPatchAllWithin(typeof(AllProjectilePatches), modID, true);
                    Debug_TTExt.Log("TerraTechETCUtil: Mass patched projectiles");
                }
                catch (Exception e)
                {
                    throw new Exception("TerraTechETCUtil failed to perform projectile mass patching", e);
                }
                try
                {
                    harmonyInstance.MassPatchAllWithin(typeof(TerraTechUIPatches), modID, true);
                    Debug_TTExt.Log("TerraTechETCUtil: Mass patched TerraTech UI");
                }
                catch (Exception e)
                {
                    throw new Exception("TerraTechETCUtil failed to perform TerraTech UI mass patching", e);
                }
                try
                {
                    harmonyInstance.MassPatchAllWithin(typeof(UnityUIPatches), modID, true);
                    Debug_TTExt.Log("TerraTechETCUtil: Mass patched Unity UI");
                }
                catch (Exception e)
                {
                    throw new Exception("TerraTechETCUtil failed to perform Unity UI mass patching", e);
                }
                try
                {
                    harmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
                    Debug_TTExt.Info("TerraTechETCUtil: Patched precise");
                }
                catch (Exception e)
                {
                    throw new Exception("TerraTechETCUtil failed to perform finer patches", e);
                }
                try
                {
                    TryInitEnclosedLegModExtOptions();
                }
                catch (Exception e)
                {
                    Debug_TTExt.Log("TerraTechETCUtil failed to find NativeOptions & ConfigHelper");
                    Debug_TTExt.Log(e);
                }
                UIHelpersExt.Init();
                ResourcesHelper.ModsPreLoadEvent.Send();
                ResourcesHelper.ModsPreLoadEvent.Subscribe(WikiPageDamageStats.ResetAllCustomDamageables);

                ResourcesHelper.ModsPostLoadEvent.Subscribe(ManIngameWiki.InitWiki);
            }
            catch (Exception e)
            {
                ModStatusChecker.EpicFail();
                throw new Exception("TerraTechETCUtil failed to boot ENTIRELY", e);
            }
        }

        /// <summary>
        /// Deinit <see cref="TerraTechETCUtil"/> and most of it's main functions.
        /// <para>Important functions still stay enabled, like the localisation and on-demand functions</para>
        /// </summary>
        public static void RemovePatches()
        {
            if (!patched)
                return;
            ResourcesHelper.ModsPostLoadEvent.Unsubscribe(ManIngameWiki.InitWiki);
            ManWorldGeneratorExt.DeInit();

            harmonyInstance.MassUnPatchAllWithin(typeof(UnityUIPatches), modID);
            harmonyInstance.MassUnPatchAllWithin(typeof(TerraTechUIPatches), modID);
            harmonyInstance.MassUnPatchAllWithin(typeof(AllProjectilePatches), modID);
            try
            {
                harmonyInstance.UnpatchAll(harmonyInstance.Id);
                Debug_TTExt.Log("TerraTechETCUtil: Unpatched batch");
            }
            catch (Exception e)
            {
                Debug_TTExt.Log("TerraTechETCUtil failed to boot: " + e);
            }
            patched = false;
        }
    }
}
#endif