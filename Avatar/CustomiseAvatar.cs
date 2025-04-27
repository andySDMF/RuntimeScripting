using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class CustomiseAvatar : MonoBehaviour
    {
        [Header("Instantiate")]
        public Transform avatarContainer;

        [Header("Sex")]
        public GameObject man;
        public GameObject woman;
        public Sex currentSex = Sex.Female;

        [Header("Custom Avatars")]
        public List<GameObject> customAvatars = new List<GameObject>();

        [Header("UI")]
        public TMP_Text sexText;
        public TMP_Text haircolorText;
        public TMP_Text hairstyleText;
        public TMP_Text faceText;
        public TMP_Text skinText;
        public TMP_Text upperText;
        public TMP_Text bottomText;
        public TMP_Text shoeText;
        public TMP_Text customerFixedAvatarText;

        public GameObject mainMenu;
        public GameObject customFixedAvatarMenu;
        public Transform colorPallete;
        public Transform accessoriesPallete;

        [Header("Camera")]
        public Transform cameraLerp;
        public float speed = 5.0f;
        

        private ICustomAvatar customiseMan;
        private ICustomAvatar customiseWoman;
        private Vector3 lerpPosition;
        private Vector3 defaultPosition;
        private string currentEditType = "";
        private string currentAccessoryType = "";

        [System.Serializable]
        public enum Sex { Female, Male }

        private Dictionary<string, TMP_Text> displayValues;
        private bool hasInit = false;
        private int m_currentCustomAvatar = 0;

        /// <summary>
        /// Return the interface of the current avatar based on sex
        /// </summary>
        public ICustomAvatar CustomAvatar
        {
            get
            {
                if (AppManager.Instance.Data.FixedAvatarUsed)
                {
                    Debug.Log("Cannot return interface on custom fixed avatars");
                    return null;
                }
                else
                {
                    if (currentSex.Equals(Sex.Female))
                    {
                        return customiseWoman;
                    }
                    else
                    {
                        return customiseMan;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the current fixed avatar
        /// </summary>
        public string GetAvatarName
        {
            get
            {
                if (AppManager.Instance.Data.FixedAvatarUsed)
                {
                    return customAvatars[m_currentCustomAvatar].name;
                }
                else
                {
                    if (currentSex.Equals(Sex.Female))
                    {
                        return woman.name;
                    }
                    else
                    {
                        return man.name;
                    }
                }
            }
        }

        private void Awake()
        {
            Initialise();
        }

        /// <summary>
        /// Initialise the setup of this script
        /// </summary>
        public void Initialise()
        {
            if (hasInit) return;

            hasInit = true;

            //create the avatar based of the project setting in app manager
            if (avatarContainer)
            {
                if (!AppManager.Instance.Data.FixedAvatarUsed)
                {
                    if (AppManager.Instance.Settings.projectSettings.avatarSetupMode.Equals(AvatarSetupMode.Standard))
                    {
                        UnityEngine.Object prefab = Resources.Load(AppManager.Instance.Settings.playerSettings.standardMan);
                        GameObject go = (GameObject)Instantiate(prefab, Vector3.zero, Quaternion.identity, avatarContainer);
                        go.name = AppManager.Instance.Settings.playerSettings.standardMan;
                        go.transform.localPosition = Vector3.zero;
                        go.transform.localScale = AppManager.Instance.Settings.playerSettings.scaleStandardMan;
                        go.transform.localEulerAngles = Vector3.zero;
                        go.layer = LayerMask.NameToLayer("AvatarConfig");

                        foreach (var child in go.GetComponentsInChildren<Transform>(true))
                        {
                            child.gameObject.layer = LayerMask.NameToLayer("AvatarConfig");
                        }

                        man = go;

                        prefab = Resources.Load(AppManager.Instance.Settings.playerSettings.standardWoman);
                        go = (GameObject)Instantiate(prefab, Vector3.zero, Quaternion.identity, avatarContainer);
                        go.name = AppManager.Instance.Settings.playerSettings.standardWoman;
                        go.transform.localPosition = Vector3.zero;
                        go.transform.localScale = AppManager.Instance.Settings.playerSettings.scaleStandardWoman;
                        go.transform.localEulerAngles = Vector3.zero;
                        go.layer = LayerMask.NameToLayer("AvatarConfig");

                        foreach (var child in go.GetComponentsInChildren<Transform>(true))
                        {
                            child.gameObject.layer = LayerMask.NameToLayer("AvatarConfig");
                        }

                        woman = go;
                    }
                    else
                    {
                        UnityEngine.Object prefab = Resources.Load(AppManager.Instance.Settings.playerSettings.simpleMan);
                        GameObject go = (GameObject)Instantiate(prefab, Vector3.zero, Quaternion.identity, avatarContainer);
                        go.name = AppManager.Instance.Settings.playerSettings.simpleMan;
                        go.transform.localPosition = Vector3.zero;
                        go.transform.localScale = AppManager.Instance.Settings.playerSettings.scaleSimpleMan;
                        go.transform.localEulerAngles = Vector3.zero;
                        go.layer = LayerMask.NameToLayer("AvatarConfig");

                        foreach (var child in go.GetComponentsInChildren<Transform>(true))
                        {
                            child.gameObject.layer = LayerMask.NameToLayer("AvatarConfig");
                        }

                        man = go;

                        prefab = Resources.Load(AppManager.Instance.Settings.playerSettings.simpleWoman);
                        go = (GameObject)Instantiate(prefab, Vector3.zero, Quaternion.identity, avatarContainer);
                        go.name = AppManager.Instance.Settings.playerSettings.simpleWoman;
                        go.transform.localPosition = Vector3.zero;
                        go.transform.localScale = AppManager.Instance.Settings.playerSettings.scaleSimpleWoman;
                        go.transform.localEulerAngles = Vector3.zero;
                        go.layer = LayerMask.NameToLayer("AvatarConfig");

                        foreach (var child in go.GetComponentsInChildren<Transform>(true))
                        {
                            child.gameObject.layer = LayerMask.NameToLayer("AvatarConfig");
                        }

                        woman = go;
                    }
                }
                else
                {
                    customAvatars.Clear();

                    for(int i = 0; i < AppManager.Instance.Settings.playerSettings.fixedAvatars.Count; i++)
                    {
                        UnityEngine.Object prefab = Resources.Load(AppManager.Instance.Settings.playerSettings.fixedAvatars[i]);
                        GameObject go = (GameObject)Instantiate(prefab, Vector3.zero, Quaternion.identity, avatarContainer);
                        go.name = AppManager.Instance.Settings.playerSettings.fixedAvatars[i];
                        go.transform.localPosition = Vector3.zero;
                        go.transform.localEulerAngles = Vector3.zero;
                        go.transform.localScale = AppManager.Instance.Settings.playerSettings.fixedAvatarScales[i];

                        if (!go.GetComponent<FixedAvatar>())
                        {
                            go.AddComponent<FixedAvatar>();
                        }
                        
                        go.layer = LayerMask.NameToLayer("AvatarConfig");

                        foreach (var child in go.GetComponentsInChildren<Transform>(true))
                        {
                            child.gameObject.layer = LayerMask.NameToLayer("AvatarConfig");
                        }

                        customAvatars.Add(go);
                    }
                }
            }

            if (AppManager.Instance.Settings.projectSettings.avatarSetupMode.Equals(AvatarSetupMode.Standard))
            {
                colorPallete.localScale = Vector3.zero;
                accessoriesPallete.localScale = Vector3.zero;
            }

            if (!AppManager.Instance.Data.FixedAvatarUsed)
            {
                if (man == null || woman == null)
                {
                    Destroy(this);
                }
            }
            else
            {
                if(customAvatars.Count > 0)
                {
                    m_currentCustomAvatar = 0;
                }

                mainMenu.SetActive(false);
                customFixedAvatarMenu.SetActive(true);
            }

            //get camera and sgtore local caches
            if(cameraLerp == null)
            {
                cameraLerp = FindFirstObjectByType<AvatarCamera>().transform;
            }

            defaultPosition = new Vector3(cameraLerp.position.x, cameraLerp.position.y, cameraLerp.position.z);
            lerpPosition = new Vector3(cameraLerp.position.x, cameraLerp.position.y, cameraLerp.position.z);

            //get interaces
            if(man != null)
            {
                customiseMan = man.GetComponentInChildren<ICustomAvatar>(true);
                customiseMan.UpdateValues();
            }

            if(woman != null)
            {
                customiseWoman = woman.GetComponentInChildren<ICustomAvatar>(true);
                customiseWoman.UpdateValues();
            }

            //set up UI dictionary of display elements
            displayValues = new Dictionary<string, TMP_Text>();
            displayValues.Add("Sex", sexText);
            displayValues.Add("HairColor", haircolorText);
            displayValues.Add("HairStyle", hairstyleText);
            displayValues.Add("Face", faceText);
            displayValues.Add("Skin", skinText);
            displayValues.Add("Upper", upperText);
            displayValues.Add("Bottom", bottomText);
            displayValues.Add("Shoe", shoeText);

            //set based on IndexedDB data
            if (!string.IsNullOrEmpty(AppManager.Instance.Data.CustomiseJson))
            {
                Hashtable hash = GetAvatarHashFromString(AppManager.Instance.Data.CustomiseJson);

                if(hash.ContainsKey("SEX"))
                {
                    if(hash["SEX"].Equals("Male"))
                    {
                        currentSex = Sex.Male;
                        customiseMan.SetProperties(hash);
                    }
                    else
                    {
                        currentSex = Sex.Female;
                        customiseWoman.SetProperties(hash);
                    }
                }
            }
        }

        private void OnEnable()
        {
            lerpPosition = new Vector3(defaultPosition.x, defaultPosition.y, defaultPosition.z);
        }

        private void Update()
        {
            if (cameraLerp != null)
            {
                cameraLerp.position = Vector3.Lerp(cameraLerp.position, lerpPosition, 5 * Time.deltaTime);
            }

            if (AppManager.Instance.Data.FixedAvatarUsed)
            {
                for (int i = 0; i < customAvatars.Count; i++)
                {
                    if(i != m_currentCustomAvatar)
                    {
                        customAvatars[i].SetActive(false);
                    }
                    else
                    {
                        customAvatars[i].SetActive(true);
                        string nm = customAvatars[i].name.Contains("RPM_") ? customAvatars[i].name.Substring(4) : customAvatars[i].name;
                        customerFixedAvatarText.text = nm;
                    }
                }
            }
            else
            {
                //man/women visibility based on sex
                if (currentSex.Equals(Sex.Female))
                {
                    man.SetActive(false);
                    woman.SetActive(true);

                    customiseWoman.UpdateDisplay(displayValues);
                }
                else
                {
                    man.SetActive(true);
                    woman.SetActive(false);

                    customiseMan.UpdateDisplay(displayValues);
                }
            }
        }

        /// <summary>
        /// Called to reset the camera position
        /// </summary>
        public void ResetCamera()
        {
            OnEnable();
        }

        /// <summary>
        /// Move camera to new tranform position
        /// </summary>
        /// <param name="newPos"></param>
        public void UpdateCamera(Transform newPos)
        {
            lerpPosition = new Vector3(newPos.position.x, newPos.position.y, newPos.position.z);
        }

        /// <summary>
        /// Called to change the accerory type for on the face
        /// </summary>
        /// <param name="type"></param>
        public void AccessoryType(string type)
        {
            currentAccessoryType = type;

            if (currentSex.Equals(Sex.Female))
            {
                customiseWoman.CustomiseAction.ChangeAccessory(currentAccessoryType);
            }
            else
            {
                customiseMan.CustomiseAction.ChangeAccessory(currentAccessoryType);
            }
        }

        /// <summary>
        /// Called to edit the current customisation type
        /// </summary>
        /// <param name="type"></param>
        public void EditType(string type)
        {
            currentEditType = type;
        }

        /// <summary>
        /// Called to update a color of an item on the avatar
        /// </summary>
        /// <param name="n"></param>
        public void ChangeFixedColor(int n)
        {
            if (currentSex.Equals(Sex.Female))
            {
                customiseWoman.CustomiseAction.ChangeColor(currentEditType, n);
            }
            else
            {
                customiseMan.CustomiseAction.ChangeColor(currentEditType, n);
            }
        }

        /// <summary>
        /// Called to set the fixed avatar on display
        /// </summary>
        /// <param name="id"></param>
        public void SetFixedAvatar(string id)
        {
            for(int i = 0; i < customAvatars.Count; i++)
            {
                if(customAvatars[i].name.Equals(id))
                {
                    customAvatars[i].SetActive(true);
                    m_currentCustomAvatar = i;
                }
                else
                {
                    customAvatars[i].SetActive(false);
                }
            }
        }

        /// <summary>
        /// Move to next CustomFixedAvatar
        /// </summary>
        public void NextCustomFixedAvatar()
        {
            m_currentCustomAvatar++;

            if(m_currentCustomAvatar > customAvatars.Count - 1)
            {
                m_currentCustomAvatar = 0;
            }
        }

        /// <summary>
        /// Move to previous CustomFixedAvatar
        /// </summary>
        public void PreviousCustomFixedAvatar()
        {
            m_currentCustomAvatar--;

            if (m_currentCustomAvatar < 0)
            {
                m_currentCustomAvatar = customAvatars.Count - 1;
            }
        }

        /// <summary>
        /// Move to next sex
        /// </summary>
        public void NextSex()
        {
            currentSex++;

            if ((int)currentSex > System.Enum.GetNames(typeof(Sex)).Length - 1)
            {
                currentSex = 0;
            }
        }

        /// <summary>
        /// Move to previous sex
        /// </summary>
        public void PreviousSex()
        {
            currentSex -= 1;

            if ((int)currentSex < 0)
            {
                currentSex = Sex.Male;
            }
        }

        /// <summary>
        /// Move to next HairsTyle
        /// </summary>
        public void NextHairStyle()
        {
            if (currentSex.Equals(Sex.Female))
            {
                customiseWoman.CustomiseAction.NextHairStyle();
            }
            else
            {
                customiseMan.CustomiseAction.NextHairStyle();
            }
        }

        /// <summary>
        /// Move to previous HairStyle
        /// </summary>
        public void PreviousHairStyle()
        {
            if (currentSex.Equals(Sex.Female))
            {
                customiseWoman.CustomiseAction.PreviousHairStyle();
            }
            else
            {
                customiseMan.CustomiseAction.PreviousHairStyle();
            }
        }

        /// <summary>
        /// Move to next HairColor
        /// </summary>
        public void NextHairColor()
        {
            if (currentSex.Equals(Sex.Female))
            {
                customiseWoman.CustomiseAction.NextHairColor();
            }
            else
            {
                customiseMan.CustomiseAction.NextHairColor();
            }
        }

        /// <summary>
        /// Move to previous HairColor
        /// </summary>
        public void PreviousHairColor()
        {
            if (currentSex.Equals(Sex.Female))
            {
                customiseWoman.CustomiseAction.PreviousHairColor();
            }
            else
            {
                customiseMan.CustomiseAction.PreviousHairColor();
            }
        }

        /// <summary>
        /// Move to next FaceType
        /// </summary>
        public void NextFaceType()
        {
            if (currentSex.Equals(Sex.Female))
            {
                customiseWoman.CustomiseAction.NextFaceType();
            }
            else
            {
                customiseMan.CustomiseAction.NextFaceType();
            }
        }

        /// <summary>
        /// Move to previous FaceType
        /// </summary>
        public void PreviousFaceType()
        {
            if (currentSex.Equals(Sex.Female))
            {
                customiseWoman.CustomiseAction.PreviousFaceType();
            }
            else
            {
                customiseMan.CustomiseAction.PreviousFaceType();
            }
        }

        /// <summary>
        /// Move to next SkinType
        /// </summary>
        public void NextSkinType()
        {
            if (currentSex.Equals(Sex.Female))
            {
                customiseWoman.CustomiseAction.NextSkinType();
            }
            else
            {
                customiseMan.CustomiseAction.NextSkinType();
            }
        }

        /// <summary>
        /// Move to prebious SkinType
        /// </summary>
        public void PreviousSkinType()
        {
            if (currentSex.Equals(Sex.Female))
            {
                customiseWoman.CustomiseAction.PreviousSkinType();
            }
            else
            {
                customiseMan.CustomiseAction.PreviousSkinType();
            }
        }

        /// <summary>
        /// Move to next Upper body Type
        /// </summary>
        public void NextUpper()
        {
            if (currentSex.Equals(Sex.Female))
            {
                customiseWoman.CustomiseAction.NextUpper();
            }
            else
            {
                customiseMan.CustomiseAction.NextUpper();
            }
        }

        /// <summary>
        /// Move to previous Upper body Type
        /// </summary>
        public void PreviousUpper()
        {
            if (currentSex.Equals(Sex.Female))
            {
                customiseWoman.CustomiseAction.PreviousUpper();
            }
            else
            {
                customiseMan.CustomiseAction.PreviousUpper();
            }
        }

        /// <summary>
        /// Move to next bottom body Type
        /// </summary>
        public void NextBottom()
        {
            if (currentSex.Equals(Sex.Female))
            {
                customiseWoman.CustomiseAction.NextBottom();
            }
            else
            {
                customiseMan.CustomiseAction.NextBottom();
            }
        }

        /// <summary>
        /// Move to previous Bottom body Type
        /// </summary>
        public void PreviousBottom()
        {
            if (currentSex.Equals(Sex.Female))
            {
                customiseWoman.CustomiseAction.PreviousBottom();
            }
            else
            {
                customiseMan.CustomiseAction.PreviousBottom();
            }
        }

        /// <summary>
        /// Move to Next ShoeColor
        /// </summary>
        public void NextShoe()
        {
            if (currentSex.Equals(Sex.Female))
            {
                customiseWoman.CustomiseAction.NextShoe();
            }
            else
            {
                customiseMan.CustomiseAction.NextShoe();
            }
        }

        /// <summary>
        /// Move to previous ShoeColor
        /// </summary>
        public void PreviousShoe()
        {
            if (currentSex.Equals(Sex.Female))
            {
                customiseWoman.CustomiseAction.PreviousShoe();
            }
            else
            {
                customiseMan.CustomiseAction.PreviousShoe();
            }
        }

        public static Hashtable GetAvatarHashFromString(string str)
        {
            Hashtable hash = new Hashtable();
            string[] outerSplit = str.Split('|');

            for (int i = 0; i < outerSplit.Length; i++)
            {
                string[] innerSplit = outerSplit[i].Split('*');
                int n = 0;

                if(innerSplit.Length > 1)
                {
                    if (int.TryParse(innerSplit[1], out n))
                    {
                        hash.Add(innerSplit[0], n);
                    }
                    else
                    {
                        hash.Add(innerSplit[0], innerSplit[1]);
                    }
                }
            }

            return hash;
        }

        public static string GetAvatarHashString(Hashtable hash)
        {
            string str = "";
            int count = 0;

            foreach (DictionaryEntry item in hash)
            {
                str += item.Key + "*" + item.Value + ((count < hash.Values.Count) ? "|" : "");
            }

            return str;
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(CustomiseAvatar), true)]
        public class CustomiseAvatar_Editor : BaseInspectorEditor
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

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("avatarContainer"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("man"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("woman"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("currentSex"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("sexText"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("haircolorText"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("hairstyleText"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("faceText"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("skinText"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("upperText"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("bottomText"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("shoeText"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("customerFixedAvatarText"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("mainMenu"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("customFixedAvatarMenu"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("colorPallete"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("accessoriesPallete"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("cameraLerp"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("speed"), true);

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

    [System.Serializable]
    public enum AvatarType { Standard, Simple }

    /// <summary>
    /// Interface for all avatar customise scripts
    /// </summary>
    public interface ICustomAvatar
    {
        AvatarType Type { get; }

        void Customise(AvatarCustomiseSettings settings);

        void UpdateValues();

        AvatarCustomiseActions CustomiseAction { get; }

        AvatarCustomiseSettings Settings { get; }

        void UpdateDisplay(Dictionary<string, TMP_Text> display);

        Hashtable GetProperties();

        void SetProperties(Hashtable hash);

        CustomiseAvatar.Sex Sex { get; }

        Color[] FixedColors { get; }
    }

    /// <summary>
    /// Base class for avatar settings for customising/randomising
    /// </summary>
    public class AvatarCustomiseSettings
    {
        public virtual AvatarCustomiseSettings Randomise()
        {
            return this;
        }
    }

    /// <summary>
    /// Class to implement customisation actions on local avator customisation scripts - this script will call them on the local avatar
    /// </summary>
    public abstract class AvatarCustomiseActions
    {
        public abstract void NextHairStyle();
        public abstract void PreviousHairStyle();
        public abstract void NextHairColor();
        public abstract void PreviousHairColor();
        public abstract void NextFaceType();
        public abstract void PreviousFaceType();
        public abstract void NextSkinType();
        public abstract void PreviousSkinType();
        public abstract void NextUpper();
        public abstract void PreviousUpper();
        public abstract void NextBottom();
        public abstract void PreviousBottom();
        public abstract void NextShoe();
        public abstract void PreviousShoe();

        public abstract void ChangeColor(string type, int n);

        public abstract void ChangeAccessory(string type);
    }
}
