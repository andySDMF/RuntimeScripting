using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.AI;

#if BRANDLAB360_INTERNAL
using BrandLab360.Internal;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class MMOPlayerSync : MonoBehaviour
    {
         private int m_maximumNumberOfServiceFrames = 1000;
         private int m_highFrequencyEveryNthFrame = 20;
         private int m_lowFrequencyEveryNthFrame = 200;
         private int m_lowFrequencyFrameOffset = 10;

         private Vector3 m_CurrentPosition;
         private Quaternion m_CurrentRotation;
         private bool m_UpdateHighFrequency = true;
         private bool m_UpdateLowFrequency = true;
         private int m_ServiceFrameCounter = 0;

         private Quaternion m_targetRotation;
         private Vector3 m_targetPosition = Vector3.zero;
         private float m_cooldown = 0.0f;
         private float m_maxCooldown = 1.0f;
         private int m_rInput = 1;
         private bool m_navMeshAgentMove = false;
         private NavMeshAgent m_navMeshAgent;

         private MMOPlayer m_networkPlayer;

         private byte m_raiseEventMove = 100;
         private byte m_raiseEventJump = 101;
         private byte m_raiseEventTeleport = 102;
         private int m_movementID = -1;
         private int m_previousMovement = -1;

#if BRANDLAB360_INTERNAL
        private PhotonEventListener m_photonEventtListener;
#endif

        private ColyseusEventListener m_colyseusEventListener;

         public bool IsLocal
         {
             get
             {
                 return m_networkPlayer.view.IsMine;
             }
         }

         public int MovementID
         {
             get
             {
                 return m_movementID;
             }
         }

         public bool OverrideAnimations
         {
             get;
             set;
         }

         public Animator MainAnimator
         {
             get
             {
                 return m_networkPlayer.animator;
             }
         }

         private void Awake()
         {
             m_networkPlayer = GetComponent<MMOPlayer>();

             //get all player settings for photon
             m_maximumNumberOfServiceFrames = CoreManager.Instance.playerSettings.maximumNumberOfServiceFrames;
             m_highFrequencyEveryNthFrame = CoreManager.Instance.playerSettings.highFrequencyEveryNthFrame;
             m_lowFrequencyEveryNthFrame = CoreManager.Instance.playerSettings.lowFrequencyEveryNthFrame;
             m_lowFrequencyFrameOffset = CoreManager.Instance.playerSettings.lowFrequencyFrameOffset;
             m_maxCooldown = CoreManager.Instance.playerSettings.cooldown;

             if (m_maxCooldown < 0.5f)
             {
                 m_maxCooldown = 0.5f;
             }
        }

        private void Start()
        {
            //initialise
            m_targetRotation = transform.rotation;
            m_targetPosition = transform.position;

            m_CurrentRotation = transform.rotation;
            m_CurrentPosition = transform.position;

            //subscribe to playermanager events
            PlayerManager.OnUpdate += OnThisUpdate;
            PlayerManager.OnFixedUpdate += OnThisFixedUpdate;

            m_navMeshAgent = GetComponent<NavMeshAgent>();

            StartCoroutine(Delay());
        }

        private IEnumerator Delay()
        {
            yield return new WaitForEndOfFrame();

            if(AppManager.Instance.Data.Mode.Equals(MultiplayerMode.Online))
            {
                if (AppManager.Instance.Settings.projectSettings.mmoProtocal.Equals(MMOProtocal.Photon))
                {
#if BRANDLAB360_INTERNAL
                    m_photonEventtListener = m_networkPlayer.view.GO.GetComponent<PhotonEventListener>();
                    m_photonEventtListener.Callback_OnEvent += OnPhotonEvent;
#endif

                }
                else
                {
                    m_colyseusEventListener = m_networkPlayer.view.GO.GetComponent<ColyseusEventListener>();
                    m_colyseusEventListener.Callback_OnEvent += OnColyseusEvent;
                }
            }
        }

         private void OnDestroy()
         {
             PlayerManager.OnUpdate -= OnThisUpdate;
             PlayerManager.OnFixedUpdate -= OnThisFixedUpdate;
         }

         /// <summary>
         /// Called to reset the animation to idle
         /// </summary>
         public void ResetAnimation()
         {
             m_movementID = -1;
         }

         /// <summary>
         /// Called to instantly jump this remote player to a position (json)
         /// </summary>
         /// <param name="data"></param>
         public void Jump(string data)
         {
             Movement move = JsonUtility.FromJson<Movement>(data);

             if (move != null)
             {
                 Jump(new Vector3(move.x, move.y, move.z), Quaternion.Euler(0.0f, move.r, 0.0f));
             }
         }

         /// <summary>
         /// Called to instanlty jump this remote player to position & rotation
         /// </summary>
         /// <param name="pos"></param>
         /// <param name="rot"></param>
         public void Jump(Vector3 pos, Quaternion rot)
         {
             m_targetRotation = rot;
             m_targetPosition = pos;

             m_CurrentRotation = rot;
             m_CurrentPosition = pos;

             transform.position = m_CurrentPosition;
             transform.rotation = m_CurrentRotation;
         }

         /// <summary>
         /// Event passed to the PlayerManager events
         /// </summary>
         private void OnThisUpdate()
         {
             if (m_networkPlayer == null || m_networkPlayer.view == null) return;

             //check if remote player
             if (!m_networkPlayer.view.IsMine)
             {
                 if (OverrideAnimations) return;

                Debug.Log("movemtn id = " + m_movementID);

                 //navmesh agent mmovement or  not
                 if (!m_navMeshAgentMove)
                 {
                    if(m_movementID < 10)
                    {
                        if (ChairManager.Instance.HasPlayerOccupiedChair(m_networkPlayer.ID))
                        {
                            //need to play sitting animation based on the chair that this avatar is on
                            m_networkPlayer.Walk(false, -1);
                            m_networkPlayer.FreezePosition = true;
                            m_networkPlayer.FreezeRotation = true;

                            if (!string.IsNullOrEmpty(m_networkPlayer.SittingAnimation))
                            {
                                if (m_networkPlayer.animator != null)
                                {
                                    m_networkPlayer.animator.SetBool(m_networkPlayer.SittingAnimation, true);
                                }
                            }

                            IChairObject ch = ChairManager.Instance.GetChairFromOccupiedPlayer(m_networkPlayer.ID);

                            if (ch != null)
                            {
                                transform.position = ch.SittingPosition(m_networkPlayer.ID);

                                if (ch.HasSittingSpot)
                                {
                                    transform.rotation = Quaternion.LookRotation(ch.SittingDirection(m_networkPlayer.ID), Vector3.up);
                                }
                                else
                                {
                                    transform.localEulerAngles = ch.SittingDirection(m_networkPlayer.ID);
                                }
                            }

                            return;
                        }
                        else if (VehicleManager.Instance.HasPlayerEntertedVehcile(m_networkPlayer.ID))
                        {
                            m_networkPlayer.Walk(false, -1);
                            m_networkPlayer.FreezePosition = true;
                            m_networkPlayer.FreezeRotation = true;

                            if (m_networkPlayer.animator != null)
                            {
                                m_networkPlayer.animator.SetBool(VehicleManager.Instance.DrivingAnimation, true);
                            }
                            return;
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(m_networkPlayer.SittingAnimation))
                            {
                                //help the remote avatar go to idle when not sitting/driving
                                if (m_networkPlayer.animator != null)
                                {
                                    if (m_networkPlayer.animator.GetBool(m_networkPlayer.SittingAnimation))
                                    {
                                        m_networkPlayer.Walk(true, -1);
                                        m_networkPlayer.FreezePosition = false;
                                        m_networkPlayer.FreezeRotation = false;
                                    }

                                    m_networkPlayer.animator.SetBool(m_networkPlayer.SittingAnimation, false);
                                }
                            }

                            if (!string.IsNullOrEmpty(VehicleManager.Instance.DrivingAnimation))
                            {
                                if (m_networkPlayer.animator != null)
                                {
                                    //help the remote avatar go to idle when not sitting/driving
                                    if (m_networkPlayer.animator.GetBool(VehicleManager.Instance.DrivingAnimation))
                                    {
                                        m_networkPlayer.Walk(true, -1);
                                        m_networkPlayer.FreezePosition = false;
                                        m_networkPlayer.FreezeRotation = false;
                                    }

                                    m_networkPlayer.animator.SetBool(VehicleManager.Instance.DrivingAnimation, false);
                                }
                            }
                        }

                        //check angle moved
                        bool angleMoved = Quaternion.Angle(transform.rotation, m_targetRotation) > 0.1f;
                        //bool check pos moved
                        bool moved = m_targetPosition != m_CurrentPosition;

                        if (moved)
                        {
                            Vector3 direction = m_targetPosition - transform.position;
                            float forward = Vector3.Dot(-direction.normalized, m_targetPosition.normalized);

                            if (m_movementID < 0 && forward != 0)
                            {
                                m_movementID = m_previousMovement;
                            }
                            else
                            {
                                if (m_previousMovement != m_movementID)
                                {
                                    m_previousMovement = m_movementID;
                                }
                            }

                            m_networkPlayer.Walk(true, m_movementID);

                            if (m_rInput >= 0)
                            {
                                m_networkPlayer.RotateAvatar(true, false);
                            }
                            else
                            {
                                m_networkPlayer.RotateAvatar(false, true);
                            }
                        }
                        else if (angleMoved)
                        {
                            m_networkPlayer.Walk(true, m_movementID);
                        }
                        else
                        {
                            Cooldown();
                        }
                    }
                    else
                    {
                        Debug.Log("swimming" + m_movementID);
                        m_networkPlayer.Walk(true, m_movementID);
                    }
                 }

                 //update pos & rot
                 if (!m_networkPlayer.FreezePosition)
                 {
                     UpdatePosition();
                 }

                 if (!m_networkPlayer.FreezeRotation)
                 {
                     UpdateRotation();
                 }
             }
             else
             {
                 //ignore if frozen
                 if (PlayerManager.Instance.GetLocalPlayer().FreezeRotation || PlayerManager.Instance.GetLocalPlayer().FreezePosition) return;

                 //ignore if no key or mouse pressed
                 if (InputManager.Instance.AnyMouseButtonDown() || InputManager.Instance.AnyKeyHeldDown())
                 {
                     //send data
                     Quaternion playerRotation = GetRotation();

                     if (Quaternion.Angle(playerRotation, m_CurrentRotation) > 360f / 16f)
                     {
                         SendRotationAtFrequencyUpdate(playerRotation);
                     }

                     Vector3 playerPosition = GetPosition();

                     if (Vector3.Distance(playerPosition, m_CurrentPosition) > 0.1f)
                     {
                         SendPositionAtFrequencyUpdate(playerPosition);
                     }
                 }
                 else
                 {
                     if (m_CurrentPosition != transform.position)
                     {
                         SendPositionAtFrequencyUpdate(GetPosition());
                     }
                 }
             }
         }

         /// <summary>
         /// Event passed to the PlayerManager
         /// </summary>
         private void OnThisFixedUpdate()
         {
             if (m_networkPlayer.view.IsMine)
             {
                 Service();
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

         /// <summary>
         /// Called to cool down to idle over a period of time
         /// </summary>
         private void Cooldown()
         {
             m_cooldown += Time.deltaTime;

             if (m_cooldown < m_maxCooldown) return;

             m_movementID = -1;
             m_previousMovement = m_movementID;
             m_networkPlayer.Walk(false);
             m_networkPlayer.RotateAvatar(true, false);
         }

         /// <summary>
         /// retuns the rotation direction
         /// </summary>
         /// <param name="from"></param>
         /// <param name="to"></param>
         /// <returns></returns>
         private bool GetRotateDirection(Quaternion from, Quaternion to)
         {
             float fromY = from.eulerAngles.y;
             float toY = to.eulerAngles.y;
             float clockWise = 0f;
             float counterClockWise = 0f;

             if (fromY <= toY)
             {
                 clockWise = toY - fromY;
                 counterClockWise = fromY + (360 - toY);
             }
             else
             {
                 clockWise = (360 - fromY) + toY;
                 counterClockWise = fromY - toY;
             }

             return (clockWise <= counterClockWise);
         }

         /// <summary>
         /// Updates the remote players rotation
         /// </summary>
         private void UpdateRotation()
         {
             if (!m_navMeshAgentMove)
             {
                 transform.rotation = Quaternion.Slerp(transform.rotation, m_targetRotation, Time.deltaTime * CoreManager.Instance.playerSettings.sensitivity / (m_movementID.Equals(4) || m_movementID.Equals(5) ? 2 : 2));
             }
         }

         /// <summary>
         /// Updates the remote players position
         /// </summary>
         private void UpdatePosition()
         {
             //ensure looking at new target position
             CalculateLook(m_targetPosition);

             //nav mesh agent moving transform or not
             if (m_navMeshAgentMove)
             {
                 if (m_navMeshAgent != null)
                 {
                     //check nav mesh distance and set values
                     if (m_navMeshAgent.remainingDistance <= 0.1f)
                     {
                         m_movementID = -1;
                         m_previousMovement = m_movementID;
                         m_networkPlayer.Walk(false, m_movementID);
                         m_networkPlayer.RotateAvatar(true, false);

                         m_CurrentPosition = transform.position;
                         m_CurrentRotation = transform.rotation;
                         m_targetPosition = m_CurrentPosition;
                         m_targetRotation = m_CurrentRotation;

                         m_navMeshAgentMove = false;
                         m_navMeshAgent.isStopped = true;
                     }
                     else
                     {
                         m_movementID = 0;
                         m_previousMovement = m_movementID;
                         m_networkPlayer.Walk(true, m_movementID);
                         m_networkPlayer.RotateAvatar(true, false);
                     }
                 }
             }
             else
             {
                 //normal position movement
                 float speed = (m_movementID == 1) ? PlayerManager.Instance.MainControlSettings.run : PlayerManager.Instance.MainControlSettings.walk;
                 transform.position = Vector3.MoveTowards(transform.position, m_targetPosition, Time.deltaTime * (m_movementID.Equals(2) || m_movementID.Equals(3) ? (PlayerManager.Instance.MainControlSettings.strife - 0.05f) : (speed - 0.05f)));
             }

             //avoid local player
             Vector3 delta = PlayerManager.Instance.GetLocalPlayer().TransformObject.position - transform.position;
             delta.y = 0f;

             float magnitude = delta.magnitude;

             if (magnitude < 1.0f)
             {
                 transform.position -= delta.normalized * (1.0f - magnitude);
             }

             m_CurrentPosition = transform.position;
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

         /// <summary>
         /// Sets Rotation at high frequency syncing
         /// </summary>
         /// <param name="rotation"></param>
         private void SendRotationAtFrequencyUpdate(Quaternion rotation)
         {
             if (rotation == m_CurrentRotation)
             {
                 return;
             }

             m_CurrentRotation = rotation;
             m_UpdateHighFrequency = true;
             m_navMeshAgentMove = false;
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
             m_navMeshAgentMove = false;
         }

         public void SendCurrentPositionToPlayer(IPlayer player)
         {
             StartCoroutine(DelaySendCurrentPositionToPlayer(player));
         }

         private IEnumerator DelaySendCurrentPositionToPlayer(IPlayer player)
         {
             yield return new WaitForSeconds(1.0f);



             int[] n = new int[1];
             n[0] = player.ActorNumber;
             RaiseUpdatePositionAndRotationEvent(m_raiseEventJump);
         }

         /// <summary>
         /// Instancly send this transforms position and rotation across network
         /// </summary>
         /// <param name="position"></param>
         /// <param name="rotation"></param>
         /// <param name="navMeshAgent"></param>
         public void SendPositionAndRotationAtFrequencyNow(Vector3 position, Quaternion rotation, bool jump = false, bool navMeshAgent = false)
         {
             if (position == m_CurrentPosition)
             {
                 return;
             }

             m_CurrentPosition = position;
             m_CurrentRotation = rotation;
             m_navMeshAgentMove = navMeshAgent;

             if (navMeshAgent)
             {
                 if (jump)
                 {
                     RaiseJump();
                 }
                 else
                 {
                     RaiseTeleport();
                 }
             }
             else if (jump)
             {
                 RaiseJump();
             }
             else
             {
                 RaiseUpdatePositionAndRotationEvent(m_raiseEventMove);
             }
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
             RaiseUpdatePositionAndRotationEvent(m_raiseEventMove);
             m_navMeshAgentMove = false;
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
             RaiseUpdatePositionAndRotationEvent(m_raiseEventMove);
             m_navMeshAgentMove = false;
         }

         /// <summary>
         /// Action called to raise the syncing and send out across network
         /// </summary>
         private void RaiseUpdatePositionAndRotationEvent(byte eventCode, int[] targetPlayers = null)
         {
            Quaternion sendQuat;
            sendQuat.x = m_CurrentPosition.x;
            sendQuat.y = m_CurrentPosition.y;
            sendQuat.z = m_CurrentPosition.z;
            sendQuat.w = m_CurrentRotation.eulerAngles.y;

            object[] content = new object[] { sendQuat, PlayerManager.Instance.GetLocalPlayer().MovementID }; // Array contains the target position and the IDs of the selected units

#if BRANDLAB360_INTERNAL
            if(m_photonEventtListener != null)
            {
                m_photonEventtListener.RaiseEvent(eventCode, content, targetPlayers);
            }
#endif

            if (m_colyseusEventListener != null)
            {
                object[] changeSet = new object[7];
                changeSet[0] = (eventCode.Equals(m_raiseEventJump)) ? "playerSyncJump" : (eventCode.Equals(m_raiseEventTeleport)) ? "playerSyncTeleport" : "playerSync";
                changeSet[1] = PlayerManager.Instance.GetLocalPlayer().ID;
                changeSet[2] = sendQuat.x;
                changeSet[3] = sendQuat.y;
                changeSet[4] = sendQuat.z;
                changeSet[5] = sendQuat.w;
                changeSet[6] = PlayerManager.Instance.GetLocalPlayer().MovementID;

                m_colyseusEventListener.RaiseEvent(changeSet);
            }
        }

         public void RaiseJump(int[] targetPlayers = null)
         {
             Quaternion sendQuat;
             sendQuat.x = m_CurrentPosition.x;
             sendQuat.y = m_CurrentPosition.y;
             sendQuat.z = m_CurrentPosition.z;
             sendQuat.w = m_CurrentRotation.eulerAngles.y;

             byte evCode = m_raiseEventJump; // Custom Event 0: Used as "MoveUnitsToTargetPosition" event
             object[] content = new object[] { sendQuat, -1 }; // Array contains the target position and the IDs of the selected units

#if BRANDLAB360_INTERNAL
            if(m_photonEventtListener != null)
            {
                m_photonEventtListener.RaiseEvent(evCode, content, targetPlayers);
            }
#endif
            if (m_colyseusEventListener != null)
            {
                object[] changeSet = new object[7];
                changeSet[0] = "playerSyncJump";
                changeSet[1] = PlayerManager.Instance.GetLocalPlayer().ID;
                changeSet[2] = sendQuat.x;
                changeSet[3] = sendQuat.y;
                changeSet[4] = sendQuat.z;
                changeSet[5] = sendQuat.w;
                changeSet[6] = -1;

                m_colyseusEventListener.RaiseEvent(changeSet);
            }
        }

         public void RaiseTeleport(int[] targetPlayers = null)
         {
             Quaternion sendQuat;
             sendQuat.x = m_CurrentPosition.x;
             sendQuat.y = m_CurrentPosition.y;
             sendQuat.z = m_CurrentPosition.z;
             sendQuat.w = m_CurrentRotation.eulerAngles.y;


             byte evCode = m_raiseEventTeleport; // Custom Event 0: Used as "MoveUnitsToTargetPosition" event
             object[] content = new object[] { sendQuat, 0 }; // Array contains the target position and the IDs of the selected units

#if BRANDLAB360_INTERNAL
            m_photonEventtListener.RaiseEvent(evCode, content, targetPlayers);
#endif
            if (m_colyseusEventListener != null)
            {
                object[] changeSet = new object[7];
                changeSet[0] = "playerSyncTeleport";
                changeSet[1] = PlayerManager.Instance.GetLocalPlayer().ID;
                changeSet[2] = sendQuat.x;
                changeSet[3] = sendQuat.y;
                changeSet[4] = sendQuat.z;
                changeSet[5] = sendQuat.w;
                changeSet[6] = 0;

                m_colyseusEventListener.RaiseEvent(changeSet);
            }
        }

        /// <summary>
        /// Called to calculate the look at target for rotation
        /// </summary>
        /// <param name="target"></param>
        private void CalculateLook(Vector3 target)
         {
             var dir = target - transform.position;
             dir.y = 0f;
             if (dir.sqrMagnitude <= float.Epsilon)
             {
                 return;
             }

             if (dir != Vector3.zero)
             {
                 m_CurrentRotation = Quaternion.LookRotation(dir, Vector3.up);
             }
         }

         /// <summary>
         /// Called on remote player to set the movement of transform
         /// </summary>
         /// <param name="move"></param>
         private void RecievedMovement(Movement move, bool jump = false, bool navmeshagent = false)
         {
             //new targets
             m_targetRotation = Quaternion.Euler(0.0f, move.r, 0.0f);
             m_targetPosition = new Vector3(move.x, move.y, move.z);
             m_cooldown = 0.0f;
             m_movementID = move.m;

             //if jump, jump to new pos/rot
             if (jump)
             {
                 Jump(m_targetPosition, m_targetRotation);
             }

             //sets the avatar roation
             m_rInput = 0;

             //nav mesh movement
             m_navMeshAgentMove = navmeshagent;

             if (m_navMeshAgentMove)
             {
                 if (m_navMeshAgent != null)
                 {
                     m_navMeshAgent.isStopped = false;
                     m_navMeshAgent.destination = m_targetPosition;
                 }
             }
             else
             {
                 if (m_navMeshAgent != null)
                 {
                     if (m_navMeshAgent.isOnNavMesh)
                     {
                         m_navMeshAgent.isStopped = true;
                     }
                 }
             }
         }

         /// <summary>
         /// Photon RaiseEvent for handling this remote players movement
         /// </summary>
         /// <param name="photonEvent"></param>
         public void OnPhotonEvent(byte eventCode, object customData, int sender)
         {
             if (!AppManager.IsCreated) return;

             if (m_networkPlayer == null || m_networkPlayer.view == null || m_networkPlayer.view.Owner == null) return;

             if (!m_networkPlayer.view.IsMine && sender.Equals(m_networkPlayer.view.Actor))
             {
                 if (customData is object[])
                 {
                     object[] data = (object[])customData;
                     Movement move = new Movement();

                     if (data != null && data.Length == 2)
                     {
                         move.x = ((Quaternion)data[0]).x;
                         move.y = ((Quaternion)data[0]).y;
                         move.z = ((Quaternion)data[0]).z;
                         move.r = ((Quaternion)data[0]).w;
                         move.m = (int)data[1];
                     }
                     else
                     {
                         move = null;
                     }

                     if (eventCode == m_raiseEventMove)
                     {
                         if (move != null)
                         {
                             RecievedMovement(move);
                         }
                     }
                     else if (eventCode == m_raiseEventJump)
                     {
                         if (move != null)
                         {
                             RecievedMovement(move, true);
                         }
                     }
                     else if (eventCode == m_raiseEventTeleport)
                     {
                         if (move != null)
                         {
                             RecievedMovement(move, false, true);
                         }
                     }
                 }
             }
         }

        public void OnColyseusEvent(string customData)
        {
            if (!AppManager.IsCreated) return;

            if (m_networkPlayer == null || m_networkPlayer.view == null) return;

            ColyseusMessages.PlayerSync syncData = JsonUtility.FromJson<ColyseusMessages.PlayerSync>(customData);

            if (syncData == null) return;

            if (!m_networkPlayer.view.IsMine && syncData.playerID.Equals(m_networkPlayer.view.ID))
            {
                Movement move = new Movement();

                move.x = syncData.xPos;
                move.y = syncData.yPos;
                move.z = syncData.zPos;
                move.r = syncData.yRot;
                move.m = syncData.movement;

                if (syncData.eventName.Equals("playerSync"))
                {
                    RecievedMovement(move);
                }
                else if (syncData.eventName.Equals("playerSyncJump"))
                {
                    RecievedMovement(move, true);
                }
                else if (syncData.eventName.Equals("playerSyncTeleport"))
                {
                    RecievedMovement(move, false, true);
                }
            }
        }

        [System.Serializable]
         public class Movement
         {
             //pos
             public float x;
             public float y;
             public float z;
             //rot
             public float r;

             //movement animation ID
             public int m = -1;
         }

#if UNITY_EDITOR
        [CustomEditor(typeof(MMOPlayerSync), true)]
        public class MMOPlayerSync_Editor : BaseInspectorEditor
        {
            private void OnEnable()
            {
                GetBanner();
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();
            }
        }
#endif
    }
}
