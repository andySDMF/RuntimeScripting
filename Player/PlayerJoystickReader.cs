using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class PlayerJoystickReader : MonoBehaviour
    {
        private JoystickController m_joystick;
        private IPlayer m_playerControl;

        public bool isMoving = false;

        public Vector3 Direction { get; private set; }

        private Vector3 m_direction = Vector3.zero;


        private void Awake()
        {
            if (!AppManager.IsCreated) return;

            m_playerControl = GetComponent<IPlayer>();
            m_joystick = HUDManager.Instance.GetHUDControlObject("MOBILE_JOYSTICK").GetComponentInChildren<JoystickController>(true);
        }

        private void Start()
        {
            if (!AppManager.IsCreated) return;

            if (m_joystick == null)
            {
                Destroy(this);
                return;
            }

            PlayerManager.OnFixedUpdate += OnThisFixedUpdate;

            m_joystick.onJoyStickMoved += GetJoyStickDirection;
        }

        private void GetJoyStickDirection(Vector2 obj)
        {
            m_direction = obj;
        }

        private void OnDestroy()
        {
            PlayerManager.OnFixedUpdate -= OnThisFixedUpdate;
        }

        public void OnThisFixedUpdate()
        {
            if (!enabled) return;

            //move player based on joystick axis
            if (InputManager.Instance.GetMouseButton(0))
            {
                Vector3 direction = Vector3.forward * m_direction.y + Vector3.right * m_direction.x;
                Direction = direction;
   
                if (direction.magnitude != 0)
                {
                    isMoving = true;
                }
                else
                {
                    isMoving = false;
                }

                if (VehicleManager.Instance.HasPlayerEntertedVehcile(PlayerManager.Instance.GetLocalPlayer().ID))
                {
                    VehicleManager.Instance.MoveCurrentVehicle(direction);
                }
                else
                {
                    if (!m_playerControl.FreezePosition)
                    {
                        m_playerControl.Move(direction);
                    }
                }
            }
            else
            {
                isMoving = false;
                if(m_direction != Vector3.zero)
                {
                    m_direction = Vector3.zero;
                    m_playerControl.Move(m_direction);
                }
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(PlayerJoystickReader), true)]
        public class PlayerJoystickReader_Editor : BaseInspectorEditor
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
