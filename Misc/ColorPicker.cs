using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace BrandLab360
{
    [RequireComponent(typeof(Image))]
    public class ColorPicker : MonoBehaviour, IPointerExitHandler, IPointerDownHandler, IDragHandler
    {
        [SerializeField]
        private GameObject colorList;
        [SerializeField]
        private GameObject colorOption;

        private Texture2D m_chart;
        private MaskableGraphic m_graphic;
        private RectTransform m_rectT;
        private RectTransform m_cursor;
        private Image m_colorChart;

        private PickerType m_type = PickerType.Wheel;

        private void Awake()
        {
            if (m_rectT == null)
            {
                m_rectT = GetComponent<RectTransform>();
            }

            m_colorChart = GetComponent<Image>();
            m_chart = m_colorChart.sprite.texture;
            m_cursor = transform.GetChild(0).GetComponent<RectTransform>();

            switch (m_type)
            {
                case PickerType.Wheel:
                    if (m_chart != null && m_graphic != null)
                    {
                        SetCursorPositionOnOpen();
                    }
                    break;
                case PickerType.List:
                    break;
                default:
                    break;
            }
        }

        private void OnDisable()
        {
            m_graphic = null;

            switch (m_type)
            {
                case PickerType.Wheel:
                    break;
                case PickerType.List:
                    GridLayoutGroup gl = colorList.GetComponentInChildren<GridLayoutGroup>();

                    for(int i = gl.transform.childCount - 1; i > 0; i--)
                    {
                        Destroy(gl.transform.GetChild(i).gameObject);
                    }

                    colorList.SetActive(false);
                    break;
                default:
                    break;
            }
        }

        public void SetGraphic(MaskableGraphic graphic, PickerDefaults defaults)
        {
            m_graphic = graphic;

            if(m_rectT == null)
            {
                m_rectT = GetComponent<RectTransform>();
            }

            Vector3 pos = InputManager.Instance.GetMousePosition();
            m_rectT.position = new Vector3(pos.x - m_rectT.rect.width / 2, pos.y - m_rectT.rect.height / 2, 0.0f);

            m_type = defaults.type;

            switch (m_type)
            {
                case PickerType.Wheel:
                    if (m_chart != null && m_graphic != null)
                    {
                        SetCursorPositionOnOpen();
                    }
                    break;
                case PickerType.List:
                    colorList.SetActive(true);
                    GridLayoutGroup gl = colorList.GetComponentInChildren<GridLayoutGroup>();

                    for (int i = 0; i < defaults.colors.Count; i++)
                    {
                        GameObject go = Instantiate(colorOption, Vector3.zero, Quaternion.identity, gl.transform);
                        go.transform.localScale = Vector3.one;
                        go.SetActive(true);
                        go.transform.GetChild(0).GetComponent<Image>().color = defaults.colors[i];

                        Color col = defaults.colors[i];
                        go.GetComponent<Button>().onClick.AddListener(() => { OnListColorPick(col); });
                    }

                    m_graphic.color = defaults.colors[0];

                    break;
                default:
                    break;
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            gameObject.SetActive(false);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if(m_type.Equals(PickerType.Wheel))
            {
                m_cursor.position = eventData.position;
                GetColor();
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (m_type.Equals(PickerType.Wheel))
            {
                float xpos = m_cursor.localPosition.x;
                float ypos = m_cursor.localPosition.y;

                m_cursor.position = eventData.position;

                if (m_cursor.localPosition.x > m_colorChart.rectTransform.rect.width || m_cursor.localPosition.x < 0.0f)
                {
                    m_cursor.localPosition = new Vector3(xpos, m_cursor.localPosition.y);
                }

                if (m_cursor.localPosition.y > m_colorChart.rectTransform.rect.height || m_cursor.localPosition.y < 0.0f)
                {
                    m_cursor.localPosition = new Vector3(m_cursor.localPosition.x, ypos);
                }

                GetColor();
            }
        }

        private void OnListColorPick(Color col)
        {
            Debug.Log("working");
            m_graphic.color = col;
        }

        private void GetColor()
        {
            Color colPick = m_chart.GetPixel((int)(m_cursor.localPosition.x * (m_chart.width / m_colorChart.rectTransform.rect.width)), (int)(m_cursor.localPosition.y * (m_chart.height / m_colorChart.rectTransform.rect.height)));
            m_graphic.color = new Color(colPick.r, colPick.g, colPick.b, 1.0f);
        }

        private void SetCursorPositionOnOpen()
        {
            for (int x = 0; x < m_chart.width; x++)
            {
                for (int y = 0; y < m_chart.height; y++)
                {
                    if (m_chart.GetPixel(x, y) == m_graphic.color)
                    {
                        m_cursor.localPosition = new Vector3(x, y, 0.0f);
                        break;
                    }
                }
            }

            GetColor();
        }

        [System.Serializable]
        public class PickerDefaults
        {
            public PickerType type = PickerType.Wheel;
            public List<Color> colors = new List<Color>();
        }

        [System.Serializable]
        public enum PickerType { Disabled, Wheel, List }

#if UNITY_EDITOR
        [CustomEditor(typeof(ColorPicker), true)]
        public class ColorPicker_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("colorList"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("colorOption"), true);

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
