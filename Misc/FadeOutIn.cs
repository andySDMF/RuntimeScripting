using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    [RequireComponent(typeof(CanvasGroup))]
    public class FadeOutIn : MonoBehaviour
    {
        [SerializeField]
        private float duration = 1.0f;

        private CanvasGroup m_canvasGroup;

        /// <summary>
        /// the amount of time the fade is paused until complete
        /// </summary>
        public float PauseTime { get; set; }

        /// <summary>
        /// Global access to the type of fade is used, change before setting GO active
        /// </summary>
        public FadeAction FadeType
        {
            get
            {
                return m_action;
            }
            set
            {
                m_action = value;

                ChangeAlpha();
            }
        }

        private FadeAction m_action = FadeAction.Out_In;

        /// <summary>
        /// The action to subscribe to, to call whilst the faded out.
        /// This is only used during fade type OUT/IN
        /// </summary>
        public System.Action ImplementChange { get; set; }
        /// <summary>
        /// The callback action to subscribe to, to be used after the fade has completed
        /// </summary>
        public System.Action Callback { get; set; }

        private void Awake()
        {
            if (m_canvasGroup == null)
            {
                m_canvasGroup = GetComponent<CanvasGroup>();
            }
        }

        private void OnEnable()
        {
            //change alpha depeneding on fade type, then fade
            ChangeAlpha();
            StartCoroutine(Fade());
        }

        private void OnDisable()
        {
            StopAllCoroutines();
        }

        /// <summary>
        /// Local function to change the setup of the fade
        /// </summary>
        private void ChangeAlpha()
        {
            if (m_canvasGroup == null)
            {
                m_canvasGroup = GetComponentInChildren<CanvasGroup>(true);
            }

            //switch vanvas group alpha
            switch (FadeType)
            {
                case FadeAction.Out:
                    m_canvasGroup.alpha = 0.0f;
                    break;
                case FadeAction.In:
                    m_canvasGroup.alpha = 1.0f;
                    break;
                default:
                    m_canvasGroup.alpha = 0.0f;
                    break;
            }
        }

        /// <summary>
        /// Called to start the fade externally if GO is on
        /// </summary>
        public void PerformFade()
        {
            if (!gameObject.activeInHierarchy) return;

            StartCoroutine(Fade());
        }

        /// <summary>
        /// Local call to action the fade
        /// </summary>
        /// <returns></returns>
        private IEnumerator Fade()
        {
            //tween settings
            float runningTime = 0.0f;
            float percentage = 0.0f;
            float time = (FadeType.Equals(FadeAction.Out_In)) ? duration / 2 : duration;

            //fade out
            if (FadeType.Equals(FadeAction.Out) || FadeType.Equals(FadeAction.Out_In))
            {
                while (percentage < 1.0f)
                {
                    runningTime += Time.deltaTime;
                    percentage = runningTime / time;

                    m_canvasGroup.alpha = Mathf.Lerp(0.0f, 1.0f, percentage);

                    yield return null;
                }
            }

            //call the implement action
            if (ImplementChange != null)
            {
                ImplementChange.Invoke();
            }

            //pause
            yield return new WaitForSeconds(PauseTime);

            //reset settings
            runningTime = 0.0f;
            percentage = 0.0f;

            //fade in
            if (FadeType.Equals(FadeAction.In) || FadeType.Equals(FadeAction.Out_In))
            {
                while (percentage < 1.0f)
                {
                    runningTime += Time.deltaTime;
                    percentage = runningTime / time;

                    m_canvasGroup.alpha = Mathf.Lerp(1.0f, 0.0f, percentage);

                    yield return null;
                }
            }

            //callback
            if (Callback != null)
            {
                Callback.Invoke();
            }

            Callback = null;
            ImplementChange = null;

            //deactive GO
            if (FadeType.Equals(FadeAction.In) || FadeType.Equals(FadeAction.Out_In))
            {
                gameObject.SetActive(false);
            }
        }

        [SerializeField]
        public enum FadeAction { Out_In, Out, In }

#if UNITY_EDITOR
        [CustomEditor(typeof(FadeOutIn), true)]
        public class FadeOutIn_Editor : BaseInspectorEditor
        {
            private void OnEnable()
            {
                GetBanner();
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();

                serializedObject.Update();

                EditorGUILayout.LabelField("Behaviour", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("duration"), true);
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
