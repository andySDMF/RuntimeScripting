using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    public class CameraBrain : MonoBehaviour
    {
        public static CameraBrain Instance;

        private void Awake()
        {
            Instance = this;

            DontDestroyOnLoad(gameObject);
        }

        public void DestroyAudioListener()
        {
            AudioListener aListener = GetComponent<AudioListener>();

            if(aListener != null)
            {
                Destroy(aListener);
            }
        }

        public void ApplySetting(bool ignoreAvatarScene = false)
        {
            Camera.main.useOcclusionCulling = AppManager.Instance.Settings.projectSettings.useOcclusionCulling;

#if UNITY_PIPELINE_HDRP
            HDAdditionalCameraData HDCData = GetComponent<HDAdditionalCameraData>();

            if(HDCData == null)
            {
                HDCData = gameObject.AddComponent<HDAdditionalCameraData>();
            }

            switch (AppManager.Instance.Settings.projectSettings.antiAliasingMode)
            {
                case AntiAliasingMode.TAA:
                    HDCData.antialiasing = HDAdditionalCameraData.AntialiasingMode.TemporalAntialiasing;

                    if (AppManager.Instance.Settings.projectSettings.TAAPresetMode.Equals(QualityPresetMode.Low))
                    {
                        HDCData.TAAQuality = HDAdditionalCameraData.TAAQualityLevel.Low;
                    }
                    else if (AppManager.Instance.Settings.projectSettings.TAAPresetMode.Equals(QualityPresetMode.Medium))
                    {
                        HDCData.TAAQuality = HDAdditionalCameraData.TAAQualityLevel.Medium;
                    }
                    else
                    {
                        HDCData.TAAQuality = HDAdditionalCameraData.TAAQualityLevel.High;
                    }

                    HDCData.taaSharpenStrength = AppManager.Instance.Settings.projectSettings.TAASettings.sharpenStength;
                    HDCData.taaHistorySharpening = AppManager.Instance.Settings.projectSettings.TAASettings.historySharpening;
                    HDCData.taaAntiFlicker = AppManager.Instance.Settings.projectSettings.TAASettings.antiFlickering;
                    HDCData.taaMotionVectorRejection = AppManager.Instance.Settings.projectSettings.TAASettings.speedRejection;
                    HDCData.taaAntiHistoryRinging = AppManager.Instance.Settings.projectSettings.TAASettings.antiRinging;
                    break;
                case AntiAliasingMode.SMAA:
                    HDCData.antialiasing = HDAdditionalCameraData.AntialiasingMode.SubpixelMorphologicalAntiAliasing;

                    if (AppManager.Instance.Settings.projectSettings.SMAAPresetMode.Equals(QualityPresetMode.Low))
                    {
                        HDCData.SMAAQuality = HDAdditionalCameraData.SMAAQualityLevel.Low;
                    }
                    else if (AppManager.Instance.Settings.projectSettings.SMAAPresetMode.Equals(QualityPresetMode.Medium))
                    {
                        HDCData.SMAAQuality = HDAdditionalCameraData.SMAAQualityLevel.Medium;
                    }
                    else
                    {
                        HDCData.SMAAQuality = HDAdditionalCameraData.SMAAQualityLevel.High;
                    }

                    break;
                case AntiAliasingMode.FXAA:
                    HDCData.antialiasing = HDAdditionalCameraData.AntialiasingMode.FastApproximateAntialiasing;
                    break;
                default:
                    HDCData.antialiasing = HDAdditionalCameraData.AntialiasingMode.None;
                    break;
            }

            Debug.Log($"Setting Anti-aliasing to to {HDCData.antialiasing}");

#elif UNITY_PIPELINE_URP
            UniversalAdditionalCameraData UCData = GetComponent<UniversalAdditionalCameraData>();

            if (UCData == null)
            {
                UCData = gameObject.AddComponent<UniversalAdditionalCameraData>();
            }

            switch (AppManager.Instance.Settings.projectSettings.antiAliasingMode)
            {
                case AntiAliasingMode.TAA:
                    UCData.antialiasing = AntialiasingMode.TemporalAntiAliasing;

                    if (AppManager.Instance.Settings.projectSettings.TAAPresetMode.Equals(QualityPresetMode.Low))
                    {
                        UCData.antialiasingQuality = AntialiasingQuality.Low;
                    }
                    else if (AppManager.Instance.Settings.projectSettings.TAAPresetMode.Equals(QualityPresetMode.Medium))
                    {
                        UCData.antialiasingQuality = AntialiasingQuality.Medium;
                    }
                    else
                    {
                        UCData.antialiasingQuality = AntialiasingQuality.High;
                    }
                    break;
                case AntiAliasingMode.SMAA:
                    UCData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;

                    if (AppManager.Instance.Settings.projectSettings.SMAAPresetMode.Equals(QualityPresetMode.Low))
                    {
                        UCData.antialiasingQuality = AntialiasingQuality.Low;
                    }
                    else if (AppManager.Instance.Settings.projectSettings.SMAAPresetMode.Equals(QualityPresetMode.Medium))
                    {
                        UCData.antialiasingQuality = AntialiasingQuality.Medium;
                    }
                    else
                    {
                        UCData.antialiasingQuality = AntialiasingQuality.High;
                    }

                    break;
                case AntiAliasingMode.FXAA:
                    UCData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
                    break;
                default:
                    UCData.antialiasing = AntialiasingMode.None;
                    break;
            }

            Debug.Log($"Setting Anti-aliasing to to {UCData.antialiasing}");
#endif
        }

        [System.Serializable]
        public enum AntiAliasingMode { None, SMAA, FXAA, TAA }

        [System.Serializable]
        public enum QualityPresetMode { Low, Medium, High }

        [System.Serializable]
        public class TAASettings
        {
            public float sharpenStength = 0.5f;
            public float historySharpening = 0.35f;
            public float antiFlickering = 0.5f;
            public float speedRejection = 0.5f;
            public bool antiRinging = false;
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(CameraBrain), true)]
        public class CameraBrain_Editor : BaseInspectorEditor
        {
            private void OnEnable()
            {
                GetBanner();
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();
            }
        }
#endif
    }
}
