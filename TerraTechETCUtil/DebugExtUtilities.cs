using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

#if !EDITOR
namespace TerraTechETCUtil
{
    public static class PoolExtensionsExt
    {
        public static void Recycle(this Transform trans, float delay)
        {
            InvokeHelper.Invoke(DoRecycleDelayed, delay, trans);
        }
        public static void DoRecycleDelayed(Transform trans)
        {
            trans.SetParent(null);
            trans.Recycle();
        }
    }
    public class DebugExtUtilities : MonoBehaviour
    {
        private static bool allowEnableDebugMenu_KeypadEnter = false;
        /// <summary>
        /// Allow the Debug menu of ExtUtil to be accessable from in-game with (HOLD) End, (PRESS) Return.
        /// </summary>
        public static bool AllowEnableDebugGUIMenu_KeypadEnter
        {
            get => allowEnableDebugMenu_KeypadEnter;
            set
            {
                if (value)
                {
                    if (!allowEnableDebugMenu_KeypadEnter)
                    {
                        Debug_TTExt.Assert("WARNING!!!   TerraTechETCUtil  DEBUG MODE ENABLED");
                        Initiate();
                    }
                }
                else
                {
                    if (allowEnableDebugMenu_KeypadEnter)
                        DeInit();
                }
                allowEnableDebugMenu_KeypadEnter = value;
            }
        }


        private static Transform DirPrefab = null;
        private static void InsureDirPrefab()
        {
            if (DirPrefab != null)
                return;
            GameObject DirPrefabGO = Instantiate(new GameObject("DebugLine"), null, false);
            DirPrefab = DirPrefabGO.transform;
            DirPrefab.rotation = Quaternion.identity;
            DirPrefab.position = Vector3.zero;
            DirPrefab.localScale = Vector3.one;

            var lr = DirPrefabGO.GetComponent<LineRenderer>();
            if (!(bool)lr)
            {
                lr = DirPrefabGO.AddComponent<LineRenderer>();
                lr.material = new Material(Shader.Find("Sprites/Default"));
                lr.positionCount = 2;
                lr.startWidth = 0.5f;
                lr.useWorldSpace = false;
            }
            lr.startColor = Color.green;
            lr.endColor = Color.green;
            Vector3[] vecs = new Vector3[2] { Vector3.zero, Vector3.up };
            lr.SetPositions(vecs);
            DirPrefabGO.SetActive(false);
            DirPrefab.CreatePool(32);
        }
        public static void RecyclePrefab(Transform trans, LineRenderer LR, float delay)
        {
            InvokeHelper.Invoke(DoRecycleDelayed, delay, trans, LR);
        }
        private static Vector3 posFar = Vector3.one * 10000;
        private static void DoRecycleDelayed(Transform trans, LineRenderer LR)
        {
            LR.positionCount = 2;
            LR.SetPositions(new Vector3[2] { posFar, posFar });
            trans.gameObject.SetActive(false);
            trans.SetParent(null);
            trans.Recycle();
        }

        /// <summary>
        /// endPosGlobal is GLOBAL ROTATION in relation to local tech.
        /// </summary>
        /// <param name="obj">Parent GO</param>
        /// <param name="num">Index (each DrawDirIndicator of this override to be shown each frame must have it's own unique index)</param>
        /// <param name="endPosGlobalSpaceOffset">The other end of the Directional Indicator. In GLOBAL ROTATION in relation to local tech</param>
        /// <param name="color">Color to display the Directional Indicator</param>
        public static void DrawDirIndicator(GameObject obj, int num, Vector3 endPosGlobalSpaceOffset, Color color)
        {
            GameObject gO;
            Transform trans;
            string lineName = "DebugLine " + num;
            var line = obj.transform.Find("DebugLine " + num);
            if (!(bool)line)
            {
                trans = DirPrefab.Spawn(obj.transform, Vector3.zero);
                gO = trans.gameObject;
                gO.name = lineName;
            }
            else
            {
                gO = line.gameObject;
                trans = gO.transform;
            }

            var lr = gO.GetComponent<LineRenderer>();
            if (!(bool)lr)
            {
                lr = gO.AddComponent<LineRenderer>();
                lr.material = new Material(Shader.Find("Sprites/Default"));
                lr.positionCount = 2;
                lr.startWidth = 0.5f;
            }
            lr.startColor = color;
            lr.endColor = color;
            Vector3 pos = obj.transform.position;
            Vector3[] vecs = new Vector3[2] { pos, endPosGlobalSpaceOffset + pos };
            lr.SetPositions(vecs);
            RecyclePrefab(trans, lr, Time.deltaTime);
            //DebugTAC_AI.Log("SPAWN DrawDirIndicator(Local)");
        }
        public static void DrawDirIndicator(Vector3 startPos, Vector3 endPos, Color color, float decayTime = 0)
        {
            InsureDirPrefab();
            Transform trans = DirPrefab.Spawn();
            GameObject gO = trans.gameObject;

            var lr = gO.GetComponent<LineRenderer>();
            lr.startColor = color;
            lr.endColor = color;
            lr.positionCount = 2;
            gO.transform.position = startPos;
            Vector3[] vecs = new Vector3[2] { Vector3.zero, gO.transform.InverseTransformPoint(endPos) };
            lr.SetPositions(vecs);
            gO.SetActive(true);
            RecyclePrefab(trans, lr, (decayTime <= 0) ? Time.deltaTime : decayTime);
            //DebugTAC_AI.Log("SPAWN DrawDirIndicator(World)");
        }
        private const int circleEdgeCount = 32;
        public static void DrawDirIndicatorCircle(Vector3 center, Vector3 normal, Vector3 flat, float radius, Color color, float decayTime = 0)
        {
            if (radius <= 0)
            {
                DrawDirIndicator(center, center + (normal * 4), color, decayTime);
                return;
            }
            InsureDirPrefab();
            Transform trans = DirPrefab.Spawn();
            GameObject gO = trans.gameObject;

            var lr = gO.GetComponent<LineRenderer>();
            lr.startColor = color;
            lr.endColor = color;
            gO.transform.position = center;
            gO.transform.rotation = Quaternion.identity;
            lr.positionCount = circleEdgeCount + 1;
            Vector3[] vecs = new Vector3[circleEdgeCount + 1];
            for (int step = 0; step <= circleEdgeCount; step++)
            {
                vecs[step] = Quaternion.AngleAxis(360 * ((float)step / circleEdgeCount), normal) * flat * radius;
            }
            lr.SetPositions(vecs);
            gO.SetActive(true);
            RecyclePrefab(trans, lr,(decayTime <= 0) ? Time.deltaTime : decayTime);
            //DebugTAC_AI.Log("SPAWN DrawDirIndicatorCircle(World)");
        }
        public static void DrawDirIndicatorSphere(Vector3 center, float radius, Color color, float decayTime = 0)
        {
            DrawDirIndicatorCircle(center, Vector3.up, Vector3.forward, radius, color, decayTime);
            DrawDirIndicatorCircle(center, Vector3.right, Vector3.forward, radius, color, decayTime);
            DrawDirIndicatorCircle(center, Vector3.forward, Vector3.up, radius, color, decayTime);
        }
        public static void DrawDirIndicatorRecPrizCorner(Vector3 center, Vector3 size, Color color, float decayTime = 0)
        {
            Vector3 fruCorner = new Vector3(size.x, size.y, size.z) + center;
            Vector3 frdCorner = new Vector3(size.x, 0, size.z) + center;
            Vector3 rruCorner = new Vector3(size.x, size.y, 0) + center;
            Vector3 rrdCorner = new Vector3(size.x, 0, 0) + center;
            Vector3 fluCorner = new Vector3(0, size.y, size.z) + center;
            Vector3 fldCorner = new Vector3(0, 0, size.z) + center;
            Vector3 rluCorner = new Vector3(0, size.y, 0) + center;
            Vector3 rldCorner = new Vector3(0, 0, 0) + center;

            DrawDirIndicator(fruCorner, frdCorner, color, decayTime);
            DrawDirIndicator(fluCorner, fldCorner, color, decayTime);
            DrawDirIndicator(fruCorner, fluCorner, color, decayTime);
            DrawDirIndicator(frdCorner, fldCorner, color, decayTime);

            DrawDirIndicator(fruCorner, rruCorner, color, decayTime);
            DrawDirIndicator(fluCorner, rluCorner, color, decayTime);
            DrawDirIndicator(frdCorner, rrdCorner, color, decayTime);
            DrawDirIndicator(fldCorner, rldCorner, color, decayTime);

            DrawDirIndicator(rruCorner, rrdCorner, color, decayTime);
            DrawDirIndicator(rluCorner, rldCorner, color, decayTime);
            DrawDirIndicator(rruCorner, rluCorner, color, decayTime);
            DrawDirIndicator(rrdCorner, rldCorner, color, decayTime);
        }
        public static void DrawDirIndicatorRecPriz(Vector3 center, Vector3 size, Color color, float decayTime = 0)
        {
            Vector3 sizeCons = size / 2;
            Vector3 fruCorner = new Vector3(sizeCons.x, sizeCons.y, sizeCons.z) + center;
            Vector3 frdCorner = new Vector3(sizeCons.x, -sizeCons.y, sizeCons.z) + center;
            Vector3 rruCorner = new Vector3(sizeCons.x, sizeCons.y, -sizeCons.z) + center;
            Vector3 rrdCorner = new Vector3(sizeCons.x, -sizeCons.y, -sizeCons.z) + center;
            Vector3 fluCorner = new Vector3(-sizeCons.x, sizeCons.y, sizeCons.z) + center;
            Vector3 fldCorner = new Vector3(-sizeCons.x, -sizeCons.y, sizeCons.z) + center;
            Vector3 rluCorner = new Vector3(-sizeCons.x, sizeCons.y, -sizeCons.z) + center;
            Vector3 rldCorner = new Vector3(-sizeCons.x, -sizeCons.y, -sizeCons.z) + center;

            DrawDirIndicator(fruCorner, frdCorner, color, decayTime);
            DrawDirIndicator(fluCorner, fldCorner, color, decayTime);
            DrawDirIndicator(fruCorner, fluCorner, color, decayTime);
            DrawDirIndicator(frdCorner, fldCorner, color, decayTime);

            DrawDirIndicator(fruCorner, rruCorner, color, decayTime);
            DrawDirIndicator(fluCorner, rluCorner, color, decayTime);
            DrawDirIndicator(frdCorner, rrdCorner, color, decayTime);
            DrawDirIndicator(fldCorner, rldCorner, color, decayTime);

            DrawDirIndicator(rruCorner, rrdCorner, color, decayTime);
            DrawDirIndicator(rluCorner, rldCorner, color, decayTime);
            DrawDirIndicator(rruCorner, rluCorner, color, decayTime);
            DrawDirIndicator(rrdCorner, rldCorner, color, decayTime);
        }
        public static void DrawDirIndicatorRecPriz(Vector3 center, Quaternion rotation, Vector3 size, Color color, float decayTime = 0)
        {
            Vector3 sizeCons = size / 2;
            Vector3 fruCorner = center + (rotation * new Vector3(sizeCons.x, sizeCons.y, sizeCons.z));
            Vector3 frdCorner = center + (rotation * new Vector3(sizeCons.x, -sizeCons.y, sizeCons.z));
            Vector3 rruCorner = center + (rotation * new Vector3(sizeCons.x, sizeCons.y, -sizeCons.z));
            Vector3 rrdCorner = center + (rotation * new Vector3(sizeCons.x, -sizeCons.y, -sizeCons.z));
            Vector3 fluCorner = center + (rotation * new Vector3(-sizeCons.x, sizeCons.y, sizeCons.z));
            Vector3 fldCorner = center + (rotation * new Vector3(-sizeCons.x, -sizeCons.y, sizeCons.z));
            Vector3 rluCorner = center + (rotation * new Vector3(-sizeCons.x, sizeCons.y, -sizeCons.z));
            Vector3 rldCorner = center + (rotation * new Vector3(-sizeCons.x, -sizeCons.y, -sizeCons.z));

            DrawDirIndicator(fruCorner, frdCorner, color, decayTime);
            DrawDirIndicator(fluCorner, fldCorner, color, decayTime);
            DrawDirIndicator(fruCorner, fluCorner, color, decayTime);
            DrawDirIndicator(frdCorner, fldCorner, color, decayTime);

            DrawDirIndicator(fruCorner, rruCorner, color, decayTime);
            DrawDirIndicator(fluCorner, rluCorner, color, decayTime);
            DrawDirIndicator(frdCorner, rrdCorner, color, decayTime);
            DrawDirIndicator(fldCorner, rldCorner, color, decayTime);

            DrawDirIndicator(rruCorner, rrdCorner, color, decayTime);
            DrawDirIndicator(rluCorner, rldCorner, color, decayTime);
            DrawDirIndicator(rruCorner, rluCorner, color, decayTime);
            DrawDirIndicator(rrdCorner, rldCorner, color, decayTime);
        }
        public static void DrawDirIndicatorRecPrizExt(Vector3 center, Vector3 extents, Color color, float decayTime = 0)
        {
            Vector3 fruCorner = center + new Vector3(extents.x, extents.y, extents.z);
            Vector3 frdCorner = center + new Vector3(extents.x, -extents.y, extents.z);
            Vector3 rruCorner = center + new Vector3(extents.x, extents.y, -extents.z);
            Vector3 rrdCorner = center + new Vector3(extents.x, -extents.y, -extents.z);
            Vector3 fluCorner = center + new Vector3(-extents.x, extents.y, extents.z);
            Vector3 fldCorner = center + new Vector3(-extents.x, -extents.y, extents.z);
            Vector3 rluCorner = center + new Vector3(-extents.x, extents.y, -extents.z);
            Vector3 rldCorner = center + new Vector3(-extents.x, -extents.y, -extents.z);

            DrawDirIndicator(fruCorner, frdCorner, color, decayTime);
            DrawDirIndicator(fluCorner, fldCorner, color, decayTime);
            DrawDirIndicator(fruCorner, fluCorner, color, decayTime);
            DrawDirIndicator(frdCorner, fldCorner, color, decayTime);

            DrawDirIndicator(fruCorner, rruCorner, color, decayTime);
            DrawDirIndicator(fluCorner, rluCorner, color, decayTime);
            DrawDirIndicator(frdCorner, rrdCorner, color, decayTime);
            DrawDirIndicator(fldCorner, rldCorner, color, decayTime);

            DrawDirIndicator(rruCorner, rrdCorner, color, decayTime);
            DrawDirIndicator(rluCorner, rldCorner, color, decayTime);
            DrawDirIndicator(rruCorner, rluCorner, color, decayTime);
            DrawDirIndicator(rrdCorner, rldCorner, color, decayTime);
        }

        private static Color[] colorBatcher = new Color[]
            {
                new Color(0,1,0),
                new Color(1,0,1),
                new Color(1,0,0),
                new Color(0,1,1),
                new Color(0,0,1),
                new Color(1,1,0),
                new Color(1,1,1),
            };
        /// <summary>
        /// Gets a color with high contrast against other colors
        /// </summary>
        /// <param name="ID">The index to display. MUST be above 0</param>
        /// <param name="opacity">The Opacity of the color</param>
        /// <returns>The generated color to use</returns>
        public static Color GetUniqueColor(int ID, float opacity = 1)
        {
            if (ID < 0)
                throw new IndexOutOfRangeException("DebugExtUtilities.GetUniqueColor does not support ID values below 0");
            Color batch = colorBatcher[ID % colorBatcher.Length];
            float depth;
            if (ID % 2 == 1)
                depth = 1 - (ID * 0.075f);
            else
                depth = (ID * 0.075f) + 0.125f;
            return new Color(batch.r * depth, batch.r * depth, batch.r * depth, opacity);
        }
        /// <summary>
        /// Blend two colors together.
        /// </summary>
        /// <param name="host">The main color to apply to</param>
        /// <param name="toApply">The color to multiply/add to <paramref name="host"/></param>
        /// <param name="strength">The strength of <paramref name="toApply"/> in adding it's colors. MUST be within [0 - 1]</param>
        /// <returns>The blended color</returns>
        public static Color BlendColors(Color host, Color toApply, float strength)
        {
            return ((host * toApply) * (1 - strength)) + ((host + toApply) * strength);
        }

        private static void Initiate()
        {
            if (GUIWindow)
                return;
            Debug_TTExt.Log("Debug_TTExt: Debugger launched");
            GUIWindow = new GameObject();
            GUIWindow.AddComponent<GUIDisplayExtUtil>();
            GUIWindow.SetActive(false);
            InvokeHelper.InvokeSingleRepeat(StaticUpdate, 0);
        }
        private static void DeInit()
        {
            if (!GUIWindow)
                return;
            Debug_TTExt.Log("Debug_TTExt: Debugger Disabled");
            InvokeHelper.CancelInvokeSingleRepeat(StaticUpdate);
            Destroy(GUIWindow);
            GUIWindow = null;
        }
        private static void StaticUpdate()
        {
            if (Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                UIIsCurrentlyOpen = !UIIsCurrentlyOpen;
                GUIWindow.SetActive(UIIsCurrentlyOpen);
                Debug_TTExt.Log("Debug_TTExt: Debugger UI Open: " + UIIsCurrentlyOpen);
            }
            //Debug_TTExt.Log("Debug_TTExt: Debugger StaticUpdate()");
        }
        internal static void Open()
        {
            if (!UIIsCurrentlyOpen)
            {
                UIIsCurrentlyOpen = true;
                GUIWindow.SetActive(true);
            }
        }
        internal static void Close()
        {
            if (UIIsCurrentlyOpen)
            {
                UIIsCurrentlyOpen = false;
                GUIWindow.SetActive(false);
            }
        }
        private static bool UIIsCurrentlyOpen = false;
        internal class GUIDisplayExtUtil : MonoBehaviour
        {
            private void OnGUI()
            {
                HotWindow = AltUI.Window(DisplayExtID, HotWindow, GUIHandlerDebug, "<b>Ext Mod Info</b>", Close);
            }
        }

        private static GameObject GUIWindow;
        internal static Rect HotWindow = new Rect(0, 0, 200, 230);   // the "window"
        private const int DisplayExtID = 8200356;
        private static Vector2 scrolll = new Vector2(0, 0);
        private const int MaxWindowHeight = 500;
        private static readonly int MaxWindowWidth = 800;
        private static void GUIHandlerDebug(int ID)
        {
            HotWindow.height = MaxWindowHeight + 80;
            HotWindow.width = MaxWindowWidth + 60;
            scrolll = GUILayout.BeginScrollView(scrolll);
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            BlockIndexer.GUIManaged.GUIGetTotalManaged();
            ResourcesHelper.GUIManaged.GUIGetTotalManaged();
            InvokeHelper.GUIManaged.GUIGetTotalManaged();
            SFXHelpers.GUIManaged.GUIGetTotalManaged();
            SpawnHelper.GUIManaged.GUIGetTotalManaged();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.EndScrollView();
            GUI.DragWindow();
        }
    }
}
#endif