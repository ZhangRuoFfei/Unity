using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class TestScene : EditorWindow
{
    bool autoScanScene = false;

    //test scene tool中需要的参数
    float stepx = 50;
    float stepy = 50;
    Vector2 scanSize = new Vector2(800, 800);
    Vector3 startPos = new Vector3(0.0f, 30.0f, 0.0f);
    int internalFrame = 10;

    //判断场景是否更改
    static string currentSceneName = null;
    string message = null;
    bool firstStart = true;
    bool firstEnd = true;

    Vector2 scrollPos;

    GameObject originalTestToolPrefab;
    GameObject TestTool;
    GameObject TestToolInScene;
    static int index = 0;
    static string[] ids = null;
    static string prefabName = "TestTool";


    //设置不合格的判断指标
    int triangles = 300000;
    int vertices = 300000;
    int batches = 250;
    int drawcalls = 250;
    int dynamicBatchedDrawCalls = 5;
    int staticBatchedDrawCalls = 5;
    int instancedBatchedDrawCalls = 5;
    int LASTtriangles = -1;
    int LASTvertices = -1;
    int LASTbatches = -1;
    int LASTdrawcalls = -1;
    int LASTdynamicBatchedDrawCalls = -1;
    int LASTstaticBatchedDrawCalls = -1;
    int LASTinstancedBatchedDrawCalls = -1;
    Vector3 LASTpos = new Vector3();

    bool IFall = false;
    bool IFtriangles = false;
    bool IFvertices = false;
    bool IFbatches = false;
    bool IFdrawcalls = false;
    bool IFdynamicBatchedDrawCalls = false;
    bool IFstaticBatchedDrawCalls = false;
    bool IFinstancedBatchedDrawCalls = false;

    //指标不合格的位置显示
    private Dictionary<Vector3, string> XbadPositions = new Dictionary<Vector3, string>();
    private Dictionary<Vector3, string> YbadPositions = new Dictionary<Vector3, string>();
    private Dictionary<Vector3, string> _XbadPositions = new Dictionary<Vector3, string>();
    private Dictionary<Vector3, string> _YbadPositions = new Dictionary<Vector3, string>();


    public static GUILayoutOption GL_EXPAND_WIDTH = GUILayout.ExpandWidth(true);
    public static GUILayoutOption GL_EXPAND_HEIGHT = GUILayout.ExpandHeight(true);
    public static GUILayoutOption GL_WIDTH_25 = GUILayout.Width(25);
    public static GUILayoutOption GL_WIDTH_100 = GUILayout.Width(100);
    public static GUILayoutOption GL_WIDTH_250 = GUILayout.Width(250);
    public static GUILayoutOption GL_HEIGHT_30 = GUILayout.Height(30);
    public static GUILayoutOption GL_HEIGHT_35 = GUILayout.Height(35);
    public static GUILayoutOption GL_HEIGHT_40 = GUILayout.Height(40);
    // GUIStyle used to draw the results of the search
    private static GUIStyle m_boxGUIStyle;
    public static GUIStyle BoxGUIStyle
    {
        get
        {
            if (m_boxGUIStyle == null)
            {
                m_boxGUIStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    alignment = TextAnchor.MiddleCenter,
                    font = EditorStyles.label.font
                };
            }

            return m_boxGUIStyle;
        }
    }

    [MenuItem("Tools/Scene Info")]
    static void Init()
    {
        // Get existing open window or if none, make a new one:
        TestScene window = (TestScene)EditorWindow.GetWindow(typeof(TestScene));
        window.titleContent.text = "Scene Info";

        currentSceneName = SceneManager.GetActiveScene().name;
        //FindPrefab();
    }

    void FindPrefab()
    {
        //to find prefab
        ids = AssetDatabase.FindAssets("t:Prefab", new string[] { "Assets" });

        for (int i = 0; i < ids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(ids[i]);

            if (!path.Contains(prefabName))
            {
                continue;
            }
            //Debug.Log(path);
            index = i;
            originalTestToolPrefab = AssetDatabase.LoadAssetAtPath(path, typeof(GameObject)) as GameObject;
            TestTool = PrefabUtility.InstantiatePrefab(originalTestToolPrefab) as GameObject;
        }
    }


    private void Update()
    {
        //如果物体为空，则啥都不干
        if (TestTool == null)
        {
            message = "当前没有拖入相机prefab！\n";
            Repaint();
            return;
        }

        if(autoScanScene)
        {
            TestTool.GetComponent<TestSceneTool>().autoScan = true;
            if(TestToolInScene!=null)
                TestToolInScene.GetComponent<TestSceneTool>().autoScan = true;

        }
        else
        {
            TestTool.GetComponent<TestSceneTool>().autoScan = false;
            if (TestToolInScene != null)
                TestToolInScene.GetComponent<TestSceneTool>().autoScan = false;
        }

        //when first playing
        if ((EditorApplication.isPlaying) && firstStart)
        {
            //把prefab生成gameObejct
            if(TestToolInScene==null)
                TestToolInScene= PrefabUtility.InstantiatePrefab(TestTool) as GameObject;

            Reset();
            firstStart = false;
        }
        else if (EditorApplication.isPlaying && !EditorApplication.isPaused)
        {
            JudgeBadPos();
        }
        else if (!EditorApplication.isPlaying && !EditorApplication.isPaused)
        {
            firstStart = true;
            //这里把他删了！！！
            if (firstEnd && TestTool != null)
            {
                firstEnd = false;
                //GameObject.DestroyImmediate(TestTool);
                //TestTool = null;
            }
        }
        Repaint();
    }

    private void Reset()
    {
        LASTpos = new Vector3();
        LASTtriangles = -1;
        LASTvertices = -1;
        LASTbatches = -1;
        LASTdrawcalls = -1;
        LASTdynamicBatchedDrawCalls = -1;
        LASTstaticBatchedDrawCalls = -1;
        LASTinstancedBatchedDrawCalls = -1;

        firstEnd = true;
        XbadPositions.Clear();
        YbadPositions.Clear();
        _XbadPositions.Clear();
        _YbadPositions.Clear();
    }

    private void DeletePrefab()
    {
        ////获取场景中的所有游戏对象  
        //GameObject[] gos = (GameObject[])FindObjectsOfType(typeof(GameObject));
        //foreach (GameObject go in gos)
        //{
        //    //判断GameObject是否为一个Prefab的引用  
        //    if (PrefabUtility.GetPrefabType(go) == PrefabType.PrefabInstance)
        //    {
        //        UnityEngine.Object parentObject = PrefabUtility.GetPrefabParent(go);
        //        string path = AssetDatabase.GetAssetPath(parentObject);
        //        //判断GameObject的Prefab是否和右键选择的Prefab是同一路径。  
        //        if (path == AssetDatabase.GUIDToAssetPath(ids[index]))
        //        {
        //            //输出场景名，以及Prefab引用的路径  
        //            Debug.Log(go.name+"是一个TestTool,已在"+EditorSceneManager.GetActiveScene().name+"删除");
        //            GameObject.DestroyImmediate(go);
        //        }
        //    }
        //}
        //输出场景名，以及Prefab引用的路径  

        if (TestToolInScene != null)
        {
            Debug.Log("删除场景中的测试相机");
            GameObject.DestroyImmediate(TestToolInScene);
            TestToolInScene = null;
        }
    }

    private void OnDestroy()
    {
        //关闭场景中的testTool
        DeletePrefab();
    }

    void JudgeBadPos()
    {
        if (TestTool == null)
        {
            Debug.Log("error!!!");
            return;
        }
        if (!IFall) return;

        if (IFtriangles && (UnityEditor.UnityStats.triangles > triangles))
        {
            if (LASTpos != new Vector3(-1, -1, -1) && LASTtriangles == UnityEditor.UnityStats.triangles)
            {
                return;
            }
            LASTtriangles = UnityEditor.UnityStats.triangles;
            //LASTvertices = UnityEditor.UnityStats.vertices;
            //LASTbatches = UnityEditor.UnityStats.batches;
            //LASTdrawcalls = UnityEditor.UnityStats.drawCalls;
            //LASTdynamicBatchedDrawCalls = UnityEditor.UnityStats.dynamicBatchedDrawCalls;
            //LASTstaticBatchedDrawCalls = UnityEditor.UnityStats.staticBatchedDrawCalls;
            //LASTinstancedBatchedDrawCalls = UnityEditor.UnityStats.instancedBatchedDrawCalls;
            LASTpos = TestToolInScene.transform.position;


            if (TestToolInScene.GetComponent<TestSceneTool>().currentDirection == TestSceneTool.direction.x && !XbadPositions.ContainsKey(TestToolInScene.transform.position))
                XbadPositions.Add(TestToolInScene.transform.position, "triangles\n");
            else if (TestToolInScene.GetComponent<TestSceneTool>().currentDirection == TestSceneTool.direction.y && !YbadPositions.ContainsKey(TestToolInScene.transform.position))
                YbadPositions.Add(TestToolInScene.transform.position, "triangles\n");
            else if (TestToolInScene.GetComponent<TestSceneTool>().currentDirection == TestSceneTool.direction._x && !_XbadPositions.ContainsKey(TestToolInScene.transform.position))
                _XbadPositions.Add(TestToolInScene.transform.position, "triangles\n");
            else if (TestToolInScene.GetComponent<TestSceneTool>().currentDirection == TestSceneTool.direction._y && !_YbadPositions.ContainsKey(TestToolInScene.transform.position))
                _YbadPositions.Add(TestToolInScene.transform.position, "triangles\n");

            else if (TestToolInScene.GetComponent<TestSceneTool>().currentDirection == TestSceneTool.direction.x && XbadPositions.ContainsKey(TestToolInScene.transform.position))
            {
                if (!XbadPositions[TestToolInScene.transform.position].Contains("triangles"))
                    XbadPositions[TestToolInScene.transform.position] += "triangles\n";
            }
            else if (TestToolInScene.GetComponent<TestSceneTool>().currentDirection == TestSceneTool.direction.y && YbadPositions.ContainsKey(TestToolInScene.transform.position))
            {
                if (!YbadPositions[TestToolInScene.transform.position].Contains("triangles"))
                    YbadPositions[TestToolInScene.transform.position] += "triangles\n";
            }
            else if (TestToolInScene.GetComponent<TestSceneTool>().currentDirection == TestSceneTool.direction._x && _XbadPositions.ContainsKey(TestToolInScene.transform.position))
            {
                if (!_XbadPositions[TestToolInScene.transform.position].Contains("triangles"))
                    _XbadPositions[TestToolInScene.transform.position] += "triangles\n";
            }
            else if (TestToolInScene.GetComponent<TestSceneTool>().currentDirection == TestSceneTool.direction._y && _YbadPositions.ContainsKey(TestToolInScene.transform.position))
            {
                if (!_YbadPositions[TestToolInScene.transform.position].Contains("triangles"))
                    _YbadPositions[TestToolInScene.transform.position] += "triangles\n";
            }

        }
        if (IFvertices && (UnityEditor.UnityStats.vertices > vertices))
        {
            if (LASTpos != new Vector3(-1,-1,-1) && LASTvertices == UnityEditor.UnityStats.vertices)
            {
                return;
            }
            //LASTtriangles = UnityEditor.UnityStats.triangles;
            LASTvertices = UnityEditor.UnityStats.vertices;
            //LASTbatches = UnityEditor.UnityStats.batches;
            //LASTdrawcalls = UnityEditor.UnityStats.drawCalls;
            //LASTdynamicBatchedDrawCalls = UnityEditor.UnityStats.dynamicBatchedDrawCalls;
            //LASTstaticBatchedDrawCalls = UnityEditor.UnityStats.staticBatchedDrawCalls;
            //LASTinstancedBatchedDrawCalls = UnityEditor.UnityStats.instancedBatchedDrawCalls;
            LASTpos = TestToolInScene.transform.position;

            if (TestToolInScene.GetComponent<TestSceneTool>().currentDirection == TestSceneTool.direction.x && !XbadPositions.ContainsKey(TestToolInScene.transform.position))
                XbadPositions.Add(TestToolInScene.transform.position, "vertices\n");
            else if (TestToolInScene.GetComponent<TestSceneTool>().currentDirection == TestSceneTool.direction.y && !YbadPositions.ContainsKey(TestToolInScene.transform.position))
                YbadPositions.Add(TestToolInScene.transform.position, "vertices\n");
            else if (TestToolInScene.GetComponent<TestSceneTool>().currentDirection == TestSceneTool.direction._x && !_XbadPositions.ContainsKey(TestToolInScene.transform.position))
                _XbadPositions.Add(TestToolInScene.transform.position, "vertices\n");
            else if (TestToolInScene.GetComponent<TestSceneTool>().currentDirection == TestSceneTool.direction._y && !_YbadPositions.ContainsKey(TestToolInScene.transform.position))
                _YbadPositions.Add(TestToolInScene.transform.position, "vertices\n");

            else if (TestToolInScene.GetComponent<TestSceneTool>().currentDirection == TestSceneTool.direction.x && XbadPositions.ContainsKey(TestToolInScene.transform.position))
            {
                if (!XbadPositions[TestToolInScene.transform.position].Contains("vertices"))
                    XbadPositions[TestToolInScene.transform.position] += "vertices\n";
            }
            else if (TestToolInScene.GetComponent<TestSceneTool>().currentDirection == TestSceneTool.direction.y && YbadPositions.ContainsKey(TestToolInScene.transform.position))
            {
                if (!YbadPositions[TestToolInScene.transform.position].Contains("vertices"))
                    YbadPositions[TestToolInScene.transform.position] += "vertices\n";
            }
            else if (TestToolInScene.GetComponent<TestSceneTool>().currentDirection == TestSceneTool.direction._x && _XbadPositions.ContainsKey(TestToolInScene.transform.position))
            {
                if (!_XbadPositions[TestToolInScene.transform.position].Contains("vertices"))
                    _XbadPositions[TestToolInScene.transform.position] += "vertices\n";
            }
            else if (TestToolInScene.GetComponent<TestSceneTool>().currentDirection == TestSceneTool.direction._y && _YbadPositions.ContainsKey(TestToolInScene.transform.position))
            {
                if (!_YbadPositions[TestToolInScene.transform.position].Contains("vertices"))
                    _YbadPositions[TestToolInScene.transform.position] += "vertices\n";
            }
        }
        if (IFbatches && (UnityEditor.UnityStats.batches > batches))
        {
            if (LASTpos != new Vector3(-1,-1,-1) && LASTbatches == UnityEditor.UnityStats.batches)
            {
                return;
            }
            //LASTtriangles = UnityEditor.UnityStats.triangles;
            //LASTvertices = UnityEditor.UnityStats.vertices;
            LASTbatches = UnityEditor.UnityStats.batches;
            //LASTdrawcalls = UnityEditor.UnityStats.drawCalls;
            //LASTdynamicBatchedDrawCalls = UnityEditor.UnityStats.dynamicBatchedDrawCalls;
            //LASTstaticBatchedDrawCalls = UnityEditor.UnityStats.staticBatchedDrawCalls;
            //LASTinstancedBatchedDrawCalls = UnityEditor.UnityStats.instancedBatchedDrawCalls;
            LASTpos = TestToolInScene.transform.position;

            if (TestToolInScene.GetComponent<TestSceneTool>().currentDirection == TestSceneTool.direction.x && !XbadPositions.ContainsKey(TestToolInScene.transform.position))
                XbadPositions.Add(TestToolInScene.transform.position, "batches\n");
            else if (TestToolInScene.GetComponent<TestSceneTool>().currentDirection == TestSceneTool.direction.y && !YbadPositions.ContainsKey(TestToolInScene.transform.position))
                YbadPositions.Add(TestToolInScene.transform.position, "batches\n");
            else if (TestToolInScene.GetComponent<TestSceneTool>().currentDirection == TestSceneTool.direction._x && !_XbadPositions.ContainsKey(TestToolInScene.transform.position))
                _XbadPositions.Add(TestToolInScene.transform.position, "batches\n");
            else if (TestToolInScene.GetComponent<TestSceneTool>().currentDirection == TestSceneTool.direction._y && !_YbadPositions.ContainsKey(TestToolInScene.transform.position))
                _YbadPositions.Add(TestToolInScene.transform.position, "batches\n");

            else if (TestToolInScene.GetComponent<TestSceneTool>().currentDirection == TestSceneTool.direction.x && XbadPositions.ContainsKey(TestToolInScene.transform.position))
            {
                if (!XbadPositions[TestToolInScene.transform.position].Contains("batches"))
                    XbadPositions[TestToolInScene.transform.position] += "batches\n";
            }
            else if (TestToolInScene.GetComponent<TestSceneTool>().currentDirection == TestSceneTool.direction.y && YbadPositions.ContainsKey(TestToolInScene.transform.position))
            {
                if (!YbadPositions[TestToolInScene.transform.position].Contains("batches"))

                    YbadPositions[TestToolInScene.transform.position] += "batches\n";
            }
            else if (TestToolInScene.GetComponent<TestSceneTool>().currentDirection == TestSceneTool.direction._x && _XbadPositions.ContainsKey(TestToolInScene.transform.position))
            {
                if (!_XbadPositions[TestToolInScene.transform.position].Contains("batches"))

                    _XbadPositions[TestToolInScene.transform.position] += "batches\n";
            }
            else if (TestToolInScene.GetComponent<TestSceneTool>().currentDirection == TestSceneTool.direction._y && _YbadPositions.ContainsKey(TestToolInScene.transform.position))
            {
                if (!_YbadPositions[TestToolInScene.transform.position].Contains("batches"))

                    _YbadPositions[TestToolInScene.transform.position] += "batches\n";
            }
        }
        if (IFdrawcalls && (UnityEditor.UnityStats.drawCalls > drawcalls))
        {

            if (LASTpos != new Vector3(-1,-1,-1) && LASTdrawcalls == UnityEditor.UnityStats.drawCalls)
            {
                return;
            }
            //LASTtriangles = UnityEditor.UnityStats.triangles;
            //LASTvertices = UnityEditor.UnityStats.vertices;
            //LASTbatches = UnityEditor.UnityStats.batches;
            LASTdrawcalls = UnityEditor.UnityStats.drawCalls;
            //LASTdynamicBatchedDrawCalls = UnityEditor.UnityStats.dynamicBatchedDrawCalls;
            //LASTstaticBatchedDrawCalls = UnityEditor.UnityStats.staticBatchedDrawCalls;
            //LASTinstancedBatchedDrawCalls = UnityEditor.UnityStats.instancedBatchedDrawCalls;
            LASTpos = TestToolInScene.transform.position;

            if ((TestToolInScene.GetComponent<TestSceneTool>().currentDirection == TestSceneTool.direction.x) && !XbadPositions.ContainsKey(TestToolInScene.transform.position))
                XbadPositions.Add(TestToolInScene.transform.position, "drawcalls\n");
            else if (TestToolInScene.GetComponent<TestSceneTool>().currentDirection == TestSceneTool.direction.y && !YbadPositions.ContainsKey(TestToolInScene.transform.position))
                YbadPositions.Add(TestToolInScene.transform.position, "drawcalls\n");
            else if (TestToolInScene.GetComponent<TestSceneTool>().currentDirection == TestSceneTool.direction._x && !_XbadPositions.ContainsKey(TestToolInScene.transform.position))
                _XbadPositions.Add(TestToolInScene.transform.position, "drawcalls\n");
            else if (TestToolInScene.GetComponent<TestSceneTool>().currentDirection == TestSceneTool.direction._y && !_YbadPositions.ContainsKey(TestToolInScene.transform.position))
                _YbadPositions.Add(TestToolInScene.transform.position, "drawcalls\n");

            else if (TestToolInScene.GetComponent<TestSceneTool>().currentDirection == TestSceneTool.direction.x && XbadPositions.ContainsKey(TestToolInScene.transform.position))
            {
                if (!XbadPositions[TestToolInScene.transform.position].Contains("drawcalls"))
                    XbadPositions[TestToolInScene.transform.position] += "drawcalls\n";
            }
            else if (TestToolInScene.GetComponent<TestSceneTool>().currentDirection == TestSceneTool.direction.y && YbadPositions.ContainsKey(TestToolInScene.transform.position))
            {
                if (!YbadPositions[TestToolInScene.transform.position].Contains("drawcalls"))

                    YbadPositions[TestToolInScene.transform.position] += "drawcalls\n";
            }
            else if (TestToolInScene.GetComponent<TestSceneTool>().currentDirection == TestSceneTool.direction._x && _XbadPositions.ContainsKey(TestToolInScene.transform.position))
            {
                if (!_XbadPositions[TestToolInScene.transform.position].Contains("drawcalls"))

                    _XbadPositions[TestToolInScene.transform.position] += "drawcalls\n";
            }
            else if (TestToolInScene.GetComponent<TestSceneTool>().currentDirection == TestSceneTool.direction._y && _YbadPositions.ContainsKey(TestToolInScene.transform.position))
            {
                if (!_YbadPositions[TestToolInScene.transform.position].Contains("drawcalls"))

                    _YbadPositions[TestToolInScene.transform.position] += "drawcalls\n";
            }
        }
        if (IFdynamicBatchedDrawCalls && (UnityEditor.UnityStats.dynamicBatchedDrawCalls > dynamicBatchedDrawCalls))
        {
            if (LASTpos != new Vector3(-1,-1,-1) && LASTdynamicBatchedDrawCalls == UnityEditor.UnityStats.dynamicBatchedDrawCalls)
            {
                return;
            }
            //LASTtriangles = UnityEditor.UnityStats.triangles;
            //LASTvertices = UnityEditor.UnityStats.vertices;
            //LASTbatches = UnityEditor.UnityStats.batches;
            //LASTdrawcalls = UnityEditor.UnityStats.drawCalls;
            LASTdynamicBatchedDrawCalls = UnityEditor.UnityStats.dynamicBatchedDrawCalls;
            //LASTstaticBatchedDrawCalls = UnityEditor.UnityStats.staticBatchedDrawCalls;
            //LASTinstancedBatchedDrawCalls = UnityEditor.UnityStats.instancedBatchedDrawCalls;
            LASTpos = TestToolInScene.transform.position;

            if (TestToolInScene.GetComponent<TestSceneTool>().currentDirection == TestSceneTool.direction.x && !XbadPositions.ContainsKey(TestToolInScene.transform.position))
                XbadPositions.Add(TestToolInScene.transform.position, "dynamicBatchedDrawCalls\n");
            else if (TestToolInScene.GetComponent<TestSceneTool>().currentDirection == TestSceneTool.direction.y && !YbadPositions.ContainsKey(TestToolInScene.transform.position))
                YbadPositions.Add(TestToolInScene.transform.position, "dynamicBatchedDrawCalls\n");
            else if (TestToolInScene.GetComponent<TestSceneTool>().currentDirection == TestSceneTool.direction._x && !_XbadPositions.ContainsKey(TestToolInScene.transform.position))
                _XbadPositions.Add(TestToolInScene.transform.position, "dynamicBatchedDrawCalls\n");
            else if (TestToolInScene.GetComponent<TestSceneTool>().currentDirection == TestSceneTool.direction._y && !_YbadPositions.ContainsKey(TestToolInScene.transform.position))
                _YbadPositions.Add(TestToolInScene.transform.position, "dynamicBatchedDrawCalls\n");

            else if (TestToolInScene.GetComponent<TestSceneTool>().currentDirection == TestSceneTool.direction.x && XbadPositions.ContainsKey(TestToolInScene.transform.position))
            {
                if (!XbadPositions[TestToolInScene.transform.position].Contains("dynamicBatchedDrawCalls"))
                    XbadPositions[TestToolInScene.transform.position] += "dynamicBatchedDrawCalls\n";
            }
            else if (TestToolInScene.GetComponent<TestSceneTool>().currentDirection == TestSceneTool.direction.y && YbadPositions.ContainsKey(TestToolInScene.transform.position))
            {
                if (!YbadPositions[TestToolInScene.transform.position].Contains("dynamicBatchedDrawCalls"))
                    YbadPositions[TestToolInScene.transform.position] += "dynamicBatchedDrawCalls\n";
            }
            else if (TestToolInScene.GetComponent<TestSceneTool>().currentDirection == TestSceneTool.direction._x && _XbadPositions.ContainsKey(TestToolInScene.transform.position))
            {
                if (!_XbadPositions[TestToolInScene.transform.position].Contains("dynamicBatchedDrawCalls"))
                    _XbadPositions[TestToolInScene.transform.position] += "dynamicBatchedDrawCalls\n";
            }
            else if (TestToolInScene.GetComponent<TestSceneTool>().currentDirection == TestSceneTool.direction._y && _YbadPositions.ContainsKey(TestToolInScene.transform.position))
            {
                if (!_YbadPositions[TestToolInScene.transform.position].Contains("dynamicBatchedDrawCalls"))
                    _YbadPositions[TestToolInScene.transform.position] += "dynamicBatchedDrawCalls\n";
            }
        }
        if (IFstaticBatchedDrawCalls && (UnityEditor.UnityStats.staticBatchedDrawCalls > staticBatchedDrawCalls))
        {
            if (LASTpos != new Vector3(-1,-1,-1) && LASTstaticBatchedDrawCalls == UnityEditor.UnityStats.staticBatchedDrawCalls)
            {
                return;
            }
            //LASTtriangles = UnityEditor.UnityStats.triangles;
            //LASTvertices = UnityEditor.UnityStats.vertices;
            //LASTbatches = UnityEditor.UnityStats.batches;
            //LASTdrawcalls = UnityEditor.UnityStats.drawCalls;
            //LASTdynamicBatchedDrawCalls = UnityEditor.UnityStats.dynamicBatchedDrawCalls;
            LASTstaticBatchedDrawCalls = UnityEditor.UnityStats.staticBatchedDrawCalls;
            //LASTinstancedBatchedDrawCalls = UnityEditor.UnityStats.instancedBatchedDrawCalls;
            //LASTpos = TestToolInScene.transform.position;

            if (TestToolInScene.GetComponent<TestSceneTool>().currentDirection == TestSceneTool.direction.x && !XbadPositions.ContainsKey(TestToolInScene.transform.position))
                XbadPositions.Add(TestToolInScene.transform.position, "staticBatchedDrawCalls\n");
            else if (TestToolInScene.GetComponent<TestSceneTool>().currentDirection == TestSceneTool.direction.y && !YbadPositions.ContainsKey(TestToolInScene.transform.position))
                YbadPositions.Add(TestToolInScene.transform.position, "staticBatchedDrawCalls\n");
            else if (TestToolInScene.GetComponent<TestSceneTool>().currentDirection == TestSceneTool.direction._x && !_XbadPositions.ContainsKey(TestToolInScene.transform.position))
                _XbadPositions.Add(TestToolInScene.transform.position, "staticBatchedDrawCalls\n");
            else if (TestToolInScene.GetComponent<TestSceneTool>().currentDirection == TestSceneTool.direction._y && !_YbadPositions.ContainsKey(TestToolInScene.transform.position))
                _YbadPositions.Add(TestToolInScene.transform.position, "staticBatchedDrawCalls\n");

            else if (TestToolInScene.GetComponent<TestSceneTool>().currentDirection == TestSceneTool.direction.x && XbadPositions.ContainsKey(TestToolInScene.transform.position))
            {
                if (!XbadPositions[TestToolInScene.transform.position].Contains("staticBatchedDrawCalls"))
                    XbadPositions[TestToolInScene.transform.position] += "staticBatchedDrawCalls\n";
            }
            else if (TestToolInScene.GetComponent<TestSceneTool>().currentDirection == TestSceneTool.direction.y && YbadPositions.ContainsKey(TestToolInScene.transform.position))
            {
                if (!YbadPositions[TestToolInScene.transform.position].Contains("staticBatchedDrawCalls"))
                    YbadPositions[TestToolInScene.transform.position] += "staticBatchedDrawCalls\n";
            }
            else if (TestToolInScene.GetComponent<TestSceneTool>().currentDirection == TestSceneTool.direction._x && _XbadPositions.ContainsKey(TestToolInScene.transform.position))
            {
                if (!_XbadPositions[TestToolInScene.transform.position].Contains("staticBatchedDrawCalls"))
                    _XbadPositions[TestToolInScene.transform.position] += "staticBatchedDrawCalls\n";
            }
            else if (TestToolInScene.GetComponent<TestSceneTool>().currentDirection == TestSceneTool.direction._y && _YbadPositions.ContainsKey(TestToolInScene.transform.position))
            {
                if (!_YbadPositions[TestToolInScene.transform.position].Contains("staticBatchedDrawCalls"))
                    _YbadPositions[TestToolInScene.transform.position] += "staticBatchedDrawCalls\n";
            }
        }
        if (IFinstancedBatchedDrawCalls && (UnityEditor.UnityStats.instancedBatchedDrawCalls > instancedBatchedDrawCalls))
        {
            if (LASTpos != new Vector3(-1,-1,-1) && LASTinstancedBatchedDrawCalls == UnityEditor.UnityStats.instancedBatchedDrawCalls)
            {
                return;
            }
            //LASTtriangles = UnityEditor.UnityStats.triangles;
            //LASTvertices = UnityEditor.UnityStats.vertices;
            //LASTbatches = UnityEditor.UnityStats.batches;
            //LASTdrawcalls = UnityEditor.UnityStats.drawCalls;
            //LASTdynamicBatchedDrawCalls = UnityEditor.UnityStats.dynamicBatchedDrawCalls;
            //LASTstaticBatchedDrawCalls = UnityEditor.UnityStats.staticBatchedDrawCalls;
            LASTinstancedBatchedDrawCalls = UnityEditor.UnityStats.instancedBatchedDrawCalls;
            LASTpos = TestToolInScene.transform.position;

            if (TestToolInScene.GetComponent<TestSceneTool>().currentDirection == TestSceneTool.direction.x && !XbadPositions.ContainsKey(TestToolInScene.transform.position))
                XbadPositions.Add(TestToolInScene.transform.position, "instancedBatchedDrawCalls\n");
            else if (TestToolInScene.GetComponent<TestSceneTool>().currentDirection == TestSceneTool.direction.y && !YbadPositions.ContainsKey(TestToolInScene.transform.position))
                YbadPositions.Add(TestToolInScene.transform.position, "instancedBatchedDrawCalls\n");
            else if (TestToolInScene.GetComponent<TestSceneTool>().currentDirection == TestSceneTool.direction._x && !_XbadPositions.ContainsKey(TestToolInScene.transform.position))
                _XbadPositions.Add(TestToolInScene.transform.position, "instancedBatchedDrawCalls\n");
            else if (TestToolInScene.GetComponent<TestSceneTool>().currentDirection == TestSceneTool.direction._y && !_YbadPositions.ContainsKey(TestToolInScene.transform.position))
                _YbadPositions.Add(TestToolInScene.transform.position, "instancedBatchedDrawCalls\n");


            else if (TestToolInScene.GetComponent<TestSceneTool>().currentDirection == TestSceneTool.direction.x && XbadPositions.ContainsKey(TestToolInScene.transform.position))
            {
                if (!XbadPositions[TestToolInScene.transform.position].Contains("instancedBatchedDrawCalls"))
                    XbadPositions[TestToolInScene.transform.position] += "instancedBatchedDrawCalls\n";
            }
            else if (TestToolInScene.GetComponent<TestSceneTool>().currentDirection == TestSceneTool.direction.y && YbadPositions.ContainsKey(TestToolInScene.transform.position))
            {
                if (!YbadPositions[TestToolInScene.transform.position].Contains("instancedBatchedDrawCalls"))
                    YbadPositions[TestToolInScene.transform.position] += "instancedBatchedDrawCalls\n";
            }
            else if (TestToolInScene.GetComponent<TestSceneTool>().currentDirection == TestSceneTool.direction._x && _XbadPositions.ContainsKey(TestToolInScene.transform.position))
            {
                if (!_XbadPositions[TestToolInScene.transform.position].Contains("instancedBatchedDrawCalls"))
                    _XbadPositions[TestToolInScene.transform.position] += "instancedBatchedDrawCalls\n";
            }
            else if (TestToolInScene.GetComponent<TestSceneTool>().currentDirection == TestSceneTool.direction._y && _YbadPositions.ContainsKey(TestToolInScene.transform.position))
            {
                if (!_YbadPositions[TestToolInScene.transform.position].Contains("instancedBatchedDrawCalls"))
                    _YbadPositions[TestToolInScene.transform.position] += "instancedBatchedDrawCalls\n";
            }
        }
        return;

    }

    private void ShowPosition()
    {
        scrollPos = GUILayout.BeginScrollView(scrollPos);

        EditorGUILayout.BeginHorizontal();
        //////////////////////////////////
        EditorGUILayout.BeginVertical();
        Color c = GUI.color;
        GUI.color = Color.cyan;
        foreach (KeyValuePair<Vector3, string> kvp in XbadPositions)
        {
            string title = "ROTATE 0" + "\n" + "(" + kvp.Key.x.ToString() + "," + kvp.Key.y.ToString() + "," + kvp.Key.z.ToString() + ")" + "\n" + kvp.Value;
            if (GUILayout.Button(title, BoxGUIStyle, GL_WIDTH_100, GUILayout.ExpandHeight(true)))
            {
                if (TestToolInScene == null)
                {
                    if(TestTool==null)
                    {
                        message = "当前没有拖入相机prefab！\n";
                        return;
                    }
                    TestToolInScene = PrefabUtility.InstantiatePrefab(TestTool) as GameObject;
                }
                TestToolInScene.transform.position = kvp.Key;
                TestToolInScene.transform.eulerAngles = new Vector3(0, 0, 0);

            }
        }
        EditorGUILayout.EndVertical();
        //////////////////////////////////
        EditorGUILayout.BeginVertical();
        GUI.color = Color.yellow;
        foreach (KeyValuePair<Vector3, string> kvp in YbadPositions)
        {
            string title = "ROTATE 90" + "\n" + "(" + kvp.Key.x.ToString() + "," + kvp.Key.y.ToString() + "," + kvp.Key.z.ToString() + ")" + "\n" + kvp.Value;
            if (GUILayout.Button(title, BoxGUIStyle, GL_WIDTH_100, GUILayout.ExpandHeight(true)))
            {
                if (TestToolInScene == null)
                {
                    if (TestTool == null)
                    {
                        message = "当前没有拖入相机prefab！\n";
                        return;
                    }
                    TestToolInScene = PrefabUtility.InstantiatePrefab(TestTool) as GameObject;
                }
                TestToolInScene.transform.position = kvp.Key;
                //旋转到世界坐标系下的逆时针90度
                TestToolInScene.transform.eulerAngles = new Vector3(0, 90, 0);

            }
        }

        EditorGUILayout.EndVertical();
        //////////////////////////////////
        EditorGUILayout.BeginVertical();
        GUI.color = Color.white;
        foreach (KeyValuePair<Vector3, string> kvp in _XbadPositions)
        {
            string title = "ROTATE 180" + "\n" + "(" + kvp.Key.x.ToString() + "," + kvp.Key.y.ToString() + "," + kvp.Key.z.ToString() + ")" + "\n" + kvp.Value;
            if (GUILayout.Button(title, BoxGUIStyle, GL_WIDTH_100, GUILayout.ExpandHeight(true)))
            {
                if (TestToolInScene == null)
                {
                    if (TestTool == null)
                    {
                        message = "当前没有拖入相机prefab！\n";
                        return;
                    }
                    TestToolInScene = PrefabUtility.InstantiatePrefab(TestTool) as GameObject;
                }
                TestToolInScene.transform.position = kvp.Key;
                //旋转到世界坐标系下的逆时针90度
                TestToolInScene.transform.eulerAngles = new Vector3(0, 180, 0);

            }
        }
        EditorGUILayout.EndVertical();
        //////////////////////////////////
        EditorGUILayout.BeginVertical();
        GUI.color = Color.grey;
        foreach (KeyValuePair<Vector3, string> kvp in _YbadPositions)
        {
            string title = "ROTATE 270" + "\n" + "(" + kvp.Key.x.ToString() + "," + kvp.Key.y.ToString() + "," + kvp.Key.z.ToString() + ")" + "\n" + kvp.Value;
            if (GUILayout.Button(title, BoxGUIStyle, GL_WIDTH_100, GUILayout.ExpandHeight(true)))
            {
                if (TestToolInScene == null)
                {
                    if (TestTool == null)
                    {
                        message = "当前没有拖入相机prefab！\n";
                        return;
                    }
                    TestToolInScene = PrefabUtility.InstantiatePrefab(TestTool) as GameObject;
                }
                TestToolInScene.transform.position = kvp.Key;
                //旋转到世界坐标系下的逆时针90度
                TestToolInScene.transform.eulerAngles = new Vector3(0, 270, 0);

            }
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
        GUILayout.EndScrollView();

        
    }



    void OnGUI()
    {
        //GUILayout.Label(message);
        autoScanScene = EditorGUILayout.ToggleLeft("AUTO SCAN SCENE", autoScanScene);
        GUILayout.Space(10);
        //相机
        TestTool = (GameObject)EditorGUILayout.ObjectField("Test Tool:", TestTool, typeof(GameObject), true);
        
        GUILayout.BeginHorizontal();
        startPos.x = EditorGUILayout.FloatField("相机初始位置（x,y,z)", startPos.x);
        startPos.y = EditorGUILayout.FloatField(startPos.y);
        startPos.z = EditorGUILayout.FloatField(startPos.z);
        GUILayout.EndHorizontal();
        if (autoScanScene)
        {
            //相机参数
            GUILayout.Label("相机自动扫描的参数设置");
            stepx = EditorGUILayout.FloatField("x方向间隔长度", stepx);
            stepy = EditorGUILayout.FloatField("z方向间隔长度", stepy);
            GUILayout.BeginHorizontal();
            scanSize.x = EditorGUILayout.FloatField("扫描区域大小(x ,z)", scanSize.x);
            scanSize.y = EditorGUILayout.FloatField(scanSize.y);
            GUILayout.EndHorizontal();
            internalFrame = EditorGUILayout.IntField("间隔帧数", internalFrame);

        }
        //修改prefab
        if (GUILayout.Button("应用"))
        {
            //FindPrefab();
            //路径包含名字
            if (TestTool != null)
            {
                TestTool.GetComponent<TestSceneTool>().startPos = startPos;
                if (autoScanScene)
                {
                    TestTool.GetComponent<TestSceneTool>().stepx = stepx;
                    TestTool.GetComponent<TestSceneTool>().stepy = stepy;
                    TestTool.GetComponent<TestSceneTool>().scanSize = scanSize;
                    TestTool.GetComponent<TestSceneTool>().internalFrame = internalFrame;
                }

                //var newTestToolPrefab = PrefabUtility.CreateEmptyPrefab(AssetDatabase.GUIDToAssetPath(ids[index]));
                //PrefabUtility.ReplacePrefab(TestTool, newTestToolPrefab, ReplacePrefabOptions.Default);
                //AssetDatabase.SaveAssets();
                Debug.Log("修改TestTool一次");
                if (TestToolInScene != null)
                {
                    Object.DestroyImmediate(TestToolInScene);
                    TestToolInScene = PrefabUtility.InstantiatePrefab(TestTool) as GameObject;
                    Debug.Log("自动扫描，重新生成TestToolInScene");
                }

            }
            else
            {
                Debug.Log("没有放入TestTool!");
            }
            //DeletePrefab();

        }
        GUILayout.Space(10);

        //设置不合格条件
        if (IFall = EditorGUILayout.ToggleLeft("设置判断条件", IFall))
        {
            GUILayout.BeginHorizontal();
            if (IFtriangles = EditorGUILayout.ToggleLeft("triangles", IFtriangles))
                triangles = EditorGUILayout.IntField(triangles);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (IFvertices = EditorGUILayout.ToggleLeft("vertices", IFvertices))
                vertices = EditorGUILayout.IntField(vertices);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (IFbatches = EditorGUILayout.ToggleLeft("batches", IFbatches))
                batches = EditorGUILayout.IntField(batches);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (IFdrawcalls = EditorGUILayout.ToggleLeft("drawcalls", IFdrawcalls))
                drawcalls = EditorGUILayout.IntField(drawcalls);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (IFdynamicBatchedDrawCalls = EditorGUILayout.ToggleLeft("dynamicBatchedDrawCalls", IFdynamicBatchedDrawCalls))
                dynamicBatchedDrawCalls = EditorGUILayout.IntField(dynamicBatchedDrawCalls);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (IFstaticBatchedDrawCalls = EditorGUILayout.ToggleLeft("staticBatchedDrawCalls", IFstaticBatchedDrawCalls))
                staticBatchedDrawCalls = EditorGUILayout.IntField(staticBatchedDrawCalls);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (IFinstancedBatchedDrawCalls = EditorGUILayout.ToggleLeft("instancedBatchedDrawCalls", IFinstancedBatchedDrawCalls))
                instancedBatchedDrawCalls = EditorGUILayout.IntField(instancedBatchedDrawCalls);
            GUILayout.EndHorizontal();

        }

        //显示数据
        GUILayout.Space(10);
        GUILayout.Label("Triangles: " + UnityEditor.UnityStats.triangles, GUILayout.Width(500));
        GUILayout.Label("Vertices: " + UnityEditor.UnityStats.vertices, GUILayout.Width(500));
        GUILayout.Space(10);
        GUILayout.Label("Batch: " + UnityEditor.UnityStats.batches, GUILayout.Width(500));
        GUILayout.Label("Static Batch: " + UnityEditor.UnityStats.staticBatches, GUILayout.Width(500));
        GUILayout.Label("DynamicBatch: " + UnityEditor.UnityStats.dynamicBatches, GUILayout.Width(500));
        GUILayout.Space(10);
        GUILayout.Label("Total DrawCall: " + UnityEditor.UnityStats.drawCalls, GUILayout.Width(500));
        GUILayout.Label("Static Batch DrawCall: " + UnityEditor.UnityStats.staticBatchedDrawCalls, GUILayout.Width(500));
        GUILayout.Label("DynamicBatch DrawCall: " + UnityEditor.UnityStats.dynamicBatchedDrawCalls, GUILayout.Width(500));
        GUILayout.Label("Instanced Batched DrawCalls: " + UnityStats.instancedBatchedDrawCalls, GUILayout.Width(500));

        ShowPosition();
        if (!(XbadPositions.Count == 0 && YbadPositions.Count == 0 && _XbadPositions.Count == 0 && _YbadPositions.Count == 0)&&!EditorApplication.isPlaying&&!EditorApplication.isPaused)
        {
            if (GUILayout.Button("清除列表"))
            {
                Reset();
                Repaint();
            }
        }
    }


}
