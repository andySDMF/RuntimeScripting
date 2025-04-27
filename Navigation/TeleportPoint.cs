using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.AI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class TeleportPoint : UniqueID, ITeleport
    {
        [SerializeField]
        private TeleportIOObject settings = new TeleportIOObject();

        [Header("Target")]
        [SerializeField]
        private Transform targetTransform;

        private Vector3 targetPosition = Vector3.zero;
        private bool m_navMeshPathUsed = false;

        private void Start()
        {
            if (!AppManager.IsCreated) return;

            if(!AppManager.Instance.Instances.ignoreIObjectSettings)
            {
                //need to get the settings from the instances script then update the settings
                foreach (AppInstances.IOObjectTeleportHandler setting in AppManager.Instance.Instances.ioTeleportObjects)
                {
                    if (setting.referenceID.Equals(GetRawID()))
                    {
                        ApplySettings(setting.settings);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Move player to this transforms position (param is ignored)
        /// </summary>
        /// <param name="v"></param>
        public void Teleport(Vector3 v)
        {
            StopAllCoroutines();

            bool jump = false;

            targetPosition = v;

            switch (settings.mode)
            {
                case TeleportMode.Tween:
                    if (CoreManager.Instance.playerSettings.createNavMeshAgent)
                    {
                        //check if position is on navmesh
                        NavMeshHit hit;

                        if (NavMesh.SamplePosition(targetPosition, out hit, 1f, NavMesh.AllAreas))
                        {
                            if (PlayerManager.Instance.GetLocalPlayer().NavMeshAgentScript != null)
                            {
                                PlayerManager.Instance.GetLocalPlayer().NavMeshAgentScript.isStopped = false;
                                PlayerManager.Instance.GetLocalPlayer().NavMeshAgentScript.destination = targetPosition;
                                m_navMeshPathUsed = true;
                            }
                        }

                        StartCoroutine(Tween());
                    }
                    else
                    {
                        m_navMeshPathUsed = false;
                        StartCoroutine(Tween());
                    }
                    break;
                default:
                    jump = true;
                    Jump();
                    break;
            }


            //sync position/ rotation
            Quaternion lookRotation = Quaternion.LookRotation(targetPosition - PlayerManager.Instance.GetLocalPlayer().TransformObject.position);
            
            if(AppManager.Instance.Data.Mode.Equals(MultiplayerMode.Online))
            {
                PlayerManager.Instance.GetLocalPlayer().TransformObject.GetComponent<MMOPlayerSync>().SendPositionAndRotationAtFrequencyNow(targetPosition, lookRotation, jump);
            }

            // If teleported during topdown mode, then exit topdown mode
            if (MapManager.Instance.TopDownViewActive)
            {
                HUDManager.Instance.TopdownToggle.isOn = false;
            }

            AnalyticsManager.Instance.PostAnalyticsEvent(EventCategory.Location, EventAction.Enter, AnalyticReference);

        }

        /// <summary>
        /// Animate player to new position over a duration of time
        /// </summary>
        /// <returns></returns>
        private IEnumerator Tween()
        {
            PlayerManager.Instance.FreezePlayer(true);
            PlayerManager.Instance.GetLocalPlayer().ThirdPerson.GetComponent<CameraThirdPerson>().ResetActiveCameraControl();
            PlayerManager.Instance.GetLocalPlayer().OverrideAnimationHandler = true;
            PlayerManager.Instance.GetLocalPlayer().Animation.SetBool("Moved", true);
            PlayerManager.Instance.GetLocalPlayer().Animation.SetFloat("MovedVal", 0);

            if (m_navMeshPathUsed)
            {
                while (!PlayerManager.Instance.GetLocalPlayer().NavMeshAgentScript.hasPath)
                {
                    if (CheckInput())
                    {
                        break;
                    }

                    yield return null;
                }

                while (PlayerManager.Instance.GetLocalPlayer().NavMeshAgentScript.remainingDistance >= 0.1f)
                {
                    if (CheckInput())
                    {
                        break;
                    }

                    //if orientation ensure the player rotation matches this transform
                    Quaternion lookRotation = Quaternion.LookRotation(targetPosition - PlayerManager.Instance.GetLocalPlayer().TransformObject.position);
                    PlayerManager.Instance.GetLocalPlayer().TransformObject.rotation = Quaternion.Slerp(PlayerManager.Instance.GetLocalPlayer().TransformObject.rotation, lookRotation, Time.deltaTime);

                    yield return null;
                }

                PlayerManager.Instance.GetLocalPlayer().NavMeshAgentScript.isStopped = true;
            }
            else
            {
                while (Vector3.Distance(PlayerManager.Instance.GetLocalPlayer().TransformObject.position, targetPosition) > 0.1f)
                {
                    //if input read then release from loop
                    if (CheckInput())
                    {
                        break;
                    }

                    PlayerManager.Instance.GetLocalPlayer().TransformObject.position = Vector3.MoveTowards(PlayerManager.Instance.GetLocalPlayer().TransformObject.position, targetPosition, Time.deltaTime * CoreManager.Instance.playerSettings.walkSpeed);
    
                    //if orientation ensure the player rotation matches this transform
                    Quaternion lookRotation = Quaternion.LookRotation(targetPosition - PlayerManager.Instance.GetLocalPlayer().TransformObject.position);
                    PlayerManager.Instance.GetLocalPlayer().TransformObject.rotation = Quaternion.Slerp(PlayerManager.Instance.GetLocalPlayer().TransformObject.rotation, lookRotation, Time.deltaTime);

                    yield return null;
                }
            }

            Vector2 vec = new Vector3(PlayerManager.Instance.GetLocalPlayer().MainCamera.GetComponent<Transform>().localEulerAngles.x * -1, PlayerManager.Instance.GetLocalPlayer().TransformObject.localEulerAngles.y);
            PlayerManager.Instance.GetLocalPlayer().TargetCameraRotation = vec;
            PlayerManager.Instance.GetLocalPlayer().Animation.SetBool("Moved", false);
            PlayerManager.Instance.GetLocalPlayer().OverrideAnimationHandler = false;
            PlayerManager.Instance.FreezePlayer(false);

            PlayerManager.Instance.IsTeleporting = false;
        }

        /// <summary>
        /// Check any input
        /// </summary>
        /// <returns></returns>
        private bool CheckInput()
        {
            //if input read then release from loop
            if (InputManager.Instance.AnyKeyHeldDown() || InputManager.Instance.AnyMouseButtonDown())
            {
                if (CoreManager.Instance.playerSettings.createNavMeshAgent)
                {
                    PlayerManager.Instance.GetLocalPlayer().NavMeshAgentScript.isStopped = true;
                }

                PlayerManager.Instance.FreezePlayer(false);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Instantly jump player to this transforms position
        /// </summary>
        public void Jump()
        {
            if (targetTransform != null) 
            { 
                targetPosition = targetTransform.position;
            }

            PlayerManager.Instance.GetLocalPlayer().TransformObject.position = targetPosition;

            //if orienation ensure the player rotation matches this transform
            if (settings.setOrientation)
            {
                PlayerManager.Instance.GetLocalPlayer().TransformObject.eulerAngles = new Vector3(0, transform.eulerAngles.y, 0);
            }

            //ensure the angle of the players camera is set to zero if true
            if (settings.zeroCameraRotation)
            {
                PlayerManager.Instance.GetLocalPlayer().MainCamera.transform.eulerAngles = new Vector3(0, PlayerManager.Instance.GetLocalPlayer().MainCamera.transform.eulerAngles.y, 0);
                PlayerManager.Instance.GetLocalPlayer().TargetCameraRotation = PlayerManager.Instance.GetLocalPlayer().MainCamera.transform.eulerAngles;
            }
        }

        /// <summary>
        /// Tween function to smoothly interpolate movement
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private float TweenOutCubic(float start, float end, float value)
        {
            value--;
            end -= start;
            return end * (value * value * value + 1) + start;
        }

        [System.Serializable]
        public class TeleportIOObject : IObjectSetting
        {
            public TeleportMode mode = TeleportMode.Jump;
            public bool setOrientation = false;
            public bool zeroCameraRotation = false;
        }

        public override IObjectSetting GetSettings(bool remove = false)
        {
            if(!remove)
            {
                IObjectSetting baseSettings = base.GetSettings();
                settings.adminOnly = baseSettings.adminOnly;
                settings.prefix = baseSettings.prefix;
                settings.controlledByUserType = baseSettings.controlledByUserType;
                settings.userTypes = baseSettings.userTypes;

                settings.GO = gameObject.name;
            }

            settings.ID = id;
            return settings;
        }

        protected override void ApplySettings(IObjectSetting settings)
        {
            if (settings == null) return;

            base.ApplySettings(settings);

            this.settings.mode = ((TeleportIOObject)settings).mode;
            this.settings.setOrientation = ((TeleportIOObject)settings).setOrientation;
            this.settings.zeroCameraRotation = ((TeleportIOObject)settings).zeroCameraRotation;
        }

#if UNITY_EDITOR

        [CustomEditor(typeof(TeleportPoint), true), CanEditMultipleObjects]
        public class Lock_Editor : UniqueID_Editor
        {
            private TeleportPoint teleportPointScript;

            private void OnEnable()
            {
                GetBanner();
                Initialise();
            }

            protected override void Clear()
            {
                base.Clear();

                if (Application.isPlaying) return;

                if (m_instances != null)
                {
                    m_instances.RemoveIOObject(teleportPointScript.GetSettings(true));
                }
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();

                serializedObject.Update();

                DrawID();

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Teleport Setup", EditorStyles.boldLabel);

                EditorGUILayout.PropertyField(serializedObject.FindProperty("settings"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("targetTransform"), true);

                if(GUI.changed || GONameChanged())
                {
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(teleportPointScript);

                    if (Application.isPlaying) return;

                    if (m_instances != null)
                    {
                        m_instances.AddIOObject(teleportPointScript.ID, teleportPointScript.GetSettings());
                    }
                }
            }

            protected override void Initialise()
            {
                base.Initialise();

                teleportPointScript = (TeleportPoint)target;

                if (Application.isPlaying) return;

                if (m_instances != null)
                {
                    //need to get the settings from the instances script then update the settings
                    foreach (AppInstances.IOObjectTeleportHandler setting in m_instances.ioTeleportObjects)
                    {
                        if (setting.referenceID.Equals(teleportPointScript.ID))
                        {
                            teleportPointScript.ApplySettings(setting.settings);
                            break;
                        }
                    }

                    m_instances.AddIOObject(teleportPointScript.ID, teleportPointScript.GetSettings());
                }
            }
        }
#endif
    }
}