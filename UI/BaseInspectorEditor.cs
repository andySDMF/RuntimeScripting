using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
#if UNITY_EDITOR
    public class BaseInspectorEditor : UnityEditor.Editor
    {
        private Sprite m_banner;

        protected void GetBanner()
        {
            AppConstReferences reference = Resources.Load<AppConstReferences>("AppConstReferences");

            if(reference != null)
            {
                //need to draw banner
                if (reference.Settings.brandlabLogo_Inspector == null)
                {
                    reference.Settings.brandlabLogo_Inspector = (Sprite)GetAsset<Sprite>("Assets/com.brandlab360.core/Editor/Sprites/BrandLab360_Inspector.png");
                }


                m_banner = reference.Settings.brandlabLogo_Inspector;
            }
        }

        protected void DisplayBanner()
        {
            if (m_banner != null)
            {
                GUILayout.Box(m_banner.texture);
            }
        }

        private Object GetAsset<T>(string path)
        {
            Object obj = AssetDatabase.LoadAssetAtPath(path, typeof(T));

            if (obj == null)
            {
                obj = GetPackageAsset<T>(path);
            }

            return obj;
        }

        private Object GetPackageAsset<T>(string path)
        {
            return AssetDatabase.LoadAssetAtPath(path.Replace("Assets", "Packages"), typeof(T));
        }
    }
#endif
}
