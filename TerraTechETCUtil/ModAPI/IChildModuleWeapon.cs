using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using TerraTechETCUtil;


namespace TerraTechETCUtil
{/*
     */

    /// <summary>
    /// Interface to get in contact with ChildModuleWeapon (if it exists)
    /// </summary>
    public interface IChildModuleWeapon : IModuleWeapon
    {
        /// <summary>
        /// Get the number of barrels on the weapon
        /// </summary>
        /// <returns></returns>
        int GetBarrelsMainCount();
        /// <summary>
        /// Get a specific barrel
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        IChildWeapBarrel GetBarrel(int index);

        /// <summary>
        /// Aim this while ignoring the primary aimer
        /// </summary>
        /// <param name="scenePos"></param>
        /// <param name="fire"></param>
        void OverrideAndAimAt(Vector3 scenePos, bool fire);
    }
    /// <summary>
    /// <see cref="IChildModuleWeapon"/> barrel interface
    /// </summary>
    public interface IChildWeapBarrel
    {
        /// <summary> self-explanitory </summary>
        Transform GetBulletTrans();
        /// <summary> self-explanitory </summary>
        MuzzleFlash GetFlashTrans();
        /// <summary> self-explanitory </summary>
        Transform GetRecoilTrans();
    }

    /// <summary>
    /// For anything that can explode
    /// </summary>
    public interface IExplodeable
    {
        /// <summary>
        /// Explode immedeately
        /// </summary>
        void Explode();
    }
}
