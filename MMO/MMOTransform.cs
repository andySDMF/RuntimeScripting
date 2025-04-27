using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class MMOTransform : MonoBehaviour
    {
        [SerializeField]
        private string id = "";

        private int m_maximumNumberOfServiceFrames = 1000;
        private int m_highFrequencyEveryNthFrame = 20;
        private int m_lowFrequencyEveryNthFrame = 200;
        private int m_lowFrequencyFrameOffset = 10;

        private bool m_UpdateHighFrequency = true;
        private bool m_UpdateLowFrequency = true;
        private int m_ServiceFrameCounter = 0;

        private Vector3 m_CurrentPosition;
        private Quaternion m_CurrentRotation;
        private Vector3 m_CurrentScale;

        private Vector3 m_rigibodyTargetPosition;
        private Quaternion m_rigidbodyTargetRotation;

        private Quaternion m_targetRotation;
        private Vector3 m_targetPosition = Vector3.zero;
        private Vector3 m_targetScale;
        private Rigidbody m_rigidbody;

        private float m_Distance;
        private float m_Angle;
        private float m_scale;
        private float m_rbodyDistance;
        private float m_rboyAngle;

        public string ID
        {
            get
            {
                return gameObject.name;
            }
        }

        private string m_owner = "";

        public string Owner
        {
            get
            {
                return m_owner;
            }
        }

        public ITransformSync SyncListener
        {
            get;
            set;
        }


        private void Awake()
        {
            if (!AppManager.IsCreated) return;

            //get all player settings for photon
            m_maximumNumberOfServiceFrames = CoreManager.Instance.playerSettings.maximumNumberOfServiceFrames;
            m_highFrequencyEveryNthFrame = CoreManager.Instance.playerSettings.highFrequencyEveryNthFrame;
            m_lowFrequencyEveryNthFrame = CoreManager.Instance.playerSettings.lowFrequencyEveryNthFrame;
            m_lowFrequencyFrameOffset = CoreManager.Instance.playerSettings.lowFrequencyFrameOffset;

            m_rigidbody = GetComponent<Rigidbody>();
        }

        private void Start()
        {
            if (!AppManager.IsCreated) return;

            //initialise
            m_targetRotation = transform.rotation;
            m_targetPosition = transform.position;
            m_targetScale = transform.localScale;

            m_CurrentRotation = transform.rotation;
            m_CurrentPosition = transform.position;
            m_CurrentScale = transform.localScale;

            if(m_rigidbody != null)
            {
                m_rigibodyTargetPosition = m_rigidbody.position;
                m_rigidbodyTargetRotation = m_rigidbody.rotation;
            }

            MMOManager.Instance.TransformSyncObjects.Add(this);
            MMORoom.Instance.OnPlayerLeftRoom += OnPlayerLeft;
        }

        private void OnDestroy()
        {
            if (!AppManager.IsCreated) return;

            MMOManager.Instance.TransformSyncObjects.Remove(this);
            MMORoom.Instance.OnPlayerLeftRoom -= OnPlayerLeft;
        }

        private void Update()
        {
            if (string.IsNullOrEmpty(m_owner)) return;

            if (m_owner.Equals(PlayerManager.Instance.GetLocalPlayer().ID))
            {
                Quaternion rot = GetRotation();
                Vector3 pos = GetPosition();
                Vector3 sca = GetScale();

                SendRotationAtFrequencyUpdate(rot);
                SendPositionAtFrequencyUpdate(pos);
                SendScaleAtFrequencyUpdate(sca);

                Service();
            }
            else
            {
                UpdatePosition();
                UpdateRotation();
                UpdateScale();

                if(m_rigidbody != null)
                {
                    UpdateRigibody();
                }
            }
        }

        public void RequestOwnership(string playerID)
        {
            if (!m_owner.Equals(playerID))
            {
                Debug.Log("MMOTransform Request Ownership [" + ID + "]");

                m_owner = playerID;
                MMOManager.Instance.SendRPC("MMOTransformOwnership", (int)MMOManager.RpcTarget.All, ID, m_owner);
            }
        }

        /// <summary>
        /// Called to Service the synsing on this transform
        /// </summary>
        private void Service()
        {
            //Service is called 10 times per second, this means high frequency updates are every 2 seconds and low frequency updates are every 20 seconds
            bool serializeHighFrequency = m_ServiceFrameCounter % m_highFrequencyEveryNthFrame == 0;
            bool serializeLowFrequency = m_ServiceFrameCounter % m_lowFrequencyEveryNthFrame == m_lowFrequencyFrameOffset;

            if (serializeHighFrequency)
            {
                OnSerializeHighFrequency();
            }

            if (serializeLowFrequency)
            {
                OnSerializeLowFrequency();
            }

            m_ServiceFrameCounter++;

            if (m_ServiceFrameCounter > m_maximumNumberOfServiceFrames)
            {
                m_ServiceFrameCounter = 0;
            }
        }

        private void SendRotationAtFrequencyUpdate(Quaternion rotation)
        {
            if (rotation == m_CurrentRotation)
            {
                return;
            }

            m_CurrentRotation = rotation;
            m_UpdateHighFrequency = true;
        }

        private void SendScaleAtFrequencyUpdate(Vector3 scale)
        {
            if (scale == m_CurrentScale)
            {
                return;
            }

            m_CurrentScale = scale;
            m_UpdateHighFrequency = true;
        }

        /// <summary>
        /// Sets Position at high frequency syncing
        /// </summary>
        /// <param name="position"></param>
        private void SendPositionAtFrequencyUpdate(Vector3 position)
        {
            if (position == m_CurrentPosition)
            {
                return;
            }

            m_CurrentPosition = position;
            m_UpdateLowFrequency = true;
            m_UpdateHighFrequency = true;
        }

        /// <summary>
        /// Sends Sync at low frequency
        /// </summary>
        private void OnSerializeLowFrequency()
        {
            if (m_UpdateLowFrequency == false)
            {
                return;
            }

            m_UpdateLowFrequency = false;

            string data = "";
            bool useSyncListener = true;

#if BRANDLAB360_VEHICLES
            if (GetComponent<Vehicles.VehicleMesh>() != null)
            {
                useSyncListener = false;
                data = GetComponent<Vehicles.VehicleMesh>().GetSyncData();
            }
#endif

            if (SyncListener != null && useSyncListener)
            {
                data = SyncListener.GetSyncData();
            }

            MMOManager.Instance.SendRPC("MMOTransformSync", (int)MMOManager.RpcTarget.Others, ID, m_owner, m_CurrentPosition, m_CurrentRotation.eulerAngles, m_CurrentScale, data);

            if (m_rigidbody != null)
            {
                MMOManager.Instance.SendRPC("MMOTransformRigidbodySync", (int)MMOManager.RpcTarget.Others, ID, m_rigidbody.position, m_rigidbody.rotation.eulerAngles);

            }
        }

        /// <summary>
        /// Send Sync at high frequency
        /// </summary>
        private void OnSerializeHighFrequency()
        {
            if (m_UpdateHighFrequency == false)
            {
                return;
            }

            m_UpdateHighFrequency = false;

            string data = "";
            bool useSyncListener = true;

#if BRANDLAB360_VEHICLES
            if (GetComponent<Vehicles.VehicleMesh>() != null)
            {
                useSyncListener = false;
                data = GetComponent<Vehicles.VehicleMesh>().GetSyncData();
            }
#endif

            if (SyncListener != null && useSyncListener)
            {
                data = SyncListener.GetSyncData();
            }

            MMOManager.Instance.SendRPC("MMOTransformSync", (int)MMOManager.RpcTarget.Others, ID, m_owner, m_CurrentPosition, m_CurrentRotation.eulerAngles, m_CurrentScale, data);

            if (m_rigidbody != null)
            {
                MMOManager.Instance.SendRPC("MMOTransformRigidbodySync", (int)MMOManager.RpcTarget.Others, ID, m_rigidbody.position, m_rigidbody.rotation.eulerAngles);
            }
        }

        /// <summary>
        /// Get this transforms current rotation
        /// </summary>
        /// <returns></returns>
        private Quaternion GetRotation()
        {
            return Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
        }

        /// <summary>
        /// Gets this transforms current position
        /// </summary>
        /// <returns></returns>
        private Vector3 GetPosition()
        {
            return transform.position;
        }

        private Vector3 GetScale()
        {
            return transform.localScale;
        }

        public void Sync(Vector3 pos, Vector3 rot, Vector3 scale, string otherData = "")
        {
            if(!AppManager.Instance.Data.CurrentSceneReady)
            {
                transform.rotation = Quaternion.Euler(rot);
                transform.position = pos;
                transform.localScale = scale;

                m_targetRotation = transform.rotation;
                m_targetPosition = transform.position;
                m_targetScale = transform.localScale;

                m_CurrentRotation = transform.rotation;
                m_CurrentPosition = transform.position;
                m_CurrentScale = transform.localScale;

                return;
            }

            if (m_owner.Equals(PlayerManager.Instance.GetLocalPlayer().ID)) return;

            m_targetPosition = pos;
            m_targetRotation = Quaternion.Euler(rot);
            m_targetScale = scale;

            //jump if the distance is vast
            if (Vector3.Distance(transform.position, m_targetPosition) > 100)
            {
                transform.position = pos;
                m_targetPosition = transform.position;
                m_CurrentPosition = transform.position;
            }

            m_Distance = Vector3.Distance(transform.position, m_targetPosition);
            m_Angle = Quaternion.Angle(transform.rotation, m_targetRotation);
            m_scale = Vector3.Distance(transform.lossyScale, m_targetScale);
            bool useSyncListerner = true;

#if BRANDLAB360_VEHICLES
            if (GetComponent<Vehicles.VehicleMesh>() != null)
            {
                useSyncListerner = false;
                GetComponent<Vehicles.VehicleMesh>().SyncData(otherData);
            }
#endif

            if(SyncListener != null && useSyncListerner)
            {
                SyncListener.SyncData(otherData);
            }
        }

        public void SyncRigidbody(Vector3 pos, Vector3 rot)
        {
            if (m_rigidbody == null) return;

            if (!AppManager.Instance.Data.CurrentSceneReady)
            {
                if(!m_rigidbody.isKinematic)
                {
                    m_rigidbody.velocity = pos;
                    m_rigidbody.angularVelocity = rot;
                }

                m_rigidbody.rotation = Quaternion.Euler(rot);
                m_rigidbody.position = pos;

                m_rigidbodyTargetRotation = m_rigidbody.rotation;
                m_rigibodyTargetPosition = m_rigidbody.position;

                return;
            }

            if (m_owner.Equals(PlayerManager.Instance.GetLocalPlayer().ID)) return;

            if (!m_rigidbody.isKinematic)
            {
                m_rigidbody.velocity = pos;
                m_rigidbody.angularVelocity = rot;
            }

            m_rigibodyTargetPosition = pos;
            m_rigidbodyTargetRotation = Quaternion.Euler(rot);

            m_rbodyDistance = Vector3.Distance(m_rigidbody.position, m_rigibodyTargetPosition);
            m_rboyAngle = Quaternion.Angle(m_rigidbody.rotation, m_rigidbodyTargetRotation);
        }

        public void SyncTransferOwnership(string playerID)
        {
            m_owner = playerID;
            bool useSyncListerner = true;

#if BRANDLAB360_VEHICLES
            if (GetComponent<Vehicles.VehicleMesh>() != null)
            {
                useSyncListerner = false;
                GetComponent<Vehicles.VehicleMesh>().SetOwnership(m_owner.Equals(PlayerManager.Instance.GetLocalPlayer().ID));
            }
#endif

            if (SyncListener != null && useSyncListerner)
            {
                SyncListener.SetOwnership(m_owner.Equals(PlayerManager.Instance.GetLocalPlayer().ID));
            }
        }


        private void UpdatePosition()
        {
            transform.position = Vector3.MoveTowards(transform.position, m_targetPosition, (m_Distance * AppManager.Instance.Settings.projectSettings.serializationRate) * (1.0f / AppManager.Instance.Settings.projectSettings.serializationRate));
            m_CurrentPosition = transform.position;
        }

        private void UpdateRotation()
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, m_targetRotation, m_Angle  * (1.0f / AppManager.Instance.Settings.projectSettings.serializationRate));
            m_CurrentRotation = transform.rotation;
        }

        private void UpdateScale()
        {
            transform.localScale = Vector3.Lerp(transform.localScale, m_targetScale, (m_scale * AppManager.Instance.Settings.projectSettings.serializationRate) *(1.0f / AppManager.Instance.Settings.projectSettings.serializationRate));
            m_CurrentScale = transform.localScale;
        }

        private void UpdateRigibody()
        {
            m_rigidbody.position = Vector3.MoveTowards(m_rigidbody.position, m_rigibodyTargetPosition, (m_rbodyDistance * AppManager.Instance.Settings.projectSettings.serializationRate) * (1.0f / AppManager.Instance.Settings.projectSettings.serializationRate));
            m_rigidbody.rotation = Quaternion.RotateTowards(m_rigidbody.rotation, m_rigidbodyTargetRotation, m_rboyAngle * (1.0f / AppManager.Instance.Settings.projectSettings.serializationRate));
        }

        private void OnPlayerLeft(IPlayer player)
        {
            if(m_owner.Equals(player.ID))
            {
                m_owner = MMOManager.Instance.GetMasterClientID();
            }
        }

        public interface ITransformSync
        {
            public void SyncData(string data);
            public string GetSyncData();
            public void SetOwnership(bool isMine);
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(MMOTransform), true)]
        public class MMOTransform_Editor : BaseInspectorEditor
        {
            private MMOTransform script;

            private void OnEnable()
            {
                script = (MMOTransform)target;
                GetBanner();
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();

                serializedObject.Update();

                if (!Application.isPlaying)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("id"), true);

                    if (script.gameObject.scene.IsValid())
                    {
                        if (string.IsNullOrEmpty(script.id))
                        {
                            if (string.IsNullOrEmpty(script.gameObject.name))
                            {
                                script.gameObject.name = "GameObject";
                            }

                            script.id = script.gameObject.name + "_" + Random.Range(0, 10001).ToString();
                            script.gameObject.name = script.id;
                        }
                        else
                        {
                            script.gameObject.name = script.id;
                        }
                    }
                }

                if (GUI.changed)
                {
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(this);
                }
            }
        }
#endif
    }

}