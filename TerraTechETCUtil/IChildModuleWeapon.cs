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
        int GetBarrelsMainCount();
        IChildWeapBarrel GetBarrel(int index);

        void OverrideAndAimAt(Vector3 scenePos, bool fire);
    }

    public interface IChildWeapBarrel
    {
        Transform GetBulletTrans();
        MuzzleFlash GetFlashTrans();
        Transform GetRecoilTrans();
    }

    public interface IExplodeable
    {
        void Explode();
    }
}
