using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace TerraTechETCUtil
{
    public static class AltUI
    {
        // INIT
        public static GUISkin MenuGUI;
        public static GUISkin MenuSharpGUI;
        public const int TitleFontSize = 32;

        public static float UIAlpha = 0.725f;
        public static string UIAlphaText = "<color=#454545ff>";
        public const string UIBlueText = "<color=#6bafe4ff>";
        public const string UIHighlightText = UIObjectiveMarkerText;//"<color=#16aadeff>";
        public const string UIObjectiveMarkerText = "<color=#ffff00ff>";
        public const string UIObjectiveText = "<color=#2596beff>";
        public const string UIEnemyText = "<color=#f23d3dff>";
        public const string UILackeyText = "<color=#308db5>";
        public const string UIWhisperText = "<color=#7d7d7d>";
        public const string UIHintText = "<color=#a3a3a3>";
        public const string UIThinkText = "<color=#919191>";
        public const string UIBuyText = "<color=#7dd7ffff>";

        public const string UITrialText = "<color=#b5b5b5>";


        public const string UIEndColor = "</color>";

        public static string UIHintPopupTitle(string title)
        {
            return "<color=#e4ac41ff><size=32><b><i>" + title + "</i></b></size></color>";
        }

        public static string UIString(string stringIn)
        {
            return UIAlphaText + stringIn + "</color>";
        }
        public static string BlueString(string stringIn)
        {
            return UIBlueText + stringIn + "</color>";
        }
        public static string HighlightString(string stringIn)
        {
            return UIHighlightText + stringIn + "</color>";
        }
        public static string ObjectiveString(string stringIn)
        {
            return UIObjectiveMarkerText + stringIn + "</color>";
        }
        public static string EnemyString(string stringIn)
        {
            return UIEnemyText + stringIn + "</color>";
        }
        public static string SideCharacterString(string stringIn)
        {
            return UILackeyText + stringIn + "</color>";
        }
        public static string WhisperString(string stringIn)
        {
            return UIWhisperText + stringIn + "</color>";
        }
        public static string HintString(string stringIn)
        {
            return UIHintText + stringIn + "</color>";
        }
        public static string ThinkString(string stringIn)
        {
            return UIThinkText + stringIn + "</color>";
        }
        public static string BuyString(string stringIn)
        {
            return UIBuyText + stringIn + "</color>";
        }



        public static Color ColorDefaultGrey = new Color(0.27f, 0.27f, 0.27f, 1);
        public static Color ColorDefaultWhite = new Color(0.975f, 0.975f, 0.975f, 1);
        public static Color ColorDefaultBlue = new Color(0.4196f, 0.6863f, 0.8941f, 1);
        public static Color ColorDefaultRed = new Color(0.949f, 0.239f, 0.239f, 1);
        public static Font ExoFont { get; private set; }
        public static Font ExoFontMediumItalic { get; private set; }
        public static Font ExoFontBold { get; private set; }
        public static Font ExoFontSemiBold { get; private set; }
        public static Font ExoFontSemiBoldItalic { get; private set; }
        public static Font ExoFontBoldItalic { get; private set; }
        public static Font ExoFontExtraBold { get; private set; }


        private static GUIStyle NewGUIElement(GUIStyle Base, ref GUIStyleState state, Texture2D background, Color textColor)
        {
            // Setup WindowHeader
            GUIStyle styleBatch = new GUIStyle(Base);
            state = new GUIStyleState()
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
        public static GUIStyle TRANSPARENT;
        private static Texture2D TransparentTex;
        private static GUIStyleState MenuTransparentStyle;

        public static GUIStyle MenuCenter;
        public static GUIStyle MenuCenterWrapText;
        private static Texture2D MenuTexRect;
        private static GUIStyleState MenuCenterStyle;

        public static GUIStyle MenuLeft;
        private static Texture2D MenuTexRectLeft;
        private static GUIStyleState MenuLeftStyleLeft;

        public static GUIStyle MenuRight;
        private static Texture2D MenuTexRectRight;
        private static GUIStyleState MenuRightStyle;

        public static GUIStyle MenuSharp;
        private static Texture2D MenuSharpTexRect;
        private static GUIStyleState MenuSharpCenterStyle;

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
            MenuLeft.padding = new RectOffset(MenuTexRectLeft.width / 16, MenuTexRectLeft.width / 16, MenuTexRectLeft.height / 18, MenuTexRectLeft.height / 18);
            MenuLeft.border = new RectOffset(MenuTexRectLeft.width / 3, MenuTexRectLeft.width / 3, MenuTexRectLeft.height / 6, MenuTexRectLeft.height / 6);
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
        public static GUIStyle LabelBlack;
        private static GUIStyleState LabelBlackStyle;
        public static GUIStyle LabelBlackNoStretch;
        public static GUIStyle LabelBlackTitle;
        private static GUIStyleState LabelBlackTitleStyle;

        public static GUIStyle LabelBlue;
        private static GUIStyleState LabelBlueStyle;
        public static GUIStyle LabelBlueTitle;
        private static GUIStyleState LabelBlueStyleTitle;

        public static GUIStyle LabelWhite;
        private static GUIStyleState LabelWhiteStyle;
        public static GUIStyle LabelWhiteTitle;
        private static GUIStyleState LabelWhiteStyleTitle;

        public static GUIStyle LabelRed;
        private static GUIStyleState LabelRedStyle;
        public static GUIStyle LabelRedTitle;
        private static GUIStyleState LabelRedStyleTitle;
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
            LabelBlackStyle = styleStateBatch;
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
            LabelBlackTitleStyle = styleStateBatch;
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
            LabelBlueStyle = styleStateBatch;
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
            LabelBlueStyleTitle = styleStateBatch;
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
            LabelWhiteStyle = styleStateBatch;
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
            LabelWhiteStyleTitle = styleStateBatch;
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
            LabelRedStyle = styleStateBatch;
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
            LabelRedStyleTitle = styleStateBatch;
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
        public static GUIStyle BoxBlack;
        private static GUIStyleState BoxBlackStyle;
        public static GUIStyle BoxBlackTitle;
        private static GUIStyleState BoxBlackTitleStyle;

        public static GUIStyle BoxBlackTextBlue;
        private static GUIStyleState BoxBlackTextBlueStyle;
        public static GUIStyle BoxBlackTextBlueTitle;
        private static GUIStyleState BoxBlackTextBlueStyleTitle;

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
            BoxBlackStyle = styleStateBatch;
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
            BoxBlackTitleStyle = styleStateBatch;
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
            BoxBlackTextBlueStyle = styleStateBatch;
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
            BoxBlackTextBlueStyleTitle = styleStateBatch;
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
        public static GUIStyle ButtonBlue;
        private static Texture2D ButtonTexMain;
        private static Texture2D ButtonTexHover;
        private static GUIStyleState ButtonStyle;
        private static GUIStyleState ButtonStyleHover;

        public static GUIStyle ButtonGreen;
        private static Texture2D ButtonTexAccept;
        private static Texture2D ButtonTexAcceptHover;
        private static GUIStyleState ButtonStyleAccept;
        private static GUIStyleState ButtonStyleAcceptHover;

        public static GUIStyle ButtonRed;
        private static Texture2D ButtonTexDisabled;
        private static Texture2D ButtonTexDisabledHover;
        private static GUIStyleState ButtonStyleDisabled;
        private static GUIStyleState ButtonStyleDisabledHover;

        public static GUIStyle ButtonBlueLarge;
        private static Texture2D ButtonSTexMain;
        private static Texture2D ButtonSTexHover;
        private static GUIStyleState ButtonSStyle;
        private static GUIStyleState ButtonSStyleHover;

        public static GUIStyle ButtonOrangeLarge;
        private static Texture2D ButtonOTexMain;
        private static Texture2D ButtonOTexHover;
        private static GUIStyleState ButtonOStyle;
        private static GUIStyleState ButtonOStyleHover;

        public static GUIStyle ButtonGreyLarge;
        private static Texture2D ButtonSDTexMain;
        private static Texture2D ButtonSDTexHover;
        private static GUIStyleState ButtonSDStyle;
        private static GUIStyleState ButtonSDStyleHover;

        // ----------------------------------
        public static GUIStyle ButtonGrey;
        private static Texture2D ButtonTexInactive;
        private static GUIStyleState ButtonStyleInactive;

        public static GUIStyle ButtonBlueActive;
        private static GUIStyleState ButtonStyleActive;
        private static Texture2D ButtonTexSelect;

        public static GUIStyle ButtonGreenActive;
        private static GUIStyleState ButtonStyleGActive;
        private static Texture2D ButtonTexSelectGreen;

        public static GUIStyle ButtonRedActive;
        private static GUIStyleState ButtonStyleRActive;
        private static Texture2D ButtonTexSelectRed;

        public static GUIStyle ButtonBlueLargeActive;
        private static GUIStyleState ButtonSStyleActive;
        private static Texture2D ButtonSTexSelect;

        public static GUIStyle ButtonOrangeLargeActive;
        private static GUIStyleState ButtonOStyleActive;
        private static Texture2D ButtonOTexSelect;

        public static GUIStyle ButtonGreyLargeActive;
        private static GUIStyleState ButtonSDStyleActive;
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
            ButtonSStyle = new GUIStyleState() { background = ButtonSTexMain, textColor = ColorDefaultWhite, };
            ButtonSStyleHover = new GUIStyleState() { background = ButtonSTexHover, textColor = ColorDefaultWhite, };
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
            ButtonSStyleActive = new GUIStyleState() { background = ButtonSTexSelect, textColor = ColorDefaultWhite, };
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
            ButtonOStyle = new GUIStyleState() { background = ButtonOTexMain, textColor = ColorDefaultWhite, };
            ButtonOStyleHover = new GUIStyleState() { background = ButtonOTexHover, textColor = ColorDefaultWhite, };
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
            ButtonOStyleActive = new GUIStyleState() { background = ButtonOTexSelect, textColor = ColorDefaultWhite, };
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
            ButtonSDStyle = new GUIStyleState() { background = ButtonSDTexMain, textColor = ColorDefaultWhite, };
            ButtonSDStyleHover = new GUIStyleState() { background = ButtonSDTexHover, textColor = ColorDefaultWhite, };
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
            ButtonSDStyleActive = new GUIStyleState() { background = ButtonSDTexSelect, textColor = ColorDefaultWhite, };
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
            ButtonStyle = new GUIStyleState() { background = ButtonTexMain, textColor = ColorDefaultWhite, };
            ButtonStyleHover = new GUIStyleState() { background = ButtonTexHover, textColor = ColorDefaultWhite, };
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
            ButtonStyleActive = new GUIStyleState() { background = ButtonTexSelect, textColor = ColorDefaultWhite, };
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
            ButtonStyleAccept = new GUIStyleState() { background = ButtonTexAccept, textColor = ColorDefaultWhite, };
            ButtonStyleAcceptHover = new GUIStyleState() { background = ButtonTexAcceptHover, textColor = ColorDefaultWhite, };
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
            ButtonStyleGActive = new GUIStyleState() { background = ButtonTexSelectGreen, textColor = ColorDefaultWhite, };
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
            ButtonStyleDisabled = new GUIStyleState() { background = ButtonTexDisabled, textColor = ColorDefaultWhite, };
            ButtonStyleDisabledHover = new GUIStyleState() { background = ButtonTexDisabledHover, textColor = ColorDefaultWhite, };
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
            ButtonStyleRActive = new GUIStyleState() { background = ButtonTexSelectRed, textColor = ColorDefaultWhite, };
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
            ButtonStyleInactive = new GUIStyleState() { background = ButtonTexInactive, textColor = ColorDefaultWhite, };
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
        public static GUIStyle TextfieldBlue;
        private static Texture2D TextfieldUTexMain;
        private static GUIStyleState TextfieldUStyle;

        public static GUIStyle TextfieldBlack;
        private static Texture2D TextfieldTexMain;
        private static GUIStyleState TextfieldStyle;

        public static GUIStyle TextfieldBlackAdjusted;

        public static GUIStyle TextfieldBordered;
        private static Texture2D TextfieldBTexMain;
        private static GUIStyleState TextfieldBStyle;

        public static GUIStyle TextfieldBorderedBlue;
        private static Texture2D TextfieldBBTexMain;
        private static GUIStyleState TextfieldBBStyle;

        public static GUIStyle TextfieldBlackHuge;
        private static Texture2D TextfieldHTexMain;
        private static GUIStyleState TextfieldHStyle;
        public static GUIStyle TextfieldBlackBlueText;
        private static GUIStyleState TextfieldHBStyle;

        public static GUIStyle TextfieldWhiteHuge;
        public static GUIStyle TextfieldWhiteMenu;
        private static GUIStyleState TextfieldWhiteMenuStyle;

        // ----------------------------------
        public static GUIStyle TextfieldBlackSearch;
        private static Texture2D TextfieldSTexMain;
        private static GUIStyleState TextfieldSStyle;

        public static GUIStyle TextfieldBlackLeft;
        private static Texture2D TextfieldLTexMain;
        private static GUIStyleState TextfieldLStyle;

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
            TextfieldUStyle = styleStateBatch;
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
            TextfieldStyle = styleStateBatch;
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
            TextfieldLStyle = styleStateBatch;
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
            TextfieldSStyle = styleStateBatch;
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
            TextfieldHStyle = styleStateBatch;
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
            TextfieldHBStyle = styleStateBatch;
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
            TextfieldBStyle = styleStateBatch;
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
            TextfieldBBStyle = styleStateBatch;
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
            TextfieldWhiteMenuStyle = new GUIStyleState() { background = MenuTexRectLeft, textColor = ColorDefaultGrey, };
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
        public static GUIStyle SwitchDefault;
        private static Texture2D SwitchTexOff;
        private static Texture2D SwitchTexOn;
        private static GUIStyleState SwitchStyleOff;
        private static GUIStyleState SwitchStyleOn;

        public static GUIStyle SwitchSlider;
        private static Texture2D SwitchSTexMain;
        private static Texture2D SwitchSTexMainInv;
        private static Texture2D SwitchSTexMainAlt;
        private static GUIStyleState SwitchSStyleOff;
        private static GUIStyleState SwitchSStyleOn;

        public static GUIStyle SwitchClose;
        public static GUIStyle SwitchCloseInv;
        private static Texture2D SwitchCTexOff;
        private static Texture2D SwitchCTexOn;
        private static GUIStyleState SwitchCStyleOff;
        private static GUIStyleState SwitchCStyleOn;

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
            SwitchStyleOff = styleOffBatch;
            GUIStyleState styleOnBatch = new GUIStyleState()
            {
                background = SwitchTexOn,
                textColor = ColorDefaultWhite,
            };
            SwitchStyleOn = styleOnBatch;
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
            SwitchSStyleOff = styleOffBatch;
            styleOnBatch = new GUIStyleState()
            {
                background = SwitchSTexMainAlt,
                textColor = ColorDefaultWhite,
            };
            SwitchSStyleOn = styleOnBatch;
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
            SwitchCStyleOff = styleOffBatch;
            styleOnBatch = new GUIStyleState()
            {
                background = SwitchCTexOn,
                textColor = ColorDefaultWhite,
            };
            SwitchCStyleOn = styleOnBatch;
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
        public static GUIStyle ScrollThumb;
        private static Texture2D ScrollThumbTex;
        public static GUIStyle ScrollThumbTransparent;
        private static Texture2D ScrollThumbTransparentTex;


        private static Texture2D ScrollBarTex;
        public static GUIStyle ScrollVertical;
        public static GUIStyle ScrollHorizontal;
        public static GUIStyle ScrollVerticalTransparent;
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
            styleBatch.normal = styleStateBatch;
            styleBatch.hover = styleStateBatch;
            styleBatch.active = styleStateBatch;
            styleBatch.focused = styleStateBatch;
            styleBatch.onNormal = styleStateBatch;
            styleBatch.onHover = styleStateBatch;
            styleBatch.onActive = styleStateBatch;
            styleBatch.onFocused = styleStateBatch;

            // Setup Scroll Thumb Trans
            styleBatch = new GUIStyle(TextBase);
            ScrollThumbTransparent = styleBatch;
            styleStateBatch = new GUIStyleState()
            {
                background = ScrollThumbTransparentTex,
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
            styleBatch.normal = styleStateBatch;
            styleBatch.hover = styleStateBatch;
            styleBatch.active = styleStateBatch;
            styleBatch.focused = styleStateBatch;
            styleBatch.onNormal = styleStateBatch;
            styleBatch.onHover = styleStateBatch;
            styleBatch.onActive = styleStateBatch;
            styleBatch.onFocused = styleStateBatch;

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
            styleBatch.normal = styleStateBatch;
            styleBatch.hover = styleStateBatch;
            styleBatch.active = styleStateBatch;
            styleBatch.focused = styleStateBatch;
            styleBatch.onNormal = styleStateBatch;
            styleBatch.onHover = styleStateBatch;
            styleBatch.onActive = styleStateBatch;
            styleBatch.onFocused = styleStateBatch;


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
            styleBatch.normal = styleStateBatch;
            styleBatch.hover = styleStateBatch;
            styleBatch.active = styleStateBatch;
            styleBatch.focused = styleStateBatch;
            styleBatch.onNormal = styleStateBatch;
            styleBatch.onHover = styleStateBatch;
            styleBatch.onActive = styleStateBatch;
            styleBatch.onFocused = styleStateBatch;

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
            styleBatch.normal = styleStateBatch;
            styleBatch.hover = styleStateBatch;
            styleBatch.active = styleStateBatch;
            styleBatch.focused = styleStateBatch;
            styleBatch.onNormal = styleStateBatch;
            styleBatch.onHover = styleStateBatch;
            styleBatch.onActive = styleStateBatch;
            styleBatch.onFocused = styleStateBatch;
        }


        public static GUIStyle WindowHeaderBlue;
        private static Texture2D WindowHeaderBlueTex;
        private static GUIStyleState WindowHeaderBlueStyle;

        public static GUIStyle WindowRightBlue;
        private static Texture2D WindowRightBlueTex;
        private static GUIStyleState WindowRightBlueStyle;


        public static GUIStyle ElementShadow;
        private static Texture2D ElementShadowTex;
        private static GUIStyleState ElementShadowStyle;
        private static Texture2D ElementShadowSelectedTex;
        private static GUIStyleState ElementShadowSelectedStyle;

        private static bool GetExtraTextures(Texture2D resCase, string name)
        {
            if (name == "SIDE_PANELS_TITLE_BAR")
                WindowHeaderBlueTex = resCase;
            else if (name == "SIDE_PANELS_SHELF_BLUE01")
                WindowRightBlueTex = resCase;
            else if (name == "POP_UPS_SCROLLING_ELEMENT_BKG")
                ElementShadowTex = resCase;
            else if (name == "DROPDOWN_LIST_WHITE_BLUE")
                ElementShadowSelectedTex = resCase;
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
            WindowHeaderBlue = NewGUIElement(ExtrasBase, ref WindowHeaderBlueStyle, WindowHeaderBlueTex, ColorDefaultGrey);
            ExtrasBase.overflow = new RectOffset(100, 4, 4, 4);
            ExtrasBase.border = new RectOffset(100, 4, 4, 4);
            ExtrasBase.margin = new RectOffset(4, 0, 0, 0);
            ExtrasBase.stretchWidth = false;
            ExtrasBase.stretchHeight = true;
            WindowRightBlue = NewGUIElement(ExtrasBase, ref WindowRightBlueStyle, WindowRightBlueTex, ColorDefaultGrey);
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
            ElementShadowStyle = styleOffBatch;
            GUIStyleState styleOnBatch = new GUIStyleState()
            {
                background = ElementShadowSelectedTex,
                textColor = ColorDefaultGrey,
            };
            ElementShadowSelectedStyle = styleOffBatch;
            styleBatch.normal = styleOffBatch;
            styleBatch.hover = styleOffBatch;
            styleBatch.active = styleOnBatch;
            styleBatch.focused = styleOffBatch;
            styleBatch.onNormal = styleOffBatch;
            styleBatch.onHover = styleOffBatch;
            styleBatch.onActive = styleOnBatch;
            styleBatch.onFocused = styleOffBatch;
        }


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
        internal static void ComesInBlack(Texture2D target, ref Texture2D output)
        {
            ComesInColor(target, ref output, ColorDefaultGrey);
        }
        internal static void ComesInBlackTrans(Texture2D target, ref Texture2D output)
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


        // SFX adders
        public static Rect Window(int id, Rect screenRect, GUI.WindowFunction func, string title, Action closeCallback = null, params GUILayoutOption[] options)
        {
            StartUISharp();
            var rect = GUILayout.Window(id, screenRect, x => {
                GUILayout.BeginHorizontal(WindowHeaderBlue, GUILayout.Height(48));
                GUILayout.Label(title, LabelBlackTitle);
                GUILayout.FlexibleSpace();
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
                ManModGUI.IsMouseOverAnyModGUI = 2;
            EndUI();
            return rect;
        }
        public static Rect Window(int id, Rect screenRect, GUI.WindowFunction func, string title, float alpha, Action closeCallback = null, params GUILayoutOption[] options)
        {
            StartUISharp(alpha, alpha);
            var rect = GUILayout.Window(id, screenRect, x => {
                GUILayout.BeginHorizontal(WindowHeaderBlue, GUILayout.Height(48));
                GUILayout.Label(title, LabelBlackTitle);
                GUILayout.FlexibleSpace();
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
            EndUI();
            return rect;
        }
        public static bool CloseButton(ManSFX.UISfxType sfx = ManSFX.UISfxType.Close, params GUILayoutOption[] options)
        {
            bool closed = GUILayout.Toggle(false, string.Empty, SwitchCloseInv, options);
            if (closed)
                ManSFX.inst.PlayUISFX(sfx);
            return closed;
        }

        public static bool Button(string text, ManSFX.UISfxType clickNoise, params GUILayoutOption[] options)
        {
            if (GUILayout.Button(text, options))
            {
                ManSFX.inst.PlayUISFX(clickNoise);
                return true;
            }
            return false;
        }
        public static bool Button(string text, ManSFX.UISfxType clickNoise, GUIStyle style, params GUILayoutOption[] options)
        {
            if (GUILayout.Button(text, style, options))
            {
                ManSFX.inst.PlayUISFX(clickNoise);
                return true;
            }
            return false;
        }
        public static bool Button(Texture2D text, ManSFX.UISfxType clickNoise, params GUILayoutOption[] options)
        {
            if (GUILayout.Button(text, options))
            {
                ManSFX.inst.PlayUISFX(clickNoise);
                return true;
            }
            return false;
        }
        public static bool Button(Texture2D text, ManSFX.UISfxType clickNoise, GUIStyle style, params GUILayoutOption[] options)
        {
            if (GUILayout.Button(text, style, options))
            {
                ManSFX.inst.PlayUISFX(clickNoise);
                return true;
            }
            return false;
        }
        public static bool Button(Sprite sprite, ManSFX.UISfxType clickNoise, params GUILayoutOption[] options)
        {
            bool pressed = GUILayout.Button(string.Empty, options);
            DrawSprite(GUILayoutUtility.GetLastRect(), sprite, true);
            if (pressed)
                ManSFX.inst.PlayUISFX(clickNoise);
            return pressed;
        }
        public static bool Button(Sprite sprite, ManSFX.UISfxType clickNoise, GUIStyle style, params GUILayoutOption[] options)
        {
            bool pressed = GUILayout.Button(string.Empty, style, options);
            DrawSprite(GUILayoutUtility.GetLastRect(), sprite, true);
            if (pressed)
                ManSFX.inst.PlayUISFX(clickNoise);
            return pressed;
        }

        public static bool Button(Sprite sprite, ManSFX.UISfxType clickNoise, bool alphaBlend, params GUILayoutOption[] options)
        {
            bool pressed = GUILayout.Button(string.Empty, options);
            DrawSprite(GUILayoutUtility.GetLastRect(), sprite, alphaBlend);
            if (pressed)
                ManSFX.inst.PlayUISFX(clickNoise);
            return pressed;
        }
        public static bool Button(Sprite sprite, ManSFX.UISfxType clickNoise, GUIStyle style, bool alphaBlend, params GUILayoutOption[] options)
        {
            bool pressed = GUILayout.Button(string.Empty, style, options);
            DrawSprite(GUILayoutUtility.GetLastRect(), sprite, alphaBlend);
            if (pressed)
                ManSFX.inst.PlayUISFX(clickNoise);
            return pressed;
        }


        public static bool ToggleLone(bool value, params GUILayoutOption[] options)
        {
            bool outP = GUILayout.Toggle(value, string.Empty, options);
            if (outP != value)
                ManSFX.inst.PlayUISFX(ManSFX.UISfxType.CheckBox);
            return outP;
        }
        public static bool ToggleLone(bool value, GUIStyle style, params GUILayoutOption[] options)
        {
            bool outP = GUILayout.Toggle(value, string.Empty, style, options);
            if (outP != value)
                ManSFX.inst.PlayUISFX(ManSFX.UISfxType.CheckBox);
            return outP;
        }
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
        public static bool ToggleNoFormat(bool value, string text, params GUILayoutOption[] options)
        {
            GUILayout.Label(text);
            bool outP = GUILayout.Toggle(value, string.Empty, options);
            if (outP != value)
                ManSFX.inst.PlayUISFX(ManSFX.UISfxType.CheckBox);
            return outP;
        }
        public static bool ToggleNoFormat(bool value, string text, GUIStyle style, params GUILayoutOption[] options)
        {
            GUILayout.Label(text);
            bool outP = GUILayout.Toggle(value, string.Empty, style, options);
            if (outP != value)
                ManSFX.inst.PlayUISFX(ManSFX.UISfxType.CheckBox);
            return outP;
        }

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
        public static void AttachModWrenchIcon() =>
            AttachIcon(UIHelpersExt.ModContentIcon, Vector2.zero, new Vector2(0.35f, 0.35f));
        public static void Sprite(Sprite sprite, params GUILayoutOption[] options)
        {
            GUILayout.Box(string.Empty, options);
            DrawSprite(GUILayoutUtility.GetLastRect(), sprite, true);
        }
        public static void Sprite(Sprite sprite, bool alphaBlend, params GUILayoutOption[] options)
        {
            GUILayout.Box(string.Empty, options);
            DrawSprite(GUILayoutUtility.GetLastRect(), sprite, alphaBlend);
        }
        public static void Sprite(Sprite sprite, GUIStyle style, params GUILayoutOption[] options)
        {
            GUILayout.Box(string.Empty, style, options);
            DrawSprite(GUILayoutUtility.GetLastRect(), sprite, true);
        }
        public static void Sprite(Sprite sprite, GUIStyle style, bool alphaBlend, params GUILayoutOption[] options)
        {
            GUILayout.Box(string.Empty, style, options);
            DrawSprite(GUILayoutUtility.GetLastRect(), sprite, alphaBlend);
        }
        public static void DrawSprite(Rect pos, Sprite sprite, bool alphaBlend = true)
        {
            GUI.DrawTextureWithTexCoords(pos, sprite.texture, new Rect(sprite.rect.position.x / sprite.texture.width, sprite.rect.position.y / 
                sprite.texture.height, sprite.rect.width / sprite.texture.width, sprite.rect.height / sprite.texture.height), alphaBlend);
        }
        public static bool SpriteButton(Sprite sprite, params GUILayoutOption[] options)
        {
            GUILayout.Box(string.Empty, options);
            return DrawSpriteButton(GUILayoutUtility.GetLastRect(), sprite, true);
        }
        public static bool SpriteButton(Sprite sprite, bool alphaBlend, params GUILayoutOption[] options)
        {
            GUILayout.Box(string.Empty, options);
            return DrawSpriteButton(GUILayoutUtility.GetLastRect(), sprite, alphaBlend);
        }
        public static bool SpriteButton(Sprite sprite, GUIStyle style, params GUILayoutOption[] options)
        {
            GUILayout.Box(string.Empty, style, options);
            return DrawSpriteButton(GUILayoutUtility.GetLastRect(), sprite, true);
        }
        public static bool SpriteButton(Sprite sprite, GUIStyle style, bool alphaBlend, params GUILayoutOption[] options)
        {
            GUILayout.Box(string.Empty, style, options);
            return DrawSpriteButton(GUILayoutUtility.GetLastRect(), sprite, alphaBlend);
        }
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
        private static GUIToolTip tooltipOverMenu = new GUIToolTip();
        private static GUIToolTipAuto tooltipWorld;
        private static bool tooltipQueued = false;
        private static bool tooltipFrame = false;
        private static bool tooltipFrameGUI = false;

        public class GUIToolTipAuto : MonoBehaviour
        {
            internal string Title = "Unset";
            internal string Text = "Unset";
            internal Rect toolWindow = new Rect(0, 0, 100, 60);   // the "window"
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
            public void GUIHandlerInfo(int ID)
            {
                GUILayout.Label(Text, LabelBlackNoStretch);
            }
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
    }
}
