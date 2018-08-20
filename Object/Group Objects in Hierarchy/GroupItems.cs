using UnityEngine;
using UnityEditor;
using System.Collections.Generic;


public static class GroupItems
{
    [MenuItem("GameObject/Group Selected %g")]
    private static void GroupSelected()
    {
        if (!Selection.activeTransform) return;

        var go = new GameObject(Selection.activeTransform.name + " Group");
        Undo.RegisterCreatedObjectUndo(go, "Group Selected");
        go.transform.SetParent(Selection.activeTransform.parent, false);

        Transform[] toTrans = Selection.transforms;
        Vector3 finalPos = new Vector3(0, 0, 0);
        int Count = 0;
        foreach (var transform in Selection.transforms)
        {
            Undo.SetTransformParent(transform, go.transform, "Group Selected");
            Count++;
            finalPos += transform.localPosition;
        }
        finalPos = finalPos / (1.0f * Count);
        go.transform.localPosition = finalPos;
        foreach (var transform in Selection.transforms)
        {
            transform.localPosition = transform.localPosition - go.transform.localPosition;
        }

        Selection.activeGameObject = go;
        
    }

    [MenuItem("GameObject/Group Selected %g", true)]
    private static bool EnableGroupSelected()
    {
        if (!EditorApplication.isPlaying) return true;
        //Debug.Log("Can't group in playing mode!");
        return false;
    }

    [MenuItem("GameObject/Cancel Group Selected %#g")]
    private static void CancelGroupSelected()
    {
        //select now
        GameObject[] currentGroups = Selection.gameObjects;
        //after cancel group in select
        List<GameObject> AfterGroups = new List<GameObject>();

        for(int m=0;m<currentGroups.Length;m++)
        {
            int count = currentGroups[m].transform.childCount;
            for (int n=0;n<count;n++)
            {
                Transform temp = currentGroups[m].transform.GetChild(0);
                AfterGroups.Add(temp.gameObject);
                temp.SetParent(currentGroups[m].transform.parent);
            }

            //delete when it is an empty object
            var components = currentGroups[m].GetComponents<Component>();
            if (components.Length<=1&& currentGroups[m].transform.childCount == 0)
                GameObject.DestroyImmediate(currentGroups[m]);
        }
        Selection.objects = AfterGroups.ToArray();

    }

    [MenuItem("GameObject/Cancel Group Selected %#g", true)]
    private static bool EnableCancelGroupSelected()
    {
        if (!EditorApplication.isPlaying) return true;
        //Debug.Log("Can't cancel group in playing mode!");
        return false;
    }

}
