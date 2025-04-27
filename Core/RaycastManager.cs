using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class RaycastManager : Singleton<RaycastManager>
    {
        public static RaycastManager Instance
        {
            get
            {
                return ((RaycastManager)instance);
            }
            set
            {
                instance = value;
            }
        }

        [Header("Cursor")]
        public GameObject cursor;

        private GameObject currentCursor;

        private Highlight highlight;
        private Tooltip tooltip;
        private Sound sound;
        private Transform hitObject = null;
        private float UIRaycastDistance = 5.0f;
        private bool m_sphereDetectedHit = false;

        public bool DisplayCursor
        {
            get;
            set;
        }

        public static System.Action OnPointerOverUI { get; set; }
        public static System.Action OnPointerOutsideOfViewport { get; set; }

        public static System.Action OnPlayerMoved { get; set; }

        public List<IRaycaster> Raycasters = new List<IRaycaster>();

        public bool CastRay
        {
            get;
            set;
        }

        public Material HighlightMaterial
        {
            get;
            private set;
        }

        private void Awake()
        {
            CastRay = false;
            DisplayCursor = true;
        }

        private void Start()
        {
            HighlightMaterial = Resources.Load<Material>(AppManager.Instance.Settings.projectSettings.highlightMaterial);

            if (CoreManager.Instance.projectSettings.useCursor)
            {
                currentCursor = Instantiate(cursor, transform);// spawn the 3d cursor
            }

            UIRaycastDistance = CoreManager.Instance.playerSettings.worldCanvasUIInteractionDistance;
        }

        private void OnDestroy()
        {
            OnPointerOutsideOfViewport = null;
            OnPointerOverUI = null;
        }

        private void LateUpdate()
        {
            if (PlayerManager.Instance.OrbitCameraActive) return;

            if (!CastRay)
            {
                if (currentCursor != null)
                {
                    currentCursor.SetActive(false);
                }

                if (highlight != null)
                {
                    highlight.HighlightObject(false);
                }

                highlight = null;

                if (tooltip)
                {
                    TooltipManager.Instance.HideTooltip();
                }

                tooltip = null;

                return;
            }

            if (CoreManager.Instance.CurrentState == state.Running)
            {
                bool hitWorldCanvas = false;

                if (currentCursor != null)
                {
                    currentCursor.SetActive(DisplayCursor);
                }

                //break if over UI object
                if (EventSystem.current.IsPointerOverGameObject())
                {
                    bool cancel = false;
                    GameObject hoveredObject = InputManager.Instance.HoveredObject(InputManager.Instance.GetMousePosition());

                    if (hoveredObject)
                    {
                        Canvas canvas = hoveredObject.GetComponent<Canvas>();

                        if(canvas == null)
                        {
                            //check if there is one in the parent
                            canvas = hoveredObject.GetComponentInParent<Canvas>();
                        }

                        if (canvas != null)
                        {
                            if (canvas.renderMode.Equals(RenderMode.WorldSpace))
                            {
                                hitWorldCanvas = true;
                                cancel = true;
                            }
                        }
                    }

                    if (!cancel)
                    {
                        if (OnPointerOverUI != null)
                        {
                            OnPointerOverUI.Invoke();
                        }

                        if (highlight != null)
                        {
                            highlight.HighlightObject(false);
                        }

                        highlight = null;

                        if (tooltip)
                        {
                            TooltipManager.Instance.HideTooltip();
                        }

                        tooltip = null;

                        DisplayCursor = false;

                        return;
                    }
                }

                //check in viewport
                if (!InputManager.Instance.CheckWithinViewport())
                {
                    DisplayCursor = false;

                    if (OnPointerOutsideOfViewport != null)
                    {
                        OnPointerOutsideOfViewport.Invoke();
                    }

                    if (highlight != null)
                    {
                        highlight.HighlightObject(false);
                    }

                    highlight = null;

                    if (tooltip)
                    {
                        TooltipManager.Instance.HideTooltip();
                    }

                    tooltip = null;
                    HUDManager.Instance.ShowRaycastPanel(false);

                    return;
                }

                //if player is moving using mouse input
                if (PlayerManager.Instance.IsPlayerControllerMoving())
                {
                    if (highlight != null)
                    {
                        highlight.HighlightObject(false);
                    }

                    highlight = null;

                    if (tooltip)
                    {
                        TooltipManager.Instance.HideTooltip();
                    }

                    tooltip = null;

                    if (OnPlayerMoved != null)
                    {
                        OnPlayerMoved.Invoke();
                    }

                    DisplayCursor = false;
                    HUDManager.Instance.ShowRaycastPanel(false);

                    return;
                }

                //need to check the new Raycast Type enum within the App Settings
                RaycastHit hit;
                Highlight hitHighlight;
                Sound hitSound;
                Tooltip hitTooltip;
                m_sphereDetectedHit = false;

                if (AppManager.Instance.Settings.playerSettings.raycastType.Equals(PlayerControlSettings.RaycastType._Box) ||
                    AppManager.Instance.Settings.playerSettings.raycastType.Equals(PlayerControlSettings.RaycastType._Both))
                {
                    float radius = CoreManager.Instance.playerSettings.interactionDistance / 2;
                    hitObject = null;

                    //ensure all raycasters have missed first
                    foreach (var iRay in Raycasters)
                    {
                        iRay.RaycastMiss();
                    }

                    if (!MapManager.Instance.TopDownViewActive)
                    {
                        if (Physics.BoxCast(PlayerManager.Instance.GetLocalPlayer().TransformObject.position, AppManager.Instance.Settings.playerSettings.RaycastBoxExtents, PlayerManager.Instance.GetLocalPlayer().TransformObject.forward, out hit, PlayerManager.Instance.GetLocalPlayer().TransformObject.rotation, radius))
                        {
                            //for now we will just handle the button script
                            if(hit.transform.GetComponentsInChildren<UnityEngine.UI.Button>().Length > 0)
                            {
                                hitObject = hit.transform;
                                m_sphereDetectedHit = true;
                            }
                            else
                            {
                                if(hit.transform.GetComponentInChildren<DropPoint>() != null)
                                {
                                    hitObject = hit.transform;
                                    m_sphereDetectedHit = true;
                                }
                            }
                        }

                        //if found hit object show Interact UI overlay and pass hitobject to it
                        if (m_sphereDetectedHit)
                        {
                            RaycastInteractionPanel panel = HUDManager.Instance.ShowRaycastPanel(true);
                            panel.InteractiveObject = hitObject.gameObject;
                        }
                        else
                        {
                            //hide Interact UI overlay
                            HUDManager.Instance.ShowRaycastPanel(false);
                        }
                    }
                }

                Camera cam = Camera.main;
                Ray ray = cam.ScreenPointToRay(InputManager.Instance.GetMousePosition());

                if (!AppManager.Instance.Settings.playerSettings.raycastType.Equals(PlayerControlSettings.RaycastType._Box) && !m_sphereDetectedHit)
                {
                    //shoot ray
                    if (cam != null && InputManager.Instance != null)
                    {
                        hitObject = null;

                        //check raycasters
                        foreach (var iRay in Raycasters)
                        {
                            float distance = CoreManager.Instance.playerSettings.interactionDistance;

                            if (iRay.OverrideDistance)
                            {
                                distance = iRay.Distance;
                            }
                            else
                            {
                                if (MapManager.Instance.TopDownViewActive)
                                {
                                    distance = 5000;
                                }
                            }

                            if (distance >= 0)
                            {
                                if (Physics.Raycast(ray, out hit, distance))
                                {
                                    iRay.RaycastHit(hit, out hitObject);

                                    if (hitObject != null)
                                    {
                                        break;
                                    }
                                    else
                                    {
                                        iRay.RaycastMiss();
                                    }
                                }
                                else
                                {
                                    iRay.RaycastMiss();
                                }
                            }
                            else
                            {
                                iRay.RaycastMiss();
                            }
                        }
                    }
                }

                //if there is a object hit based on raycaster
                if (hitObject != null)
                {
                    //check if hit object as highlight
                    hitHighlight = hitObject.GetComponent<Highlight>();

                    if (hitHighlight)
                    {
                        if (highlight != null && !highlight.Equals(hitHighlight))
                        {
                            highlight.HighlightObject(false);
                            highlight = null;
                        }

                        hitHighlight.HighlightObject(true);
                        highlight = hitHighlight;
                    }
                    else
                    {
                        if (highlight != null)
                        {
                            highlight.HighlightObject(false);
                        }

                        highlight = null;
                    }

                    //check if hit object as highlight
                    hitSound = hitObject.GetComponent<Sound>();

                    if (hitSound)
                    {
                        if (sound != null && !sound.Equals(hitSound))
                        {
                            sound.PlaySound(false);
                            sound = null;
                        }

                        hitSound.PlaySound(true);
                        sound = hitSound;
                    }
                    else
                    {
                        if (sound != null)
                        {
                            sound.PlaySound(false);
                        }

                        sound = null;
                    }

                    //tooltip
                    hitTooltip = hitObject.GetComponent<Tooltip>();

                    if (hitTooltip)
                    {
                        if (tooltip != null && !tooltip.Equals(hitTooltip))
                        {
                            TooltipManager.Instance.HideTooltip();
                            tooltip = null;
                        }

                        TooltipManager.Instance.ShowTooltip(hitTooltip.gameObject);
                        tooltip = hitTooltip;
                    }
                    else
                    {
                        if (tooltip != null)
                        {
                            TooltipManager.Instance.HideTooltip();
                        }

                        tooltip = null;
                    }

                    DisplayCursor = false;
                    return;
                }
                else
                {
                    if (highlight)
                    {
                        highlight.HighlightObject(false);
                    }

                    highlight = null;

                    if (tooltip)
                    {
                        TooltipManager.Instance.HideTooltip();
                    }

                    tooltip = null;
                }

                //display curser
                if (AppManager.Instance.Settings.playerSettings.enableNavMeshMovement)
                {
                    if (Physics.Raycast(ray, out hit) && !MapManager.Instance.TopDownViewActive)
                    {
                        if (hitWorldCanvas)
                        {
                            DisplayCursor = false;
                        }
                        else
                        {
                            NavMeshHit hitNavMesh;
                            bool blocked = NavMesh.Raycast(ray.origin, hit.point, out hitNavMesh, NavMesh.AllAreas);

                            if (!blocked && hitObject == null && !PlayerManager.Instance.IsPlayerControllerMoving())
                            {
                                // Move the 3d cursor in the world, only if the mouse moves
                                if (!CoreManager.Instance.IsMobile)
                                {
                                    DisplayCursor = true;
                                    hitNavMesh.position.Normalize();

                                    if (CoreManager.Instance.projectSettings.useCursor && currentCursor != null)
                                    {
                                        if (hitNavMesh.position != null)
                                        {
                                            float x = hitNavMesh.position.x;
                                            float y = hitNavMesh.position.y;
                                            float z = hitNavMesh.position.z;

                                            if (float.IsNaN(x) || float.IsInfinity(x))
                                            {
                                                x = currentCursor.transform.position.x;
                                            }

                                            if (float.IsNaN(y) || float.IsInfinity(y))
                                            {
                                                y = currentCursor.transform.position.y;
                                            }

                                            if (float.IsNaN(z) || float.IsInfinity(z))
                                            {
                                                z = currentCursor.transform.position.z;
                                            }

                                            currentCursor.transform.position = new Vector3(x, y, z);
                                        }
                                        else
                                        {
                                            currentCursor.transform.position = currentCursor.transform.position;
                                        }

                                        if (hitNavMesh.normal != null)
                                        {
                                            if (!hitNavMesh.normal.Equals(Vector3.zero))
                                            {
                                                float x = hitNavMesh.normal.x;
                                                float y = hitNavMesh.normal.y;
                                                float z = hitNavMesh.normal.z;

                                                if (float.IsNaN(x) || float.IsInfinity(x))
                                                {
                                                    x = currentCursor.transform.eulerAngles.x;
                                                }

                                                if (float.IsNaN(y) || float.IsInfinity(y))
                                                {
                                                    y = currentCursor.transform.eulerAngles.y;
                                                }

                                                if (float.IsNaN(z) || float.IsInfinity(z))
                                                {
                                                    z = currentCursor.transform.eulerAngles.z;
                                                }

                                                currentCursor.transform.rotation = Quaternion.LookRotation(new Vector3(x, y, x));
                                            }
                                            else
                                            {
                                                currentCursor.transform.rotation = currentCursor.transform.rotation;
                                            }
                                        }
                                        else
                                        {
                                            currentCursor.transform.rotation = currentCursor.transform.rotation;
                                        }
                                    }

                                    if (InputManager.Instance.GetMouseButtonUp(0) && InputManager.Instance.CheckWithinViewport())
                                    {
                                        //check if NaN
                                        float x = hitNavMesh.position.x;
                                        float y = hitNavMesh.position.y;
                                        float z = hitNavMesh.position.z;
                                     
                                        if ((float.IsNaN(x) || float.IsInfinity(x)) || (float.IsNaN(y) || float.IsInfinity(y)) || (float.IsNaN(z) || float.IsInfinity(z)))
                                        {
                                          
                                        }
                                        else
                                        {
                                            NavigationManager.Instance.NavMeshTeleport(hitNavMesh.position);
                                        }
                                    }
                                }
                                else
                                {
                                    DisplayCursor = false;
                                }

                                foreach (var iRay in Raycasters)
                                {
                                    iRay.RaycastMiss();
                                }
                            }
                            else
                            {
                                DisplayCursor = false;
                            }
                        }
                    }
                    else
                    {
                        DisplayCursor = false;
                    }
                }
                else
                {
                    DisplayCursor = false;
                }

            }
        }

        public void SetCastRay(bool cast)
        {
            CastRay = cast;
        }

        public bool UIRaycastOperation(GameObject obj, bool ignoreRayCastCheck = false)
        {
            if (PlayerManager.Instance.GetLocalPlayer() == null) return false;

            if (!CastRay && !ignoreRayCastCheck)
            {
                //check if selectable
                if (obj.GetComponent<UnityEngine.UI.Selectable>() == null)
                {
                    return false;
                }
            }

            Camera cam = Camera.main;
            Ray ray = cam.ScreenPointToRay(InputManager.Instance.GetMousePosition());
            RaycastHit hit;

            //need to check if hit a collider first
            if(!ignoreRayCastCheck)
            {
                if (Physics.Raycast(ray, out hit))
                {
                    if (hit.transform.gameObject != obj)
                    {
                        return false;
                    }
                }
            }

            if(AppManager.Instance.Data.SceneSpawnLocation != null)
            {
                return false;
            }

            if(PlayerManager.Instance.GetLocalPlayer().TransformObject == null)
            {
                return false;
            }

            return Vector3.Distance(PlayerManager.Instance.GetLocalPlayer().TransformObject.position, obj.transform.position) < UIRaycastDistance;
        }

        public void UIRaycastSelectablePressed(UnityEngine.UI.Selectable sel)
        {
            if (sel == null) return;

            StartCoroutine(SelectablePressed(sel));
        }

        private IEnumerator SelectablePressed(UnityEngine.UI.Selectable sel)
        {
            if (sel.targetGraphic != null)
            {
                Color col = sel.targetGraphic.color;
                sel.targetGraphic.CrossFadeAlpha(CoreManager.Instance.playerSettings.worldCanvasUIPressedColor.a, 0.1f, true);
                sel.targetGraphic.color = CoreManager.Instance.playerSettings.worldCanvasUIPressedColor;

                yield return new WaitForSeconds(0.1f);

                if (sel != null && sel.targetGraphic != null)
                {
                    sel.targetGraphic.color = col;
                    sel.targetGraphic.CrossFadeAlpha(col.a, 0.1f, true);
                }
            }

            yield return 0;
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(RaycastManager), true)]
        public class RaycastManager_Editor : BaseInspectorEditor
        {
            private void OnEnable()
            {
                GetBanner();
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();

                if (Application.productName.Equals("BL360 Plugin"))
                {
                    serializedObject.Update();

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("cursor"), true);

                    if (GUI.changed)
                    {
                        serializedObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(this);
                    }
                }
            }
        }
#endif
    }

    [System.Serializable]
    public enum HighlightType { Disabled, Color, Material }

    public interface IRaycaster
    {
        float Distance { get; }

        bool OverrideDistance { get; }

        void RaycastHit(RaycastHit hit, out Transform hitObject);

        void RaycastMiss();

        string UserCheckKey { get; }
    }
}
