using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.AI;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class MMOPlayer : MonoBehaviour
    {
        [HideInInspector]
        public MMOView view;

        [Header("Avatar")]
        public Animator animator;

        [Header("Player Name")]
        public TMP_Text LabelNameFront;

        [Header("Visual Locator")]
        [SerializeField]
        public GameObject locator;

        [Header("Chat Message")]
        public CanvasGroup chatMessageCanvas;
        public TMP_Text chatMessageLabel;

        private IPlayer playerController;
        [HideInInspector]
        public Transform animatorHolder;
        private float chatMessageDisplayTimer = 0.0f;
        private bool chatTimerOn = false;
        private bool hasInit = false;

        //animation blending
        private float m_strifeVal = 0.0f;
        private float m_turnVal = 0.0f;
        private float m_moveVal = 0.0f;

        private bool m_playerFound = false;

        /// <summary>
        /// freeze network player position
        /// </summary>
        public bool FreezePosition
        {
            get;
            set;
        }

        /// <summary>
        /// freeze network player rotatrion
        /// </summary>
        public bool FreezeRotation
        {
            get;
            set;
        }

        /// <summary>
        /// The sitting animation that should be played
        /// </summary>
        public string SittingAnimation
        {
            get;
            set;
        }

        /// <summary>
        /// Returns nav mesh agent if attached
        /// </summary>
        public NavMeshAgent NavMeshAgentUsed
        {
            get
            {
                return GetComponent<NavMeshAgent>();
            }
        }

        /// <summary>
        /// returns the ID of this networked player
        /// </summary>
        public string ID
        {
            get
            {
                if (view == null) return "null";

                return view.ID;
            }
        }

        public string Nickname
        {
            get
            {
                if (view == null) return "";

                return view.Nickname;
            }
        }

        public int Actor
        {
            get
            {
                if (view == null) return -1;

                return view.Actor;
            }
        }

        public MMOView View
        {
            get
            {
                return view;
            }
        }


        private void Awake()
        {
            view = GetComponent<MMOView>();
        }

        private void Start()
        {
            Initialize();

            PlayerManager.OnUpdate += OnThisUpdate;
        }

        private void OnDestroy()
        {
            PlayerManager.OnUpdate -= OnThisUpdate;
        }

        private void OnThisUpdate()
        {
            if (animator != null)
            {
                animator.transform.localPosition = Vector3.zero;
                if (!FreezeRotation) animator.transform.localEulerAngles = Vector3.zero;
            }

            if(!view.IsMine)
            {
                chatMessageCanvas.transform.LookAt(PlayerManager.Instance.GetLocalPlayer().MainCamera.transform);
                LabelNameFront.transform.parent.LookAt(PlayerManager.Instance.GetLocalPlayer().MainCamera.transform);

                if (chatTimerOn)
                {
                    if (chatMessageCanvas.alpha < 1.0f)
                    {
                        chatMessageCanvas.alpha += Time.deltaTime;
                    }

                    if (chatMessageDisplayTimer < 5.0f)
                    {
                        chatMessageDisplayTimer += Time.deltaTime;
                    }
                    else
                    {
                        chatTimerOn = false;
                    }
                }
                else
                {
                    if (chatMessageCanvas.alpha > 0.0f)
                    {
                        chatMessageCanvas.alpha -= Time.deltaTime;
                    }
                }
            }

            if (!m_playerFound && !view.IsMine)
            {
                IPlayer iplayer = MMOManager.Instance.GetPlayerByUserID(ID);

                if (iplayer != null)
                {
                    bool found = false;

                    if (AppManager.Instance.Data.FixedAvatarUsed)
                    {
                        found = MMOManager.Instance.PlayerHasProperty(iplayer, "FIXEDAVATAR");
                    }
                    else
                    {
                        found = MMOManager.Instance.PlayerHasProperty(iplayer, "CUSTOMIZATIONDATA");
                    }

                    if (found)
                    {
                        m_playerFound = true;
                        AvatarManager.Instance.CustomiseNetworkPlayer(iplayer, MMOManager.Instance.GetPlayerProperties(iplayer));
                        PlayerManager.Instance.OnPlayerEnteredRoom(iplayer);
                    }
                }
            }
        }

        /// <summary>
        /// Initialize the network player based on whether it is a remote or local user
        /// </summary>
        private void Initialize()
        {
            playerController = GetComponent<IPlayer>();

            if (view.IsMine || AppManager.Instance.Data.Mode == MultiplayerMode.Offline)
            {
                //animator.gameObject.SetActive(false);
                LabelNameFront.transform.parent.gameObject.SetActive(false);
                playerController.IsLocal = true;

                playerController.MainCamera.gameObject.SetActive(true);

                gameObject.name = gameObject.name + "_" + "You";
            }
            else
            {
                playerController.MainCamera.gameObject.SetActive(false);
                Destroy(playerController.MainCamera.GetComponent<AudioListener>()); //.enabled = false;
                playerController.IsLocal = false;
                playerController.ToggleCanMove(false);

                if (GetComponent<Collider>())
                {
                    GetComponent<Collider>().enabled = false;
                }

                ChangeAvatarLayer(0);

                if (GetComponent<Rigidbody>())
                {
                    Destroy(GetComponent<Rigidbody>());

                    //GetComponent<Rigidbody>().isKinematic = true;
                    //GetComponent<Rigidbody>().interpolation = RigidbodyInterpolation.None;
                }

                SetNameLabel(PlayerManager.Instance.GetPlayerName(view.Nickname));
                gameObject.name = gameObject.name + "_" + ID;

                locator.GetComponentInChildren<UnityEngine.UI.Image>().color = CoreManager.Instance.playerSettings.pointerColor;

                IPlayer iplayer = MMOManager.Instance.GetPlayerByUserID(ID);

                if (iplayer != null)
                {
                    Debug.Log("player found");

                    bool found = false;

                    if (AppManager.Instance.Data.FixedAvatarUsed)
                    {
                        found = MMOManager.Instance.PlayerHasProperty(iplayer, "FIXEDAVATAR");
                    }
                    else
                    {
                        found = MMOManager.Instance.PlayerHasProperty(iplayer, "CUSTOMIZATIONDATA");
                    }

                    if (found)
                    {
                        m_playerFound = true;
                        AvatarManager.Instance.CustomiseNetworkPlayer(iplayer, MMOManager.Instance.GetPlayerProperties(iplayer));
                        PlayerManager.Instance.OnPlayerEnteredRoom(iplayer);
                    }
                }
            }

            if (animator != null)
            {
                animatorHolder = animator.transform.parent;
            }

            hasInit = true;
        }

        public void RecieveChatMessage(string message)
        {
            chatMessageLabel.text = message;
            chatMessageDisplayTimer = 0.0f;
            chatTimerOn = true;
        }

        public void ChangeAvatarLayer(int layer)
        {
            if (animator == null) return;

            foreach (Transform trans in animator.transform.parent.GetComponentsInChildren<Transform>(true))
            {
                trans.gameObject.layer = layer;
            }
        }

        public void SetPlayerToIdle()
        {
            GetComponent<MMOPlayerSync>().ResetAnimation();
        }

        public void Walk(bool state, int ani = -1)
        {
            if (hasInit && animator != null)
            {
                if (state)
                {
                    float speed = CoreManager.Instance.playerSettings.walkSpeed;

                    if (ani.Equals(10))
                    {
                        animator.SetBool("Emote", false);
                        animator.SetBool("Turning", false);
                        animator.SetBool("Strifing", false);
                        animator.SetBool("Moved", false);

                        animator.SetBool("Swimming", true);
                        animator.SetFloat("SwimVal", -0.01f);
                    }
                    else if(ani.Equals(11))
                    {
                        animator.SetBool("Emote", false);
                        animator.SetBool("Turning", false);
                        animator.SetBool("Strifing", false);
                        animator.SetBool("Moved", false);

                        animator.SetBool("Swimming", true);
                        animator.SetFloat("SwimVal", 0.01f);
                    }
                    else if (ani.Equals(-1))
                    {
                        animator.SetBool("Turning", false);
                        animator.SetBool("Strifing", false);
                        animator.SetBool("Moved", false);
                        animator.SetBool("Swimming", false);

                        m_strifeVal = 0.0f;
                        m_turnVal = 0.0f;
                        m_moveVal = 0.0f;
                    }
                    else if (ani.Equals(0) || ani.Equals(1))
                    {
                        animator.SetBool("Emote", false);
                        animator.SetBool("Turning", false);
                        animator.SetBool("Strifing", false);
                        animator.SetBool("Moved", true);
                        animator.SetBool("Swimming", false);

                        m_strifeVal = 0.0f;
                        m_turnVal = 0.0f;

                        if (ani.Equals(1) && m_moveVal <= 0.01f)
                        {
                            m_moveVal += Time.deltaTime * speed;
                        }
                        else if (m_moveVal >= 0f)
                        {
                            m_moveVal -= Time.deltaTime * speed;
                        }

                        m_moveVal = Mathf.Clamp(m_moveVal, 0.0f, 0.01f);

                        animator.SetFloat("MovedVal", m_moveVal, 0.1f, Time.deltaTime * speed);
                    }
                    else if (ani.Equals(2) || ani.Equals(3))
                    {
                        animator.SetBool("Emote", false);
                        animator.SetBool("Turning", false);
                        animator.SetBool("Moved", false);
                        animator.SetBool("Strifing", true);
                        animator.SetBool("Swimming", false);

                        m_turnVal = 0.0f;
                        m_moveVal = 0.0f;

                        if (ani.Equals(3) && m_strifeVal <= 0.01f)
                        {
                            m_strifeVal += Time.deltaTime * speed;
                        }
                        else if (m_strifeVal >= -0.01f)
                        {
                            m_strifeVal -= Time.deltaTime * speed;
                        }

                        m_strifeVal = Mathf.Clamp(m_strifeVal, -0.01f, 0.01f);

                        animator.SetFloat("StrifeVal", m_strifeVal, 0.5f, Time.deltaTime * speed);
                    }
                    else if (ani.Equals(4) || ani.Equals(5))
                    {
                        animator.SetBool("Emote", false);
                        animator.SetBool("Moved", false);
                        animator.SetBool("Strifing", false);
                        animator.SetBool("Turning", true);
                        animator.SetBool("Swimming", false);

                        m_strifeVal = 0.0f;
                        m_moveVal = 0.0f;

                        if (ani.Equals(5) && m_turnVal <= 0.01f)
                        {
                            m_turnVal += Time.deltaTime * speed;
                        }
                        else if (m_strifeVal >= -0.01f)
                        {
                            m_turnVal -= Time.deltaTime * speed;
                        }

                        m_turnVal = Mathf.Clamp(m_turnVal, -0.01f, 0.01f);

                        animator.SetFloat("TurnVal", m_strifeVal, 0.5f, Time.deltaTime * speed);
                    }
                }
                else
                {
                    if (ani >= 10)
                    {
                        animator.SetBool("Turning", false);
                        animator.SetBool("Strifing", false);
                        animator.SetBool("Moved", false);

                        animator.SetBool("Swimming", true);

                        if (ani.Equals(10))
                        {
                            animator.SetFloat("SwimVal", -0.01f);
                        }
                        else if (ani.Equals(11))
                        {
                            animator.SetFloat("SwimVal", 0.01f);
                        }
                    }
                    else
                    {
                        animator.SetBool("Turning", false);
                        animator.SetBool("Strifing", false);
                        animator.SetBool("Moved", false);
                        animator.SetBool("Swimming", false);
                    }

                    m_strifeVal = 0.0f;
                    m_turnVal = 0.0f;
                    m_moveVal = 0.0f;
                }
            }
        }

        /// <summary>
        /// Rotates the players avatar based on walking direction
        /// </summary>
        /// <param name="forawrd"></param>
        /// <param name="backward"></param>
        public void RotateAvatar(bool foward, bool backward)
        {
            if (!view.IsMine && !FreezeRotation)
            {
                if (CoreManager.Instance.playerSettings.avatarBackwardsBehaviour.Equals(AvatarBackwardBehaviour.Rotate))
                {
                    if (foward)
                    {
                        animatorHolder.localEulerAngles = Vector3.zero;
                    }
                    else if (backward)
                    {
                        animatorHolder.localEulerAngles = new Vector3(0, 180, 0);
                    }
                    else
                    {
                        animatorHolder.localEulerAngles = Vector3.zero;
                    }
                }
            }
        }

        /// <summary>
        /// Set the user's name label
        /// </summary>
        /// <param name="username">the user name to display on their label</param>
        public void SetNameLabel(string username)
        {
            LabelNameFront.text = username;
        }

        /// <summary>
        /// Remove the photon specific components for local user
        /// </summary>
        public void RemovePhotonComponents()
        {
            view.RemoveNetworkComponents();
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(MMOPlayer), true)]
        public class MMOPlayer_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("animator"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("LabelNameFront"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("locator"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("chatMessageCanvas"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("chatMessageLabel"), true);

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
