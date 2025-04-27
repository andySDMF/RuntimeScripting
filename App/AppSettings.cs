using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace BrandLab360
{
    [CreateAssetMenu(fileName = "ProjectAppSettings", menuName = "ScriptableObjects/ProjectAppSettings", order = 1)]
    public class AppSettings : ScriptableObject
    {
        [Header("Editor Window")]
        public Sprite brandlabLogo;
        public Sprite brandlabLogo_Banner;
        public Sprite brandlabLogo_Inspector;

        [Header("Editor Tools")]
        public EditorTools editorTools;

        [Header("Runtime Settings")]
        public ProjectSettings projectSettings;
        public PlayerControlSettings playerSettings;
        public AKFSettings AFKSettings;
        public ChatSettings chatSettings;
        public HUDSettings HUDSettings;
        public ThemeSettings themeSettings;
        public NPCSettings NPCSettings;
        public SocialMediaSettings socialMediaSettings;

#if UNITY_EDITOR
        [HideInInspector]
        public bool editorShowAdvancedSettings = false;
#endif
    }
}
