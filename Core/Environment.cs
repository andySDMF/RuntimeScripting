using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class Environment : MonoBehaviour
    {
        private void Awake()
        {
            // Removing this because it's slow - Luke
            // 
            //need to find all object that have environment tag and attach to this
            //GameObject[] all = GameObject.FindGameObjectsWithTag("Environment");

            //for(int i = 0; i < all.Length; i++)
            //{
            //    all[i].transform.SetParent(transform);
            //}
        }

        public void Activate(bool enabled)
        {
            gameObject.SetActive(enabled);
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(Environment), true)]
        public class Environment_Editor : BaseInspectorEditor
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
