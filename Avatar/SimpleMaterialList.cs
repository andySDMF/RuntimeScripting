using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class SimpleMaterialList : MonoBehaviour
    {
		[Header("Physical Objects")]
		public GameObject skin;
		public GameObject[] hair;

		public GameObject[] earings;
		public GameObject[] glasses;
		public GameObject[] eyes;

		public GameObject[] tops;
		public GameObject[] bottoms;
		public GameObject[] shoes;

		[Header("Colors")]
		public Color[] skinColors;
		public Color[] fixedColors;

#if UNITY_EDITOR
		[CustomEditor(typeof(SimpleMaterialList), true)]
		public class SimpleMaterialList_Editor : BaseInspectorEditor
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
					base.OnInspectorGUI();
				}
			}
		}
#endif
	}
}
