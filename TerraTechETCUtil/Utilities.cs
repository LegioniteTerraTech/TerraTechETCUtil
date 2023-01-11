using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;

namespace TerraTechETCUtil
{
    public static class Utilities
    {
        /// <summary>
        /// For block attachment updates when Tank is set to a valid reference
        /// </summary>
        public static void SubToBlockAttachConnected(this TankBlock TB, Action attachEvent, Action detachEvent)
        {
            if (attachEvent != null)
                TB.AttachedEvent.Subscribe(attachEvent);
            if (detachEvent != null)
                TB.DetachingEvent.Subscribe(detachEvent);
        }
        /// <summary>
        /// For block attachment updates when Tank is set to a valid reference
        /// </summary>
        public static void UnSubToBlockAttachConnected(this TankBlock TB, Action attachEvent, Action detachEvent)
        {
            if (attachEvent != null)
                TB.AttachedEvent.Unsubscribe(attachEvent);
            if (detachEvent != null)
                TB.DetachingEvent.Unsubscribe(detachEvent);
        }

        public static Transform HeavyObjectSearch(this Transform trans, string name)
        {
            if (name.NullOrEmpty())
                return null;
            return trans.gameObject.GetComponentsInChildren<Transform>().ToList().Find(delegate (Transform cand)
            {
                if (cand.name.NullOrEmpty())
                    return false;
                return cand.name.CompareTo(name) == 0;
            });
        }
    }
}
