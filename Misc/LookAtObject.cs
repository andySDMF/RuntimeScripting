using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class LookAtObject : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("How this Object is looking at the focus via the XYZ axis")]
        protected Contraints contraints;

        private Vector3 targetPostition;
        private Transform focus;
        private Transform[] m_focuses;
        private bool m_isMultipleFocuses = false;

        public void UpdateFocus(Transform focus)
        {
            this.focus = focus;
            m_isMultipleFocuses = false;
        }

        public void UpdateFocus(Transform[] focuses)
        {
            m_focuses = focuses;
            m_isMultipleFocuses = true;
        }


        private void Update()
        {
            //if there is a focus object look at object based on constraints
            if(m_isMultipleFocuses)
            {
                if(m_focuses != null && m_focuses.Length > 0)
                {
                    //need to find the closest m_focus
                    Transform closest = null;

                    for(int i = 0; i < m_focuses.Length; i++)
                    {
                        if(closest == null)
                        {
                            closest = m_focuses[i];
                        }
                        else
                        {
                            if(Vector3.Distance(transform.position, m_focuses[i].position) < Vector3.Distance(transform.position, closest.position))
                            {
                                closest = m_focuses[i];
                            }
                        }
                    }

                    focus = closest;
                }
            }

            if (focus != null)
            {
                targetPostition = new Vector3((!contraints.contrainX) ? focus.position.x : this.transform.position.x, (!contraints.contrainY) ? focus.position.y : this.transform.position.y, (!contraints.contrainZ) ? focus.position.z : this.transform.position.z);
                transform.LookAt(targetPostition);
            }
        }

        [System.Serializable]
        protected class Contraints
        {
            public bool contrainX = false;
            public bool contrainY = false;
            public bool contrainZ = false;
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(LookAtObject), true)]
        public class LookAtObject_Editor : BaseInspectorEditor
        {
            private void OnEnable()
            {
                GetBanner();
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();
                serializedObject.Update();

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("contraints"), true);

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
