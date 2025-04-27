using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class ToastPanel : MonoBehaviour
    {
        [Header("Display")]
        [SerializeField]
        private int maxMessageCharDisplay = 25;

        [SerializeField]
        private float displayDuration = 5.0f;

        [Header("Prefab")]
        [SerializeField]
        private GameObject toastPrefab;

        [SerializeField]
        private Transform toastContainer;

        [Header("Styles")]
        [SerializeField]
        private ToastTypeStyle messageStyle;
        [SerializeField]
        private ToastTypeStyle callStyle;
        [SerializeField]
        private ToastTypeStyle videoStyle;
        [SerializeField]
        private ToastTypeStyle friendStyle;

        private RectTransform m_mainLayout;
        private float m_layoutWidth;
        private float m_offset;

        private void Awake()
        {
            m_mainLayout = transform.GetChild(0).GetComponent<RectTransform>();
            m_layoutWidth = m_mainLayout.sizeDelta.x;
            m_offset = Math.Abs(m_mainLayout.anchoredPosition.y);
        }

        private void OnEnable()
        {
            OrientationManager.Instance.OnOrientationChanged += OnOrientation;
            OnOrientation(OrientationManager.Instance.CurrentOrientation, (int)OrientationManager.Instance.ScreenSize.x, (int)OrientationManager.Instance.ScreenSize.y);
        }

        private void OnDisable()
        {
            OrientationManager.Instance.OnOrientationChanged -= OnOrientation;
        }

        private void OnOrientation(OrientationType arg0, int arg1, int arg2)
        {
            if (AppManager.Instance.Data.IsMobile)
            {
                if (arg0.Equals(OrientationType.landscape))
                {
                    m_mainLayout.sizeDelta = new Vector2(m_layoutWidth, m_mainLayout.sizeDelta.y);
                    m_mainLayout.offsetMax = new Vector2(m_mainLayout.offsetMax.x, (m_offset * AppManager.Instance.Settings.HUDSettings.mobileButtonScaler) * -1);
                }
                else
                {
                    float aspect = arg2 / arg1;

                    m_mainLayout.sizeDelta = new Vector2(m_layoutWidth * aspect, m_mainLayout.sizeDelta.y);
                    m_mainLayout.offsetMax = new Vector2(m_mainLayout.offsetMax.x, ((m_offset * AppManager.Instance.Settings.HUDSettings.mobileButtonScaler) * aspect) * -1);
 
                }
            }
        }

        public void Add(ToastType type, string player, string message )
        {
            if(!gameObject.activeInHierarchy)
            {
                gameObject.SetActive(true);
            }

            GameObject toast = Instantiate(toastPrefab, Vector3.zero, Quaternion.identity, toastContainer);
            toast.transform.localScale = Vector3.one;

            ToastTypeStyle style = null;
            
            switch(type)
            {
                case ToastType._Message:
                    style = messageStyle;
                    break;
                case ToastType._Call:
                    style = callStyle;
                    break;
                case ToastType._Friend:
                    style = friendStyle;
                    break;
                case ToastType._Video:
                    style = videoStyle;
                    break;
                default:
                    break;
            }

            toast.GetComponentInChildren<SmartphoneToast>(true).Set(type, style, player, message, maxMessageCharDisplay, displayDuration);
        }

        public Sprite GetToastIcon(ToastType style)
        {
            Sprite sp = null;

            switch (style)
            {
                case ToastType._Message:
                    sp = messageStyle.icon;
                    break;
                case ToastType._Call:
                    sp = callStyle.icon;
                    break;
                case ToastType._Friend:
                    sp = friendStyle.icon;
                    break;
                case ToastType._Video:
                    sp = videoStyle.icon;
                    break;
                default:
                    break;
            }

            return sp;
        }

        public enum ToastType { _Message, _Call, _Video, _Friend }

        [System.Serializable]
        public class ToastTypeStyle
        {
            public string title;
            public Sprite icon;
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(ToastPanel), true)]
        public class ToastPanel_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("maxMessageCharDisplay"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("displayDuration"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("toastPrefab"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("toastContainer"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("messageStyle"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("callStyle"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("videoStyle"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("friendStyle"), true);

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
