using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class DropPanel : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField]
        private TextMeshProUGUI title;
        [SerializeField]
        private TextMeshProUGUI message;
        [SerializeField]
        private TextMeshProUGUI button;
        [SerializeField]
        private RectTransform icon;

        [Header("Panels")]
        [SerializeField]
        private GameObject instruction;


        private Vector2 m_messagePosiiton;

        private RectTransform m_buttonRectT;
        private RectTransform m_messageRectT;
        private Vector2 m_cacheAnchorPosition;
        private float m_cacheButtonAnchorFromBottom;
        private bool m_isLandscape = false;


        private float m_messgeFontSize;
        private float m_titleFontSize;
        private Vector2 m_hintIconSize;

        private void Awake()
        {
            m_buttonRectT = transform.GetChild(0).GetComponent<RectTransform>();
            m_cacheButtonAnchorFromBottom = m_buttonRectT.anchoredPosition.y;

            m_messageRectT = transform.GetChild(1).GetComponent<RectTransform>();
            m_cacheAnchorPosition = m_messageRectT.anchoredPosition;

            m_hintIconSize = icon.sizeDelta;
            m_messgeFontSize = message.fontSize;
            m_titleFontSize = title.fontSize;

            m_messagePosiiton = new Vector2(transform.GetChild(1).GetComponent<RectTransform>().sizeDelta.x, transform.GetChild(1).GetComponent<RectTransform>().anchoredPosition.y);
        }

        private void OnEnable()
        {
            PopupManager.instance.HideHint();

            OrientationManager.Instance.OnOrientationChanged += OnOrientation;

            OnOrientation(OrientationManager.Instance.CurrentOrientation, (int)OrientationManager.Instance.ScreenSize.x, (int)OrientationManager.Instance.ScreenSize.y);

            ((UnityEngine.UI.Toggle)HUDManager.Instance.GetMenuFeature(HUDManager.MenuFeature._Chat)).isOn = false;

        }

        private void OnDisable()
        {
            OrientationManager.Instance.OnOrientationChanged -= OnOrientation;
        }

        private void Update()
        {
            if (AppManager.IsCreated)
            {
                if (AppManager.Instance.Data.IsMobile)
                {
                    RectTransform mobileControl = HUDManager.Instance.GetHUDControlObject("MOBILE_JOYSTICK").GetComponent<RectTransform>();

                    if (mobileControl.gameObject.activeInHierarchy)
                    {
                        if (PlayerManager.Instance.MainControlSettings.controllerType == 0)
                        {
                            m_messageRectT.anchoredPosition = new Vector2(m_cacheAnchorPosition.x, m_cacheAnchorPosition.y + mobileControl.GetChild(0).GetComponent<RectTransform>().sizeDelta.y + (!m_isLandscape ? m_buttonRectT.sizeDelta.y + 35 : 10));
                        }
                        else
                        {
                            m_messageRectT.anchoredPosition = new Vector2(m_cacheAnchorPosition.x, m_cacheAnchorPosition.y + mobileControl.GetChild(1).GetComponent<RectTransform>().sizeDelta.y + (!m_isLandscape ? m_buttonRectT.sizeDelta.y + 35 : 10));
                        }
                    }
                    else
                    {
                        m_messageRectT.anchoredPosition = m_cacheAnchorPosition;
                    }
                }
            }
        }

        public void SetStrings(bool showIntruction = true, string title = "Instruction", string message = "", string button = "Drop")
        {
            this.title.text = title;
            this.message.text = message;
            this.button.text = button;

            instruction.SetActive(showIntruction);
        }

        public void HideInstruction()
        {
            instruction.SetActive(false);
        }

        private void OnOrientation(OrientationType arg0, int arg1, int arg2)
        {
            if (AppManager.Instance.Data.IsMobile)
            {
                RectTransform mobileControl = HUDManager.Instance.GetHUDControlObject("MOBILE_JOYSTICK").GetComponent<RectTransform>();


                if (arg0.Equals(OrientationType.landscape))
                {
                    m_buttonRectT.pivot = new Vector2(0.5f, 0.0f);
                    m_buttonRectT.anchoredPosition = new Vector2(0.0f, m_cacheButtonAnchorFromBottom);

                    m_messageRectT.sizeDelta = new Vector2(m_messagePosiiton.x, m_messageRectT.sizeDelta.y);

                    icon.sizeDelta = m_hintIconSize;
                    message.fontSize = m_messgeFontSize;
                    title.fontSize = m_titleFontSize;

                    m_isLandscape = true;
                }
                else
                {

                    float aspect = arg2 / arg1;
                    m_buttonRectT.pivot = new Vector2(0.0f, 0.0f);

                    if (mobileControl.gameObject.activeInHierarchy)
                    {

                        if (PlayerManager.Instance.MainControlSettings.controllerType == 0)
                        {
                            m_buttonRectT.anchoredPosition = new Vector2(mobileControl.GetChild(0).GetComponent<RectTransform>().anchoredPosition.x, m_cacheButtonAnchorFromBottom + mobileControl.GetChild(0).GetComponent<RectTransform>().sizeDelta.y * aspect);
                        }
                        else
                        {
                            m_buttonRectT.anchoredPosition = new Vector2(mobileControl.GetChild(1).GetComponent<RectTransform>().anchoredPosition.x, m_cacheButtonAnchorFromBottom + mobileControl.GetChild(1).GetComponent<RectTransform>().sizeDelta.y * aspect);
                        }
                    }
                    else
                    {
                        m_buttonRectT.anchoredPosition = new Vector2(25.0f, m_cacheButtonAnchorFromBottom);
                    }


                    m_messageRectT.sizeDelta = new Vector2(m_messagePosiiton.x * aspect, m_messageRectT.sizeDelta.y);

                    icon.sizeDelta = new Vector2(m_hintIconSize.x * AppManager.Instance.Settings.HUDSettings.mobileIconFontScaler, m_hintIconSize.y * AppManager.Instance.Settings.HUDSettings.mobileIconFontScaler);
                    message.fontSize = m_messgeFontSize * AppManager.Instance.Settings.HUDSettings.mobileFontScaler;
                    title.fontSize = m_titleFontSize * AppManager.Instance.Settings.HUDSettings.mobileFontScaler;

                    m_isLandscape = false;
                }
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(DropPanel), true)]
        public class DropPanel_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("title"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("icon"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("message"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("button"), true);

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
