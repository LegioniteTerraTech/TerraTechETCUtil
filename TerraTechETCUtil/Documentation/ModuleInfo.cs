using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TerraTechETCUtil
{
    /// <summary>
    /// Extracts module info about a targeted module.
    /// <para>Can even extract it as JSON</para>
    /// </summary>
    public class ModuleInfo : AutoDataExtractorInst
    {
        private static HashSet<object> grabbed = new HashSet<object>();
        private static List<ModuleInfo> cacher = new List<ModuleInfo>();
        internal static ModuleInfo[] TryGetModules(GameObject toExplore, HashSet<Type> AllowedTypes)
        {
            try
            {
                if (toExplore == null)
                    throw new ArgumentNullException(nameof(toExplore));
                foreach (var item in toExplore.GetComponents<MonoBehaviour>())
                {
                    Type typeCase = item.GetType();
                    if (grabbed.Contains(item))
                        continue;
                    grabbed.Add(item);
                    if (AllowedTypes.Contains(typeCase))
                    {
                        cacher.Add(new ModuleInfo(typeCase, item, grabbed));
                    }
                    else if (item is Module)
                    {
                        if (!ignoreModuleTypes.Contains(typeCase))
                            cacher.Add(new ModuleInfo(typeCase, item, grabbed));
                    }
                    else if (item is ExtModule)
                    {
                        if (!ignoreModuleTypesExt.Contains(typeCase))
                            cacher.Add(new ModuleInfo(typeCase, item, grabbed));
                    }
                }
                return cacher.ToArray();
            }
            catch (Exception e)
            {
                throw new Exception("TryGetModules failed on " + (toExplore.name.NullOrEmpty() ?
                    "<NULL>" : toExplore.name) + " - ", e);
            }
            finally
            {
                cacher.Clear();
                grabbed.Clear();
            }
        }
        internal static ModuleInfo[] TryGetModulesBlacklisted(GameObject toExplore, HashSet<Type> BlockedTypes)
        {
            try
            {
                foreach (var item in toExplore.GetComponents<Component>())
                {
                    Type typeCase = item.GetType();
                    if (grabbed.Contains(item))
                        continue;
                    grabbed.Add(item);
                    if (item is Module)
                    {
                        if (!ignoreModuleTypes.Contains(typeCase))
                            cacher.Add(new ModuleInfo(typeCase, item, grabbed));
                    }
                    else if (item is ExtModule)
                    {
                        if (!ignoreModuleTypesExt.Contains(typeCase))
                            cacher.Add(new ModuleInfo(typeCase, item, grabbed));
                    }
                    else if (BlockedTypes.Contains(typeCase))
                    {
                    }
                    else
                        cacher.Add(new ModuleInfo(typeCase, item, grabbed));
                }
                return cacher.ToArray();
            }
            catch (Exception e)
            {
                throw new Exception("TryGetModules failed - ", e);
            }
            finally
            {
                cacher.Clear();
                grabbed.Clear();
            }
        }

        /// <summary>
        /// Creates a new ModuleInfo to extract and cache data about a target <see cref="Module"/> immedeately.
        /// <para>Also stores instance information for immedeate later access.</para>
        /// </summary>
        /// <param name="grabbedType">The type this is targeting</param>
        /// <param name="prefab">The actual prefab to gather context from</param>
        /// <param name="grabbedAlready">For recursive calls, what has already been extracted to prevent duplicates</param>
        public ModuleInfo(Type grabbedType, Component prefab, HashSet<object> grabbedAlready) : base(
            SpecialNames.TryGetValue(grabbedType.Name, out string altName) ? altName :
            grabbedType.Name.Replace("Module", "").ToString().SplitCamelCase(),
            grabbedType, prefab, grabbedAlready)
        {
        }


        internal static HashSet<Type> AllowedTypesUIWiki = new HashSet<Type>() {
                    typeof(TankBlock),
                    typeof(ResourceDispenser),
                    typeof(ResourcePickup),
                    //
                    typeof(Damageable),
                    typeof(FireData),
                    typeof(ModuleWing.Aerofoil),
                    typeof(ManWheels.WheelParams),
                    typeof(ManWheels.TireProperties),
                    typeof(WeaponRound),
                };

        internal static HashSet<Type> BlockedTypesExporter = new HashSet<Type>() {
                    typeof(Visible),
                    typeof(TankBlock),
                    typeof(Transform),
                    typeof(ResourceDispenser),
                    typeof(ResourcePickup),
                    typeof(AutoSpriteRenderer),
                    typeof(Animator),
                    typeof(Animation),
                    typeof(AnimEvent),
                };
    }
}
