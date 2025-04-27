using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    [RequireComponent(typeof(MMOTransform))]
    public class TestMMOTransform : MonoBehaviour
    {
        [SerializeField]
        private Transform canvas;

        private MMOTransform m_sync;

        private bool m_state = false;
        private bool m_isFrozen = false;

        private void Awake()
        {
            m_sync = GetComponent<MMOTransform>();
        }

        private void OnMouseUp()
        {
            //send owenership

            if(!m_state)
            {
                m_state = true;
                m_sync.RequestOwnership(PlayerManager.Instance.GetLocalPlayer().ID);
                PlayerManager.Instance.FreezePlayer(true);
                m_isFrozen = true;
            }
            else
            {
                m_state = false;
                m_sync.RequestOwnership("");
                PlayerManager.Instance.FreezePlayer(false);
                m_isFrozen = false;
            }
        }

        private void Update()
        {
            if(PlayerManager.Instance.GetLocalPlayer() != null)
            {
                canvas.LookAt(PlayerManager.Instance.GetLocalPlayer().TransformObject);
            }

            if (m_isFrozen && m_state)
            {
                if (m_sync.Owner != PlayerManager.Instance.GetLocalPlayer().ID)
                {
                    m_isFrozen = false;
                    m_state = false;
                    PlayerManager.Instance.FreezePlayer(false);
                    return;
                }
            }


            if(m_state)
            {
                if(InputManager.Instance.GetKey("W"))
                {
                    transform.position += new Vector3(0, 0, 2) * Time.deltaTime;
                }
                else if(InputManager.Instance.GetKey("S"))
                {
                    transform.position += new Vector3(0, 0, -2) * Time.deltaTime;
                }
                
                if (InputManager.Instance.GetKey("A"))
                {
                    transform.position += new Vector3(-2, 0, 0) * Time.deltaTime;
                }
                else if (InputManager.Instance.GetKey("D"))
                {
                    transform.position += new Vector3(2, 0, 0) * Time.deltaTime;
                }
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(TestMMOTransform), true)]
        public class TestMMOTransform_Editor : BaseInspectorEditor
        {

            private void OnEnable()
            {
                GetBanner();
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();

                serializedObject.Update();

                EditorGUILayout.PropertyField(serializedObject.FindProperty("canvas"), true);

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
