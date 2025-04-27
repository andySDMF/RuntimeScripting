using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.EventSystems;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class CameraOrbit : MonoBehaviour
    {
        [Header("Zoom")]
        [SerializeField]
        private float minZoom = 5;
        [SerializeField]
        private float maxZoom = 20;

        [Header("Pan")]
        [SerializeField]
        private bool limitPanning = false;
        [SerializeField]
        private Vector3 panLimits = new Vector3(100f, 1f, 100f);

        [Header("Sensitivity")]
        [SerializeField]
        private float zoomSpeed = 1;
        [SerializeField]
        private float panSpeed = 1;
        [SerializeField]
        private float rotateSpeed = 1;

        private CinemachineCamera m_freeLook;
        private CinemachineFollow m_follow;
        private float m_CameraDistanceTarget;
        private float m_CurrentCameraDistance;

        private Vector3 m_panScreenPoint;
        private Vector3 m_panOffset;
        private Tool m_tool = Tool.Pan;

        private Vector3 m_rotateScreenPoint;
        private float m_YAngle = 0.0f;
        private float m_XAngle = 0.0f;

        public bool Freeze { get; set; }

        public Tool CurrentTool
        {
            get
            {
                return m_tool;
            }
        }

        public float CameraDistance
        {
            get
            {
                return m_CurrentCameraDistance;
            }
        }

        private void Awake()
        {
            m_freeLook = GetComponentInChildren<CinemachineCamera>(true);
            m_follow = GetComponentInChildren<CinemachineFollow>(true);
            Freeze = false;
        }

        private void OnEnable()
        {
            PlayerManager.Instance.FreezePlayer(true);
            RaycastManager.Instance.CastRay = false;
            PlayerManager.OnUpdate += OnThisUpdate;

            m_CameraDistanceTarget = m_follow.FollowOffset.z;
        }

        private void OnDisable()
        {
            PlayerManager.OnUpdate -= OnThisUpdate;

            if (AppManager.IsCreated)
            {
                PlayerManager.Instance.FreezePlayer(false);
                RaycastManager.Instance.CastRay = true;
            }
        }

        private void OnThisUpdate()
        {
            if (Freeze) return;

            if (CinemachineCore.SoloCamera != null && CinemachineCore.SoloCamera.Name != m_freeLook.name)
            {
                CinemachineCore.SoloCamera = m_freeLook;
                return;
            }

            if (InputManager.Instance.GetMouseScrollWheel() != 0.0f)
            {
                m_CameraDistanceTarget -= (InputManager.Instance.GetMouseScrollWheel()) * zoomSpeed;
                m_CameraDistanceTarget = Mathf.Clamp(m_CameraDistanceTarget, minZoom, maxZoom);
            }

            UpdateCameraDistance(m_CameraDistanceTarget);

            //check if pointer of 2D UI
            if (EventSystem.current.IsPointerOverGameObject())
            {
                bool cancel = false;
                GameObject hoveredObject = InputManager.Instance.HoveredObject(InputManager.Instance.GetMousePosition());

                if (hoveredObject)
                {
                    Canvas canvas = hoveredObject.GetComponent<Canvas>();

                    if (canvas == null)
                    {
                        //check if there is one in the parent
                        canvas = hoveredObject.GetComponentInParent<Canvas>();
                    }

                    if (canvas != null)
                    {
                        if (canvas.renderMode.Equals(RenderMode.WorldSpace))
                        {
                            cancel = true;
                        }
                    }
                }

                if (!cancel)
                {
                    return;
                }
            }

            //check in viewport
            if (!InputManager.Instance.CheckWithinViewport())
            {
                return;
            }

            //cannot use multiple inputs at once
            if (!AppManager.Instance.Data.IsMobile && InputManager.Instance.MultipleMouseInputs())
            {
                m_tool = Tool.None;
                return;
            }


            if (m_tool == Tool.Rotate)
            {
                UpdateRotation();
            }
            else if (m_tool == Tool.Pan)
            {
                UpdatePan();
            }
            else
            {
                m_tool = Tool.None;
            }
        }

        public void UpdateCameraDistance(float target)
        {
            m_CameraDistanceTarget = target;
            m_CurrentCameraDistance = Damp(m_CurrentCameraDistance, m_CameraDistanceTarget, zoomSpeed);

            Vector3 m_lerp = new Vector3(0, 0, m_CurrentCameraDistance);
            m_follow.FollowOffset = m_lerp;
        }

        private void UpdateRotation()
        {
            Vector3 curRotation = transform.eulerAngles;
            m_XAngle = curRotation.y;
            m_YAngle = curRotation.x;

            if (InputManager.Instance.GetMouseButtonDown(1) || InputManager.Instance.GetMouseButtonDown(0))
            {
                m_rotateScreenPoint = Camera.main.WorldToScreenPoint(gameObject.transform.position);
            }
            else if (InputManager.Instance.GetMouseButton(1) || InputManager.Instance.GetMouseButton(0))
            {
                Vector2 delta = InputManager.Instance.GetMouseDelta("Mouse X", "Mouse Y");
                Vector3 screenPos = InputManager.Instance.GetMousePosition();

                if (screenPos.x != m_rotateScreenPoint.x)
                {
                    m_XAngle += delta.x;
                }

                if (screenPos.y != m_rotateScreenPoint.y)
                {
                    m_YAngle += delta.y;
                }

                m_rotateScreenPoint = screenPos;
            }

            float xAngle = Mathf.LerpAngle(curRotation.x, m_YAngle, Time.deltaTime * rotateSpeed);

            if (xAngle > 300 || xAngle < 60)
            {
                m_YAngle = xAngle;
            }
            else
            {
                m_YAngle = curRotation.x;
            }

            transform.eulerAngles = new Vector3(m_YAngle, Mathf.LerpAngle(curRotation.y, m_XAngle, Time.deltaTime * rotateSpeed), 0.0f);
        }

        private void UpdatePan()
        {
            Vector3 curPosition = transform.position;

            if (InputManager.Instance.GetMouseButtonDown(1) || InputManager.Instance.GetMouseButtonDown(0))
            {
                m_panScreenPoint = Camera.main.WorldToScreenPoint(gameObject.transform.position);
                Vector3 mousePos = InputManager.Instance.GetMousePosition();
                m_panOffset = gameObject.transform.position - Camera.main.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, m_panScreenPoint.z));

            }
            else if (InputManager.Instance.GetMouseButton(1) || InputManager.Instance.GetMouseButton(0))
            {
                Vector3 mousePos = InputManager.Instance.GetMousePosition();
                Vector3 curScreenPoint = new Vector3(mousePos.x, mousePos.y, m_panScreenPoint.z);
                curPosition = Camera.main.ScreenToWorldPoint(curScreenPoint) + m_panOffset;

                //need to check if the current position is within the bounds
                if (limitPanning)
                {
                    curPosition.x = Mathf.Clamp(curPosition.x, -panLimits.x / 2f, panLimits.x / 2f);
                    curPosition.y = Mathf.Clamp(curPosition.y, -panLimits.y / 2f, panLimits.y / 2f);
                    curPosition.z = Mathf.Clamp(curPosition.z, -panLimits.z / 2f, panLimits.z / 2f);
                }
            }

            transform.position = Vector3.Lerp(transform.position, curPosition, Time.deltaTime * panSpeed);
        }

        public void SetTool(Tool tool)
        {
            m_tool = tool;
        }

        public enum Tool { Pan, Rotate, ZoomIn, ZoomOut, None }


        private float Damp(float from, float to, float smoothing)
        {
            return Mathf.Lerp(from, to, 1f - Mathf.Exp(-smoothing * Time.unscaledDeltaTime));
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(CameraOrbit), true)]
        public class CameraOrbit_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("minZoom"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("maxZoom"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("limitPanning"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("panLimits"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("zoomSpeed"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("panSpeed"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("rotateSpeed"), true);

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
}
