using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Utilities;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;

namespace TerraTechETCUtil
{
    /// <summary>
    /// Manages custom minimap elements and drag-selection for more options
    /// <para><b>INSURE YOU CALL <see cref="LegModExt.InsurePatches()"/> BEFORE USING</b></para>
    /// </summary>
    public class ManMinimapExt
    {
        /// <summary>
        /// If this is allowed to apply it's changes to the UI
        /// <para>See <see cref="DeInitAll"/> to remove the instance</para>
        /// </summary>
        public static bool Enabled = true;

        /// <summary>
        /// Allow the player to swap techs from anywhere in the world using the map
        /// </summary>
        public static bool PermitPlayerMapJumpInAllNonMPModes = true;
        private const int layerPrioritySpacing = 1000;
        private const float MouseDeltaTillButtonIgnored = 9;

        /// <summary>
        /// Existing vanilla <see cref="ManRadar.IconType"/> count
        /// </summary>
        public static int VanillaMapIconCount { get; } = EnumValuesIterator<ManRadar.IconType>.Count;
        private static int LatestAddedMinimapIndex = VanillaMapIconCount - 1;
        /// <summary>
        /// The count of minimaps that were added
        /// </summary>
        public static int AddedMinimapIndexes = LatestAddedMinimapIndex;
        /// <summary>
        /// Tracking of the icons on the map
        /// </summary>
        public static Dictionary<Func<TrackedVisible, bool>, ManRadar.IconType> iconConditions = new Dictionary<Func<TrackedVisible, bool>, ManRadar.IconType>();
        /// <summary>
        /// New icons added
        /// </summary>
        public static Dictionary<ManRadar.IconType, ManRadar.IconEntry> addedIcons = new Dictionary<ManRadar.IconType, ManRadar.IconEntry>();

        /// <summary>
        /// Assign your event popup when a minimap element is selected.
        /// <para>VisibleID, <see cref="UIMiniMapElement"/> instance</para>
        /// </summary>
        public static Event<int, UIMiniMapElement> MiniMapElementSelectEvent = new Event<int, UIMiniMapElement>();
        /// <summary>
        /// The player is looking at the world map
        /// </summary>
        public static bool WorldMapActive => instWorld != null ? instWorld.gameObject.activeInHierarchy : false;

        internal static MinimapExt instWorld;
        internal static MinimapExt instMini;


        private static Dictionary<int, Type> LayersIndexedCached = new Dictionary<int, Type>();

        private static FieldInfo layers = typeof(UIMiniMapDisplay).GetField("m_ContentLayers", BindingFlags.NonPublic | BindingFlags.Instance);

        /// <summary>
        /// Forcibly removes this from the minimap system.
        /// <para>See <see cref="Enabled"/> to toggle it off entirely</para>
        /// </summary>
        public static void DeInitAll()
        {
            if (instWorld)
            {
                instWorld.DeInitInst();
                Debug_TTExt.Log("MinimapExtended DeInit MinimapExtended for " + instWorld.gameObject.name);
                instWorld = null;
            }
            if (instMini)
            {
                instMini.DeInitInst();
                Debug_TTExt.Log("MinimapExtended DeInit MinimapExtended for " + instMini.gameObject.name);
                instMini = null;
            }
            MenuSelectables = null;
        }
        internal static UIMiniMapElement lastElementLMB;
        internal static Vector2 startPosLMB;
        internal static UIMiniMapElement lastElementMMB;
        internal static Vector2 startPosMMB;
        internal static UIMiniMapElement lastElementRMB;
        /// <summary>
        /// Last <see cref="UIMiniMapElement"/> we opened the modal on
        /// </summary>
        public static UIMiniMapElement LastModaledTarget { get; internal set; } = null;
        /// <summary>
        /// Current <see cref="UIMiniMapElement"/> we opened the modal on
        /// </summary>
        public static UIMiniMapElement ModaledSelectTarget { get; private set; } = null;
        internal static Vector2 startPosRMB;
        internal static float lastClickTime = 0;
        internal static TrackedVisible nextPlayerTech;
        internal static bool transferInProgress = false;
        internal static Dictionary<ObjectTypes, List<KeyValuePair<Func<UIMiniMapElement, bool>, GUI_BM_Element>>> MenuSelectables =
            new Dictionary<ObjectTypes, List<KeyValuePair<Func<UIMiniMapElement, bool>, GUI_BM_Element>>>();

        internal static bool CanPlayerControl(TrackedVisible TV)
        {
            if (TV.visible == null)
            {
                var uT = TV.GetUnloadedTech();
                if (uT != null)
                {
                    foreach (var item in uT.m_TechData.m_BlockSpecs)
                    {
                        if (ManSpawn.inst.HasBlockDescriptorEnum(item.m_BlockType, typeof(BlockAttributes), 12))
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
            else
            {
                return TV.visible.tank.ControllableByLocalPlayer;
            }
        }

        /// <summary>
        /// Add a new Minimap layer to the map
        /// </summary>
        /// <param name="layerToAdd"></param>
        /// <param name="priority">Higher priorities (int is less) display over lower priorites (int is more)</param>
        /// <returns>True if added</returns>
        public static bool AddMinimapLayer(Type layerToAdd, int priority)
        {
            if (LayersIndexedCached.TryGetValue(priority, out Type other))
            {
                Debug_TTExt.Assert("MinimapExtended: The minimap layer " + layerToAdd.GetType().FullName + " could not be added as there was already "
                    + "a layer taking the priority level " + priority + " of type " + other.GetType().FullName);
                return false;
            }
            LayersIndexedCached.Add(priority, layerToAdd);
            UpdateAllMaps();
            return true;
        }
        /// <summary>
        /// Remove a Minimap layer from the map
        /// </summary>
        /// <param name="layerToRemove"></param>
        /// <param name="priority">Higher priorities (int is less) display over lower priorites (int is more)</param>
        /// <returns>True if removed</returns>
        public static void RemoveMinimapLayer(Type layerToRemove, int priority)
        {
            if (LayersIndexedCached.TryGetValue(priority, out Type other) && other == layerToRemove)
            {
                Debug_TTExt.Log("MinimapExtended: Removed minimap layer " + layerToRemove.GetType().FullName + " from priority level " + priority +
                    " successfully.");
                LayersIndexedCached.Remove(priority);
                UpdateAllMaps();
            }
        }

        private static FieldInfo iconClose = typeof(UIMiniMapLayerTech).GetField("m_ClosestIcons", BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo iconCache = typeof(UIMiniMapLayerTech).GetField("m_IconCache", BindingFlags.Instance | BindingFlags.NonPublic);
        private static Type iconCloseInst = typeof(UIMiniMapLayerTech).GetNestedType("ClosestIcons", BindingFlags.NonPublic);
        private static MethodInfo iconCloseInstReset = iconCloseInst.GetMethod("Reset", BindingFlags.Public);
        private static Type iconCacheInst = typeof(UIMiniMapLayerTech).GetNestedType("IconCache", BindingFlags.NonPublic);
        private static FieldInfo iconList = iconCacheInst.GetField("icons", BindingFlags.Instance | BindingFlags.NonPublic);

        /// <summary>
        ///  MIGHT BE SLOW - CHECK!!!!
        /// </summary>
        private static void RebuildClosestIcons()
        {
            try
            {
                AddedMinimapIndexes = LatestAddedMinimapIndex;
                if (instWorld != null)
                {
                    UIMiniMapLayerTech layer = instWorld.GetComponentInChildren<UIMiniMapLayerTech>(true);
                    object cache = Activator.CreateInstance(iconCacheInst, true);
                    //iconList.SetValue(cache, new List<UIMiniMapElement>());
                    ((List<UIMiniMapElement>)iconList.GetValue(cache)).Clear();
                    iconCache.SetValue(layer, cache);
                    //iconClose.SetValue(layer, Activator.CreateInstance(iconCloseInst, true));
                    iconCloseInstReset.Invoke(iconClose.GetValue(layer), Array.Empty<object>());
                }
                if (instMini != null)
                {
                    UIMiniMapLayerTech layer = instMini.GetComponentInChildren<UIMiniMapLayerTech>(true);
                    object cache = Activator.CreateInstance(iconCacheInst, true);
                    //iconList.SetValue(cache, new List<UIMiniMapElement>());
                    ((List<UIMiniMapElement>)iconList.GetValue(cache)).Clear();
                    iconCache.SetValue(layer, cache);
                    //iconClose.SetValue(layer, Activator.CreateInstance(iconCloseInst, true));
                    iconCloseInstReset.Invoke(iconClose.GetValue(layer), Array.Empty<object>());
                }
            }
            catch (Exception e)
            {
                DebugUtil.inst.ReRaiseException = e;
            }
        }
        /// <summary>
        /// YOU MUST CALL THIS BEFORE ANYTHING INITS
        /// </summary>
        /// <param name="ShouldShow"></param>
        /// <param name="texture"></param>
        /// <param name="color"></param>
        /// <param name="visibleBeyondMapBorder"></param>
        /// <param name="maxCountVisibleBeyondMapBorder"></param>
        /// <param name="visiblePriority"></param>
        /// <returns>0 if failed, otherwise a non-zero value that is the RadarType it is assigned to</returns>
        public static ManRadar.IconType AddCustomMinimapTechIconType(Func<TrackedVisible, bool> ShouldShow, Sprite sprite, Color color,
            bool visibleBeyondMapBorder, int maxCountVisibleBeyondMapBorder, float visiblePriority)
        {
            if (iconConditions.ContainsKey(ShouldShow))
                return 0;
            AddedMinimapIndexes = 0;
            LatestAddedMinimapIndex++;
            iconConditions.Add(ShouldShow, (ManRadar.IconType)LatestAddedMinimapIndex);
            var prefabBase = ManRadar.inst.GetIconElementPrefab(ManRadar.IconType.FriendlyVehicle);
            var prefab = prefabBase.transform.UnpooledSpawn(prefabBase.transform.parent, true).GetComponent<UIMiniMapElement>();
            if (prefab == null)
                throw new InvalidOperationException("Failed to create prefab");
            var element = prefab.GetComponent<UIMiniMapElement>();
            element.Icon.sprite = sprite;
            element.Icon.color = color;
            prefab.CreatePool(8);
            addedIcons.Add((ManRadar.IconType)LatestAddedMinimapIndex, new ManRadar.IconEntry()
            {
                mesh = null,
                canBeRadarMarkerIcon = false,
                colour = color,
                mapIconPrefab = prefab,
                numDisplayingAtRange = maxCountVisibleBeyondMapBorder,
                offMapRotates = visibleBeyondMapBorder,
                priority = visiblePriority,
            });
            if (!addedIcons.TryGetValue((ManRadar.IconType)LatestAddedMinimapIndex, out var val))
                throw new NullReferenceException("Stored " + ((ManRadar.IconType)LatestAddedMinimapIndex).ToString() + " but didn't get it back???");
            if (val.mapIconPrefab == null)
                throw new NullReferenceException("Stored " + ((ManRadar.IconType)LatestAddedMinimapIndex).ToString() +
                    " but failed to fetch instance?!?");
            InvokeHelper.CancelInvoke(RebuildClosestIcons);
            InvokeHelper.InvokeNextUpdate(RebuildClosestIcons);
            return (ManRadar.IconType)LatestAddedMinimapIndex;
        }

        /// <summary>
        /// Add a custom modal menu for an interactable on the minimap
        /// <para>Call <see cref="UpdateAllMaps"/> after you are fully done with this</para>
        /// </summary>
        /// <param name="type"></param>
        /// <param name="Name">Name of the element to display on UI</param>
        /// <param name="canShow">Boolean check if this should be shown in the modal with given conditions</param>
        /// <param name="onTriggered">Gives a float which can be processed into a set value for the element return on the ui, which is in range [0 ~ 1]</param>
        /// <param name="sprite">Sprite icon to use for it</param>
        /// <param name="sliderDescIfIsSlider">Name for the slider if this has one</param>
        /// <param name="numClampSteps">Number of positions the slider can take</param>
        /// <returns>The new element</returns>
        public static void AddMinimapInteractable(ObjectTypes type, string Name, Func<UIMiniMapElement, bool> canShow, 
            Func<float, float> onTriggered, Func<Sprite> sprite, Func<string> sliderDescIfIsSlider = null, int numClampSteps = 0)
        {
            if (MenuSelectables.TryGetValue(type, out var vals))
            {
                vals.Add(new KeyValuePair<Func<UIMiniMapElement, bool>, GUI_BM_Element>(canShow, ExtModuleClickable.MakeElement(
                    Name, onTriggered, sprite, sliderDescIfIsSlider, numClampSteps)));
            }
            else
                MenuSelectables.Add(type, new List<KeyValuePair<Func<UIMiniMapElement, bool>, GUI_BM_Element>> {
                    new KeyValuePair<Func<UIMiniMapElement, bool>, GUI_BM_Element>(canShow,
                    ExtModuleClickable.MakeElement(Name, onTriggered, sprite, sliderDescIfIsSlider, numClampSteps)) });
        }
        /// <inheritdoc cref="AddMinimapInteractable(ObjectTypes, string, Func{UIMiniMapElement, bool}, Func{float, float}, Func{Sprite}, Func{string}, int)"/>
        public static void AddMinimapInteractable(ObjectTypes type, Func<string> Name, Func<UIMiniMapElement, bool> canShow,
            Func<float, float> onTriggered, Func<Sprite> sprite, Func<string> sliderDescIfIsSlider = null, int numClampSteps = 0)
        {
            if (MenuSelectables.TryGetValue(type, out var vals))
            {
                vals.Add(new KeyValuePair<Func<UIMiniMapElement, bool>, GUI_BM_Element>(canShow, ExtModuleClickable.MakeElement(
                    Name, onTriggered, sprite, sliderDescIfIsSlider, numClampSteps)));
            }
            else
                MenuSelectables.Add(type, new List<KeyValuePair<Func<UIMiniMapElement, bool>, GUI_BM_Element>> {
                    new KeyValuePair<Func<UIMiniMapElement, bool>, GUI_BM_Element>(canShow,
                    ExtModuleClickable.MakeElement(Name, onTriggered, sprite, sliderDescIfIsSlider, numClampSteps)) });
        }
        /// <summary>
        /// Remove the minimap layer
        /// <para>Call <see cref="UpdateAllMaps"/> after you are fully done with this</para>
        /// </summary>
        /// <param name="type"></param>
        /// <param name="Name">Name of the element to display on UI</param>
        public static void RemoveMinimapInteractable(ObjectTypes type, string Name)
        {
            if (MenuSelectables.TryGetValue(type, out var vals))
            {
                vals.RemoveAll(x => x.Value.GetName == Name);
            }
        }

        private static List<GUI_BM_Element> tempCollect = new List<GUI_BM_Element>();
        /// <summary>
        /// The modal for the map was opened
        /// </summary>
        public static bool OpenedModal { get; private set; } = false;
        /// <summary>
        /// Modal was opened at this realtime + 0.4f
        /// </summary>
        public static float OpenedModalTime { get; private set; } = 0f;
        internal static void BringUpMinimapModal()
        {
            Debug_TTExt.Log("MinimapExtended - MiniMapElementSelectEvent " + (LastModaledTarget?.TrackedVis != null));
            if (LastModaledTarget?.TrackedVis == null)
                return;
            Debug_TTExt.Log("   Type: " + LastModaledTarget.TrackedVis.ObjectType.ToString());
            ModaledSelectTarget = LastModaledTarget;
            var tracked = ModaledSelectTarget.TrackedVis;
            if (tracked != null && MenuSelectables.TryGetValue(tracked.ObjectType, out var vals))
            {
                tempCollect.Clear();
                foreach (var val in vals)
                {
                    if (val.Key(ModaledSelectTarget))
                        tempCollect.Add(val.Value);
                }
                if (tempCollect.Count > 0)
                {
                    OpenedModal = true;
                    OpenedModalTime = Time.realtimeSinceStartup + 0.4f;
                    Debug_TTExt.Log("   Can open for: " + tempCollect.Count.ToString());
                    GUIModModal.OpenModal(tracked.ObjectType.ToString(), tempCollect.ToArray(), CanDisplayModal);// ONLY ON UI 
                }
            }
        }
        private static bool CanDisplayModal()
        {
            return ModaledSelectTarget != null && GUIModModal.CanContinueDisplayOverlap();
        }

        /// <summary>
        /// Update all added/removed layers shown on maps
        /// </summary>
        public static void UpdateAllMaps()
        {
            if (instMini)
                instMini.RemoveMinimapLayersAdded();
            if (instWorld)
                instWorld.RemoveMinimapLayersAdded();
            foreach (var item in LayersIndexedCached)
            {
                if (instMini)
                    instMini.AddMinimapLayer_Internal(item.Value, item.Key);
                if (instWorld)
                    instWorld.AddMinimapLayer_Internal(item.Value, item.Key);
            }
        }



        /// <summary>
        /// Upgrade the map to display tracks, allow Tech switching over far distances, ETC.
        ///   Higher priorities means the higher up it will be when loaded in
        /// Alters UIMiniMapDisplay.
        /// </summary>
        public class MinimapExt : MonoBehaviour
        {
            /// <summary>
            /// The minimap instance
            /// </summary>
            public UIMiniMapDisplay disp { get; private set; } = null;
            /// <summary>
            /// If this is the BIG world map
            /// </summary>
            public bool WorldMap { get; private set; } = false;
            private Dictionary<int, UIMiniMapLayer> LayersIndexed = new Dictionary<int, UIMiniMapLayer>();
            private HashSet<int> LayersIndexedAdded = new HashSet<int>();

            /// <summary>
            /// HANDLED AUTOMATICALLY
            /// </summary>
            /// <param name="target"></param>
            internal void InitInst(UIMiniMapDisplay target)
            {
                disp = target;
                //targInst = FindObjectOfType<UIMiniMapDisplay>();
                if (disp == null)
                {
                    Debug_TTExt.Assert("MinimapExtended in " + gameObject.name + " COULD NOT INITATE as it could not find UIMiniMapDisplay!");
                    return;
                }
                int PriorityStepper = 0;

                foreach (var item in GetMapLayers())
                {
                    LayersIndexed.Add(PriorityStepper, item);
                    PriorityStepper += layerPrioritySpacing;
                }
                if (disp.gameObject.name.GetHashCode() == "MapDisplay".GetHashCode())
                {
                    WorldMap = true;
                    disp.PointerDownEvent.Subscribe(OnClick);
                    disp.PointerUpEvent.Subscribe(OnRelease);
                    instWorld = this;
                    Debug_TTExt.Log("MinimapExtended Init MinimapExtended for " + disp.gameObject.name + " in mode World");
                }
                else
                {
                    WorldMap = false;
                    instMini = this;
                    Debug_TTExt.Log("MinimapExtended Init MinimapExtended for " + disp.gameObject.name + " in mode Mini");
                }
                disp.HideEvent.Subscribe(OnHide);
                UpdateAllMaps();
            }
            /// <summary>
            /// Remove this
            /// </summary>
            internal void DeInitInst()
            {
                disp.HideEvent.Unsubscribe(OnHide);
                if (WorldMap)
                {
                    disp.PointerUpEvent.Unsubscribe(OnRelease);
                    disp.PointerDownEvent.Unsubscribe(OnClick);
                }
                disp = null;
                LayersIndexed.Clear();
                Destroy(this);
            }



            internal void OnHide()
            {
                CancelInvoke();
            }

            internal void OnClick(PointerEventData PED)
            {
                OpenedModal = false;
                if (gameObject.activeInHierarchy)
                {
                    OpenedModalTime = 0f;
                    UIMiniMapElement selected = null;
                    if (PED.pointerPress != null)
                    {
                        selected = PED.pointerPress.GetComponent<UIMiniMapElement>();
                        if (selected?.TrackedVis == null || selected.TrackedVis.ObjectType == ObjectTypes.Waypoint)
                            selected = null;
                    }
                    if (selected == null && PED.rawPointerPress != null)
                    {
                        selected = PED.rawPointerPress.GetComponent<UIMiniMapElement>();
                        if (selected?.TrackedVis == null || selected.TrackedVis.ObjectType == ObjectTypes.Waypoint)
                            selected = null;
                    }
                    if (selected == null && PED.selectedObject != null)
                    {
                        selected = PED.selectedObject.GetComponent<UIMiniMapElement>();
                        if (selected?.TrackedVis == null || selected.TrackedVis.ObjectType == ObjectTypes.Waypoint)
                            selected = null;
                    }
                    if (selected == null)
                    {
                        var list = PED.hovered.FindAll(x => x != null && x.GetComponent<UIMiniMapElement>()).Select(x => x.GetComponent<UIMiniMapElement>());
                        if (list.Any())
                            selected = list.FirstOrDefault(x => x.TrackedVis != null && x.TrackedVis.ObjectType != ObjectTypes.Waypoint);
                    }
                    if (selected != null)
                    {
                        Debug_TTExt.Info("MinimapExtended - OnClick " + PED.button + " | " + selected.name);
                        switch (PED.button)
                        {
                            case PointerEventData.InputButton.Left:
                                lastElementLMB = selected;
                                startPosLMB = PED.position;
                                break;
                            case PointerEventData.InputButton.Middle:
                                lastElementMMB = selected;
                                startPosMMB = PED.position;
                                break;
                            case PointerEventData.InputButton.Right:
                                // Unreliable, doesn't work most of the time
                                lastElementRMB = selected;
                                startPosRMB = PED.position;
                                break;
                        }
                    }
                    else
                    {
                        Debug_TTExt.Info("MinimapExtended - OnClick " + PED.button + " | None");
                        switch (PED.button)
                        {
                            case PointerEventData.InputButton.Left:
                                lastElementLMB = null;
                                break;
                            case PointerEventData.InputButton.Middle:
                                lastElementMMB = null;
                                break;
                            case PointerEventData.InputButton.Right:
                                // Unreliable, doesn't work most of the time
                                lastElementRMB = null;
                                if (LastModaledTarget != null)
                                    BringUpMinimapModal();
                                break;
                        }
                    }
                }
            }
            internal void OnRelease(PointerEventData PED)
            {
                if (gameObject.activeInHierarchy)
                {
                    UIMiniMapElement selected = null;
                    if (PED.pointerPress != null)
                    {
                        selected = PED.pointerPress.GetComponent<UIMiniMapElement>();
                        if (selected?.TrackedVis == null || selected.TrackedVis.ObjectType == ObjectTypes.Waypoint)
                            selected = null;
                    }
                    if (selected == null && PED.rawPointerPress != null)
                    {
                        selected = PED.rawPointerPress.GetComponent<UIMiniMapElement>();
                        if (selected?.TrackedVis == null || selected.TrackedVis.ObjectType == ObjectTypes.Waypoint)
                            selected = null;
                    }
                    if (selected == null && PED.selectedObject != null)
                    {
                        selected = PED.selectedObject.GetComponent<UIMiniMapElement>();
                        if (selected?.TrackedVis == null || selected.TrackedVis.ObjectType == ObjectTypes.Waypoint)
                            selected = null;
                    }
                    if (selected == null)
                    {
                        var list = PED.hovered.FindAll(x => x != null && x.GetComponent<UIMiniMapElement>()).Select(x => x.GetComponent<UIMiniMapElement>());
                        if (list.Any())
                            selected = list.FirstOrDefault(x => x.TrackedVis != null && x.TrackedVis.ObjectType != ObjectTypes.Waypoint);
                    }
                    if (selected != null)
                    {
                        UIMiniMapElement lastElement = null;
                        Vector2 startPos = Vector2.zero;
                        switch (PED.button)
                        {
                            case PointerEventData.InputButton.Left:
                                lastElement = lastElementLMB;
                                lastElementLMB = null;
                                startPos = startPosLMB;
                                break;
                            case PointerEventData.InputButton.Middle:
                                lastElement = lastElementMMB;
                                lastElementMMB = null;
                                startPos = startPosMMB;
                                break;
                            case PointerEventData.InputButton.Right:
                                // Unreliable, doesn't work most of the time
                                lastElement = lastElementRMB;
                                lastElementRMB = null;
                                startPos = startPosRMB;
                                break;
                        }
                        if (lastElement != null && (startPos - PED.position).sqrMagnitude < MouseDeltaTillButtonIgnored)
                        {
                            //DebugRandAddi.Log("MinimapExtended - MiniMapElementSelectEvent " + PED.button + " | " + list.FirstOrDefault().name);
                            MiniMapElementSelectEvent.Send((int)PED.button, selected);
                        }
                    }
                }
            }

            internal static void HostLoadAllTilesOverlapped(IntVector2 tilePos)
            {
                for (int x = tilePos.x - 1; x < tilePos.x + 1; x++)
                {
                    for (int y = tilePos.y - 1; y < tilePos.y + 1; y++)
                    {
                        ManWorldTileExt.ClientTempLoadTile(new IntVector2(x, y), false, 2.5f);
                    }
                }
            }
            
            private static MethodInfo invokeRebuild = typeof(UITechManagerHUD).GetMethod("FullyRebuildTechList", BindingFlags.Instance | BindingFlags.NonPublic);
            

            /// <summary>
            /// Get all layers registered
            /// </summary>
            /// <returns></returns>
            public UIMiniMapLayer[] GetMapLayers()
            {
                return (UIMiniMapLayer[])layers.GetValue(disp);
            }


            internal bool AddMinimapLayer_Internal(Type layerToAdd, int priority)
            {
                var layer = (UIMiniMapLayer)gameObject.AddComponent(layerToAdd);
                layer.Init(disp);
                Debug_TTExt.Log("MinimapExtended: Added minimap layer " + layerToAdd.FullName + " to priority level " + priority +
                    " successfully.");
                LayersIndexed.Add(priority, layer);
                LayersIndexedAdded.Add(priority);
                UpdateAndSyncMinimapLayers();
                return true;
            }
            internal void RemoveMinimapLayersAdded()
            {
                foreach (var item in new HashSet<int>(LayersIndexedAdded))
                {
                    RemoveMinimapLayer_Internal(item);
                }
                UpdateAndSyncMinimapLayers();
            }
            internal void RemoveMinimapLayer_Internal(int priority)
            {
                if (LayersIndexed.TryGetValue(priority, out UIMiniMapLayer other))
                {
                    Debug_TTExt.Log("MinimapExtended: Removed minimap layer priority level " + priority + " successfully.");
                    LayersIndexedAdded.Remove(priority);
                    if (other is UIMiniMapLayerExt ext)
                        ext.OnRecycle();
                    Destroy(other);
                    LayersIndexed.Remove(priority);
                }
            }

            private void UpdateAndSyncMinimapLayers()
            {
                int arraySize = LayersIndexed.Count;
                var array = (UIMiniMapLayer[])layers.GetValue(disp);
                Array.Resize(ref array, arraySize);
                var toAdd = LayersIndexed.OrderBy(x => x.Key).ToList(); // RARE CALL
                for (int step = 0; step < arraySize; step++)
                {
                    array[step] = toAdd[step].Value;
                }
                layers.SetValue(disp, array);

                Debug_TTExt.Log("MinimapExtended: Rearranged " + array.Length + " layers.");
            }
        }
    }
    /// <summary>
    /// Adds a custom layer
    /// </summary>
    public class UIMiniMapLayerExt : UIMiniMapLayer
    {
        protected ManMinimapExt.MinimapExt ext;
        private bool init = false;
        protected bool WorldMap { get; private set; } = false;


        private void InsureInit()
        {
            if (!init)
            {
                init = true;
                ext = m_MapDisplay.GetComponent<ManMinimapExt.MinimapExt>();
                if (ext.WorldMap)
                    WorldMap = true;
                foreach (var item in ext.GetMapLayers())
                {
                    if (item is UIMiniMapLayerTech t)
                        m_RectTrans = t.GetComponent<RectTransform>();
                }
                ext.disp.ShowEvent.Subscribe(OnShow);
                ext.disp.HideEvent.Subscribe(OnHide);
                Init();
            }
        }
        /// <summary>
        /// Called when created
        /// </summary>
        protected virtual void Init() { }
        private void OnShow()
        {
            if (init)
            {
                Show();
            }
        }
        /// <summary>
        /// Called when menu opened
        /// </summary>
        protected virtual void Show() { }
        private void OnHide()
        {
            if (init)
            {
                Hide();
            }
        }
        /// <summary>
        /// Called when menu hidden
        /// </summary>
        protected virtual void Hide() { }
        internal void OnRecycle()
        {
            if (init)
            {
                init = false;
                Recycle();
                ext.disp.HideEvent.Unsubscribe(OnHide);
                ext.disp.ShowEvent.Unsubscribe(OnShow);
            }
        }
        /// <summary>
        /// Called when destroyed
        /// </summary>
        protected virtual void Recycle() { }

        /// <summary>
        /// Called when layer is updated
        /// </summary>
        public sealed override void UpdateLayer()
        {
            InsureInit();
            if (Singleton.playerTank)
                OnUpdateLayer();
        }
        /// <summary>
        /// Called when layer is updated
        /// </summary>
        public virtual void OnUpdateLayer() { }

        /// <summary>
        /// Custom icons pool
        /// </summary>
        protected class IconPool
        {
            private readonly UIMiniMapElement prefab;
            private readonly Stack<UIMiniMapElement> elementsUnused = new Stack<UIMiniMapElement>();
            private readonly Stack<UIMiniMapElement> elementsUsed = new Stack<UIMiniMapElement>();

            /// <summary>
            /// 
            /// </summary>
            /// <param name="prefab"></param>
            /// <param name="initSize"></param>
            public IconPool(UIMiniMapElement prefab, int initSize)
            {
                prefab.CreatePool(initSize);
                this.prefab = prefab;
            }
            /// <summary>
            /// 
            /// </summary>
            /// <param name="parent"></param>
            /// <returns></returns>
            public UIMiniMapElement ReuseOrSpawn(RectTransform parent)
            {
                UIMiniMapElement spawned;
                if (elementsUnused.Count > 0)
                {
                    spawned = elementsUnused.Pop();
                }
                else
                {
                    spawned = prefab.Spawn();
                    spawned.RectTrans.SetParent(parent, false);
                    foreach (var item in spawned.GetComponents<MonoBehaviour>())
                    {
                        item.enabled = true;
                    }
                    spawned.gameObject.SetActive(true);
                }
                elementsUsed.Push(spawned);
                return spawned;
            }
            /// <summary>
            /// 
            /// </summary>
            public void Reset()
            {
                while (elementsUsed.Count > 0)
                {
                    elementsUnused.Push(elementsUsed.Pop());
                }
            }
            /// <summary>
            /// 
            /// </summary>
            public void RemoveAllUnused()
            {
                while (elementsUnused.Count > 0)
                {
                    var ele = elementsUnused.Pop();
                    ele.RectTrans.SetParent(null);
                    ele.gameObject.SetActive(false);
                    ele.Recycle(false);
                }
            }
            /// <summary>
            /// 
            /// </summary>
            public void RemoveAll()
            {
                Reset();
                RemoveAllUnused();
            }
            /// <summary>
            /// 
            /// </summary>
            /// <param name="destroyPrefabToo"></param>
            public void DestroyAll(bool destroyPrefabToo = true)
            {
                RemoveAll();
                while (elementsUsed.Count > 0)
                {
                    var ele = elementsUsed.Pop();
                    ele.RectTrans.SetParent(null);
                    ele.gameObject.SetActive(false);
                    ele.Recycle(false);
                }
                prefab.DeletePool();
                if (destroyPrefabToo)
                    Destroy(prefab.gameObject);
            }
        }
    }
}
