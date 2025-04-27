using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class HUDRolloverTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField]
        private RolloverEvent rolloverEvent = RolloverEvent._Both;

        [SerializeField]
        private string tooltip;

        [SerializeField]
        private AudioClip clip;

        public string Tooltip
        {
            get
            {
                return tooltip;
            }
            set
            {
                tooltip = value;
            }
        }

        public RolloverEvent RolloverType
        {
            get
            {
                return rolloverEvent;
            }
        }

        public AudioClip Audio
        {
            get
            {
                return clip;
            }
        }

        private bool m_isOver = false;

        private void OnDisable()
        {
            if (m_isOver)
            {
                HUDManager.Instance.ShowUIRolloverTooltip(false, "");
            }

            m_isOver = false;
        }

        public void OnPointerEnter(PointerEventData evtData)
        {
            m_isOver = true;
            bool childHasRollover = false;

            for (int i = 0; i < evtData.hovered.Count; i++)
            {
                if (evtData.hovered[i].GetComponent<HUDRolloverTooltip>())
                {
                    HUDRolloverTooltip HUDScript = evtData.hovered[i].GetComponent<HUDRolloverTooltip>();

                    if (HUDScript.RolloverType.Equals(RolloverEvent._Both) || HUDScript.RolloverType.Equals(RolloverEvent._Text))
                    {
                        HUDManager.Instance.ShowUIRolloverTooltip(true, HUDScript.Tooltip);
                    }

                    if (HUDScript.RolloverType.Equals(RolloverEvent._Both) || HUDScript.RolloverType.Equals(RolloverEvent._Sound))
                    {
                        if (AppManager.Instance.Settings.HUDSettings.enableUIRolloverSound)
                        {
                            if (HUDScript.clip == null)
                            {
                                HUDManager.Instance.PlayUISound(AppManager.Instance.Settings.HUDSettings.defaultRolloverClip);
                            }
                            else
                            {
                                HUDManager.Instance.PlayUISound(HUDScript.clip);
                            }
                        }
                    }

                    childHasRollover = true;
                    break;
                }
            }

            if (!childHasRollover)
            {
                if (rolloverEvent.Equals(RolloverEvent._Both) || rolloverEvent.Equals(RolloverEvent._Text))
                {
                    HUDManager.Instance.ShowUIRolloverTooltip(true, tooltip);
                }

                if (rolloverEvent.Equals(RolloverEvent._Both) || rolloverEvent.Equals(RolloverEvent._Sound))
                {
                    if (AppManager.Instance.Settings.HUDSettings.enableUIRolloverSound)
                    {
                        if (clip == null)
                        {
                            HUDManager.Instance.PlayUISound(AppManager.Instance.Settings.HUDSettings.defaultRolloverClip);
                        }
                        else
                        {
                            HUDManager.Instance.PlayUISound(clip);
                        }
                    }
                }
            }
        }

        public void OnPointerExit(PointerEventData evtData)
        {
            m_isOver = false;
            bool childHasRollover = false;

            for (int i = 0; i < evtData.hovered.Count; i++)
            {
                if (evtData.hovered[i].GetComponent<HUDRolloverTooltip>())
                {
                    HUDRolloverTooltip HUDScript = evtData.hovered[i].GetComponent<HUDRolloverTooltip>();

                    if (HUDScript.RolloverType.Equals(RolloverEvent._Both) || HUDScript.RolloverType.Equals(RolloverEvent._Text))
                    {
                        HUDManager.Instance.ShowUIRolloverTooltip(false, "");
                    }

                    if (HUDScript.RolloverType.Equals(RolloverEvent._Both) || HUDScript.RolloverType.Equals(RolloverEvent._Sound))
                    {
                        if (AppManager.Instance.Settings.HUDSettings.enableUIRolloverSound)
                        {
                            HUDManager.Instance.PlayUISound(null);
                        }
                    }


                    childHasRollover = true;
                    break;
                }
            }

            if (!childHasRollover)
            {
                if (rolloverEvent.Equals(RolloverEvent._Both) || rolloverEvent.Equals(RolloverEvent._Text))
                {
                    HUDManager.Instance.ShowUIRolloverTooltip(false, "");
                }

                if (rolloverEvent.Equals(RolloverEvent._Both) || rolloverEvent.Equals(RolloverEvent._Sound))
                {
                    if (AppManager.Instance.Settings.HUDSettings.enableUIRolloverSound)
                    {
                        HUDManager.Instance.PlayUISound(null);
                    }
                }
            }
        }

        [System.Serializable]
        public enum RolloverEvent { _Text, _Sound, _Both }

#if UNITY_EDITOR
        [CustomEditor(typeof(HUDRolloverTooltip), true)]
        public class HUDRolloverTooltip_Editor : BaseInspectorEditor
        {
            private void OnEnable()
            {
                GetBanner();
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();

                serializedObject.Update();

                EditorGUILayout.LabelField("Action", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("rolloverEvent"), true);

                EditorGUILayout.Space();


                if(serializedObject.FindProperty("rolloverEvent").enumValueIndex == 0)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("tooltip"), true);
                }
                else if (serializedObject.FindProperty("rolloverEvent").enumValueIndex == 1)
                {
                    EditorGUILayout.LabelField("If sound is null, it will use HUD Settings rollover sound", EditorStyles.miniLabel);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("clip"), true);
                }
                else
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("tooltip"), true);
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("If sound is null, it will use HUD Settings rollover sound", EditorStyles.miniLabel);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("clip"), true);
                }

                if (GUI.changed)
                {
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(this);
                }
            }
        }
#endif
    }
}
