using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class LockManager : Singleton<LockManager>, IRaycaster
    {
        public static LockManager Instance
        {
            get
            {
                return ((LockManager)instance);
            }
            set
            {
                instance = value;
            }
        }

        [Header("Interaction")]
        private float interactionDistance = 5;
        private bool useLocalDistance = true;

        public bool OverrideDistance { get { return useLocalDistance; } }

        private GameObject passwordProtection;
        private TMPro.TMP_InputField passwordInput;

        [Header("Source Icons")]
        public Sprite lockedSprite;
        public Sprite unlockedSprite;

        private Lock m_lockSelected;
        public string UserCheckKey
        { 
            get
            {
                return m_userKey;
            }
        }

        private string m_userKey = "USERTYPE";

        private void Awake()
        {
            RaycastManager.Instance.Raycasters.Add(this);
        }

        private void Start()
        {
            AppManager.Instance.Assets.Add("lockedSprite", lockedSprite);
            AppManager.Instance.Assets.Add("unlockedSprite", unlockedSprite);

            PlayerControlSettings.ManagerInteraction mInteration = CoreManager.Instance.playerSettings.GetIRaycasterManager(gameObject.name);

            if (mInteration != null)
            {
                interactionDistance = mInteration.interactionDistance;
                useLocalDistance = mInteration.overrideInteraction;
                m_userKey = mInteration.userCheckKey;
            }
            else
            {
                useLocalDistance = false;
            }
        }

        public float Distance
        {
            get
            {
                float distance = 5000;

                //define camera to use
                if (!MapManager.Instance.TopDownViewActive)
                {
                    if (PlayerManager.Instance.ThirdPersonCameraActive)
                    {
                        distance = interactionDistance + Vector3.Distance(PlayerManager.Instance.GetLocalPlayer().TransformObject.position, PlayerManager.Instance.GetLocalPlayer().MainCamera.transform.position);
                    }
                    else
                    {
                        distance = interactionDistance;
                    }
                }
                else
                {
                    //cannot perform door stuff in topdown view
                    return -1;
                }

                return distance;
            }
        }

        public void RaycastHit(RaycastHit hit, out Transform hitObject)
        {
            if (hit.transform.GetComponent<Lock>())
            {
                hitObject = hit.transform;
            }
            else
            {
                hitObject = null;
            }

            if (InputManager.Instance.GetMouseButtonUp(0) && m_lockSelected == null)
            {
                //hit lock
                Lock lLock = hit.transform.GetComponent<Lock>();

                //check if raycast can continue
                if (lLock && !lLock.IgnoreRaycast)
                {
                    string user = PlayerManager.Instance.GetLocalPlayer().CustomizationData.ContainsKey(m_userKey) ? PlayerManager.Instance.GetLocalPlayer().CustomizationData[m_userKey].ToString() : "";

                    if (lLock.CanUserControlThis(user))
                    {
                        m_lockSelected = lLock;

                        //handle the UI/lock
                        if (m_lockSelected.IsLocked)
                        {
                            ShowPasswordProtection(true);
                        }
                        else
                        {
                            if (m_lockSelected.IsNetworked)
                            {
                                ShowPasswordProtection(true);
                            }
                            else
                            {
                                m_lockSelected.LockThis(true);
                                m_lockSelected = null;
                            }
                        }
                    }
                }
            }
        }

        public void RaycastMiss()
        {

        }

        public void SetManualLockSelected(Lock newLock)
        {
            if(m_lockSelected == null)
            {
                m_lockSelected = newLock;

                if (m_lockSelected.IsLocked)
                {
                    ShowPasswordProtection(true);
                }
                else
                {
                    if (m_lockSelected.IsNetworked)
                    {
                        ShowPasswordProtection(true);
                    }
                    else
                    {
                        m_lockSelected.LockThis(true);
                        m_lockSelected = null;
                    }
                }
            }
        }

        /// <summary>
        /// Local func to shwo the lock password UI
        /// </summary>
        /// <param name="show"></param>
        private void ShowPasswordProtection(bool show)
        {
            if(passwordProtection == null)
            {
                passwordProtection = HUDManager.Instance.GetHUDMessageObject("LOCKPASSWORD_MESSAGE");
                passwordInput = passwordProtection.GetComponentInChildren<TMPro.TMP_InputField>(true);
            }

            passwordInput.text = "";

            Debug.Log("LOCK: ShowPasswordProtection: " + show.ToString());

            //freeze player
            PlayerManager.Instance.GetLocalPlayer().FreezePosition = show;
            PlayerManager.Instance.GetLocalPlayer().FreezeRotation = show;
            //ignore ray casts
            RaycastManager.Instance.CastRay = !show;

            m_lockSelected.GetComponent<Collider>().enabled = false;

            passwordProtection.SetActive(show);

            if (show)
            {
                passwordInput.Select();
            }
        }

        /// <summary>
        /// Called to apply/check the lock password
        /// </summary>
        public void ApplyPassword()
        {
            if(m_lockSelected)
            {
                //if empty password, create new lock password
                if(string.IsNullOrEmpty(m_lockSelected.Password))
                {
                    m_lockSelected.LocalPlayerClicked = true;
                    m_lockSelected.Password = passwordInput.text;

                    //if password is still null, ensure lock is unlocked
                    if(!string.IsNullOrEmpty(m_lockSelected.Password))
                    {
                        m_lockSelected.UnlockThis(true);
                    }

                    Debug.Log("LOCK: ApplyPassword: Success: " + passwordInput.text);

                    m_lockSelected.PushToDataAPI();

                    ShowPasswordProtection(false);
                }
                else
                {
                    //check password against lock
                    if(passwordInput.text.Equals(m_lockSelected.Password))
                    {
                        passwordProtection.GetComponentInChildren<LockPassword>().ShowError(false);
                        m_lockSelected.LocalPlayerClicked = true;
                        m_lockSelected.UnlockThis(true);
                        ShowPasswordProtection(false);

                        Debug.Log("LOCK: ApplyPassword: Success: " + passwordInput.text);
                    }
                    else
                    {
                        passwordProtection.GetComponentInChildren<LockPassword>().ShowError(true);
                        return;
                    }
                }
            }

            //set lock gloabl vars
            m_lockSelected.GetComponent<Collider>().enabled = true;
            m_lockSelected.LocalPlayerClicked = false;
            m_lockSelected = null;
            RaycastManager.Instance.CastRay = true;
        }

        public void CancelLockPassword()
        {
            if(m_lockSelected != null)
            {
                if(m_lockSelected.OnCancel != null)
                {
                    m_lockSelected.OnCancel.Invoke();
                }

                ShowPasswordProtection(false);

                //set lock gloabl vars
                m_lockSelected.GetComponent<Collider>().enabled = true;
                m_lockSelected.LocalPlayerClicked = false;
                m_lockSelected = null;
                RaycastManager.Instance.CastRay = true;
                
            }
        }

        /// <summary>
        /// Called to network the lock on all players
        /// </summary>
        /// <param name="id"></param>
        /// <param name="state"></param>
        public void NetworkLock(string id, bool state)
        {
            Lock[] all = FindObjectsByType<Lock>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            Debug.Log("LOCK: NetworkLock: " + id + "|" + state.ToString());

            for (int i = 0; i < all.Length; i++)
            {
                //match id
                if (all[i].ID.Equals(id))
                {
                    if (!all[i].IsLocked.Equals(state))
                    {
                        all[i].IsLocked = state;
                    }

                    break;
                }
            }
        }

        [System.Serializable]
        public class LockSetting
        {
            public bool useDataAPIPassword = false;
            public Lock.LockDisplayType displayType = Lock.LockDisplayType.UIButton;
            public string password = "";
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(LockManager), true)]
        public class LockManager_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("lockedSprite"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("unlockedSprite"), true);

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