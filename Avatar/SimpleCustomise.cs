using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class SimpleCustomise : MonoBehaviour, ICustomAvatar
    {
        [SerializeField]
        private AvatarType type = AvatarType.Simple;
		[SerializeField]
		private CustomiseAvatar.Sex sex = CustomiseAvatar.Sex.Female;

		private int m_currentSkin = 0;
		private int m_currentGlasses = 0;
		private int m_currentEarings = 0;
		private int m_currentHair = 0;
		private int m_currentEyes = 0;
		private int m_currentTop = 0;
		private int m_currentBottom = 0;
		private int m_currentShoes = 0;

		public int m_currentHairColor = 10;
		public int m_currentTopColor = 8;
		public int m_currentBottomColor = 4;
		public int m_currentShoeColor = 1;

		private SimpleMaterialList m_MaterialList;
		private string m_currentAccessory = "";

		public AvatarType Type { get { return type; } }

		public bool OverrideClothingColors
        {
			get;
			set;
        }

		/// <summary>
		/// Public access to the this avatar customise actions
		/// </summary>
		public AvatarCustomiseActions CustomiseAction
		{
			get;
			private set;
		}

		public Color[] FixedColors { get { return m_MaterialList.fixedColors ; } }

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
				return sex;
			}
		}

		/// <summary>
		/// Return local custom settings
		/// </summary>
		/// <returns></returns>
		private AvatarCustomiseSettings GetSettings()
		{
			SimpleCustomiseSettings settings = new SimpleCustomiseSettings(m_currentSkin, m_currentGlasses,
				m_currentEarings, m_currentHair, m_currentEyes, m_currentTop, m_currentBottom, m_currentShoes,
				m_currentHairColor, m_currentTopColor, m_currentBottomColor, m_currentShoeColor);

			if(m_MaterialList == null)
            {
				m_MaterialList = GetComponentInChildren<SimpleMaterialList>(true);
            }

			settings.materialList = m_MaterialList;

			return settings;
		}

        private void Start()
        {
#if UNITY_PIPELINE_HDRP
			foreach(Renderer rend in GetComponentsInChildren<Renderer>(true))
            {
				for(int i = 0; i< rend.materials.Length; i++)
                {
                    if (!rend.materials[i].shader.name.Contains("Universal Render Pipeline")) continue;

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
		/// Implementation of this avatars customise action
		/// </summary>
		public class SimpleCustomiseActions : AvatarCustomiseActions
		{
			public SimpleCustomise customise;
			public SimpleMaterialList materialList;

			public override void NextHairStyle()
			{
				if (customise.m_currentHair + 1 >= materialList.hair.Length)
				{
					customise.m_currentHair = 0;
				}
				else
				{
					customise.m_currentHair++;
				}

				customise.UpdateValues();
			}

			public override void PreviousHairStyle()
			{
				if (customise.m_currentHair - 1 < 0)
				{
					customise.m_currentHair = materialList.hair.Length - 1;
				}
				else
				{
					customise.m_currentHair--;
				}

				customise.UpdateValues();
			}

			public override void NextHairColor()
			{
				if (customise.m_currentHairColor + 1 >= materialList.skinColors.Length)
				{
					customise.m_currentHairColor = 0;
				}
				else
				{
					customise.m_currentHairColor++;
				}

				customise.UpdateValues();
			}

			public override void PreviousHairColor()
			{
				if (customise.m_currentHairColor - 1 < 0)
				{
					customise.m_currentHairColor = materialList.skinColors.Length - 1;
				}
				else
				{
					customise.m_currentHairColor--;
				}

				customise.UpdateValues();
			}

			public override void NextFaceType()
			{
				if (customise.m_currentAccessory.Equals("EYES"))
				{
					if (customise.m_currentEyes + 1 >= materialList.eyes.Length)
					{
						customise.m_currentEyes = 0;
					}
					else
					{
						customise.m_currentEyes++;
					}
				}
				else if (customise.m_currentAccessory.Equals("GLASSES"))
				{
					if (customise.m_currentGlasses + 1 >= materialList.glasses.Length)
					{
						customise.m_currentGlasses = 0;
					}
					else
					{
						customise.m_currentGlasses++;
					}
				}
				else
				{
					if (customise.m_currentEarings + 1 >= materialList.earings.Length)
					{
						customise.m_currentEarings = 0;
					}
					else
					{
						customise.m_currentEarings++;
					}
				}

				customise.UpdateValues();
			}

			public override void PreviousFaceType()
			{
				if (customise.m_currentAccessory.Equals("EYES"))
				{
					if (customise.m_currentEyes - 1 < 0)
					{
						customise.m_currentEyes = materialList.eyes.Length - 1;
					}
					else
					{
						customise.m_currentEyes--;
					}
				}
				else if (customise.m_currentAccessory.Equals("GLASSES"))
				{
					if (customise.m_currentGlasses - 1 < 0)
					{
						customise.m_currentGlasses = materialList.glasses.Length - 1;
					}
					else
					{
						customise.m_currentGlasses--;
					}
				}
				else
				{
					if (customise.m_currentEarings - 1 < 0)
					{
						customise.m_currentEarings = materialList.earings.Length - 1;
					}
					else
					{
						customise.m_currentEarings--;
					}
				}

				customise.UpdateValues();
			}

			public override void NextSkinType()
			{
				if (customise.m_currentSkin + 1 >= materialList.skinColors.Length)
				{
					customise.m_currentSkin = 0;
				}
				else
				{
					customise.m_currentSkin++;
				}

				customise.UpdateValues();
			}

			public override void PreviousSkinType()
			{
				if(customise.m_currentSkin - 1 < 0)
                {
					customise.m_currentSkin = materialList.skinColors.Length - 1;
				}
				else
                {
					customise.m_currentSkin--;
				}

				customise.UpdateValues();
			}

			public override void NextUpper()
			{
				if (customise.m_currentTop + 1 >= materialList.tops.Length)
				{
					customise.m_currentTop = 0;
				}
				else
				{
					customise.m_currentTop++;
				}

				customise.UpdateValues();
			}

			public override void PreviousUpper()
			{
				if (customise.m_currentTop - 1 < 0)
				{
					customise.m_currentTop = materialList.tops.Length - 1;
				}
				else
				{
					customise.m_currentTop--;
				}

				customise.UpdateValues();
			}

			public override void NextBottom()
			{
				if (customise.m_currentBottom + 1 >= materialList.bottoms.Length)
				{
					customise.m_currentBottom = 0;
				}
				else
				{
					customise.m_currentBottom++;
				}

				customise.UpdateValues();
			}

			public override void PreviousBottom()
			{
				if (customise.m_currentBottom - 1 < 0)
				{
					customise.m_currentBottom = materialList.bottoms.Length - 1;
				}
				else
				{
					customise.m_currentBottom--;
				}

				customise.UpdateValues();
			}

			public override void NextShoe()
			{
				if (customise.m_currentShoes + 1 >= materialList.shoes.Length)
				{
					customise.m_currentShoes = 0;
				}
				else
				{
					customise.m_currentShoes++;
				}

				customise.UpdateValues();
			}

			public override void PreviousShoe()
			{
				if (customise.m_currentShoes - 1 < 0)
				{
					customise.m_currentShoes = materialList.shoes.Length - 1;
				}
				else
				{
					customise.m_currentShoes--;
				}

				customise.UpdateValues();
			}

			public override void ChangeColor(string type, int n)
			{
				//identify which color require changing
				switch(type)
                {
					case "UPPER":
						customise.m_currentTopColor = n;
						break;
					case "LOWER":
						customise.m_currentBottomColor = n;
						break;
					case "SHOES":
						customise.m_currentShoeColor = n;
						break;
				}

				customise.UpdateValues();
			}

			public override void ChangeAccessory(string type)
            {
				customise.m_currentAccessory = type;
			}
		}

		/// <summary>
		/// Class for this avatar custom settings
		/// </summary>
		public class SimpleCustomiseSettings : AvatarCustomiseSettings
		{
			public int currentSkin = 0;
			public int currentGlasses = 0;
			public int currentEarings = 0;
			public int currentHair = 0;
			public int currentEyes = 0;
			public int currentTop = 0;
			public int currentBottom = 0;
			public int currentShoes = 0;

			public int currentHairColor = 0;
			public int currentTopColor = 0;
			public int currentBottomColor = 0;
			public int currentShoeColor = 0;


			public SimpleMaterialList materialList;

			//Constructor
			public SimpleCustomiseSettings(int skin, int glasses, int earings, int hair, int eyes, int top, int bottom, int shoes, int hairColor, int topColor, int bottomColor, int shoeColor)
			{
				currentSkin = skin;
				currentGlasses = glasses;
				currentEarings = earings;
				currentHair = hair;
				currentEyes = eyes;
				currentTop = top;
				currentBottom = bottom;
				currentShoes = shoes;

				currentHairColor = hairColor;
				currentTopColor = topColor;
				currentBottomColor = bottomColor;
				currentShoeColor = shoeColor;
			}

			/// <summary>
			/// Randomise Avatar
			/// </summary>
			/// <returns></returns>
			public override AvatarCustomiseSettings Randomise()
			{
				currentSkin = Random.Range(0, materialList.skinColors.Length);
				currentGlasses = Random.Range(0, materialList.glasses.Length);
				currentEarings = Random.Range(0, materialList.earings.Length);
				currentHair = Random.Range(0, materialList.hair.Length);
				currentEyes = Random.Range(0, materialList.eyes.Length);
				currentTop = Random.Range(0, materialList.tops.Length);
				currentBottom = Random.Range(0, materialList.bottoms.Length);
				currentShoes = Random.Range(0, materialList.shoes.Length);

				currentHairColor = Random.Range(0, materialList.skinColors.Length);
				currentTopColor = Random.Range(0, materialList.fixedColors.Length);
				currentBottomColor = Random.Range(0, materialList.fixedColors.Length);
				currentShoeColor = Random.Range(0, materialList.fixedColors.Length);

				return this;
			}
		}


		private void Awake()
		{
			if(m_MaterialList == null)
            {
				m_MaterialList = GetComponent<SimpleMaterialList>();
			}

			OverrideClothingColors = (GetComponentInParent<NPCBot>()) ? NPCManager.Instance.OverrideBotClothingColors : AppManager.Instance.Settings.playerSettings.overrideClothingColors;

			if (OverrideClothingColors)
            {
				m_MaterialList.fixedColors = AppManager.Instance.Settings.playerSettings.simpleClothingColors;
			}

			if (m_currentTopColor < 0)
			{
				m_currentTopColor = 0;
			}

			if (m_currentBottomColor < 0)
			{
				m_currentBottomColor = 0;
			}

			if (m_currentShoeColor < 0)
			{
				m_currentShoeColor = 0;
			}

			if (m_MaterialList.fixedColors.Length <= 0)
            {
				m_MaterialList.fixedColors = new Color[3] { Color.white, Color.blue, Color.red };

				m_currentTopColor = 0;
				m_currentBottomColor = 1;
				m_currentShoeColor = 2;
			}
			else
            {
				if(m_currentTopColor > m_MaterialList.fixedColors.Length - 1)
                {
					m_currentTopColor = m_MaterialList.fixedColors.Length - 1;
				}

				if (m_currentBottomColor > m_MaterialList.fixedColors.Length - 1)
				{
					m_currentBottomColor = m_MaterialList.fixedColors.Length - 1;
				}

				if (m_currentShoeColor > m_MaterialList.fixedColors.Length - 1)
				{
					m_currentShoeColor = m_MaterialList.fixedColors.Length - 1;
				}
			}

			CustomiseAction = new SimpleCustomiseActions();
			((SimpleCustomiseActions)CustomiseAction).customise = this;
			((SimpleCustomiseActions)CustomiseAction).materialList = m_MaterialList;
		}

		/// <summary>
		/// This class handles what is displayed in the UI AvatarScreen
		/// </summary>
		/// <param name="display"></param>
		public void UpdateDisplay(Dictionary<string, TMP_Text> display)
		{
			display["Sex"].text = sex.ToString();
			display["Skin"].text = "Skin Tone " + m_currentSkin.ToString();
			display["HairStyle"].text = "Hair Style " + m_currentHair.ToString();
			display["HairColor"].text = "Hair Tone " + m_currentHairColor.ToString();
			display["Upper"].text = "Upper Style " + m_currentTop.ToString();
			display["Bottom"].text = "Bottom Style " + m_currentBottom.ToString();
			display["Shoe"].text = "Shoe Style " + m_currentShoes.ToString();

			if(m_currentAccessory.Equals("EYES"))
            {
				display["Face"].text = "Eye Color " + m_currentEyes.ToString();
			}
			else if(m_currentAccessory.Equals("GLASSES"))
            {
				display["Face"].text = "Glasses Style " + m_currentGlasses.ToString();
			}
			else
            {
				display["Face"].text = "Earing Style " + m_currentEarings.ToString();
			}
		}

		/// <summary>
		/// Pass avatar settings to this class to customise avatar
		/// </summary>
		/// <param name="settings"></param>
		public void Customise(AvatarCustomiseSettings settings)
		{
			if (settings == null) return;

			if(settings is SimpleCustomiseSettings)
            {
				if (m_MaterialList == null)
				{
					m_MaterialList = GetComponentInChildren<SimpleMaterialList>(true);
				}

				Material _mat = null; ;

				if(!Application.isPlaying)
                {
					_mat = new Material(m_MaterialList.skin.GetComponentInChildren<Renderer>(true).sharedMaterial);
					_mat.color = m_MaterialList.skinColors[((SimpleCustomiseSettings)settings).currentSkin];
					m_MaterialList.skin.GetComponentInChildren<Renderer>(true).sharedMaterial = _mat;
				}
				else
                {
					//Set Face
					m_MaterialList.skin.GetComponentInChildren<Renderer>(true).materials[0].color = m_MaterialList.skinColors[((SimpleCustomiseSettings)settings).currentSkin];
					m_MaterialList.skin.GetComponentInChildren<Renderer>(true).materials[1].color = m_MaterialList.skinColors[((SimpleCustomiseSettings)settings).currentHairColor];
				}

				//set accessories on face
				for (int i = 0; i < m_MaterialList.earings.Length; i++)
				{
					if (m_MaterialList.earings[i] == null) continue;

					if (i.Equals(((SimpleCustomiseSettings)settings).currentEarings))
					{
						m_MaterialList.earings[i].SetActive(true);
					}
					else
					{
						m_MaterialList.earings[i].SetActive(false);
					}
				}

				for (int i = 0; i < m_MaterialList.eyes.Length; i++)
				{
					if (i.Equals(((SimpleCustomiseSettings)settings).currentEyes))
					{
						m_MaterialList.eyes[i].SetActive(true);
					}
					else
					{
						m_MaterialList.eyes[i].SetActive(false);
					}
				}

				for (int i = 0; i < m_MaterialList.glasses.Length; i++)
				{
					if (m_MaterialList.glasses[i] == null) continue;

					if (i.Equals(((SimpleCustomiseSettings)settings).currentGlasses))
					{
						m_MaterialList.glasses[i].SetActive(true);
					}
					else
					{
						m_MaterialList.glasses[i].SetActive(false);
					}
				}

				//set hair
				for (int i = 0; i < m_MaterialList.hair.Length; i++)
				{
					if(!Application.isPlaying)
                    {
						_mat = new Material(m_MaterialList.hair[i].GetComponentInChildren<Renderer>(true).sharedMaterial);
						_mat.color = m_MaterialList.skinColors[((SimpleCustomiseSettings)settings).currentHairColor];

						m_MaterialList.hair[i].GetComponentInChildren<Renderer>(true).sharedMaterial = _mat;
					}
					else
                    {
						m_MaterialList.hair[i].GetComponentInChildren<Renderer>(true).material.color = m_MaterialList.skinColors[((SimpleCustomiseSettings)settings).currentHairColor];
					}

					if (i.Equals(((SimpleCustomiseSettings)settings).currentHair))
					{
						m_MaterialList.hair[i].SetActive(true);
					}
					else
					{
						m_MaterialList.hair[i].SetActive(false);
					}
				}

				//set top
				for (int i = 0; i < m_MaterialList.tops.Length; i++)
                {
					if (!Application.isPlaying)
					{
						_mat = new Material(m_MaterialList.tops[i].GetComponentInChildren<Renderer>(true).sharedMaterial);
						_mat.color = m_MaterialList.fixedColors[((SimpleCustomiseSettings)settings).currentTopColor];

						m_MaterialList.tops[i].GetComponentInChildren<Renderer>(true).sharedMaterial = _mat;
					}
					else
					{
						m_MaterialList.tops[i].GetComponentInChildren<Renderer>(true).material.color = m_MaterialList.fixedColors[((SimpleCustomiseSettings)settings).currentTopColor];

					}

					if (i.Equals(((SimpleCustomiseSettings)settings).currentTop))
                    {
						m_MaterialList.tops[i].SetActive(true);
					}
					else
                    {
						m_MaterialList.tops[i].SetActive(false);
					}
                }

				//set bottom
				for (int i = 0; i < m_MaterialList.bottoms.Length; i++)
				{
					if (!Application.isPlaying)
					{
						_mat = new Material(m_MaterialList.bottoms[i].GetComponentInChildren<Renderer>(true).sharedMaterial);
						_mat.color = m_MaterialList.fixedColors[((SimpleCustomiseSettings)settings).currentBottomColor];

						m_MaterialList.bottoms[i].GetComponentInChildren<Renderer>(true).sharedMaterial = _mat;
					}
					else
                    {
						m_MaterialList.bottoms[i].GetComponentInChildren<Renderer>(true).material.color = m_MaterialList.fixedColors[((SimpleCustomiseSettings)settings).currentBottomColor];
					}

					if (i.Equals(((SimpleCustomiseSettings)settings).currentBottom))
					{
						m_MaterialList.bottoms[i].SetActive(true);
					}
					else
					{
						m_MaterialList.bottoms[i].SetActive(false);
					}
				}

				//set shoe
				for (int i = 0; i < m_MaterialList.shoes.Length; i++)
				{
					if (!Application.isPlaying)
					{
						_mat = new Material(m_MaterialList.shoes[i].GetComponentInChildren<Renderer>(true).sharedMaterial);
						_mat.color = m_MaterialList.fixedColors[((SimpleCustomiseSettings)settings).currentShoeColor];

						m_MaterialList.shoes[i].GetComponentInChildren<Renderer>(true).sharedMaterial = _mat;
					}
					else
                    {
						m_MaterialList.shoes[i].GetComponentInChildren<Renderer>(true).material.color = m_MaterialList.fixedColors[((SimpleCustomiseSettings)settings).currentShoeColor];

					}

					if (i.Equals(((SimpleCustomiseSettings)settings).currentShoes))
					{
						m_MaterialList.shoes[i].SetActive(true);
					}
					else
					{
						m_MaterialList.shoes[i].SetActive(false);
					}
				}

				m_currentSkin = ((SimpleCustomiseSettings)settings).currentSkin;
				m_currentGlasses = ((SimpleCustomiseSettings)settings).currentGlasses;
				m_currentEarings = ((SimpleCustomiseSettings)settings).currentEarings;
				m_currentHair = ((SimpleCustomiseSettings)settings).currentHair;
				m_currentEyes = ((SimpleCustomiseSettings)settings).currentEyes;
				m_currentTop = ((SimpleCustomiseSettings)settings).currentTop;
				m_currentBottom = ((SimpleCustomiseSettings)settings).currentBottom;
				m_currentShoes = ((SimpleCustomiseSettings)settings).currentShoes;

				m_currentHairColor = ((SimpleCustomiseSettings)settings).currentHairColor;
				m_currentTopColor = ((SimpleCustomiseSettings)settings).currentTopColor;
				m_currentBottomColor = ((SimpleCustomiseSettings)settings).currentBottomColor;
				m_currentShoeColor = ((SimpleCustomiseSettings)settings).currentShoeColor;

			}
		}

		/// <summary>
		/// Get player avatar network properties
		/// </summary>
		/// <returns></returns>
		public Hashtable GetProperties()
		{
			var hash = new Hashtable();
			hash.Clear();

			hash.Add("TYPE", type.ToString());

			hash.Add("SEX", sex.Equals(CustomiseAvatar.Sex.Female) ? "Female" : "Male");
			hash.Add("SCT", m_currentSkin);
			hash.Add("EC", m_currentEyes);
			hash.Add("TT", m_currentTop);
			hash.Add("TC", m_currentTopColor);
			hash.Add("BT", m_currentBottom);
			hash.Add("BC", m_currentBottomColor);
			hash.Add("HT", m_currentHair);
			hash.Add("HC", m_currentHairColor);
			hash.Add("ST", m_currentShoes);
			hash.Add("SC", m_currentShoeColor);

			hash.Add("GT", m_currentGlasses);
			hash.Add("ET", m_currentEarings);

			return hash;
		}

		/// <summary>
		/// Set player avatar network properties
		/// </summary>
		/// <param name="hash"></param>
		public void SetProperties(Hashtable hash)
		{
			if (hash.ContainsKey("TYPE") && type.ToString() != hash["TYPE"].ToString()) return;

			m_currentSkin = (int)hash["SCT"];
			m_currentEyes = (int)hash["EC"];
			m_currentTop = (int)hash["TT"];
			m_currentTopColor = (int)hash["TC"];
			m_currentBottom = (int)hash["BT"];
			m_currentBottomColor = (int)hash["BC"];
			m_currentHair = (int)hash["HT"];
			m_currentHairColor = (int)hash["HC"];
			m_currentShoes = (int)hash["ST"];
			m_currentShoeColor = (int)hash["SC"];
			m_currentGlasses = (int)hash["GT"];
			m_currentEarings = (int)hash["ET"];

			UpdateValues();
		}

		/// <summary>
		/// Update customise values
		/// </summary>
		public void UpdateValues()
		{
			Customise(GetSettings());
		}

#if UNITY_EDITOR
		[CustomEditor(typeof(SimpleCustomise), true)]
		public class SimpleCustomise_Editor : BaseInspectorEditor
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
