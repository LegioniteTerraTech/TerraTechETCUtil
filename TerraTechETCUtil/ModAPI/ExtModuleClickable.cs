using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TerraTechETCUtil
{
    /// <summary>
    /// <inheritdoc cref=" ExtModule"/>
    /// <para>For modules that open the modal menu</para>
    /// </summary>
    public abstract class ExtModuleClickable : ExtModule, ManPointer.OpenMenuEventConsumer
    {
        private static FieldInfo cont = typeof(TankBlock).GetField("m_ContextMenuType", BindingFlags.NonPublic | BindingFlags.Instance);

        private static ExtModuleClickable openMouseStartTarg = null;
        private static Vector2 openMouseStart = Vector2.zero;
        private static float openMouseTime = 0;

        /// <summary>
        /// When it is clicked
        /// </summary>
        /// <param name="RMB"></param>
        /// <param name="down"></param>
        /// <param name="rayman"></param>
        private static void OnClick(bool RMB, bool down, RaycastHit rayman)
        {
            if (!RMB)
                return;
            if (down && rayman.collider)
            {
                var vis = ManVisible.inst.FindVisible(rayman.collider);
                if (vis)
                {
                    var targVis = vis.block;
                    if (targVis)
                    {
                        var EMC = targVis.GetComponent<ExtModuleClickable>();
                        if (EMC && EMC.UseDefault)
                            EMC.ShowDelayedNoCheck();
                    }
                }
            }
        }
        /// <summary>
        /// Use the click mouse menu
        /// </summary>
        public bool UseClick = true;
        /// <summary>
        /// If this instance is pooled
        /// </summary>
        protected bool Pooled = false;
        /// <summary>
        /// Use the vanilla modal menu
        /// </summary>
        public abstract bool UseDefault { get; }

        private static bool PoolStart = false;
        /// <summary>
        /// Insure that we are pooled
        /// </summary>
        protected virtual void PoolInsure()
        {
            if (Pooled)
                return;
            if (!PoolStart)
            {
                PoolStart = true;
                InvokeHelper.InsureInit();
                ResourcesHelper.ModsUpdateEvent.Subscribe(UpdateThis);
                InvokeHelper.ClickEventSimple.Subscribe(OnClick);
            }
            Pooled = true;
            block.TrySetBlockFlag(TankBlock.Flags.HasContextMenu, true);
            Debug_TTExt.Info("PoolInsure() Has HasContextMenu value: " + block.HasContextMenu);
            block.m_ContextMenuForPlayerTechOnly = false;
            cont.SetValue(block, UIHelpersExt.customElement);
        }

        /// <summary>
        /// Check to see if the menu can be opened
        /// </summary>
        /// <param name="radial"></param>
        /// <returns></returns>
        public bool CanOpenMenu(bool radial) => tank != null && ManPlayer.inst.PlayerTeam == tank.Team;
        /// <summary>
        /// Impossible to figure out why it's soo slow - OnOpenMenuEvent is delayed for non-native DLLs and I can't find any reason for it to do so.
        /// </summary>
        /// <param name="OMED"></param>
        /// <returns></returns>
        public bool OnOpenMenuEvent(OpenMenuEventData OMED)
        {
            if (OMED.m_AllowRadialMenu)
            {
                if (UseClick)
                    ShowDelayedNoCheck();
                return true;
            }
            else
                return false;
        }
        /// <summary>
        /// Show this on the UI modal
        /// </summary>
        public void ShowDelayedNoCheck()
        {
            if (openMouseStartTarg == null)
            {
                openMouseStartTarg = this;
                openMouseStart = ManHUD.inst.GetMousePositionOnScreen();
                openMouseTime = Time.time + UIHelpersExt.ROROpenTimeDelay;
                Debug_TTExt.Info("ShowDelayedNoCheck() - " + Time.time);
                //ManSFX.inst.PlayUISFX(ManSFX.UISfxType.LockOn);
            }
        }
        internal static void UpdateThis()
        {
            try
            {
                if (openMouseStartTarg != null && Time.time > openMouseTime)
                {
                    if ((openMouseStart - ManHUD.inst.GetMousePositionOnScreen()).sqrMagnitude < UIHelpersExt.ROROpenAllowedMouseDeltaSqr
                    && ManInput.inst.GetRadialInputController(ManInput.RadialInputController.Mouse).IsSelecting())
                        openMouseStartTarg.OnShow();
                    openMouseStartTarg = null;
                    //DebugRandAddi.Log("QueueDelayedOpen(end) - " + Time.time);
                }
            }
            catch (Exception e)
            {
                Debug_TTExt.LogError("Exception on updating " + nameof(UpdateThis) + " - " + e);
            }
        }
        /// <summary>
        /// Invoked when the modal is shown
        /// </summary>
        public abstract void OnShow();

        /// <summary>
        /// Create a new <see cref="GUI_BM_Element"/>
        /// </summary>
        /// <param name="Name">Name of the element to display on UI</param>
        /// <param name="onTriggered">Gives a float which can be processed into a set value for the element return on the ui, which is in range [0 ~ 1]</param>
        /// <param name="sprite">Sprite icon to use for it</param>
        /// <param name="sliderDescIfIsSlider">Name for the slider if this has one</param>
        /// <param name="numClampSteps">Number of positions the slider can take</param>
        /// <returns>The new element</returns>
        public static GUI_BM_Element MakeElement(string Name, Func<float, float> onTriggered, Func<Sprite> sprite, Func<string> sliderDescIfIsSlider = null, int numClampSteps = 0)
        {
            return new GUI_BM_Element_Simple()
            {
                Name = Name,
                OnIcon = sprite,
                OnDesc = sliderDescIfIsSlider,
                ClampSteps = numClampSteps,
                LastVal = 0,
                OnSet = onTriggered,
            };
        }
        /// <inheritdoc cref=" MakeElement(string, Func{float, float}, Func{Sprite}, Func{string}, int)"/>
        public static GUI_BM_Element MakeElement(LocExtString Name, Func<float, float> onTriggered, Func<Sprite> sprite, Func<string> sliderDescIfIsSlider = null, int numClampSteps = 0)
        {
            return new GUI_BM_Element_Complex()
            {
                Name = Name.ToString,
                OnIcon = sprite,
                OnDesc = sliderDescIfIsSlider,
                ClampSteps = numClampSteps,
                LastVal = 0,
                OnSet = onTriggered,
            };
        }
        /// <inheritdoc cref=" MakeElement(string, Func{float, float}, Func{Sprite}, Func{string}, int)"/>
        public static GUI_BM_Element MakeElement(Func<string> Name, Func<float, float> onTriggered, Func<Sprite> sprite, Func<string> sliderDescIfIsSlider = null, int numClampSteps = 0)
        {
            return new GUI_BM_Element_Complex()
            {
                Name = Name,
                OnIcon = sprite,
                OnDesc = sliderDescIfIsSlider,
                ClampSteps = numClampSteps,
                LastVal = 0,
                OnSet = onTriggered,
            };
        }
    }
}
