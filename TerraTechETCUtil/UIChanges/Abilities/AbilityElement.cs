using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;


namespace TerraTechETCUtil
{
    public abstract class AbilityElement
    {
        public readonly string Name;
        public readonly Sprite Sprite;
        protected GameObject inst;
        protected float Cooldown = 1;
        private float cooldownCur = 0;
        protected Image[] images;
        public AbilityElement(string name, Sprite iconSprite, float cooldown)
        {
            Name = name;
            Sprite = iconSprite;
            Cooldown = cooldown;
        }
        public void SetShown(bool state) => inst.SetActive(state);
        public void Show()
        {
            inst.SetActive(true);
        }
        public void Hide() => inst.SetActive(false);
        public void Destroy()
        {
            UnityEngine.Object.Destroy(inst);
            ManAbilities.Active.Remove(this);
            ManAbilities.updating.Remove(this);
            if (!ManAbilities.updating.Any())
                InvokeHelper.CancelInvokeSingleRepeat(CheckCooldowns);
        }
        internal abstract void Initiate();
        internal abstract void SetAvail(bool state);
        public void SetFillState(float fillPercent)
        {
            if (inst)
            {
                foreach (var item in images)
                {
                    item.fillAmount = fillPercent;
                }
            }
        }
        public void TriggerCooldown()
        {
            if (inst)
            {
                cooldownCur = Cooldown;
                if (!ManAbilities.updating.Any())
                    InvokeHelper.InvokeSingleRepeat(CheckCooldowns, 0);
                SetAvail(false);
                ManAbilities.updating.Add(this);
            }
        }
        private static void CheckCooldowns()
        {
            for (int step = 0; step < ManAbilities.updating.Count; step++)
            {
                var item = ManAbilities.updating[step];
                if (item.inst)
                {
                    item.cooldownCur -= Time.deltaTime;
                    if (item.cooldownCur <= 0)
                    {
                        item.cooldownCur = 0;
                        item.SetFillState(1);
                        item.SetAvail(true);
                        ManAbilities.updating.RemoveAt(step);
                    }
                    else
                        item.SetFillState(1 - Mathf.Clamp01(item.cooldownCur / item.Cooldown));
                }
            }
            if (!ManAbilities.updating.Any())
                InvokeHelper.CancelInvokeSingleRepeat(CheckCooldowns);
        }
    }
}
