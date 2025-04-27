using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Text.RegularExpressions;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class WomanCustomise : MonoBehaviour, ICustomAvatar
    {
		[SerializeField]
		private AvatarType type = AvatarType.Standard;

		public FaceType faceType;
		public SkinType skinType;
		public EyeColor eyeColor;
		public TopType topType;
		public JacketColor jacketColor;
		public ShirtColor shirtColor;
		public BottomType bottomType;
		public PantsColor pantsColor;
		public SkirtColor skirtColor;
		public HairType hairType;
		public HairColor hairColor;
		public ShoesColor shoesColor;

		private WomanMaterialList materialsList;

		public AvatarType Type { get { return type; } }

		public Color[] FixedColors { get { return null; } }

		/// <summary>
		/// Public access to the this avatar customise actions
		/// </summary>
		public AvatarCustomiseActions CustomiseAction
        {
			get;
			private set;
        }

		/// <summary>
		/// Public access to this avatars current customise settings
		/// </summary>
		public AvatarCustomiseSettings Settings
        {
            get
            {
				return GetSettings();
            }
        }

		/// <summary>
		/// state what sex this script is
		/// </summary>
		public CustomiseAvatar.Sex Sex
		{
			get
			{
				return CustomiseAvatar.Sex.Female;
			}
		}

		/// <summary>
		/// Return local custom settings
		/// </summary>
		/// <returns></returns>
		private WomanCustomiseSettings GetSettings()
        {
			WomanCustomiseSettings settings = new WomanCustomiseSettings((int)faceType,
				(int)eyeColor, (int)topType, (int)bottomType,
				(int)hairType, (int)hairColor, (int)skinType, (int)jacketColor, (int)shirtColor, (int)skirtColor, (int)pantsColor, (int)shoesColor);

			return settings;
		}

		/// <summary>
		/// Implementation of this avatars customise action
		/// </summary>
		public class WomanCustomiseActions : AvatarCustomiseActions
        {
			public WomanCustomise customise;

			public override void NextHairStyle()
            {
				customise.hairType++;

				if ((int)customise.hairType > System.Enum.GetNames(typeof(HairType)).Length - 1)
				{
					customise.hairType = 0;
				}

				customise.UpdateValues();
			}

            public override void PreviousHairStyle()
            {
				customise.hairType--;

				if ((int)customise.hairType < 0)
				{
					customise.hairType = HairType.Short;
				}

				customise.UpdateValues();
			}

            public override void NextHairColor()
            {
				customise.hairColor++;

				if ((int)customise.hairColor > System.Enum.GetNames(typeof(HairColor)).Length - 1)
				{
					customise.hairColor = 0;
				}

				customise.UpdateValues();
			}

            public override void PreviousHairColor()
            {
				customise.hairColor--;

				if ((int)customise.hairColor < 0)
				{
					customise.hairColor = HairColor.White;
				}

				customise.UpdateValues();
			}

            public override void NextFaceType()
            {
				customise.faceType++;

				if ((int)customise.faceType > System.Enum.GetNames(typeof(FaceType)).Length - 1)
				{
					customise.faceType = 0;
				}

				customise.UpdateValues();
			}

            public override void PreviousFaceType()
            {
				customise.faceType--;

				if ((int)customise.faceType < 0)
				{
					customise.faceType = FaceType.FaceE;
				}

				customise.UpdateValues();
			}

            public override void NextSkinType()
            {
				customise.skinType++;

				if ((int)customise.skinType > System.Enum.GetNames(typeof(SkinType)).Length - 1)
				{
					customise.skinType = 0;
				}

				customise.UpdateValues();
			}

            public override void PreviousSkinType()
            {
				customise.skinType--;

				if ((int)customise.skinType < 0)
				{
					customise.skinType = SkinType.SkinTypeE;
				}

				customise.UpdateValues();
			}

            public override void NextUpper()
            {
				customise.shirtColor++;

				if ((int)customise.shirtColor > System.Enum.GetNames(typeof(ShirtColor)).Length - 1)
				{
					customise.shirtColor = 0;
				}

				customise.UpdateValues();
			}

            public override void PreviousUpper()
            {
				customise.shirtColor--;

				if ((int)customise.shirtColor < 0)
				{
					customise.shirtColor = ShirtColor.White;
				}

				customise.UpdateValues();
			}

            public override void NextBottom()
            {
				customise.pantsColor++;

				if ((int)customise.pantsColor > System.Enum.GetNames(typeof(PantsColor)).Length - 1)
				{
					customise.pantsColor = 0;
				}

				customise.UpdateValues();
			}

            public override void PreviousBottom()
            {
				customise.pantsColor--;

				if ((int)customise.pantsColor < 0)
				{
					customise.pantsColor = PantsColor.White;
				}

				customise.UpdateValues();
			}

            public override void NextShoe()
            {
				customise.shoesColor++;

				if ((int)customise.shoesColor > System.Enum.GetNames(typeof(ShoesColor)).Length - 1)
				{
					customise.shoesColor = 0;
				}

				customise.UpdateValues();
			}

            public override void PreviousShoe()
            {
				customise.shoesColor--;

				if ((int)customise.shoesColor < 0)
				{
					customise.shoesColor = ShoesColor.White;
				}

				customise.UpdateValues();
			}

			public override void ChangeColor(string type, int n)
			{

			}

			public override void ChangeAccessory(string type)
			{

			}
		}

		/// <summary>
		/// class for this avatar custom settings
		/// </summary>
		public class WomanCustomiseSettings : AvatarCustomiseSettings
		{
			public int face;
			public int eye;
			public int topT;
			public int bottomT;
			public int hairT;
			public int hairC;
			public int skinT;
			public int jacketC;
			public int shirtC;
			public int skirtC;
			public int pantsC;
			public int shoesC;

			//Constructor
			public WomanCustomiseSettings(int faceTyp, int eyeCol, int topTyp, int btmTyp, int hairTyp, int hairCol, int skinTyp, int jacketCol, int shirtCol, int skirtCol, int pantsCol, int shoesCol)
            {
				face = faceTyp;
				eye = eyeCol;
				topT = topTyp;
				bottomT = btmTyp;
				hairT = hairTyp;
				hairC = hairCol;
				skinT = skinTyp;
				jacketC = jacketCol;
				shirtC = shirtCol;
				skirtC = skirtCol;
				pantsC = pantsCol;
				shoesC = shoesCol;
			}

			/// <summary>
			/// Randomise Avatar
			/// </summary>
			/// <returns></returns>
            public override AvatarCustomiseSettings Randomise()
            {
				face = Random.Range(0, System.Enum.GetNames(typeof(FaceType)).Length);
				eye = Random.Range(0, System.Enum.GetNames(typeof(EyeColor)).Length);
				topT = Random.Range(0, System.Enum.GetNames(typeof(TopType)).Length);
				bottomT = Random.Range(0, System.Enum.GetNames(typeof(BottomType)).Length);
				hairT = Random.Range(0, System.Enum.GetNames(typeof(HairType)).Length);
				hairC = Random.Range(0, System.Enum.GetNames(typeof(HairColor)).Length);
				skinT = Random.Range(0, System.Enum.GetNames(typeof(SkinType)).Length);
				jacketC = Random.Range(0, System.Enum.GetNames(typeof(JacketColor)).Length);
				shirtC = Random.Range(0, System.Enum.GetNames(typeof(ShirtColor)).Length);
				skirtC = Random.Range(0, System.Enum.GetNames(typeof(SkirtColor)).Length);
				pantsC = Random.Range(0, System.Enum.GetNames(typeof(PantsColor)).Length);
				shoesC = Random.Range(0, System.Enum.GetNames(typeof(ShoesColor)).Length);

				return this;
			}
        }

        private void Awake()
        {
			CustomiseAction = new WomanCustomiseActions();
			((WomanCustomiseActions)CustomiseAction).customise = this;
		}

        private void Start()
        {
			PipelineUpdate();
		}

		private void PipelineUpdate()
        {
#if UNITY_PIPELINE_HDRP
			foreach(Renderer rend in GetComponentsInChildren<Renderer>(true))
            {
				for(int i = 0; i< rend.materials.Length; i++)
                {
                    if (!rend.materials[i].shader.name.StartsWith("Universal Render Pipeline")) continue;

					Texture tex = rend.materials[i].mainTexture;
					rend.materials[i].shader = Shader.Find(rend.materials[i].shader.name.Replace("Universal Render Pipeline", "HDRP"));
					rend.materials[i].mainTexture = tex;
                }
            }

#elif UNITY_PIPELINE_URP
			foreach (Renderer rend in GetComponentsInChildren<Renderer>(true))
			{
				for (int i = 0; i < rend.materials.Length; i++)
				{
					if (!rend.materials[i].shader.name.StartsWith("HDRP")) continue;

					Texture tex = rend.materials[i].mainTexture;
					rend.materials[i].shader = Shader.Find(rend.materials[i].shader.name.Replace("HDRP", "Universal Render Pipeline"));
					rend.materials[i].mainTexture = tex;
				}
			}
#else
			foreach (Renderer rend in GetComponentsInChildren<Renderer>(true))
            {
				for(int i = 0; i< rend.materials.Length; i++)
                {
					Texture tex = rend.materials[i].mainTexture;
					rend.materials[i].shader = Shader.Find("Standard");
					rend.materials[i].mainTexture = tex;
                }
            }
#endif
		}

		/// <summary>
		/// This class handles what is displayed in the UI AvatarScreen
		/// </summary>
		/// <param name="display"></param>
		public void UpdateDisplay(Dictionary<string, TMP_Text> display)
		{
			display["HairColor"].text = Regex.Replace(hairColor.ToString(), "([a-z])_?([A-Z])", "$1 $2");
			display["HairStyle"].text = Regex.Replace(hairType.ToString(), "([a-z])_?([A-Z])", "$1 $2");
			display["Face"].text = Regex.Replace(faceType.ToString(), "([a-z])_?([A-Z])", "$1 $2");
			display["Skin"].text = Regex.Replace(skinType.ToString(), "([a-z])_?([A-Z])", "$1 $2");
			display["Upper"].text = Regex.Replace(shirtColor.ToString(), "([a-z])_?([A-Z])", "$1 $2");
			display["Bottom"].text = Regex.Replace(pantsColor.ToString(), "([a-z])_?([A-Z])", "$1 $2");
			display["Shoe"].text = Regex.Replace(shoesColor.ToString(), "([a-z])_?([A-Z])", "$1 $2");
			display["Sex"].text = "Female";
		}

		/// <summary>
		/// Pass avatar settings to this class to customise avatar
		/// </summary>
		/// <param name="settings"></param>
		public void Customise(AvatarCustomiseSettings settings)
        {
			if (settings == null) return;

			//must be of type woman
			if(settings is WomanCustomiseSettings)
            {
				Material[] mat;
				materialsList = gameObject.GetComponent<WomanMaterialList>();

				//Set Face
				for (int i = 0; i < materialsList.faceType.Length; i++)
				{
					materialsList.faceType[i].SetActive(false);
				}
				materialsList.faceType[((WomanCustomiseSettings)settings).face].SetActive(true);

				//Set Top
				for (int i = 0; i < materialsList.TopObjects.Length; i++)
				{
					materialsList.TopObjects[i].SetActive(false);
				}

				materialsList.TopObjects[((WomanCustomiseSettings)settings).topT].SetActive(true);

				//Set Bottom
				for (int i = 0; i < materialsList.BottomObjects.Length; i++)
				{
					materialsList.BottomObjects[i].SetActive(false);
					materialsList.LegsObject.SetActive(false);
				}

				//materialsList.BottomObjects[((WomanCustomiseSettings)settings).bottomT].SetActive(true);
				//always pants
				materialsList.BottomObjects[1].SetActive(true);

				if (((WomanCustomiseSettings)settings).bottomT == 0)
				{
					materialsList.LegsObject.SetActive(true);
				}

				//HairA==========================
				foreach (Transform child in materialsList.hairMainObjects[0].transform)
				{
					Renderer skinRend = child.gameObject.GetComponent<Renderer>();
					string oName = child.gameObject.name;
					string hName = oName.Substring(oName.Length - 1);

					mat = new Material[2];

					if (hName == "0")
					{
						mat[0] = materialsList.hairA_FadeMaterials[((WomanCustomiseSettings)settings).hairC];
						mat[1] = materialsList.hairA_Materials[((WomanCustomiseSettings)settings).hairC];
						skinRend.materials = mat;
					}
					else
					{
						skinRend.material = materialsList.hairA_FadeMaterials[((WomanCustomiseSettings)settings).hairC];
					}
				}

				//HairB==========================
				foreach (Transform child in materialsList.hairMainObjects[1].transform)
				{
					Renderer skinRend = child.gameObject.GetComponent<Renderer>();
					string oName = child.gameObject.name;
					string hName = oName.Substring(oName.Length - 1);

					mat = new Material[2];

					if (hName == "0")
					{
						mat[0] = materialsList.hairB_FadeMaterials[((WomanCustomiseSettings)settings).hairC];
						mat[1] = materialsList.hairB_Materials[((WomanCustomiseSettings)settings).hairC];
						skinRend.materials = mat;
					}
					else
					{
						skinRend.material = materialsList.hairB_FadeMaterials[((WomanCustomiseSettings)settings).hairC];
					}
				}

				//HairC==========================
				foreach (Transform child in materialsList.hairMainObjects[2].transform)
				{
					Renderer skinRend = child.gameObject.GetComponent<Renderer>();
					string oName = child.gameObject.name;
					string hName = oName.Substring(oName.Length - 1);

					mat = new Material[2];

					if (hName == "0")
					{
						mat[0] = materialsList.hairC_FadeMaterials[((WomanCustomiseSettings)settings).hairC];
						mat[1] = materialsList.hairC_Materials[((WomanCustomiseSettings)settings).hairC];
						skinRend.materials = mat;
					}
					else
					{
						skinRend.material = materialsList.hairC_FadeMaterials[((WomanCustomiseSettings)settings).hairC];
					}
				}

				//HairD==========================
				foreach (Transform child in materialsList.hairMainObjects[3].transform)
				{
					Renderer skinRend = child.gameObject.GetComponent<Renderer>();
					string oName = child.gameObject.name;
					string hName = oName.Substring(oName.Length - 1);

					mat = new Material[2];

					if (hName == "0")
					{
						mat[0] = materialsList.hairD_FadeMaterials[((WomanCustomiseSettings)settings).hairC];
						mat[1] = materialsList.hairD_Materials[((WomanCustomiseSettings)settings).hairC];
						skinRend.materials = mat;
					}
					else
					{
						skinRend.material = materialsList.hairD_FadeMaterials[((WomanCustomiseSettings)settings).hairC];
					}
				}

				//HairE==========================
				foreach (Transform child in materialsList.hairMainObjects[4].transform)
				{
					Renderer skinRend = child.gameObject.GetComponent<Renderer>();
					string oName = child.gameObject.name;
					string hName = oName.Substring(oName.Length - 1);

					mat = new Material[2];

					if (hName == "0")
					{
						mat[0] = materialsList.hairB_FadeMaterials[((WomanCustomiseSettings)settings).hairC];
						mat[1] = materialsList.hairB_Materials[((WomanCustomiseSettings)settings).hairC];
						skinRend.materials = mat;
					}
					else
					{
						skinRend.material = materialsList.hairB_FadeMaterials[((WomanCustomiseSettings)settings).hairC];
					}
				}

				//===========================================
				//Hair Type
				for (int i = 0; i < materialsList.hairMainObjects.Length; i++)
				{
					materialsList.hairMainObjects[i].SetActive(false);
				}

				materialsList.hairMainObjects[((WomanCustomiseSettings)settings).hairT].SetActive(true);

				//Set Body Color
				foreach (Transform child in materialsList.BodyObject.transform)
				{
					Renderer skinRend = child.gameObject.GetComponent<Renderer>();
					skinRend.material = materialsList.body_Materials[((WomanCustomiseSettings)settings).skinT];
				}

				foreach (Transform child in materialsList.LegsObject.transform)
				{
					Renderer skinRend = child.gameObject.GetComponent<Renderer>();
					skinRend.material = materialsList.body_Materials[((WomanCustomiseSettings)settings).skinT];
				}

				foreach (Transform child in materialsList.faceType[((WomanCustomiseSettings)settings).face].transform)
				{
					Renderer skinRend = child.gameObject.GetComponent<Renderer>();
					skinRend.material = materialsList.face_Materials[((WomanCustomiseSettings)settings).skinT];
				}

				// Eyes colors
				foreach (Transform child in materialsList.eyes_Object.transform)
				{
					Renderer skinRend = child.gameObject.GetComponent<Renderer>();
					skinRend.material = materialsList.eyeColors[((WomanCustomiseSettings)settings).eye];
				}

				//Jacket Color==========================
				foreach (Transform child in materialsList.TopObjects[1].transform)
				{
					Renderer skinRend = child.gameObject.GetComponent<Renderer>();
					string oName = child.gameObject.name;
					string hName = oName.Substring(oName.Length - 1);
					mat = new Material[2];
					mat[0] = materialsList.JacketMaterials[((WomanCustomiseSettings)settings).jacketC];
					mat[1] = materialsList.ShirtMaterials[((WomanCustomiseSettings)settings).shirtC];
					skinRend.materials = mat;
				}

				// Shirt colors
				foreach (Transform child in materialsList.TopObjects[0].transform)
				{
					Renderer skinRend = child.gameObject.GetComponent<Renderer>();
					skinRend.material = materialsList.ShirtMaterials[((WomanCustomiseSettings)settings).shirtC];
				}

				// Skirt colors
				foreach (Transform child in materialsList.BottomObjects[0].transform)
				{
					Renderer skinRend = child.gameObject.GetComponent<Renderer>();
					skinRend.material = materialsList.SkirtMaterials[((WomanCustomiseSettings)settings).pantsC];
				}

				// Pants colors
				foreach (Transform child in materialsList.BottomObjects[1].transform)
				{
					Renderer skinRend = child.gameObject.GetComponent<Renderer>();
					skinRend.material = materialsList.PantsMaterials[((WomanCustomiseSettings)settings).pantsC];
				}

				// Shoes colors
				foreach (Transform child in materialsList.shoesObject.transform)
				{
					Renderer skinRend = child.gameObject.GetComponent<Renderer>();
					skinRend.material = materialsList.ShoesMaterials[((WomanCustomiseSettings)settings).shoesC];
				}

				//need to update the local enums with the settings
				faceType = (FaceType)((WomanCustomiseSettings)settings).face;
				skinType = (SkinType)((WomanCustomiseSettings)settings).skinT;
				eyeColor = (EyeColor)((WomanCustomiseSettings)settings).eye;
				topType = (TopType)((WomanCustomiseSettings)settings).topT;
				jacketColor = (JacketColor)((WomanCustomiseSettings)settings).jacketC;
				shirtColor = (ShirtColor)((WomanCustomiseSettings)settings).shirtC;
				bottomType = (BottomType)((WomanCustomiseSettings)settings).bottomT;
				pantsColor = (PantsColor)((WomanCustomiseSettings)settings).pantsC;
				skirtColor = (SkirtColor)((WomanCustomiseSettings)settings).skirtC;
				hairType = (HairType)((WomanCustomiseSettings)settings).hairT;
				hairColor = (HairColor)((WomanCustomiseSettings)settings).hairC;
				shoesColor = (ShoesColor)((WomanCustomiseSettings)settings).shoesC;

				PipelineUpdate();
			}
		}

		/* private void OnValidate()
		 {
			 if (!Application.isPlaying)
			 {
				 //code for In Editor customize
				 UpdateValues();
			 }
		 }*/

		/// <summary>
		/// Get player avatar network properties
		/// </summary>
		/// <returns></returns>
		public Hashtable GetProperties()
        {
			var hash = new Hashtable();
			hash.Clear();

			hash.Add("TYPE", type.ToString());
			hash.Add("SEX", "Female");
			hash.Add("FT", (int)faceType);
			hash.Add("SCT", (int)skinType);
			hash.Add("EC", (int)eyeColor);
			hash.Add("TT", (int)topType);
			hash.Add("JC", (int)jacketColor);
			hash.Add("SHC", (int)shirtColor);
			hash.Add("BT", (int)bottomType);
			hash.Add("PC", (int)pantsColor);
			hash.Add("SKC", (int)skirtColor);
			hash.Add("HT", (int)hairType);
			hash.Add("HC", (int)hairColor);
			hash.Add("SC", (int)shoesColor);

			return hash;
        }

		/// <summary>
		/// Set player avatar network properties
		/// </summary>
		/// <param name="hash"></param>
		public void SetProperties(Hashtable hash)
        {
			if (type.ToString() != hash["TYPE"].ToString()) return;

			faceType = (FaceType)hash["FT"];
			skinType = (SkinType)hash["SCT"];
			eyeColor = (EyeColor)hash["EC"];
			topType = (TopType)hash["TT"];
			jacketColor = (JacketColor)hash["JC"];
			shirtColor = (ShirtColor)hash["SHC"];
			bottomType = (BottomType)hash["BT"];
			pantsColor = (PantsColor)hash["PC"];
			skirtColor = (SkirtColor)hash["SKC"];
			hairType = (HairType)hash["HT"];
			hairColor = (HairColor)hash["HC"];
			shoesColor = (ShoesColor)hash["SC"];

			UpdateValues();
		}

		/// <summary>
		/// Update customise values
		/// </summary>
		public void UpdateValues()
		{
			Customise(GetSettings());
		}

		public enum ShoesColor
		{
			Blue,
			Black,
			Gray,
			LightGray,
			Red,
			White
		}
		public enum JacketColor
		{
			Blue,
			Black,
			Gray,
			LightGray,
			Red,
			White
		}

		public enum SkirtColor
		{
			Blue,
			Black,
			Gray,
			LightGray,
			Red,
			White
		}

		public enum PantsColor
		{
			Blue,
			Black,
			Gray,
			LightGray,
			Red,
			White
		}

		public enum ShirtColor
		{
			Blue,
			Black,
			Gray,
			LightBlue,
			Red,
			White
		}

		public enum HairType
		{
			Medium,
			PonyTail,
			FrenchRoll,
			Short,
			Bun
		}

		public enum HairColor
		{
			Blonde,
			White,
			Dark,
			Red,
			Brown
		}

		public enum EyeColor
		{
			Brown,
			Blue,
			Green,
			Black,
			DarkBlue,
			LightBrown
		}

		public enum FaceType
		{
			FaceA,
			FaceB,
			FaceC,
			FaceD,
			FaceE
		}

		public enum SkinType
		{
			SkinTypeA,
			SkinTypeB,
			SkinTypeC,
			SkinTypeD,
			SkinTypeE
		}

		public enum TopColors
		{
			WhiteBlue,
			Blue,
			Grey,
			WhitePurple
		}

		public enum TopType
		{
			Shirt,
			Jacket
		}

		public enum BottomType
		{
			Skirt,
			Pants
		}

#if UNITY_EDITOR
		[CustomEditor(typeof(WomanCustomise), true)]
		public class WomanCustomise_Editor : BaseInspectorEditor
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
