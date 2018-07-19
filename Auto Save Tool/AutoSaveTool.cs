/*
 * this script can help auto save scenes, Open it by click 'Window->AutoSave'
 * it can't work during 'PLAY' mode
 * Remember : always keep 'AutoSaveTool' window active like Inspector, Project and etc. DO NOT hide or close this window, or it will not work.  Example : you can drag it and put it below Inspector window, and you can click 'Window->Layouts->save layout' to save current layout  
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System;

public class AutoSaveTool : EditorWindow
{

    //whether auto save
    private bool autoSaveScene = true;
    //whether show debug log
    private bool showMessage = true;
    private bool isFirst = true;
    private bool isStarted = false;
    private int intervalScene = 100;
    private DateTime lastSaveTimeScene = DateTime.Now;
    //show whether save successfully last time
    private string lastSavedResult;

    //private string projectPath = Application.dataPath;
    private string scenePath;
    //the current number of loaded Scenes in the Editor
    private int LoadedSceneCount;

    [MenuItem("Window/AutoSave")]
    public static void Init()
    {
        EditorWindow window = EditorWindow.GetWindow<AutoSaveTool>();
    }
    private void OnGUI()
    {
        GUILayout.Label("Info:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Saving to: ", Application.dataPath);
        EditorGUILayout.LabelField("Saving scene (main active scene): ", scenePath);//path is like:"Assets/MyScenes/MyScene.unity".
        EditorGUILayout.LabelField("Saving number: ", LoadedSceneCount.ToString());

        GUILayout.Label("Options: ", EditorStyles.boldLabel);
        //choose whether auto save
        autoSaveScene = EditorGUILayout.BeginToggleGroup("Auto save", autoSaveScene);
        intervalScene = EditorGUILayout.IntSlider("Interval(minutes)", intervalScene, 1, 30);
        if (isStarted)
        {
            EditorGUILayout.LabelField("Last save time: ", "" + lastSaveTimeScene);
            EditorGUILayout.LabelField("Last save result:", lastSavedResult);
        }
        EditorGUILayout.EndToggleGroup();

        //choose whether show console debug log
        showMessage = EditorGUILayout.BeginToggleGroup("Show Message(Console Log)", showMessage);
        EditorGUILayout.EndToggleGroup();
    }

    private void Update()
    {
        scenePath = EditorSceneManager.GetActiveScene().path;
        LoadedSceneCount = EditorSceneManager.loadedSceneCount;

        if (autoSaveScene)
        {
            if (DateTime.Now.Minute >= (lastSaveTimeScene.Minute + intervalScene) || isFirst)
            {
                lastSaveTimeScene = DateTime.Now;
                isFirst = false;
                SaveScene();
            }
        }
        else
        {
            isStarted = false;
        }
    }

    private void SaveScene()
    {
        bool success = EditorSceneManager.SaveOpenScenes();

        if (success)
        {            
            if (showMessage)
            {
                Debug.Log("Auto Save "+ LoadedSceneCount.ToString()+" Scene(s) Successfully: " + scenePath + " on " + lastSaveTimeScene);
            }
            lastSavedResult = "Success";
        }
        else//if save open scenes fail
        {
            if (showMessage)
            {
                Debug.Log("SAVED FAILED!!!!!!: " + scenePath + " on " + lastSaveTimeScene);
            }
            lastSavedResult = "FAILED!!! you have better save manually";

        }
        isStarted = true;
        Repaint();
    }
}
