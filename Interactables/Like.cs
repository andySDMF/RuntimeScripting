using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class Like : UniqueID, IDataAPICallback
    {
        [SerializeField]
        private TextMeshProUGUI likeCount;

        private int m_likeCount = 0;
        private bool m_likedThisSession = false;

        public void OnClick()
        {
            if (m_likedThisSession) return;

            m_likedThisSession = true;

            m_likeCount++;
            likeCount.text = m_likeCount.ToString();

            AnalyticsManager.Instance.PostAnalyticsEvent(EventCategory.Content, EventAction.Click, AnalyticReference);

            LikeManager.Instance.PostLike(id, m_likeCount);
        }

        

        public void DataAPICallback(List<DataObject> objs)
        {
            for (int i = 0; i < objs.Count; i++)
            {
                if (objs[i].uniqueId.Equals(ID))
                {
                    int n;

                    if(int.TryParse(objs[i].data, out n))
                    {
                        m_likeCount = n;
                    }

                    likeCount.text = m_likeCount.ToString();
                    return;
                }
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(Like), true), CanEditMultipleObjects]
        public class Like_Editor : UniqueID_Editor
        {
            public override void OnInspectorGUI()
            {
                serializedObject.Update();

                base.OnInspectorGUI();

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Like", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("likeCount"), true);

                if (GUI.changed)
                {
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(script);
                }
            }
        }
#endif
    }
}