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
    public class AbilityToggle : AbilityElement
    {
        public Toggle toggle;
        public readonly UnityAction<bool> Callback;
        private UIHUDToggleButton togUI;
        public AbilityToggle(string name, Sprite iconSprite, UnityAction<bool> callback, float cooldown) :
            base(name, iconSprite, cooldown)
        {
            Callback = callback;
            ManAbilities.InitElement(this);
        }
        public void TriggerCooldownWrapper(bool input)
        {
            Callback.Invoke(input);
            //TriggerCooldown();
        }
        internal override void SetAvail(bool state)
        {
            toggle.interactable = state;
        }
        internal override void Initiate()
        {
            toggle = ManAbilities.MakePrefabToggle(Name, Sprite, TriggerCooldownWrapper);
            inst = toggle.gameObject;
            images = inst.GetComponentsInChildren<Image>(true);
            togUI = inst.GetComponentInChildren<UIHUDToggleButton>(true);
            ManAbilities.Active.Add(this);
        }
        public void SetToggleState(bool state)
        {
            if (inst)
                togUI.SetToggledState(state);
        }
    }
}
