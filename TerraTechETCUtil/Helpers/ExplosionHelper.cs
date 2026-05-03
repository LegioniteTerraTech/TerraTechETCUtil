using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static MapGenerator;
using static TerraTechETCUtil.ResourcesHelper;

namespace TerraTechETCUtil
{
    /// <summary>
    /// Goto for explosion effects
    /// <para>Aka "<b>Pyromanic</b>"</para>
    /// </summary>
    public class ExplosionHelper
    {
        /// <summary>
        /// Explosion getter
        /// </summary>
        public static readonly FieldInfo explodeType = typeof(Explosion).GetField("m_DamageType", BindingFlags.NonPublic | BindingFlags.Instance);
        /// <summary>
        /// Explosion getter
        /// </summary>
        public static readonly FieldInfo explodeSFX = typeof(Explosion).GetField("m_ExplosionType", BindingFlags.NonPublic | BindingFlags.Instance);
        /// <summary>
        /// Explosion getter
        /// </summary>
        public static readonly FieldInfo explodeSFXSize = typeof(Explosion).GetField("m_ExplosionSize", BindingFlags.NonPublic | BindingFlags.Instance);


        /// <summary>
        /// Explosion type
        /// </summary>
        public enum Type
        {
            /// <summary> Explode like normal </summary>
            Explosive,
            /// <summary> Explode like energy </summary>
            EnergyBlue,
            /// <summary> Explode like energy </summary>
            EnergyRed,
            /// <summary> Explode like battery </summary>
            Chemical,
            /// <summary> Explode like fuel tank </summary>
            Oil,
            /// <summary> Explode like GSO block </summary>
            Debris_GSO,
            /// <summary> Explode like GC block </summary>
            Debris_GC,
            /// <summary> Explode like RR block </summary>
            Debris_RR,
            /// <summary> Explode like VEN block </summary>
            Debris_VEN,
            /// <summary> Explode like HE block </summary>
            Debris_HE,
            /// <summary> Explode like BF block </summary>
            Debris_BF,
            /// <summary> Explode like SJ block </summary>
            Debris_SJ,
        }
        /// <summary>
        /// Entry for an explosion
        /// </summary>
        public class ExplosionEntry
        {
            /// <summary> The explosion prefab</summary>
            public readonly Transform prefab;

            /// <summary> The explosion prefab</summary>
            public readonly ManDamage.DamageType DamageType;
            /// <summary> The explosion SFX type to use</summary>
            public readonly ManSFX.ExplosionType ExplodeSFX;
            /// <summary> The explosion SFX size to use</summary>
            public readonly ManSFX.ExplosionSize ExplodeSFXSize;

            /// <summary> The max radius this explosion will reach out from </summary>
            public readonly float EffectRadius;
            /// <summary> The [0 ~ 1] radius range where the explosion deals its max damage inwards from where 0 is the center and 1 is the outwards reach</summary>
            public readonly float EffectRadiusMaxStrengthPercent;
            /// <summary> The maximum damage this explosion can inflict</summary>
            public readonly float MaxDamage;
            /// <summary> The maximum launching force this explosion can inflict</summary>
            public readonly float MaxImpulse;
            internal ExplosionEntry(Transform trans, float strength)
            {
                prefab = trans;
                Explosion explo = trans.GetComponent<Explosion>();
                if (explo == null)
                {
                    explo = trans.gameObject.AddComponent<Explosion>();
                    explo.m_EffectRadius = strength / 500f;
                    explo.m_EffectRadiusMaxStrength = 0.2f;
                    explo.m_MaxDamageStrength = strength;
                    explo.m_MaxImpulseStrength = strength / 25f;
                }
                EffectRadius = explo.m_EffectRadius;
                EffectRadiusMaxStrengthPercent = explo.m_EffectRadiusMaxStrength;
                MaxDamage = explo.m_MaxDamageStrength;
                MaxImpulse = explo.m_MaxImpulseStrength;
                ExplodeSFX = (ManSFX.ExplosionType)explodeSFX.GetValue(explo);
                ExplodeSFXSize = (ManSFX.ExplosionSize)explodeSFXSize.GetValue(explo);
                DamageType = (ManDamage.DamageType)explodeType.GetValue(explo);
            }
        }

        /// <summary>
        /// Dirty lookup of explosions
        /// </summary>
        public static Dictionary<Type, List<KeyValuePair<float, ExplosionEntry>>> Explosions
        {
            get
            {
                if (_Explosions == null)
                    GetExplosions();
                return _Explosions;
            }
        }
        private static Dictionary<Type, List<KeyValuePair<float, ExplosionEntry>>> _Explosions = null;

        private static void GetExplosions()
        {
            _Explosions = new Dictionary<Type, List<KeyValuePair<float, ExplosionEntry>>>();
            Transform exploder;

            // Normal
            var explo = new List<KeyValuePair<float, ExplosionEntry>>();
            FetchExplosionWeapon(BlockTypes.GSOBigBertha_845, out exploder);
            explo.Add(new KeyValuePair<float, ExplosionEntry>(200, new ExplosionEntry(exploder, 200)));
            FetchExplosionWeapon(BlockTypes.HE_Cruise_Missile_51_121, out exploder);
            explo.Add(new KeyValuePair<float, ExplosionEntry>(150, new ExplosionEntry(exploder, 150)));
            FetchExplosionWeapon(BlockTypes.GSOMediumCannon_222, out exploder);
            explo.Add(new KeyValuePair<float, ExplosionEntry>(100, new ExplosionEntry(exploder, 100)));
            FetchExplosionWeapon(BlockTypes.VENMicroMissile_112, out exploder);
            explo.Add(new KeyValuePair<float, ExplosionEntry>(50, new ExplosionEntry(exploder, 50)));
            FetchExplosionWeapon(BlockTypes.GSOMGunFixed_111, out exploder);
            explo.Add(new KeyValuePair<float, ExplosionEntry>(0, new ExplosionEntry(exploder, 10)));
            explo = explo.OrderByDescending(x => x.Key).ToList();// CALLED ONCE
            _Explosions.Add(Type.Explosive, explo);

            // Energy blue
            explo = new List<KeyValuePair<float, ExplosionEntry>>();
            FetchExplosionWeapon(BlockTypes.EXP_PrototypeGun_02_626, out exploder);
            explo.Add(new KeyValuePair<float, ExplosionEntry>(200, new ExplosionEntry(exploder, 200)));
            FetchExplosionWeapon(BlockTypes.BF_Laser_Deathray_214, out exploder);
            explo.Add(new KeyValuePair<float, ExplosionEntry>(125, new ExplosionEntry(exploder, 125)));
            FetchExplosionWeapon(BlockTypes.BF_Laser_Gatling_423, out exploder);
            explo.Add(new KeyValuePair<float, ExplosionEntry>(50, new ExplosionEntry(exploder, 50)));
            FetchExplosionWeapon(BlockTypes.BF_Laser_112, out exploder);
            explo.Add(new KeyValuePair<float, ExplosionEntry>(0, new ExplosionEntry(exploder, 10)));
            _Explosions.Add(Type.EnergyBlue, explo);

            // Energy red
            explo = new List<KeyValuePair<float, ExplosionEntry>>();
            FetchExplosionBlock(BlockTypes.GC_Battery_424, out exploder);
            explo.Add(new KeyValuePair<float, ExplosionEntry>(200, new ExplosionEntry(exploder, 200)));
            FetchExplosionBlock(BlockTypes.GC_SamSite_Shield_222, out exploder);
            explo.Add(new KeyValuePair<float, ExplosionEntry>(125, new ExplosionEntry(exploder, 125)));
            FetchExplosionWeapon(BlockTypes.HE_LaserZeus_222, out exploder);
            explo.Add(new KeyValuePair<float, ExplosionEntry>(50, new ExplosionEntry(exploder, 50)));
            FetchExplosionWeapon(BlockTypes.GSOLaserFixed_111, out exploder);
            explo.Add(new KeyValuePair<float, ExplosionEntry>(0, new ExplosionEntry(exploder, 10)));
            _Explosions.Add(Type.EnergyRed, explo);

            // Chemical green
            explo = new List<KeyValuePair<float, ExplosionEntry>>();
            FetchExplosionBlock(BlockTypes.GCBattery_222, out exploder);
            explo.Add(new KeyValuePair<float, ExplosionEntry>(200, new ExplosionEntry(exploder, 200)));
            FetchExplosionBlock(BlockTypes.BF_Battery_112, out exploder);
            explo.Add(new KeyValuePair<float, ExplosionEntry>(100, new ExplosionEntry(exploder, 100)));
            FetchExplosionBlock(BlockTypes.VENBattery_111, out exploder);
            explo.Add(new KeyValuePair<float, ExplosionEntry>(0, new ExplosionEntry(exploder, 10)));
            _Explosions.Add(Type.Chemical, explo);

            // Oil orange
            explo = new List<KeyValuePair<float, ExplosionEntry>>();
            FetchExplosionBlock(BlockTypes.GSOFuelTank_222, out exploder);
            explo.Add(new KeyValuePair<float, ExplosionEntry>(200, new ExplosionEntry(exploder, 200)));
            FetchExplosionBlock(BlockTypes.BF_FuelTank_212, out exploder);
            explo.Add(new KeyValuePair<float, ExplosionEntry>(100, new ExplosionEntry(exploder, 100)));
            FetchExplosionBlock(BlockTypes.VENFuelPod_111, out exploder);
            explo.Add(new KeyValuePair<float, ExplosionEntry>(0, new ExplosionEntry(exploder, 10)));
            _Explosions.Add(Type.Oil, explo);

            // Debris GSO
            explo = new List<KeyValuePair<float, ExplosionEntry>>();
            FetchExplosionBlock(BlockTypes.GSOBigBertha_845, out exploder);
            explo.Add(new KeyValuePair<float, ExplosionEntry>(200, new ExplosionEntry(exploder, 200)));
            FetchExplosionBlock(BlockTypes.GCFlail_446, out exploder);
            explo.Add(new KeyValuePair<float, ExplosionEntry>(125, new ExplosionEntry(exploder, 125)));
            FetchExplosionBlock(BlockTypes.GCBlock_222, out exploder);
            explo.Add(new KeyValuePair<float, ExplosionEntry>(50, new ExplosionEntry(exploder, 50)));
            FetchExplosionBlock(BlockTypes.GSOBlock_111, out exploder);
            explo.Add(new KeyValuePair<float, ExplosionEntry>(0, new ExplosionEntry(exploder, 10)));
            _Explosions.Add(Type.Debris_GSO, explo);

            // Debris GC
            explo = new List<KeyValuePair<float, ExplosionEntry>>();
            FetchExplosionBlock(BlockTypes.GCFlail_446, out exploder);
            explo.Add(new KeyValuePair<float, ExplosionEntry>(200, new ExplosionEntry(exploder, 200)));
            FetchExplosionBlock(BlockTypes.GCWheel_Stupid_588, out exploder);
            explo.Add(new KeyValuePair<float, ExplosionEntry>(125, new ExplosionEntry(exploder, 125)));
            FetchExplosionBlock(BlockTypes.GCBlock_222, out exploder);
            explo.Add(new KeyValuePair<float, ExplosionEntry>(50, new ExplosionEntry(exploder, 50)));
            FetchExplosionBlock(BlockTypes.GSOBlock_111, out exploder);
            explo.Add(new KeyValuePair<float, ExplosionEntry>(0, new ExplosionEntry(exploder, 10)));
            _Explosions.Add(Type.Debris_GC, explo);

            // Debris RR
            explo = new List<KeyValuePair<float, ExplosionEntry>>();
            FetchExplosionBlock(BlockTypes.EXP_PrototypeGun_02_626, out exploder);
            explo.Add(new KeyValuePair<float, ExplosionEntry>(200, new ExplosionEntry(exploder, 200)));
            FetchExplosionBlock(BlockTypes.GCBlock_222, out exploder);
            explo.Add(new KeyValuePair<float, ExplosionEntry>(125, new ExplosionEntry(exploder, 125)));
            FetchExplosionBlock(BlockTypes.EXP_Block_212, out exploder);
            explo.Add(new KeyValuePair<float, ExplosionEntry>(50, new ExplosionEntry(exploder, 50)));
            FetchExplosionBlock(BlockTypes.EXP_Block_111, out exploder);
            explo.Add(new KeyValuePair<float, ExplosionEntry>(0, new ExplosionEntry(exploder, 10)));
            _Explosions.Add(Type.Debris_RR, explo);

            // Debris VEN
            explo = new List<KeyValuePair<float, ExplosionEntry>>();
            FetchExplosionBlock(BlockTypes.GCBlock_222, out exploder);
            explo.Add(new KeyValuePair<float, ExplosionEntry>(200, new ExplosionEntry(exploder, 200)));
            FetchExplosionBlock(BlockTypes.VENWheel_Titan_666, out exploder);
            explo.Add(new KeyValuePair<float, ExplosionEntry>(100, new ExplosionEntry(exploder, 100)));
            FetchExplosionBlock(BlockTypes.VENBlock_111, out exploder);
            explo.Add(new KeyValuePair<float, ExplosionEntry>(0, new ExplosionEntry(exploder, 10)));
            _Explosions.Add(Type.Debris_VEN, explo);

            // Debris HE
            explo = new List<KeyValuePair<float, ExplosionEntry>>();
            FetchExplosionBlock(BlockTypes.GCBlock_222, out exploder);
            explo.Add(new KeyValuePair<float, ExplosionEntry>(200, new ExplosionEntry(exploder, 200)));
            FetchExplosionBlock(BlockTypes.HE_Fort_Wall_842, out exploder);
            explo.Add(new KeyValuePair<float, ExplosionEntry>(150, new ExplosionEntry(exploder, 150)));
            FetchExplosionBlock(BlockTypes.HE_StdBlock_Alt_1_09_121, out exploder);
            explo.Add(new KeyValuePair<float, ExplosionEntry>(50, new ExplosionEntry(exploder, 50)));
            FetchExplosionBlock(BlockTypes.HE_StdBlock_01_111, out exploder);
            explo.Add(new KeyValuePair<float, ExplosionEntry>(0, new ExplosionEntry(exploder, 10)));
            _Explosions.Add(Type.Debris_HE, explo);

            // Debris BF
            explo = new List<KeyValuePair<float, ExplosionEntry>>();
            FetchExplosionBlock(BlockTypes.GCBlock_222, out exploder);
            explo.Add(new KeyValuePair<float, ExplosionEntry>(200, new ExplosionEntry(exploder, 200)));
            FetchExplosionBlock(BlockTypes.VENWheel_Titan_666, out exploder);
            explo.Add(new KeyValuePair<float, ExplosionEntry>(125, new ExplosionEntry(exploder, 125)));
            FetchExplosionBlock(BlockTypes.BF_Block_111, out exploder);
            explo.Add(new KeyValuePair<float, ExplosionEntry>(0, new ExplosionEntry(exploder, 10)));
            _Explosions.Add(Type.Debris_BF, explo);

            // Debris SJ
            explo = new List<KeyValuePair<float, ExplosionEntry>>();
            FetchExplosionBlock(BlockTypes.SJ_Deconstructor_333, out exploder);
            explo.Add(new KeyValuePair<float, ExplosionEntry>(200, new ExplosionEntry(exploder, 200)));
            FetchExplosionBlock(BlockTypes.GCBlock_222, out exploder);
            explo.Add(new KeyValuePair<float, ExplosionEntry>(150, new ExplosionEntry(exploder, 150)));
            FetchExplosionBlock(BlockTypes.SJ_Block_212, out exploder);
            explo.Add(new KeyValuePair<float, ExplosionEntry>(50, new ExplosionEntry(exploder, 50)));
            FetchExplosionBlock(BlockTypes.SJ_Block_111, out exploder);
            explo.Add(new KeyValuePair<float, ExplosionEntry>(0, new ExplosionEntry(exploder, 10)));
            _Explosions.Add(Type.Debris_SJ, explo);
        }

        private static bool StripOffNonExplosionParticles(Transform trans)
        {
            bool removedAny = false;
            bool removed;
            do
            {
                removed = false;
                for (int i = 0; trans.childCount > i; i++)
                {
                    var child = trans.GetChild(i);
                    StripOffNonExplosionParticles(child);
                    if (child.name != null)
                    {
                        var lower = child.name.ToLower();
                        if (!lower.Contains("smoke") && !lower.Contains("explosion") && !lower.Contains("light") &&
                            !lower.Contains("distortion") && !lower.Contains("spark") && !lower.Contains("shockwave"))
                        {
                            child.SetParent(null);
                            Debug_TTExt.Info("removed " + child.name);
                            UnityEngine.Object.Destroy(child.gameObject);
                            removed = true;
                            removedAny = true;
                            break;
                        }
                    }
                }
            }
            while (removed);
            return removedAny;
        }

        /// <summary>
        /// CREATES NEW POOL FOR <paramref name="exploder"/>
        /// </summary>
        private static float FetchExplosionWeapon(BlockTypes BT, out Transform exploder)
        {
            try
            {
                TankBlock TB = ManSpawn.inst.GetBlockPrefab(BT);
                if (TB)
                {
                    FireData FD = TB.GetComponent<FireData>();
                    if (FD)
                    {
                        if (FD.m_BulletPrefab)
                        {
                            Projectile proj = FD.m_BulletPrefab.GetComponent<Projectile>();
                            if (proj)
                            {
                                Transform transCase = (Transform)ManExtProj.explodeProj.GetValue(proj);
                                if (transCase)
                                {
                                    Debug_TTExt.Info("Making proj explosion prefab for " + BT.ToString() +
                                        ", transform name " + transCase.name);
                                    exploder = transCase.UnpooledSpawn();
                                    StripOffNonExplosionParticles(exploder);
                                    exploder.CreatePool(8);
                                    exploder.gameObject.SetActive(false);
                                    if (transCase.GetComponent<Explosion>())
                                    {
                                        float deals = transCase.GetComponent<Explosion>().m_MaxDamageStrength;
                                        Debug_TTExt.Info("explosion trans " + BT.ToString() + " deals " + deals);
                                        return deals;
                                    }
                                    Debug_TTExt.Info("explosion trans " + BT.ToString() + " deals nothing");
                                    return 0;
                                }
                                else
                                    Debug_TTExt.Assert("Failed to fetch explosion trans from " + BT.ToString());
                            }
                            else
                                Debug_TTExt.Assert("Failed to fetch projectile from " + BT.ToString());
                        }
                        else
                            Debug_TTExt.Assert("Failed to fetch WeaponRound from " + BT.ToString());
                    }
                    else
                        Debug_TTExt.Assert("Failed to fetch fireData from " + BT.ToString());
                }
                else
                    Debug_TTExt.Assert("Failed to fetch prefab " + BT.ToString());
            }
            catch (Exception e)
            {
                Debug_TTExt.Assert("Failed to fetch explosion from " + BT.ToString() + " | " + e);
            }
            exploder = null;
            return float.MaxValue;
        }

        /// <summary>
        /// CREATES NEW POOL FOR <paramref name="exploder"/>
        /// </summary>
        private static float FetchExplosionBlock(BlockTypes BT, out Transform exploder)
        {
            try
            {
                TankBlock TB = ManSpawn.inst.GetBlockPrefab(BT);
                if (TB)
                {
                    Transform transCase = TB.GetComponent<ModuleDamage>()?.deathExplosion;
                    if (transCase)
                    { 
                        Debug_TTExt.Info("Making explosion prefab for " + BT.ToString() +
                            ", transform name " + transCase.name);
                        exploder = transCase.UnpooledSpawn();
                        StripOffNonExplosionParticles(exploder);
                        exploder.CreatePool(8);
                        exploder.gameObject.SetActive(false);
                        if (transCase.GetComponent<Explosion>())
                        {
                            float deals = transCase.GetComponent<Explosion>().m_MaxDamageStrength;
                            Debug_TTExt.Info("explosion trans " + BT.ToString() + " deals " + deals);
                            return deals;
                        }
                        Debug_TTExt.Info("explosion trans " + BT.ToString() + " deals nothing");
                        return 0;
                    }
                    else
                        Debug_TTExt.Assert("Failed to fetch deathExplosion from " + BT.ToString());
                }
                else
                    Debug_TTExt.Assert("Failed to fetch prefab " + BT.ToString());
            }
            catch (Exception e)
            {
                Debug_TTExt.Assert("Failed to fetch explosion from " + BT.ToString() + " | " + e);
            }
            exploder = null;
            return float.MaxValue;
        }

        /// <summary>
        /// Spawn an explosion based on it's strength.
        /// <para>See <seealso cref="SpawnHelper"/> for more explosions.</para>
        /// </summary>
        /// <param name="type">Explosion visual type</param>
        /// <param name="pos">Position in scene space</param>
        /// <param name="bright">Orange glow parts</param>
        /// <param name="dealDamage">Apply the damage</param>
        /// <param name="strength">Strength based on the damage inflicted
        /// <para>Usually in a range from [0 ~ 250]</para></param>
        /// <param name="launchForce">launching force inflicted on Visibles hit by this.
        /// <para>Leave at 0 to use preset</para></param>
        /// <param name="effectRadius"> The max radius this explosion will reach out from
        /// <para>Leave at 0 to use preset</para></param>
        /// <param name="effectRadiusMaxStrengthPercent">The [0 ~ 1] radius range where the explosion deals its max damage inwards from where 0 is the center and 1 is the outwards reach
        /// <para>Leave at 0 to use preset</para></param>
        /// <param name="dmgType">The damageable type to deal damage with using.
        /// <para>Leave at -1 to use preset</para></param>
        /// <param name="exploSFX">The sfx type used for the explosion
        /// <para>Leave at -1 to use preset</para></param>
        /// <param name="exploSFXSize">The sfx size used for the explosion
        /// <para>Leave at -1 to use preset</para></param>
        /// <param name="shockwave">Do a shockwave if this explosion has any</param>
        /// <returns>The explosion</returns>
        public static Explosion SpawnExplosionByStrength(Type type, Vector3 pos, bool bright, float strength, 
            bool dealDamage = true, float launchForce = 0, float effectRadius = 0, float effectRadiusMaxStrengthPercent = 0,
            ManDamage.DamageType dmgType = (ManDamage.DamageType)(-1), ManSFX.ExplosionType exploSFX = (ManSFX.ExplosionType)(-1), 
            ManSFX.ExplosionSize exploSFXSize = (ManSFX.ExplosionSize)(-1), bool shockwave = false)
        {
            if (Explosions.TryGetValue(type, out var lookup))
            {
                foreach (var explo in lookup)
                {
                    if (explo.Key <= strength && explo.Value.prefab != null)
                    {
                        Debug_TTExt.Info("Used explosion of strength " + explo.Key);
                        var exp = explo.Value.prefab.Spawn(null, pos).GetComponent<Explosion>();
                        exp.DoDamage = dealDamage;
                        exp.enabled = true;
                        foreach (var item in exp.GetComponentsInChildren<Transform>(true))
                        {
                            if (item.name != null)
                            {
                                var name = item.name.ToLower();
                                if (name.Contains("bright"))
                                    item.gameObject.SetActive(bright);
                                /*if (name.Contains("dark"))
                                    item.gameObject.SetActive(bright);//*/
                                if (name.Contains("shock"))
                                    item.gameObject.SetActive(shockwave);
                            }
                        }
                        exp.gameObject.SetActive(true);
                        exp.m_MaxDamageStrength = strength;
                        exp.m_MaxImpulseStrength = (launchForce == 0) ? explo.Value.MaxImpulse : launchForce;
                        exp.m_EffectRadius = (effectRadius == 0) ? explo.Value.EffectRadius : effectRadius;
                        exp.m_EffectRadiusMaxStrength = (effectRadiusMaxStrengthPercent == 0) ? explo.Value.EffectRadiusMaxStrengthPercent : effectRadiusMaxStrengthPercent;
                        explodeType.SetValue(exp, (dmgType == (ManDamage.DamageType)(-1)) ? explo.Value.DamageType : dmgType);
                        explodeSFX.SetValue(exp, (exploSFX == (ManSFX.ExplosionType)(-1)) ? explo.Value.ExplodeSFX : exploSFX);
                        explodeSFXSize.SetValue(exp, (exploSFXSize == (ManSFX.ExplosionSize)(-1)) ? explo.Value.ExplodeSFXSize : exploSFXSize);
                        return exp;
                    }
                }
            }
            return null;
        }


        internal class GUIManaged : GUILayoutHelpers
        {
            private static bool controlledDisp = false;
            private static HashSet<string> enabledTabs = null;
            private static HashSet<Type> enabledTypes = null;
            public static void GUIGetTotalManaged()
            {
                if (enabledTabs == null)
                {
                    enabledTabs = new HashSet<string>();
                    enabledTypes = new HashSet<Type>();
                }
                GUIExplosions();
            }
            public static void GUIExplosions()
            {
                GUILayout.Box("--- All Explosions --- ", AltUI.BoxBlackTextBlueTitle);
                bool show = controlledDisp && Singleton.playerTank;
                if (GUILayout.Button("Enabled Loading: " + show))
                    controlledDisp = !controlledDisp;
                if (controlledDisp)
                {
                    try
                    {
                        foreach (var explo in Explosions)
                        {
                            var prev = enabledTypes.Contains(explo.Key);
                            var state = AltUI.Toggle(prev, explo.Key.ToString());

                            if (state != prev)
                            {
                                if (state)
                                    enabledTypes.Add(explo.Key);
                                else
                                    enabledTypes.Remove(explo.Key);
                            }
                            if (state)
                            {
                                GUILayout.BeginVertical(AltUI.TextfieldBordered);
                                foreach (var item1 in explo.Value)
                                {
                                    GUILayout.BeginHorizontal(AltUI.TextfieldBlackAdjusted);
                                    GUILayout.Label(item1.Key.ToString(), AltUI.LabelBlack);
                                    GUILayout.Label(" Spawn:", AltUI.LabelBlack);
                                    if (GUILayout.Button("Normal", AltUI.ButtonBlue) && Singleton.cameraTrans != null)
                                        SpawnExplosionByStrength(explo.Key,
                                            Singleton.cameraTrans.position + Singleton.cameraTrans.forward * 100f, true, item1.Key, true);
                                    if (GUILayout.Button("No Bright", AltUI.ButtonRed) && Singleton.cameraTrans != null)
                                        SpawnExplosionByStrength(explo.Key,
                                            Singleton.cameraTrans.position + Singleton.cameraTrans.forward * 100f, false, item1.Key, true);
                                    if (GUILayout.Button("No Damage", AltUI.ButtonGreen) && Singleton.cameraTrans != null)
                                        SpawnExplosionByStrength(explo.Key,
                                            Singleton.cameraTrans.position + Singleton.cameraTrans.forward * 100f, true, item1.Key, false);
                                    GUILayout.EndHorizontal();
                                }
                                GUILayout.EndVertical();
                            }
                        }
                    }
                    catch (ExitGUIException e)
                    {
                        throw e;
                    }
                    catch (Exception e)
                    {
                        Debug_TTExt.Log("ExplosionHelper UI Debug errored - " + e);
                    }
                }
            }

        }

    }
}
