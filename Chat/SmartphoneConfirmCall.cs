using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class SmartphoneConfirmCall : MonoBehaviour
    {
        private IPlayer m_player;

        public void Set(IPlayer player)
        {
            m_player = player;
            gameObject.SetActive(true);
        }

        public void StartCall()
        {
            MMOChat.Instance.DialVoiceCall(m_player.ID);
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(SmartphoneConfirmCall), true)]
        public class SmartphoneConfirmCall_Editor : BaseInspectorEditor
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
