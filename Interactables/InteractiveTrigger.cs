using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    [RequireComponent(typeof(BoxCollider))]
    public class InteractiveTrigger : MonoBehaviour
    {
        [SerializeField]
        private InteractionEvent eventType = InteractionEvent._UnityEvent;

        [SerializeField]
        private UnityEvent eventActionEntered;

        [SerializeField]
        private bool useExitTriggerEvent = false;
        [SerializeField]
        private UnityEvent eventActionExit;

        [SerializeField]
        private Component component;

        [SerializeField]
        private bool showPrompt = true;
        [SerializeField]
        private string promptMessage = "";

        [SerializeField]
        private bool playPromptAudio = false;
        [SerializeField]
        private AudioClip promptAudio;

        [SerializeField]
        private bool hideMeshOnStart = true;

        private bool m_performedAction = false;
        private PromptMessagePanel m_promptMP;
        private Collider m_col;
        private bool m_processExit = false;

        private void Start()
        {
            m_col = GetComponent<BoxCollider>();
            m_col.isTrigger = true;

            if(GetComponent<MeshRenderer>())
            {
                GetComponent<MeshRenderer>().enabled = !hideMeshOnStart;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if(other.transform.Equals(PlayerManager.Instance.GetLocalPlayer().TransformObject) && !m_processExit)
            {
                if (m_performedAction) return;

                m_performedAction = true;

                if (playPromptAudio)
                {
                    SoundManager.Instance.PlaySound(promptAudio);
                }

                if (showPrompt)
                {
                    m_promptMP = HUDManager.Instance.GetHUDMessageObject("PROMPT_MESSAGE").GetComponentInChildren<PromptMessagePanel>(true);
                    m_promptMP.Set(promptMessage, OnPromptCallback);
                    HUDManager.Instance.ToggleHUDMessage("PROMPT_MESSAGE", true);
                    m_col.enabled = false;
                }
                else
                {
                    Perform(true);
                }

                AnalyticsManager.Instance.PostAnalyticsEvent(EventCategory.Content, EventAction.Enter, gameObject.name);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.transform.Equals(PlayerManager.Instance.GetLocalPlayer().TransformObject) && !PlayerManager.Instance.GetLocalPlayer().FreezePosition)
            {
                if (!useExitTriggerEvent)
                {
                    if(!m_processExit)
                    {
                        m_processExit = true;
                        StartCoroutine(Delay());
                    }

                    return;
                }

                StartCoroutine(Wait());
            }
        }

        private IEnumerator Delay()
        {
            yield return new WaitForSeconds(2.0f);

            m_processExit = false;
            m_performedAction = false;
        }

        private IEnumerator Wait()
        {
            yield return new WaitForSeconds(0.1f);

            if (m_promptMP.gameObject.activeInHierarchy) yield break;

            m_performedAction = false;

            Perform(false);
        }

        private void OnPromptCallback(bool responce)
        {
            if(responce)
            {
                Perform(true);

                m_col.enabled = true;
            }
        }

        private void Perform(bool trigger)
        {
            if(eventType.Equals(InteractionEvent._UnityEvent))
            {
                if(trigger)
                {
                    eventActionEntered.Invoke();
                }
                else
                {
                    eventActionExit.Invoke();
                }
            }
            else
            {
                if(component.gameObject.GetComponent<UniqueID>())
                {
                    string user = PlayerManager.Instance.GetLocalPlayer().CustomizationData.ContainsKey("USERTYPE") ? PlayerManager.Instance.GetLocalPlayer().CustomizationData["USERTYPE"].ToString() : "";

                    if (!component.gameObject.GetComponent<UniqueID>().CanUserControlThis(user))
                    {
                        return;
                    }
                }

                //will need to add more but for now will door
                if (component.gameObject.GetComponent<Door>())
                {
                    Door comp = component.gameObject.GetComponent<Door>();

                    if (trigger)
                    {
                        if (!comp.IsOpen)
                        {
                            comp.Open();
                        }
                    }
                    else
                    {
                        if (comp.IsOpen)
                        {
                            comp.Close();
                        }
                    }
                }
                else if (component.gameObject.GetComponent<PickupItem>())
                {
                    if (trigger)
                    {
                        PickupItem comp = component.gameObject.GetComponent<PickupItem>();

                        if (ItemManager.Instance.IsHolding)
                        {
                            ItemManager.Instance.Drop3D();
                        }

                        comp.Pickup();
                    }
                }
                else if (component.gameObject.GetComponent<DropPoint>())
                {
                    if(trigger)
                    {
                        DropPoint comp = component.gameObject.GetComponent<DropPoint>();

                        if (ItemManager.Instance.IsHolding)
                        {
                            ItemManager.Instance.PlaceCurrent(comp);
                        }
                    }
                }
            }
        }

        [System.Serializable]
        private enum InteractionEvent { _UnityEvent, _Component}

#if UNITY_EDITOR
        [CustomEditor(typeof(InteractiveTrigger), true), CanEditMultipleObjects]
        public class InteractiveTrigger_Editor : BaseInspectorEditor
        {
            private InteractiveTrigger script;

            private void OnEnable()
            {
                script = (InteractiveTrigger)target;
                GetBanner();
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();
                serializedObject.Update();

                EditorGUILayout.PropertyField(serializedObject.FindProperty("showPrompt"), true);

                if(script.showPrompt)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("promptMessage"), true);

                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("playPromptAudio"), true);

                    if (script.playPromptAudio)
                    {
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("promptAudio"), true);
                    }

                    EditorGUILayout.Space();
                }
                
                if (script.gameObject.GetComponent<MeshRenderer>())
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("hideMeshOnStart"), true);
                }
                
                EditorGUILayout.PropertyField(serializedObject.FindProperty("eventType"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("useExitTriggerEvent"), true);

                if (script.eventType.Equals(InteractionEvent._UnityEvent))
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("eventActionEntered"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("eventActionExit"), true);
                }
                else
                {
                    EditorGUILayout.LabelField("Currently handles; Door, items, droppoint, else WIP", EditorStyles.miniLabel);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("component"), true);
                }

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
