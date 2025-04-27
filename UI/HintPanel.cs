using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class HintPanel : MonoBehaviour
    {
        [SerializeField]
        private RectTransform hintIcon;
        [SerializeField]
        private TextMeshProUGUI titleText;
        [SerializeField]
        private TextMeshProUGUI messageText;


        private RectTransform m_rectT;
        private Vector2 m_cacheAnchorPosition;

        private float m_messgeFontSize;
        private float m_titleFontSize;
        private Vector2 m_hintIconSize;

        private void Awake()
        {
            m_rectT = GetComponent<RectTransform>();
            m_cacheAnchorPosition = m_rectT.anchoredPosition;

            if (AppManager.IsCreated)
            {
                OrientationManager.Instance.OnOrientationChanged += OnOrientation;
            }
        }

        private void Start()
        {
            StartCoroutine(Wait());
        }

        private IEnumerator Wait()
        {
            yield return new WaitForSeconds(0.5f);

            m_hintIconSize = hintIcon.sizeDelta;
            m_messgeFontSize = messageText.fontSize;
            m_titleFontSize = titleText.fontSize;

            if (AppManager.Instance.Data.IsMobile)
            {
                OnOrientation(OrientationManager.Instance.CurrentOrientation, (int)OrientationManager.Instance.ScreenSize.x, (int)OrientationManager.Instance.ScreenSize.y);
            }
        }

        private void OnDestroy()
        {
            if (AppManager.IsCreated)
            {
                OrientationManager.Instance.OnOrientationChanged -= OnOrientation;
            }
        }

        private void Update()
        {
            if(AppManager.IsCreated)
            {
                if(AppManager.Instance.Data.IsMobile && m_rectT != null)
                {
                    //need to get mobile control

                    RectTransform mobileControl = HUDManager.Instance.GetHUDControlObject("MOBILE_JOYSTICK").GetComponent<RectTransform>();

                    if (mobileControl.gameObject.activeInHierarchy)
                    {
                        if(PlayerManager.Instance.MainControlSettings.controllerType == 0)
                        {
                            m_rectT.anchoredPosition = new Vector2(m_cacheAnchorPosition.x, m_cacheAnchorPosition.y + mobileControl.GetChild(0).GetComponent<RectTransform>().sizeDelta.y);
                        }
                        else
                        {
                            m_rectT.anchoredPosition = new Vector2(m_cacheAnchorPosition.x, m_cacheAnchorPosition.y + mobileControl.GetChild(1).GetComponent<RectTransform>().sizeDelta.y);
                        }
                    }
                    else
                    {
                        m_rectT.anchoredPosition = m_cacheAnchorPosition;
                    }
                }
            }
        }

        private void OnOrientation(OrientationType arg0, int arg1, int arg2)
        {
            if (AppManager.Instance.Data.IsMobile)
            {
                if (arg0.Equals(OrientationType.landscape))
                {
                    hintIcon.sizeDelta = m_hintIconSize;
                    messageText.fontSize = m_messgeFontSize;
                    titleText.fontSize = m_titleFontSize;
                }
                else
                {
                    hintIcon.sizeDelta = new Vector2(m_hintIconSize.x * AppManager.Instance.Settings.HUDSettings.mobileIconFontScaler, m_hintIconSize.y * AppManager.Instance.Settings.HUDSettings.mobileIconFontScaler);
                    messageText.fontSize = m_messgeFontSize * AppManager.Instance.Settings.HUDSettings.mobileFontScaler;
                    titleText.fontSize = m_titleFontSize * AppManager.Instance.Settings.HUDSettings.mobileFontScaler;
                }
            }
        }

        public void Close()
        {
            PopupManager.instance.HideHint();
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(HintPanel), true)]
        public class HintPanel_Editor : BaseInspectorEditor
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

                    EditorGUILayout.LabelField("Components", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("hintIcon"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("titleText"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("messageText"), true);

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
