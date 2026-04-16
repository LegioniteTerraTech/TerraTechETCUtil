using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;


namespace TerraTechETCUtil
{
    /// <summary>
    /// <inheritdoc/>
    /// <para>This is the toggle variant that can be turned on or off</para>
    /// </summary>
    public class AbilityToggle : AbilityElement
    {
        /// <summary>
        /// Toggle on the UI
        /// </summary>
        public Toggle toggle;
        /// <summary>
        /// Called when the toggle is toggled
        /// </summary>
        public readonly UnityAction<bool> Callback;
        private UIHUDToggleButton togUI;
        /// <inheritdoc/>
        protected override bool PressedState() => toggle.isOn;
        /// <inheritdoc cref = "AbilityElement(LocExtStringMod, Sprite, float)" />
        /// <summary>
        /// </summary>
        /// <param name="name"></param>
        /// <param name="iconSprite"></param>
        /// <param name="cooldown"></param>
        /// <param name="callback">Called when the toggle is toggled</param>
        public AbilityToggle(LocExtStringMod name, Sprite iconSprite, float cooldown, UnityAction<bool> callback) :
            base(name, iconSprite, cooldown)
        {
            Callback = callback;
            ManAbilities.InitElement(this);
        }
        /// <inheritdoc cref = "AbilityElement(string, Sprite, float)" />
        /// <summary>
        /// </summary>
        /// <param name="name"></param>
        /// <param name="iconSprite"></param>
        /// <param name="cooldown"></param>
        /// <param name="callback">Called when the toggle is toggled</param>
        public AbilityToggle(string name, Sprite iconSprite, UnityAction<bool> callback, float cooldown) :
            base(name, iconSprite, cooldown)
        {
            Callback = callback;
            ManAbilities.InitElement(this);
        }
        /// <inheritdoc/>
        public override void TriggerNow() => toggle.isOn = !toggle.isOn;
        private void TriggerThis(bool input)
        {
            if (toggle.interactable)
            {
                Callback.Invoke(input);
                TriggerCooldown();
            }
        }
        internal override void SetAvail(bool state)
        {
            toggle.interactable = state;
        }
        internal override void Initiate()
        {
            if (NameLoc != null)
                toggle = ManAbilities.MakePrefabToggle(NameLoc, Sprite, TriggerThis);
            else
                toggle = ManAbilities.MakePrefabToggle(NameMain, Sprite, TriggerThis);
            inst = toggle.gameObject;
            images = inst.GetComponentsInChildren<Image>(true);
            togUI = inst.GetComponentInChildren<UIHUDToggleButton>(true);
            ManAbilities.Ready.Add(this);
        }
        /// <summary>
        /// Set the toggle state directly
        /// </summary>
        /// <param name="state"></param>
        public void SetToggleState(bool state)
        {
            if (inst)
                togUI.SetToggledState(state);
        }
    }
}
