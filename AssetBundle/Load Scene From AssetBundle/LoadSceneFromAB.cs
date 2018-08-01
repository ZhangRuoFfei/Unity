using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace EditorCoroutines
{
    [CanEditMultipleObjects]
    public class LoadSceneFromAB : EditorWindow
    {
        [MenuItem("AssetBundle/load Scene From AssetBundle")]
        static void Init()
        {
            GetWindow<LoadSceneFromAB>();
        }

        [SerializeField]
        protected  List<string> AssetBundleName = new List<string>();
        protected SerializedObject _serializedObject;
        protected SerializedProperty _assetLstProperty;

        bool closeCurrentScenes = false;
        bool loadAdditive = true;
        bool loadAB = true;
        EditorCoroutines.EditorCoroutine load;
        EditorCoroutines.EditorCoroutine loadProgress;
        float loadProgressBar = 0.0f;
        bool ifLoading = false;

        protected void OnEnable()
        {
            _serializedObject = new SerializedObject(this);
            _assetLstProperty = _serializedObject.FindProperty("AssetBundleName");
        }

        private void Update()
        {
            Repaint();
        }

        private void OnGUI()
        {

            EditorGUILayout.LabelField("请填写想要加载的AssetBundle的名字（确保填写正确），并选择是否关闭当前场景");
            loadAB = EditorGUILayout.ToggleLeft("加载AssetBundle", loadAB);
            if (!loadAB)
                GUI.enabled=false;

            EditorGUILayout.LabelField("要加载的AssetBundle的名字:");
            _serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_assetLstProperty, true);
            if(EditorGUI.EndChangeCheck())
            {
                _serializedObject.ApplyModifiedProperties();
            }
            GUILayout.Space(10);
            closeCurrentScenes=EditorGUILayout.ToggleLeft("Close Current Scene", closeCurrentScenes);
            GUILayout.Space(10);
            loadAdditive = EditorGUILayout.ToggleLeft("Add Scenes additively", loadAdditive);

            if (!EditorApplication.isPlaying)
                ifLoading = false;

            if(loadAB)
                if(EditorApplication.isPlaying)
                    if(!ifLoading)
                    {
                        ifLoading = true;
                       load= this.StartCoroutine(LoadSceneFromAssetBundle());
                        loadProgress = this.StartCoroutine(ProgressBar());

                    }

            //GUILayout.Space(10);
            //if (GUILayout.Button("导入"))
            //{
            //    this.StartCoroutine( LoadSceneFromAssetBundle() );
            //}
            //GUILayout.Space(10);


        }
        IEnumerator ProgressBar()
        {
            while(loadProgressBar < 1.0f)
            {
                if (EditorUtility.DisplayCancelableProgressBar("loading...", "loading progress", loadProgressBar))
                {
                    this.StopCoroutine(LoadSceneFromAssetBundle());
                    EditorUtility.ClearProgressBar();
                    yield break;
                }
                yield return new WaitForFixedUpdate();
            }
            EditorUtility.ClearProgressBar();
        }

        IEnumerator LoadSceneFromAssetBundle()
        {
            //get assetBundle from selection
            string[] ABpaths = AssetBundleName.ToArray();
            bool firstScene = true;

            float sum = 0;
            float current_sum = 0;
            for(int j=0;j<ABpaths.Length;j++)
            {
                Debug.Log(Application.dataPath);
                ABpaths[j] = Application.dataPath + "/../../../AssetBundles/Windows/" + ABpaths[j];
                AssetBundle currentBundle = AssetBundle.LoadFromFile(ABpaths[j]);
                sum += currentBundle.GetAllScenePaths().Length;
                currentBundle.Unload(true);
            }

            for(int j=0;j<ABpaths.Length;j++)
            {
                AssetBundle currentBundle=AssetBundle.LoadFromFile(ABpaths[j]);
                //yield return currentBundle;

                if(currentBundle==null)
                {
                    Debug.Log("no such AssetBundle!");
                    //currentBundle.Unload(true);
                    yield break;
                }
                string[] scenes = currentBundle.GetAllScenePaths();

                //add scenes to building setting
                EditorBuildSettingsScene[] currentBuildScenes = EditorBuildSettings.scenes;
                EditorBuildSettingsScene[] toBuildScenes = new EditorBuildSettingsScene[scenes.Length+currentBuildScenes.Length];
                System.Array.Copy(currentBuildScenes, toBuildScenes, currentBuildScenes.Length);
                for(int i=currentBuildScenes.Length;i<toBuildScenes.Length;i++)
                {
                    toBuildScenes[i] = new EditorBuildSettingsScene(scenes[i-currentBuildScenes.Length], true);
                }
                EditorBuildSettings.scenes = toBuildScenes;

                //load scene
                foreach (string tempPath in scenes)
                {
                    current_sum += 1;
                    loadProgressBar = current_sum / sum;
                    if (loadAdditive)
                    {
                        if (!firstScene)
                        {
                            yield return this.StartCoroutine(LoadLevel(currentBundle, tempPath, true));
                        }
                    }
                    else
                    {
                        if (!firstScene)
                        {
                            yield return this.StartCoroutine(LoadLevel(currentBundle, tempPath, false));
                        }
                    }
                    if (closeCurrentScenes)
                    {
                        if(firstScene)
                        {
                            yield return this.StartCoroutine(LoadLevel(currentBundle, tempPath, false));
                            firstScene = false;
                        }
                    }
                    else
                    {
                        if (firstScene)
                        {
                            yield return this.StartCoroutine(LoadLevel(currentBundle, tempPath, true));
                            firstScene = false;
                        }
                    }

                }

                //unload build settings
                EditorBuildSettings.scenes = currentBuildScenes;
                currentBundle.Unload(false);
            }
            Debug.Log("finish!!");

        }

        private IEnumerator LoadLevel(AssetBundle assetBundleName, string levelName, bool isAdditive)
        {
            string shortLevelName = Path.GetFileNameWithoutExtension(levelName);

            Debug.Log("Start to load scene " + shortLevelName + " at frame " + Time.frameCount+" in assetBundle "+assetBundleName.name);

            // Load level from assetBundle.
            //AssetBundleLoadOperation request = AssetBundleManager.LoadLevelAsync(assetBundleName, levelName, isAdditive);
            //if (request == null)
            //    yield break;
            //yield return StartCoroutine(request);
            if (isAdditive)
            {
                try
                {
                    SceneManager.LoadSceneAsync(levelName, LoadSceneMode.Additive); //sceneName不能加后缀,只是场景名称
                }
                catch(System.Exception e)
                {
                    assetBundleName.Unload(true);
                    Caching.CleanCache();
                    ifLoading = false;
                }
                yield return null;
            }
            else
            {
                try
                {
                    SceneManager.LoadSceneAsync(levelName, LoadSceneMode.Single);
                }
                catch(System.Exception e)
                {
                    assetBundleName.Unload(true);
                    Caching.CleanCache();
                    ifLoading = false;
                }
                yield return null;
            }

            // This log will only be output when loading level additively.
            Debug.Log("Finish loading scene " + shortLevelName + " at frame " + Time.frameCount + " in assetBundle " + assetBundleName);
            yield return null;
        }
        

    }
}

