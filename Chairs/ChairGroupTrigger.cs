using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    [RequireComponent(typeof(Collider))]
    public class ChairGroupTrigger : MonoBehaviour
    {
        public System.Action<IPlayer> OnTriggerEvent { get; set; }

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        public void OnTriggerEnter(Collider other)
        {
            IPlayer player = other.transform.GetComponent<IPlayer>();

            if (player != null && player.IsLocal)
            {
                if(OnTriggerEvent != null)
                {
                    OnTriggerEvent.Invoke(player);
                    gameObject.SetActive(false);
                }
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(ChairGroupTrigger), true)]
        public class ChairGroupTrigger_Editor : BaseInspectorEditor
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
