using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System;
using System.Reflection;

public enum Phase { Setup,Processing,Complete};//search peocess
public enum PathDrawingMode { Full, ShortRelevantParts, Shortest };//search process
public enum SearchedType { Asset, SceneObject, Mixed };//search target type

[Serializable]
public class SubAssetToSearch
{
    public UnityEngine.Object subAsset;
    public bool shouldSearch;

    public SubAssetToSearch(UnityEngine.Object ssubAsset,bool sshouldSearch)
    {
        this.subAsset = ssubAsset;
        this.shouldSearch = sshouldSearch;
    }
}

// Custom class to hold the results for a single scene or Assets folder
[Serializable]
public class ReferenceHolder:ISerializationCallbackReceiver
{
    //a single path to a reference
    public struct ReferencePath
    {
        public readonly ReferenceNode startNode;
        public readonly int[] pathLinksToFollow;

        public ReferencePath(ReferenceNode startNode,int[]pathIndices)
        {
            this.startNode = startNode;
            pathLinksToFollow = pathIndices;
        }
    }
    [Serializable]
    public class SerializableNode
    {
        public int instanceId;
        public bool isUnityObject;
        public string description;
        public List<int> links;
        public List<string> linkDescriptions;
    }
    private string title;
    private bool clickable;
    private List<ReferenceNode> references;
    private List<ReferencePath> referencePathsShortUnique;
    private List<ReferencePath> referencePathsShortest;
    private List<SerializableNode> serializedNodes;
    private List<int> initialSerializedNodes;

    public int NumberOfReferences
    {
        get
        {
            return references.Count;
        }
    }

    public ReferenceHolder(string title,bool clickable)
    {
        this.title = title;
        this.clickable = clickable;
        references = new List<ReferenceNode>();
        referencePathsShortUnique = null;
        referencePathsShortest = null;
    }

    public void AddReference(ReferenceNode node)
    {
        references.Add(node);
    }

    public void InitializeNodes()
    {
        for (int i = 0; i < references.Count; i++)
            references[i].InitializeRecurisively();
    }
    //check whether contained
    public bool Contains(ReferenceNode node)
    {
        for(int i=0;i<references.Count;i++)
        {
            if (references[i] == node)
                return true;
        }
        return false;
    }
    // Add all the Object's in this container to the set
    public void AddObjectsTo(HashSet<UnityEngine.Object> objectsSet)
    {
        CalculateShortestPathsToReferences();

        for (int i = 0; i < referencePathsShortUnique.Count; i++)
        {
            UnityEngine.Object obj = referencePathsShortUnique[i].startNode.UnityObject;
            if (obj != null && !obj.Equals(null))
                objectsSet.Add(obj);
        }
    }

    // Add all the GameObject's in this container to the set
    public void AddGameObjectsTo(HashSet<GameObject> gameObjectsSet)
    {
        CalculateShortestPathsToReferences();

        for (int i = 0; i < referencePathsShortUnique.Count; i++)
        {
            UnityEngine.Object obj = referencePathsShortUnique[i].startNode.UnityObject;
            if (obj != null && !obj.Equals(null))
            {
                if (obj is GameObject)
                    gameObjectsSet.Add((GameObject)obj);
                else if (obj is Component)
                    gameObjectsSet.Add(((Component)obj).gameObject);
            }
        }
    }

    // Calculate short unique paths to the references
    public void CalculateShortestPathsToReferences()
    {
        if (referencePathsShortUnique != null)
            return;

        referencePathsShortUnique = new List<ReferencePath>(32);
        for (int i = 0; i < references.Count; i++)
            references[i].CalculateShortUniquePaths(referencePathsShortUnique);

        referencePathsShortest = new List<ReferencePath>(referencePathsShortUnique.Count);
        for (int i = 0; i < referencePathsShortUnique.Count; i++)
        {
            int[] linksToFollow = referencePathsShortUnique[i].pathLinksToFollow;

            // Find the last two nodes in this path
            ReferenceNode nodeBeforeLast = referencePathsShortUnique[i].startNode;
            for (int j = 0; j < linksToFollow.Length - 1; j++)
                nodeBeforeLast = nodeBeforeLast[linksToFollow[j]].targetNode;

            // Check if these two nodes are unique
            bool isUnique = true;
            for (int j = 0; j < referencePathsShortest.Count; j++)
            {
                ReferencePath path = referencePathsShortest[j];
                if (path.startNode == nodeBeforeLast && path.pathLinksToFollow[0] == linksToFollow[linksToFollow.Length - 1])
                {
                    isUnique = false;
                    break;
                }
            }

            if (isUnique)
                referencePathsShortest.Add(new ReferencePath(nodeBeforeLast, new int[1] { linksToFollow[linksToFollow.Length - 1] }));
        }
    }
    private bool IsAsset(UnityEngine.Object obj)
    {
        return obj is UnityEngine.Object && !string.IsNullOrEmpty(AssetDatabase.GetAssetPath((UnityEngine.Object)obj));
    }
    //ping an object in editor
    private void PingInEditor(UnityEngine.Object obj)
    {
        if (obj is Component)
            obj = ((Component)obj).gameObject;
        if (IsAsset(obj) && obj is GameObject)
        {
            Transform objTR = ((GameObject)obj).transform;
            while (objTR.parent != null && objTR.parent.parent != null)
                objTR = objTR.parent;

            obj = objTR.gameObject;
        }

        EditorGUIUtility.PingObject(obj);

    }
    //select obj in window
    public void SelectInEditor(UnityEngine.Object obj)
    {
        if (obj == null)
            return;
        Event e = Event.current;
        //press ctrl key can multi-select
        if (!e.control)
        {
            PingInEditor(obj);
            Selection.activeObject = obj;
        }
        else
        {
            Component objAsComp = obj as Component;
            GameObject objAsGO = obj as GameObject;
            int selectionIndex = -1;

            UnityEngine.Object[] selection = Selection.objects;
            for (int i = 0; i < selection.Length; i++)
            {
                UnityEngine.Object selected = selection[i];
                if (selected == obj || (objAsComp != null && selected == objAsComp.gameObject) ||
    (objAsGO != null && selected is Component && ((Component)selected).gameObject == objAsGO))
                {
                    selectionIndex = i;
                    break;
                }
            }
            UnityEngine.Object[] newSelection;
            if (selectionIndex == -1)
            {
                // Include object in selection
                newSelection = new UnityEngine.Object[selection.Length + 1];
                selection.CopyTo(newSelection, 0);
                newSelection[selection.Length] = obj;
            }
            else
            {
                // Remove object from selection
                newSelection = new UnityEngine.Object[selection.Length - 1];
                int j = 0;
                for (int i = 0; i < selectionIndex; i++, j++)
                    newSelection[j] = selection[i];
                for (int i = selectionIndex + 1; i < selection.Length; i++, j++)
                    newSelection[j] = selection[i];
            }

            Selection.objects = newSelection;
        }
    }

    // Draw the results found in this scene/asset
    public void DrawOnGUI(PathDrawingMode pathDrawingMode)
    {
        Color c = GUI.color;
        GUI.color = Color.cyan;
        //show in which scene or asset
        if (GUILayout.Button(title, AssetUsageDetector.BoxGUIStyle, AssetUsageDetector.GUI_EXPAND_WIDTH) && clickable)
        {
            // If the container (scene, usually) is clicked, highlight it on Project view
            SelectInEditor(AssetDatabase.LoadAssetAtPath<SceneAsset>(title));
        }

        GUI.color = Color.white;
        //show full paths
        if (pathDrawingMode == PathDrawingMode.Full)
        {
            for (int i = 0; i < references.Count; i++)
            {
                //line space
                GUILayout.Space(5);
                references[i].DrawOnGUIRecursively();
            }
        }
        else
        {
            if (referencePathsShortUnique == null)
                CalculateShortestPathsToReferences();

            List<ReferencePath> pathsToDraw;
            if (pathDrawingMode == PathDrawingMode.ShortRelevantParts)
                pathsToDraw = referencePathsShortUnique;
            else
                pathsToDraw = referencePathsShortest;
            //draw all paths at each line
            for (int i = 0; i < pathsToDraw.Count; i++)
            {
                GUILayout.Space(5);
                GUILayout.BeginHorizontal();

                ReferencePath path = pathsToDraw[i];
                path.startNode.DrawOnGUI(null);

                ReferenceNode currentNode = path.startNode;
                for (int j = 0; j < path.pathLinksToFollow.Length; j++)
                {
                    ReferenceNode.Link link = currentNode[path.pathLinksToFollow[j]];
                    link.targetNode.DrawOnGUI(link.description);
                    currentNode = link.targetNode;
                }
                GUILayout.EndHorizontal();
            }
        }
        GUI.color = c;
        GUILayout.Space(10);
    }

    // Assembly reloading; serialize nodes in a way that Unity can serialize
    public void OnBeforeSerialize()
    {
        if (references == null)
            return;

        if (serializedNodes == null)
            serializedNodes = new List<SerializableNode>(references.Count * 5);
        else
            serializedNodes.Clear();

        if (initialSerializedNodes == null)
            initialSerializedNodes = new List<int>(references.Count);
        else
            initialSerializedNodes.Clear();

        Dictionary<ReferenceNode, int> nodeToIndex = new Dictionary<ReferenceNode, int>(references.Count * 5);
        for (int i = 0; i < references.Count; i++)
            initialSerializedNodes.Add(references[i].SerializeRecursively(nodeToIndex, serializedNodes));
    }

    // Assembly reloaded; deserialize nodes to construct the original graph
    public void OnAfterDeserialize()
    {
        if (initialSerializedNodes == null || serializedNodes == null)
            return;

        if (references == null)
            references = new List<ReferenceNode>(initialSerializedNodes.Count);
        else
            references.Clear();

        List<ReferenceNode> allNodes = new List<ReferenceNode>(serializedNodes.Count);
        for (int i = 0; i < serializedNodes.Count; i++)
            allNodes.Add(new ReferenceNode());

        for (int i = 0; i < serializedNodes.Count; i++)
            allNodes[i].Deserialize(serializedNodes[i], allNodes);

        for (int i = 0; i < initialSerializedNodes.Count; i++)
            references.Add(allNodes[initialSerializedNodes[i]]);

        referencePathsShortUnique = null;
        referencePathsShortest = null;

        serializedNodes.Clear();
        initialSerializedNodes.Clear();
    }


}

public class ReferenceNode
{
    public struct Link
    {
        public readonly ReferenceNode targetNode;
        public readonly string description;
        public Link(ReferenceNode targetNode, string description)
        {
            this.targetNode = targetNode;
            this.description = description;
        }
    }
    public object nodeObject;
    private readonly List<Link> links;

    private static int uidLast = 0;
    private readonly int uid;

    //可空类型，就是一种特殊的值类型,可以为null
    private int? instanceId; // instanceId of the nodeObject if it is a Unity object, null otherwise
    private string description; // String to print on this node

    //get InstanceIDToObject
    public UnityEngine.Object UnityObject
    {
        get
        {
            if (instanceId.HasValue)
                return EditorUtility.InstanceIDToObject(instanceId.Value);
            else
                return null;
        }
    }
    //get links count
    public int NumberOfOutgoingLinks
    {
        get
        {
            return links.Count;
        }
    }
    //get link at index
    public Link this[int index]
    {
        get
        {
            return links[index];
        }
    }
    //constructor
    public ReferenceNode()
    {
        links = new List<Link>(2);
        uid = uidLast++;
    }
    public ReferenceNode(UnityEngine.Object obj)
    {
        links = new List<Link>(2);
        uid = uidLast++;
        nodeObject = obj;
    }
    //add a node to links
    public void AddLinkTo(ReferenceNode nextNode,string description=null)
    {
        if(nextNode!=null)
        {
            if (!string.IsNullOrEmpty(description))
                description = "[" + description + "]";
            links.Add(new Link(nextNode, description));
        }
    }

    public void InitializeRecurisively()
    {
        if (description != null)
            return;
        UnityEngine.Object unityObject = nodeObject as UnityEngine.Object;
        //if unityObject is an object
        if(unityObject!=null)
        {
            instanceId = unityObject.GetInstanceID();
            description = unityObject.name + "(" + unityObject.GetType() + ")";
        }
        //if unityObject is not a object
        else if(nodeObject!=null)
        {
            instanceId = null;
            description = nodeObject.GetType() + "object";
        }
        else
        {
            instanceId = null;
            description = "<destroyed>>";
        }

        nodeObject = null;

        for (int i = 0; i < links.Count; i++)
            links[i].targetNode.InitializeRecurisively();
    }

    //clear this node
    public void Clear()
    {
        nodeObject = null;
        links.Clear();
    }

    private void CalculateShortUniquePaths(List<ReferenceHolder.ReferencePath> shortestPaths, List<ReferenceNode>currentPath,List<int>currentPathIndices,int latestObjectIndexInPath)
    {
        int currentIndex = currentPath.Count;
        currentPath.Add(this);
        if(links.Count==0)
        {
            // Check if the path to the reference is unique (not discovered so far)
            bool isUnique = true;
            for (int i = 0; i < shortestPaths.Count; i++)
            {
                if (shortestPaths[i].startNode == currentPath[latestObjectIndexInPath] && shortestPaths[i].pathLinksToFollow.Length == currentPathIndices.Count - latestObjectIndexInPath - 1)
                {
                    int j = latestObjectIndexInPath + 1;
                    for (int k = 0; j < currentPathIndices.Count; j++, k++)
                    {
                        if (shortestPaths[i].pathLinksToFollow[k] != currentPathIndices[j])
                            break;
                    }
                    if (j == currentPathIndices.Count)
                    {
                        isUnique = false;
                        break;
                    }
                }
            }
            // Don't allow duplicate short paths
            if (isUnique)
            {
                int[] pathIndices = new int[currentPathIndices.Count - latestObjectIndexInPath - 1];
                for (int i = latestObjectIndexInPath + 1, j = 0; i < currentPathIndices.Count; i++, j++)
                    pathIndices[j] = currentPathIndices[i];

                shortestPaths.Add(new ReferenceHolder.ReferencePath(currentPath[latestObjectIndexInPath], pathIndices));
            }
        }
        else
        {
            if (instanceId.HasValue) // nodeObject is Unity object
                latestObjectIndexInPath = currentIndex;
            for (int i = 0; i < links.Count; i++)
            {
                currentPathIndices.Add(i);
                links[i].targetNode.CalculateShortUniquePaths(shortestPaths, currentPath, currentPathIndices, latestObjectIndexInPath);
                currentPathIndices.RemoveAt(currentIndex + 1);
            }
        }
        currentPath.RemoveAt(currentIndex);
    }
    // Calculate short unique paths that start with this node
    public void CalculateShortUniquePaths(List<ReferenceHolder.ReferencePath> currentPaths)
    {
        CalculateShortUniquePaths(currentPaths, new List<ReferenceNode>(8), new List<int>(8) { -1 }, 0);
    }

    //getthe string representation of this node
    private string GetNodeContent(string linkToPrevNodeDescription=null)
    {
        if (!string.IsNullOrEmpty(linkToPrevNodeDescription))
            return linkToPrevNodeDescription + "\n" + description;
        return description;
    }
    //whether an asset
    private bool IsAsset(UnityEngine.Object obj)
    {
        return obj is UnityEngine.Object && !string.IsNullOrEmpty(AssetDatabase.GetAssetPath((UnityEngine.Object)obj));
    }
    //ping an object in editor
    private void PingInEditor(UnityEngine.Object obj)
    {
        if (obj is Component)
            obj = ((Component)obj).gameObject;
        if (IsAsset(obj) && obj is GameObject)
        {
            Transform objTR = ((GameObject)obj).transform;
            while (objTR.parent != null && objTR.parent.parent != null)
                objTR = objTR.parent;

            obj = objTR.gameObject;
        }

        EditorGUIUtility.PingObject(obj);

    }
    //select obj in window
    public void SelectInEditor(UnityEngine.Object obj)
    {
        if (obj == null)
            return;
        Event e = Event.current;
        //press ctrl key can multi-select
        if (!e.control)
        {
            PingInEditor(obj);
            Selection.activeObject = obj;
        }
        else
        {
            Component objAsComp = obj as Component;
            GameObject objAsGO = obj as GameObject;
            int selectionIndex = -1;

            UnityEngine.Object[] selection = Selection.objects;
            for (int i = 0; i < selection.Length; i++)
            {
                UnityEngine.Object selected = selection[i];
                if (selected == obj || (objAsComp != null && selected == objAsComp.gameObject) ||
    (objAsGO != null && selected is Component && ((Component)selected).gameObject == objAsGO))
                {
                    selectionIndex = i;
                    break;
                }
            }
            UnityEngine.Object[] newSelection;
            if (selectionIndex == -1)
            {
                // Include object in selection
                newSelection = new UnityEngine.Object[selection.Length + 1];
                selection.CopyTo(newSelection, 0);
                newSelection[selection.Length] = obj;
            }
            else
            {
                // Remove object from selection
                newSelection = new UnityEngine.Object[selection.Length - 1];
                int j = 0;
                for (int i = 0; i < selectionIndex; i++, j++)
                    newSelection[j] = selection[i];
                for (int i = selectionIndex + 1; i < selection.Length; i++, j++)
                    newSelection[j] = selection[i];
            }

            Selection.objects = newSelection;
        }
    }
    //draw one node on GUI
    public void DrawOnGUI(string linkToPrevNodeDescription)
    {
        string label = GetNodeContent(linkToPrevNodeDescription);
        if(GUILayout.Button(label,AssetUsageDetector.BoxGUIStyle,AssetUsageDetector.GUI_EXPAND_WIDTH))
        {
            SelectInEditor(UnityObject);
        }
        //whether show tooltip
        if (AssetUsageDetector.showToolTips && Event.current.type == EventType.Repaint && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
            AssetUsageDetector.toolTip = label;
    }
    //Draw all paths that start with this node
    public void DrawOnGUIRecursively(string linkToPrevNodeDescription=null)
    {
        GUILayout.BeginHorizontal();
        DrawOnGUI(linkToPrevNodeDescription);
        if (links.Count > 0)
        {
            GUILayout.BeginVertical();

            for (int i = 0; i < links.Count; i++)
            {
                ReferenceNode next = links[i].targetNode;
                next.DrawOnGUIRecursively(links[i].description);
            }

            GUILayout.EndVertical();
        }
        GUILayout.EndHorizontal();
    }
    // Serialize this node and its connected nodes recursively
    public int SerializeRecursively(Dictionary<ReferenceNode, int> nodeToIndex, List<ReferenceHolder.SerializableNode> serializedNodes)
    {
        int index;
        if (nodeToIndex.TryGetValue(this, out index))
            return index;

        ReferenceHolder.SerializableNode serializedNode = new ReferenceHolder.SerializableNode()
        {
            instanceId = instanceId ?? 0,
            isUnityObject = instanceId.HasValue,
            description = description
        };

        index = serializedNodes.Count;
        nodeToIndex[this] = index;
        serializedNodes.Add(serializedNode);

        if (links.Count > 0)
        {
            serializedNode.links = new List<int>(links.Count);
            serializedNode.linkDescriptions = new List<string>(links.Count);

            for (int i = 0; i < links.Count; i++)
            {
                serializedNode.links.Add(links[i].targetNode.SerializeRecursively(nodeToIndex, serializedNodes));
                serializedNode.linkDescriptions.Add(links[i].description);
            }
        }

        return index;
    }

    // Deserialize this node and its links from the serialized data
    public void Deserialize(ReferenceHolder.SerializableNode serializedNode, List<ReferenceNode> allNodes)
    {
        if (serializedNode.isUnityObject)
            instanceId = serializedNode.instanceId;
        else
            instanceId = null;

        description = serializedNode.description;

        if (serializedNode.links != null)
        {
            for (int i = 0; i < serializedNode.links.Count; i++)
                links.Add(new Link(allNodes[serializedNode.links[i]], serializedNode.linkDescriptions[i]));
        }
    }

    public override int GetHashCode()
    {
        return uid;
    }

}

// Delegate to get the value of a variable (either field or property)
public delegate object VariableGetVal(object obj);

// Credit: http://stackoverflow.com/questions/724143/how-do-i-create-a-delegate-for-a-net-property
public interface IPropertyAccessor
{
    object GetValue(object source);
}

// A wrapper class for properties to get their values more efficiently
public class PropertyWrapper<TObject, TValue> : IPropertyAccessor where TObject : class
{
    private readonly Func<TObject, TValue> getter;

    public PropertyWrapper(MethodInfo getterMethod)
    {
        getter = (Func<TObject, TValue>)Delegate.CreateDelegate(typeof(Func<TObject, TValue>), getterMethod);
    }

    public object GetValue(object obj)
    {
        try
        {
            return getter((TObject)obj);
        }
        catch
        {
            // Property getters may return various kinds of exceptions
            // if their backing fields are not initialized (yet)
            return null;
        }
    }
}

// Custom struct to hold a variable, its important properties and its getter function
public struct VariableGetterHolder
{
    public readonly string name;
    public readonly bool isProperty;
    public readonly bool isSerializable;
    private readonly VariableGetVal getter;

    public VariableGetterHolder(FieldInfo fieldInfo, VariableGetVal getter, bool isSerializable)
    {
        name = fieldInfo.Name;
        isProperty = false;
        this.isSerializable = isSerializable;
        this.getter = getter;
    }

    public VariableGetterHolder(PropertyInfo propertyInfo, VariableGetVal getter, bool isSerializable)
    {
        name = propertyInfo.Name;
        isProperty = true;
        this.isSerializable = isSerializable;
        this.getter = getter;
    }

    public object Get(object obj)
    {
        return getter(obj);
    }
}

//main class
public class AssetUsageDetector : EditorWindow
{
    //GUI Layout
    private string errorMessage = string.Empty;
    private Vector2 scrollPosition = Vector2.zero;
    public static GUILayoutOption GUI_WIDTH_25 = GUILayout.Width(25);
    public static GUILayoutOption GUI_EXPAND_WIDTH = GUILayout.ExpandWidth(true);
    public static GUILayoutOption GUI_EXPAND_HEIGHT = GUILayout.ExpandHeight(true);
    private static GUIStyle m_boxGUIStyle; // GUIStyle used to draw the results of the search
    public static GUIStyle BoxGUIStyle
    {
        get
        {
            if(m_boxGUIStyle==null)
            {
                m_boxGUIStyle=new GUIStyle(EditorStyles.helpBox)
                {
                    alignment = TextAnchor.MiddleCenter,
                    font = EditorStyles.label.font
                };
            }
            return m_boxGUIStyle;
        }
    }

    //asset to search,target assets you want to find dependencies
    private List<UnityEngine.Object> AssetsToSearch = new List<UnityEngine.Object>() {null};
    private List<SubAssetToSearch> subAssetsToSearch = new List<SubAssetToSearch>();
    private bool showSubAssetsFoldout = true;//whether show sub assets included in search

    //a set that contains assets that be searched and their subAssets
    private HashSet<UnityEngine.Object> assetsSet;
    private Type[] assetClasses;//the searched objects

    //default options at the beginning
    private Phase currentPhase = Phase.Setup;//at first user should set choice
    private PathDrawingMode pathDrawingMode = PathDrawingMode.ShortRelevantParts;
    private bool restoreInitialSceneSetup = true;//

    //tooltip
    public static string toolTip = null;
    public static bool showToolTips = false;//whether show tips when mouse is on the button
    private static GUIStyle m_tooltipGUIStyle; // GUIStyle used to draw the tooltip
    private static GUIStyle TooltipGUIStyle
    {
        get
        {
            GUIStyleState normalState;

            if (m_tooltipGUIStyle != null)
                normalState = m_tooltipGUIStyle.normal;
            else
            {
                m_tooltipGUIStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    alignment = TextAnchor.MiddleCenter,
                    font = EditorStyles.label.font
                };

                normalState = m_tooltipGUIStyle.normal;

                normalState.background = null;
                normalState.textColor = Color.black;
            }

            if (normalState.background == null || normalState.background.Equals(null))
            {
                Texture2D backgroundTexture = new Texture2D(1, 1);
                backgroundTexture.SetPixel(0, 0, new Color(0.88f, 0.88f, 0.88f, 0.85f));
                backgroundTexture.Apply();

                normalState.background = backgroundTexture;
            }

            return m_tooltipGUIStyle;
        }
    }

    //refresh interval in paly mode
    private const float playModeRefreshInterval = 1.0f;
    private double nextPlayModeRefreshTime = 0.0f;

    //initial open or loaded scenes
    private SceneSetup[] initialSceneSetup;

    //search field
    private bool searchInAssetsFolder = true;
    private bool searchInAllScenes = false;
    private bool searchInScenesInBuild = false;
    private bool searchInScenesInBuildTicked = false;
    private bool searchInOpenScenes = true;

    //Search Depth
    private int searchDepthLimit = 1;
    private int currentDepth = 0;

    //Search variables
    private BindingFlags fieldModifiers = BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic;
    private BindingFlags propertyModifiers = BindingFlags.Instance | BindingFlags.DeclaredOnly;
    private int prevSearchDepthLimit;
    private BindingFlags prevFieldModifiers;
    private BindingFlags prevPropertyModifiers;

    //search target type
    private bool searchMaterialAssets;
    private bool searchGameObjectReferences;
    private bool searchMonoBehavioursForScript;
    private bool searchRenderers;
    private bool searchMaterialsForShader;
    private bool searchMaterialsForTexture;
    private bool searchSerializableVariablesOnly;
    private SearchedType searchedType; // Whether we are searching for an asset's references or a scene object's references, or mixed

    //search result
    private int searchCount;//the number of th search result
    private double searchStartTime;
    private List<ReferenceHolder> searchResult = new List<ReferenceHolder>(); // Overall search results
    private List<ReferenceNode> nodesPool = new List<ReferenceNode>(32);
    private List<VariableGetterHolder> validVariables = new List<VariableGetterHolder>(32);

    private ReferenceHolder currentReferenceHolder; // Results for the currently searched scene
    private Dictionary<Type, VariableGetterHolder[]> typeToVariables; // An optimization to fetch & filter fields and properties of a class only once
    private Dictionary<Type, bool> searchableTypes; // An optimization to search only certain types for references that can store the searched object(s) in their variables
    private Dictionary<string, ReferenceNode> searchedObjects; // An optimization to search an object only once (key is a hash of the searched object)
    private Stack<object> callStack; // Stack of SearchObject function parameters to avoid infinite loops (which happens when same object is passed as parameter to function)
    private Stack<Type> searchedTypesStack; // Stack of TypeCanContainReferences function parameters to avoid infinite loops

    //main entrance in 'window/asset usage detector'
    [MenuItem("Window/Asset Usage Detector")]
    static void Init()
    {
        AssetUsageDetector window = GetWindow<AssetUsageDetector>();
        window.titleContent = new GUIContent("Asset Usage Detector");

        window.Show();
    }

    //update at a regular interval
    private void Update()
    {
        if(EditorApplication.isPlaying&&currentPhase==Phase.Complete&&showToolTips&&EditorApplication.timeSinceStartup>=nextPlayModeRefreshTime)
        {
            nextPlayModeRefreshTime = EditorApplication.timeSinceStartup + playModeRefreshInterval;
            Repaint();
        }
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUI_EXPAND_WIDTH, GUI_EXPAND_HEIGHT);
        GUILayout.BeginVertical();

        //When there is sth wrong before start searching
        if(errorMessage.Length!=0)
        {
            EditorGUILayout.HelpBox(errorMessage, MessageType.Error);
        }

        //status 1
        if(currentPhase==Phase.Processing)
        {
            GUILayout.Label("error!");
            //error error error
            restoreInitialSceneSetup = EditorGUILayout.ToggleLeft("Restore initial scene setup (Recommended)", restoreInitialSceneSetup);
            if(GUILayout.Button("Return"))
            {
                if(!restoreInitialSceneSetup||RestoreIntialSceneSetup())
                {
                    errorMessage = string.Empty;
                    currentPhase = Phase.Setup;
                }
            }
        }
        //status 2
        else if(currentPhase==Phase.Setup)
        {
            //show target assets
            if(ExposedTargetAssets())
            {
                //if targets have changed
                OnTargetAssetsChanged();
            }
            //if there exists sub assets
            if(subAssetsToSearch.Count>0)
            {
                GUILayout.BeginHorizontal();
                bool toggleAllSubAssets = subAssetsToSearch[0].shouldSearch;
                bool mixedToggle = false;
                for(int i=0;i<subAssetsToSearch.Count;i++)
                    if(subAssetsToSearch[i].shouldSearch!=toggleAllSubAssets)
                    {
                        mixedToggle = true;
                        break;
                    }
                if (mixedToggle)
                    EditorGUI.showMixedValue = true;

                GUI.changed = false;
                toggleAllSubAssets = EditorGUILayout.Toggle(toggleAllSubAssets);
                //once change toggle,modify 'shouldSearch' in subAssetsToSearch
                if (GUI.changed)
                    for (int i = 0; i < subAssetsToSearch.Count; i++)
                        subAssetsToSearch[i].shouldSearch = toggleAllSubAssets;

                EditorGUI.showMixedValue = false;
                showSubAssetsFoldout = EditorGUILayout.Foldout(showSubAssetsFoldout, "Include subAssets in search:", true);
                GUILayout.EndHorizontal();

                if(showSubAssetsFoldout)
                {
                    for(int i=0;i<subAssetsToSearch.Count;i++)
                    {
                        GUILayout.BeginHorizontal();
                        subAssetsToSearch[i].shouldSearch = EditorGUILayout.Toggle(subAssetsToSearch[i].shouldSearch);
                        //users can't edit subAsset
                        GUI.enabled = false;
                        EditorGUILayout.ObjectField(string.Empty, subAssetsToSearch[i].subAsset, typeof(UnityEngine.Object), true);
                        GUI.enabled = true;
                        GUILayout.EndHorizontal();
                        
                    }
                }


            }

            //second part
            GUILayout.Box("Search In", GUI_EXPAND_WIDTH);
            searchInAssetsFolder = EditorGUILayout.ToggleLeft("Whole Project (Assets Folder)",searchInAssetsFolder);
            if(EditorApplication.isPlaying)
            {
                searchInScenesInBuild = false;
                searchInScenesInBuildTicked = false;
                searchInAllScenes = false;
            }

            GUI.enabled = false;
            if (!EditorApplication.isPlaying)
                GUI.enabled = true;
            searchInAllScenes = EditorGUILayout.ToggleLeft("All Scene(s) in Project", searchInAllScenes);

            GUI.enabled = true;
            if (searchInAllScenes&&!EditorApplication.isPlaying)
                GUI.enabled = false;
            searchInOpenScenes = EditorGUILayout.ToggleLeft("Currently Open Scene(s)", searchInOpenScenes);

            GUI.enabled = true;
            if (searchInAllScenes || EditorApplication.isPlaying)
                GUI.enabled = false;
            searchInScenesInBuild = EditorGUILayout.ToggleLeft("Scene(s) in Build Setting", searchInScenesInBuild);
            if(searchInScenesInBuild)
            {
                GUILayout.BeginHorizontal();
                searchInScenesInBuildTicked = EditorGUILayout.ToggleLeft("Ticked Only", searchInScenesInBuildTicked);
                searchInScenesInBuildTicked = !EditorGUILayout.ToggleLeft("All", !searchInScenesInBuildTicked);
                GUILayout.EndHorizontal();
            }
            GUI.enabled = true;


            //third part
            GUILayout.Box("Search Settings", GUI_EXPAND_WIDTH);
            //search depth
            searchDepthLimit= EditorGUILayout.IntSlider("Search Depth:", searchDepthLimit, 0, 4);
            GUILayout.Label(new GUIContent("(recursive depth)" , "Depth limit for recursively searching variables of objects"));
            //search variables
            GUILayout.Label("Search Variables:");
            GUILayout.BeginHorizontal();

            if (EditorGUILayout.ToggleLeft("Public", (fieldModifiers & BindingFlags.Public) == BindingFlags.Public))
                fieldModifiers |= BindingFlags.Public;
            else
                fieldModifiers &= ~BindingFlags.Public;

            if(EditorGUILayout.ToggleLeft("Non Public", (fieldModifiers & BindingFlags.NonPublic) == BindingFlags.NonPublic))
                fieldModifiers |= BindingFlags.NonPublic;
            else
                fieldModifiers &= ~BindingFlags.NonPublic;
            GUILayout.EndHorizontal();

            //search properties
            GUILayout.Label("Search Properties:");
            GUILayout.BeginHorizontal();
            if (EditorGUILayout.ToggleLeft("Public", (propertyModifiers & BindingFlags.Public) == BindingFlags.Public))
                propertyModifiers |= BindingFlags.Public;
            else
                propertyModifiers &= ~BindingFlags.Public;

            if (EditorGUILayout.ToggleLeft("Non Public", (propertyModifiers & BindingFlags.NonPublic) == BindingFlags.NonPublic))
                propertyModifiers |= BindingFlags.NonPublic;
            else
                propertyModifiers &= ~BindingFlags.NonPublic;
            GUILayout.EndHorizontal();

            //fourth part,start to search
            if (!searchInAllScenes && !searchInAssetsFolder && !searchInOpenScenes && !searchInScenesInBuild && !searchInScenesInBuildTicked)
                GUI.enabled = false;
            if(GUILayout.Button("Start Search"))
            {
                if (AreTargetAssetsEmpty())
                    errorMessage = "ADD A TARGET ASSET!";
                else if (!EditorApplication.isPlaying && !AreScenesSaved())
                    errorMessage = "SAVE ALL SCENES FIRST!";
                else
                {
                    errorMessage = string.Empty;
                    currentPhase = Phase.Processing;
                    if (!EditorApplication.isPlaying)
                        initialSceneSetup = EditorSceneManager.GetSceneManagerSetup();//scenes open right now
                    else
                        initialSceneSetup = null;
                    //start seach!!!
                    ExecuteQuery();
                }

            }
        }
        //status 3
        else if(currentPhase==Phase.Complete)
        {
            //draw the result
            GUI.enabled = false;
            ExposedTargetAssets();
            GUI.enabled = true;

            if (GUILayout.Button("Reset Search"))
            {
                if (!restoreInitialSceneSetup || RestoreIntialSceneSetup())
                {
                    errorMessage = string.Empty;
                    currentPhase = Phase.Setup;

                    if (searchResult != null)
                        searchResult.Clear();
                }
            }

            Color c = GUI.color;
            GUI.color = Color.red;
            GUILayout.Box("Don't forget to save scene(s) if you made any changes!", GUI_EXPAND_WIDTH);
            GUI.color = c;
            if(searchResult.Count == 0)
                {
                GUILayout.Box("No results found...", GUI_EXPAND_WIDTH);
            }
                else
                {
                GUILayout.BeginHorizontal();

                // Select all the references after filtering them (select only the GameObject's)
                if (GUILayout.Button("Select All\n(GameObject-wise)"))
                {
                    HashSet<GameObject> uniqueGameObjects = new HashSet<GameObject>();
                    for (int i = 0; i < searchResult.Count; i++)
                        searchResult[i].AddGameObjectsTo(uniqueGameObjects);

                    if (uniqueGameObjects.Count > 0)
                    {
                        GameObject[] objects = new GameObject[uniqueGameObjects.Count];
                        uniqueGameObjects.CopyTo(objects);
                        Selection.objects = objects;
                    }
                }

                // Select all the references without filtering them
                if (GUILayout.Button("Select All\n(Object-wise)"))
                {
                    HashSet<UnityEngine.Object> uniqueObjects = new HashSet<UnityEngine.Object>();
                    for (int i = 0; i < searchResult.Count; i++)
                        searchResult[i].AddObjectsTo(uniqueObjects);

                    if (uniqueObjects.Count > 0)
                    {
                        UnityEngine.Object[] objects = new UnityEngine.Object[uniqueObjects.Count];
                        uniqueObjects.CopyTo(objects);
                        Selection.objects = objects;
                    }
                }

                GUILayout.EndHorizontal();
                GUILayout.Space(10);
                showToolTips = EditorGUILayout.ToggleLeft("Show tooltips", showToolTips);
                GUILayout.Space(10);
                GUILayout.Label("Path drawing mode:");

                GUILayout.BeginHorizontal();
                GUILayout.Space(35);
                if (EditorGUILayout.ToggleLeft("Full: Draw the complete paths to the references (can be slow with too many references)", pathDrawingMode == PathDrawingMode.Full))
                    pathDrawingMode = PathDrawingMode.Full;
                GUILayout.EndHorizontal();

                //GUILayout.BeginHorizontal();

                //GUILayout.Space(35);

                //if (EditorGUILayout.ToggleLeft("Shorter: Draw only the most relevant unique parts of the complete paths that start with a UnityEngine.Object", pathDrawingMode == PathDrawingMode.ShortRelevantParts))
                //    pathDrawingMode = PathDrawingMode.ShortRelevantParts;

                //GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Space(35);
                if (EditorGUILayout.ToggleLeft("Shortest: Draw only the last two nodes of complete paths that are unique", pathDrawingMode == PathDrawingMode.Shortest))
                    pathDrawingMode = PathDrawingMode.Shortest;
                GUILayout.EndHorizontal();

                // Tooltip gets its value in ReferenceHolder.DrawOnGUI function
                toolTip = null;
                for (int i = 0; i < searchResult.Count; i++)
                    searchResult[i].DrawOnGUI(pathDrawingMode);

                if (toolTip != null)
                {
                    // Show tooltip at mouse position
                    Vector2 mousePos = Event.current.mousePosition;
                    Vector2 size = TooltipGUIStyle.CalcSize(new GUIContent(toolTip));

                    GUI.Box(new Rect(new Vector2(mousePos.x - size.x * 0.5f, mousePos.y - size.y), size), toolTip, TooltipGUIStyle);
                }
            }

        }
        GUILayout.EndVertical();
        GUILayout.EndScrollView();

    }
    
    //close scenes which are not in initial scene setup
    private bool RestoreIntialSceneSetup()
    {
        //
        if (EditorApplication.isPlaying || initialSceneSetup == null)
            return true;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return false;
        SceneSetup[] sceneFinalSetup = EditorSceneManager.GetSceneManagerSetup();
        //close scenes that are not in initialscenes
        for(int i=0;i<sceneFinalSetup.Length;i++)
        {
            bool sceneIsOneOfInitials = false;
            for(int j=0;j<initialSceneSetup.Length;j++)
            {
                if(sceneFinalSetup[i].path==initialSceneSetup[j].path)
                {
                    sceneIsOneOfInitials = true;
                    break;
                }
                if (!sceneIsOneOfInitials)
                    EditorSceneManager.CloseScene(EditorSceneManager.GetSceneByPath(sceneFinalSetup[i].path), true);
            }
        }

        for (int i = 0; i < initialSceneSetup.Length; i++)
        {
            if (!initialSceneSetup[i].isLoaded)
                EditorSceneManager.CloseScene(EditorSceneManager.GetSceneByPath(initialSceneSetup[i].path), false);
        }

        return true;

    }

    //show users the serachedAssets as an array field on GUI
    private bool ExposedTargetAssets()
    {
        bool hasChanged = false;
        Event e=Event.current;

        GUILayout.BeginHorizontal();
        //the first line at the top
        GUILayout.Label("Asset(s) you want to search dependencies :");
        //users can drag a group of asssets to ↑ to add targets
        if(GUI.enabled)
        {
            if((e.type==EventType.DragPerform||e.type==EventType.DragUpdated)&&GUILayoutUtility.GetLastRect().Contains(e.mousePosition))
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                if(e.type==EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    UnityEngine.Object[] draggedObjects = DragAndDrop.objectReferences;
                    if(draggedObjects.Length!=0)
                    {
                        for(int i=0;i<draggedObjects.Length;i++)
                        {
                            if(draggedObjects[i]!=null&&!draggedObjects[i].Equals(null))
                            {
                                hasChanged = true;
                                AssetsToSearch.Add(draggedObjects[i]);
                            }
                        }
                    }
                }
                e.Use();
            }
            if(GUILayout.Button("+", GUI_WIDTH_25))
            {
                //add a new target at the beginning
                AssetsToSearch.Insert(0, null);
            }
        }
        GUILayout.EndHorizontal();

        //Show target assets in GUI
        for(int i=0;i<AssetsToSearch.Count;i++)
        {
            GUI.changed = false;
            GUILayout.BeginHorizontal();
            UnityEngine.Object preAssetToSearch = AssetsToSearch[i];
            AssetsToSearch[i] = EditorGUILayout.ObjectField("", AssetsToSearch[i], typeof(UnityEngine.Object), true);
            if (GUI.changed && preAssetToSearch != AssetsToSearch[i])
                hasChanged = true;
            if (GUI.enabled)
            {
                if (GUILayout.Button("+", GUI_WIDTH_25))
                    AssetsToSearch.Insert(i + 1, null);
                if (GUILayout.Button("-", GUI_WIDTH_25))
                {
                    if (AssetsToSearch[i] != null && !AssetsToSearch[i].Equals(null))
                        hasChanged = true;
                    AssetsToSearch.RemoveAt(i);
                    i = i - 1;
                }                  

            }
            GUILayout.EndHorizontal();
        }
        return hasChanged;
    }

    private bool AreTargetAssetsEmpty()
    {
        for (int i = 0; i < AssetsToSearch.Count; i++)
        {
            if (AssetsToSearch[i] != null && !AssetsToSearch[i].Equals(null))
                return false;
        }
        return true;
    }

    private bool AreScenesSaved()
    {
        for(int i=0;i<EditorSceneManager.loadedSceneCount;i++)
        {
            Scene scene = EditorSceneManager.GetSceneAt(i);
            if (scene.isDirty || string.IsNullOrEmpty(scene.path))
                return false;
        }
        return true;
    }

    //judge whether this obj is asset
    private bool IsAsset(UnityEngine.Object obj)
    {
        return obj is UnityEngine.Object&&!string.IsNullOrEmpty(AssetDatabase.GetAssetPath((UnityEngine.Object)obj));
    }

    //when target assets changed in editor window
    private void OnTargetAssetsChanged()
    {
        errorMessage = string.Empty;
        subAssetsToSearch.Clear();
        if (AreTargetAssetsEmpty())
            return;

        MonoScript[] monoScriptsInProject = null;
        HashSet<UnityEngine.Object> currentSubAssets = new HashSet<UnityEngine.Object>();
        for(int i=0;i<AssetsToSearch.Count;i++)
        {
            UnityEngine.Object assetToSearch = AssetsToSearch[i];
            if (assetToSearch == null || assetToSearch.Equals(null))
                continue;
            //when it doesn't have subAsset
            if (!IsAsset(assetToSearch) || !AssetDatabase.IsMainAsset(assetToSearch) || assetToSearch is SceneAsset)
                continue;

            //find subAsset of the searched assets
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(assetToSearch));
            for(int j=0;j<assets.Length;j++)
            {
                UnityEngine.Object asset = assets[j];
                if (asset == null || asset.Equals(null) || asset is Component)
                    continue;
                if (currentSubAssets.Contains(asset))
                    continue;
                //if asset has subasset
                if(asset!=assetToSearch)
                {
                    currentSubAssets.Add(asset);
                    subAssetsToSearch.Add(new SubAssetToSearch(asset, true));
                }
                //if asset is monoscript
                if(asset is MonoScript)
                {
                    Type monoScriptType = ((MonoScript)asset).GetClass();
                    if (monoScriptType == null || (!monoScriptType.IsInterface && !typeof(Component).IsAssignableFrom(monoScriptType)))
                        continue;
                    // Find all MonoScript objects in the project
                    if(monoScriptsInProject==null)
                    {
                        string[] pathToMonoScripts = AssetDatabase.FindAssets("t:MonoScript");//t: Types
                        monoScriptsInProject = new MonoScript[pathToMonoScripts.Length];
                        for (int k = 0; k < pathToMonoScripts.Length; k++)
                            monoScriptsInProject[k] = AssetDatabase.LoadAssetAtPath<MonoScript>(AssetDatabase.GUIDToAssetPath(pathToMonoScripts[k]));
                    }

                    for(int k=0;k<monoScriptsInProject.Length;k++)
                    {
                        Type otherMonoScriptType = monoScriptsInProject[k].GetClass();
                        if (otherMonoScriptType == null || monoScriptType == otherMonoScriptType || !monoScriptType.IsAssignableFrom(otherMonoScriptType))
                            continue;
                        if(!currentSubAssets.Contains(monoScriptsInProject[k]))
                        {
                            subAssetsToSearch.Add(new SubAssetToSearch(monoScriptsInProject[k], false));
                            currentSubAssets.Add(monoScriptsInProject[k]);
                        }
                    }
                }
            }

            
        }

    }

    private string Hash(object obj)
    {
        if (obj is UnityEngine.Object)
            return obj.GetHashCode().ToString();

        return obj.GetHashCode() + obj.GetType().Name;
    }

    private void ExecuteQuery()
    {
        //prepare before search
        searchCount = 0;
        searchStartTime = EditorApplication.timeSinceStartup;
        if (searchResult == null)
            searchResult = new List<ReferenceHolder>(16);
        else
            searchResult.Clear();
        if (typeToVariables == null)
            typeToVariables = new Dictionary<Type, VariableGetterHolder[]>(4096);
        else if (searchDepthLimit != prevSearchDepthLimit || prevFieldModifiers != fieldModifiers || prevPropertyModifiers != propertyModifiers)
            typeToVariables.Clear();

        if (searchableTypes == null)
            searchableTypes = new Dictionary<Type, bool>(4096);
        else
            searchableTypes.Clear();

        if (searchedObjects == null)
            searchedObjects = new Dictionary<string, ReferenceNode>(32768);
        else
            searchedObjects.Clear();

        if (callStack == null)
            callStack = new Stack<object>(64);
        else
            callStack.Clear();

        if (searchedTypesStack == null)
            searchedTypesStack = new Stack<Type>(8);

        if (assetsSet == null)
            assetsSet = new HashSet<UnityEngine.Object>();
        else
            assetsSet.Clear();

        prevSearchDepthLimit = searchDepthLimit;
        prevFieldModifiers = fieldModifiers;
        prevPropertyModifiers = propertyModifiers;

        HashSet<Type> allAssetClasses = new HashSet<Type>();

        searchMaterialAssets = false;
        searchGameObjectReferences = false;
        searchMonoBehavioursForScript = false;
        searchRenderers = false;
        searchMaterialsForShader = false;
        searchMaterialsForTexture = false;

        for (int i = 0; i < AssetsToSearch.Count; i++)
        {
            bool isAsset = IsAsset(AssetsToSearch[i]);

            if (i == 0)
                searchedType = isAsset ? SearchedType.Asset : SearchedType.SceneObject;
            else if (searchedType != SearchedType.Mixed)
            {
                if (isAsset && searchedType == SearchedType.SceneObject)
                    searchedType = SearchedType.Mixed;
                else if (!isAsset && searchedType == SearchedType.Asset)
                    searchedType = SearchedType.Mixed;
            }
        }

        //store the searched assets and their sub-assets in a set
        try
        {
            // Temporarily add main searched asset(s) to sub-assets list to avoid duplicate code
            for (int i = 0; i < AssetsToSearch.Count; i++)
                subAssetsToSearch.Add(new SubAssetToSearch(AssetsToSearch[i], true));

            for (int i = 0; i < subAssetsToSearch.Count; i++)
            {
                if (subAssetsToSearch[i].shouldSearch)
                {
                    UnityEngine.Object asset = subAssetsToSearch[i].subAsset;
                    if (asset == null || asset.Equals(null))
                        continue;

                    assetsSet.Add(asset);
                    allAssetClasses.Add(asset.GetType());

                    if (asset is MonoScript)
                    {
                        Type monoScriptType = ((MonoScript)asset).GetClass();
                        if (monoScriptType != null && typeof(Component).IsAssignableFrom(monoScriptType))
                            allAssetClasses.Add(monoScriptType);
                    }
                    else if (asset is GameObject)
                    {
                        // If searched asset is a GameObject, include its components in the search
                        Component[] components = ((GameObject)asset).GetComponents<Component>();
                        for (int j = 0; j < components.Length; j++)
                        {
                            if (components[j] == null || components[j].Equals(null))
                                continue;

                            assetsSet.Add(components[j]);
                            allAssetClasses.Add(components[j].GetType());
                        }
                    }
                }
            }
        }
        finally
        {
            subAssetsToSearch.RemoveRange(subAssetsToSearch.Count - AssetsToSearch.Count, AssetsToSearch.Count);
        }
        assetClasses = new Type[allAssetClasses.Count];
        allAssetClasses.CopyTo(assetClasses);
        foreach (UnityEngine.Object asset in assetsSet)
        {
            // Initialize the nodes of searched asset(s)
            searchedObjects.Add(Hash(asset), new ReferenceNode(asset));

            if (asset is Texture)
            {
                searchMaterialAssets = true;
                searchRenderers = true;
                searchMaterialsForTexture = true;
            }
            else if (asset is Material)
            {
                searchRenderers = true;
            }
            else if (asset is MonoScript)
            {
                searchMonoBehavioursForScript = true;
            }
            else if (asset is Shader)
            {
                searchMaterialAssets = true;
                searchRenderers = true;
                searchMaterialsForShader = true;
            }
            else if (asset is GameObject)
            {
                searchGameObjectReferences = true;
            }
        }

        // Find the scenes to search for references
        HashSet<string> scenesToSearch = new HashSet<string>();
        if (searchInAllScenes)
        {
            // Get all scenes from the Assets folder
            string[] scenesTemp = AssetDatabase.FindAssets("t:SceneAsset");
            for (int i = 0; i < scenesTemp.Length; i++)
                scenesToSearch.Add(AssetDatabase.GUIDToAssetPath(scenesTemp[i]));
        }
        else
        {
            if (searchInOpenScenes)
            {
                // Get all open (and loaded) scenes
                for (int i = 0; i < EditorSceneManager.loadedSceneCount; i++)
                {
                    Scene scene = EditorSceneManager.GetSceneAt(i);
                    if (scene.IsValid())
                        scenesToSearch.Add(scene.path);
                }
            }

            if (searchInScenesInBuild)
            {
                // Get all scenes in build settings
                EditorBuildSettingsScene[] scenesTemp = EditorBuildSettings.scenes;
                for (int i = 0; i < scenesTemp.Length; i++)
                {
                    if (!searchInScenesInBuildTicked || scenesTemp[i].enabled)
                        scenesToSearch.Add(scenesTemp[i].path);
                }
            }
        }

        // By default, search only serializable variables for references
        searchSerializableVariablesOnly = true;

        // Don't search assets if searched object is a scene object as assets can't hold references to scene objects
        if (searchInAssetsFolder && searchedType != SearchedType.SceneObject)
        {
            currentReferenceHolder = new ReferenceHolder("Project View (Assets)", false);

            // Search through all the prefabs and imported models in the project
            string[] pathsToAssets = AssetDatabase.FindAssets("t:GameObject");
            for (int i = 0; i < pathsToAssets.Length; i++)
                SearchGameObjectRecursively(AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(pathsToAssets[i])));

            // Search through all the scriptable objects in the project
            pathsToAssets = AssetDatabase.FindAssets("t:ScriptableObject");
            for (int i = 0; i < pathsToAssets.Length; i++)
                BeginSearchObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(pathsToAssets[i])));

            // If a searched asset is shader or texture, search through all the materials in the project
            if (searchMaterialAssets)
            {
                pathsToAssets = AssetDatabase.FindAssets("t:Material");
                for (int i = 0; i < pathsToAssets.Length; i++)
                    BeginSearchObject(AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(pathsToAssets[i])));
            }

            // Search through all the animation clips in the project
            pathsToAssets = AssetDatabase.FindAssets("t:AnimationClip");
            for (int i = 0; i < pathsToAssets.Length; i++)
                BeginSearchObject(AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(pathsToAssets[i])));

            // Search through all the animator controllers in the project
            pathsToAssets = AssetDatabase.FindAssets("t:RuntimeAnimatorController");
            for (int i = 0; i < pathsToAssets.Length; i++)
                BeginSearchObject(AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AssetDatabase.GUIDToAssetPath(pathsToAssets[i])));

            // If a reference is found in the Project view, save the results
            if (currentReferenceHolder.NumberOfReferences > 0)
                searchResult.Add(currentReferenceHolder);
        }

        // Search non-serializable variables for references only if we are currently searching a scene and the editor is in play mode
        if (EditorApplication.isPlaying)
            searchSerializableVariablesOnly = false;

        foreach (string scenePath in scenesToSearch)
        {
            // Search scene for references
            if (scenePath != null)
                SearchScene(scenePath);
        }

        // Search through all the GameObjects under the DontDestroyOnLoad scene (if exists)
        if (EditorApplication.isPlaying)
        {
            currentReferenceHolder = new ReferenceHolder("DontDestroyOnLoad", false);

            GameObject[] rootGameObjects = GetDontDestroyOnLoadObjects().ToArray();
            for (int i = 0; i < rootGameObjects.Length; i++)
                SearchGameObjectRecursively(rootGameObjects[i]);

            if (currentReferenceHolder.NumberOfReferences > 0)
                searchResult.Add(currentReferenceHolder);
        }

        for (int i = 0; i < searchResult.Count; i++)
            searchResult[i].InitializeNodes();

        // Log some c00l stuff to console
        Debug.Log("Searched " + searchCount + " objects in " + (EditorApplication.timeSinceStartup - searchStartTime).ToString("F2") + " seconds");

        // Search is complete!
        currentPhase = Phase.Complete;
    }

    // Retrieve the game objects listed under the DontDestroyOnLoad scene
    private List<GameObject> GetDontDestroyOnLoadObjects()
    {
        List<GameObject> result = new List<GameObject>();

        List<GameObject> rootGameObjectsExceptDontDestroyOnLoad = new List<GameObject>();
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            rootGameObjectsExceptDontDestroyOnLoad.AddRange(SceneManager.GetSceneAt(i).GetRootGameObjects());
        }

        List<GameObject> rootGameObjects = new List<GameObject>();
        Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
        for (int i = 0; i < allTransforms.Length; i++)
        {
            Transform root = allTransforms[i].root;
            if (root.hideFlags == HideFlags.None && !rootGameObjects.Contains(root.gameObject))
            {
                rootGameObjects.Add(root.gameObject);
            }
        }

        for (int i = 0; i < rootGameObjects.Count; i++)
        {
            if (!rootGameObjectsExceptDontDestroyOnLoad.Contains(rootGameObjects[i]))
                result.Add(rootGameObjects[i]);
        }

        return result;
    }

    // Get reference node for object
    private ReferenceNode GetReferenceNode(object nodeObject, string hash = null)
    {
        if (hash == null)
            hash = Hash(nodeObject);

        ReferenceNode result;
        if (!searchedObjects.TryGetValue(hash, out result) || result == null)
        {
            result = PopReferenceNode(nodeObject);
            searchedObjects[hash] = result;
        }

        return result;
    }

    // Fetch a reference node from pool
    private ReferenceNode PopReferenceNode(object nodeObject)
    {
        if (nodesPool.Count == 0)
        {
            for (int i = 0; i < 32; i++)
                nodesPool.Add(new ReferenceNode());
        }

        int index = nodesPool.Count - 1;
        ReferenceNode node = nodesPool[index];
        node.nodeObject = nodeObject;
        nodesPool.RemoveAt(index);

        return node;
    }

    // Pool a reference node
    private void PoolReferenceNode(ReferenceNode node)
    {
        node.Clear();
        nodesPool.Add(node);
    }
    
    // Check if this type can possibly contain references to the searched asset(s)
    private bool IsTypeSearchable(Type type, int depth = 0)
    {
        if (type.IsPrimitive || type == typeof(string))
            return false;

        if (searchedTypesStack.Contains(type))
            return false;

        bool result;
        if (searchableTypes.TryGetValue(type, out result))
            return result;

        if (type == typeof(GameObject) || type == typeof(AnimationClip) || type == typeof(Animation) ||
            type == typeof(Animator) || typeof(RuntimeAnimatorController).IsAssignableFrom(type) ||
            ((searchMaterialsForShader || searchMaterialsForTexture) && typeof(Material).IsAssignableFrom(type)) ||
            (searchRenderers && typeof(Renderer).IsAssignableFrom(type)))
        {
            searchableTypes.Add(type, true);
            return true;
        }

        for (int i = 0; i < assetClasses.Length; i++)
        {
            if (type.IsAssignableFrom(assetClasses[i]))
            {
                searchableTypes.Add(type, true);
                return true;
            }
        }

        if (type.IsArray)
        {
            if (IsTypeSearchable(type.GetElementType(), depth))
            {
                searchableTypes.Add(type, true);
                return true;
            }
        }

        if (type.IsGenericType)
        {
            Type[] generics = type.GetGenericArguments();
            for (int i = 0; i < generics.Length; i++)
            {
                if (IsTypeSearchable(generics[i], depth))
                {
                    searchableTypes.Add(type, true);
                    return true;
                }
            }
        }

        if (depth < searchDepthLimit)
        {
            try
            {
                searchedTypesStack.Push(type);

                HashSet<Type> searchedVariableTypes = new HashSet<Type>();
                FieldInfo[] fields = type.GetFields(fieldModifiers);
                for (int i = 0; i < fields.Length; i++)
                {
                    Type fieldType = fields[i].FieldType;
                    if (searchedVariableTypes.Contains(fieldType))
                        continue;

                    searchedVariableTypes.Add(fieldType);
                    if (IsTypeSearchable(fieldType, depth + 1))
                    {
                        searchableTypes.Add(type, true);
                        return true;
                    }
                }

                PropertyInfo[] properties = type.GetProperties(propertyModifiers);
                for (int i = 0; i < properties.Length; i++)
                {
                    Type propertyType = properties[i].PropertyType;
                    if (searchedVariableTypes.Contains(propertyType))
                        continue;

                    if (properties[i].GetIndexParameters().Length == 0 && properties[i].GetGetMethod((propertyModifiers & BindingFlags.NonPublic) == BindingFlags.NonPublic) != null)
                    {
                        searchedVariableTypes.Add(propertyType);
                        if (IsTypeSearchable(properties[i].PropertyType, depth + 1))
                        {
                            searchableTypes.Add(type, true);
                            return true;
                        }
                    }
                }
            }
            finally
            {
                searchedTypesStack.Pop();
            }
        }

        if (depth == 0)
            searchableTypes.Add(type, false);

        return false;
    }
    
    // Get filtered variables for a type
    private VariableGetterHolder[] GetFilteredVariablesForType(Type type)
    {
        VariableGetterHolder[] result;
        if (typeToVariables.TryGetValue(type, out result))
            return result;

        // This is the first time this type of object is seen, filter and cache its variables
        // Variable filtering process:
        // 1- skip Obsolete variables
        // 2- skip primitive types, enums and strings
        // 3- skip common Unity types that can't hold any references (e.g. Vector3, Rect, Color, Quaternion)

        validVariables.Clear();

        // Filter the fields
        if (fieldModifiers != (BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            Type currType = type;
            while (currType != typeof(object))
            {
                FieldInfo[] fields = currType.GetFields(fieldModifiers);
                for (int i = 0; i < fields.Length; i++)
                {
                    // Skip obsolete fields
                    if (Attribute.IsDefined(fields[i], typeof(ObsoleteAttribute)))
                        continue;

                    // Skip primitive types
                    Type fieldType = fields[i].FieldType;
                    if (fieldType.IsPrimitive || fieldType == typeof(string) || fieldType.IsEnum)
                        continue;

#if USE_EXPERIMENTAL_METHOD
						if( !IsTypeSearchable( fieldType ) )
							continue;
#else
                    if (fieldType.IsPrimitiveUnityType())
                        continue;
#endif

                    VariableGetVal getter = fields[i].CreateGetter(type);
                    if (getter != null)
                        validVariables.Add(new VariableGetterHolder(fields[i], getter, fields[i].IsSerializable()));
                }

                currType = currType.BaseType;
            }
        }

        if (propertyModifiers != (BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            Type currType = type;
            while (currType != typeof(object))
            {
                PropertyInfo[] properties = currType.GetProperties(propertyModifiers);
                for (int i = 0; i < properties.Length; i++)
                {
                    // Skip obsolete properties
                    if (Attribute.IsDefined(properties[i], typeof(ObsoleteAttribute)))
                        continue;

                    // Skip primitive types
                    Type propertyType = properties[i].PropertyType;
                    if (propertyType.IsPrimitive || propertyType == typeof(string) || propertyType.IsEnum)
                        continue;

#if USE_EXPERIMENTAL_METHOD
						if( !IsTypeSearchable( propertyType ) )
							continue;
#else
                    if (propertyType.IsPrimitiveUnityType())
                        continue;
#endif

                    // Additional filtering for properties:
                    // 1- Ignore "gameObject", "transform", "rectTransform" and "attachedRigidbody" properties of Component's to get more useful results
                    // 2- Ignore "canvasRenderer" and "canvas" properties of Graphic components
                    // 3 & 4- Prevent accessing properties of Unity that instantiate an existing resource (causing leak)
                    string propertyName = properties[i].Name;
                    if (typeof(Component).IsAssignableFrom(currType) && (propertyName.Equals("gameObject") ||
                        propertyName.Equals("transform") || propertyName.Equals("attachedRigidbody") || propertyName.Equals("rectTransform")))
                        continue;
                    else if (typeof(UnityEngine.UI.Graphic).IsAssignableFrom(currType) &&
                        (propertyName.Equals("canvasRenderer") || propertyName.Equals("canvas")))
                        continue;
                    else if (typeof(MeshFilter).IsAssignableFrom(currType) && propertyName.Equals("mesh"))
                        continue;
                    else if (typeof(Renderer).IsAssignableFrom(currType) &&
                        (propertyName.Equals("sharedMaterial") || propertyName.Equals("sharedMaterials")))
                        continue;
                    else if ((propertyName.Equals("material") || propertyName.Equals("materials")) &&
                        (typeof(Renderer).IsAssignableFrom(currType) || typeof(Collider).IsAssignableFrom(currType) ||
                        typeof(Collider2D).IsAssignableFrom(currType) || typeof(GUIText).IsAssignableFrom(currType)))
                        continue;
                    else
                    {
                        VariableGetVal getter = properties[i].CreateGetter();
                        if (getter != null)
                            validVariables.Add(new VariableGetterHolder(properties[i], getter, properties[i].IsSerializable()));
                    }
                }

                currType = currType.BaseType;
            }
        }

        result = validVariables.ToArray();

        // Cache the filtered fields
        typeToVariables.Add(type, result);

        return result;
    }

    // Search through field and properties of an object for references
    private void SearchFieldsAndPropertiesOf(ReferenceNode referenceNode)
    {
        // Get filtered variables for this object
        VariableGetterHolder[] variables = GetFilteredVariablesForType(referenceNode.nodeObject.GetType());
        for (int i = 0; i < variables.Length; i++)
        {
            // When possible, don't search non-serializable variables
            if (searchSerializableVariablesOnly && !variables[i].isSerializable)
                continue;

            try
            {
                object variableValue = variables[i].Get(referenceNode.nodeObject);
                if (variableValue == null)
                    continue;

                if (!(variableValue is IEnumerable) || variableValue is Transform)
                    referenceNode.AddLinkTo(SearchObject(variableValue), (variables[i].isProperty ? "Property: " : "Variable: ") + variables[i].name);
                else
                {
                    // If the field is IEnumerable (possibly an array or collection), search through members of it
                    // Note that Transform IEnumerable (children of the transform) is not iterated
                    foreach (object arrayItem in (IEnumerable)variableValue)
                        referenceNode.AddLinkTo(SearchObject(arrayItem), (variables[i].isProperty ? "Property (IEnumerable): " : "Variable (IEnumerable): ") + variables[i].name);
                }
            }
            catch (UnassignedReferenceException)
            { }
            catch (MissingReferenceException)
            { }
        }
    }

    // Check if the asset is used in this animation clip (and its keyframes)
    private ReferenceNode SearchAnimationClip(AnimationClip clip)
    {
        ReferenceNode referenceNode = PopReferenceNode(clip);

        // Get all curves from animation clip
        EditorCurveBinding[] objectCurves = AnimationUtility.GetObjectReferenceCurveBindings(clip);
        for (int i = 0; i < objectCurves.Length; i++)
        {
            // Search through all the keyframes in this curve
            ObjectReferenceKeyframe[] keyframes = AnimationUtility.GetObjectReferenceCurve(clip, objectCurves[i]);
            for (int j = 0; j < keyframes.Length; j++)
                referenceNode.AddLinkTo(SearchObject(keyframes[j].value), "Keyframe: " + keyframes[j].time);
        }

        return referenceNode;
    }

    // Check if the asset is used in this animator controller
    private ReferenceNode SearchAnimatorController(RuntimeAnimatorController controller)
    {
        ReferenceNode referenceNode = PopReferenceNode(controller);

        AnimationClip[] animClips = controller.animationClips;
        for (int j = 0; j < animClips.Length; j++)
            referenceNode.AddLinkTo(SearchObject(animClips[j]));

        return referenceNode;
    }

    // Check if the asset is used in this material
    private ReferenceNode SearchMaterial(Material material)
    {
        ReferenceNode referenceNode = PopReferenceNode(material);

        if (searchMaterialsForShader && assetsSet.Contains(material.shader))
            referenceNode.AddLinkTo(GetReferenceNode(material.shader), "Shader");

        if (searchMaterialsForTexture)
        {
            // Search through all the textures attached to this material
            // Credit: http://answers.unity3d.com/answers/1116025/view.html
            Shader shader = material.shader;
            int shaderPropertyCount = ShaderUtil.GetPropertyCount(shader);
            for (int k = 0; k < shaderPropertyCount; k++)
            {
                if (ShaderUtil.GetPropertyType(shader, k) == ShaderUtil.ShaderPropertyType.TexEnv)
                {
                    string propertyName = ShaderUtil.GetPropertyName(shader, k);
                    Texture assignedTexture = material.GetTexture(propertyName);
                    if (assetsSet.Contains(assignedTexture))
                        referenceNode.AddLinkTo(GetReferenceNode(assignedTexture), "Shader property: " + propertyName);
                }
            }
        }

        return referenceNode;
    }

    // Check if the asset is used in this component
    private ReferenceNode SearchComponent(Component component)
    {
        // Ignore Transform component (no object field to search for)
        if (component is Transform)
            return null;

        ReferenceNode referenceNode = PopReferenceNode(component);

        if (searchMonoBehavioursForScript && component is MonoBehaviour)
        {
            // If a searched asset is script, check if this component is an instance of it
            MonoScript script = MonoScript.FromMonoBehaviour((MonoBehaviour)component);
            if (assetsSet.Contains(script))
                referenceNode.AddLinkTo(GetReferenceNode(script));
        }

        if (searchRenderers && component is Renderer)
        {
            // If an asset is a shader, texture or material, and this component is a Renderer,
            // search it for references
            Material[] materials = ((Renderer)component).sharedMaterials;
            for (int j = 0; j < materials.Length; j++)
                referenceNode.AddLinkTo(SearchObject(materials[j]));
        }

        if (component is Animation)
        {
            // If this component is an Animation, search its animation clips for references
            foreach (AnimationState anim in (Animation)component)
                referenceNode.AddLinkTo(SearchObject(anim.clip));
        }

        if (component is Animator)
        {
            // If this component is an Animator, search its animation clips for references
            referenceNode.AddLinkTo(SearchObject(((Animator)component).runtimeAnimatorController));
        }

        SearchFieldsAndPropertiesOf(referenceNode);

        return referenceNode;
    }

    // Search through components of this GameObject in detail
    private ReferenceNode SearchGameObject(GameObject go)
    {
        ReferenceNode referenceNode = PopReferenceNode(go);

        // Check if this GameObject's prefab is one of the selected assets
        if (searchGameObjectReferences)
        {
            UnityEngine.Object prefab = PrefabUtility.GetPrefabParent(go);
            if (assetsSet.Contains(prefab) && go == PrefabUtility.FindRootGameObjectWithSameParentPrefab(go))
                referenceNode.AddLinkTo(GetReferenceNode(prefab), "Prefab object");
        }

        // Search through all the components of the object
        Component[] components = go.GetComponents<Component>();
        for (int i = 0; i < components.Length; i++)
        {
            ReferenceNode result = SearchObject(components[i]);
            if (result != null)
                referenceNode.AddLinkTo(result);
        }

        return referenceNode;
    }

    // Search an object for references
    private ReferenceNode SearchObject(object obj)
    {
        if (obj == null || obj.Equals(null))
            return null;

        // Avoid recursion (which leads to stackoverflow exception) using a stack
        if (callStack.Contains(obj))
            return null;

        // Hashing does not work well with structs all the time, don't cache search results for structs
        string objHash = null;
        if (!(obj is ValueType))
        {
            objHash = Hash(obj);

            // If object was searched before, return the cached result
            ReferenceNode cachedResult;
            if (searchedObjects.TryGetValue(objHash, out cachedResult))
            {
                if (cachedResult != null)
                    return cachedResult;

                return null;
            }
        }

        searchCount++;

        ReferenceNode result;
        UnityEngine.Object unityObject = obj as UnityEngine.Object;
        if (unityObject != null)
        {
            // If we hit a searched asset
            if (assetsSet.Contains(unityObject))
            {
                result = new ReferenceNode(unityObject);
                searchedObjects.Add(objHash, result);

                return result;
            }

            // Search the Object in detail only if EditorUtility.CollectDependencies returns a reference
            if (searchedType == SearchedType.Asset && !unityObject.HasAnyReferenceTo(assetsSet))
            {
                searchedObjects.Add(objHash, null);
                return null;
            }

            callStack.Push(unityObject);

            // Search the Object in detail
            if (unityObject is GameObject)
                result = SearchGameObject((GameObject)unityObject);
            else if (unityObject is Component)
                result = SearchComponent((Component)unityObject);
            else if (unityObject is Material)
                result = SearchMaterial((Material)unityObject);
            else if (unityObject is RuntimeAnimatorController)
                result = SearchAnimatorController((RuntimeAnimatorController)unityObject);
            else if (unityObject is AnimationClip)
                result = SearchAnimationClip((AnimationClip)unityObject);
            else
            {
                result = PopReferenceNode(unityObject);
                SearchFieldsAndPropertiesOf(result);
            }

            callStack.Pop();
        }
        else
        {
            // Comply with the recursive search limit
            if (currentDepth >= searchDepthLimit)
                return null;

            callStack.Push(obj);
            currentDepth++;

            result = PopReferenceNode(obj);
            SearchFieldsAndPropertiesOf(result);

            currentDepth--;
            callStack.Pop();
        }

        if (result != null && result.NumberOfOutgoingLinks == 0)
        {
            PoolReferenceNode(result);
            result = null;
        }

        // Cache the search result if we are skimming through a class (not a struct; i.e. objHash != null)
        // and if the object is a UnityEngine.Object (if not, cache the result only if we have actually found something
        // or we are at the root of the search; i.e. currentDepth == 0)
        if ((result != null || unityObject != null || currentDepth == 0) && objHash != null)
            searchedObjects.Add(objHash, result);

        return result;
    }
    
    // Begin searching a root object (like a GameObject or an asset)
    private void BeginSearchObject(UnityEngine.Object obj)
    {
        if (assetsSet.Contains(obj))
        {
            // Rare case: if searched object is a scene GameObject, search its components for references 
            // instead of completely ignoring the GameObject
            if (obj is GameObject && !IsAsset(obj))
            {
                ReferenceNode referenceNode = PopReferenceNode(obj);
                Component[] components = ((GameObject)obj).GetComponents<Component>();
                for (int i = 0; i < components.Length; i++)
                {
                    ReferenceNode componentNode = SearchComponent(components[i]);
                    if (componentNode == null)
                        continue;

                    if (componentNode.NumberOfOutgoingLinks > 0)
                        referenceNode.AddLinkTo(componentNode);
                    else
                        PoolReferenceNode(componentNode);
                }

                if (referenceNode.NumberOfOutgoingLinks > 0)
                    currentReferenceHolder.AddReference(referenceNode);
                else
                    PoolReferenceNode(referenceNode);
            }

            return;
        }

        ReferenceNode searchResult = SearchObject(obj);
        if (searchResult != null)
            currentReferenceHolder.AddReference(searchResult);
    }

    // Search a GameObject and its children for references recursively
    private void SearchGameObjectRecursively(GameObject go)
    {
        BeginSearchObject(go);

        Transform tr = go.transform;
        for (int i = 0; i < tr.childCount; i++)
            SearchGameObjectRecursively(tr.GetChild(i).gameObject);
    }

    // Search a scene for references
    private void SearchScene(string scenePath)
    {
        Scene scene = EditorSceneManager.GetSceneByPath(scenePath);

        if (!EditorApplication.isPlaying)
        {
            // Open the scene additively (to access its objects) only if it seems to contain some references to searched object(s)
            if (searchedType == SearchedType.Asset && !AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath).HasAnyReferenceTo(assetsSet))
                return;

            scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
        }

        currentReferenceHolder = new ReferenceHolder(scenePath, true);

        // Search through all the GameObjects in the scene
        GameObject[] rootGameObjects = scene.GetRootGameObjects();
        for (int i = 0; i < rootGameObjects.Length; i++)
            SearchGameObjectRecursively(rootGameObjects[i]);

        // If no references are found in the scene and if the scene is not part of the initial scene setup, close it
        if (currentReferenceHolder.NumberOfReferences == 0)
        {
            if (!EditorApplication.isPlaying)
            {
                bool sceneIsOneOfInitials = false;
                for (int i = 0; i < initialSceneSetup.Length; i++)
                {
                    if (initialSceneSetup[i].path == scenePath)
                    {
                        if (!initialSceneSetup[i].isLoaded)
                            EditorSceneManager.CloseScene(scene, false);

                        sceneIsOneOfInitials = true;
                        break;
                    }
                }

                if (!sceneIsOneOfInitials)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }
        else
        {
            // Some references are found in this scene, save the results
            searchResult.Add(currentReferenceHolder);
        }
    }

}

#region Extension Functions
public static class AssetUsageDetectorExtensions
{
     // Check if object depends on any of the references
    public static bool HasAnyReferenceTo(this UnityEngine.Object obj, HashSet<UnityEngine.Object> references)
    {
        UnityEngine.Object[] dependencies = EditorUtility.CollectDependencies(new UnityEngine.Object[] { obj });
        for (int i = 0; i < dependencies.Length; i++)
        {
            if (references.Contains(dependencies[i]))
                return true;
        }

        return false;
    }

    // Check if the field is serializable
    public static bool IsSerializable(this FieldInfo fieldInfo)
    {
        // see Serialization Rules: https://docs.unity3d.com/Manual/script-Serialization.html
        if (fieldInfo.IsInitOnly || ((!fieldInfo.IsPublic || fieldInfo.IsNotSerialized) &&
           !Attribute.IsDefined(fieldInfo, typeof(SerializeField))))
            return false;

        return IsTypeSerializable(fieldInfo.FieldType);
    }

    // Check if the property is serializable
    public static bool IsSerializable(this PropertyInfo propertyInfo)
    {
        return IsTypeSerializable(propertyInfo.PropertyType);
    }

    // Check if type is serializable
    private static bool IsTypeSerializable(Type type)
    {
        // see Serialization Rules: https://docs.unity3d.com/Manual/script-Serialization.html
        if (typeof(UnityEngine.Object).IsAssignableFrom(type))
            return true;

        if (type.IsArray)
        {
            if (type.GetArrayRank() != 1)
                return false;

            type = type.GetElementType();

            if (typeof(UnityEngine.Object).IsAssignableFrom(type))
                return true;
        }
        else if (type.IsGenericType)
        {
            if (type.GetGenericTypeDefinition() != typeof(List<>))
                return false;

            type = type.GetGenericArguments()[0];

            if (typeof(UnityEngine.Object).IsAssignableFrom(type))
                return true;
        }

        if (type.IsGenericType)
            return false;

        return Attribute.IsDefined(type, typeof(SerializableAttribute), false);
    }
    // Check if the type is a common Unity type (let's call them primitives)
    public static bool IsPrimitiveUnityType(this Type type)
    {
        return type.IsPrimitive || primitiveUnityTypes.Contains(type);
    }

    // Get <get> function for a field
    public static VariableGetVal CreateGetter(this FieldInfo fieldInfo, Type type)
    {
        // Commented the IL generator code below because it might actually be slower than simply using reflection
        // Credit: https://www.codeproject.com/Articles/14560/Fast-Dynamic-Property-Field-Accessors
        //DynamicMethod dm = new DynamicMethod( "Get" + fieldInfo.Name, fieldInfo.FieldType, new Type[] { typeof( object ) }, type );
        //ILGenerator il = dm.GetILGenerator();
        //// Load the instance of the object (argument 0) onto the stack
        //il.Emit( OpCodes.Ldarg_0 );
        //// Load the value of the object's field (fi) onto the stack
        //il.Emit( OpCodes.Ldfld, fieldInfo );
        //// return the value on the top of the stack
        //il.Emit( OpCodes.Ret );

        //return (VariableGetVal) dm.CreateDelegate( typeof( VariableGetVal ) );

        return fieldInfo.GetValue;
    }

    // Get <get> function for a property
    public static VariableGetVal CreateGetter(this PropertyInfo propertyInfo)
    {
        // Ignore indexer properties
        if (propertyInfo.GetIndexParameters().Length > 0)
            return null;

        MethodInfo mi = propertyInfo.GetGetMethod(true);
        if (mi != null)
        {
            Type GenType = typeof(PropertyWrapper<,>).MakeGenericType(propertyInfo.DeclaringType, propertyInfo.PropertyType);
            return ((IPropertyAccessor)Activator.CreateInstance(GenType, mi)).GetValue;
        }

        return null;
    }

    // A set of commonly used Unity types
    private static HashSet<Type> primitiveUnityTypes = new HashSet<Type>() { typeof( string ), typeof( Vector3 ), typeof( Vector2 ), typeof( Rect ),
                typeof( Quaternion ), typeof( Color ), typeof( Color32 ), typeof( LayerMask ), typeof( Vector4 ),
                typeof( Matrix4x4 ), typeof( AnimationCurve ), typeof( Gradient ), typeof( RectOffset ) };

}
#endregion
