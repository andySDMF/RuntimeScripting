using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

#if UNITY_PIPELINE_HDRP
using UnityEngine.Rendering.HighDefinition;
#endif

#if UNITY_PIPELINE_URP
using UnityEngine.Rendering.Universal;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    [RequireComponent(typeof(CinemachineCamera))]
    public class CameraCullingMask : MonoBehaviour
    {
        [SerializeField]
        private LayerMask cullingMask;

        [SerializeField]
        private CameraBackground backgroundType = CameraBackground.Sky;

        [SerializeField]
        private Color backgroundColor = Color.black;

        [SerializeField]
        private RenderOutput output = RenderOutput._Camera;

        [SerializeField]
        private RenderTexture cameraOutput;

        private float m_fovCache;
        private CinemachineCamera m_cam;

        private void Awake()
        {
            if (!AppManager.IsCreated) return;

            m_cam = GetComponent<CinemachineCamera>();
            m_fovCache = m_cam.Lens.FieldOfView;
        }

        private void OnEnable()
        {
            if (!AppManager.IsCreated) return;

            CinemachineCore.SoloCamera = GetComponent<CinemachineCamera>();

            if (Camera.main != null)
            {
                bool isPlayer = GetComponentInParent<IPlayer>() != null;
                bool isFirstPerson = GetComponentInParent<CameraThirdPerson>() == null;

                if (isPlayer)
                {
                    if (isFirstPerson)
                    {
                        if (CoreManager.Instance.playerSettings.overrideFPLayerMask)
                        {
                            cullingMask = CoreManager.Instance.playerSettings.FPMask;
                        }
                    }
                    else
                    {
                        if (CoreManager.Instance.playerSettings.overrideThirdPersonLayerMask)
                        {
                            cullingMask = CoreManager.Instance.playerSettings.TPMask;
                        }
                    }
                }

#if UNITY_PIPELINE_HDRP
                HDAdditionalCameraData HDData = Camera.main.GetComponent<HDAdditionalCameraData>();

                if(HDData == null)
                {
                    HDData = Camera.main.gameObject.AddComponent<HDAdditionalCameraData>();
                }

                HDData.clearColorMode = (backgroundType.Equals(CameraBackground.Color) ? HDAdditionalCameraData.ClearColorMode.Color : HDAdditionalCameraData.ClearColorMode.Sky);
                HDData.backgroundColorHDR = backgroundColor;
#elif UNITY_PIPELINE_URP
                UniversalAdditionalCameraData UData = Camera.main.GetComponent<UniversalAdditionalCameraData>();

                if(UData == null)
                {
                    UData = Camera.main.gameObject.AddComponent<UniversalAdditionalCameraData>();
                }

                Camera.main.clearFlags = (backgroundType.Equals(CameraBackground.Color) ? CameraClearFlags.Color : CameraClearFlags.Skybox);
                Camera.main.backgroundColor = backgroundColor;
                    

#else
                Camera.main.clearFlags = (backgroundType.Equals(CameraBackground.Color) ? CameraClearFlags.Color : CameraClearFlags.Skybox);
                Camera.main.backgroundColor = backgroundColor;
#endif


                Camera.main.cullingMask = cullingMask;

                if(output.Equals(RenderOutput._RenderTexture))
                {
                    Camera.main.targetTexture = cameraOutput;
                }
                
                Camera.main.orthographic = false;
            }
        }

        private void Update()
        {
            if(AppManager.IsCreated && m_cam != null)
            {
                if(OrientationManager.Instance.CurrentOrientation.Equals(OrientationType.landscape))
                {
                    m_cam.Lens.FieldOfView = m_fovCache;
                }
                else
                {
                    m_cam.Lens.FieldOfView = m_fovCache * 2.0f;
                }
            }
        }


        private void OnDestroy()
        {
            CinemachineCore.SoloCamera = null;
        }

        private enum CameraBackground { Sky, Color }

        private enum RenderOutput { _Camera, _RenderTexture }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(CameraCullingMask), true)]
    public class CameraCullingMask_Editor : BaseInspectorEditor
    {
        private void OnEnable()
        {
            GetBanner();
        }

        public override void OnInspectorGUI()
        {
            DisplayBanner();

            serializedObject.Update();

            EditorGUILayout.LabelField("Rendered Objects", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("cullingMask"), true);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Appearance", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("backgroundType"), true);

            if(serializedObject.FindProperty("backgroundType").enumValueIndex == 1)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("backgroundColor"), true);
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Render Output", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("output"), true);

            if (serializedObject.FindProperty("output").enumValueIndex == 1)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("cameraOutput"), true);
            }

            if(GUI.changed)
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(this);
            }
        }
    }
#endif
}
