using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class OrientationAvatarSceneListener : MonoBehaviour
    {
        [SerializeField]
        private GridLayoutGroup colorPallete;

        [SerializeField]
        private GridLayoutGroup accessoriesPallete;

        [SerializeField]
        private RectTransform avatarSelectionLayout;


        private void Start()
        {
            if (!AppManager.IsCreated) return;

            if (OrientationManager.Instance.RecievedWebResponse)
            {
                Vector2 size = OrientationManager.Instance.ScreenSize;
                OnOrientationChange(OrientationManager.Instance.CurrentOrientation, (int)size.x, (int)size.y);
            }
        }

        public void OnOrientationChange(OrientationType orientation, int width, int height)
        {
            if (orientation.Equals(OrientationType.portrait))
            {
                colorPallete.cellSize = new Vector2(100, 100);
                accessoriesPallete.cellSize = new Vector2(125, 125);

                avatarSelectionLayout.offsetMax = new Vector2(-100, -100);
                avatarSelectionLayout.offsetMin = new Vector2(100, 100);
            }
            else
            {
                colorPallete.cellSize = new Vector2(50, 50);
                accessoriesPallete.cellSize = new Vector2(80, 80);

                avatarSelectionLayout.offsetMax = new Vector2(-400, -100);
                avatarSelectionLayout.offsetMin = new Vector2(400, 100);
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(OrientationAvatarSceneListener), true)]
        public class OrientationAvatarSceneListener_Editor : BaseInspectorEditor
        {
            private void OnEnable()
            {
                GetBanner();
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();

                if (Application.productName.Equals("BL360 Plugin"))
                {
                    serializedObject.Update();

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("colorPallete"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("accessoriesPallete"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("avatarSelectionLayout"), true);

                    if (GUI.changed)
                    {
                        serializedObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(this);
                    }
                }
            }
        }
#endif
    }
}
