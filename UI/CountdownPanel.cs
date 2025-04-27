using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class CountdownPanel : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI countdownMessage;

        [SerializeField]
        private TextMeshProUGUI countdownTimer;

        [SerializeField]
        private TextMeshProUGUI countdownInstruction;

        private float m_countdown = 6.0f;
        private bool m_started = false;
        private System.Action m_onComplete;

        private float m_titleFontSize;
        private float m_timerFontSize;
        private float m_instructionFontSize;
        private RectTransform m_mainLayout;
        private float m_layoutWidth;

        private void Awake()
        {
            m_titleFontSize = countdownMessage.fontSize;
            m_timerFontSize = countdownTimer.fontSize;
            m_instructionFontSize = countdownInstruction.fontSize;

            m_mainLayout = transform.GetChild(0).GetComponent<RectTransform>();
            m_layoutWidth = m_mainLayout.sizeDelta.x;
        }

        private void OnEnable()
        {
            OrientationManager.Instance.OnOrientationChanged += OnOrientation;

            OnOrientation(OrientationManager.Instance.CurrentOrientation, (int)OrientationManager.Instance.ScreenSize.x, (int)OrientationManager.Instance.ScreenSize.y);

            SubtitleManager.Instance.Stop();
            SubtitleManager.Instance.ToggleButtonVisibiliy(false);
        }

        private void OnDisable()
        {
            OrientationManager.Instance.OnOrientationChanged -= OnOrientation;

            SubtitleManager.Instance.ToggleButtonVisibiliy(true);
        }

        private void Update()
        {
            if (!m_started) return;

            float minutes = Mathf.FloorToInt(m_countdown / 60);
            float seconds = Mathf.FloorToInt(m_countdown % 60);
            countdownTimer.text = string.Format("{0:00}:{1:00}", minutes, seconds);

            if (m_countdown > 0)
            {
                m_countdown -= Time.deltaTime;
            }
            else
            {
                if (m_onComplete != null)
                {
                    m_started = false;
                    m_onComplete.Invoke();
                    gameObject.SetActive(false);
                }
            }
        }

        public void Begin(string message, string instruction, int timer, System.Action callback)
        {
            m_countdown = timer + 1;
            m_started = true;
            countdownMessage.text = message;
            countdownInstruction.text = instruction;

            m_onComplete = callback;
            gameObject.SetActive(true);
        }

        private void OnOrientation(OrientationType arg0, int arg1, int arg2)
        {
            if (AppManager.Instance.Data.IsMobile)
            {
                if (arg0.Equals(OrientationType.landscape))
                {
                    countdownMessage.fontSize = m_titleFontSize;
                    countdownTimer.fontSize = m_timerFontSize;
                    countdownInstruction.fontSize = m_instructionFontSize;

                    m_mainLayout.anchorMin = new Vector2(0.5f, 0.5f);
                    m_mainLayout.anchorMax = new Vector2(0.5f, 0.5f);
                    m_mainLayout.anchoredPosition = Vector2.zero;
                    m_mainLayout.sizeDelta = new Vector2(m_layoutWidth, m_mainLayout.sizeDelta.y);
                }
                else
                {
                    float aspect = arg2 / arg1;
                    countdownMessage.fontSize = m_titleFontSize * aspect;
                    countdownTimer.fontSize = m_timerFontSize * aspect;
                    countdownInstruction.fontSize = m_instructionFontSize * aspect;

                    m_mainLayout.anchorMin = new Vector2(0f, 0.5f);
                    m_mainLayout.anchorMax = new Vector2(1f, 0.5f);
                    m_mainLayout.offsetMax = new Vector2(-50, 0);
                    m_mainLayout.offsetMin = new Vector2(50, 0);
                }
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(CountdownPanel), true)]
        public class CountdownPanel_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("countdownMessage"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("countdownTimer"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("countdownInstruction"), true);


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
