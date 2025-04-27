using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class VoiceChatTrigger : MonoBehaviour
    {
        public string voiceChatId;

        private void OnTriggerEnter(Collider other)
        {
            if(other.CompareTag("Player"))
            {
                if (other.GetComponent<MMOPlayer>().view.IsMine)
                {
                    MMOChat.Instance.JoinVoiceAreaTrigger(voiceChatId);

                    AnalyticsManager.Instance.PostAnalyticsEvent(EventCategory.Location, EventAction.Enter, "Voice Chat " + gameObject.name);
                }
            }
        }


        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                if (other.GetComponent<MMOPlayer>().view.IsMine)
                {
                    MMOChat.Instance.LeaveVoiceAreaTrigger(voiceChatId);
                }
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(VoiceChatTrigger), true)]
        public class VoiceChatTrigger_Editor : BaseInspectorEditor
        {
            private void OnEnable()
            {
                GetBanner();
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();


                serializedObject.Update();

                EditorGUILayout.PropertyField(serializedObject.FindProperty("voiceChatId"), true);


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
