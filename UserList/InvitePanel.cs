using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace BrandLab360
{
    public class InvitePanel : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField]
        private Transform listContainer;
        [SerializeField]
        private TMP_Dropdown dropdownInviteType;
        [SerializeField]
        private GameObject inviteButton;

        [Header("Mobile Layout")]
        [SerializeField]
        private TextMeshProUGUI titleText;
        [SerializeField]
        private LayoutElement toggleHolderLayout;
        [SerializeField]
        private RectTransform footerLayout;

        private InviteManager.InviteType m_inviteType = InviteManager.InviteType.Room;


        private float m_layoutWidth;
        private RectTransform m_mainLayout;
        private float m_cacheTitleText;
        private float m_togleHolderCaheHeight;
        private float m_toggleTextHeight;

        private void Awake()
        {
            m_mainLayout = transform.GetChild(0).GetComponent<RectTransform>();
            m_layoutWidth = m_mainLayout.sizeDelta.x;

            m_cacheTitleText = titleText.fontSize;

            m_togleHolderCaheHeight = toggleHolderLayout.minHeight;
            m_toggleTextHeight = toggleHolderLayout.GetComponentInChildren<TextMeshProUGUI>(true).fontSize;
        }

        private void OnEnable()
        {
            if (inviteButton.activeInHierarchy)
            {
                inviteButton.SetActive(false);
            }

            dropdownInviteType.transform.parent.gameObject.SetActive(false);
            dropdownInviteType.value = 1;

            OrientationManager.Instance.OnOrientationChanged += OnOrientation;

            OnOrientation(OrientationManager.Instance.CurrentOrientation, (int)OrientationManager.Instance.ScreenSize.x, (int)OrientationManager.Instance.ScreenSize.y);

            SubtitleManager.Instance.ToggleButtonVisibiliy(false);
        }

        private void OnDisable()
        {
            OrientationManager.Instance.OnOrientationChanged -= OnOrientation;

            SubtitleManager.Instance.ToggleButtonVisibiliy(true);
        }

        private void Start()
        {
            StartCoroutine(WaitFrame());
        }

        private void OnOrientation(OrientationType arg0, int arg1, int arg2)
        {
            if (AppManager.Instance.Data.IsMobile)
            {
                if (arg0.Equals(OrientationType.landscape))
                {
                    m_mainLayout.anchorMin = new Vector2(0.5f, 0.0f);
                    m_mainLayout.anchorMax = new Vector2(0.5f, 1.0f);
                    m_mainLayout.anchoredPosition = Vector2.zero;
                    m_mainLayout.sizeDelta = new Vector2(m_layoutWidth, m_mainLayout.sizeDelta.y);

                    titleText.fontSize = m_cacheTitleText;

                    toggleHolderLayout.preferredHeight = toggleHolderLayout.minHeight;

                    foreach (TextMeshProUGUI ft in toggleHolderLayout.GetComponentsInChildren<TextMeshProUGUI>(true))
                    {
                        ft.fontSize = m_toggleTextHeight;
                    }

                    foreach (Selectable but in footerLayout.GetComponentsInChildren<Selectable>(true))
                    {
                        LayoutElement le = but.GetComponent<LayoutElement>();
                        le.minHeight = 52 * AppManager.Instance.Settings.HUDSettings.mobileButtonScaler;

                        TextMeshProUGUI txt = but.GetComponentInChildren<TextMeshProUGUI>(true);
                        txt.fontSize = 32;
                    }
                }
                else
                {
                    float aspect = arg2 / arg1;

                    m_mainLayout.anchorMin = new Vector2(0f, 0.0f);
                    m_mainLayout.anchorMax = new Vector2(1f, 1.0f);
                    m_mainLayout.offsetMax = new Vector2(-50, m_mainLayout.offsetMax.y);
                    m_mainLayout.offsetMin = new Vector2(50, m_mainLayout.offsetMin.y);

                    m_mainLayout.anchoredPosition = Vector2.zero;

                    titleText.fontSize = m_cacheTitleText * aspect;

                    toggleHolderLayout.minHeight = m_togleHolderCaheHeight * aspect;
                    toggleHolderLayout.preferredHeight = toggleHolderLayout.minHeight;

                    foreach (TextMeshProUGUI ft in toggleHolderLayout.GetComponentsInChildren<TextMeshProUGUI>(true))
                    {
                        ft.fontSize = m_toggleTextHeight * aspect;
                    }

                    foreach (Selectable but in footerLayout.GetComponentsInChildren<Selectable>(true))
                    {
                        LayoutElement le = but.GetComponent<LayoutElement>();
                        le.minHeight = (60 * AppManager.Instance.Settings.HUDSettings.mobileButtonScaler) * aspect;

                        TextMeshProUGUI txt = but.GetComponentInChildren<TextMeshProUGUI>(true);
                        txt.fontSize = (32 * AppManager.Instance.Settings.HUDSettings.mobileFontScaler) *aspect;
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

        private void Update()
        {
            if(listContainer.childCount > 1)
            {
                if(!inviteButton.activeInHierarchy)
                {
                    inviteButton.SetActive(true);
                }
            }
            else
            {
                if (inviteButton.activeInHierarchy)
                {
                    inviteButton.SetActive(false);
                }
            }
        }

        public void Invite()
        {
            InviteUserListButton[] temp = listContainer.GetComponentsInChildren<InviteUserListButton>();
            List<string> invitedPlayers = new List<string>();

            for(int i = 0; i < temp.Length; i++)
            {
                if(temp[i].IsSelected)
                {
                    invitedPlayers.Add(temp[i].Owner);
                }
            }

            if(invitedPlayers.Count >= 0)
            {
                InviteManager.Instance.Invite(m_inviteType, invitedPlayers);

                Cancel();
            }
        }

        public void OnInviteTypeChange(int val)
        {
            if(val > 0)
            {
                m_inviteType = InviteManager.InviteType.Room;
            }
            else
            {
                m_inviteType = InviteManager.InviteType.Video;
            }
        }

        public void Cancel()
        {
            HUDManager.Instance.GetMenuItem("Toggle_Invite").GetComponent<UnityEngine.UI.Toggle>().isOn = false;
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(InvitePanel), true)]
        public class InvitePanel_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("listContainer"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("dropdownInviteType"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("inviteButton"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("titleText"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("toggleHolderLayout"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("footerLayout"), true);

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
