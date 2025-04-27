using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360 
{
    public class EndAnimation : MonoBehaviour
    {
        public bool ReleasePlayerOnEmoteEnd { get; set; }
        public System.Action OnEmoteEnd { get; set; }

        private void Awake()
        {
            ReleasePlayerOnEmoteEnd = true;
        }

        public void End()
        {
            if(GetComponentInParent<IPlayer>() != null && ReleasePlayerOnEmoteEnd)
            {
                if(GetComponentInParent<IPlayer>().IsLocal)
                {
                    PlayerManager.Instance.FreezePlayer(false);
                }
            }

            ReleasePlayerOnEmoteEnd = true;

            if(OnEmoteEnd != null)
            {
                OnEmoteEnd.Invoke();
            }

            OnEmoteEnd = null;
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(EndAnimation), true)]
        public class EndAnimation_Editor : BaseInspectorEditor
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
