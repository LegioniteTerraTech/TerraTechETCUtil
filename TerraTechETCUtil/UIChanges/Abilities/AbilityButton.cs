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
    public class AbilityButton : AbilityElement
    {
        public Button button;
        public readonly UnityAction Callback;
        public override bool PressedState() => false;
        public AbilityButton(string name, Sprite iconSprite, UnityAction callback, float cooldown) :
            base(name, iconSprite, cooldown)
        {
            Callback = callback;
            ManAbilities.InitElement(this);
        }
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
            button = ManAbilities.MakePrefabButton(Name, Sprite, TriggerThis);
            inst = button.gameObject;
            images = inst.GetComponentsInChildren<Image>(true);
            ManAbilities.Ready.Add(this);
        }
    }
}
