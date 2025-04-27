using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace BrandLab360
{
    public class AppConstReferences : ScriptableObject
    {
        [SerializeField]
        private AppSettings settings;
        [SerializeField]
        private AppInstances instances;

        public AppSettings Settings
        {
            get
            {
                if(settings == null)
                {
                    settings = Resources.Load<AppSettings>("ProjectAppSettings");

#if UNITY_EDITOR
                    EditorUtility.SetDirty(this);
                    AssetDatabase.SaveAssets();
#endif

                }

                return settings;
            }
        }

        public AppInstances Instances
        {
            get
            {
                if (instances == null)
                {
                    instances = Resources.Load<AppInstances>("ProjectAppInstances");
#if UNITY_EDITOR
                    EditorUtility.SetDirty(this);
                    AssetDatabase.SaveAssets();
#endif
                }

                return instances;
            }
        }

#if UNITY_EDITOR
        public bool OpenAppSetting()
        {
            string path = OpenFile("Settings");
            path = path.Replace(Application.dataPath, "Assets");
            AppSettings temp = AssetDatabase.LoadAssetAtPath<AppSettings>(path);

            if (temp != null)
            {
                settings = temp;
                return true;
            }
            else
            {
                EditorUtility.DisplayDialog("Warning", "Asset is not of type AppSettings", "OK");
            }

            return false;
        }

        public bool OpenAppInstances()
        {
            string path = OpenFile("Instances");
            path = path.Replace(Application.dataPath, "Assets");
            AppInstances temp = AssetDatabase.LoadAssetAtPath<AppInstances>(path);

            if(temp != null)
            {
                instances = temp;
                return true;
            }
            else
            {
                EditorUtility.DisplayDialog("Warning", "Asset is not of type AppInstances", "OK");
            }

            return false;
        }

        private string OpenFile(string setting)
        {
            return EditorUtility.OpenFilePanel("Select [" + setting + "]", Application.dataPath + "/Resources/", "asset");
        }
#endif
    }
}
