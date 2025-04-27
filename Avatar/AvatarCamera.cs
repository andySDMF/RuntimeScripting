using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    [RequireComponent(typeof(CinemachineCamera))]
    public class AvatarCamera : MonoBehaviour
    {

        public CinemachineCamera Cam
        {
            get
            {
                return GetComponent<CinemachineCamera>();
            }
        }

        private void Start()
        {
#if UNITY_PIPELINE_HDRP
        
#elif UNITY_PIPELINE_URP

            if(AppManager.IsCreated)
            {
                if(AppManager.Instance.Settings.projectSettings.avatarSetupMode.Equals(AvatarSetupMode.Simple))
                {
                    Light l = FindFirstObjectByType<Light>();
                    l.intensity = 2;
                }
            }
#else
            Light l = FindFirstObjectByType<Light>();
            l.intensity = 2;
#endif
        }

        private void Update()
        {
            //get the directional light in scene and update the lighting
            if (AppManager.Instance.Settings.projectSettings.avatarSetupMode.Equals(AvatarSetupMode.Simple))
            {
                Light[] lights = FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);

                for (int i = 0; i < lights.Length; i++)
                {
                    if (lights[i].gameObject.scene.name != AppManager.Instance.Settings.projectSettings.avatarSceneName) continue;

                    lights[i].intensity = 2;
                }
            }
            else if (AppManager.Instance.Settings.projectSettings.overrideAvatarLightingIntensity)
            {
                Light[] lights = FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);

                for (int i = 0; i < lights.Length; i++)
                {
                    if (lights[i].gameObject.scene.name != AppManager.Instance.Settings.projectSettings.avatarSceneName) continue;

                    lights[i].intensity = AppManager.Instance.Settings.projectSettings.avatarLightingIntensity;
                }
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(AvatarCamera), true)]
        public class AvatarCamera_Editor : BaseInspectorEditor
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
