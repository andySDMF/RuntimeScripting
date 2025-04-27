using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class AFKManager : Singleton<AFKManager>
    {
        [Header("Popup Message")]
        public GameObject popupMessage;

        [Header("Components")]
        public TMPro.TextMeshProUGUI popupMessageText;
        public TMPro.TextMeshProUGUI popupMessageTimer;
        public TMPro.TextMeshProUGUI warningMessage;
        public Image logo;

        private string textMessage = "ARE YOU STILL THERE?";
        private string countdownTimerMessage = "DISCONNECT IN: ";
        private string sessionEndMessage = "SESSION ENDED!";

        private float countdownMinutes = 0.5f;

        public static AFKManager Instance
        {
            get
            {
                return ((AFKManager)instance);
            }
            set
            {
                instance = value;
            }
        }

        public bool Process { get; set; }

        private float m_elaspedTime = 0.0f;
        private float m_timer = 0.0f;
        private bool m_freeze = false;
        private float m_countdown = 0.0f;
        private bool m_disconnected = false;
        private Vector3 m_cacheMousePosition;
        private bool m_wasPreviouslyFrozon;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            Process = true;
        }

        private void Start()
        {
            countdownMinutes = AppManager.Instance.Settings.AFKSettings.countdownMinutes;
            textMessage = AppManager.Instance.Settings.AFKSettings.textMessage;
            countdownTimerMessage = AppManager.Instance.Settings.AFKSettings.countdownTimerMessage;
            sessionEndMessage = AppManager.Instance.Settings.AFKSettings.sessionEndMessage;
            warningMessage.text = textMessage;

            m_timer = AppManager.Instance.Settings.AFKSettings.displayAfterMinute * 60;
            popupMessage.SetActive(false);
            popupMessageTimer.text = "";

            m_countdown = countdownMinutes * 60;

            m_cacheMousePosition = InputManager.Instance.GetMousePosition();

            if(logo.sprite == null)
            {
                logo.gameObject.SetActive(false);
            }

            StartCoroutine(Timer());

            OrientationManager.Instance.OnOrientationChanged += OnOrientation;

            OnOrientation(OrientationManager.Instance.CurrentOrientation, (int)OrientationManager.Instance.ScreenSize.x, (int)OrientationManager.Instance.ScreenSize.y);
        }

        private void OnDestroy()
        {
            OrientationManager.Instance.OnOrientationChanged -= OnOrientation;
        }


        private void OnOrientation(OrientationType arg0, int arg1, int arg2)
        {
            if (AppManager.Instance.Data.IsMobile)
            {
                if (arg0.Equals(OrientationType.landscape))
                {
                    popupMessage.transform.GetChild(0).localScale = new Vector3(1, 1, 1);
                    popupMessage.transform.GetChild(1).localScale = new Vector3(1, 1, 1);
                }
                else
                {
                    float aspect = arg2 / arg1;
                    float scaler = aspect / 4;
                    popupMessage.transform.GetChild(0).localScale = new Vector3(1 + scaler, 1 + scaler, 1);
                    popupMessage.transform.GetChild(1).localScale = new Vector3(1 + scaler, 1 + scaler, 1);
                }
            }
        }

        private IEnumerator Timer()
        {
            while (true)
            {
                if (Process)
                {
                    if (AppManager.Instance.Settings.AFKSettings.enable)
                    {
                        if (m_freeze)
                        {
                            if (!m_disconnected)
                            {
                                if (popupMessage.activeInHierarchy)
                                {
                                    float minutes = Mathf.FloorToInt(m_countdown / 60);
                                    float seconds = Mathf.FloorToInt(m_countdown % 60);
                                    popupMessageTimer.text = countdownTimerMessage + string.Format("{0:00}:{1:00}", minutes, seconds);

                                    if (m_countdown > 0)
                                    {
                                        yield return new WaitForSeconds(1.0f);
                                        m_countdown -= 1.0f;
                                    }
                                    else
                                    {
                                        //send disconnect to webclient

                                        if (AppManager.Instance.Data.RoomEstablished)
                                        {
                                            MMOManager.Instance.Disconnect();
                                        }

                                        m_disconnected = true;

                                        popupMessageText.text = sessionEndMessage;
                                        popupMessageTimer.gameObject.SetActive(false);

                                        WebclientManager.Instance.Send(JsonUtility.ToJson(new DisconnecJson()));

#if UNITY_EDITOR
                                        UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_STANDALONE
                                Application.Quit();
#endif
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (m_elaspedTime < m_timer)
                            {
                                yield return new WaitForSeconds(1.0f);
                                m_elaspedTime += 1.0f;
                            }
                            else
                            {
                                //freeze timer
                                m_freeze = true;

                                //freeze player
                                if (AppManager.Instance.Data.RoomEstablished)
                                {
                                    m_wasPreviouslyFrozon = PlayerManager.Instance.GetLocalPlayer().FreezePosition;

                                    PlayerManager.Instance.GetLocalPlayer().FreezePosition = true;
                                    PlayerManager.Instance.GetLocalPlayer().FreezeRotation = true;
                                    RaycastManager.Instance.CastRay = false;
                                }

                                //show pop up
                                popupMessageText.text = textMessage;
                                popupMessage.SetActive(true);
                            }

                            bool occupiedChair = false;

                            if (AppManager.Instance.Data.RoomEstablished && AppManager.Instance.Data.SceneSpawnLocation == null)
                            {
                                if (PlayerManager.Instance.GetLocalPlayer() != null && !string.IsNullOrEmpty(PlayerManager.Instance.GetLocalPlayer().ID))
                                {
                                    occupiedChair = ChairManager.Instance.HasPlayerOccupiedChair(PlayerManager.Instance.GetLocalPlayer().ID);
                                }
                                else
                                {
                                    occupiedChair = false;
                                }
                            }

                            if (InputManager.Instance.AnyKeyHeldDown() || (InputManager.Instance.GetMousePosition() != m_cacheMousePosition) || occupiedChair)
                            {
                                m_elaspedTime = 0.0f;
                                m_countdown = countdownMinutes * 60;
                            }

                            m_cacheMousePosition = InputManager.Instance.GetMousePosition();
                        }
                    }
                    else
                    {
                        m_elaspedTime = 0.0f;
                    }
                }

                yield return null;
            }
        }

        public void Restart()
        {
            popupMessage.SetActive(false);
            m_elaspedTime = 0.0f;
            m_countdown = countdownMinutes * 60;

            if (AppManager.Instance.Data.RoomEstablished)
            {
                if(!m_wasPreviouslyFrozon)
                {
                    PlayerManager.Instance.FreezePlayer(false);

                    if (!MMOManager.Instance.IsPlayerBusy(PlayerManager.Instance.GetLocalPlayer().ID))
                    {
                        RaycastManager.Instance.CastRay = true;
                    }
                }
            }

            m_wasPreviouslyFrozon = false;
            m_disconnected = false;
            m_freeze = false;
        }

        [System.Serializable]
        public class DisconnecJson
        {
            public bool disconnect = true;
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(AFKManager), true)]
        public class AFKManager_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("popupMessage"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("popupMessageText"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("popupMessageTimer"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("warningMessage"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("logo"), true);

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
