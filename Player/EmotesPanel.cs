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
    public class EmotesPanel : MonoBehaviour
    {
        [Header("Actions")]
        [SerializeField]
        private GameObject actionPrefab;

        [SerializeField]
        private Transform actionContainer;

        [Header("Emotes")]
        [SerializeField]
        private GameObject emotesPrefab;

        [SerializeField]
        private Transform emotesContainer;

        [Header("Mobile Layout")]
        [SerializeField]
        private TextMeshProUGUI titleText;
        [SerializeField]
        private LayoutElement toggleHolderLayout;


        private float m_scaler;
        private float m_cacheTitleText;
        private Vector2 m_cacheSize;
        private float m_togleHolderCaheHeight;
        private float m_toggleTextHeight;

        private void Start()
        {
            if(AppManager.IsCreated)
            {
                m_cacheTitleText = titleText.fontSize;
                m_cacheSize = new Vector2(transform.GetChild(0).GetComponent<RectTransform>().sizeDelta.x, actionContainer.GetComponentInParent<ScrollRect>(true).GetComponent<LayoutElement>().minHeight);
                m_togleHolderCaheHeight = toggleHolderLayout.minHeight;
                m_toggleTextHeight = toggleHolderLayout.GetComponentInChildren<TextMeshProUGUI>(true).fontSize;

                for (int i = 0; i < AppManager.Instance.Settings.playerSettings.emoteActions.Count; i++)
                {
                    GameObject go = Instantiate(actionPrefab, Vector3.zero, Quaternion.identity, actionContainer);
                    go.transform.localScale = Vector3.one;
                    go.name = "Cell_Action_" + AppManager.Instance.Settings.playerSettings.emoteActions[i].name;
                    go.SetActive(true);
                    go.transform.GetChild(0).GetComponent<Image>().sprite = AppManager.Instance.Settings.playerSettings.emoteActions[i].icon;
                    go.transform.GetChild(0).GetComponent<Image>().SetNativeSize();

                    int n = AppManager.Instance.Settings.playerSettings.emoteActions[i].id;
                    go.GetComponent<Button>().onClick.AddListener(() => { PlayAction(n); });
                }

                for (int i = 0; i < AppManager.Instance.Settings.playerSettings.emoteIcons.Count; i++)
                {
                    GameObject go = Instantiate(emotesPrefab, Vector3.zero, Quaternion.identity, emotesContainer);
                    go.transform.localScale = Vector3.one;
                    go.name = "Cell_Emoji_" + AppManager.Instance.Settings.playerSettings.emoteIcons[i].name;
                    go.SetActive(true);
                    go.transform.GetChild(0).GetComponent<Image>().sprite = AppManager.Instance.Settings.playerSettings.emoteIcons[i].icon;
                    go.transform.GetChild(0).GetComponent<Image>().SetNativeSize();

                    int n = AppManager.Instance.Settings.playerSettings.emoteIcons[i].id;
                    go.GetComponent<Button>().onClick.AddListener(() => { PlayEmote(n); });
                }

                m_scaler = AppManager.Instance.Settings.HUDSettings.mobileButtonScaler;

                Toggle[] togs = GetComponentsInChildren<Toggle>();

                for (int i = 0; i < togs.Length; i++)
                {
                    if (togs[i].name.Contains("Actions"))
                    {
                        togs[i].isOn = true;
                        break;
                    }
                }

                if (AppManager.Instance.Data.IsMobile || AppManager.Instance.Settings.HUDSettings.useMobileToolsForDesktop)
                {
                    transform.GetChild(0).GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 25);
                }

                OrientationManager.Instance.OnOrientationChanged += OnOrientation;

                OnOrientation(OrientationManager.Instance.CurrentOrientation, (int)OrientationManager.Instance.ScreenSize.x, (int)OrientationManager.Instance.ScreenSize.y);

            }
        }

        private void OnDestroy()
        {
            if (!AppManager.IsCreated) return;

            OrientationManager.Instance.OnOrientationChanged -= OnOrientation;
        }

        private void OnOrientation(OrientationType arg0, int arg1, int arg2)
        {
            if (AppManager.Instance.Data.IsMobile)
            {
                Vector2 cellsize = new Vector2(52 * m_scaler, 52 * m_scaler);

                if (arg0.Equals(OrientationType.landscape))
                {
                    emotesContainer.GetComponent<GridLayoutGroup>().cellSize = cellsize;
                    actionContainer.GetComponent<GridLayoutGroup>().cellSize = cellsize;

                    emotesContainer.GetComponentInParent<ScrollRect>(true).GetComponent<LayoutElement>().minHeight = m_cacheSize.y;
                    actionContainer.GetComponentInParent<ScrollRect>(true).GetComponent<LayoutElement>().minHeight = m_cacheSize.y;

                    emotesContainer.GetComponent<LayoutElement>().minHeight = m_cacheSize.y;
                    actionContainer.GetComponent<LayoutElement>().minHeight = m_cacheSize.y;

                    transform.GetChild(0).GetComponent<RectTransform>().sizeDelta = new Vector2(m_cacheSize.x, 0);

                    titleText.fontSize = m_cacheTitleText;

                    toggleHolderLayout.minHeight = m_togleHolderCaheHeight;
                    toggleHolderLayout.preferredHeight = toggleHolderLayout.minHeight;

                    foreach(TextMeshProUGUI ft in toggleHolderLayout.GetComponentsInChildren<TextMeshProUGUI>(true))
                    {
                        ft.fontSize = m_toggleTextHeight;
                    }


                    for (int i = 1; i < actionContainer.childCount; i++)
                    {
                        actionContainer.GetChild(i).GetChild(0).GetComponentInChildren<Image>(true).SetNativeSize();
                    }

                    for (int i = 1; i < emotesContainer.childCount; i++)
                    {
                        emotesContainer.GetChild(i).GetChild(0).GetComponentInChildren<Image>(true).SetNativeSize();
                    }
                }
                else
                {
                    float aspect = arg2 / arg1;
                    emotesContainer.GetComponent<GridLayoutGroup>().cellSize = new Vector2(cellsize.x * aspect, cellsize.y * aspect);
                    actionContainer.GetComponent<GridLayoutGroup>().cellSize = new Vector2(cellsize.x * aspect, cellsize.y * aspect);
                    titleText.fontSize = m_cacheTitleText * aspect;

                    emotesContainer.GetComponentInParent<ScrollRect>(true).GetComponent<LayoutElement>().minHeight = m_cacheSize.y * aspect;
                    actionContainer.GetComponentInParent<ScrollRect>(true).GetComponent<LayoutElement>().minHeight = m_cacheSize.y * aspect;

                    transform.GetChild(0).GetComponent<RectTransform>().sizeDelta = new Vector2(m_cacheSize.x * aspect, 0);

                    emotesContainer.GetComponent<LayoutElement>().minHeight = m_cacheSize.y * aspect;
                    actionContainer.GetComponent<LayoutElement>().minHeight = m_cacheSize.y * aspect;

                    toggleHolderLayout.minHeight = m_togleHolderCaheHeight * aspect;
                    toggleHolderLayout.preferredHeight = toggleHolderLayout.minHeight;

                    foreach (TextMeshProUGUI ft in toggleHolderLayout.GetComponentsInChildren<TextMeshProUGUI>(true))
                    {
                        ft.fontSize = m_toggleTextHeight * aspect;
                    }

                    for (int i = 1; i < actionContainer.childCount; i++)
                    {
                        RectTransform rt = actionContainer.GetChild(i).GetChild(0).GetComponentInChildren<RectTransform>(true);

                        if(rt != null)
                        {
                            rt.sizeDelta = new Vector2(rt.sizeDelta.x * aspect, rt.sizeDelta.y * aspect);
                        }
                       
                    }

                    for (int i = 1; i < emotesContainer.childCount; i++)
                    {
                        RectTransform rt = emotesContainer.GetChild(i).GetChild(0).GetComponentInChildren<RectTransform>(true);

                        if (rt != null)
                        {
                            rt.sizeDelta = new Vector2(rt.sizeDelta.x * aspect, rt.sizeDelta.y * aspect);
                        }
                    }
                }
            }
        }

        private void Update()
        {
            if (ItemManager.Instance.IsHolding || ProductManager.Instance.isHolding || ChairManager.Instance.HasPlayerOccupiedChair(PlayerManager.Instance.GetLocalPlayer().ID)
                || ConfiguratorManager.instance.ActiveRTEObject != null || !HUDManager.Instance.NavigationHUDVisibility)
            {
                //close the emote HUD via the toggle
                HUDManager.Instance.GetMenuItem("Toggle_Emotes").GetComponent<UnityEngine.UI.Toggle>().isOn = false;
            }

            if (AppManager.Instance.Data.IsMobile)
            {
                RectTransform mobileControl = HUDManager.Instance.GetHUDControlObject("MOBILE_JOYSTICK").GetComponent<RectTransform>();

                if (OrientationManager.Instance.CurrentOrientation.Equals(OrientationType.landscape))
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
                if (togs[i].name.Contains("Actions"))
                {
                    togs[i].isOn = true;
                    break;
                }
            }
        }

        public void Close()
        {
            HUDManager.Instance.GetMenuItem("Toggle_Emotes").GetComponent<UnityEngine.UI.Toggle>().isOn = false;
        }

        private void PlayAction(int n)
        {
            PlayerManager.Instance.PlayEmote(n);
            //close the emote HUD via the toggle
            HUDManager.Instance.GetMenuItem("Toggle_Emotes").GetComponent<UnityEngine.UI.Toggle>().isOn = false;
        }

        private void PlayEmote(int n)
        {
            PlayerManager.Instance.PlayEmoji(n);
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(EmotesPanel), true)]
        public class EmotesPanel_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("actionPrefab"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("actionContainer"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("emotesPrefab"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("emotesContainer"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("titleText"), true);
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
