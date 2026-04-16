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
    /// <summary>
    /// A custom UI button that can be triggered to call upon a Tech ability
    /// </summary>
    public abstract class AbilityElement
    {
        /// <summary>
        /// Display name
        /// </summary>
        public string Name => NameLoc == null ? NameMain : NameLoc.ToString();
        /// <summary>
        /// Display name in ENGLISH
        /// </summary>
        public readonly string NameMain;
        /// <summary>
        /// Display name in localised text
        /// </summary>
        public readonly LocExtStringMod NameLoc;
        /// <summary>
        /// Sprite to use on the hotbar
        /// </summary>
        public readonly Sprite Sprite;
        /// <summary>
        /// Affiliated visual button GameObject
        /// </summary>
        protected GameObject inst;
        /// <summary>
        /// Time until it can be pressed again
        /// </summary>
        protected float Cooldown = 0;
        private float cooldownCur = 0;
        /// <summary>
        /// Affiliated images
        /// </summary>
        protected Image[] images;
        /// <summary>
        /// Return the state of the button if it is pressed or not
        /// </summary>
        /// <returns></returns>
        protected abstract bool PressedState();
        /// <summary>
        /// Create a new instance
        /// </summary>
        /// <param name="name">Display name</param>
        /// <param name="iconSprite">Sprite to use on the hotbar</param>
        /// <param name="cooldown">Time until it can be pressed again</param>
        public AbilityElement(LocExtStringMod name, Sprite iconSprite, float cooldown)
        {
            NameLoc = name;
            Sprite = iconSprite;
            Cooldown = cooldown;
        }
        /// <summary>
        /// Create a new instance
        /// </summary>
        /// <param name="name">Display name</param>
        /// <param name="iconSprite">Sprite to use on the hotbar</param>
        /// <param name="cooldown">Time until it can be pressed again</param>
        public AbilityElement(string name, Sprite iconSprite, float cooldown)
        {
            NameMain = name;
            Sprite = iconSprite;
            Cooldown = cooldown;
        }
        internal void ShowInUI_Internal(bool state) => inst.SetActive(state);
        /// <summary>
        /// Show or hide it on the hotbar
        /// </summary>
        /// <param name="state"></param>
        public void SetShown(bool state)
        {
            if (state)
                Show();
            else
                Hide();
        }
        /// <summary>
        /// Show it on the hotbar
        /// </summary>
        public void Show()
        {
            ManAbilities.Shown.Add(this);
            ManAbilities.RefreshPage();
        }
        /// <summary>
        /// Hide it from the hotbar
        /// </summary>
        public void Hide()
        {
            ManAbilities.Shown.Remove(this);
            ManAbilities.RefreshPage();
        } 
        /// <summary>
        /// Remove it
        /// </summary>
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
        /// <summary>
        /// Set the visual fill state of the sprite icon
        /// </summary>
        /// <param name="fillPercent"></param>
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
        /// <summary>
        /// Called when the player activates this UI element
        /// </summary>
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
