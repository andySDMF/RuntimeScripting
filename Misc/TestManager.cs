using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class TestManager : MonoBehaviour
    {
        private void Start()
        {
            if(AppManager.IsCreated)
            {
                StartCoroutine(Wait());
            }
        }

        private IEnumerator Wait()
        {
            while(!AppManager.Instance.Data.RoomEstablished)
            {
                yield return null;
            }

            yield return new WaitForSeconds(3.0f);

            //You can add anything here to test on start
            //////////////////////////////////////////////////////////////////////////////////////
            ///
            /*string str = "This is a hint test";

            for(int i = 0; i < 800; i++)
            {
                str += "d";
            }

            PopupManager.instance.ShowPopUp("Hint Test", str, "OK", null , null, "www.google.co.uk");*/

            //PopupManager.instance.ShowHint("Hint Test", "This is a hint test", 20.0f, null);
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(TestManager), true)]
        public class TestManager_Editor : BaseInspectorEditor
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
