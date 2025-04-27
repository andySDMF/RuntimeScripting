using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class NPCBotQuestions : MonoBehaviour
    {
        [SerializeField]
        private Transform questionContainer;

        [SerializeField]
        private GameObject questionPrefab;

        private List<GameObject> m_current = new List<GameObject>();
        System.Action<int> m_callback;

        public void SetQuestions(BotQuestion[] questions, System.Action<int> callback)
        {
            //need to create the questions
            Clear();

            m_callback = callback;

            for (int i = 0; i < questions.Length; i++)
            {
                GameObject go = Instantiate(questionPrefab, Vector3.zero, Quaternion.identity, questionContainer);
                go.transform.localScale = Vector3.one;
                go.transform.localPosition = Vector3.zero;
                go.transform.localEulerAngles = Vector3.zero;
                go.GetComponentInChildren<TMPro.TextMeshProUGUI>(true).text = questions[i].question;
                Button button = go.GetComponentInChildren<Button>(true);
                int n = i;
                button.onClick.AddListener(() => { AskQuestion(n); });
                go.SetActive(true);

                m_current.Add(go);
            }

            if(m_current.Count > 0)
            {
                gameObject.SetActive(true);
            }
        }

        private void AskQuestion(int question)
        {
            gameObject.SetActive(false);

            if(m_callback != null)
            {
                m_callback.Invoke(question);
            }
        }

        private void Clear()
        {
            for(int i = 0; i < m_current.Count; i++)
            {
                Destroy(m_current[i]);
            }

            m_current.Clear();
        }

        [System.Serializable]
        public class BotQuestion
        {
            public string question;
            public string response;
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(NPCBotQuestions), true)]
        public class NPCBotQuestions_Editor : BaseInspectorEditor
        {
            private void OnEnable()
            {
                GetBanner();
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();


                serializedObject.Update();

                EditorGUILayout.PropertyField(serializedObject.FindProperty("questionContainer"), true);

                EditorGUILayout.PropertyField(serializedObject.FindProperty("questionPrefab"), true);

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
