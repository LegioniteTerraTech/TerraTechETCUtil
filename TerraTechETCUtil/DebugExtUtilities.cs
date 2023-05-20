using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TerraTechETCUtil
{
    public class DebugExtUtilities : MonoBehaviour
    {

        public static bool allowEnableDebugMenu_KeypadEnter = false;
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
                        Initiate();
                }
                else
                {
                    if (allowEnableDebugMenu_KeypadEnter)
                        DeInit();
                }
                allowEnableDebugMenu_KeypadEnter = value;
            }
        }
        private static GameObject DirPrefab = null;
        private static void InsureDirPrefab()
        {
            if (DirPrefab != null)
                return;
            DirPrefab = Instantiate(new GameObject("DebugLine"), null, false);
            var trans = DirPrefab.transform;
            trans.rotation = Quaternion.identity;
            trans.position = Vector3.zero;
            trans.localScale = Vector3.one;

            var lr = trans.GetComponent<LineRenderer>();
            if (!(bool)lr)
            {
                lr = DirPrefab.AddComponent<LineRenderer>();
                lr.material = new Material(Shader.Find("Sprites/Default"));
                lr.positionCount = 2;
                lr.startWidth = 0.5f;
                lr.useWorldSpace = false;
            }
            lr.startColor = Color.green;
            lr.endColor = Color.green;
            Vector3[] vecs = new Vector3[2] { Vector3.zero, Vector3.up };
            lr.SetPositions(vecs);
            DirPrefab.SetActive(false);
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
            var line = obj.transform.Find("DebugLine " + num);
            if (!(bool)line)
            {
                gO = UnityEngine.Object.Instantiate(new GameObject("DebugLine " + num), obj.transform, false);
            }
            else
                gO = line.gameObject;

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
            UnityEngine.Object.Destroy(gO, Time.deltaTime);
        }
        public static void DrawDirIndicator(Vector3 startPos, Vector3 endPos, Color color, float decayTime = 0)
        {
            InsureDirPrefab();
            GameObject gO;
            gO = UnityEngine.Object.Instantiate(DirPrefab, null);

            var lr = gO.GetComponent<LineRenderer>();
            lr.startColor = color;
            lr.endColor = color;
            gO.transform.position = startPos;
            Vector3[] vecs = new Vector3[2] { Vector3.zero, gO.transform.InverseTransformPoint(endPos) };
            lr.SetPositions(vecs);
            gO.SetActive(true);
            UnityEngine.Object.Destroy(gO, (decayTime <= 0) ? Time.deltaTime : decayTime);
            //DebugTAC_AI.Log("SPAWN DrawDirIndicator(World)");
        }
        private const int circleEdgeCount = 32;
        public static void DrawDirIndicatorCircle(Vector3 center, Vector3 normal, Vector3 flat, float radius, Color color, float decayTime = 0)
        {
            InsureDirPrefab();
            GameObject gO;
            gO = UnityEngine.Object.Instantiate(DirPrefab, null);

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
            UnityEngine.Object.Destroy(gO, (decayTime <= 0) ? Time.deltaTime : decayTime);
            //DebugTAC_AI.Log("SPAWN DrawDirIndicator(World)");
        }
        public static void DrawDirIndicatorSphere(Vector3 center, float radius, Color color, float decayTime = 0)
        {
            DrawDirIndicatorCircle(center, Vector3.up, Vector3.forward, radius, color, decayTime);
            DrawDirIndicatorCircle(center, Vector3.right, Vector3.forward, radius, color, decayTime);
            DrawDirIndicatorCircle(center, Vector3.forward, Vector3.up, radius, color, decayTime);
        }
        public static void DrawDirIndicatorRecPriz(Vector3 center, Vector3 size, Color color, float decayTime = 0)
        {
            Vector3 sizeCons = size / 2;
            Vector3 fruCorner = new Vector3(sizeCons.x, sizeCons.y, sizeCons.z);
            Vector3 frdCorner = new Vector3(sizeCons.x, -sizeCons.y, sizeCons.z);
            Vector3 rruCorner = new Vector3(sizeCons.x, sizeCons.y, -sizeCons.z);
            Vector3 rrdCorner = new Vector3(sizeCons.x, -sizeCons.y, -sizeCons.z);
            Vector3 fluCorner = new Vector3(-sizeCons.x, sizeCons.y, sizeCons.z);
            Vector3 fldCorner = new Vector3(-sizeCons.x, -sizeCons.y, sizeCons.z);
            Vector3 rluCorner = new Vector3(-sizeCons.x, sizeCons.y, -sizeCons.z);
            Vector3 rldCorner = new Vector3(-sizeCons.x, -sizeCons.y, -sizeCons.z);

            DrawDirIndicator(center + fruCorner, center + frdCorner, color, decayTime);
            DrawDirIndicator(center + fluCorner, center + fldCorner, color, decayTime);
            DrawDirIndicator(center + fruCorner, center + fluCorner, color, decayTime);
            DrawDirIndicator(center + frdCorner, center + fldCorner, color, decayTime);

            DrawDirIndicator(center + fruCorner, center + rruCorner, color, decayTime);
            DrawDirIndicator(center + fluCorner, center + rluCorner, color, decayTime);
            DrawDirIndicator(center + frdCorner, center + rrdCorner, color, decayTime);
            DrawDirIndicator(center + fldCorner, center + rldCorner, color, decayTime);

            DrawDirIndicator(center + rruCorner, center + rrdCorner, color, decayTime);
            DrawDirIndicator(center + rluCorner, center + rldCorner, color, decayTime);
            DrawDirIndicator(center + rruCorner, center + rluCorner, color, decayTime);
            DrawDirIndicator(center + rrdCorner, center + rldCorner, color, decayTime);
        }
        public static void DrawDirIndicatorRecPriz(Vector3 center, Quaternion rotation, Vector3 size, Color color, float decayTime = 0)
        {
            Vector3 sizeCons = size / 2;
            Vector3 fruCorner = rotation * new Vector3(sizeCons.x, sizeCons.y, sizeCons.z);
            Vector3 frdCorner = rotation * new Vector3(sizeCons.x, -sizeCons.y, sizeCons.z);
            Vector3 rruCorner = rotation * new Vector3(sizeCons.x, sizeCons.y, -sizeCons.z);
            Vector3 rrdCorner = rotation * new Vector3(sizeCons.x, -sizeCons.y, -sizeCons.z);
            Vector3 fluCorner = rotation * new Vector3(-sizeCons.x, sizeCons.y, sizeCons.z);
            Vector3 fldCorner = rotation * new Vector3(-sizeCons.x, -sizeCons.y, sizeCons.z);
            Vector3 rluCorner = rotation * new Vector3(-sizeCons.x, sizeCons.y, -sizeCons.z);
            Vector3 rldCorner = rotation * new Vector3(-sizeCons.x, -sizeCons.y, -sizeCons.z);

            DrawDirIndicator(center + fruCorner, center + frdCorner, color, decayTime);
            DrawDirIndicator(center + fluCorner, center + fldCorner, color, decayTime);
            DrawDirIndicator(center + fruCorner, center + fluCorner, color, decayTime);
            DrawDirIndicator(center + frdCorner, center + fldCorner, color, decayTime);

            DrawDirIndicator(center + fruCorner, center + rruCorner, color, decayTime);
            DrawDirIndicator(center + fluCorner, center + rluCorner, color, decayTime);
            DrawDirIndicator(center + frdCorner, center + rrdCorner, color, decayTime);
            DrawDirIndicator(center + fldCorner, center + rldCorner, color, decayTime);

            DrawDirIndicator(center + rruCorner, center + rrdCorner, color, decayTime);
            DrawDirIndicator(center + rluCorner, center + rldCorner, color, decayTime);
            DrawDirIndicator(center + rruCorner, center + rluCorner, color, decayTime);
            DrawDirIndicator(center + rrdCorner, center + rldCorner, color, decayTime);
        }
        public static void DrawDirIndicatorRecPrizExt(Vector3 center, Vector3 extents, Color color, float decayTime = 0)
        {
            Vector3 fruCorner = new Vector3(extents.x, extents.y, extents.z);
            Vector3 frdCorner = new Vector3(extents.x, -extents.y, extents.z);
            Vector3 rruCorner = new Vector3(extents.x, extents.y, -extents.z);
            Vector3 rrdCorner = new Vector3(extents.x, -extents.y, -extents.z);
            Vector3 fluCorner = new Vector3(-extents.x, extents.y, extents.z);
            Vector3 fldCorner = new Vector3(-extents.x, -extents.y, extents.z);
            Vector3 rluCorner = new Vector3(-extents.x, extents.y, -extents.z);
            Vector3 rldCorner = new Vector3(-extents.x, -extents.y, -extents.z);

            DrawDirIndicator(center + fruCorner, center + frdCorner, color, decayTime);
            DrawDirIndicator(center + fluCorner, center + fldCorner, color, decayTime);
            DrawDirIndicator(center + fruCorner, center + fluCorner, color, decayTime);
            DrawDirIndicator(center + frdCorner, center + fldCorner, color, decayTime);

            DrawDirIndicator(center + fruCorner, center + rruCorner, color, decayTime);
            DrawDirIndicator(center + fluCorner, center + rluCorner, color, decayTime);
            DrawDirIndicator(center + frdCorner, center + rrdCorner, color, decayTime);
            DrawDirIndicator(center + fldCorner, center + rldCorner, color, decayTime);

            DrawDirIndicator(center + rruCorner, center + rrdCorner, color, decayTime);
            DrawDirIndicator(center + rluCorner, center + rldCorner, color, decayTime);
            DrawDirIndicator(center + rruCorner, center + rluCorner, color, decayTime);
            DrawDirIndicator(center + rrdCorner, center + rldCorner, color, decayTime);
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
        private static bool UIIsCurrentlyOpen = false;
        internal class GUIDisplayExtUtil : MonoBehaviour
        {
            private void OnGUI()
            {
                HotWindow = GUILayout.Window(DisplayExtID, HotWindow, GUIHandlerDebug, "<b>Ext Mod Info</b>");
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
            GUILayout.BeginVertical(GUILayout.Width(HotWindow.width / 2));
            ResourcesHelper.GUIManaged.GUIGetTotalManaged();
            InvokeHelper.GUIManaged.GUIGetTotalManaged();
            GUILayout.EndVertical();
            GUILayout.BeginVertical();
            SFXHelpers.GUIManaged.GUIGetTotalManaged();
            SpawnHelper.GUIManaged.GUIGetTotalManaged();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.EndScrollView();
            GUI.DragWindow();
        }
    }
}
