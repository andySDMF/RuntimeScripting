using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{

    [RequireComponent(typeof(NPCBot))]
    public class NPCBotFollow : MonoBehaviour
    {
        [SerializeField]
        private Transform follow;

        [SerializeField]
        private float distanceFromObject = 2.0f;

        private bool m_follow = false;
        private Vector3 m_target;
        private float m_distance = 0.0f;
        private NPCBot m_bot;
        private float m_speed = 1.0f;
        private Vector3 m_rot;
        private Vector3 m_pos;
        private Coroutine m_processIdle;

        private bool m_isLookAt = false;
        private Dictionary<string, bool> m_animationsBooleans = new Dictionary<string, bool>();

        public bool HasReachedTarget
        {
            get
            {
                if (Vector3.Distance(transform.position, m_target) <= m_distance)
                {
                    return true;
                }

                return false;
            }
        }

        private void Start()
        {
            if (m_bot == null)
            {
                m_bot = GetComponent<NPCBot>();
            }

            m_rot = m_bot.Ani.transform.localEulerAngles;
            m_pos = m_bot.Ani.transform.localPosition;
        }

        private void Update()
        {
            if (CoreManager.Instance.CurrentState == state.Running)
            {
                if (m_isLookAt)
                {
                    transform.LookAt(follow, Vector3.up);
                    return;
                }

                if (m_follow)
                {
                    //only move if follow object is moving
                    if (follow != null)
                    {
                        m_target = follow.position;
                        m_distance = distanceFromObject;
                    }

                    if (Vector3.Distance(transform.position, m_target) >= m_distance && !m_bot.EncounteredObstacle)
                    {
                        transform.position = Vector3.MoveTowards(transform.position, m_target, m_speed * Time.deltaTime);
                        transform.LookAt(follow, Vector3.up);

                        if (m_processIdle != null)
                        {
                            StopCoroutine(m_processIdle);
                            m_processIdle = null;
                        }

                        if (m_bot.Ani != null)
                        {
                            if (m_animationsBooleans.Count > 0)
                            {
                                foreach (KeyValuePair<string, bool> ani in m_animationsBooleans)
                                {
                                    m_bot.Ani.SetBool(ani.Key, ani.Value);
                                }
                            }
                            else
                            {
                                m_bot.Ani.SetBool("Moved", true);
                            }
                        }
                    }
                    else
                    {
                        if (m_processIdle == null)
                        {
                            m_processIdle = StartCoroutine(DelayPause());
                        }
                    }
                }

                if (!NPCManager.Instance.BotExistsInCollection(m_bot))
                {
                    m_bot.Avatar.localEulerAngles = m_rot;
                    m_bot.Avatar.localPosition = m_pos;
                }
            }
        }

        public void ForceLookAt(Transform t, bool updateThis = false)
        {
            m_isLookAt = updateThis;

            if (m_isLookAt)
            {
                follow = t;
            }
            else
            {
                follow = null;
            }

            transform.LookAt(t, Vector3.up);
        }

        public void Follow(Vector3 vec, float speed = 1.0f)
        {
            StopAllCoroutines();

            m_speed = speed;
            follow = null;
            m_target = vec;
            m_distance = 0.0f;

            StartCoroutine(DelayFollow());
        }

        public void Follow(Transform t, float speed = 1.0f, Dictionary<string, bool> animationBooleans = null)
        {
            StopAllCoroutines();

            if (animationBooleans != null)
            {
                m_animationsBooleans.Clear();

                foreach (KeyValuePair<string, bool> ani in animationBooleans)
                {
                    m_animationsBooleans.Add(ani.Key, ani.Value);
                }
            }

            ForceLookAt(t);
            m_speed = speed;
            follow = t;
            m_target = follow.position;
            m_distance = distanceFromObject;

            StartCoroutine(DelayFollow());
        }

        public void Stop()
        {
            m_follow = false;
            m_distance = 0.0f;
            follow = null;

            if (m_animationsBooleans.Count > 0)
            {
                foreach (KeyValuePair<string, bool> ani in m_animationsBooleans)
                {
                    m_bot.Ani.SetBool(ani.Key, !ani.Value);
                }
            }

            m_animationsBooleans.Clear();

            if (NPCManager.Instance.BotExistsInCollection(m_bot))
            {
                m_bot.ResetOnNavMesh(NPCManager.Instance.GetClosestPostionOnNavMesh(transform.position));
            }
        }

        private IEnumerator DelayFollow()
        {
            yield return new WaitForSeconds(1.0f);

            m_follow = true;
        }

        private IEnumerator DelayPause()
        {
            yield return new WaitForSeconds(1.0f);

            if (m_bot.Ani != null)
            {
                if (m_animationsBooleans.Count > 0)
                {
                    foreach (KeyValuePair<string, bool> ani in m_animationsBooleans)
                    {
                        m_bot.Ani.SetBool(ani.Key, !ani.Value);
                    }
                }
                else
                {
                    m_bot.Ani.SetBool("Moved", false);
                }
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(NPCBotFollow), true)]
        public class NPCBotFollow_Editor : BaseInspectorEditor
        {
            private void OnEnable()
            {
                GetBanner();
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();


                serializedObject.Update();

                EditorGUILayout.PropertyField(serializedObject.FindProperty("follow"), true);

                EditorGUILayout.PropertyField(serializedObject.FindProperty("distanceFromObject"), true);

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