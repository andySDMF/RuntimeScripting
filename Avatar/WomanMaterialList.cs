using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
	public class WomanMaterialList : MonoBehaviour
	{
		[Header("Physical Objects")]
		public GameObject[] faceType = new GameObject[5];
		public GameObject BodyObject;
		public GameObject LegsObject;
		public GameObject[] TopObjects = new GameObject[2];
		public GameObject[] BottomObjects = new GameObject[2];
		public GameObject[] hairMainObjects = new GameObject[5];
		public GameObject shoesObject;
		public GameObject eyes_Object;

		[Header("Materials")]
		public Material[] eyeColors = new Material[6];
		public Material[] body_Materials = new Material[5];
		public Material[] face_Materials = new Material[5];

		public Material[] JacketMaterials = new Material[6];
		public Material[] ShirtMaterials = new Material[6];

		public Material[] SkirtMaterials = new Material[6];
		public Material[] PantsMaterials = new Material[6];
		public Material[] ShoesMaterials = new Material[6];

		public Material[] hairA_Materials = new Material[5];
		public Material[] hairA_FadeMaterials = new Material[5];

		//HairB Materials and Objects
		public Material[] hairB_Materials = new Material[5];
		public Material[] hairB_FadeMaterials = new Material[5];

		//HairC Materials and Objects
		public Material[] hairC_Materials = new Material[5];
		public Material[] hairC_FadeMaterials = new Material[5];

		//HairD Materials and Objects
		public Material[] hairD_Materials = new Material[5];
		public Material[] hairD_FadeMaterials = new Material[5];

#if UNITY_EDITOR
		[CustomEditor(typeof(WomanMaterialList), true)]
		public class WomanMaterialList_Editor : BaseInspectorEditor
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
