using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;

namespace TerraTechETCUtil
{
    internal class AttractPatches
    {
        internal static class ModeAttractPatches
        {
            internal static Type target = typeof(ModeAttract);

            // this is a VERY big mod
            //   we must make it look big like it is
            //RestartAttract - Checking title techs
            private static void UpdateModeImpl_Prefix(ModeAttract __instance)
            {
                SpecialAttract.CheckShouldRestart(__instance);
            }

            //SetupTerrainCustom -  Setup main menu scene
            private static bool SetupTerrain_Prefix(ModeAttract __instance)
            {
                return SpecialAttract.SetupTerrain(__instance);
            }

            //ThrowCoolAIInAttract - Setup main menu techs
            private static bool SetupTechs_Prefix(ModeAttract __instance)
            {
                return SpecialAttract.SetupTechsStart(__instance);
            }
            private static void SetupTechs_Postfix(ModeAttract __instance)
            {
                SpecialAttract.SetupTechsEnd(__instance);
            }

        }
    }
    /// <summary>
    /// Add new Attract Types using this to showcase cool stuff on the title screen I guess
    /// </summary>
    public static class SpecialAttract
    {
        public class AttractInfo
        {
            public readonly int ID;
            public readonly string name;
            public readonly float weight;
            public readonly bool restart;
            public readonly Func<ModeAttract, bool> preStart;
            public readonly Func<ModeAttract, bool> start;
            public readonly Action<ModeAttract> end;
            public readonly int time;

            /// <summary>
            /// Weight CANNOT be negative or 0!
            /// </summary>
            /// <param name="Name"></param>
            /// <param name="Weight"></param>
            public AttractInfo(string Name, float Weight, Func<ModeAttract, bool> PreStart = null, 
                Func<ModeAttract, bool> Start = null, 
                Action<ModeAttract> End = null, bool restartBeyond125 = false, int timeOfDay = -1)
            {
                if (Name.NullOrEmpty())
                    throw new ArgumentException("AttractInfo.Name CANNOT be null or empty!");
                if (Weight <= 0)
                    throw new ArgumentOutOfRangeException("AttractInfo.Weight CANNOT be negative or 0!");
                if (!Active)
                {
                    Active = true;
                    InvokeHelper.InvokeSingleRepeat(UpdateStatic, 0.3f);
                    if (!MassPatcher.MassPatchAllWithin(LegModExt.harmonyInstance, typeof(AttractPatches), "TerraTechModExt"))
                        Debug_TTExt.FatalError("Error on patching SpecialAttract");
                    new AttractInfo("Default", 1.0f);
                }
                ID = attracts;
                attracts++;
                name = Name;
                weight = Weight;
                start = Start;
                end = End;
                preStart = PreStart;
                restart = restartBeyond125;
                time = timeOfDay;
                weightedAttracts.Add(ID, this);
                Debug_TTExt.Log("Registered attract " + name + " with weight " + weight);
                RecalcWeightCache();
            }
            public void Release()
            {
                weightedAttracts.Remove(ID);
                RecalcWeightCache();
            }
        }
        private static bool Active = false;

        public static int CurAttractID => curAttractID;
        private static int curAttractID = 0;

        public static int ForcedAttractID = -1;

        private static readonly FieldInfo spawnNum = typeof(ModeAttract).GetField("spawnIndex", BindingFlags.NonPublic | BindingFlags.Instance),
            rTime = typeof(ModeAttract).GetField("resetAtTime", BindingFlags.NonPublic | BindingFlags.Instance);

        private static Dictionary<int, AttractInfo> weightedAttracts = new Dictionary<int, AttractInfo>();
        private static ModeAttract attractor;
        private static float totalWeightCached = 1;
        private static int attracts = 0;
        public static Vector3 AttractPosition = Vector3.zero;

        private static bool NeedsRestart = false;

        private static void UpdateStatic()
        {
            if (ManGameMode.inst.GetCurrentGameType() != ManGameMode.GameType.Attract)
            {
                //DebugTAC_AI.Log("Resetting Camera...");
                CameraManager.inst.GetCamera<TankCamera>().SetFollowTech(null);
                UseFollowCam = false;
            }
        }

        private static void RecalcWeightCache()
        {
            totalWeightCached = 0;
            foreach (var item in weightedAttracts.Values)
            {
                totalWeightCached += item.weight;
            }
        }
        internal static int WeightedDetermineRAND()
        {
            float select = UnityEngine.Random.Range(0, totalWeightCached);
            foreach (var item in weightedAttracts.Values)
            {
                select -= item.weight;
                if (select <= 0)
                    return item.ID;
            }
            return 0;
        }

        private static List<Tank> techLazyIterator = new List<Tank>();
        internal static void CheckShouldRestart(ModeAttract __instance)
        {
            FieldInfo state = typeof(ModeAttract).GetField("m_State", BindingFlags.NonPublic | BindingFlags.Instance);
            int mode = (int)state.GetValue(__instance);
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Equals))
            {
                UILoadingScreenHints.SuppressNextHint = true;
                Singleton.Manager<ManUI>.inst.FadeToBlack();
                state.SetValue(__instance, 3);
            }
            if (mode == 2)
            {
                var attract = weightedAttracts[curAttractID];
                if (attract.restart)
                {
                    bool restart = false;
                    foreach (Tank tonk in Singleton.Manager<ManTechs>.inst.CurrentTechs)
                    {
                        if ((tonk.boundsCentreWorldNoCheck - Singleton.cameraTrans.position).magnitude > 125)
                        {
                            Debug_TTExt.Log("CheckShouldRestart(restart) - tech " + tonk.name + " too far");
                            restart = true;
                        }
                    }
                    if (restart == true)
                    {
                        UILoadingScreenHints.SuppressNextHint = true;
                        Singleton.Manager<ManUI>.inst.FadeToBlack();
                        state.SetValue(__instance, 3);
                        Debug_TTExt.Log("CheckShouldRestart(restart) - Restarted due to tech too far");
                    }
                }
                else
                {
                    bool restart = true;
                    try
                    {
                        foreach (Tank tonk in Singleton.Manager<ManTechs>.inst.IterateTechs())
                        {
                            if (tonk.Weapons.GetFirstWeapon().IsNotNull())
                            {
                                foreach (Tank tonk2 in Singleton.Manager<ManTechs>.inst.CurrentTechs)
                                {
                                    if (tonk.IsEnemy(tonk2.Team))
                                        restart = false;
                                }
                            }
                            if (tonk.IsSleeping)
                            {
                                techLazyIterator.Add(tonk);
                            }
                        }
                        foreach (var tonk in techLazyIterator)
                        {
                            foreach (TankBlock block in tonk.blockman.IterateBlocks())
                            {
                                block.damage.SelfDestruct(0.5f);
                            }
                            tonk.blockman.Disintegrate(true, false);
                        }
                    }
                    finally
                    {
                        techLazyIterator.Clear();
                    }
                    if (restart == true)
                    {
                        UILoadingScreenHints.SuppressNextHint = true;
                        Singleton.Manager<ManUI>.inst.FadeToBlack();
                        state.SetValue(__instance, 3);
                        Debug_TTExt.Log("CheckShouldRestart - Restarted due to techs sleeping or too little");
                    }
                }
            }
            if (UseFollowCam)
            {
                if (FollowTech)
                {
                    if (!FollowTech.visible.isActive)
                    {
                        TankCamera instCam = CameraManager.inst.GetCamera<TankCamera>();
                        Tank nextTech = null;
                        foreach (var item in ManTechs.inst.IterateTechs())
                        {
                            if (item.blockman.blockCount > 5)
                            {
                                nextTech = item;
                                break;
                            }
                        }
                        if (nextTech)
                        {
                            FollowTech = nextTech;
                            instCam.ManualZoom(FollowTech.blockBounds.size.magnitude * 1.5f);
                            instCam.SetFollowTech(FollowTech);
                        }
                    }
                    else
                    {
                        if (FollowTech.blockman.blockCount < 6)
                            FollowTech = null;
                        //var help = FollowTech.GetComponent<AI.TankAIHelper>();
                        //if (help && help.gr)
                    }
                }
                else
                {
                    TankCamera instCam = CameraManager.inst.GetCamera<TankCamera>();
                    Tank nextTech = null;
                    foreach (var item in ManTechs.inst.IterateTechs())
                    {
                        if (!nextTech && item.blockman.blockCount > 5)
                        {
                            nextTech = item;
                            break;
                        }
                    }
                    if (nextTech)
                    {
                        FollowTech = nextTech;
                        instCam.ManualZoom(FollowTech.blockBounds.size.magnitude * 1.5f);
                        instCam.SetFollowTech(FollowTech);
                    }
                }
            }
        }
        internal static bool UseFollowCam = true;
        private static Tank FollowTech;

        internal static bool SetupTerrain(ModeAttract __instance)
        {
            // Testing
            UseFollowCam = false;
            CameraManager.inst.Switch(CameraManager.inst.GetCamera<FramingCamera>());

            AttractInfo attract;
            if (NeedsRestart)
            {
                NeedsRestart = false;
                attract = weightedAttracts[curAttractID];
            }
            else
            {
                if (ForcedAttractID != -1)
                {
                    if (weightedAttracts.ContainsKey(ForcedAttractID))
                        curAttractID = ForcedAttractID;
                    else
                        curAttractID = WeightedDetermineRAND();
                    attract = weightedAttracts[curAttractID];
                    if (attract.preStart != null)
                    {
                        NeedsRestart = true;
                        if (!attract.preStart.Invoke(__instance))
                        {
                            Debug_TTExt.Log("SpecialAttract: Pre-Setup for attract type " + attract.name);
                            return false;
                        }
                    }
                }
                else
                {
                    curAttractID = WeightedDetermineRAND();
                    attract = weightedAttracts[curAttractID];
                    if (attract.preStart != null)
                    {
                        NeedsRestart = true;
                        if (!attract.preStart.Invoke(__instance))
                        {
                            Debug_TTExt.Log("SpecialAttract: Pre-Setup for attract type " + attract.name);
                            return false;
                        }
                    }
                }
            }
            Debug_TTExt.Log("SpecialAttract: Pre-Setup for attract type " + attract.name);
            if (curAttractID == 0)
                return true;
            int spawnIndex = (int)spawnNum.GetValue(__instance);
            Singleton.Manager<ManWorld>.inst.SeedString = null;
            Singleton.Manager<ManGameMode>.inst.RegenerateWorld(__instance.spawns[spawnIndex].biomeMap, __instance.spawns[spawnIndex].cameraSpawn.position, __instance.spawns[spawnIndex].cameraSpawn.orientation);
            Singleton.Manager<ManTimeOfDay>.inst.EnableSkyDome(enable: true);
            Singleton.Manager<ManTimeOfDay>.inst.EnableTimeProgression(enable: false);
            if (attract.time == -1)
                Singleton.Manager<ManTimeOfDay>.inst.SetTimeOfDay(UnityEngine.Random.Range(8, 18), 0, 0);//11 is midday
            else
                Singleton.Manager<ManTimeOfDay>.inst.SetTimeOfDay(attract.time, 0, 0);//11 is midday
            return false;
        }
        public static void SetupTechCam(Tank target = null)
        {
            UseFollowCam = true;
            //Vector3 frameCamPos = CameraManager.inst.GetCamera<FramingCamera>().transform.position;
            //Quaternion frameCamRot = CameraManager.inst.GetCamera<FramingCamera>().transform.rotation;
            TankCamera instCam = CameraManager.inst.GetCamera<TankCamera>();
            CameraManager.inst.Switch(instCam);
            if (target)
            {
                FollowTech = target;
            }
            else
            {
                FollowTech = null;
                foreach (var item in ManTechs.inst.IterateTechs())
                {
                    if (!FollowTech)
                        FollowTech = item;
                    if (item.rbody)
                    {
                        item.rbody.velocity += item.rootBlockTrans.forward * 45;
                    }
                }
            }
            if (FollowTech)
            {
                instCam.ManualZoom(FollowTech.blockBounds.size.magnitude * 1.5f);
                //instCam.SetFollowSpringStrength(0.05f);
                instCam.SetFollowTech(FollowTech);
                Quaternion look = Quaternion.LookRotation(FollowTech.trans.forward);
                CameraManager.inst.ResetCamera(FollowTech.trans.position + (look * new Vector3(-12, 5, 0)), look);
            }
        }

        private static List<Vector3> tanksToConsider = new List<Vector3>();
        public static List<Vector3> GetRandomStartingPositions()
        {
            tanksToConsider.Clear();

            int spawnIndex = (int)spawnNum.GetValue(attractor);
            Vector3 spawn = attractor.spawns[spawnIndex].vehicleSpawnCentre.position;
            Singleton.Manager<ManWorld>.inst.GetTerrainHeight(spawn, out float height);
            spawn.y = height;

            int numToSpawn = 3;
            float rad = 360f / (float)numToSpawn;
            for (int step = 0; step < numToSpawn; step++)
            {
                Quaternion rotation = Quaternion.AngleAxis(UnityEngine.Random.value * 360f, Vector3.up);
                Vector3 offset = Quaternion.Euler(0f, (float)step * rad, 0f) * Vector3.forward * 16;
                tanksToConsider.Add(attractor.spawns[spawnIndex].vehicleSpawnCentre.position + offset);
            }
            return tanksToConsider;
        }

        // TECH COMBAT
        internal static bool SetupTechsStart(ModeAttract __instance)
        {
            attractor = __instance;
            AttractInfo attract = default;
            try
            {
                if (curAttractID == 0)
                    return true;
                attract = weightedAttracts[curAttractID];
                Debug_TTExt.Log("SpecialAttract: Setup for attract type " + attract.name);

                if (attract.start != null && !attract.start.Invoke(__instance))
                        return false;
            }
            catch (Exception e)
            {
                try
                {
                    Debug_TTExt.Log("SpecialAttract: FAILED on attract name " + attract.name + ", Start  - " + e);
                }
                catch
                {
                    Debug_TTExt.Log("SpecialAttract: FAILED on attract number " + curAttractID.ToString() + ", Start  - " + e);
                }
            }
            return true;
        }
        internal static void SetupTechsEnd(ModeAttract __instance)
        {
            AttractInfo attract = default;
            try
            {
                rTime.SetValue(__instance, Time.time + __instance.resetTime);
                if (curAttractID == 0)
                    return;
                attract = weightedAttracts[curAttractID];
                Debug_TTExt.Log("SpecialAttract: Post-Setup for attract type " + attract.name);

                if (attract.end != null)
                    attract.end(__instance);
            }
            catch (Exception e)
            {
                try
                {
                    Debug_TTExt.Log("SpecialAttract: FAILED on attract name " + attract.name + ", End  - " + e);
                }
                catch
                {
                    Debug_TTExt.Log("SpecialAttract: FAILED on attract number " + curAttractID.ToString() + ", End  - " + e);
                }
            }
        }
    }
}
