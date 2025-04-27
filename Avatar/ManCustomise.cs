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
    public class ManCustomise : MonoBehaviour, ICustomAvatar
    {
        [SerializeField]
        private AvatarType type = AvatarType.Standard;

        public FaceType faceType;
        public SkinType skinType;
        public EyeColor eyeCol;
        public Hair hair;
        public HairColor hairCol;
        public Jacket jacketType;
        public JacketColor jacketCol;
        public ShirtColor shirtCol;
        public WaistcoatColor waistcoatCol;
        public TieColor tieCol;
        public PantsColor pantsCol;
        public ShoesColor shoesCol;

        private ManMaterialList materialList;

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
                return CustomiseAvatar.Sex.Male;
            }
        }

        /// <summary>
        /// Return local custom settings
        /// </summary>
        /// <returns></returns>
        private ManCustomiseSettings GetSettings()
        {
            ManCustomiseSettings settings = new ManCustomiseSettings((int)faceType, (int)eyeCol, (int)hair,
                (int)hairCol, (int)skinType, (int)jacketType, (int)jacketCol, (int)shirtCol, (int)pantsCol, (int)shoesCol);

            return settings;
        }

        /// <summary>
        /// Implementation of this avatars customise action
        /// </summary>
        public class ManCustomiseActions : AvatarCustomiseActions
        {
            public ManCustomise customise;

            public override void NextHairStyle()
            {
                customise.hair++;

                if ((int)customise.hair > System.Enum.GetNames(typeof(Hair)).Length - 1)
                {
                    customise.hair = 0;
                }

                customise.UpdateValues();
            }

            public override void PreviousHairStyle()
            {
                customise.hair--;

                if ((int)customise.hair < 0)
                {
                    customise.hair = Hair.HairI;
                }

                customise.UpdateValues();
            }

            public override void NextHairColor()
            {
                customise.hairCol++;

                if ((int)customise.hairCol > System.Enum.GetNames(typeof(HairColor)).Length - 1)
                {
                    customise.hairCol = 0;
                }

                customise.UpdateValues();
            }

            public override void PreviousHairColor()
            {
                customise.hairCol--;

                if ((int)customise.hairCol < 0)
                {
                    customise.hairCol = HairColor.Gray;
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

                if ((int)customise.hair < 0)
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
                customise.jacketCol++;

                if ((int)customise.jacketCol > System.Enum.GetNames(typeof(JacketColor)).Length - 1)
                {
                    customise.jacketCol = 0;
                }

                customise.UpdateValues();
            }

            public override void PreviousUpper()
            {
                customise.jacketCol--;

                if ((int)customise.jacketCol < 0)
                {
                    customise.jacketCol = JacketColor.White;
                }

                customise.UpdateValues();
            }

            public override void NextBottom()
            {
                customise.pantsCol++;

                if ((int)customise.pantsCol > System.Enum.GetNames(typeof(PantsColor)).Length - 1)
                {
                    customise.pantsCol = 0;
                }

                customise.UpdateValues();
            }

            public override void PreviousBottom()
            {
                customise.pantsCol--;

                if ((int)customise.pantsCol < 0)
                {
                    customise.pantsCol = PantsColor.White;
                }

                customise.UpdateValues();
            }

            public override void NextShoe()
            {
                customise.shoesCol++;

                if ((int)customise.shoesCol > System.Enum.GetNames(typeof(ShoesColor)).Length - 1)
                {
                    customise.shoesCol = 0;
                }

                customise.UpdateValues();
            }

            public override void PreviousShoe()
            {
                customise.shoesCol--;

                if ((int)customise.shoesCol < 0)
                {
                    customise.shoesCol = ShoesColor.White;
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
        public class ManCustomiseSettings : AvatarCustomiseSettings
        {
            public int face;
            public int eye;
            public int hairT;
            public int hairC;
            public int skinT;
            public int jacketT;
            public int jacketC;
            public int shirtC;
            public int pantsC;
            public int shoesC;

            //Constructor
            public ManCustomiseSettings(int faceTyp, int eyeCol, int hairTyp, int hairCol, int skinTyp, int jacketTyp, int jacketCol, int shirtCol, int pantsCol, int shoesCol)
            {
                face = faceTyp;
                eye = eyeCol;
                hairT = hairTyp;
                hairC = hairCol;
                skinT = skinTyp;
                jacketT = jacketTyp;
                jacketC = jacketCol;
                shirtC = shirtCol;
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
                hairT = Random.Range(0, System.Enum.GetNames(typeof(Hair)).Length);
                hairC = Random.Range(0, System.Enum.GetNames(typeof(HairColor)).Length);
                skinT = Random.Range(0, System.Enum.GetNames(typeof(SkinType)).Length);
                jacketC = Random.Range(0, System.Enum.GetNames(typeof(JacketColor)).Length);
                shirtC = Random.Range(0, System.Enum.GetNames(typeof(ShirtColor)).Length);
                jacketT = Random.Range(0, System.Enum.GetNames(typeof(Jacket)).Length);
                pantsC = Random.Range(0, System.Enum.GetNames(typeof(PantsColor)).Length);
                shoesC = Random.Range(0, System.Enum.GetNames(typeof(ShoesColor)).Length);

                return this;
            }
        }

        private void Awake()
        {
            CustomiseAction = new ManCustomiseActions();
            ((ManCustomiseActions)CustomiseAction).customise = this;
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
                    if (!rend.materials[i].shader.name.StartsWith("UHDRP")) continue;

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
            display["HairColor"].text = Regex.Replace(hairCol.ToString(), "([a-z])_?([A-Z])", "$1 $2");
            display["HairStyle"].text = Regex.Replace(hair.ToString(), "([a-z])_?([A-Z])", "$1 $2");
            display["Face"].text = Regex.Replace(faceType.ToString(), "([a-z])_?([A-Z])", "$1 $2");
            display["Skin"].text = Regex.Replace(skinType.ToString(), "([a-z])_?([A-Z])", "$1 $2");
            display["Upper"].text = Regex.Replace(jacketCol.ToString(), "([a-z])_?([A-Z])", "$1 $2");
            display["Bottom"].text = Regex.Replace(pantsCol.ToString(), "([a-z])_?([A-Z])", "$1 $2");
            display["Shoe"].text = Regex.Replace(shoesCol.ToString(), "([a-z])_?([A-Z])", "$1 $2");
            display["Sex"].text = "Male";
        }

        /// <summary>
        /// Pass avatar settings to this class to customise avatar
        /// </summary>
        /// <param name="settings"></param>
        public void Customise(AvatarCustomiseSettings settings)
        {
            if (settings == null) return;

            //must be type man
            if (settings is ManCustomiseSettings)
            {
                materialList = gameObject.GetComponent<ManMaterialList>();

                foreach (Transform child in materialList.EyeObj.transform)
                {
                    Renderer skinRend = child.gameObject.GetComponent<Renderer>();
                    skinRend.material = materialList.Eye_Materials[((ManCustomiseSettings)settings).eye];
                }

                foreach (Transform child in materialList.ClosedJacketObj.transform)
                {
                    Renderer skinRend = child.gameObject.GetComponent<Renderer>();
                    skinRend.material = materialList.JacketMaterials[((ManCustomiseSettings)settings).jacketC];
                }

                foreach (Transform child in materialList.OpenJacketObj.transform)
                {
                    Renderer skinRend = child.gameObject.GetComponent<Renderer>();
                    skinRend.material = materialList.JacketMaterials[((ManCustomiseSettings)settings).jacketC];
                }

                foreach (Transform child in materialList.ClosedShirtObj.transform)
                {
                    Renderer skinRend = child.gameObject.GetComponent<Renderer>();
                    skinRend.material = materialList.ShirtMaterials[((ManCustomiseSettings)settings).shirtC];
                }

                foreach (Transform child in materialList.OpenShirtObj.transform)
                {
                    Renderer skinRend = child.gameObject.GetComponent<Renderer>();
                    skinRend.material = materialList.ShirtMaterials[((ManCustomiseSettings)settings).shirtC];
                }

                foreach (Transform child in materialList.ShirtObj.transform)
                {
                    Renderer skinRend = child.gameObject.GetComponent<Renderer>();
                    skinRend.material = materialList.ShirtMaterials[((ManCustomiseSettings)settings).shirtC];
                }

                foreach (Transform child in materialList.ShirtWObj.transform)
                {
                    Renderer skinRend = child.gameObject.GetComponent<Renderer>();
                    skinRend.material = materialList.ShirtMaterials[((ManCustomiseSettings)settings).shirtC];
                }

                /*foreach (Transform child in materialList.ClosedWaistcoatObj.transform)
                {

                    Renderer skinRend = child.gameObject.GetComponent<Renderer>();
                    skinRend.material = materialList.WaistcoatMaterials[((ManCustomiseSettings)settings).waistcoatCo];
                }
                foreach (Transform child in materialList.OpenWaistcoatObj.transform)
                {
                    Renderer skinRend = child.gameObject.GetComponent<Renderer>();
                    skinRend.material = materialList.WaistcoatMaterials[((ManCustomiseSettings)settings).waistcoatCo];
                }
                foreach (Transform child in materialList.WaistcoatObj.transform)
                {
                    Renderer skinRend = child.gameObject.GetComponent<Renderer>();
                    skinRend.material = materialList.WaistcoatMaterials[((ManCustomiseSettings)settings).waistcoatCo];
                }*/

                /*foreach (Transform child in materialList.ButterflyTieObj.transform)
                {
                    Renderer skinRend = child.gameObject.GetComponent<Renderer>();
                    skinRend.material = materialList.TieMaterials[((ManCustomiseSettings)settings).tieCo];
                }
                foreach (Transform child in materialList.TieObj.transform)
                {
                    Renderer skinRend = child.gameObject.GetComponent<Renderer>();
                    skinRend.material = materialList.TieMaterials[((ManCustomiseSettings)settings).tieCo];
                }
                foreach (Transform child in materialList.ClosedTieObj.transform)
                {
                    Renderer skinRend = child.gameObject.GetComponent<Renderer>();
                    skinRend.material = materialList.TieMaterials[((ManCustomiseSettings)settings).tieCo];
                }
                foreach (Transform child in materialList.OpenTieWObj.transform)
                {
                    Renderer skinRend = child.gameObject.GetComponent<Renderer>();
                    skinRend.material = materialList.TieMaterials[((ManCustomiseSettings)settings).tieCo];
                }*/

                foreach (Transform child in materialList.PantsObj.transform)
                {
                    Renderer skinRend = child.gameObject.GetComponent<Renderer>();
                    skinRend.material = materialList.PantsMaterials[((ManCustomiseSettings)settings).pantsC];
                }

                foreach (Transform child in materialList.PantsLoopsObj.transform)
                {
                    Renderer skinRend = child.gameObject.GetComponent<Renderer>();
                    skinRend.material = materialList.PantsMaterials[((ManCustomiseSettings)settings).pantsC];
                }

                foreach (Transform child in materialList.ShoesObj.transform)
                {
                    Renderer skinRend = child.gameObject.GetComponent<Renderer>();
                    skinRend.material = materialList.ShoesMaterials[((ManCustomiseSettings)settings).shoesC];
                }

                /*foreach (Transform child in materialList.OpenHandkerchiefObj.transform)
                {
                    Renderer skinRend = child.gameObject.GetComponent<Renderer>();
                    skinRend.material = materialList.HandkerchiefMaterials[((ManCustomiseSettings)settings).handkerchiefCo];
                }
                foreach (Transform child in materialList.ClosedHandkerchiefObj.transform)
                {
                    Renderer skinRend = child.gameObject.GetComponent<Renderer>();
                    skinRend.material = materialList.HandkerchiefMaterials[((ManCustomiseSettings)settings).handkerchiefCo];
                }*/

                //HairA==========================
                foreach (Transform child in materialList.HairObjects[0].transform)
                {
                    Renderer skinRend = child.gameObject.GetComponent<Renderer>();
                    skinRend.material = materialList.HairA_Materials[((ManCustomiseSettings)settings).hairC];
                }

                //HairB==========================
                foreach (Transform child in materialList.HairObjects[1].transform)
                {
                    Renderer skinRend = child.gameObject.GetComponent<Renderer>();
                    skinRend.material = materialList.HairB_Materials[((ManCustomiseSettings)settings).hairC];
                }

                //HairC==========================
                foreach (Transform child in materialList.HairObjects[2].transform)
                {
                    Renderer skinRend = child.gameObject.GetComponent<Renderer>();
                    skinRend.material = materialList.HairC_Materials[((ManCustomiseSettings)settings).hairC];
                }

                //HairD==========================
                foreach (Transform child in materialList.HairObjects[3].transform)
                {
                    Renderer skinRend = child.gameObject.GetComponent<Renderer>();
                    skinRend.material = materialList.HairD_Materials[((ManCustomiseSettings)settings).hairC];
                }

                //HairE==========================
                foreach (Transform child in materialList.HairObjects[4].transform)
                {
                    Renderer skinRend = child.gameObject.GetComponent<Renderer>();
                    skinRend.material = materialList.HairE_Materials[((ManCustomiseSettings)settings).hairC];
                }

                //HairF==========================
                foreach (Transform child in materialList.HairObjects[5].transform)
                {
                    Renderer skinRend = child.gameObject.GetComponent<Renderer>();
                    skinRend.material = materialList.HairF_Materials[((ManCustomiseSettings)settings).hairC];
                }

                //HairG==========================
                foreach (Transform child in materialList.HairObjects[6].transform)
                {
                    Renderer skinRend = child.gameObject.GetComponent<Renderer>();
                    skinRend.material = materialList.HairG_Materials[((ManCustomiseSettings)settings).hairC];
                }

                //HairH==========================
                foreach (Transform child in materialList.HairObjects[7].transform)
                {
                    Renderer skinRend = child.gameObject.GetComponent<Renderer>();
                    skinRend.material = materialList.HairH_Materials[((ManCustomiseSettings)settings).hairC];
                }

                //HairI==========================
                foreach (Transform child in materialList.HairObjects[8].transform)
                {
                    Renderer skinRend = child.gameObject.GetComponent<Renderer>();
                    skinRend.material = materialList.HairI_Materials[((ManCustomiseSettings)settings).hairC];
                }

                materialList.BeltObj.SetActive(false);
                materialList.ButterflyTieObj.SetActive(false);
                materialList.ClosedJacketObj.SetActive(false);
                materialList.ClosedShirtObj.SetActive(false);
                materialList.ClosedTieObj.SetActive(false);
                materialList.ClosedWaistcoatObj.SetActive(false);
                materialList.OpenJacketObj.SetActive(false);
                materialList.OpenShirtObj.SetActive(false);
                materialList.OpenTieWObj.SetActive(false);
                materialList.OpenWaistcoatObj.SetActive(false);

                materialList.PantsLoopsObj.SetActive(false);
                materialList.ShirtObj.SetActive(false);
                materialList.ShirtWObj.SetActive(false);

                materialList.TieObj.SetActive(false);
                materialList.WaistcoatObj.SetActive(false);
                materialList.OpenHandkerchiefObj.SetActive(false);
                materialList.ClosedHandkerchiefObj.SetActive(false);

                //Set hair
                for (int i = 0; i < materialList.HairObjects.Length; i++)
                {
                    materialList.HairObjects[i].SetActive(false);
                }

                materialList.HairObjects[((ManCustomiseSettings)settings).hairT].SetActive(true);

                //Set glasses
                for (int i = 0; i < materialList.GlassesObjects.Length; i++)
                {
                    materialList.GlassesObjects[i].SetActive(false);
                }

                /*
                if (glassesTy > 0)
                {
                    assetsList.GlassesObjects[faceTy].SetActive(true);
                    foreach (Transform child in assetsList.GlassesObjects[faceTy].transform)
                    {

                        Renderer skinRend = child.gameObject.GetComponent<Renderer>();

                        mat = new Material[2];
                        mat[0] = assetsList.Glass_Materials[0];
                        mat[1] = assetsList.Glass_Materials[glassesTy];
                        skinRend.materials = mat;
                    }
                }*/

                // set face
                for (int i = 0; i < materialList.FaceObjects.Length; i++)
                {
                    materialList.FaceObjects[i].SetActive(false);
                }

                materialList.FaceObjects[((ManCustomiseSettings)settings).face].SetActive(true);

                foreach (Transform child in materialList.FaceObjects[((ManCustomiseSettings)settings).face].transform)
                {
                    string oName = child.gameObject.name;
                    string hName = oName.Substring(0, 1);

                    if (hName == "F")
                    {
                        Renderer skinRend = child.gameObject.GetComponent<Renderer>();
                        skinRend.material = materialList.Skin_Materials[((ManCustomiseSettings)settings).skinT];
                    }
                }

                /*
                if (beltTy == 0)
                {
                    assetsList.BeltObj.SetActive(true);
                }
                */

                if (((ManCustomiseSettings)settings).jacketT == 0)
                {
                    materialList.OpenJacketObj.SetActive(true);
                    materialList.OpenShirtObj.SetActive(true);
                    //  materialList.OpenHandkerchiefObj.SetActive(true);

                    /*if (tieTy == 0)
                    {
                        assetsList.TieObj.SetActive(true);

                    }
                    if (waistcoatTy == 0)
                    {
                        assetsList.OpenWaistcoatObj.SetActive(true);
                        assetsList.BeltObj.SetActive(false);
                        if (tieTy == 0)
                        {
                            assetsList.TieObj.SetActive(false);
                            assetsList.ClosedTieObj.SetActive(true);
                        }
                    }*/

                }
                else if (((ManCustomiseSettings)settings).jacketT == 1)
                {
                    materialList.ClosedJacketObj.SetActive(true);
                    materialList.ClosedShirtObj.SetActive(true);
                    // materialList.ClosedHandkerchiefObj.SetActive(true);
                    materialList.BeltObj.SetActive(false);

                    /*if (tieTy == 0)
                    {
                        assetsList.TieObj.SetActive(false);
                        assetsList.OpenTieWObj.SetActive(true);
                    }
                    if (waistcoatTy == 0)
                    {
                        assetsList.BeltObj.SetActive(false);
                        assetsList.ClosedWaistcoatObj.SetActive(true);
                    }*/
                }
                else if (((ManCustomiseSettings)settings).jacketT == 2)
                {
                    materialList.ShirtObj.SetActive(true);
                    materialList.PantsLoopsObj.SetActive(true);

                    /*if (tieTy == 0)
                    {
                        assetsList.TieObj.SetActive(true);

                    }
                    if (waistcoatTy == 0)
                    {
                        assetsList.BeltObj.SetActive(false);
                        assetsList.ShirtObj.SetActive(false);
                        assetsList.ShirtWObj.SetActive(true);
                        assetsList.WaistcoatObj.SetActive(true);
                        assetsList.OpenTieWObj.SetActive(true);
                        assetsList.TieObj.SetActive(false);
                        assetsList.PantsLoopsObj.SetActive(false);
                    }*/

                }
                /*
                if (tieTy == 1)
                {
                    assetsList.ButterflyTieObj.SetActive(true);
                    assetsList.OpenTieWObj.SetActive(false);
                }
                if (tieTy == 2)
                {
                    assetsList.ButterflyTieObj.SetActive(false);
                    assetsList.OpenTieWObj.SetActive(false);
                }
                if (handkerchiefTy == 1)
                {
                    assetsList.OpenHandkerchiefObj.SetActive(false);
                    assetsList.ClosedHandkerchiefObj.SetActive(false);
                }*/

                //need to update the local enums with the settings
                faceType = (FaceType)((ManCustomiseSettings)settings).face;
                skinType = (SkinType)((ManCustomiseSettings)settings).skinT;
                eyeCol = (EyeColor)((ManCustomiseSettings)settings).eye;
                jacketType = (Jacket)((ManCustomiseSettings)settings).jacketT;
                jacketCol = (JacketColor)((ManCustomiseSettings)settings).jacketC;
                shirtCol = (ShirtColor)((ManCustomiseSettings)settings).shirtC;
                pantsCol = (PantsColor)((ManCustomiseSettings)settings).pantsC;
                hair = (Hair)((ManCustomiseSettings)settings).hairT;
                hairCol = (HairColor)((ManCustomiseSettings)settings).hairC;
                shoesCol = (ShoesColor)((ManCustomiseSettings)settings).shoesC;

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
        /// Update customise values
        /// </summary>
        public void UpdateValues()
        {
            Customise(GetSettings());
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
            hash.Add("SEX", "Male");
            hash.Add("FT", (int)faceType);
            hash.Add("SCT", (int)skinType);
            hash.Add("EC", (int)eyeCol);
            hash.Add("HT", (int)hair);
            hash.Add("HC", (int)hairCol);
            hash.Add("JT", (int)jacketType);
            hash.Add("JC", (int)jacketCol);
            hash.Add("SHC", (int)shirtCol);
            hash.Add("WCC", (int)waistcoatCol);
            hash.Add("TC", (int)tieCol);
            hash.Add("PC", (int)pantsCol);
            hash.Add("SC", (int)shoesCol);

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
            eyeCol = (EyeColor)hash["EC"];
            jacketType = (Jacket)hash["JT"];
            jacketCol = (JacketColor)hash["JC"];
            shirtCol = (ShirtColor)hash["SHC"];
            pantsCol = (PantsColor)hash["PC"];
            waistcoatCol = (WaistcoatColor)hash["WCC"];
            tieCol = (TieColor)hash["TC"];
            hair = (Hair)hash["HT"];
            hairCol = (HairColor)hash["HC"];
            shoesCol = (ShoesColor)hash["SC"];

            UpdateValues();
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

        public enum EyeColor
        {
            Brown,
            Blue,
            Green,
            Black,
            Gray,
            LightBrown
        }

        public enum Hair
        {
            HairA,
            HairB,
            HairC,
            HairD,
            HairE,
            HairF,
            HairG,
            HairH,
            HairI
        }

        public enum HairColor
        {
            Blonde,
            Brown,
            Gray,
            Brunette,
            Black
        }

        public enum Jacket
        {
            Open,
            Closed,
            No
        }

        public enum JacketColor
        {
            Black,
            Charcoal,
            Navy,
            Grey,
            LightGray,
            White,
            Vintage,
            Blue,
            Tan,
            Brown
        }

        public enum ShirtColor
        {
            Black,
            Charcoal,
            Navy,
            Grey,
            LightGray,
            White,
            LightBlue,
            Blue
        }

        public enum WaistcoatColor
        {
            Black,
            Charcoal,
            Navy,
            Grey,
            LightGray,
            White,
            Vintage,
            Blue,
            Tan,
            Brown
        }

        public enum TieColor
        {
            Black,
            White,
            Blue,
            RedBlue,
            Red,
            BlueB,
            Purple,
            LightBlue,
            Gray,
            Brown
        }

        public enum PantsColor
        {
            Black,
            Charcoal,
            Navy,
            Grey,
            LightGray,
            White,
            Vintage,
            Blue,
            Tan,
            Brown
        }

        public enum ShoesColor
        {
            Black,
            Brown,
            RedBrown,
            White
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(ManCustomise), true)]
        public class ManCustomise_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("type"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("faceType"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("skinType"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("eyeCol"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("hair"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("hairCol"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("jacketType"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("jacketCol"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("shirtCol"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("waistcoatCol"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("tieCol"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("pantsCol"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("shoesCol"), true);

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
