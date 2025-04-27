using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    public class SwitchSceneTrigger : UniqueID, IPointerEnterHandler, IPointerExitHandler
    {
        public bool useTooltip = false;

        [SerializeField]
        private SceneLoadType loadBy = SceneLoadType.Name;

        [SerializeField]
        private SceneLoadSource loadSource = SceneLoadSource.BuildSetting;

        [SerializeField]
        private string sceneAssetBundle = "";

        [SerializeField]
        private string sceneName = "";

        [SerializeField]
        private int buildIndex = 0;

        [SerializeField]
        private string sceneSpawnPoint = "";

        [SerializeField]
        private LockManager.LockSetting lockSettings;

        [SerializeField]
        private bool lockVisible = true;

        [Tooltip("Only applies if in Third Person View")]
        [SerializeField]
        private CameraStartView cameraView = CameraStartView._Font;

        private bool m_enteredTriggerWhenRoomNotReady = false;
        private bool m_isButton = false;
        private bool m_lockUsed = false;
        private Lock m_lock;
        private bool m_lockOpened = false;

        private bool m_delayTriggered = false;

        public string SceneName
        {
            get
            {
                return sceneName;
            }
        }

        private void Start()
        {
            if (GetComponent<Tooltip>() != null)
            {
                Debug.Log("Tooltips are all internal for switch scene triggers. Destroying tooltip.cs");
                Destroy(GetComponent<Tooltip>());
            }

            Button but = GetComponent<Button>();
            bool containsOnClick = false;
            m_lock = GetComponentInChildren<Lock>();
            m_lockUsed = m_lock != null;

            GetComponent<Collider>().isTrigger = true;
            GetComponent<Renderer>().enabled = false;

            if(m_lockUsed)
            {
                m_lock.IsNetworked = false;
                m_lock.OnUnlock += OnLockUnlocked;
                m_lock.OnCancel += OnLockLocked;
                m_lock.OverrideSettings(lockSettings.useDataAPIPassword, lockSettings.password, lockSettings.displayType);
            }

            if (but == null)
            {
                Button[] buts = GetComponentsInChildren<Button>(true);

                for(int i = 0; i < buts.Length; i++)
                {
                    if (buts[i].name.Contains("Lock")) continue;

                    but = buts[i];
                }
            }

            if (but != null)
            {
                m_isButton = true;

                for (int i = 0; i < but.onClick.GetPersistentEventCount(); i++)
                {
                    if(but.onClick.GetPersistentTarget(i).name == this.name)
                    {
                        containsOnClick = true;
                    }
                }

                if(!containsOnClick)
                {
                    but.onClick.AddListener(OnClick);
                }

                if(m_lockUsed)
                {
                    but.interactable = false;
                }
            }
            else
            {
                if(m_lockUsed)
                {
                    m_lock.transform.localScale = (!lockVisible) ? Vector3.zero : m_lock.transform.localScale;
                    m_lock.IgnoreRaycast = true;
                }
            }
        }

        public void OnTriggerEnter(Collider other)
        {
            if (ProductManager.Instance.isHolding || ItemManager.Instance.IsHolding) return;

            if (other.transform.Equals(PlayerManager.Instance.GetLocalPlayer().TransformObject) && !m_delayTriggered)
            {
                m_delayTriggered = true;
                StartCoroutine(DelayOnEnter(other));
            }
        }

        private IEnumerator DelayOnEnter(Collider other)
        {
            yield return new WaitForSeconds(0.5f);

            m_delayTriggered = false;

            if (AppManager.Instance.Data.CurrentSceneReady)
            {
                if (!m_enteredTriggerWhenRoomNotReady && !PlayerManager.Instance.IsTeleporting)
                {
                    //need to check if lock is used, if true show lock, if false continue
                    if (m_lockUsed)
                    {
                        if (!m_lockOpened)
                        {
                            m_lockOpened = true;
                            LockManager.Instance.SetManualLockSelected(m_lock);
                        }
                    }
                    else
                    {
                        OnClick();
                    }
                }
            }
            else
            {
                if (!m_enteredTriggerWhenRoomNotReady)
                {
                    m_enteredTriggerWhenRoomNotReady = true;
                    StartCoroutine(ReleaseTriggerBlock());
                }
            }
        }

        /// <summary>
        /// Called to release this trigger if spanwed within it
        /// </summary>
        /// <returns></returns>
        private IEnumerator ReleaseTriggerBlock()
        {
            while(!AppManager.Instance.Data.CurrentSceneReady)
            {
                yield return null;
            }

            yield return new WaitForSeconds(1.0f);

            m_enteredTriggerWhenRoomNotReady = false;
        }

        private void OnLockUnlocked()
        {
            OnClick();
        }

        private void OnLockLocked()
        {
            StartCoroutine(Delay());
        }

        private IEnumerator Delay()
        {
            yield return new WaitForSeconds(1.0f);

            m_lockOpened = false;
        }

        /// <summary>
        /// Called to action this trigger
        /// </summary>
        public void OnClick()
        {
            if (m_isButton && !RaycastManager.Instance.UIRaycastOperation(gameObject)) return;

            if(m_isButton)
            {
                RaycastManager.Instance.UIRaycastSelectablePressed(GetComponent<Selectable>());
            }

            //freeze player
            PlayerManager.Instance.FreezePlayer(true);

            //do not cast ray anymore
            RaycastManager.Instance.CastRay = false;

            //save position/rotation on app data for this scene
            AppManager.Instance.Data.UpdatePlayerData(PlayerManager.Instance.GetLocalPlayer());

            string sName = sceneName;

            if(loadBy.Equals(SceneLoadType.BuildIndex))
            {
                sName = UnityEngine.SceneManagement.SceneManager.GetSceneByBuildIndex(buildIndex).name;
            }

            AppManager.Instance.Data.CurrentSceneReady = false;

            AppManager.Instance.Data.UpdatePlayerSceneData(sceneSpawnPoint);

            AnalyticsManager.Instance.PostAnalyticsEvent(EventCategory.Location, EventAction.Enter, AnalyticReference);

            //fade out
            HUDManager.Instance.Fade(FadeOutIn.FadeAction.Out, OnFadeDisconnect, OnFadeComplete, 0.5f);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!RaycastManager.Instance.UIRaycastOperation(gameObject, true)) return;

            if (useTooltip)
            {
                RaycastManager.Instance.CastRay = false;
                TooltipManager.Instance.ShowTooltip(AppManager.Instance.Instances.GetFixedTooltip("SwitchSceneTrigger"));
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            //need to check if tooltip is active
            if (!TooltipManager.Instance.IsVisible) return;

            if (useTooltip)
            {
                TooltipManager.Instance.HideTooltip();
            }

            RaycastManager.Instance.CastRay = true;
        }

        /// <summary>
        /// disconnect player
        /// </summary>
        private void OnFadeDisconnect()
        {
            //disconnect from photon
            MMOManager.Instance.Disconnect();
        }

        /// <summary>
        /// Called when the fade out is complete
        /// </summary>
        private void OnFadeComplete()
        {
            //need to show black screen
            BlackScreen.Instance.Show(true);

            if(loadSource.Equals(SceneLoadSource.BuildSetting))
            {
                //load new scene
                if (loadBy.Equals(SceneLoadType.Name))
                {
                    AppManager.Instance.SceneAsyncOperation(true, sceneName, UnityEngine.SceneManagement.LoadSceneMode.Single, null, true);
                }
                else
                {
                    AppManager.Instance.SceneAsyncOperation(true, buildIndex, UnityEngine.SceneManagement.LoadSceneMode.Single, null, true);
                }
            }
            else
            {
                //load asset
                if (sceneAssetBundle.Contains("http") || sceneAssetBundle.Contains("https"))
                {
                    StartCoroutine(AssetBundleManager.Instance.LoadWebAssetBundle(sceneAssetBundle, OnAssetBundleLoaded));
                }
                else
                {
                    StartCoroutine(AssetBundleManager.Instance.LoadAssetBundleAsync(Application.streamingAssetsPath + "/" + sceneAssetBundle, OnAssetBundleLoaded));
                }
            }
        }

        private void OnAssetBundleLoaded(AssetBundle bundle)
        {
            //if scene
            if (bundle.GetAllScenePaths().Length > 0)
            {
                string[] scenePath = bundle.GetAllScenePaths();
                AppManager.Instance.Data.CurrentAssetBundleScene = bundle;
                AppManager.Instance.SceneAsyncOperation(true, scenePath[0], UnityEngine.SceneManagement.LoadSceneMode.Single, 
                AppManager.Instance.Data.UnloadCurrentAssetBundleScene, true);
            }
        }

        /// <summary>
        /// Draw rotation line for dev
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Vector3 direction = transform.TransformDirection(Vector3.forward) * 5;
            Gizmos.DrawRay(transform.position, direction);
        }

        [System.Serializable]
        private enum SceneLoadType { Name, BuildIndex }

        [System.Serializable]
        private enum SceneLoadSource { BuildSetting, AssetBundle }

#if UNITY_EDITOR
        [CustomEditor(typeof(SwitchSceneTrigger), true), CanEditMultipleObjects]
        public class SwitchSceneTrigger_Editor : UniqueID_Editor
        {
            private SwitchSceneTrigger switchTrigger;
            private AppSettings m_settings;
            private AppInstances m_content;
            private AppInstances.SwitchSceneTriggerID m_triggerReference;

            private string[] m_sceneLabels;
            private int m_selectedSceneLabel = 0;

            private string[] m_sceneLabelsSpawnPoint;
            private int m_selectedSceneLabelSpawnPoint = 0;

            private bool m_hasLock = false;

            private void OnEnable()
            {
                GetBanner();
                Initialise();
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();

                serializedObject.Update();
                DisplayID();
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

                EditorGUILayout.PropertyField(serializedObject.FindProperty("useTooltip"), true);

                EditorGUILayout.PropertyField(serializedObject.FindProperty("loadSource"), true);

                if(serializedObject.FindProperty("loadSource").enumValueIndex == 0)
                {
                    if (serializedObject.FindProperty("loadBy").enumValueIndex == 0)
                    {
                        EditorGUILayout.LabelField("Load Scene", EditorStyles.boldLabel);

                        if (m_sceneLabels != null && m_sceneLabels.Length > 0)
                        {
                            int selected = EditorGUILayout.Popup("Scene", m_selectedSceneLabel, m_sceneLabels);

                            if (selected != m_selectedSceneLabel)
                            {
                                m_selectedSceneLabel = selected;
                                AppInstances.SwitchSceneTriggerID[] temp = m_content.GetSwitchTriggerSceneReferences(m_sceneLabels[m_selectedSceneLabel]).ToArray();
                                m_sceneLabelsSpawnPoint = new string[temp.Length];

                                for (int i = 0; i < temp.Length; i++)
                                {
                                    m_sceneLabelsSpawnPoint[i] = temp[i].triggerObjectName + ":" + temp[i].id;
                                }
                            }

                            serializedObject.FindProperty("sceneName").stringValue = m_sceneLabels[m_selectedSceneLabel];
                        }
                        else
                        {
                            if (Application.isPlaying)
                            {
                                EditorGUILayout.LabelField(serializedObject.FindProperty("sceneName").stringValue, EditorStyles.miniLabel);
                            }
                            else
                            {
                                serializedObject.FindProperty("sceneName").stringValue = "";
                                EditorGUILayout.LabelField("No Scenes Exist!", EditorStyles.miniLabel);
                            }
                        }
                    }
                    else
                    {
                        // EditorGUILayout.PropertyField(serializedObject.FindProperty("buildIndex"), true);
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("Load Asset Bundle", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("sceneAssetBundle"), true);
                }

                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                EditorGUILayout.LabelField("Third Person Camera", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("cameraView"), true);

                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                EditorGUILayout.LabelField("Scene Spawn Point", EditorStyles.boldLabel);

                if(m_sceneLabelsSpawnPoint != null && m_sceneLabelsSpawnPoint.Length > 0)
                {
                    m_selectedSceneLabelSpawnPoint = EditorGUILayout.Popup("Point", m_selectedSceneLabelSpawnPoint, m_sceneLabelsSpawnPoint);
                    serializedObject.FindProperty("sceneSpawnPoint").stringValue = m_sceneLabelsSpawnPoint[m_selectedSceneLabelSpawnPoint].Split(':')[1];
                }
                else
                {
                    if (Application.isPlaying)
                    {
                        EditorGUILayout.LabelField(serializedObject.FindProperty("sceneSpawnPoint").stringValue, EditorStyles.miniLabel);
                    }
                    else
                    {
                        serializedObject.FindProperty("sceneSpawnPoint").stringValue = "";
                        EditorGUILayout.LabelField("No Spawn Points Exist!", EditorStyles.miniLabel);
                    }
                }

                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                string buttonName = m_hasLock ? "Remove Lock" : "Add Lock";

                if (GUILayout.Button(buttonName))
                { 
                    if(m_hasLock)
                    {
                        GameObject gLock = switchTrigger.gameObject.GetComponentInChildren<Lock>().gameObject;
                        DestroyImmediate(gLock);
                        m_hasLock = false;
                    }
                    else
                    {
                        //create lock
                        UnityEngine.Object prefab = (GameObject)CoreUtilities.GetAsset<GameObject>("Assets/com.brandlab360.core/Runtime/Prefabs/Lock.prefab");

                        if (prefab != null)
                        {
                            bool isButton = switchTrigger.gameObject.GetComponentInChildren<Button>();

                            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                            go.transform.SetParent(switchTrigger.transform);

                            if(isButton)
                            {
                                go.transform.localPosition = Vector3.zero;
                                RectTransform rectT = go.GetComponent<RectTransform>();
                                rectT.anchorMax = new Vector2(0.5f, 0.5f);
                                rectT.anchorMin = new Vector2(0.5f, 0.5f);
                                rectT.pivot = new Vector2(0.5f, 0.5f);
                                rectT.anchoredPosition = new Vector2(0, -(switchTrigger.GetComponent<RectTransform>().sizeDelta.y / 2));
                                rectT.localPosition = new Vector3(rectT.localPosition.x, rectT.localPosition.y, 0.02f);
                                go.transform.localScale = new Vector3(go.transform.localScale.x, go.transform.localScale.x, go.transform.localScale.z);
                            }
                            else
                            {
                                go.transform.localPosition = switchTrigger.transform.up;
                            }

                            go.transform.localEulerAngles = Vector3.zero;
                            go.name = "Lock";

                            m_hasLock = true;
                        }
                    }
                }

                if(m_hasLock)
                {
                    Resize(switchTrigger.gameObject.GetComponentInChildren<Lock>().transform, 1);

                    EditorGUILayout.LabelField("Lock Settings", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("lockSettings").FindPropertyRelative("useDataAPIPassword"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("lockSettings").FindPropertyRelative("password"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("lockSettings").FindPropertyRelative("displayType"), true);

                    bool isTrigger = switchTrigger.GetComponent<MeshRenderer>() != null;

                    if (isTrigger)
                    {
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("lockVisible"), true);
                    }
                }

                if (GUI.changed || (m_triggerReference != null && !m_triggerReference.triggerObjectName.Equals(switchTrigger.gameObject.name)))
                {
                    m_triggerReference.id = script.ID;
                    m_triggerReference.scene = script.gameObject.scene.name;
                    m_triggerReference.view = switchTrigger.cameraView.ToString();

                    if (m_sceneLabelsSpawnPoint != null && m_sceneLabelsSpawnPoint.Length > 0)
                    {
                        m_triggerReference.spawnPoint = m_sceneLabelsSpawnPoint[m_selectedSceneLabelSpawnPoint];
                    }

                    m_triggerReference.triggerObjectName = switchTrigger.gameObject.name;

                    string sName = serializedObject.FindProperty("sceneName").stringValue;

                    if (switchTrigger.loadBy.Equals(SceneLoadType.BuildIndex))
                    {
                        UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetSceneByBuildIndex(switchTrigger.buildIndex);
                        
                        if(scene != null)
                        {
                            sName = scene.name;
                        }
                    }

                    m_triggerReference.load = (serializedObject.FindProperty("loadSource").enumValueIndex != 0) ? serializedObject.FindProperty("sceneAssetBundle").stringValue : sName;

                    m_content.UpdateSwitchTrigger(m_triggerReference);
                    EditorUtility.SetDirty(m_content);
                    EditorUtility.SetDirty(m_settings);
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(switchTrigger);
                }
            }
            private void Resize(Transform t, float scale)
            {
                t.localScale = new Vector3(scale / switchTrigger.transform.localScale.x, scale / switchTrigger.transform.localScale.y, scale / switchTrigger.transform.localScale.z);
            }

            protected override void Clear()
            {
                if (Application.isPlaying) return;

                base.Clear();

                //clear from app settings
                if(m_triggerReference != null)
                {
                    if (m_content != null && !m_content.UniqueIDExists(m_triggerReference.id))
                    {
                        m_content.SwitchSceneReferences.Remove(m_triggerReference);
                    }
                }
            }

            protected override void Initialise()
            {
                base.Initialise();

                switchTrigger = (SwitchSceneTrigger)target;

                m_hasLock = switchTrigger.gameObject.GetComponentInChildren<Lock>();
                AppConstReferences appReferences = Resources.Load<AppConstReferences>("AppConstReferences");

                if (appReferences != null)
                {
                    m_settings = appReferences.Settings;
                    m_content = appReferences.Instances;
                }
                else
                {
                    m_settings = Resources.Load<AppSettings>("ProjectAppSettings");
                    m_content = Resources.Load<AppInstances>("ProjectAppInstances");
                }

                if (Application.isPlaying) return;

                if(m_settings != null && m_content != null)
                {
                    //change to build setting instead
                    /*AppSettings.SwitchSceneTriggerID[] temp = m_settings.GetSwitchSceneReferences(switchTrigger.gameObject.scene.name).ToArray();
                    m_sceneLabels = new string[temp.Length];*/

                    List<string> temp = new List<string>();

                    /*  for(int i = 0; i < EditorBuildSettings.scenes.Length; i++)
                      {
                          string filename = System.IO.Path.GetFileName(EditorBuildSettings.scenes[i].path);
                          string extension = System.IO.Path.GetExtension(EditorBuildSettings.scenes[i].path);
                          filename = filename.Replace(extension, "");

                          if(filename.Equals(m_settings.projectSettings.loginSceneName) || filename.Equals(m_settings.projectSettings.avatarSceneName))
                          {
                              continue;
                          }

                          if(filename.Equals(switchTrigger.gameObject.scene.name))
                          {
                              continue;
                          }

                          temp.Add(filename);
                      }
                    */

                    for (int i = 0; i < m_content.SwitchSceneReferences.Count; i++)
                    {
                        if(m_content.SwitchSceneReferences[i].scene != script.gameObject.scene.name)
                        {
                            temp.Add(m_content.SwitchSceneReferences[i].scene);
                        }
                    }

                    m_sceneLabels = new string[temp.Count];

                    for (int i = 0; i < temp.Count; i++)
                    {
                        m_sceneLabels[i] = temp[i];

                        if(m_sceneLabels[i].Equals(switchTrigger.sceneName))
                        {
                            m_selectedSceneLabel = i;
                        }
                    }

                    if(m_sceneLabels.Length > 0)
                    {
                        switchTrigger.sceneName = m_sceneLabels[m_selectedSceneLabel];
                    }

                    m_triggerReference = m_content.GetSwitchSceneReference(script.ID);

                    //need to check if the current triggers have the same ID
                    if (m_triggerReference != null && m_triggerReference.scene != switchTrigger.gameObject.scene.name)
                    {
                        m_triggerReference = null;
                    }

                    if (m_triggerReference == null)
                    {
                        m_triggerReference = new AppInstances.SwitchSceneTriggerID();
                        m_triggerReference.id = script.ID;
                        m_triggerReference.scene = switchTrigger.gameObject.scene.name;
                        m_triggerReference.view = switchTrigger.cameraView.ToString();
                        string sName = switchTrigger.sceneName;

                        if (switchTrigger.loadBy.Equals(SceneLoadType.BuildIndex))
                        {
                            UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetSceneByBuildIndex(switchTrigger.buildIndex);

                            if (scene != null)
                            {
                                sName = scene.name;
                            }
                        }

                        m_triggerReference.load = sName;

                        m_content.SwitchSceneReferences.Add(m_triggerReference);
                    }

                    if (m_sceneLabels.Length > 0)
                    {
                        AppInstances.SwitchSceneTriggerID[] temp2 = m_content.GetSwitchTriggerSceneReferences(m_sceneLabels[m_selectedSceneLabel]).ToArray();
                        m_sceneLabelsSpawnPoint = new string[temp2.Length];

                        for (int i = 0; i < temp2.Length; i++)
                        {
                            m_sceneLabelsSpawnPoint[i] = temp2[i].triggerObjectName + ":" + temp2[i].id;

                            if(serializedObject.FindProperty("sceneSpawnPoint").stringValue.Equals(temp2[i].id))
                            {
                                m_selectedSceneLabelSpawnPoint = i;
                            }
                        }
                    }

                    if (m_triggerReference != null)
                    {
                        m_triggerReference.triggerObjectName = switchTrigger.gameObject.name;
                        m_content.UpdateSwitchTrigger(m_triggerReference);
                    }

                    EditorUtility.SetDirty(m_settings);
                }
            }
        }
#endif
    }
}
