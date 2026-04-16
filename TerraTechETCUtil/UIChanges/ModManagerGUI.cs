using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TerraTechETCUtil
{
    // WIP
    /// <summary>
    /// It's main goal is to create a persistant, managed GUI window.
    /// </summary>
    public class ModManagerGUI : GUIMiniMenu<ModManagerGUI>
    {
        private string name;
        public string Name
        {
            get => Display.Header;
            set => Display.Header = value;
        }
        public Rect HotWindow
        {
            get => Display.Window;
            set => Display.Window = value;
        }
        public float UIAlpha
        {
            get => Display.alpha;
            set => Display.alpha = value;
        }

        public ModManagerGUI() { }
        public ModManagerGUI(ModBase requestor, string Name, Rect HotWindow, float alpha = 1)
        {
            ManModGUI.RequestInit(requestor);
            ManModGUI.RegisterPopupSingle(this, Name, new GUIDisplayStats
            {
                windowSize = HotWindow,
                alpha = alpha
            });
        }

        /// <inheritdoc/>
        public override void Setup(GUIDisplayStats stats) { }

        /// <inheritdoc/>
        public override void RunGUI(int ID)
        {
        }

        /// <inheritdoc/>
        public override void DelayedUpdate() { }
        /// <inheritdoc/>
        public override void FastUpdate() { }
        /// <inheritdoc/>
        public override void OnRemoval() { }
    }
}
