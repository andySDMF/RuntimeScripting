using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class FlythroughControl : MonoBehaviour
    {
        public bool inverted = false;
        public float speed = 1.0f;
        public float sensitivity = 1.0f;

        public bool smoothing = false;
        public float acceleration = 0.1f;

        private float m_actingSpeed = 0.0f;
        private Vector3 m_lastMousePos = new Vector3(0, 0, 0);
        private Vector3 m_lastDir = Vector3.zero;
        private bool m_keyHeldDown = false;

        private void Update()
        {
            //mouse button intitaiates the movement
            if (InputManager.Instance.GetMouseButton(1))
            {
                 m_keyHeldDown = true;

                //get mouse pos
                 m_lastMousePos = InputManager.Instance.GetMousePosition() - m_lastMousePos;

                 if(inverted)
                 {
                     m_lastMousePos.y = -m_lastMousePos.y;
                 }

                 //calculate new pos
                 m_lastMousePos *= sensitivity;
                 m_lastMousePos = new Vector3(transform.eulerAngles.x + m_lastMousePos.y, transform.eulerAngles.y + m_lastMousePos.x, 0.0f);

                //apply rotation
                 if(smoothing)
                 {
                     transform.eulerAngles = Vector3.Lerp(transform.eulerAngles, m_lastMousePos, Time.deltaTime);
                 }
                 else
                 {
                     transform.eulerAngles = m_lastMousePos;
                 }

                 m_lastMousePos = InputManager.Instance.GetMousePosition();
            }
            else
            {
                m_lastMousePos = InputManager.Instance.GetMousePosition();
                m_keyHeldDown = false;
            }

            if(m_keyHeldDown)
            {
                Vector3 dir = new Vector3();

                //get movement
                if (InputManager.Instance.GetKey("W") || InputManager.Instance.GetKey("UpArrow")) dir.z += 1.0f;
                if (InputManager.Instance.GetKey("S") || InputManager.Instance.GetKey("DownArrow")) dir.z -= 1.0f;
                if (InputManager.Instance.GetKey("A") || InputManager.Instance.GetKey("LeftArrow")) dir.x -= 1.0f;
                if (InputManager.Instance.GetKey("D") || InputManager.Instance.GetKey("RightArrow")) dir.x += 1.0f;

                dir.Normalize();

                //caclulate speed of movement
                if (dir != Vector3.zero)
                {
                    //move
                    if (m_actingSpeed < 1.0f)
                    {
                        m_actingSpeed += acceleration * Time.deltaTime * 20;
                    }
                    else
                    {
                        m_actingSpeed = 1.0f;
                    }

                    m_lastDir = dir;
                }
                else
                {
                    //stationary
                    if (m_actingSpeed > 0)
                    {
                        m_actingSpeed -= acceleration * Time.deltaTime * 20;
                    }
                    else
                    {
                        m_actingSpeed = 0.0f;
                    }
                }

                //apply movement
                if (smoothing)
                {
                    transform.Translate(m_lastDir * m_actingSpeed * speed * Time.deltaTime);
                }
                else
                {
                    transform.Translate(dir * speed * Time.deltaTime);
                }
            }
        }

        public void SetControls(bool inverted, float speed, float sensitivity, bool smoothing, float acceleration)
        {
            this.inverted = inverted;
            this.speed = speed;
            this.sensitivity = sensitivity;
            this.smoothing = smoothing;
            this.acceleration = acceleration;
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(FlythroughControl), true)]
        public class FlythroughControl_Editor : BaseInspectorEditor
        {
            private void OnEnable()
            {
                GetBanner();
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();

                serializedObject.Update();

                EditorGUILayout.PropertyField(serializedObject.FindProperty("inverted"), true);

                EditorGUILayout.PropertyField(serializedObject.FindProperty("speed"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("sensitivity"), true);

                EditorGUILayout.PropertyField(serializedObject.FindProperty("smoothing"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("acceleration"), true);

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
