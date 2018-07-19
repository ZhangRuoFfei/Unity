/*
 * this script can help auto save scenes and assets
 * click 'Edit->Preference->Auto Save' to set
 * the minimum interval is 60seconds
 */

using System.Collections;
using UnityEditor;
using UnityEngine;
using System.IO;
using UnityEditor.SceneManagement;


[InitializeOnLoad]//Running Editor Script Code on Launch
public class AutoSave
{
	public static readonly string manualSaveKey = "autosave@manualSave";//readonly indicates that assignment to the field

    static double nextTime = 0;
	///static bool isChangedHierarchy = false;

	static AutoSave ()//need static constructor
	{
		IsManualSave = true;
        	//when playmode change
		EditorApplication.playmodeStateChanged += () =>
		{
			if ( IsAutoSave && !EditorApplication.isPlaying && EditorApplication.isPlayingOrWillChangePlaymode)
            		{

				IsManualSave = false;

				if (IsSavePrefab)
					AssetDatabase.SaveAssets ();
				if (IsSaveScene)
                		{
                    			if(showMessage)
                    			{
                        			if (EditorSceneManager.SaveOpenScenes())
						{
                            				Debug.Log("Auto Save " + EditorSceneManager.loadedSceneCount.ToString() + " Scene(s) Successfully: " + EditorSceneManager.GetActiveScene().path + " on " + System.DateTime.Now);
                        			}
                        			else
                       				{
                            				Debug.Log("SAVED FAILED!!!!!!: " + EditorSceneManager.GetActiveScene().path + " on " + System.DateTime.Now);
                        			}
                    			}
                		}
				IsManualSave = true;
			}
			//isChangedHierarchy = false;
		};

        //when time is out
		nextTime = EditorApplication.timeSinceStartup + Interval;
		EditorApplication.update += () =>
		{
			if ( nextTime < EditorApplication.timeSinceStartup)
            		{
				nextTime = EditorApplication.timeSinceStartup + Interval;

				IsManualSave = false;

				if (IsAutoSave && !EditorApplication.isPlaying)
               			{
					if (IsSavePrefab)
						AssetDatabase.SaveAssets ();
					if (IsSaveScene)
                    			{
						if(showMessage)
						{
						    if(EditorSceneManager.SaveOpenScenes())
						    {
							Debug.Log("Auto Save " + EditorSceneManager.loadedSceneCount.ToString() + " Scene(s) Successfully: " + EditorSceneManager.GetActiveScene().path + " on " + System.DateTime.Now);
						    }
						    else
						    {
							Debug.Log("SAVED FAILED!!!!!!: " + EditorSceneManager.GetActiveScene().path + " on " + System.DateTime.Now);
						    }
						}
                    			}
                		}
				//isChangedHierarchy = false;
				IsManualSave = true;
			}
		};

        //when hierarchy change
		//EditorApplication.hierarchyWindowChanged += ()=>
		//{
		//	if(! EditorApplication.isPlaying)
		//		isChangedHierarchy = true;
		//};
	}

	public static bool IsManualSave
    	{
		get
        	{
			return EditorPrefs.GetBool (manualSaveKey);
		}
		private set
        	{
			EditorPrefs.SetBool (manualSaveKey, value);
		}
	}

	private static readonly string autoSave = "auto save";
	static bool IsAutoSave
    	{
		get
        	{
			string value = EditorUserSettings.GetConfigValue (autoSave);
			return!string.IsNullOrEmpty (value) && value.Equals ("True");
		}
		set
        	{
			EditorUserSettings.SetConfigValue (autoSave, value.ToString ());
		}
	}

	private static readonly string autoSavePrefab = "auto save prefab";
	static bool IsSavePrefab
    	{
		get
        	{
			string value = EditorUserSettings.GetConfigValue (autoSavePrefab);
			return!string.IsNullOrEmpty (value) && value.Equals ("True");
		}
		set
        	{
			EditorUserSettings.SetConfigValue (autoSavePrefab, value.ToString ());
		}
	}

	private static readonly string autoSaveScene = "auto save scene";
	static bool IsSaveScene
    	{
		get
        	{
			string value = EditorUserSettings.GetConfigValue (autoSaveScene);
			return!string.IsNullOrEmpty (value) && value.Equals ("True");
		}
		set
        	{
			EditorUserSettings.SetConfigValue (autoSaveScene, value.ToString ());
		}
	}

	private static readonly string autoSaveInterval = "save scene interval";
	static int Interval
    	{
		get
        	{
			string value = EditorUserSettings.GetConfigValue (autoSaveInterval);
			if (value == null)
            		{
				value = "60";
			}
			return int.Parse (value);
		}
		set
        	{
			if (value < 60)
				value = 60;
			EditorUserSettings.SetConfigValue (autoSaveInterval, value.ToString ());
		}
	}

    private static readonly string ShowMessage = "Show Message(Console Log)";
    static bool showMessage
    {
        get
        {
            string value = EditorUserSettings.GetConfigValue(ShowMessage);
            return !string.IsNullOrEmpty(value) && value.Equals("True");
        }
        set
        {
            EditorUserSettings.SetConfigValue(ShowMessage, value.ToString());
        }
    }

    [PreferenceItem("Auto Save")] 
	static void ExampleOnGUI ()
	{
		bool isAutoSave = EditorGUILayout.BeginToggleGroup ("auto save", IsAutoSave);

		IsAutoSave = isAutoSave;
		EditorGUILayout.Space ();

		IsSavePrefab = EditorGUILayout.ToggleLeft ("save prefab", IsSavePrefab);
		IsSaveScene = EditorGUILayout.ToggleLeft ("save scene", IsSaveScene);
		Interval = EditorGUILayout.IntField ("interval(sec) (min 60sec)", Interval);
		EditorGUILayout.EndToggleGroup ();

        //choose whether show console debug log
        showMessage = EditorGUILayout.BeginToggleGroup("Show Message(Console Log)", showMessage);
        EditorGUILayout.EndToggleGroup();
    }
}
