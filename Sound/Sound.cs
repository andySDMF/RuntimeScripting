using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class Sound : MonoBehaviour
    {
        [SerializeField]
        private AudioClip clip;

        private bool m_isActive = false;

        //need to replace
        public void PlaySound(bool soundOn)
        {
            if(m_isActive.Equals(soundOn))
            {
                return;
            }

            m_isActive = soundOn;

            if (soundOn)
            {
                SoundManager.Instance.PlaySound(clip);
            }
            else
            {
                SoundManager.Instance.Stop(true);
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(Sound), true)]
        public class Sound_Editor : BaseInspectorEditor
        {
            private void OnEnable()
            {
                GetBanner();
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();
                serializedObject.Update();

                EditorGUILayout.PropertyField(serializedObject.FindProperty("clip"), true);

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
