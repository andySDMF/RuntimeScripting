using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Script taken from Spacehub project and adapted
/// </summary>
namespace BrandLab360
{
    public class CameraThirdPerson : MonoBehaviour
    {
        [Header("Components")]
        public Transform LookTarget;
        public CinemachineCamera FreeLookCamera;

        [Header("Follow")]
        public Vector3 followMinOffset = new Vector3(0, 0.3f, 0.8f);
        public Vector3 followMaxOffset = new Vector3(0, 3, 8);
        public float distanceOffset = 0.5f;
        public float smoothing = 0.1f;

        private float m_CameraDistanceTarget;
        private float m_CurrentCameraDistance;
        private ResetCameraWhen m_resetUpon = ResetCameraWhen._PlayerSprint;
        private CinemachineFollow m_follow;

        private Vector3 m_rotateScreenPoint;
        private float m_YAngle = 0.0f;
        private float m_XAngle = 0.0f;
        private PlayerJoystickReader m_joystickReader;

        private float m_xTarget = 0.0f;
        private float m_yTarget = 0.0f;

        public bool Freeze { get; set; }

        public bool IsRotating
        {
            get;
            private set;
        }

        public void SetCameraTarget(float target)
        {
            if (target < 0)
            {
                target = 0.0f;
            }

            if (target > 1.0f)
            {
                target = 1.0f;
            }

            m_CameraDistanceTarget = target;
            m_CurrentCameraDistance = m_CameraDistanceTarget;
        }

        public void SetMaxTarget()
        {
            SetCameraTarget(1.0f);
        }

        public void SetTargetRotation(float x, float y)
        {
            m_xTarget = y;
            m_yTarget = x;

            m_XAngle = m_xTarget;
            m_YAngle = m_yTarget;
        }


        private void Awake()
        {
            if (AppManager.IsCreated)
            {
                IsRotating = false;

                PlayerManager.OnUpdate += OnThisUpdate;

                m_follow = GetComponentInChildren<CinemachineFollow>(true);
                m_joystickReader = GetComponentInParent<PlayerJoystickReader>(true);

                m_resetUpon = AppManager.Instance.Settings.playerSettings.resetCameraWhen;

                ResetActiveCameraControl();
            }
        }

        private void OnDestroy()
        {
            PlayerManager.OnUpdate -= OnThisUpdate;
        }

        public void FreezeControl(bool freeze)
        {
            if (FreeLookCamera != null)
            {
                FreeLookCamera.enabled = freeze;
            }

            Freeze = freeze;
        }

        public void ResetActiveCameraControl()
        {
            if (FreeLookCamera == null) return;

            smoothing = AppManager.Instance.Settings.playerSettings.Smoothing;
            followMaxOffset = AppManager.Instance.Settings.playerSettings.followMaxOffset;
            followMinOffset = AppManager.Instance.Settings.playerSettings.followMinOffset;
            distanceOffset = AppManager.Instance.Settings.playerSettings.distanceOffset;

            m_xTarget = 0.0f;
            m_yTarget = 0.0f;

            m_XAngle = 0.0f;
            m_YAngle = 0.0f;

            SetCameraTarget(distanceOffset);
            IsRotating = false;
        }

        private void OnThisUpdate()
        {
            if (MapManager.Instance.TopDownViewActive)
            {
                return;
            }

            if (!Freeze && !PlayerManager.Instance.GetLocalPlayer().FreezePosition && !m_joystickReader.isMoving)
            {
                if (!UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                {
                    UpdateRotation();
                    UpdateCameraDistance();
                }
            }
            else
            {
                if(FreeLookCamera != null)
                {
                    float xAngle = Mathf.LerpAngle(m_YAngle, m_yTarget, Time.deltaTime * smoothing);
                    LookTarget.localEulerAngles = new Vector3(xAngle, Mathf.LerpAngle(m_XAngle, m_xTarget, Time.deltaTime * smoothing), 0.0f);
                }
            }
        }

        private void UpdateCameraDistance()
        {
            if (Freeze || PlayerManager.Instance.GetLocalPlayer().FreezePosition) return;

            if (FreeLookCamera == null) return;

            if (InputManager.Instance.GetMouseScrollWheel() != 0.0f)
            {
#if UNITY_EDITOR
                m_CameraDistanceTarget -= (InputManager.Instance.GetMouseScrollWheel() / 100) * smoothing;
#else
                m_CameraDistanceTarget -= InputManager.Instance.GetMouseScrollWheel() * smoothing;
#endif
                m_CameraDistanceTarget = Mathf.Clamp(m_CameraDistanceTarget, 0, 1);
            }

            m_CurrentCameraDistance = Damp(m_CurrentCameraDistance, m_CameraDistanceTarget, smoothing);

            float x = Mathf.Lerp(followMinOffset.x, followMaxOffset.x, m_CurrentCameraDistance);
            float y = Mathf.Lerp(followMinOffset.y, followMaxOffset.y, m_CurrentCameraDistance);
            float z = Mathf.Lerp(followMinOffset.z, followMaxOffset.z, m_CurrentCameraDistance);

            m_follow.FollowOffset = new Vector3(x, y, z * -1);
        }

        private void UpdateRotation()
        {
            if (Freeze || PlayerManager.Instance.GetLocalPlayer().FreezePosition) return;

            if (FreeLookCamera == null) return;

            Vector3 curRotation = LookTarget.localEulerAngles;

            bool isSprinting = m_resetUpon.Equals(ResetCameraWhen._PlayerMove) ? PlayerManager.Instance.GetLocalPlayer().MovementID > -1 && PlayerManager.Instance.GetLocalPlayer().MovementID < 2 : PlayerManager.Instance.GetLocalPlayer().IsSprinting;

            if (isSprinting)
            {
                ResetActiveCameraControl();
            }
            else
            {
                m_XAngle = curRotation.y;
                m_YAngle = curRotation.x;

                m_xTarget = m_XAngle;
                m_yTarget = m_YAngle;
            }

            if (InputManager.Instance.GetMouseButtonDown(1) || InputManager.Instance.GetMouseButtonDown(0))
            {
                m_rotateScreenPoint = Camera.main.WorldToScreenPoint(gameObject.transform.position);
            }
            else if (InputManager.Instance.GetMouseButton(1)  && !isSprinting|| (AppManager.Instance.Data.IsMobile && InputManager.Instance.GetMouseButton(0)) && !isSprinting)
            {
                IsRotating = true;

                Vector2 delta = InputManager.Instance.GetMouseDelta("Mouse X", "Mouse Y");
                Vector3 screenPos = InputManager.Instance.GetMousePosition();

                if (screenPos.x != m_rotateScreenPoint.x)
                {
                    m_xTarget += delta.x;
                   // m_XAngle += delta.x;
                }

                if (screenPos.y != m_rotateScreenPoint.y)
                {
                    m_yTarget += delta.y;
                   // m_YAngle += delta.y;
                }

                m_rotateScreenPoint = screenPos;
            }
            else if (InputManager.Instance.GetKey("NumpadPlus") && !isSprinting)
            {
                m_CameraDistanceTarget += 10.0f;
            }
            else if (InputManager.Instance.GetKey("NumpadMinus") && !isSprinting)
            {
                m_CameraDistanceTarget -= 10.0f;
            }
            else
            {
                IsRotating = false;
            }

            //float xAngle = Mathf.LerpAngle(curRotation.x, m_YAngle, Time.deltaTime * (smoothing * 2.0f));

            float xAngle = Mathf.LerpAngle(m_YAngle, m_yTarget, Time.deltaTime * (smoothing * 2.0f));

            if (xAngle > 300 || xAngle < 60)
            {
                m_yTarget = xAngle;
            }
            else
            {
                m_yTarget = m_YAngle;
            }

            LookTarget.localEulerAngles = new Vector3(m_yTarget, Mathf.LerpAngle(m_XAngle, m_xTarget, Time.deltaTime * smoothing), 0.0f);
        }

        private float Damp(float from, float to, float smoothing)
        {
            return Mathf.Lerp(from, to, 1f - Mathf.Exp(-smoothing * Time.unscaledDeltaTime));
        }

        [System.Serializable]
        public enum ResetCameraWhen { _PlayerMove, _PlayerSprint }

#if UNITY_EDITOR
        [CustomEditor(typeof(CameraThirdPerson), true)]
        public class CameraThirdPerson_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("LookTarget"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("FreeLookCamera"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("followMinOffset"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("followMaxOffset"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("distanceOffset"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("smoothing"), true);

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
