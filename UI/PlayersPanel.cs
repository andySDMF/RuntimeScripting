using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class PlayersPanel : MonoBehaviour
    {

        [Header("Mobile Layout")]
        [SerializeField]
        private TextMeshProUGUI titleText;
        [SerializeField]
        private RectTransform scrollViewport;
        [SerializeField]
        private LayoutElement toggleHolderLayout;

        private Vector2 m_cacheSize;
        private float m_cacheTitleText;
        private float m_togleHolderCaheHeight;
        private float m_toggleTextHeight;


        private void Start()
        {
            m_cacheSize = new Vector2(transform.GetChild(0).GetComponent<RectTransform>().sizeDelta.x, transform.GetChild(0).GetComponent<RectTransform>().sizeDelta.y);
            m_cacheTitleText = titleText.fontSize;
            m_togleHolderCaheHeight = toggleHolderLayout.minHeight;
            m_toggleTextHeight = toggleHolderLayout.GetComponentInChildren<TextMeshProUGUI>(true).fontSize;

            StartCoroutine(WaitFrame());

            if (AppManager.Instance.Data.IsMobile || AppManager.Instance.Settings.HUDSettings.useMobileToolsForDesktop)
            {
                transform.GetChild(0).GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 25);
            }

            OrientationManager.Instance.OnOrientationChanged += OnOrientation;

            OnOrientation(OrientationManager.Instance.CurrentOrientation, (int)OrientationManager.Instance.ScreenSize.x, (int)OrientationManager.Instance.ScreenSize.y);
        }

        private void OnDestroy()
        {
            if (!AppManager.IsCreated) return;

            OrientationManager.Instance.OnOrientationChanged -= OnOrientation;
        }

        private void Update()
        {
            if (ItemManager.Instance.IsHolding || ProductManager.Instance.isHolding || ChairManager.Instance.HasPlayerOccupiedChair(PlayerManager.Instance.GetLocalPlayer().ID)
                || ConfiguratorManager.instance.ActiveRTEObject != null || !HUDManager.Instance.NavigationHUDVisibility)
            {
                //close the emote HUD via the toggle
                HUDManager.Instance.GetMenuItem("Toggle_Players").GetComponent<UnityEngine.UI.Toggle>().isOn = false;
            }

            if(AppManager.Instance.Data.IsMobile)
            {
                RectTransform mobileControl = HUDManager.Instance.GetHUDControlObject("MOBILE_JOYSTICK").GetComponent<RectTransform>();

                if(OrientationManager.Instance.CurrentOrientation.Equals(OrientationType.landscape))
                {

                    transform.GetChild(0).GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 25);
                }
                else
                {
                    if (mobileControl.gameObject.activeInHierarchy)
                    {
                        if (PlayerManager.Instance.MainControlSettings.controllerType == 0)
                        {
                            transform.GetChild(0).GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 25 + (mobileControl.GetChild(0).GetComponent<RectTransform>().sizeDelta.y + 10));
                        }
                        else
                        {
                            transform.GetChild(0).GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 25 + (mobileControl.GetChild(1).GetComponent<RectTransform>().sizeDelta.y + 10));
                        }
                    }
                    else
                    {
                        transform.GetChild(0).GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 25 + (MainHUDMenuPanel.Instance.MobileButton.sizeDelta.y + 10));
                    }
                }
            }
        }

        private IEnumerator WaitFrame()
        {
            yield return new WaitForEndOfFrame();

            Toggle[] togs = GetComponentsInChildren<Toggle>();

            for (int i = 0; i < togs.Length; i++)
            {
                if (togs[i].transform.parent.name != "Layout_Tabs") continue;

                if (togs[i].name.Contains("People"))
                {
                    togs[i].isOn = true;
                    break;
                }
            }
        }

        private void OnOrientation(OrientationType arg0, int arg1, int arg2)
        {
            if (AppManager.Instance.Data.IsMobile)
            {
                if (arg0.Equals(OrientationType.landscape))
                {
                    titleText.fontSize = m_cacheTitleText;

                    transform.GetChild(0).GetComponent<RectTransform>().sizeDelta = new Vector2(m_cacheSize.x, m_cacheSize.y);

                    toggleHolderLayout.minHeight = m_togleHolderCaheHeight;
                    toggleHolderLayout.preferredHeight = toggleHolderLayout.minHeight;


                    foreach (TextMeshProUGUI ft in toggleHolderLayout.GetComponentsInChildren<TextMeshProUGUI>(true))
                    {
                        ft.fontSize = m_toggleTextHeight;
                    }
                }
                else
                {
                    float aspect = arg2 / arg1;

                    titleText.fontSize = m_cacheTitleText * aspect;

                    transform.GetChild(0).GetComponent<RectTransform>().sizeDelta = new Vector2(m_cacheSize.x * aspect, m_cacheSize.y * aspect);

                    toggleHolderLayout.minHeight = m_togleHolderCaheHeight * aspect;
                    toggleHolderLayout.preferredHeight = toggleHolderLayout.minHeight;

                    foreach (TextMeshProUGUI ft in toggleHolderLayout.GetComponentsInChildren<TextMeshProUGUI>(true))
                    {
                        ft.fontSize = m_toggleTextHeight * aspect;
                    }
                }
            }
        }

        public void Close()
        {
            HUDManager.Instance.GetMenuItem("Toggle_Players").GetComponent<UnityEngine.UI.Toggle>().isOn = false;
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(PlayersPanel), true)]
        public class PlayersPanel_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("titleText"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("scrollViewport"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("toggleHolderLayout"), true);

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
