using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
	public class ManMaterialList : MonoBehaviour
	{
		[Header("Physical Objects")]
		public GameObject EyeObj;
		public GameObject BeltObj;
		public GameObject ButterflyTieObj;
		public GameObject ClosedJacketObj;
		public GameObject ClosedShirtObj;
		public GameObject ClosedTieObj;
		public GameObject ClosedWaistcoatObj;
		public GameObject OpenJacketObj;
		public GameObject OpenShirtObj;
		public GameObject OpenTieWObj;
		public GameObject OpenWaistcoatObj;
		public GameObject PantsObj;
		public GameObject PantsLoopsObj;
		public GameObject ShirtObj;
		public GameObject ShirtWObj;
		public GameObject ShoesObj;
		public GameObject TieObj;
		public GameObject WaistcoatObj;
		public GameObject OpenHandkerchiefObj;
		public GameObject ClosedHandkerchiefObj;

		public GameObject[] FaceObjects = new GameObject[5];
		public GameObject[] HairObjects = new GameObject[8];
		public GameObject[] GlassesObjects = new GameObject[5];

		[Header("Materials")]
		public Material[] Skin_Materials = new Material[5];
		public Material[] Eye_Materials = new Material[6];
		public Material[] Glass_Materials = new Material[3];
		public Material[] HairA_Materials = new Material[5];
		public Material[] HairB_Materials = new Material[5];
		public Material[] HairC_Materials = new Material[5];
		public Material[] HairD_Materials = new Material[5];
		public Material[] HairE_Materials = new Material[5];
		public Material[] HairF_Materials = new Material[5];
		public Material[] HairG_Materials = new Material[5];
		public Material[] HairH_Materials = new Material[5];
		public Material[] HairI_Materials = new Material[5];
		public Material[] JacketMaterials = new Material[10];
		public Material[] PantsMaterials = new Material[10];
		public Material[] ShirtMaterials = new Material[8];
		public Material[] ShoesMaterials = new Material[4];
		public Material[] TieMaterials = new Material[10];
		public Material[] WaistcoatMaterials = new Material[10];
		public Material[] HandkerchiefMaterials = new Material[6];

#if UNITY_EDITOR
		[CustomEditor(typeof(ManMaterialList), true)]
		public class ManMaterialList_Editor : BaseInspectorEditor
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
