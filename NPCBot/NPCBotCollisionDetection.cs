using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class NPCBotCollisionDetection : MonoBehaviour
    {
        /// <summary>
        /// Access to the state of this collision detection
        /// </summary>
        public bool HasCollided
        {
            get
            {
                return m_triggerCount.Count > 0;
            }
        }

        public Vector3 Size
        {
            get
            {
                return transform.localScale;
            }
        }

        private List<Collider> m_triggerCount = new List<Collider>();
        private bool m_process = true;

        public List<Transform> Obstacles = new List<Transform>();

        public bool Enabled { get; set; }

        public void OnTriggerEnter(Collider other)
        {
            if (!m_process) return;

            if (!Enabled) return;

            m_triggerCount.Add(other);
            Obstacles.Add(other.transform);
        }

        public void OnTriggerExit(Collider other)
        {
            if (!m_process) return;

            if (!Enabled) return;

            m_triggerCount.Remove(other);
            Obstacles.Remove(other.transform);
        }

        /// <summary>
        /// Action to clear the remaining trigger objects
        /// </summary>
        public void Clear()
        {
            m_triggerCount.Clear();
            Obstacles.Clear();
            m_process = false;
            GetComponent<Collider>().enabled = false;
            StartCoroutine(DelayProcess());
        }

        /// <summary>
        /// Delay the trigger events
        /// </summary>
        /// <returns></returns>
        private IEnumerator DelayProcess()
        {
            yield return new WaitForSeconds(0.5f);
            m_process = true;
            GetComponent<Collider>().enabled = true;
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(NPCBotCollisionDetection), true)]
        public class NPCBotCollisionDetection_Editor : BaseInspectorEditor
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
