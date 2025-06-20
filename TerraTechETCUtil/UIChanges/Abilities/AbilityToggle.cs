using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;


namespace TerraTechETCUtil
{
    public class AbilityToggle : AbilityElement
    {
        public Toggle toggle;
        public readonly UnityAction<bool> Callback;
        private UIHUDToggleButton togUI;
        public override bool PressedState() => toggle.isOn;
        public AbilityToggle(LocExtStringMod name, Sprite iconSprite, UnityAction<bool> callback, float cooldown) :
            base(name, iconSprite, cooldown)
        {
            Callback = callback;
            ManAbilities.InitElement(this);
        }
        public AbilityToggle(string name, Sprite iconSprite, UnityAction<bool> callback, float cooldown) :
            base(name, iconSprite, cooldown)
        {
            Callback = callback;
            ManAbilities.InitElement(this);
        }
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
        public void SetToggleState(bool state)
        {
            if (inst)
                togUI.SetToggledState(state);
        }
    }
}
