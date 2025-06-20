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
        public string Name => NameLoc == null ? NameMain : NameLoc.ToString();
        public readonly string NameMain;
        public readonly LocExtStringMod NameLoc;
        public readonly Sprite Sprite;
        protected GameObject inst;
        protected float Cooldown = 0;
        private float cooldownCur = 0;
        protected Image[] images;
        public abstract bool PressedState();
        public AbilityElement(LocExtStringMod name, Sprite iconSprite, float cooldown)
        {
            NameLoc = name;
            Sprite = iconSprite;
            Cooldown = cooldown;
        }
        public AbilityElement(string name, Sprite iconSprite, float cooldown)
        {
            NameMain = name;
            Sprite = iconSprite;
            Cooldown = cooldown;
        }
        internal void ShowInUI_Internal(bool state) => inst.SetActive(state);
        public void SetShown(bool state)
        {
            if (state)
                Show();
            else
                Hide();
        }
        public void Show()
        {
            ManAbilities.Shown.Add(this);
            ManAbilities.RefreshPage();
        }
        public void Hide()
        {
            ManAbilities.Shown.Remove(this);
            ManAbilities.RefreshPage();
        } 
        public void Destroy()
        {
            UnityEngine.Object.Destroy(inst);
            ManAbilities.Shown.Remove(this);
            ManAbilities.Ready.Remove(this);
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
        public abstract void TriggerNow();
        internal void TriggerCooldown()
        {
            if (inst && Cooldown > 0)
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
