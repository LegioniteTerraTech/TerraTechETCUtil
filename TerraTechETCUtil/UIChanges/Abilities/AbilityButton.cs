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
    /// <inheritdoc/>
    /// <para>This is the button variant that activates on press</para>
    /// </summary>
    public class AbilityButton : AbilityElement
    {
        /// <summary>
        /// Button on the UI
        /// </summary>
        public Button button;
        /// <summary>
        /// Called when the button is pressed
        /// </summary>
        public readonly UnityAction Callback;
        /// <inheritdoc/>
        protected override bool PressedState() => false;
        /// <inheritdoc cref = "AbilityElement(LocExtStringMod, Sprite, float)" />
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="iconSprite"></param>
        /// <param name="callback">Called when the button is pressed</param>
        /// <param name="cooldown"></param>
        public AbilityButton(LocExtStringMod name, Sprite iconSprite, UnityAction callback, float cooldown) :
            base(name, iconSprite, cooldown)
        {
            Callback = callback;
            ManAbilities.InitElement(this);
        }
        /// <inheritdoc cref = "AbilityElement(string, Sprite, float)" />
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="iconSprite"></param>
        /// <param name="callback">Called when the button is pressed</param>
        /// <param name="cooldown"></param>
        public AbilityButton(string name, Sprite iconSprite, UnityAction callback, float cooldown) :
            base(name, iconSprite, cooldown)
        {
            Callback = callback;
            ManAbilities.InitElement(this);
        }
        /// <inheritdoc/>
        public override void TriggerNow() => TriggerThis();
        private void TriggerThis()
        {
            if (button.interactable)
            {
                Callback.Invoke();
                TriggerCooldown();
            }
        }
        internal override void SetAvail(bool state)
        {
            button.interactable = state;
        }
        internal override void Initiate()
        {
            if (NameLoc != null)
                button = ManAbilities.MakePrefabButton(NameLoc, Sprite, TriggerThis);
            else
                button = ManAbilities.MakePrefabButton(NameMain, Sprite, TriggerThis);
            inst = button.gameObject;
            images = inst.GetComponentsInChildren<Image>(true);
            ManAbilities.Ready.Add(this);
        }
    }
}
