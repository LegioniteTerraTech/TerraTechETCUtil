using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TerraTechETCUtil
{
    /// <summary>
    /// A mini menu managed by <see cref="ManModGUI"/>
    /// <para>Override this to make your own custom auto-managed UI</para>
    /// </summary>
    public abstract class GUIMiniMenu
    {
        internal int typeHash => GetType().GetHashCode();
        /// <summary>
        /// The managed instance for this display
        /// </summary>
        public GUIPopupDisplay Display { get; internal set; }

        /// <inheritdoc cref=" ManModGUI.ShowPopup(bool)"/>
        public bool ShowPopup()
        {
            return ManModGUI.ShowPopup(Display);
        }
        /// <inheritdoc cref="ManModGUI.ShowPopup(GUIMiniMenu, Vector2, bool)"/>
        public bool ShowPopup(Vector2 screenPosPercent)
        {
            return ManModGUI.ShowPopup(screenPosPercent, Display);
        }
        /// <inheritdoc cref="ManModGUI.HidePopup(GUIMiniMenu)"/>
        public bool HidePopup()
        {
            return ManModGUI.HidePopup(Display);
        }
        /// <inheritdoc cref="ManModGUI.ChangePopupPositioning(Vector2, GUIPopupDisplay)"/>
        public void MovePopup(Vector2 screenPosPercent)
        {
            ManModGUI.ChangePopupPositioning(screenPosPercent, Display);
        }
        /// <inheritdoc cref="ManModGUI.RemovePopup(GUIPopupDisplay)"/>
        public bool RemovePopup()
        {
            return ManModGUI.RemovePopup(Display);
        }

        /// <summary>
        /// Setup this to register it with <see cref="ManModGUI"/>
        /// </summary>
        /// <param name="stats"></param>
        public abstract void Setup(GUIDisplayStats stats);

        /// <summary>
        /// What to display on the GUI window itself
        /// </summary>
        public abstract void RunGUI(int ID);

        /// <summary>
        /// Once every 50 visual frames
        /// </summary>
        public abstract void DelayedUpdate();
        /// <summary>
        /// Once every visual frames
        /// </summary>
        public abstract void FastUpdate();

        /// <summary>
        /// Triggered when the window is removed entirely
        /// </summary>
        public abstract void OnRemoval();

        /// <summary>
        /// Triggered when the window is opened
        /// </summary>
        public virtual void OnOpen() { }
    }

    /// <summary>
    /// <para>A universal menu for use with generic formatting</para>
    /// <inheritdoc cref=" GUIMiniMenu"/>
    /// </summary>
    public abstract class GUIMiniMenu<T> : GUIMiniMenu where T : GUIMiniMenu<T>, new()
    {
        /// <summary>
        /// Add this to <see cref="ManModGUI"/> to begin using it
        /// </summary>
        /// <param name="name">Unique name to register in the manager by</param>
        /// <param name="stats"></param>
        /// <returns>True if it was able to register</returns>
        public static bool RegisterMenuToManager(string name, GUIDisplayStats stats = null)
        {
            return ManModGUI.AddPopupSingle<T>(name, stats);
        }
        /// <inheritdoc cref="RegisterMenuToManager(string, GUIDisplayStats)"/>
        /// <summary>
        /// <para>The altered version for </para>
        /// </summary>
        /// <param name="name">Unique name to register in the manager by</param>
        /// <param name="stats"></param>
        /// <returns></returns>
        public static bool Register1StackableMenuToManager(string name, GUIDisplayStats stats = null)
        {
            return ManModGUI.AddPopupStackable<T>(name, stats);
        }
    }
    /// <summary>
    /// <para>A simple universal menu for use</para>
    /// <inheritdoc cref="GUIMiniMenu"/>
    /// </summary>
    public class GUIMiniMenuBasic : GUIMiniMenu<GUIMiniMenuBasic>
    {
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
