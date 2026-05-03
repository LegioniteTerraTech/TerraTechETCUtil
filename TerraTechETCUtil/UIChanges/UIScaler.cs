using System;
using UnityEngine;

namespace TerraTechETCUtil
{
    /// <summary>
    /// Allows rescaling of <b>IMGUI</b>'s UI elements.
    /// <para>Compatable with <see cref="ManModGUI"/> calls while changing <see cref="ManModGUI.GUIScale"/>, 
    /// but <b>does not change <see cref="AltUI.UIScalingMatrix"/></b></para>
    /// </summary>
    public class UIScaler
    {
        /// <summary>
        /// The cached previous matrix used before this
        /// </summary>
        public Matrix4x4 prevMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one);
        /// <summary>
        /// Cached scale of the UI in <see cref="ManModGUI"/>
        /// </summary>
        public float prevUIScale = default;
        /// <summary>
        /// Cached scale of the UI in <see cref="ManModGUI"/>
        /// </summary>
        public float prevUIWindowScale = default;
        internal Matrix4x4 UIMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one);

        /// <summary>
        /// <see cref="ApplyScaling"/> was called.  Make sure that <see cref="PrevScaling"/> is called after we are finished using the scaling effect
        /// </summary>
        public bool IsRunning { get; private set; } = false;
        /// <summary>
        /// <see cref="ApplyScaling"/> or <see cref="PrevScaling"/> was called a second time, 
        /// meaning the whole <b>IMGUI</b> system may be messed up
        /// </summary>
        public static bool IsErrored { get; private set; } = false;

        private float _UIWindowScale = 1f;
        /// <summary>
        /// Multiplier for the scale of the UI window to relative the screen when this is applied
        /// </summary>
        public float UIWindowScale => _UIScale;

        private float _UIScale = 1f;
        /// <summary>
        /// Multiplier for the scale of the UI's elements relative to the screen when this is applied
        /// </summary>
        public float UIScale => _UIScale;
        /// <summary>
        /// Inverted multiplier for the scale of the UI relative to the screen when this is applied
        /// </summary>
        public float UIScaleInv => 1f / _UIScale;
        /// <summary>
        /// The rescaled height of the main window of the game when this is applied
        /// </summary>
        public float ScaledWidth => Display.main.renderingWidth / _UIScale;
        /// <summary>
        /// The rescaled width of the main window of the game when this is applied
        /// </summary>
        public float ScaledHeight => Display.main.renderingHeight / _UIScale;
        /// <summary>
        /// Create a new IMGUI Scaler to rescale the UI.
        /// <para><b>This constructor must be called outside of the usual UI calls</b></para>
        /// </summary>
        /// <param name="scale">The new scale for the UI's elements. Cannot be zero or negative.</param>
        /// <param name="windowScale">The new scale for the UI's window. Cannot be zero or negative.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="scale"/> cannot be zero or negative.</exception>
        public UIScaler(float scale = 1f, float windowScale = 1f)
        {
            SetUIScale(scale);
            _UIWindowScale = windowScale;
        }
        /// <summary>
        /// Set the scale of the UI's contents.  It is not advised to utilize this in a OnGUI() call
        /// </summary>
        /// <param name="newScale">The new scale for the UI's contents. Cannot be zero or negative.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="newScale"/> cannot be zero or negative.</exception>
        public void SetUIScale(float newScale)
        {
            if (_UIScale != newScale)
            {
                if (newScale <= 0f)
                    throw new ArgumentOutOfRangeException(nameof(newScale) + " cannot be zero or negative.");
                _UIScale = newScale;
                UIMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * newScale);
            }
        }
        /// <summary>
        /// Set the scale of the UI's windows.  It is not advised to utilize this in a OnGUI() call
        /// </summary>
        /// <param name="newScale">The new scale for the UI's windows. Cannot be zero or negative.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="newScale"/> cannot be zero or negative.</exception>
        public void SetWindowScale(float newScale)
        {
            if (_UIWindowScale != newScale)
            {
                if (newScale <= 0f)
                    throw new ArgumentOutOfRangeException(nameof(newScale) + " cannot be zero or negative.");
                _UIWindowScale = newScale;
            }
        }

        /// <summary>
        /// Set the scale of the UI to <see cref="UIMatrix"/>
        /// <para>Use a try-finally block to call <see cref="PrevScaling"/> after you are done using this incase of a UI issue.</para>
        /// </summary>
        public void ApplyScaling()
        {
            if (IsRunning)
            {
                if (!IsErrored)
                {
                    IsErrored = true;
                    ManModGUI.ShowErrorPopup("UIScaler.ApplyScaling cannot be nest-called");
                    throw new InvalidOperationException("UIScaler.ApplyScaling cannot be nest-called");
                }
            }
            IsRunning = true;
            prevMatrix = GUI.matrix;
            prevUIWindowScale = ManModGUI.CurrentGUIWindowScale;
            prevUIScale = ManModGUI.CurrentGUIScale;
            ManModGUI.CurrentGUIScale = _UIScale;
            ManModGUI.CurrentGUIWindowScale = _UIWindowScale;
            GUI.matrix = UIMatrix;
        }
        /// <summary>
        /// Set the scale of the UI back to the previous scaling value.
        /// </summary>
        public void PrevScaling()
        {
            if (!IsRunning)
            {
                if (!IsErrored)
                {
                    IsErrored = true;
                    ManModGUI.ShowErrorPopup("UIScaler.ApplyScaling cannot be nest-called");
                    throw new InvalidOperationException("UIScaler.ApplyScaling cannot be nest-called");
                }
            }
            IsRunning = false;
            ManModGUI.CurrentGUIScale = prevUIScale;
            ManModGUI.CurrentGUIWindowScale = prevUIWindowScale;
            GUI.matrix = prevMatrix;
        }

        /// <summary>
        /// Makes this work as if it was a <see cref="ManModGUI"/> menu and manages it in relation to other GUI
        /// </summary>
        public void FinishGUIMenuAndManageHovering(Rect pos)
        {
            if (MouseIsOverGUIMenu(pos))
                ManModGUI.IsMouseOverAnyModGUI = 2;
        }

        /// <inheritdoc cref="UIHelpersExt.MouseIsOverGUIMenu(Rect)"/>
        public bool MouseIsOverGUIMenu(Rect pos)
        {
            Vector3 vector = Input.mousePosition;
            vector.y = ScaledHeight - vector.y;
            float x = pos.x;
            float num = pos.x + pos.width;
            float y = pos.y;
            float num2 = pos.y + pos.height;
            return vector.x > x && vector.x < num && vector.y > y && vector.y < num2;
        }
        /// <summary>
        /// Convert positioning of an object in scene space into this <see cref="UIScaler"/>'s space
        /// </summary>
        /// <param name="scenePos">Position in scene</param>
        /// <returns><see cref="Vector3"/> relative to the GUI rescaled by this</returns>
        public Vector3 GetGUIPos(Vector3 scenePos) =>
            Singleton.camera.WorldToScreenPoint(scenePos) * UIScaleInv;

        /// <summary>
        /// <para><b>This is the <see cref="UIScaler"/> version which only returns the UIScaler's space!</b></para>
        /// <inheritdoc cref="TerraTechETCUtil.UIHelpersExt.ClampGUIToScreen(ref Rect, bool, float, float, float)"/>
        /// </summary>
        /// <inheritdoc cref="TerraTechETCUtil.UIHelpersExt.ClampGUIToScreen(ref Rect, bool)"/>
        /// <param name="pos"></param>
        /// <param name="centerOnMouse"></param>
        /// <param name="extraSpacing"></param>
        /// <param name="centerOnMouseXOffset"></param>
        /// <param name="centerOnMouseYOffset"></param>
        public void ClampGUIToScreen(ref Rect pos, bool centerOnMouse, float extraSpacing = 0f,
            float centerOnMouseXOffset = 0.5f, float centerOnMouseYOffset = 0f)
        {
            float widthAdj = pos.width * _UIWindowScale;
            float heightAdj = pos.height * _UIWindowScale;
            if (centerOnMouse)
            {
                Vector3 vector = Input.mousePosition * UIScaleInv;
                pos.x = vector.x - widthAdj * centerOnMouseXOffset;
                pos.y = ScaledHeight - vector.y - heightAdj * centerOnMouseYOffset;
            }

            pos.x = Mathf.Clamp(pos.x, -extraSpacing, ScaledWidth - widthAdj + extraSpacing);
            pos.y = Mathf.Clamp(pos.y, -extraSpacing, ScaledHeight - heightAdj + extraSpacing);
        }

        /// <summary>
        /// <para><b>This is the <see cref="UIScaler"/> version which only returns the UIScaler's space!</b></para>
        /// <inheritdoc cref="TerraTechETCUtil.UIHelpersExt.ClampGUIToScreenNonStrict(ref Rect, float)"/>
        /// </summary>
        /// <inheritdoc cref="TerraTechETCUtil.UIHelpersExt.ClampGUIToScreenNonStrict(ref Rect, float)"/>
        /// <param name="pos"></param>
        /// <param name="extraSpacing"></param>
        public void ClampGUIToScreenNonStrict(ref Rect pos, float extraSpacing = 10f)
        {
            float widthAdj = pos.width * _UIWindowScale;
            float heightAdj = pos.height * _UIWindowScale;
            pos.x = Mathf.Clamp(pos.x, extraSpacing - widthAdj, ScaledWidth - extraSpacing);
            pos.y = Mathf.Clamp(pos.y, extraSpacing - heightAdj, ScaledHeight - extraSpacing);
        }
        /// <inheritdoc cref="TerraTechETCUtil.UIHelpersExt.ClampGUIToScreenNonStrictHeader(ref Rect, float)"/>
        /// <summary>
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="extraSpacing"></param>
        public void ClampGUIToScreenNonStrictHeader(ref Rect pos, float extraSpacing = 10f)
        {
            float widthAdj = pos.width * _UIWindowScale;
            float heightAdj = AltUI.HeaderBarHeight * _UIWindowScale;
            pos.x = Mathf.Clamp(pos.x, extraSpacing - widthAdj, ScaledWidth - extraSpacing);
            pos.y = Mathf.Clamp(pos.y, Mathf.Min(-heightAdj, extraSpacing - heightAdj), ScaledHeight - extraSpacing);
        }

        /// <summary>
        /// <para><b>This is the <see cref="UIScaler"/> version</b></para>
        /// <inheritdoc cref="AltUI.Window(int, Rect, GUI.WindowFunction, string, float, Action, Action, bool, bool, GUILayoutOption[])"/>
        /// </summary>
        /// <inheritdoc cref="AltUI.Window(int, Rect, GUI.WindowFunction, string, float, Action, Action, bool, bool, GUILayoutOption[])"/>
        /// <param name="id"></param>
        /// <param name="screenRect"></param>
        /// <param name="func"></param>
        /// <param name="title"></param>
        /// <param name="alpha"></param>
        /// <param name="closeCallback"></param>
        /// <param name="topBarExtraGUI"></param>
        /// <param name="blockCursorControl"></param>
        /// <param name="draggableHeader"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public Rect Window(int id, Rect screenRect, GUI.WindowFunction func, string title, float alpha, Action closeCallback,
            Action topBarExtraGUI, bool blockCursorControl = true, bool draggableHeader = false, params GUILayoutOption[] options)
        {
            if (ManModGUI.HideGUICompletelyWhenDragging && ManModGUI.UIFadeState)
                return screenRect;

            alpha *= AltUI.UIAlphaAuto;
            AltUI.StartUISharp(alpha, alpha);
            try
            {
                ApplyScaling();
                try
                {
                    float rescaleValue = _UIWindowScale / _UIScale;
                    screenRect.width *= rescaleValue;
                    screenRect.height *= rescaleValue;
                    screenRect = GUILayout.Window(id, screenRect, delegate
                    {
                        GUILayout.BeginHorizontal(AltUI.WindowHeaderBlue, GUILayout.Height(48f));
                        GUILayout.Label(title, AltUI.LabelBlackTitle, GUILayout.Height(48f), GUILayout.ExpandWidth(true));
                        GUILayout.FlexibleSpace();
                        topBarExtraGUI?.Invoke();
                        bool flag = false;
                        if (closeCallback != null)
                            flag = AltUI.ToggleNoFormat(false, string.Empty, AltUI.SwitchCloseInv, GUILayout.Width(48f), GUILayout.Height(48f));

                        GUILayout.EndHorizontal();
                        Rect HeaderRect = GUILayoutUtility.GetLastRect();
                        func(id);
                        AltUI.Tooltip.EndDisplayGUIToolTip();
                        if (flag)
                        {
                            Singleton.Manager<ManSFX>.inst.PlayUISFX(ManSFX.UISfxType.Close);
                            closeCallback();
                        }

                        if (draggableHeader)
                            GUI.DragWindow(new Rect(0f, 0f, HeaderRect.width, AltUI.HeaderBarHeight));
                    }, string.Empty, options);
                    screenRect.width /= rescaleValue;
                    screenRect.height /= rescaleValue;
                }
                finally
                {
                    PrevScaling();
                }
            }
            finally
            {
                AltUI.EndUI();
            }
            if (blockCursorControl && MouseIsOverGUIMenu(screenRect))
                ManModGUI.IsMouseOverAnyModGUI = 4;
            ClampGUIToScreenNonStrictHeader(ref screenRect);

            return screenRect;
        }

        /// <inheritdoc cref="Window(int, Rect, GUI.WindowFunction, string, float, Action, Action, bool, bool, GUILayoutOption[])"/>
        public Rect Window(int id, Rect screenRect, GUI.WindowFunction func, string title,
            Action closeCallback, Action topBarExtraGUI, bool blockCursorControl = true, bool draggableHeader = false, params GUILayoutOption[] options) =>
            Window(id, screenRect, func, title, 1f, closeCallback, topBarExtraGUI, blockCursorControl, draggableHeader, options);
        /// <inheritdoc cref="Window(int, Rect, GUI.WindowFunction, string, float, Action, Action, bool, bool, GUILayoutOption[])"/>
        public Rect Window(int id, Rect screenRect, GUI.WindowFunction func, string title,
            float alpha, Action closeCallback, bool blockCursorControl = true, bool draggableHeader = false, params GUILayoutOption[] options) =>
            Window(id, screenRect, func, title, alpha, closeCallback, null, blockCursorControl, draggableHeader, options);
        /// <inheritdoc cref="Window(int, Rect, GUI.WindowFunction, string, float, Action, Action, bool, bool, GUILayoutOption[])"/>
        public Rect Window(int id, Rect screenRect, GUI.WindowFunction func, string title,
            Action closeCallback, bool blockCursorControl = true, bool draggableHeader = false, params GUILayoutOption[] options) =>
            Window(id, screenRect, func, title, 1f, closeCallback, null, blockCursorControl, draggableHeader, options);
        /// <inheritdoc cref="Window(int, Rect, GUI.WindowFunction, string, float, Action, Action, bool, bool, GUILayoutOption[])"/>
        public Rect Window(int id, Rect screenRect, GUI.WindowFunction func, string title,
            bool blockCursorControl = true, bool draggableHeader = false, params GUILayoutOption[] options) =>
            Window(id, screenRect, func, title, 1f, null, null, blockCursorControl, draggableHeader, options);
    }
}
