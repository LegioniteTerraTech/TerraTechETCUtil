using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace TerraTechETCUtil
{
    /// <summary>
    /// Alternate vanilla-like UI for IMGUI used by mods
    /// <para>See <seealso cref="UIHelpersExt"/> and <seealso cref="ManModGUI"/> for more UI helpers</para>
    /// </summary>
    public static class AltUI
    {
        // INIT
        /// <summary> Curved white UI </summary>
        public static GUISkin MenuGUI;
        /// <summary> Sharp white UI </summary>
        public static GUISkin MenuSharpGUI;
        /// <summary> Default title font size </summary>
        public const int TitleFontSize = 32;

        /// <summary> Global mod UI alpha for UI managed by <see cref="AltUI"/>, <b>default fixed value</b> </summary>
        public static float UIAlpha = 0.725f;
        /// <summary> Color text tag </summary>
        public static string UIAlphaText = "<color=#454545ff>";
        /// <summary> Color text tag </summary>
        public const string UIBlueTextHUD = "<color=#6bafe4ff>";
        /// <summary> Color text tag </summary>
        public const string UIBlueTextMessage = "<color=#16aadeff>";
        /// <summary> Color text tag </summary>
        public const string UIHighlightText = UIObjectiveMarkerText;//"<color=#16aadeff>";
        /// <summary> Color text tag </summary>
        public const string UIObjectiveMarkerText = "<color=#ffff00ff>";
        /// <summary> Color text tag </summary>
        public const string UIObjectiveText = "<color=#e4ac41ff>";
        /// <summary> Color text tag </summary>
        public static string UIPlayerText = "<color=#" + ColorDefaultPlayer.ToRGBA255().ToString() + ">";
        /// <summary> Color text tag </summary>
        public static string UIFriendlyText = "<color=#" + ColorDefaultFriendly.ToRGBA255().ToString() + ">";
        /// <summary> Color text tag </summary>
        public static string UINeutralText = "<color=#" + ColorDefaultNeutral.ToRGBA255().ToString() + ">";
        /// <summary> Color text tag </summary>
        public const string UIEnemyText = "<color=#f23d3dff>";
        /// <summary> Color text tag </summary>
        public const string UILackeyText = "<color=#308db5>";
        /// <summary> Color text tag </summary>
        public const string UIWhisperText = "<color=#7d7d7d>";
        /// <summary> Color text tag </summary>
        public const string UIHintText = "<color=#a3a3a3>";
        /// <summary> Color text tag </summary>
        public const string UIThinkText = "<color=#919191>";
        /// <summary> Color text tag </summary>
        public const string UIBuyText = "<color=#7dd7ffff>";

        /// <summary> Color text tag </summary>
        public const string UITrialText = "<color=#b5b5b5>";


        /// <summary> Color text tag </summary>
        public const string UIEndColor = "</color>";

        /// <summary> Text colorer </summary>
        public static string UIHintPopupTitle(string title)
        {
            return "<color=#e4ac41ff><size=32><b><i>" + title + "</i></b></size></color>";
        }

        /// <summary> Text colorer </summary>
        public static string UIString(string stringIn)
        {
            return UIAlphaText + stringIn + "</color>";
        }
        /// <summary> Text colorer </summary>
        public static string BlueStringHUD(string stringIn)
        {
            return UIBlueTextHUD + stringIn + "</color>";
        }
        /// <summary> Text colorer </summary>
        public static string BlueStringMsg(string stringIn)
        {
            return UIBlueTextMessage + stringIn + "</color>";
        }
        /// <summary> Text colorer </summary>
        public static string HighlightString(string stringIn)
        {
            return UIHighlightText + stringIn + "</color>";
        }
        /// <summary> Text colorer </summary>
        public static string ObjectiveString(string stringIn)
        {
            return UIObjectiveText + stringIn + "</color>";
        }
        /// <summary> Text colorer </summary>
        public static string EnemyString(string stringIn)
        {
            return UIEnemyText + stringIn + "</color>";
        }
        /// <summary> Text colorer </summary>
        public static string PlayerString(string stringIn)
        {
            return ColorDefaultPlayer.ToRGBA255().ColorString(stringIn);
        }
        /// <summary> Text colorer </summary>
        public static string FriendlyString(string stringIn)
        {
            return ColorDefaultFriendly.ToRGBA255().ColorString(stringIn);
        }
        /// <summary> Text colorer </summary>
        public static string NeutralString(string stringIn)
        {
            return ColorDefaultNeutral.ToRGBA255().ColorString(stringIn);
        }
        /// <summary> Text colorer </summary>
        public static string SideCharacterString(string stringIn)
        {
            return UILackeyText + stringIn + "</color>";
        }
        /// <summary> Text colorer </summary>
        public static string WhisperString(string stringIn)
        {
            return UIWhisperText + stringIn + "</color>";
        }
        /// <summary> Text colorer </summary>
        public static string HintString(string stringIn)
        {
            return UIHintText + stringIn + "</color>";
        }
        /// <summary> Text colorer </summary>
        public static string ThinkString(string stringIn)
        {
            return UIThinkText + stringIn + "</color>";
        }
        /// <summary> Text colorer </summary>
        public static string BuyString(string stringIn)
        {
            return UIBuyText + stringIn + "</color>";
        }



        /// <summary> Default colors </summary>
        public static Color ColorDefaultGrey = new Color(0.27f, 0.27f, 0.27f, 1);
        /// <summary> Default colors </summary>
        public static Color ColorDefaultWhite = new Color(0.975f, 0.975f, 0.975f, 1);
        /// <summary> Default colors </summary>
        public static Color ColorDefaultBlue = new Color(0.4196f, 0.6863f, 0.8941f, 1);
        /// <summary> Default colors </summary>
        public static Color ColorDefaultRed = new Color(0.949f, 0.239f, 0.239f, 1);
        /// <summary> Default colors </summary>
        public static Color ColorDefaultGold = new Color(0.894f, 0.675f, 0.255f, 1);

        /// <summary> Default colors </summary>
        public static Color ColorDefaultPlayer = new Color(0.5f, 0.75f, 0.95f, 1);
        /// <summary> Default colors </summary>
        public static Color ColorDefaultEnemy = new Color(0.95f, 0.1f, 0.1f, 1);
        /// <summary> Default colors </summary>
        public static Color ColorDefaultNeutral = new Color(0.3f, 0, 0.7f, 1);
        /// <summary> Default colors </summary>
        public static Color ColorDefaultFriendly = new Color(0.2f, 0.95f, 0.2f, 1);

        /// <summary> Default game font </summary>
        public static Font ExoFont { get; private set; }
        /// <summary> Default game font </summary>
        public static Font ExoFontMediumItalic { get; private set; }
        /// <summary> Default game font </summary>
        public static Font ExoFontBold { get; private set; }
        /// <summary> Default game font </summary>
        public static Font ExoFontSemiBold { get; private set; }
        /// <summary> Default game font </summary>
        public static Font ExoFontSemiBoldItalic { get; private set; }
        /// <summary> Default game font </summary>
        public static Font ExoFontBoldItalic { get; private set; }
        /// <summary> Default game font </summary>
        public static Font ExoFontExtraBold { get; private set; }


        private static GUIStyle NewGUIElement(GUIStyle Base, Texture2D background, Color textColor)
        {
            // Setup WindowHeader
            GUIStyle styleBatch = new GUIStyle(Base);
            var state = new GUIStyleState()
            {
                background = background,
                textColor = textColor,
            };
            styleBatch.normal = state;
            styleBatch.hover = state;
            styleBatch.active = state;
            styleBatch.focused = state;
            styleBatch.onNormal = state;
            styleBatch.onHover = state;
            styleBatch.onActive = state;
            styleBatch.onFocused = state;
            return styleBatch;
        }

        private static void NewGUIButton()
        {
        }


        // -------------- Menus --------------
        /// <summary> GUI Style present </summary>
        public static GUIStyle TRANSPARENT;
        private static Texture2D TransparentTex;
        private static GUIStyleState MenuTransparentStyle;

        /// <summary> GUI Style present </summary>
        public static GUIStyle MenuCenter;
        /// <summary> GUI Style present </summary>
        public static GUIStyle MenuCenterWrapText;
        private static Texture2D MenuTexRect;
        private static GUIStyleState MenuCenterStyle;

        /// <summary> GUI Style present </summary>
        public static GUIStyle MenuLeft;
        private static Texture2D MenuTexRectLeft;
        private static GUIStyleState MenuLeftStyleLeft;

        /// <summary> GUI Style present </summary>
        public static GUIStyle MenuRight;
        private static Texture2D MenuTexRectRight;
        private static GUIStyleState MenuRightStyle;

        /// <summary> GUI Style present </summary>
        public static GUIStyle MenuSharp;
        private static Texture2D MenuSharpTexRect;
        private static GUIStyleState MenuSharpCenterStyle;

        /// <summary> GUI Style present </summary>
        public static GUIStyle MenuBubble;
        private static Texture2D MenuBubbleTexRect;
        private static GUIStyleState MenuBubbleCenterStyle;

        private static void MakeMenus()
        {
            // Setup Menus
            TRANSPARENT = new GUIStyle(GUI.skin.window);
            TRANSPARENT.font = ExoFont;
            TRANSPARENT.fontSize = 18;
            MenuTransparentStyle = new GUIStyleState() { background = TransparentTex, textColor = ColorDefaultGrey, };
            TRANSPARENT.overflow = new RectOffset(0, 0, 0, 0);
            TRANSPARENT.padding = new RectOffset(2,2,2,2);
            TRANSPARENT.border = new RectOffset(0,0,0,0);
            TRANSPARENT.normal = MenuTransparentStyle;
            TRANSPARENT.hover = MenuTransparentStyle;
            TRANSPARENT.active = MenuTransparentStyle;
            TRANSPARENT.focused = MenuTransparentStyle;
            TRANSPARENT.onNormal = MenuTransparentStyle;
            TRANSPARENT.onHover = MenuTransparentStyle;
            TRANSPARENT.onActive = MenuTransparentStyle;
            TRANSPARENT.onFocused = MenuTransparentStyle;

            MenuCenter = new GUIStyle(GUI.skin.window);
            MenuCenter.font = ExoFont;
            MenuCenter.fontSize = 18;
            MenuCenterStyle = new GUIStyleState() { background = MenuTexRect, textColor = ColorDefaultGrey, };
            MenuCenter.overflow = new RectOffset(10, 10, 10, 10);
            MenuCenter.padding = new RectOffset(0, 0, 6, 0);
            MenuCenter.border = new RectOffset(10, 10, 10, 10);
            MenuCenter.margin = new RectOffset(0, 0, 0, 0);
            MenuCenter.normal = MenuCenterStyle;
            MenuCenter.hover = MenuCenterStyle;
            MenuCenter.active = MenuCenterStyle;
            MenuCenter.focused = MenuCenterStyle;
            MenuCenter.onNormal = MenuCenterStyle;
            MenuCenter.onHover = MenuCenterStyle;
            MenuCenter.onActive = MenuCenterStyle;
            MenuCenter.onFocused = MenuCenterStyle;

            MenuCenterWrapText = new GUIStyle(MenuCenter);
            MenuCenterWrapText.font = ExoFontBold;
            MenuCenterWrapText.fontSize = 18;
            MenuCenterWrapText.wordWrap = true;
            MenuCenterWrapText.alignment = TextAnchor.MiddleCenter;


            MenuLeft = new GUIStyle(MenuCenter);
            MenuLeftStyleLeft = new GUIStyleState() { background = MenuTexRectLeft, textColor = ColorDefaultGrey, };
            //MenuLeft.padding = new RectOffset(MenuTexRectLeft.width / 16, MenuTexRectLeft.width / 16, MenuTexRectLeft.height / 18, MenuTexRectLeft.height / 18);
            //MenuLeft.border = new RectOffset(MenuTexRectLeft.width / 3, MenuTexRectLeft.width / 3, MenuTexRectLeft.height / 6, MenuTexRectLeft.height / 6);
            MenuLeft.padding = new RectOffset(MenuTexRectLeft.width / 32, MenuTexRectLeft.width / 32, MenuTexRectLeft.height / 36, MenuTexRectLeft.height / 36);
            MenuLeft.border = new RectOffset(MenuTexRectLeft.width / 6, MenuTexRectLeft.width / 6, MenuTexRectLeft.height / 12, MenuTexRectLeft.height / 12);
            MenuLeft.normal = MenuLeftStyleLeft;
            MenuLeft.hover = MenuLeftStyleLeft;
            MenuLeft.active = MenuLeftStyleLeft;
            MenuLeft.focused = MenuLeftStyleLeft;
            MenuLeft.onNormal = MenuLeftStyleLeft;
            MenuLeft.onHover = MenuLeftStyleLeft;
            MenuLeft.onActive = MenuLeftStyleLeft;
            MenuLeft.onFocused = MenuLeftStyleLeft;


            MenuRight = new GUIStyle(MenuCenter);
            MenuRightStyle = new GUIStyleState() { background = MenuTexRectRight, textColor = new Color(0, 0, 0, 1), };
            MenuRight.padding = new RectOffset(MenuTexRectRight.width / 6, MenuTexRectRight.width / 6, 
                MenuTexRectRight.height / 12, MenuTexRectRight.height / 12);
            MenuRight.border = new RectOffset(Mathf.RoundToInt(MenuTexRectRight.width / 2.25f), 
                Mathf.RoundToInt(MenuTexRectRight.width / 2.25f), MenuTexRectRight.height / 6, MenuTexRectRight.height / 6);
            MenuRight.normal = MenuRightStyle;
            MenuRight.hover = MenuRightStyle;
            MenuRight.active = MenuRightStyle;
            MenuRight.focused = MenuRightStyle;
            MenuRight.onNormal = MenuRightStyle;
            MenuRight.onHover = MenuRightStyle;
            MenuRight.onActive = MenuRightStyle;
            MenuRight.onFocused = MenuRightStyle;


            MenuSharp = new GUIStyle(MenuCenter);
            MenuSharpCenterStyle = new GUIStyleState() { 
                background = MenuSharpTexRect, 
                textColor = new Color(0, 0, 0, 1),
            };
            MenuSharp.overflow = new RectOffset(16, 16, 0, 38);
            MenuSharp.padding = new RectOffset(0, 0, 0, 0);
            MenuSharp.border = new RectOffset(16, 16, 0, 38);
            MenuSharp.margin = new RectOffset(0, 0, 0, 0);
            MenuSharp.normal = MenuSharpCenterStyle;
            MenuSharp.hover = MenuSharpCenterStyle;
            MenuSharp.active = MenuSharpCenterStyle;
            MenuSharp.focused = MenuSharpCenterStyle;
            MenuSharp.onNormal = MenuSharpCenterStyle;
            MenuSharp.onHover = MenuSharpCenterStyle;
            MenuSharp.onActive = MenuSharpCenterStyle;
            MenuSharp.onFocused = MenuSharpCenterStyle;


            MenuBubble = new GUIStyle(MenuCenter);
            MenuBubbleCenterStyle = new GUIStyleState() { background = MenuBubbleTexRect, textColor = new Color(0, 0, 0, 1), };
            MenuBubble.overflow = new RectOffset(16, 16, 0, 38);
            MenuBubble.padding = new RectOffset(16, 16, 8, 38);
            MenuBubble.border = new RectOffset(0, 0, 0, 0);
            MenuBubble.normal = MenuBubbleCenterStyle;
            MenuBubble.hover = MenuBubbleCenterStyle;
            MenuBubble.active = MenuBubbleCenterStyle;
            MenuBubble.focused = MenuBubbleCenterStyle;
            MenuBubble.onNormal = MenuBubbleCenterStyle;
            MenuBubble.onHover = MenuBubbleCenterStyle;
            MenuBubble.onActive = MenuBubbleCenterStyle;
            MenuBubble.onFocused = MenuBubbleCenterStyle;
        }



        // -------------- Labels --------------
        /// <summary> GUI Style present </summary>
        public static GUIStyle LabelBlack;
        /// <summary> GUI Style present </summary>
        public static GUIStyle LabelBlackNoStretch;
        /// <summary> GUI Style present </summary>
        public static GUIStyle LabelBlackTitle;

        /// <summary> GUI Style present </summary>
        public static GUIStyle LabelBlue;
        /// <summary> GUI Style present </summary>
        public static GUIStyle LabelBlueTitle;
        /// <summary> GUI Style present </summary>

        /// <summary> GUI Style present </summary>
        public static GUIStyle LabelWhite;
        /// <summary> GUI Style present </summary>
        public static GUIStyle LabelWhiteTitle;

        /// <summary> GUI Style present </summary>
        public static GUIStyle LabelRed;
        /// <summary> GUI Style present </summary>
        public static GUIStyle LabelRedTitle;

        /// <summary> GUI Style present </summary>
        public static GUIStyle LabelGold;
        /// <summary> GUI Style present </summary>
        public static GUIStyle LabelGoldTitle;
        private static void MakeLabels()
        {

            GUIStyle TextBase = new GUIStyle(GUI.skin.label);
            TextBase.alignment = TextAnchor.MiddleLeft;
            TextBase.fontStyle = FontStyle.Normal;
            TextBase.normal.textColor = ColorDefaultGrey;
            TextBase.font = ExoFontSemiBold;
            TextBase.clipping = TextClipping.Overflow;

            // Setup Label Black
            GUIStyle styleBatch = new GUIStyle(TextBase);
            LabelBlack = styleBatch;
            GUIStyleState styleStateBatch = new GUIStyleState()
            {
                background = null,
                textColor = ColorDefaultGrey,
            };
            var LabelBlackStyle = styleStateBatch;
            styleBatch.normal = styleStateBatch;
            styleBatch.hover = styleStateBatch;
            styleBatch.active = styleStateBatch;
            styleBatch.focused = styleStateBatch;
            styleBatch.onNormal = styleStateBatch;
            styleBatch.onHover = styleStateBatch;
            styleBatch.onActive = styleStateBatch;
            styleBatch.onFocused = styleStateBatch;

            styleBatch = new GUIStyle(LabelBlack);
            LabelBlackNoStretch = styleBatch;
            LabelBlackNoStretch.stretchHeight = false;
            LabelBlackNoStretch.stretchWidth = false;

            // Setup Label BlackTitle
            styleBatch = new GUIStyle(TextBase);
            styleBatch.font = ExoFontSemiBoldItalic;
            styleBatch.fontSize = TitleFontSize;
            styleBatch.border = new RectOffset(64, 0, 0, 0);
            LabelBlackTitle = styleBatch;
            styleStateBatch = new GUIStyleState()
            {
                background = null,
                textColor = ColorDefaultGrey,
            };
            var LabelBlackTitleStyle = styleStateBatch;
            styleBatch.normal = styleStateBatch;
            styleBatch.hover = styleStateBatch;
            styleBatch.active = styleStateBatch;
            styleBatch.focused = styleStateBatch;
            styleBatch.onNormal = styleStateBatch;
            styleBatch.onHover = styleStateBatch;
            styleBatch.onActive = styleStateBatch;
            styleBatch.onFocused = styleStateBatch;

            // Setup Label Blue
            styleBatch = new GUIStyle(TextBase);
            LabelBlue = styleBatch;
            styleStateBatch = new GUIStyleState()
            {
                background = null,
                textColor = ColorDefaultBlue,
            };
            var LabelBlueStyle = styleStateBatch;
            styleBatch.normal = styleStateBatch;
            styleBatch.hover = styleStateBatch;
            styleBatch.active = styleStateBatch;
            styleBatch.focused = styleStateBatch;
            styleBatch.onNormal = styleStateBatch;
            styleBatch.onHover = styleStateBatch;
            styleBatch.onActive = styleStateBatch;
            styleBatch.onFocused = styleStateBatch;

            // Setup Label BlueTitle
            styleBatch = new GUIStyle(LabelBlackTitle);
            LabelBlueTitle = styleBatch;
            styleStateBatch = new GUIStyleState()
            {
                background = null,
                textColor = ColorDefaultBlue,
            };
            var LabelBlueStyleTitle = styleStateBatch;
            styleBatch.normal = styleStateBatch;
            styleBatch.hover = styleStateBatch;
            styleBatch.active = styleStateBatch;
            styleBatch.focused = styleStateBatch;
            styleBatch.onNormal = styleStateBatch;
            styleBatch.onHover = styleStateBatch;
            styleBatch.onActive = styleStateBatch;
            styleBatch.onFocused = styleStateBatch;

            // Setup Label White
            styleBatch = new GUIStyle(TextBase);
            LabelWhite = styleBatch;
            styleStateBatch = new GUIStyleState()
            {
                background = null,
                textColor = ColorDefaultWhite,
            };
            var LabelWhiteStyle = styleStateBatch;
            styleBatch.normal = styleStateBatch;
            styleBatch.hover = styleStateBatch;
            styleBatch.active = styleStateBatch;
            styleBatch.focused = styleStateBatch;
            styleBatch.onNormal = styleStateBatch;
            styleBatch.onHover = styleStateBatch;
            styleBatch.onActive = styleStateBatch;
            styleBatch.onFocused = styleStateBatch;

            // Setup Label White Title
            styleBatch = new GUIStyle(LabelBlackTitle);
            LabelWhiteTitle = styleBatch;
            styleStateBatch = new GUIStyleState()
            {
                background = null,
                textColor = ColorDefaultWhite,
            };
            var LabelWhiteStyleTitle = styleStateBatch;
            styleBatch.normal = styleStateBatch;
            styleBatch.hover = styleStateBatch;
            styleBatch.active = styleStateBatch;
            styleBatch.focused = styleStateBatch;
            styleBatch.onNormal = styleStateBatch;
            styleBatch.onHover = styleStateBatch;
            styleBatch.onActive = styleStateBatch;
            styleBatch.onFocused = styleStateBatch;

            // Setup Label Red
            styleBatch = new GUIStyle(TextBase);
            LabelRed = styleBatch;
            styleStateBatch = new GUIStyleState()
            {
                background = null,
                textColor = ColorDefaultRed,
            };
            var LabelRedStyle = styleStateBatch;
            styleBatch.normal = styleStateBatch;
            styleBatch.hover = styleStateBatch;
            styleBatch.active = styleStateBatch;
            styleBatch.focused = styleStateBatch;
            styleBatch.onNormal = styleStateBatch;
            styleBatch.onHover = styleStateBatch;
            styleBatch.onActive = styleStateBatch;
            styleBatch.onFocused = styleStateBatch;

            // Setup Label Red Title
            styleBatch = new GUIStyle(LabelBlackTitle);
            LabelRedTitle = styleBatch;
            styleStateBatch = new GUIStyleState()
            {
                background = null,
                textColor = ColorDefaultRed,
            };
            var LabelRedStyleTitle = styleStateBatch;
            styleBatch.normal = styleStateBatch;
            styleBatch.hover = styleStateBatch;
            styleBatch.active = styleStateBatch;
            styleBatch.focused = styleStateBatch;
            styleBatch.onNormal = styleStateBatch;
            styleBatch.onHover = styleStateBatch;
            styleBatch.onActive = styleStateBatch;
            styleBatch.onFocused = styleStateBatch;

            // Setup Label Gold
            styleBatch = new GUIStyle(TextBase);
            LabelGold = styleBatch;
            styleStateBatch = new GUIStyleState()
            {
                background = null,
                textColor = ColorDefaultGold,
            };
            var LabelGoldStyle = styleStateBatch;
            styleBatch.normal = styleStateBatch;
            styleBatch.hover = styleStateBatch;
            styleBatch.active = styleStateBatch;
            styleBatch.focused = styleStateBatch;
            styleBatch.onNormal = styleStateBatch;
            styleBatch.onHover = styleStateBatch;
            styleBatch.onActive = styleStateBatch;
            styleBatch.onFocused = styleStateBatch;

            // Setup Label Gold Title
            styleBatch = new GUIStyle(LabelBlackTitle);
            LabelGoldTitle = styleBatch;
            styleStateBatch = new GUIStyleState()
            {
                background = null,
                textColor = ColorDefaultGold,
            };
            var LabelGoldStyleTitle = styleStateBatch;
            styleBatch.normal = styleStateBatch;
            styleBatch.hover = styleStateBatch;
            styleBatch.active = styleStateBatch;
            styleBatch.focused = styleStateBatch;
            styleBatch.onNormal = styleStateBatch;
            styleBatch.onHover = styleStateBatch;
            styleBatch.onActive = styleStateBatch;
            styleBatch.onFocused = styleStateBatch;
        }


        // -------------- Boxes --------------
        /// <summary> GUI Style present </summary>
        public static GUIStyle BoxBlack;
        /// <summary> GUI Style present </summary>
        public static GUIStyle BoxBlackTitle;

        /// <summary> GUI Style present </summary>
        public static GUIStyle BoxBlackTextBlue;
        /// <summary> GUI Style present </summary>
        public static GUIStyle BoxBlackTextBlueTitle;

        private static void MakeBoxes()
        {
            GUIStyle TextBase = new GUIStyle(TextfieldBlackHuge);
            TextBase.alignment = TextAnchor.MiddleLeft;
            TextBase.fontStyle = FontStyle.Normal;
            TextBase.normal.textColor = ColorDefaultGrey;
            TextBase.font = ExoFontSemiBold;
            TextBase.clipping = TextClipping.Overflow;

            // Setup Label Black
            GUIStyle styleBatch = new GUIStyle(TextBase);
            BoxBlack = styleBatch;
            GUIStyleState styleStateBatch = new GUIStyleState()
            {
                background = TextfieldHTexMain,
                textColor = ColorDefaultWhite,
            };
            var BoxBlackStyle = styleStateBatch;
            styleBatch.normal = styleStateBatch;
            styleBatch.hover = styleStateBatch;
            styleBatch.active = styleStateBatch;
            styleBatch.focused = styleStateBatch;
            styleBatch.onNormal = styleStateBatch;
            styleBatch.onHover = styleStateBatch;
            styleBatch.onActive = styleStateBatch;
            styleBatch.onFocused = styleStateBatch;

            // Setup Label BlackTitle
            styleBatch = new GUIStyle(TextBase);
            styleBatch.font = ExoFontSemiBoldItalic;
            styleBatch.fontSize = TitleFontSize;
            styleBatch.border = new RectOffset(64, 0, 0, 0);
            BoxBlackTitle = styleBatch;
            styleStateBatch = new GUIStyleState()
            {
                background = TextfieldHTexMain,
                textColor = ColorDefaultWhite,
            };
            var BoxBlackTitleStyle = styleStateBatch;
            styleBatch.normal = styleStateBatch;
            styleBatch.hover = styleStateBatch;
            styleBatch.active = styleStateBatch;
            styleBatch.focused = styleStateBatch;
            styleBatch.onNormal = styleStateBatch;
            styleBatch.onHover = styleStateBatch;
            styleBatch.onActive = styleStateBatch;
            styleBatch.onFocused = styleStateBatch;

            // Setup Label Blue
            styleBatch = new GUIStyle(TextBase);
            BoxBlackTextBlue = styleBatch;
            styleStateBatch = new GUIStyleState()
            {
                background = TextfieldHTexMain,
                textColor = ColorDefaultBlue,
            };
            var BoxBlackTextBlueStyle = styleStateBatch;
            styleBatch.normal = styleStateBatch;
            styleBatch.hover = styleStateBatch;
            styleBatch.active = styleStateBatch;
            styleBatch.focused = styleStateBatch;
            styleBatch.onNormal = styleStateBatch;
            styleBatch.onHover = styleStateBatch;
            styleBatch.onActive = styleStateBatch;
            styleBatch.onFocused = styleStateBatch;

            // Setup Label BlueTitle
            styleBatch = new GUIStyle(LabelBlackTitle);
            BoxBlackTextBlueTitle = styleBatch;
            styleStateBatch = new GUIStyleState()
            {
                background = TextfieldHTexMain,
                textColor = ColorDefaultBlue,
            };
            var BoxBlackTextBlueStyleTitle = styleStateBatch;
            styleBatch.normal = styleStateBatch;
            styleBatch.hover = styleStateBatch;
            styleBatch.active = styleStateBatch;
            styleBatch.focused = styleStateBatch;
            styleBatch.onNormal = styleStateBatch;
            styleBatch.onHover = styleStateBatch;
            styleBatch.onActive = styleStateBatch;
            styleBatch.onFocused = styleStateBatch;
        }


        // -------------- Buttons --------------
        /// <summary> GUI Style present </summary>
        public static GUIStyle ButtonBlue;
        private static Texture2D ButtonTexMain;
        private static Texture2D ButtonTexHover;

        /// <summary> GUI Style present </summary>
        public static GUIStyle ButtonGreen;
        private static Texture2D ButtonTexAccept;
        private static Texture2D ButtonTexAcceptHover;

        /// <summary> GUI Style present </summary>
        public static GUIStyle ButtonRed;
        private static Texture2D ButtonTexDisabled;
        private static Texture2D ButtonTexDisabledHover;

        /// <summary> GUI Style present </summary>
        public static GUIStyle ButtonBlueLarge;
        private static Texture2D ButtonSTexMain;
        private static Texture2D ButtonSTexHover;

        /// <summary> GUI Style present </summary>
        public static GUIStyle ButtonOrangeLarge;
        private static Texture2D ButtonOTexMain;
        private static Texture2D ButtonOTexHover;

        /// <summary> GUI Style present </summary>
        public static GUIStyle ButtonGreyLarge;
        private static Texture2D ButtonSDTexMain;
        private static Texture2D ButtonSDTexHover;

        // ----------------------------------
        /// <summary> GUI Style present </summary>
        public static GUIStyle ButtonGrey;
        private static Texture2D ButtonTexInactive;

        /// <summary> GUI Style present </summary>
        public static GUIStyle ButtonBlueActive;
        private static Texture2D ButtonTexSelect;

        /// <summary> GUI Style present </summary>
        public static GUIStyle ButtonGreenActive;
        private static Texture2D ButtonTexSelectGreen;

        /// <summary> GUI Style present </summary>
        public static GUIStyle ButtonRedActive;
        private static Texture2D ButtonTexSelectRed;

        /// <summary> GUI Style present </summary>
        public static GUIStyle ButtonBlueLargeActive;
        private static Texture2D ButtonSTexSelect;

        /// <summary> GUI Style present </summary>
        public static GUIStyle ButtonOrangeLargeActive;
        private static Texture2D ButtonOTexSelect;

        /// <summary> GUI Style present </summary>
        public static GUIStyle ButtonGreyLargeActive;
        private static Texture2D ButtonSDTexSelect;

        private static void MakeButtons()
        {
            GUIStyle ButtonBase = new GUIStyle(GUI.skin.button);
            ButtonBase.imagePosition = ImagePosition.ImageLeft;
            ButtonBase.overflow = new RectOffset(0, 0, 0, 0);
            ButtonBase.margin = new RectOffset(1, 1, 1, 1);
            ButtonBase.padding = new RectOffset(12, 12, 8, 8);
            ButtonBase.border = new RectOffset(12, 12, 12, 12);

            //ButtonBase.fixedWidth = 128;
            //ButtonBase.fixedHeight = 32;
            ButtonBase.stretchWidth = true;
            ButtonBase.stretchHeight = false;
            ButtonBase.clipping = TextClipping.Overflow;
            ButtonBase.font = ExoFontBold;

            // Setup Button Sharp
            ButtonBlueLarge = new GUIStyle(ButtonBase);
            ButtonBlueLarge.font = ExoFontMediumItalic;
            ButtonBlueLarge.fontSize = 28;
            ButtonBlueLarge.fixedWidth = 0;
            ButtonBlueLarge.fixedHeight = 0;
            ButtonBlueLarge.stretchWidth = true;
            ButtonBlueLarge.stretchHeight = true;
            ButtonBlueLarge.overflow = new RectOffset(20, 20, 6, 30);
            ButtonBlueLarge.border = new RectOffset(30, 50, 20, 60);
            var ButtonSStyle = new GUIStyleState() { background = ButtonSTexMain, textColor = ColorDefaultWhite, };
            var ButtonSStyleHover = new GUIStyleState() { background = ButtonSTexHover, textColor = ColorDefaultWhite, };
            ButtonBlueLarge.normal = ButtonSStyle;
            ButtonBlueLarge.hover = ButtonSStyleHover;
            ButtonBlueLarge.active = ButtonSStyle;
            ButtonBlueLarge.focused = ButtonSStyle;
            ButtonBlueLarge.onNormal = ButtonSStyle;
            ButtonBlueLarge.onHover = ButtonSStyleHover;
            ButtonBlueLarge.onActive = ButtonSStyle;
            ButtonBlueLarge.onFocused = ButtonSStyle;


            // Setup Button Sharp Active
            ButtonBlueLargeActive = new GUIStyle(ButtonBlueLarge);
            var ButtonSStyleActive = new GUIStyleState() { background = ButtonSTexSelect, textColor = ColorDefaultWhite, };
            ButtonBlueLargeActive.normal = ButtonSStyleActive;
            ButtonBlueLargeActive.hover = ButtonSStyleActive;
            ButtonBlueLargeActive.active = ButtonSStyleActive;
            ButtonBlueLargeActive.focused = ButtonSStyleActive;
            ButtonBlueLargeActive.onNormal = ButtonSStyleActive;
            ButtonBlueLargeActive.onHover = ButtonSStyleActive;
            ButtonBlueLargeActive.onActive = ButtonSStyleActive;
            ButtonBlueLargeActive.onFocused = ButtonSStyleActive;

            ButtonBlueLarge.active = ButtonSStyleActive;
            ButtonBlueLarge.onActive = ButtonSStyleActive;


            // Setup Button Orange
            ButtonOrangeLarge = new GUIStyle(ButtonBlueLarge);
            var ButtonOStyle = new GUIStyleState() { background = ButtonOTexMain, textColor = ColorDefaultWhite, };
            var ButtonOStyleHover = new GUIStyleState() { background = ButtonOTexHover, textColor = ColorDefaultWhite, };
            ButtonOrangeLarge.normal = ButtonOStyle;
            ButtonOrangeLarge.hover = ButtonOStyleHover;
            ButtonOrangeLarge.active = ButtonOStyle;
            ButtonOrangeLarge.focused = ButtonOStyle;
            ButtonOrangeLarge.onNormal = ButtonOStyle;
            ButtonOrangeLarge.onHover = ButtonOStyleHover;
            ButtonOrangeLarge.onActive = ButtonOStyle;
            ButtonOrangeLarge.onFocused = ButtonOStyle;

            // Setup Button Orange Active
            ButtonOrangeLargeActive = new GUIStyle(ButtonOrangeLarge);
            var ButtonOStyleActive = new GUIStyleState() { background = ButtonOTexSelect, textColor = ColorDefaultWhite, };
            ButtonOrangeLargeActive.normal = ButtonOStyleActive;
            ButtonOrangeLargeActive.hover = ButtonOStyleActive;
            ButtonOrangeLargeActive.active = ButtonOStyleActive;
            ButtonOrangeLargeActive.focused = ButtonOStyleActive;
            ButtonOrangeLargeActive.onNormal = ButtonOStyleActive;
            ButtonOrangeLargeActive.onHover = ButtonOStyleActive;
            ButtonOrangeLargeActive.onActive = ButtonOStyleActive;
            ButtonOrangeLargeActive.onFocused = ButtonOStyleActive;

            ButtonOrangeLarge.active = ButtonOStyleActive;
            ButtonOrangeLarge.onActive = ButtonOStyleActive;


            // Setup Button Grey Sharp
            ButtonGreyLarge = new GUIStyle(ButtonBlueLarge);
            var ButtonSDStyle = new GUIStyleState() { background = ButtonSDTexMain, textColor = ColorDefaultWhite, };
            var ButtonSDStyleHover = new GUIStyleState() { background = ButtonSDTexHover, textColor = ColorDefaultWhite, };
            ButtonGreyLarge.normal = ButtonSDStyle;
            ButtonGreyLarge.hover = ButtonSDStyleHover;
            ButtonGreyLarge.active = ButtonSDStyle;
            ButtonGreyLarge.focused = ButtonSDStyle;
            ButtonGreyLarge.onNormal = ButtonSDStyle;
            ButtonGreyLarge.onHover = ButtonSDStyleHover;
            ButtonGreyLarge.onActive = ButtonSDStyle;
            ButtonGreyLarge.onFocused = ButtonSDStyle;

            // Setup Button Grey Sharp Active
            ButtonGreyLargeActive = new GUIStyle(ButtonGreyLarge);
            var ButtonSDStyleActive = new GUIStyleState() { background = ButtonSDTexSelect, textColor = ColorDefaultWhite, };
            ButtonGreyLargeActive.normal = ButtonSDStyleActive;
            ButtonGreyLargeActive.hover = ButtonSDStyleActive;
            ButtonGreyLargeActive.active = ButtonSDStyleActive;
            ButtonGreyLargeActive.focused = ButtonSDStyleActive;
            ButtonGreyLargeActive.onNormal = ButtonSDStyleActive;
            ButtonGreyLargeActive.onHover = ButtonSDStyleActive;
            ButtonGreyLargeActive.onActive = ButtonSDStyleActive;
            ButtonGreyLargeActive.onFocused = ButtonSDStyleActive;

            ButtonGreyLarge.active = ButtonSDStyleActive;
            ButtonGreyLarge.onActive = ButtonSDStyleActive;


            // Setup Button Default
            ButtonBlue = new GUIStyle(ButtonBase);
            var ButtonStyle = new GUIStyleState() { background = ButtonTexMain, textColor = ColorDefaultWhite, };
            var ButtonStyleHover = new GUIStyleState() { background = ButtonTexHover, textColor = ColorDefaultWhite, };
            ButtonBlue.normal = ButtonStyle;
            ButtonBlue.hover = ButtonStyleHover;
            ButtonBlue.active = ButtonStyle;
            ButtonBlue.focused = ButtonStyle;
            ButtonBlue.onNormal = ButtonStyle;
            ButtonBlue.onHover = ButtonStyleHover;
            ButtonBlue.onActive = ButtonStyle;
            ButtonBlue.onFocused = ButtonStyle;

            // Setup Button Active
            ButtonBlueActive = new GUIStyle(ButtonBase);
            var ButtonStyleActive = new GUIStyleState() { background = ButtonTexSelect, textColor = ColorDefaultWhite, };
            ButtonBlueActive.normal = ButtonStyleActive;
            ButtonBlueActive.hover = ButtonStyleActive;
            ButtonBlueActive.active = ButtonStyleActive;
            ButtonBlueActive.focused = ButtonStyleActive;
            ButtonBlueActive.onNormal = ButtonStyleActive;
            ButtonBlueActive.onHover = ButtonStyleActive;
            ButtonBlueActive.onActive = ButtonStyleActive;
            ButtonBlueActive.onFocused = ButtonStyleActive;

            ButtonBlue.active = ButtonStyleActive;
            ButtonBlue.onActive = ButtonStyleActive;


            // Setup Button Accept
            ButtonGreen = new GUIStyle(ButtonBase);
            var ButtonStyleAccept = new GUIStyleState() { background = ButtonTexAccept, textColor = ColorDefaultWhite, };
            var ButtonStyleAcceptHover = new GUIStyleState() { background = ButtonTexAcceptHover, textColor = ColorDefaultWhite, };
            ButtonGreen.normal = ButtonStyleAccept;
            ButtonGreen.hover = ButtonStyleAcceptHover;
            ButtonGreen.active = ButtonStyleAccept;
            ButtonGreen.focused = ButtonStyleAccept;
            ButtonGreen.onNormal = ButtonStyleAccept;
            ButtonGreen.onHover = ButtonStyleAcceptHover;
            ButtonGreen.onActive = ButtonStyleAccept;
            ButtonGreen.onFocused = ButtonStyleAccept;

            // Setup Button Green Active
            ButtonGreenActive = new GUIStyle(ButtonBase);
            var ButtonStyleGActive = new GUIStyleState() { background = ButtonTexSelectGreen, textColor = ColorDefaultWhite, };
            ButtonGreenActive.normal = ButtonStyleGActive;
            ButtonGreenActive.hover = ButtonStyleGActive;
            ButtonGreenActive.active = ButtonStyleGActive;
            ButtonGreenActive.focused = ButtonStyleGActive;
            ButtonGreenActive.onNormal = ButtonStyleGActive;
            ButtonGreenActive.onHover = ButtonStyleGActive;
            ButtonGreenActive.onActive = ButtonStyleGActive;
            ButtonGreenActive.onFocused = ButtonStyleGActive;

            ButtonGreen.active = ButtonStyleGActive;
            ButtonGreen.onActive = ButtonStyleGActive;


            // Setup Button Disabled
            ButtonRed = new GUIStyle(ButtonBase);
            var ButtonStyleDisabled = new GUIStyleState() { background = ButtonTexDisabled, textColor = ColorDefaultWhite, };
            var ButtonStyleDisabledHover = new GUIStyleState() { background = ButtonTexDisabledHover, textColor = ColorDefaultWhite, };
            ButtonRed.normal = ButtonStyleDisabled;
            ButtonRed.hover = ButtonStyleDisabledHover;
            ButtonRed.active = ButtonStyleDisabled;
            ButtonRed.focused = ButtonStyleDisabled;
            ButtonRed.onNormal = ButtonStyleDisabled;
            ButtonRed.onHover = ButtonStyleDisabledHover;
            ButtonRed.onActive = ButtonStyleDisabled;
            ButtonRed.onFocused = ButtonStyleDisabled;

            // Setup Button Red Active
            ButtonRedActive = new GUIStyle(ButtonBase);
            var ButtonStyleRActive = new GUIStyleState() { background = ButtonTexSelectRed, textColor = ColorDefaultWhite, };
            ButtonRedActive.normal = ButtonStyleRActive;
            ButtonRedActive.hover = ButtonStyleRActive;
            ButtonRedActive.active = ButtonStyleRActive;
            ButtonRedActive.focused = ButtonStyleRActive;
            ButtonRedActive.onNormal = ButtonStyleRActive;
            ButtonRedActive.onHover = ButtonStyleRActive;
            ButtonRedActive.onActive = ButtonStyleRActive;
            ButtonRedActive.onFocused = ButtonStyleRActive;

            ButtonRed.active = ButtonStyleRActive;
            ButtonRed.onActive = ButtonStyleRActive;


            // Setup Button Not Active
            ButtonGrey = new GUIStyle(ButtonBase);
            var ButtonStyleInactive = new GUIStyleState() { background = ButtonTexInactive, textColor = ColorDefaultWhite, };
            ButtonGrey.normal = ButtonStyleInactive;
            ButtonGrey.hover = ButtonStyleInactive;
            ButtonGrey.active = ButtonStyleInactive;
            ButtonGrey.focused = ButtonStyleInactive;
            ButtonGrey.onNormal = ButtonStyleInactive;
            ButtonGrey.onHover = ButtonStyleInactive;
            ButtonGrey.onActive = ButtonStyleInactive;
            ButtonGrey.onFocused = ButtonStyleInactive;
        }



        // -------------- Textfields --------------
        /// <summary>
        /// Panel_BLUE_BG
        /// </summary>
        public static GUIStyle TextfieldBlue;
        private static Texture2D TextfieldUTexMain;

        /// <summary> GUI Style present </summary>
        public static GUIStyle TextfieldBlack;
        private static Texture2D TextfieldTexMain;

        /// <summary> GUI Style present </summary>
        public static GUIStyle TextfieldBlackAdjusted;

        /// <summary> GUI Style present </summary>
        public static GUIStyle TextfieldBordered;
        private static Texture2D TextfieldBTexMain;

        /// <summary>
        /// STD_Text_Box_01
        /// </summary>
        public static GUIStyle TextfieldBorderedBlue;
        private static Texture2D TextfieldBBTexMain;

        /// <summary> GUI Style present </summary>
        public static GUIStyle TextfieldBlackHuge;
        private static Texture2D TextfieldHTexMain;
        /// <summary> GUI Style present </summary>
        public static GUIStyle TextfieldBlackBlueText;

        /// <summary> GUI Style present </summary>
        public static GUIStyle TextfieldWhiteHuge;
        /// <summary> GUI Style present </summary>
        public static GUIStyle TextfieldWhiteMenu;

        // ----------------------------------
        /// <summary> GUI Style present </summary>
        public static GUIStyle TextfieldBlackSearch;
        private static Texture2D TextfieldSTexMain;

        /// <summary> GUI Style present </summary>
        public static GUIStyle TextfieldBlackLeft;
        private static Texture2D TextfieldLTexMain;

        private static void MakeTextBoxes()
        {
            GUIStyle TextBase = new GUIStyle(GUI.skin.button);
            TextBase.clipping = TextClipping.Overflow;
            TextBase.alignment = TextAnchor.MiddleLeft;
            TextBase.overflow = new RectOffset(0, 0, 0, 0);
            TextBase.padding = new RectOffset(22, 22, 3, 3);
            TextBase.border = new RectOffset(TextfieldBTexMain.width / 3, TextfieldBTexMain.width / 3, TextfieldBTexMain.height / 3, TextfieldBTexMain.height / 3);

            //TextBase.fixedWidth = 128;
            TextBase.stretchWidth = true;
            TextBase.stretchHeight = false;
            TextBase.font = ExoFontSemiBold;

            // Setup Textfield Blue
            GUIStyle styleBatch = new GUIStyle(TextBase);
            styleBatch.border = new RectOffset(12,12,12,12);
            styleBatch.padding = new RectOffset(8, 8, 3, 3);
            TextfieldBlue = styleBatch;
            GUIStyleState styleStateBatch = new GUIStyleState()
            {
                background = TextfieldUTexMain,
                textColor = ColorDefaultWhite,
            };
            styleBatch.normal = styleStateBatch;
            styleBatch.hover = styleStateBatch;
            styleBatch.active = styleStateBatch;
            styleBatch.focused = styleStateBatch;
            styleBatch.onNormal = styleStateBatch;
            styleBatch.onHover = styleStateBatch;
            styleBatch.onActive = styleStateBatch;
            styleBatch.onFocused = styleStateBatch;

            // Setup Textfield Default
            styleBatch = new GUIStyle(TextBase);
            styleBatch.alignment = TextAnchor.MiddleRight;
            styleBatch.padding = new RectOffset(TextfieldTexMain.width / 6, TextfieldTexMain.width / 6, 3, 3);
            styleBatch.border = new RectOffset(TextfieldTexMain.width / 6, TextfieldTexMain.width / 6, 0, 0);
            TextfieldBlack = styleBatch;
            styleStateBatch = new GUIStyleState()
            {
                background = TextfieldTexMain,
                textColor = ColorDefaultGrey,
            };
            styleBatch.normal = styleStateBatch;
            styleBatch.hover = styleStateBatch;
            styleBatch.active = styleStateBatch;
            styleBatch.focused = styleStateBatch;
            styleBatch.onNormal = styleStateBatch;
            styleBatch.onHover = styleStateBatch;
            styleBatch.onActive = styleStateBatch;
            styleBatch.onFocused = styleStateBatch;

            styleBatch = new GUIStyle(TextfieldBlack);
            TextfieldBlackAdjusted = styleBatch;


            // Setup Textfield Left
            styleBatch = new GUIStyle(TextfieldBlack);
            styleBatch.padding = new RectOffset(3, TextfieldTexMain.width / 6, 3, 3);
            styleBatch.border = new RectOffset(0, TextfieldTexMain.width / 6, 0, 0);
            TextfieldBlackLeft = styleBatch;
            styleStateBatch = new GUIStyleState()
            {
                background = TextfieldLTexMain,
                textColor = ColorDefaultGrey,
            };
            styleBatch.normal = styleStateBatch;
            styleBatch.hover = styleStateBatch;
            styleBatch.active = styleStateBatch;
            styleBatch.focused = styleStateBatch;
            styleBatch.onNormal = styleStateBatch;
            styleBatch.onHover = styleStateBatch;
            styleBatch.onActive = styleStateBatch;
            styleBatch.onFocused = styleStateBatch;

            // Setup Textfield Search
            styleBatch = new GUIStyle(TextfieldBlack);
            styleBatch.border = new RectOffset(Mathf.RoundToInt(TextfieldSTexMain.width / 2.25f),
                Mathf.RoundToInt(TextfieldSTexMain.width / 2.25f), 0, 0);
            RectOffset alt = styleBatch.border;
            alt.top = 3;
            alt.bottom = 3;
            styleBatch.padding = alt;
            TextfieldBlackSearch = styleBatch;
            styleStateBatch = new GUIStyleState()
            {
                background = TextfieldSTexMain,
                textColor = ColorDefaultGrey,
            };
            styleBatch.normal = styleStateBatch;
            styleBatch.hover = styleStateBatch;
            styleBatch.active = styleStateBatch;
            styleBatch.focused = styleStateBatch;
            styleBatch.onNormal = styleStateBatch;
            styleBatch.onHover = styleStateBatch;
            styleBatch.onActive = styleStateBatch;
            styleBatch.onFocused = styleStateBatch;

            // Setup Textfield Huge
            styleBatch = new GUIStyle(TextBase);
            styleBatch.border = new RectOffset(16, 16, 16, 16);
            styleBatch.clipping = TextClipping.Clip;
            styleBatch.alignment = TextAnchor.UpperLeft;
            styleBatch.fixedWidth = 0;
            styleBatch.fixedHeight = 0;
            styleBatch.stretchHeight = true;
            styleBatch.wordWrap = true;
            TextfieldBlackHuge = styleBatch;
            styleStateBatch = new GUIStyleState()
            {
                background = TextfieldHTexMain,
                textColor = ColorDefaultWhite,
            };
            styleBatch.normal = styleStateBatch;
            styleBatch.hover = styleStateBatch;
            styleBatch.active = styleStateBatch;
            styleBatch.focused = styleStateBatch;
            styleBatch.onNormal = styleStateBatch;
            styleBatch.onHover = styleStateBatch;
            styleBatch.onActive = styleStateBatch;
            styleBatch.onFocused = styleStateBatch;

            styleBatch = new GUIStyle(TextfieldBlackHuge);
            styleBatch.font = ExoFontSemiBoldItalic;

            TextfieldBlackBlueText = styleBatch;
            styleStateBatch = new GUIStyleState()
            {
                background = TextfieldHTexMain,
                textColor = ColorDefaultWhite,
            };
            styleBatch.normal = styleStateBatch;
            styleBatch.hover = styleStateBatch;
            styleBatch.active = styleStateBatch;
            styleBatch.focused = styleStateBatch;
            styleBatch.onNormal = styleStateBatch;
            styleBatch.onHover = styleStateBatch;
            styleBatch.onActive = styleStateBatch;
            styleBatch.onFocused = styleStateBatch;

            // Setup Textfield White Huge
            styleBatch = new GUIStyle(TextfieldBlackHuge);
            styleBatch.overflow = new RectOffset(0, 0, 0, 0);
            styleBatch.padding = new RectOffset(16, 16, 16, 16);
            styleBatch.border = new RectOffset(16, 16, 16, 16);
            TextfieldWhiteHuge = styleBatch;
            styleStateBatch = new GUIStyleState()
            {
                background = MenuTexRect,
                textColor = ColorDefaultGrey,
            };
            styleBatch.normal = styleStateBatch;
            styleBatch.hover = styleStateBatch;
            styleBatch.active = styleStateBatch;
            styleBatch.focused = styleStateBatch;
            styleBatch.onNormal = styleStateBatch;
            styleBatch.onHover = styleStateBatch;
            styleBatch.onActive = styleStateBatch;
            styleBatch.onFocused = styleStateBatch;



            // Setup Textfield Bordered
            styleBatch = new GUIStyle(TextBase);
            styleBatch.padding = new RectOffset(9, 9, 9, 9);
            styleBatch.border = new RectOffset(10, 10, 10, 10);
            //styleBatch.border = new RectOffset(TextfieldBTexMain.width / 3, TextfieldBTexMain.width / 3, TextfieldBTexMain.height / 3, TextfieldBTexMain.height / 3);
            TextfieldBordered = styleBatch;
            styleStateBatch = new GUIStyleState()
            {
                background = TextfieldBTexMain,
                textColor = ColorDefaultGrey,
            };
            styleBatch.normal = styleStateBatch;
            styleBatch.hover = styleStateBatch;
            styleBatch.active = styleStateBatch;
            styleBatch.focused = styleStateBatch;
            styleBatch.onNormal = styleStateBatch;
            styleBatch.onHover = styleStateBatch;
            styleBatch.onActive = styleStateBatch;
            styleBatch.onFocused = styleStateBatch;

            // Setup Textfield Blue
            styleBatch = new GUIStyle(TextBase);
            styleBatch.padding = new RectOffset(8, 8, 8, 8);
            styleBatch.border = new RectOffset(12, 12, 12, 12);
            //styleBatch.border = new RectOffset(TextfieldBBTexMain.width / 3, TextfieldBBTexMain.width / 3, TextfieldBBTexMain.height / 3, TextfieldBBTexMain.height / 3);
            TextfieldBorderedBlue = styleBatch;
            styleStateBatch = new GUIStyleState()
            {
                background = TextfieldBBTexMain,
                textColor = ColorDefaultWhite,
            };
            styleBatch.normal = styleStateBatch;
            styleBatch.hover = styleStateBatch;
            styleBatch.active = styleStateBatch;
            styleBatch.focused = styleStateBatch;
            styleBatch.onNormal = styleStateBatch;
            styleBatch.onHover = styleStateBatch;
            styleBatch.onActive = styleStateBatch;
            styleBatch.onFocused = styleStateBatch;


            TextfieldWhiteMenu = new GUIStyle(TextBase);
            TextfieldWhiteMenu.wordWrap = true;
            var TextfieldWhiteMenuStyle = new GUIStyleState() { background = MenuTexRectLeft, textColor = ColorDefaultGrey, };
            TextfieldWhiteMenu.padding = new RectOffset(MenuTexRectLeft.width / 6, MenuTexRectLeft.width / 6, MenuTexRectLeft.height / 12, MenuTexRectLeft.height / 12);
            TextfieldWhiteMenu.border = new RectOffset(MenuTexRectLeft.width / 3, MenuTexRectLeft.width / 3, MenuTexRectLeft.height / 6, MenuTexRectLeft.height / 6);
            TextfieldWhiteMenu.normal = TextfieldWhiteMenuStyle;
            TextfieldWhiteMenu.hover = TextfieldWhiteMenuStyle;
            TextfieldWhiteMenu.active = TextfieldWhiteMenuStyle;
            TextfieldWhiteMenu.focused = TextfieldWhiteMenuStyle;
            TextfieldWhiteMenu.onNormal = TextfieldWhiteMenuStyle;
            TextfieldWhiteMenu.onHover = TextfieldWhiteMenuStyle;
            TextfieldWhiteMenu.onActive = TextfieldWhiteMenuStyle;
            TextfieldWhiteMenu.onFocused = TextfieldWhiteMenuStyle;
        }



        // -------------- Switches --------------
        /// <summary> GUI Style present </summary>
        public static GUIStyle SwitchDefault;
        private static Texture2D SwitchTexOff;
        private static Texture2D SwitchTexOn;

        /// <summary> GUI Style present </summary>
        public static GUIStyle SwitchSlider;
        private static Texture2D SwitchSTexMain;
        private static Texture2D SwitchSTexMainInv;
        private static Texture2D SwitchSTexMainAlt;

        /// <summary> GUI Style present </summary>
        public static GUIStyle SwitchClose;
        /// <summary> GUI Style present </summary>
        public static GUIStyle SwitchCloseInv;
        private static Texture2D SwitchCTexOff;
        private static Texture2D SwitchCTexOn;

        private static void MakeSwitches()
        {
            GUIStyle SwitchBase = new GUIStyle(GUI.skin.toggle);
            SwitchBase.clipping = TextClipping.Clip;
            SwitchBase.alignment = TextAnchor.MiddleLeft;
            SwitchBase.overflow = new RectOffset(0, 0, 0, 0);
            SwitchBase.padding = new RectOffset(0, 0, 0, 0);
            SwitchBase.border = new RectOffset(0, 0, 0, 0);
            SwitchBase.fixedWidth = 32;
            SwitchBase.fixedHeight = 32;
            SwitchBase.stretchWidth = true;
            SwitchBase.stretchHeight = true;
            SwitchBase.font = ExoFontSemiBold;

            // Setup Switch Default
            GUIStyle styleBatch = new GUIStyle(SwitchBase);
            SwitchDefault = styleBatch;
            styleBatch.padding = new RectOffset(21, 21, 42, 42);
            GUIStyleState styleOffBatch = new GUIStyleState()
            {
                background = SwitchTexOff,
                textColor = ColorDefaultWhite,
            };
            GUIStyleState styleOnBatch = new GUIStyleState()
            {
                background = SwitchTexOn,
                textColor = ColorDefaultWhite,
            };
            styleBatch.normal = styleOffBatch;
            styleBatch.hover = styleOffBatch;
            styleBatch.active = styleOffBatch;
            styleBatch.focused = styleOffBatch;
            styleBatch.onNormal = styleOnBatch;
            styleBatch.onHover = styleOnBatch;
            styleBatch.onActive = styleOnBatch;
            styleBatch.onFocused = styleOnBatch;

            // Setup Switch Slider
            styleBatch = new GUIStyle(SwitchBase);
            SwitchSlider = styleBatch;
            styleBatch.fixedWidth = 64;
            styleOffBatch = new GUIStyleState()
            {
                background = SwitchSTexMain,
                textColor = ColorDefaultWhite,
            };
            styleOnBatch = new GUIStyleState()
            {
                background = SwitchSTexMainAlt,
                textColor = ColorDefaultWhite,
            };
            styleBatch.normal = styleOffBatch;
            styleBatch.hover = styleOffBatch;
            styleBatch.active = styleOffBatch;
            styleBatch.focused = styleOffBatch;
            styleBatch.onNormal = styleOnBatch;
            styleBatch.onHover = styleOnBatch;
            styleBatch.onActive = styleOnBatch;
            styleBatch.onFocused = styleOnBatch;

            // Setup Switch Close Button
            styleBatch = new GUIStyle(SwitchBase);
            styleBatch.fixedWidth = 0;
            styleBatch.fixedHeight = 0;
            SwitchClose = styleBatch;
            styleBatch.padding = new RectOffset(21, 21, 42, 42);
            styleOffBatch = new GUIStyleState()
            {
                background = SwitchCTexOff,
                textColor = ColorDefaultWhite,
            };
            styleOnBatch = new GUIStyleState()
            {
                background = SwitchCTexOn,
                textColor = ColorDefaultWhite,
            };
            styleBatch.normal = styleOffBatch;
            styleBatch.hover = styleOnBatch;
            styleBatch.active = styleOffBatch;
            styleBatch.focused = styleOffBatch;
            styleBatch.onNormal = styleOnBatch;
            styleBatch.onHover = styleOffBatch;
            styleBatch.onActive = styleOnBatch;
            styleBatch.onFocused = styleOnBatch;

            styleBatch = new GUIStyle(SwitchClose);
            SwitchCloseInv = styleBatch;
            styleBatch.normal = styleOnBatch;
            styleBatch.hover = styleOffBatch;
            styleBatch.active = styleOnBatch;
            styleBatch.focused = styleOnBatch;
            styleBatch.onNormal = styleOffBatch;
            styleBatch.onHover = styleOnBatch;
            styleBatch.onActive = styleOffBatch;
            styleBatch.onFocused = styleOffBatch;
        }


        // -------------- Scrollbars --------------
        /// <summary> GUI Style present </summary>
        public static GUIStyle ScrollThumb;
        private static Texture2D ScrollThumbTex;
        /// <summary> GUI Style present </summary>
        public static GUIStyle ScrollThumbTransparent;
        private static Texture2D ScrollThumbTransparentTex;


        private static Texture2D ScrollBarTex;
        /// <summary> GUI Style present </summary>
        public static GUIStyle ScrollVertical;
        /// <summary> GUI Style present </summary>
        public static GUIStyle ScrollHorizontal;
        /// <summary> GUI Style present </summary>
        public static GUIStyle ScrollVerticalTransparent;
        /// <summary> GUI Style present </summary>
        public static GUIStyle ScrollHorizontalTransparent;

        private static void MakeScrollers()
        {
            GUIStyle TextBase = new GUIStyle(SwitchDefault);
            TextBase.alignment = TextAnchor.MiddleLeft;
            TextBase.fontStyle = FontStyle.Normal;
            TextBase.normal.textColor = ColorDefaultGrey;
            TextBase.fixedWidth = 0;
            TextBase.fixedHeight = 0;
            TextBase.overflow = new RectOffset(0, 0, 0, 0);
            TextBase.padding = new RectOffset(0, 0, 0, 0);
            TextBase.border = new RectOffset(0, 0, 0, 0);
            TextBase.margin = new RectOffset(0, 0, 0, 0);
            TextBase.font = ExoFontSemiBold;


            // Setup Scroll Thumb
            GUIStyle styleBatch = new GUIStyle(TextBase);
            ScrollThumb = styleBatch;
            GUIStyleState styleStateBatch = new GUIStyleState()
            {
                background = ScrollThumbTex,
                textColor = ColorDefaultWhite,
            };
            AssignStyleQuick(styleBatch, styleStateBatch);

            // Setup Scroll Thumb Trans
            styleBatch = new GUIStyle(TextBase);
            ScrollThumbTransparent = styleBatch;
            styleStateBatch = new GUIStyleState()
            {
                background = ScrollThumbTransparentTex,
                textColor = ColorDefaultWhite,
            };
            AssignStyleQuick(styleBatch, styleStateBatch);
            

            // Setup ScrollBar Vertical
            styleBatch = new GUIStyle(ScrollThumb);
            styleBatch.fixedWidth = 12;
            styleBatch.fixedHeight = 0;
            styleBatch.stretchHeight = true;
            ScrollVertical = styleBatch;
            styleStateBatch = new GUIStyleState()
            {
                background = ScrollBarTex,
                textColor = ColorDefaultGrey,
            };
            AssignStyleQuick(styleBatch, styleStateBatch);

            // Setup ScrollBar Horizontal
            styleBatch = new GUIStyle(ScrollThumb);
            styleBatch.fixedWidth = 0;
            styleBatch.fixedHeight = 12;
            styleBatch.stretchWidth = true;
            ScrollHorizontal = styleBatch;
            styleStateBatch = new GUIStyleState()
            {
                background = ScrollBarTex,
                textColor = ColorDefaultGrey,
            };
            AssignStyleQuick(styleBatch, styleStateBatch);


            // Setup ScrollBar Vertical
            styleBatch = new GUIStyle(ScrollThumb);
            styleBatch.fixedWidth = 12;
            styleBatch.fixedHeight = 24;
            styleBatch.stretchHeight = true;
            ScrollVerticalTransparent = styleBatch;
            styleStateBatch = new GUIStyleState()
            {
                background = TransparentTex,
                textColor = ColorDefaultGrey,
            };
            AssignStyleQuick(styleBatch, styleStateBatch);

            // Setup ScrollBar Horizontal
            styleBatch = new GUIStyle(ScrollThumb);
            styleBatch.fixedWidth = 24;
            styleBatch.fixedHeight = 12;
            styleBatch.stretchWidth = true;
            ScrollHorizontalTransparent = styleBatch;
            styleStateBatch = new GUIStyleState()
            {
                background = TransparentTex,
                textColor = ColorDefaultGrey,
            };
            AssignStyleQuick(styleBatch, styleStateBatch);
        }


        /// <summary> GUI Style present </summary>
        public static GUIStyle WindowHeaderBlue;
        private static Texture2D WindowHeaderBlueTex;

        /// <summary> GUI Style present </summary>
        public static GUIStyle WindowRightBlue;
        private static Texture2D WindowRightBlueTex;


        /// <summary> GUI Style present </summary>
        public static GUIStyle ElementShadow;
        private static Texture2D ElementShadowTex;
        private static Texture2D ElementShadowSelectedTex;

        /// <summary> GUI Style present </summary>
        public static GUIStyle ListBG_DBlue;
        private static Texture2D ListBG_DBlueTex;

        /// <summary> GUI Style present </summary>
        public static GUIStyle Dropdown;
        private static Texture2D DropdownTex;
        private static Texture2D DropdownActiveTex;

        /// <summary> GUI Style present </summary>
        public static GUIStyle DropdownLight;
        private static Texture2D Dropdown2Tex;
        private static Texture2D Dropdown2ActiveTex;

        /// <summary> GUI Style present </summary>
        public static GUIStyle DropdownArrow;
        private static Texture2D DropdownArrowTex;
        /// <summary> GUI Style present </summary>
        public static GUIStyle DropdownArrowFill;
        private static Texture2D DropdownArrowFillTex;
        private static bool GetExtraTextures(Texture2D resCase, string name)
        {
            if (name == "SIDE_PANELS_TITLE_BAR")
                WindowHeaderBlueTex = resCase;
            else if (name == "SIDE_PANELS_SHELF_BLUE01")
                WindowRightBlueTex = resCase;
            else if (name == "POP_UPS_SCROLLING_ELEMENT_BKG")//POP_UPS_BUTTON_PANEL
                ElementShadowTex = resCase;
            else if (name == "DROPDOWN_LIST_WHITE_BLUE")
                ElementShadowSelectedTex = resCase;
            else if (name == "Dropdown_BG")
                DropdownTex = resCase;
            else if (name == "Dropdown_Active_BG")
                DropdownActiveTex = resCase;
            else if (name == "Profile_Dropdown_BG")
                Dropdown2Tex = resCase;
            else if (name == "Profile_Dropdown_Highlight_BG")
                Dropdown2ActiveTex = resCase;
            else if (name == "List_BG")
                ListBG_DBlueTex = resCase;
            else if (name == "DropdownArrow")
                DropdownArrowTex = resCase;
            else if (name == "DROPDOWN_ARROW")
                DropdownArrowFillTex = resCase;
            else 
                return false;
            return true;
        }
        private static void MakeExtras()
        {
            GUIStyle ExtrasBase = new GUIStyle(TextfieldBordered);
            ExtrasBase.clipping = TextClipping.Clip;
            ExtrasBase.alignment = TextAnchor.MiddleLeft;
            ExtrasBase.overflow = new RectOffset(0, 0, 0, 100);
            ExtrasBase.padding = new RectOffset(9, 9, 4, 4);
            ExtrasBase.border = new RectOffset(0, 0, 0, 100);
            ExtrasBase.margin = new RectOffset(0, 0, 0, 4);
            ExtrasBase.stretchWidth = true;
            ExtrasBase.fontSize = TitleFontSize;
            ExtrasBase.font = ExoFontSemiBoldItalic;


            // Setup WindowHeader
            ExtrasBase.clipping = TextClipping.Overflow;
            WindowHeaderBlue = NewGUIElement(ExtrasBase, WindowHeaderBlueTex, ColorDefaultGrey);
            ExtrasBase.overflow = new RectOffset(100, 4, 4, 4);
            ExtrasBase.border = new RectOffset(100, 4, 4, 4);
            ExtrasBase.margin = new RectOffset(4, 0, 0, 0);
            ExtrasBase.stretchWidth = false;
            ExtrasBase.stretchHeight = true;
            WindowRightBlue = NewGUIElement(ExtrasBase, WindowRightBlueTex, ColorDefaultGrey);
            ExtrasBase.stretchHeight = false;
            ExtrasBase.margin = new RectOffset(0, 0, 0, 4);

            GUIStyle styleBatch = new GUIStyle(ExtrasBase);
            ElementShadow = styleBatch;
            styleBatch.padding = new RectOffset(21, 21, 42, 42);
            GUIStyleState styleOffBatch = new GUIStyleState()
            {
                background = ElementShadowTex,
                textColor = ColorDefaultGrey,
            };
            GUIStyleState styleOnBatch = new GUIStyleState()
            {
                background = ElementShadowSelectedTex,
                textColor = ColorDefaultGrey,
            };
            AssignStyleOnOffQuick(styleBatch, styleOffBatch, styleOnBatch);
        }

        private static void AssignStyleQuick(GUIStyle styleBatch, GUIStyleState state)
        {
            styleBatch.normal = state;
            styleBatch.hover = state;
            styleBatch.active = state;
            styleBatch.focused = state;
            styleBatch.onNormal = state;
            styleBatch.onHover = state;
            styleBatch.onActive = state;
            styleBatch.onFocused = state;
        }
        private static void AssignStyleOnOffQuick(GUIStyle styleBatch, GUIStyleState styleOffBatch,
            GUIStyleState styleOnBatch)
        {
            styleBatch.normal = styleOffBatch;
            styleBatch.hover = styleOffBatch;
            styleBatch.active = styleOnBatch;
            styleBatch.focused = styleOffBatch;
            styleBatch.onNormal = styleOffBatch;
            styleBatch.onHover = styleOffBatch;
            styleBatch.onActive = styleOnBatch;
            styleBatch.onFocused = styleOffBatch;
        }


        /// <summary>
        /// Flood color-changes the given <see cref="Texture2D"/> to a specific flat color based on alpha.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="output"></param>
        /// <param name="color"></param>
        public static void ComesInColor(Texture2D target, ref Texture2D output, Color color)
        {
            var temp = new RenderTexture(target.width, target.height, 0, RenderTextureFormat.ARGBInt,
                RenderTextureReadWrite.Linear);
            output = new Texture2D(target.width, target.height, TextureFormat.RGBA32, target.mipmapCount > 1);
            var act = RenderTexture.active;
            Graphics.Blit(target, temp);

            output.ReadPixels(new Rect(0, 0, temp.width, temp.height), 0, 0);
            output.Apply();
            for (int x = 0; x < output.width; x++)
            {
                for (int y = 0; y < output.height; y++)
                {
                    output.SetPixel(x, y, new Color(color.r, color.g,
                        color.b, output.GetPixel(x, y).a));
                }
            }
            temp.Release();
            output.Apply();
            RenderTexture.active = act;
        }
        /// <summary>
        /// Flood color-changes the given <see cref="Texture2D"/> to <see cref="ColorDefaultGrey"/> based on alpha.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="output"></param>
        public static void ComesInBlack(Texture2D target, ref Texture2D output)
        {
            ComesInColor(target, ref output, ColorDefaultGrey);
        }
        /// <summary>
        /// Flood color-changes the given <see cref="Texture2D"/> to <see cref="ColorDefaultGrey"/> 
        /// based on 25% of the original alpha.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="output"></param>
        public static void ComesInBlackTrans(Texture2D target, ref Texture2D output)
        {
            var temp = new RenderTexture(target.width, target.height, 0, RenderTextureFormat.ARGBInt,  
                RenderTextureReadWrite.Linear);
            output = new Texture2D(target.width, target.height, TextureFormat.RGBA32, target.mipmapCount > 1);
            var act = RenderTexture.active;
            Graphics.Blit(target, temp);

            output.ReadPixels(new Rect(0, 0, temp.width, temp.height), 0, 0);
            output.Apply();
            for (int x = 0; x < output.width; x++)
            {
                for (int y = 0; y < output.height; y++)
                {
                    output.SetPixel(x, y, new Color(ColorDefaultGrey.r, ColorDefaultGrey.g,
                        ColorDefaultGrey.b, output.GetPixel(x, y).a * 0.25f));
                }
            }
            temp.Release();
            output.Apply();
            RenderTexture.active = act;
        }
        private static void FlipX(Texture2D target, ref Texture2D output)
        {
            var temp = new RenderTexture(target.width, target.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
            output = new Texture2D(target.width, target.height, target.format, target.mipmapCount > 1);
            var act = RenderTexture.active;
            Graphics.Blit(target, temp);

            output.ReadPixels(new Rect(0, 0, temp.width, temp.height), 0, 0);
            output.Apply();
            //Graphics.CopyTexture(toFlip, output);
            for (int x = 0; x < output.width; x++)
            {
                for (int y = 0; y < output.height; y++)
                {
                    output.SetPixel(x, y, output.GetPixel(output.width - x, y));
                }
            }
            temp.Release();
            output.Apply();
            RenderTexture.active = act;
        }

        private static void GatherTextures()
        {
            try
            {
                Texture2D[] res = Resources.FindObjectsOfTypeAll<Texture2D>();
                for (int step = 0; step < res.Length; step++)
                {
                    Texture2D resCase = res[step];
                    string nameC = resCase.name;
                    if (resCase && !nameC.NullOrEmpty())
                    {
                        if (nameC == "TOTALY_TRANSPARENT_01")
                            TransparentTex = resCase;
                        else if (nameC == "ACTION_MENU_BKG")
                            MenuTexRectRight = resCase;
                        else if (nameC == "ACTION_MENU_SHORT_BKG")
                            MenuTexRectLeft = resCase;
                        else if (nameC == "POP_UPS_MED_LRG_BKG")
                            MenuSharpTexRect = resCase;
                        else if (nameC == "MAIN_BUTTON_BLUE_DEFAULT")
                            ButtonSTexMain = resCase;
                        else if (nameC == "MAIN_BUTTON_BLUE_HOVER")
                            ButtonSTexHover = resCase;
                        else if (nameC == "MAIN_BUTTON_BLUE_PRESS")
                            ButtonSTexSelect = resCase;
                        else if (nameC == "MAIN_BUTTON_ORANGE_DEFAULT")
                            ButtonOTexMain = resCase;
                        else if (nameC == "MAIN_BUTTON_ORANGE_HOVER")
                            ButtonOTexHover = resCase;
                        else if (nameC == "MAIN_BUTTON_ORANGE_PRESS")
                            ButtonOTexSelect = resCase;
                        else if (nameC == "MAIN_BUTTON_GREY_DEFAULT")
                            ButtonSDTexMain = resCase;
                        else if (nameC == "MAIN_BUTTON_GREY_HOVER")
                            ButtonSDTexHover = resCase;
                        else if (nameC == "MAIN_BUTTON_GREY_PRESS")
                            ButtonSDTexSelect = resCase;
                        else if (nameC == "Button_BLUE")       // HUD_Button_BG
                            ButtonTexMain = resCase;
                        else if (nameC == "Button_BLUE_Highlight")// HUD_Button_Highlight
                            ButtonTexHover = resCase;
                        else if (nameC == "Button_BLUE_Pressed") // HUD_Button_Selected
                            ButtonTexSelect = resCase;
                        else if (nameC == "Button_GREEN")        // ????
                            ButtonTexAccept = resCase;
                        else if (nameC == "Button_GREEN_Highlight")// ????
                            ButtonTexAcceptHover = resCase;
                        else if (nameC == "Button_GREEN_Pressed")// ????
                            ButtonTexSelectGreen = resCase;
                        else if (nameC == "Button_RED")          // HUD_Button_Disabled_BG
                            ButtonTexDisabled = resCase;
                        else if (nameC == "Button_RED_Highlight")        // ????
                            ButtonTexDisabledHover = resCase;
                        else if (nameC == "Button_RED_Pressed")        // ????
                            ButtonTexSelectRed = resCase;
                        else if (nameC == "Button_ALL_Disabled") // HUD_Button_InActive
                            ButtonTexInactive = resCase;
                        else if (nameC == "InputFieldBackground")
                            TextfieldBTexMain = resCase;
                        else if (nameC == "STD_Text_Box_01")
                            TextfieldBBTexMain = resCase;
                        else if (nameC == "Panel_BLUE_BG")
                        {
                            TextfieldUTexMain = resCase;
                        }
                        else if (nameC == "Inventory_Shop_Desc_panel_Title")
                        {
                            TextfieldHTexMain = resCase;
                        }
                        else if (nameC == "TEXT_FIELD_STANDARD")
                            ComesInBlackTrans(resCase, ref TextfieldTexMain);
                        else if (nameC == "SEARCHBAR_TEXTFIELD")
                            ComesInBlackTrans(resCase, ref TextfieldSTexMain);
                        else if (nameC == "TEXT_FIELD_VERT_LEFT")
                            ComesInBlackTrans(resCase, ref TextfieldLTexMain);
                        else if (nameC == "POP_UPS_ROUND_CORNERS")
                        {
                            MenuTexRect = resCase;
                            //ComesInBlack(resCase, ref TextfieldHTexMain);
                        }
                        else if (nameC == "Options_Pressed_Toggle_Off")
                            SwitchTexOn = resCase;
                        else if (nameC == "Options_Pressed_Toggle_On")
                            SwitchTexOff = resCase;
                        else if (nameC == "All_None_Toggle_BG")
                        {
                            SwitchSTexMain = resCase;
                            FlipX(resCase, ref SwitchSTexMainInv);
                        }
                        else if (nameC == "All_None_Toggle_BG_02")
                            SwitchSTexMainAlt = resCase;
                        else if (nameC == "ICON_NAV_CLOSE")
                        {
                            SwitchCTexOff = resCase;
                            ComesInBlack(resCase, ref SwitchCTexOn);
                        }
                        else if (nameC == "SCROLL_HANDLE")
                        {
                            ScrollBarTex = resCase;
                            ComesInBlack(resCase, ref ScrollThumbTex);
                            ComesInColor(resCase, ref ScrollThumbTransparentTex, new Color(0,0,0,0.25f));
                        }
                        else
                            GetExtraTextures(resCase, nameC);
                    }
                }
                ValidateTextures();
            }
            catch (Exception e)
            {
                Debug_TTExt.Assert(true, "AltUI: failed to fetch textures");
                throw new Exception("AltUI: failed to fetch textures due to: ", e);
            }
        }
        private static void ValidateTextures()
        {
            if (MenuTexRectRight == null)
                throw new Exception("AltUI: MenuTexRectRight is null");
            if (MenuTexRectLeft == null)
                throw new Exception("AltUI: MenuTexRect is null");
            if (ButtonTexMain == null)
                throw new Exception("AltUI: ButtonTexMain is null");
            if (ButtonTexHover == null)
                throw new Exception("AltUI: ButtonTexHover is null");
            if (ButtonTexSelect == null)
                throw new Exception("AltUI: ButtonTexSelect is null");
            if (ButtonTexAccept == null)
                throw new Exception("AltUI: ButtonTexAccept is null");
            if (ButtonTexAcceptHover == null)
                throw new Exception("AltUI: ButtonTexAcceptHover is null");
            if (ButtonTexSelectGreen == null)
                throw new Exception("AltUI: ButtonTexSelectGreen is null");
            if (ButtonTexDisabled == null)
                throw new Exception("AltUI: ButtonTexDisabled is null");
            if (ButtonTexDisabledHover == null)
                throw new Exception("AltUI: ButtonTexDisabledHover is null");
            if (ButtonTexSelectRed == null)
                throw new Exception("AltUI: ButtonTexSelectRed is null");
            if (ButtonTexInactive == null)
                throw new Exception("AltUI: ButtonTexInactive is null");
            if (TextfieldBTexMain == null)
                throw new Exception("AltUI: TextfieldBTexMain is null");
            if (TextfieldBBTexMain == null)
                throw new Exception("AltUI: TextfieldBBTexMain is null");
            if (TextfieldTexMain == null)
                throw new Exception("AltUI: TextfieldTexMain is null");
            if (TextfieldSTexMain == null)
                throw new Exception("AltUI: TextfieldSTexMain is null");
            if (TextfieldLTexMain == null)
                throw new Exception("AltUI: TextfieldLTexMain is null");
            if (SwitchTexOn == null)
                throw new Exception("AltUI: SwitchTexOn is null");
            if (SwitchTexOff == null)
                throw new Exception("AltUI: SwitchTexOff is null");
            if (SwitchSTexMain == null)
                throw new Exception("AltUI: SwitchSTexMain is null");
            if (SwitchSTexMainAlt == null)
                throw new Exception("AltUI: SwitchSTexMainAlt is null");
        }
        private static void BuildUI()
        {
            if (MenuLeft == null)
            {
                Debug_TTExt.Log("AltUI: Init");

                GatherTextures();

                Debug_TTExt.Log("AltUI: Init stage 1");
                try
                {
                    ExoFont = Resources.FindObjectsOfTypeAll<Font>().FirstOrDefault(delegate (Font cand)
                    { return cand.name == "Exo-Regular"; });
                    ExoFontMediumItalic = Resources.FindObjectsOfTypeAll<Font>().FirstOrDefault(delegate (Font cand)
                    { return cand.name == "Exo-MediumItalic"; });
                    ExoFontSemiBold = Resources.FindObjectsOfTypeAll<Font>().FirstOrDefault(delegate (Font cand)
                    { return cand.name == "Exo-SemiBold"; });
                    ExoFontSemiBoldItalic = Resources.FindObjectsOfTypeAll<Font>().FirstOrDefault(delegate (Font cand)
                    { return cand.name == "Exo-SemiBoldItalic"; });
                    ExoFontBold = Resources.FindObjectsOfTypeAll<Font>().FirstOrDefault(delegate (Font cand)
                    { return cand.name == "Exo-Bold"; });
                    ExoFontBoldItalic = Resources.FindObjectsOfTypeAll<Font>().FirstOrDefault(delegate (Font cand)
                    { return cand.name == "Exo-BoldItalic"; });
                    ExoFontExtraBold = Resources.FindObjectsOfTypeAll<Font>().FirstOrDefault(delegate (Font cand)
                    { return cand.name == "Exo-ExtraBold"; });
                }
                catch { }
                Debug_TTExt.Log("AltUI: Init stage 2");

                MakeLabels();
                MakeButtons();
                MakeTextBoxes();
                MakeBoxes();
                MakeSwitches();
                MakeScrollers();
                MakeExtras();
                MakeMenus();

                Debug_TTExt.Log("AltUI: Init stage 3");

                MenuGUI = (GUISkin)ScriptableObject.CreateInstance("GUISkin");
                GUISkin mSkin = MenuGUI;

                mSkin.font = LabelBlack.font;
                mSkin.window = MenuCenter;

                mSkin.label = LabelBlack;

                mSkin.button = ButtonBlue;

                mSkin.box = TextfieldBlue;
                /*
                mSkin.box = new GUIStyle(GUI.skin.box);
                mSkin.box.font = idealFont;
                mSkin.box.normal.textColor = ColorDefaultGrey;
                */

                mSkin.textField = TextfieldBlack;

                mSkin.textArea = TextfieldBlackHuge;

                mSkin.toggle = SwitchDefault;

                mSkin.horizontalScrollbar = ScrollHorizontal;
                mSkin.horizontalScrollbarThumb = ScrollThumb;
                mSkin.horizontalSlider = ScrollHorizontal;
                mSkin.horizontalSliderThumb = ScrollThumb;
                mSkin.verticalScrollbar = ScrollVertical;
                mSkin.verticalScrollbarThumb = ScrollThumb;
                mSkin.verticalSlider = ScrollVertical;
                mSkin.verticalSliderThumb = ScrollThumb;

                Debug_TTExt.Log("AltUI: Init stage 4");

                MenuSharpGUI = (GUISkin)ScriptableObject.CreateInstance("GUISkin");
                mSkin = MenuSharpGUI;
                mSkin.font = LabelBlack.font;
                mSkin.window = MenuSharp;

                mSkin.label = LabelBlack;

                mSkin.button = ButtonBlue;

                mSkin.box = BoxBlackTextBlueTitle;

                mSkin.textField = TextfieldBlack;

                mSkin.textArea = TextfieldBlackHuge;

                mSkin.toggle = SwitchDefault;

                mSkin.horizontalScrollbar = ScrollHorizontal;
                mSkin.horizontalScrollbarThumb = ScrollThumb;
                mSkin.horizontalSlider = ScrollHorizontal;
                mSkin.horizontalSliderThumb = ScrollThumb;
                mSkin.verticalScrollbar = ScrollVertical;
                mSkin.verticalScrollbarThumb = ScrollThumb;
                mSkin.verticalSlider = ScrollVertical;
                mSkin.verticalSliderThumb = ScrollThumb;

                Debug_TTExt.Log("AltUI: Init fully");
            }
        }





        private static GUISkin cache;
        private static Color cacheColor;
        private static Color cacheColorContent;
        private static Color cacheColorBackground;
        private static Color GUIColor = new Color(1, 1, 1, UIAlpha);
        private static Color GUIColorSolid = new Color(1, 1, 1, 1);
        private static bool UIRunning = false;
        private static bool UIErrored = false;
        /// <summary>
        /// Change the UI to a softer hybrid style of mostly current and some old.
        /// <para><b>Make sure to finish it with </b><c>EndUI()</c></para>
        /// <para>Use a try-finally block to insure you call <c>EndUI()</c> even if an exception happens!</para>
        /// </summary>
        /// Use a try-finally block to insure you call <c>EndUI()</c> even if an exception happens!
        /// <exception cref="InvalidOperationException">The UI crashed and now everything is screwed up</exception>
        public static void StartUI()
        {
            if (UIRunning)
            {
                if (!UIErrored)
                {
                    UIErrored = true;
                    throw new InvalidOperationException("AltUI.StartUI cannot be nest-called");
                }
            }
            UIRunning = true;
            BuildUI();
            cache = GUI.skin;
            cacheColor = GUI.color;
            cacheColorContent = GUI.contentColor;
            cacheColorBackground = GUI.backgroundColor;
            GUI.skin = MenuGUI;
            GUI.color = GUIColor;
        }
        /// <summary>
        /// Change the UI to a softer hybrid style of mostly current and some old.
        /// <para><b>Make sure to finish it with </b><c>EndUI()</c></para>
        /// <para>Use a try-finally block to insure you call <c>EndUI()</c> even if an exception happens!</para>
        /// </summary>
        /// <param name="UIAlpha">Transparency of the window itself</param>
        /// <exception cref="InvalidOperationException">The UI crashed and now everything is screwed up</exception>
        public static void StartUI(float UIAlpha)
        {
            if (UIRunning)
            {
                if (!UIErrored)
                {
                    UIErrored = true;
                    throw new InvalidOperationException("AltUI.StartUI cannot be nest-called");
                }
            }
            UIRunning = true;
            BuildUI();
            cache = GUI.skin;
            cacheColor = GUI.color;
            cacheColorContent = GUI.contentColor;
            cacheColorBackground = GUI.backgroundColor;
            GUI.skin = MenuGUI;
            GUI.color = new Color(1, 1, 1, UIAlpha);
        }
        /// <summary>
        /// Change the UI to a softer hybrid style of mostly current and some old.
        /// <para><b>Make sure to finish it with </b><c>EndUI()</c></para>
        /// <para>Use a try-finally block to insure you call <c>EndUI()</c> even if an exception happens!</para>
        /// </summary>
        /// <param name="UIAlpha">Transparency of the window itself</param>
        /// <param name="UIContentAlpha">Transparency of content INSIDE the window</param>
        /// <exception cref="InvalidOperationException">The UI crashed and now everything is screwed up</exception>
        public static void StartUI(float UIAlpha, float UIContentAlpha)
        {
            if (UIRunning)
            {
                if (!UIErrored)
                {
                    UIErrored = true;
                    throw new InvalidOperationException("AltUI.StartUI cannot be nest-called");
                }
            }
            else
            {
                UIRunning = true;
                BuildUI();
                cache = GUI.skin;
                cacheColor = GUI.color;
                cacheColorContent = GUI.contentColor;
                cacheColorBackground = GUI.backgroundColor;
            }
            GUI.skin = MenuGUI;
            GUI.color = new Color(1, 1, 1, UIAlpha);
            GUI.contentColor = new Color(1, 1, 1, UIContentAlpha);
            GUI.backgroundColor = new Color(1, 1, 1, UIContentAlpha);
        }
        /// <summary>
        /// Change the UI to a non-transparancy, softer hybrid style of mostly current and some old.
        /// <para><b>Make sure to finish it with </b><c>EndUI()</c></para>
        /// <para>Use a try-finally block to insure you call <c>EndUI()</c> even if an exception happens!</para>
        /// </summary>
        /// <exception cref="InvalidOperationException">The UI crashed and now everything is screwed up</exception>
        public static void StartUIOpaque()
        {
            if (UIRunning)
            {
                if (!UIErrored)
                {
                    UIErrored = true;
                    throw new InvalidOperationException("AltUI.StartUIOpaque cannot be nest-called");
                }
            }
            else
            {
                UIRunning = true;
                BuildUI();
                cache = GUI.skin;
                cacheColor = GUI.color;
                cacheColorContent = GUI.contentColor;
            }
            GUI.skin = MenuGUI;
            GUI.color = GUIColorSolid;
        }

        /// <summary>
        /// Change the UI to a hybrid style of mostly current and some old.
        /// <para><b>Make sure to finish it with </b><c>EndUI()</c></para>
        /// <para>Use a try-finally block to insure you call <c>EndUI()</c> even if an exception happens!</para>
        /// </summary>
        /// <exception cref="InvalidOperationException">The UI crashed and now everything is screwed up</exception>
        public static void StartUISharp()
        {
            if (UIRunning)
            {
                if (!UIErrored)
                {
                    UIErrored = true;
                    throw new InvalidOperationException("AltUI.StartUISharp cannot be nest-called");
                }
            }
            else
            {
                UIRunning = true;
                BuildUI();
                cache = GUI.skin;
                cacheColor = GUI.color;
                cacheColorContent = GUI.contentColor;
                cacheColorBackground = GUI.backgroundColor;
            }
            GUI.skin = MenuSharpGUI;
            GUI.color = GUIColor;
        }
        /// <summary>
        /// Change the UI to a hybrid style of mostly current and some old.
        /// <para><b>Make sure to finish it with </b><c>EndUI()</c></para>
        /// <para>Use a try-finally block to insure you call <c>EndUI()</c> even if an exception happens!</para>
        /// </summary>
        /// <param name="UIAlpha">Transparency of the window itself</param>
        /// <exception cref="InvalidOperationException">The UI crashed and now everything is screwed up</exception>
        public static void StartUISharp(float UIAlpha)
        {
            if (UIRunning)
            {
                if (!UIErrored)
                {
                    UIErrored = true;
                    throw new InvalidOperationException("AltUI.StartUISharp cannot be nest-called");
                }
            }
            else
            {
                UIRunning = true;
                BuildUI();
                cache = GUI.skin;
                cacheColor = GUI.color;
                cacheColorContent = GUI.contentColor;
                cacheColorBackground = GUI.backgroundColor;
            }
            GUI.skin = MenuSharpGUI;
            GUI.color = new Color(1, 1, 1, UIAlpha);
        }
        /// <summary>
        /// Change the UI to a hybrid style of mostly current and some old.
        /// <para><b>Make sure to finish it with </b><c>EndUI()</c>.</para>
        /// <para>Use a try-finally block to insure you call <c>EndUI()</c> even if an exception happens!</para>
        /// </summary>
        /// <param name="UIAlpha">Transparency of the window itself</param>
        /// <param name="UIContentAlpha">Transparency of content INSIDE the window</param>
        /// <exception cref="InvalidOperationException">The UI crashed and now everything is screwed up</exception>
        public static void StartUISharp(float UIAlpha, float UIContentAlpha)
        {
            if (UIRunning)
            {
                if (!UIErrored)
                {
                    UIErrored = true;
                    throw new InvalidOperationException("AltUI.StartUISharp cannot be nest-called");
                }
            }
            else
            {
                UIRunning = true;
                BuildUI();
                cache = GUI.skin;
                cacheColor = GUI.color;
                cacheColorContent = GUI.contentColor;
                cacheColorBackground = GUI.backgroundColor;
            }
            GUI.skin = MenuSharpGUI;
            GUI.color = new Color(1, 1, 1, UIAlpha);
            GUI.contentColor = new Color(1, 1, 1, UIContentAlpha);
            GUI.backgroundColor = new Color(1, 1, 1, UIContentAlpha);
        }

        /// <summary>
        /// Returns the custom UI back to a standard format.
        /// </summary>
        /// <exception cref="InvalidOperationException">The UI crashed and now everything is screwed up</exception>
        public static void EndUI()
        {
            if (!UIRunning)
            {
                if (!UIErrored)
                {
                    UIErrored = true;
                    throw new InvalidOperationException("AltUI.EndUI cannot be nest-called");
                }
            }
            UIRunning = false;
            GUI.backgroundColor = cacheColorBackground;
            GUI.contentColor = cacheColorContent;
            GUI.color = cacheColor;
            GUI.skin = cache;
        }



        /// <summary>
        /// For the Popups that appear like the BB sold thing
        /// </summary>
        static readonly FieldInfo
            textInput = typeof(FloatingTextPanel).GetField("m_AmountText", BindingFlags.NonPublic | BindingFlags.Instance),
            listOverlays = typeof(ManOverlay).GetField("m_ActiveOverlays", BindingFlags.NonPublic | BindingFlags.Instance),
            rects = typeof(FloatingTextPanel).GetField("m_Rect", BindingFlags.NonPublic | BindingFlags.Instance),
            sScale = typeof(FloatingTextPanel).GetField("m_InitialScale", BindingFlags.NonPublic | BindingFlags.Instance),
            scale = typeof(FloatingTextPanel).GetField("m_scaler", BindingFlags.NonPublic | BindingFlags.Instance),
            canvas = typeof(FloatingTextPanel).GetField("m_CanvasGroup", BindingFlags.NonPublic | BindingFlags.Instance),
            CaseThis = typeof(ManOverlay).GetField("m_ConsumptionAddMoneyOverlayData", BindingFlags.NonPublic | BindingFlags.Instance);

        /// <summary>
        /// Create a new instance of <see cref="FloatingTextOverlay"/> to show in the world like the BB earn effect.
        /// <para><b>This is an instance of which to call <see cref="PopupCustomInfo(string, WorldPosition, FloatingTextOverlayData)"/></b>
        /// with, not the actual popup effect itself!</para>
        /// </summary>
        /// <param name="name"></param>
        /// <param name="colorToSet"></param>
        /// <param name="CallToShow">Cache this somewhere to call <see cref="PopupCustomInfo(string, WorldPosition, FloatingTextOverlayData)"/>
        ///  with later</param>
        /// <returns></returns>
        public static GameObject CreateCustomPopupInfo(string name, Color colorToSet, out FloatingTextOverlayData CallToShow)
        {
            GameObject TextStor = new GameObject(name, typeof(RectTransform));
            RectTransform rTrans = TextStor.GetComponent<RectTransform>();
            Text texter = rTrans.gameObject.AddComponent<Text>();
            FloatingTextOverlayData refer = (FloatingTextOverlayData)CaseThis.GetValue(ManOverlay.inst);
            Text textRefer = (Text)textInput.GetValue(refer.m_PanelPrefab);

            texter.horizontalOverflow = HorizontalWrapMode.Overflow;
            texter.fontStyle = textRefer.fontStyle;
            texter.material = textRefer.material;
            texter.alignment = textRefer.alignment;
            texter.font = textRefer.font;
            texter.color = colorToSet;
            texter.fontSize = (int)((float)texter.fontSize * 2f);
            texter.SetAllDirty();

            FloatingTextPanel panel = TextStor.AddComponent<FloatingTextPanel>();
            CanvasGroup newCanvasG;
            try
            {
                CanvasGroup cG = (CanvasGroup)canvas.GetValue(refer.m_PanelPrefab);
                newCanvasG = rTrans.gameObject.AddComponent<CanvasGroup>();
                newCanvasG.alpha = 0.95f;
                newCanvasG.blocksRaycasts = false;
                newCanvasG.hideFlags = 0;
                newCanvasG.ignoreParentGroups = true;
                newCanvasG.interactable = false;
            }
            catch
            {
                Debug.Assert(true, "AltUI: FAILED to create modded PopupInfo extract!");
                CallToShow = null;
                return null;
            }

            canvas.SetValue(panel, newCanvasG);
            rects.SetValue(panel, rTrans);
            sScale.SetValue(panel, Vector3.one * 2.5f);
            scale.SetValue(panel, 2.5f);

            textInput.SetValue(panel, texter);

            CallToShow = TextStor.AddComponent<FloatingTextOverlayData>();
            CallToShow.m_HiddenInModes = new List<ManGameMode.GameType>
                {
                    ManGameMode.GameType.Attract,
                    ManGameMode.GameType.Gauntlet,
                    ManGameMode.GameType.SumoShowdown,
                };
            CallToShow.m_StayTime = refer.m_StayTime;
            CallToShow.m_FadeOutTime = refer.m_FadeOutTime;
            CallToShow.m_MaxCameraResizeDist = refer.m_MaxCameraResizeDist;
            CallToShow.m_HiddenInModes = refer.m_HiddenInModes;
            CallToShow.m_MinCameraResizeDist = refer.m_MinCameraResizeDist;
            CallToShow.m_CamResizeCurve = refer.m_CamResizeCurve;
            CallToShow.m_AboveDist = refer.m_AboveDist;
            CallToShow.m_PanelPrefab = panel;

            return TextStor;
        }

        /// <summary>
        /// Display floating text in the world like the BB earn effect.
        /// <para>To use, call <see cref="CreateCustomPopupInfo(string, Color, out FloatingTextOverlayData)"/> first to make a 
        /// <see cref="FloatingTextOverlayData"/> to use with this</para>
        /// </summary>
        /// <param name="text"></param>
        /// <param name="pos"></param>
        /// <param name="FTOD">Get this and cache it from <see cref="CreateCustomPopupInfo(string, Color, out FloatingTextOverlayData)"/></param>
        public static void PopupCustomInfo(string text, WorldPosition pos, FloatingTextOverlayData FTOD)
        {
            FloatingTextOverlay fOverlay = new FloatingTextOverlay(FTOD);

            fOverlay.Set(text, pos);

            FloatingTextOverlayData textCase = (FloatingTextOverlayData)CaseThis.GetValue(ManOverlay.inst);
            if (textCase.VisibleInCurrentMode && fOverlay != null)
            {
                List<Overlay> over = (List<Overlay>)listOverlays.GetValue(ManOverlay.inst);
                over.Add(fOverlay);
                listOverlays.SetValue(ManOverlay.inst, over);
            }
        }

        /// <summary> The current global mod UI alpha for UI managed by <see cref="AltUI"/>, <b>automatically changes with mouse hovering</b> </summary>
        public static float UIAlphaAuto { get; internal set; } = 1f;

        // SFX adders
        /// <summary>
        /// Create a UI window complete with SFX and vanilla-like styling.
        /// </summary>
        /// <param name="id">Unique window ID</param>
        /// <param name="screenRect">The rect of the window, stored as a field somewhere</param>
        /// <param name="func">Window IMGUI contents call</param>
        /// <param name="title">Header title for the page</param>
        /// <param name="alpha">Transparency multiplier.  Multiplied by <see cref="UIAlphaAuto"/></param>
        /// <param name="closeCallback">Optional Action for a close UI button</param>
        /// <param name="topBarExtraGUI">Extra IMGUI contents call for the top blue bar</param>
        /// <param name="options">Additional UI options for this</param>
        /// <returns>The rect of the window adjusted with window dragging</returns>
        public static Rect Window(int id, Rect screenRect, GUI.WindowFunction func, string title, 
            float alpha, Action closeCallback, Action topBarExtraGUI, params GUILayoutOption[] options)
        {
            if (ManModGUI.HideGUICompletelyWhenDragging && ManModGUI.UIFadeState)
                return screenRect;
            StartUISharp(alpha, alpha);
            try
            {
                alpha *= UIAlphaAuto;
                screenRect = GUILayout.Window(id, screenRect, x => {
                    GUILayout.BeginHorizontal(WindowHeaderBlue, GUILayout.Height(48));
                    GUILayout.Label(title, LabelBlackTitle);
                    GUILayout.FlexibleSpace();
                    topBarExtraGUI?.Invoke();
                    bool callClose = false;
                    if (closeCallback != null)
                        callClose = ToggleNoFormat(false, string.Empty, SwitchCloseInv, GUILayout.Width(48), GUILayout.Height(48));
                    GUILayout.EndHorizontal();
                    func(id);
                    tooltipOverMenu.EndDisplayGUIToolTip();
                    if (callClose)
                    {
                        ManSFX.inst.PlayUISFX(ManSFX.UISfxType.Close);
                        closeCallback.Invoke();
                    }
                }, string.Empty, options);
                if (UIHelpersExt.MouseIsOverSubMenu(screenRect))
                    ManModGUI.IsMouseOverAnyModGUI = 4;
            }
            finally
            {
                EndUI();
            }
            return screenRect;
        }
        /// <inheritdoc cref="Window(int, Rect, GUI.WindowFunction, string, float, Action, Action, GUILayoutOption[])"/>
        public static Rect Window(int id, Rect screenRect, GUI.WindowFunction func, string title,
            Action closeCallback, Action topBarExtraGUI, params GUILayoutOption[] options) =>
            Window(id, screenRect, func, title, 1f, closeCallback, topBarExtraGUI, options);
        /// <inheritdoc cref="Window(int, Rect, GUI.WindowFunction, string, float, Action, Action, GUILayoutOption[])"/>
        public static Rect Window(int id, Rect screenRect, GUI.WindowFunction func, string title,
            float alpha, Action closeCallback, params GUILayoutOption[] options) =>
            Window(id, screenRect, func, title, alpha, closeCallback, null, options);
        /// <inheritdoc cref="Window(int, Rect, GUI.WindowFunction, string, float, Action, Action, GUILayoutOption[])"/>
        public static Rect Window(int id, Rect screenRect, GUI.WindowFunction func, string title, 
            Action closeCallback, params GUILayoutOption[] options) =>
            Window(id, screenRect, func, title, 1f, closeCallback, null, options);
        /// <inheritdoc cref="Window(int, Rect, GUI.WindowFunction, string, float, Action, Action, GUILayoutOption[])"/>
        public static Rect Window(int id, Rect screenRect, GUI.WindowFunction func, string title,
            params GUILayoutOption[] options) =>
            Window(id, screenRect, func, title, 1f, null, null, options);

        /// <summary>
        /// Displays an 'X' close button for use with <see cref="Window(int, Rect, GUI.WindowFunction, string, float, Action, Action, GUILayoutOption[])"/>
        /// </summary>
        /// <param name="sfx">Type of UI SFX to use on activation</param>
        /// <param name="options">Additional UI options for this</param>
        /// <returns>True if it was pressed</returns>
        public static bool CloseButton(ManSFX.UISfxType sfx = ManSFX.UISfxType.Close, params GUILayoutOption[] options)
        {
            bool closed = GUILayout.Toggle(false, string.Empty, SwitchCloseInv, options);
            if (closed)
                ManSFX.inst.PlayUISFX(sfx);
            return closed;
        }

        /// <summary>
        /// A Vanilla-like button for use similar to <see cref="GUILayout.Button(GUIContent, GUILayoutOption[])"/> but with added sfx
        /// </summary>
        /// <param name="text">Button content</param>
        /// <param name="clickNoise">Type of UI SFX to use on activation</param>
        /// <param name="style">Override GUIStyle</param>
        /// <param name="options">Additional UI options for this</param>
        /// <returns>True if it was pressed</returns>
        public static bool Button(string text, ManSFX.UISfxType clickNoise, GUIStyle style, params GUILayoutOption[] options)
        {
            if (GUILayout.Button(text, style, options))
            {
                ManSFX.inst.PlayUISFX(clickNoise);
                return true;
            }
            return false;
        }
        /// <inheritdoc cref="Button(string, ManSFX.UISfxType, GUIStyle, GUILayoutOption[])"/>
        public static bool Button(string text, ManSFX.UISfxType clickNoise, params GUILayoutOption[] options)
        {
            if (GUILayout.Button(text, options))
            {
                ManSFX.inst.PlayUISFX(clickNoise);
                return true;
            }
            return false;
        }
        /// <inheritdoc cref="Button(string, ManSFX.UISfxType, GUIStyle, GUILayoutOption[])"/>
        public static bool Button(Texture2D text, ManSFX.UISfxType clickNoise, params GUILayoutOption[] options)
        {
            if (GUILayout.Button(text, options))
            {
                ManSFX.inst.PlayUISFX(clickNoise);
                return true;
            }
            return false;
        }
        /// <inheritdoc cref="Button(string, ManSFX.UISfxType, GUIStyle, GUILayoutOption[])"/>
        public static bool Button(Texture2D text, ManSFX.UISfxType clickNoise, GUIStyle style, params GUILayoutOption[] options)
        {
            if (GUILayout.Button(text, style, options))
            {
                ManSFX.inst.PlayUISFX(clickNoise);
                return true;
            }
            return false;
        }


        /// <inheritdoc cref="Button(string, ManSFX.UISfxType, GUIStyle, GUILayoutOption[])"/>
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sprite">Sprite to use on the button</param>
        /// <param name="clickNoise"></param>
        /// <param name="style"></param>
        /// <param name="alphaBlend">Set to true to change the sprite transparancy relative to <see cref="UIAlphaAuto"/></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static bool Button(Sprite sprite, ManSFX.UISfxType clickNoise, GUIStyle style,
            bool alphaBlend, params GUILayoutOption[] options)
        {
            bool pressed = GUILayout.Button(string.Empty, style, options);
            DrawSprite(GUILayoutUtility.GetLastRect(), sprite, alphaBlend);
            if (pressed)
                ManSFX.inst.PlayUISFX(clickNoise);
            return pressed;
        }
        /// <inheritdoc cref="Button(UnityEngine.Sprite, ManSFX.UISfxType, GUIStyle, bool, GUILayoutOption[])"/>
        public static bool Button(Sprite sprite, ManSFX.UISfxType clickNoise, bool alphaBlend, params GUILayoutOption[] options)
        {
            bool pressed = GUILayout.Button(string.Empty, options);
            DrawSprite(GUILayoutUtility.GetLastRect(), sprite, alphaBlend);
            if (pressed)
                ManSFX.inst.PlayUISFX(clickNoise);
            return pressed;
        }
        /// <inheritdoc cref="Button(UnityEngine.Sprite, ManSFX.UISfxType, GUIStyle, bool, GUILayoutOption[])"/>
        public static bool Button(Sprite sprite, ManSFX.UISfxType clickNoise, params GUILayoutOption[] options)
        {
            bool pressed = GUILayout.Button(string.Empty, options);
            DrawSprite(GUILayoutUtility.GetLastRect(), sprite, true);
            if (pressed)
                ManSFX.inst.PlayUISFX(clickNoise);
            return pressed;
        }
        /// <inheritdoc cref="Button(UnityEngine.Sprite, ManSFX.UISfxType, GUIStyle, bool, GUILayoutOption[])"/>
        public static bool Button(Sprite sprite, ManSFX.UISfxType clickNoise, GUIStyle style, params GUILayoutOption[] options)
        {
            bool pressed = GUILayout.Button(string.Empty, style, options);
            DrawSprite(GUILayoutUtility.GetLastRect(), sprite, true);
            if (pressed)
                ManSFX.inst.PlayUISFX(clickNoise);
            return pressed;
        }


        /// <inheritdoc cref=" ToggleLone(bool, GUIStyle, GUILayoutOption[])"/>
        public static bool ToggleLone(bool value, params GUILayoutOption[] options)
        {
            bool outP = GUILayout.Toggle(value, string.Empty, options);
            if (outP != value)
                ManSFX.inst.PlayUISFX(ManSFX.UISfxType.CheckBox);
            return outP;
        }
        /// <inheritdoc cref=" Toggle(bool, string, GUIStyle, GUILayoutOption[])"/>
        /// <summary>
        /// <para>This is the lone version without text to the side</para>
        /// </summary>
        /// <param name="value"></param>
        /// <param name="style"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static bool ToggleLone(bool value, GUIStyle style, params GUILayoutOption[] options)
        {
            bool outP = GUILayout.Toggle(value, string.Empty, style, options);
            if (outP != value)
                ManSFX.inst.PlayUISFX(ManSFX.UISfxType.CheckBox);
            return outP;
        }
        /// <inheritdoc cref="Toggle(bool, string, GUIStyle, GUILayoutOption[])"/>
        public static bool Toggle(bool value, string text, params GUILayoutOption[] options)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(text);
            GUILayout.FlexibleSpace();
            bool outP = GUILayout.Toggle(value, string.Empty, options);
            GUILayout.EndHorizontal();
            if (outP != value)
                ManSFX.inst.PlayUISFX(ManSFX.UISfxType.CheckBox);
            return outP;
        }

        /// <summary>
        /// A Vanilla-like toggle for use similar to <see cref="GUILayout.Toggle(bool, GUIContent, GUILayoutOption[])"/> but with added sfx
        /// </summary>
        /// <param name="value">The current state of the toggle, from a field somewhere</param>
        /// <param name="text">The text to display to the left of this, with flexable space in-between</param>
        /// <param name="style">Override GUIStyle</param>
        /// <param name="options">Additional UI options for this</param>
        /// <returns>The new state of the toggle after user interaction</returns>
        public static bool Toggle(bool value, string text, GUIStyle style, params GUILayoutOption[] options)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(text);
            GUILayout.FlexibleSpace();
            bool outP = GUILayout.Toggle(value, string.Empty, style, options);
            GUILayout.EndHorizontal();
            if (outP != value)
                ManSFX.inst.PlayUISFX(ManSFX.UISfxType.CheckBox);
            return outP;
        }
        /// <inheritdoc cref=" Toggle(bool, string, GUIStyle, GUILayoutOption[])"/>
        public static bool ToggleNoFormat(bool value, string text, params GUILayoutOption[] options)
        {
            GUILayout.Label(text);
            bool outP = GUILayout.Toggle(value, string.Empty, options);
            if (outP != value)
                ManSFX.inst.PlayUISFX(ManSFX.UISfxType.CheckBox);
            return outP;
        }
        /// <inheritdoc cref=" Toggle(bool, string, GUIStyle, GUILayoutOption[])"/>
        public static bool ToggleNoFormat(bool value, string text, GUIStyle style, params GUILayoutOption[] options)
        {
            GUILayout.Label(text);
            bool outP = GUILayout.Toggle(value, string.Empty, style, options);
            if (outP != value)
                ManSFX.inst.PlayUISFX(ManSFX.UISfxType.CheckBox);
            return outP;
        }

        /// <inheritdoc cref="SpriteButton(UnityEngine.Sprite, GUIStyle, bool, GUILayoutOption[])"/>
        /// <summary>
        /// <para>For putting a sprite over other content</para>
        /// </summary>
        /// <param name="sprite"></param>
        /// <param name="pos">Position on the UI</param>
        /// <param name="scale">Scale of the sprite</param>
        /// <param name="alphaBlend">Set to true to change the sprite transparancy relative to <see cref="UIAlphaAuto"/></param>
        public static void AttachIcon(Sprite sprite, Vector2 pos, Vector2 scale, bool alphaBlend = false)
        {
            pos.Clamp(Vector2.zero, Vector2.one);
            scale.Clamp(Vector2.zero, Vector2.one);
            Rect revised = GUILayoutUtility.GetLastRect();
            float anonWidth = revised.width * scale.x;
            float anonHeight = revised.height * scale.y;
            revised.position = revised.position + new Vector2(
                pos.x * (revised.width - anonWidth),
                pos.y * (revised.height - anonWidth));
            revised.width = anonWidth;
            revised.height = anonHeight;
            DrawSprite(revised, sprite, alphaBlend);
        }
        /// <summary>
        /// For putting that mod wrench sprite over some other UI elements
        /// </summary>
        public static void AttachModWrenchIcon() =>
            AttachIcon(UIHelpersExt.ModContentIcon, Vector2.zero, new Vector2(0.35f, 0.35f));


        /// <inheritdoc cref="Sprite(UnityEngine.Sprite, GUIStyle, bool, GUILayoutOption[])"/>
        public static void Sprite(Sprite sprite, params GUILayoutOption[] options)
        {
            GUILayout.Box(string.Empty, options);
            DrawSprite(GUILayoutUtility.GetLastRect(), sprite, true);
        }

        /// <inheritdoc cref="Sprite(UnityEngine.Sprite, GUIStyle, bool, GUILayoutOption[])"/>
        public static void Sprite(Sprite sprite, bool alphaBlend, params GUILayoutOption[] options)
        {
            GUILayout.Box(string.Empty, options);
            DrawSprite(GUILayoutUtility.GetLastRect(), sprite, alphaBlend);
        }
        /// <inheritdoc cref="Sprite(UnityEngine.Sprite, GUIStyle, bool, GUILayoutOption[])"/>
        public static void Sprite(Sprite sprite, GUIStyle style, params GUILayoutOption[] options)
        {
            GUILayout.Box(string.Empty, style, options);
            DrawSprite(GUILayoutUtility.GetLastRect(), sprite, true);
        }

        /// <summary>
        /// Attach a sprite to somewhere on the UI like <see cref="GUILayout.Label(GUIContent, GUILayoutOption[])"/>
        /// </summary>
        /// <param name="sprite">Sprite to use</param>
        /// <param name="style">Scale of the sprite</param>
        /// <param name="alphaBlend">Set to true to change the sprite transparancy relative to <see cref="UIAlphaAuto"/></param>
        /// <param name="options">Additional UI options for this</param>
        public static void Sprite(Sprite sprite, GUIStyle style, bool alphaBlend, params GUILayoutOption[] options)
        {
            GUILayout.Box(string.Empty, style, options);
            DrawSprite(GUILayoutUtility.GetLastRect(), sprite, alphaBlend);
        }

        /// <summary>
        /// Attach a sprite to somewhere on the UI like <see cref="GUI.DrawTexture(Rect, Texture)"/>
        /// </summary>
        /// <param name="pos">UI positioning of the sprite</param>
        /// <param name="sprite">Sprite to use</param>
        /// <param name="alphaBlend">Set to true to change the sprite transparancy relative to <see cref="UIAlphaAuto"/></param>
        public static void DrawSprite(Rect pos, Sprite sprite, bool alphaBlend = true)
        {
            GUI.DrawTextureWithTexCoords(pos, sprite.texture, new Rect(sprite.rect.position.x / sprite.texture.width, sprite.rect.position.y / 
                sprite.texture.height, sprite.rect.width / sprite.texture.width, sprite.rect.height / sprite.texture.height), alphaBlend);
        }
        /// <inheritdoc cref="SpriteButton(UnityEngine.Sprite, GUIStyle, bool, GUILayoutOption[])"/>
        public static bool SpriteButton(Sprite sprite, params GUILayoutOption[] options)
        {
            GUILayout.Box(string.Empty, options);
            return DrawSpriteButton(GUILayoutUtility.GetLastRect(), sprite, true);
        }
        /// <inheritdoc cref="SpriteButton(UnityEngine.Sprite, GUIStyle, bool, GUILayoutOption[])"/>
        public static bool SpriteButton(Sprite sprite, bool alphaBlend, params GUILayoutOption[] options)
        {
            GUILayout.Box(string.Empty, options);
            return DrawSpriteButton(GUILayoutUtility.GetLastRect(), sprite, alphaBlend);
        }
        /// <inheritdoc cref="SpriteButton(UnityEngine.Sprite, GUIStyle, bool, GUILayoutOption[])"/>
        public static bool SpriteButton(Sprite sprite, GUIStyle style, params GUILayoutOption[] options)
        {
            GUILayout.Box(string.Empty, style, options);
            return DrawSpriteButton(GUILayoutUtility.GetLastRect(), sprite, true);
        }
        /// <summary>
        /// Attach a clickable sprite button to somewhere on the UI like <see cref="GUILayout.Button(GUIContent, GUILayoutOption[])"/>
        /// </summary>
        /// <param name="sprite">Sprite to use</param>
        /// <param name="style">Scale of the sprite</param>
        /// <param name="alphaBlend">Set to true to change the sprite transparancy relative to <see cref="UIAlphaAuto"/></param>
        /// <param name="options">Additional UI options for this</param>
        /// <returns>True if it was pressed</returns>
        public static bool SpriteButton(Sprite sprite, GUIStyle style, bool alphaBlend, params GUILayoutOption[] options)
        {
            GUILayout.Box(string.Empty, style, options);
            return DrawSpriteButton(GUILayoutUtility.GetLastRect(), sprite, alphaBlend);
        }
        /// <summary>
        /// Attach a clickable sprite button to somewhere on the UI like <see cref="GUI.Button(Rect, GUIContent)"/>
        /// </summary>
        /// <param name="pos">UI positioning of the sprite</param>
        /// <param name="sprite">Sprite to use</param>
        /// <param name="alphaBlend">Set to true to change the sprite transparancy relative to <see cref="UIAlphaAuto"/></param>
        public static bool DrawSpriteButton(Rect pos, Sprite sprite, bool alphaBlend = true)
        {
            GUI.DrawTextureWithTexCoords(pos, sprite.texture, new Rect(sprite.rect.position.x, sprite.rect.position.y,
                sprite.rect.width / sprite.texture.width, sprite.rect.height / sprite.texture.height), alphaBlend);
            return GUI.Button(pos, string.Empty, TRANSPARENT);
        }
        /*
        public static void LastRectTooltip(string displayString, bool fixate = false)
        {
            if (tooltipFrame)
                return;
            if (tooltipWorld == null)
                tooltipWorld = new GameObject("ToolTip").AddComponent<GUIToolTipAuto>();
            tooltipOverMenu.GUITooltip(displayString, fixate);
            tooltipFrame = true;
        }*/
        /// <summary>
        /// Advise using <see cref="Tooltip"/> -> <see cref="GUIToolTip.GUITooltip(string, bool)"/> instead
        /// <para>Display a tooltip in the world space UI relative to the <b>last UI <see cref="Rect"/></b> made and used by IMGUI</para>
        /// </summary>
        /// <param name="Title">Title of the tooltip.  Not likely to be seen</param>
        /// <param name="displayString">The description of the tooltip</param>
        /// <param name="fixate"><b>[BROKEN]</b> Lock it below the content hovered instead of the cursor</param>
        public static void TooltipWorld(string Title, string displayString, bool fixate = false)
        {
            if (tooltipFrame)
                return;
            if (tooltipWorld == null)
                tooltipWorld = new GameObject("ToolTip").AddComponent<GUIToolTipAuto>();

            if (tooltipWorld.Title != Title)
                tooltipWorld.Title = Title;
            if (tooltipWorld.Text != displayString)
                tooltipWorld.Text = displayString;
            tooltipFrame = true;
        }
        /// <inheritdoc cref="TooltipWorld(string, string, bool)"/>
        public static void TooltipWorld(string displayString, bool fixate = false)
        {
            if (tooltipQueued)
                return;
            if (tooltipWorld == null)
                tooltipWorld = new GameObject("ToolTip").AddComponent<GUIToolTipAuto>();

            if (tooltipWorld.Title != string.Empty)
                tooltipWorld.Title = string.Empty;
            if (tooltipWorld.Text != displayString)
                tooltipWorld.Text = displayString;
            tooltipQueued = true;
        }

        /// <summary>
        /// The general use tooltip. Use this for additional context when hovering
        /// </summary>
        public static GUIToolTip Tooltip => tooltipOverMenu;
        private static GUIToolTip tooltipOverMenu = new GUIToolTip();
        private static GUIToolTipAuto tooltipWorld;
        private static bool tooltipQueued = false;
        private static bool tooltipFrame = false;
        private static bool tooltipFrameGUI = false;

        /// <summary>
        /// Automatic tooltip.
        /// <para>Would advise using <see cref="Tooltip"/> -> <see cref="GUIToolTip.GUITooltip(string, bool)"/> instead</para>
        /// </summary>
        public class GUIToolTipAuto : MonoBehaviour
        {
            internal string Title = "Unset";
            internal string Text = "Unset";
            internal Rect toolWindow = new Rect(0, 0, 100, 60);   // the "window"
            /// <summary> </summary>
            public void OnGUI()
            {
                if (tooltipFrame)
                {
                    StartUI(0.9f);
                    Vector3 Mous = Input.mousePosition;
                    Mous.y = Display.main.renderingHeight - Mous.y;
                    GUILayout.BeginHorizontal(GUILayout.MaxWidth(240));
                    Rect rescale = GUILayoutUtility.GetRect(new GUIContent(Text), LabelBlackNoStretch);
                    if (Event.current.type == EventType.Repaint)
                    {
                        toolWindow.width = rescale.width + 26;
                        toolWindow.height = rescale.height + 26;
                    }
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                    GUILayout.FlexibleSpace();
                    toolWindow.x = Mathf.Clamp(Mous.x + 16, 0, Display.main.renderingWidth - toolWindow.width);
                    toolWindow.y = Mathf.Clamp(Mous.y + 16, 0, Display.main.renderingHeight - toolWindow.height);
                    toolWindow = GUI.Window(5625662, toolWindow, GUIHandlerInfo, Title, MenuLeft);
                    GUI.BringWindowToFront(5625662);
                    EndUI();
                }
            }
            /// <summary> </summary>
            public void GUIHandlerInfo(int ID)
            {
                GUILayout.Label(Text, LabelBlackNoStretch);
            }
            /// <summary> </summary>
            public void Update()
            {
                if (GUIUtility.hotControl == 0 || tooltipFrameGUI)
                {
                    tooltipFrame = tooltipQueued;
                    tooltipQueued = false;
                    tooltipFrameGUI = false;
                }
                GUIToolTip.DoUpdate();
            }
        }

        /// <summary>
        /// The large expandable mod GUI Tooltip that can be displayed on IMGUI <see cref="Rect"/> hovering
        /// </summary>
        public class GUIToolTip
        {
            private string Text = "Unset";
            private Rect ScanWindow = new Rect(0, 0, 2, 2);   // the "window"
            private Rect maxToolWindow = new Rect(0, 0, 300, 160);   // the "window"
            private Rect toolWindow = new Rect(0, 0, 300, 160);   // the "window"
            private static bool fakeTooltipShown = false;
            private static bool doShowFakeTooltip = false;
            private bool Fixate = false;
            private int IgnoreClose = 0;
            internal static void DoUpdate()
            {
                doShowFakeTooltip = fakeTooltipShown;
            }
            /// <summary>
            /// Call this at the end of every IMGUI window call you want to use <see cref="GUITooltip(string, bool)"/> in
            /// </summary>
            public void EndDisplayGUIToolTip()
            {
                if (!ScanWindow.Contains(Event.current.mousePosition))
                {
                    if (IgnoreClose == 0)
                        fakeTooltipShown = false;
                    else
                        IgnoreClose--;
                }
                if (Event.current.type == EventType.Repaint)
                {
                    var InGUIRect = GUILayoutUtility.GetLastRect();
                    if (Fixate)
                    {
                        var posOff = ScanWindow.position + new Vector2(0, ScanWindow.size.y);
                        toolWindow = new Rect(posOff.x - InGUIRect.x, posOff.y - InGUIRect.y,
                            maxToolWindow.width, maxToolWindow.height);
                    }
                    else
                    {
                        var posOff = Event.current.mousePosition - new Vector2(16, 16);
                        toolWindow = new Rect(posOff.x - InGUIRect.x, posOff.y - InGUIRect.y,
                            maxToolWindow.width, maxToolWindow.height);
                    }
                }

                if (!doShowFakeTooltip)
                    return;
                try
                {
                    GUI.Label(toolWindow, Text, TextfieldWhiteHuge);
                }
                catch (ExitGUIException e)
                {
                    throw e;
                }
            }
            /// <summary>
            /// <para>Display a tooltip in the world space UI relative to the <b>last UI <see cref="Rect"/></b> made and used by IMGUI</para>
            /// </summary>
            /// <param name="displayString">The description of the tooltip</param>
            /// <param name="fixate"><b>[BROKEN]</b> Lock it below the content hovered instead of the cursor</param>
            public void GUITooltip(string displayString, bool fixate = false)
            {
                if (Event.current.type == EventType.Repaint)
                {
                    var lastWindow = GUILayoutUtility.GetLastRect();
                    if (lastWindow.Contains(Event.current.mousePosition))
                    {
                        TooltipWorld(displayString, fixate);
                        tooltipFrameGUI = true;
                    }
                }
                /*
                if (Event.current.type == EventType.Repaint)
                {
                    var lastWindow = GUILayoutUtility.GetLastRect();
                    if (lastWindow.Contains(Event.current.mousePosition))
                    {
                        ScanWindow = lastWindow;
                        if (Text != displayString)
                        {
                            Text = displayString;
                        }
                        IgnoreClose = 10;
                        fakeTooltipShown = true;
                        Fixate = fixate;
                    }
                }*/
            }

        }

        // --------------------------------------------------------------------

        private static bool SavedPopup = false;
        private static FloatingTextOverlayData debugOverEdit;
        private static GameObject debugTextStor;
        //private static CanvasGroup enemyCanGroup;
        /// <summary>
        /// Displays a BLUE popup in the world like the BB sell popup
        /// </summary>
        /// <param name="text">Text to display</param>
        /// <param name="pos">Position in the world</param>
        public static void PopupDebugInfo(string text, WorldPosition pos)
        {
            if (!SavedPopup)
            {
                debugTextStor = CreateCustomPopupInfo("DebugPopup", Color.blue, out debugOverEdit);
                SavedPopup = true;
            }
            PopupCustomInfo(text, pos, debugOverEdit);
        }
        /// <inheritdoc cref="PopupDebugInfo(string, WorldPosition)"/>
        /// <summary>
        /// </summary>
        /// <param name="text"></param>
        /// <param name="scenePos">Position in Scene space</param>
        public static void PopupDebugInfo(string text, Vector3 scenePos) =>
            PopupDebugInfo(text, WorldPosition.FromScenePosition(scenePos));

        private static bool SavedPopupW = false;
        private static FloatingTextOverlayData debugOverEditW;
        private static GameObject debugTextStorW;
        /// <summary>
        /// Displays a WHITE popup in the world like the BB sell popup
        /// </summary>
        /// <param name="text">Text to display</param>
        /// <param name="pos">Position in the world</param>
        public static void PopupDebugInfoWhite(string text, WorldPosition pos)
        {
            if (!SavedPopupW)
            {
                debugTextStorW = CreateCustomPopupInfo("DebugPopup", Color.white, out debugOverEditW);
                SavedPopupW = true;
            }
            PopupCustomInfo(text, pos, debugOverEditW);
        }
        /// <inheritdoc cref="PopupDebugInfoWhite(string, WorldPosition)"/>
        /// <summary>
        /// </summary>
        /// <param name="text"></param>
        /// <param name="scenePos">Position in Scene space</param>
        public static void PopupDebugInfoWhite(string text, Vector3 scenePos) =>
            PopupDebugInfoWhite(text, WorldPosition.FromScenePosition(scenePos));

        private static bool SavedPopupB = false;
        private static FloatingTextOverlayData debugOverEditB;
        private static GameObject debugTextStorB;
        /// <summary>
        /// Displays a BLACK popup in the world like the BB sell popup
        /// </summary>
        /// <param name="text">Text to display</param>
        /// <param name="pos">Position in the world</param>
        public static void PopupDebugInfoBlack(string text, WorldPosition pos)
        {
            if (!SavedPopupB)
            {
                debugTextStorB = CreateCustomPopupInfo("DebugPopup", Color.black, out debugOverEditB);
                SavedPopupB = true;
            }
            PopupCustomInfo(text, pos, debugOverEditB);
        }
        /// <inheritdoc cref="PopupDebugInfoBlack(string, WorldPosition)"/>
        /// <summary>
        /// </summary>
        /// <param name="text"></param>
        /// <param name="scenePos">Position in Scene space</param>
        public static void PopupDebugInfoBlack(string text, Vector3 scenePos) =>
            PopupDebugInfoBlack(text, WorldPosition.FromScenePosition(scenePos));

    }
}
