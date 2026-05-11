using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TerraTechETCUtil
{
    /// <summary>
    /// Keeps track of mod data for easy comperisons and access
    /// </summary>
    public struct ModDataHandle
    {
        /// <summary>
        /// The ModID of the ModDataHandle
        /// </summary>
        public readonly string ModID;
        /// <inheritdoc/>
        public override int GetHashCode()
        {
            if (ModID == null)
                return 0;
            return ModID.GetHashCode();
        }
        /// <inheritdoc/>
        public static bool operator ==(ModDataHandle script1, ModDataHandle script2)
        {
            return script1.ModID == script2.ModID;
        }
        /// <inheritdoc/>
        public static bool operator !=(ModDataHandle script1, ModDataHandle script2)
        {
            return script1.ModID != script2.ModID;
        }
        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is ModDataHandle MDH && (MDH == this);
        }
        /// <summary>
        /// Creates a ModDataHandle for the given mod
        /// </summary>
        /// <param name="modID">The actual ModID for the mod, usually not the display name of the mod</param>
        /// <exception cref="NullReferenceException">The mod does not exist now</exception>
        public ModDataHandle(string modID)
        {
            if (modID != LegModExt.modID && !ManMods.inst.ModExists(modID))// Bypass for this DLL! DANGEROUS
                throw new NullReferenceException("ModResourceHandle - ModID " + modID + " does not exists");
            ModID = modID;
        }

        /// <summary>
        /// Get the ModContainer
        /// </summary>
        /// <returns>The mod</returns>
        public ModContainer GetModContainer()
        {
            return ResourcesHelper.GetModContainerFromScript(this);
        }

        /// <summary>
        /// Subscribe an event to BEFORE all mods load
        /// </summary>
        /// <param name="preEvent"></param>
        public void SubToModsPreLoad(Action preEvent)
        {
            InvokeHelper.ModsPreLoadEvent.Subscribe(preEvent);
        }
        /// <summary>
        /// Subscribe an event to AFTER all mods are loaded
        /// </summary>
        /// <param name="postEvent"></param>
        public void SubToModsPostLoad(Action postEvent)
        {
            InvokeHelper.ModsPostLoadEvent.Subscribe(postEvent);
        }
        /// <summary>
        /// Subscribe an event to AFTER all blocks are loaded
        /// </summary>
        /// <param name="postEvent"></param>
        public void SubToBlocksPostChange(Action postEvent)
        {
            InvokeHelper.BlocksPostChangeEvent.Subscribe(postEvent);
        }

        /// <summary>
        /// Return the ModID
        /// </summary>
        public string GetModName()
        {
            return ModID;
        }
        /// <summary>
        /// Log this mod's contents in the game's output_log.txt
        /// </summary>
        /// <inheritdoc cref="ResourcesHelper.LookIntoModContents(ModContainer)"/>
        public void DebugLogModContents()
        {
            ResourcesHelper.LookIntoModContents(GetModContainer());
        }
        /// <summary>
        /// Try get a resource from this mod's container
        /// </summary>
        /// <inheritdoc cref="ResourcesHelper.GetTextureFromModAssetBundle(ModContainer, string, bool, bool)"/>
        public Texture2D GetModTexture(string nameNoExt)
        {
            return ResourcesHelper.GetTextureFromModAssetBundle(GetModContainer(), nameNoExt);
        }
        /// <summary>
        /// Try get a resource from this mod's container
        /// </summary>
        /// <inheritdoc cref="ResourcesHelper.GetObjectFromModContainer(ModContainer, string)"/>
        public T GetModObject<T>(string nameNoExt) where T : UnityEngine.Object
        {
            return ResourcesHelper.GetObjectFromModContainer<T>(GetModContainer(), nameNoExt);
        }
        /// <summary>
        /// Try iterate resources in this mod's container
        /// </summary>
        /// <inheritdoc cref="ResourcesHelper.IterateAssetsInModContainer(ModContainer)"/>
        public IEnumerable<T> GetModObjects<T>() where T : UnityEngine.Object
        {
            return ResourcesHelper.IterateAssetsInModContainer<T>(GetModContainer());
        }
        /// <summary>
        /// Try iterate resources in this mod's container
        /// </summary>
        /// <param name="nameNoExt">The start of the <see cref="UnityEngine.Object.name"/> to look for</param>
        /// <inheritdoc cref="ResourcesHelper.IterateAssetsInModContainer(ModContainer, string)"/>
        public IEnumerable<T> GetModObjects<T>(string nameNoExt) where T : UnityEngine.Object
        {
            return ResourcesHelper.IterateAssetsInModContainer<T>(GetModContainer(), nameNoExt);
        }
    }
}
