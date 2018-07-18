using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
[CanEditMultipleObjects]

public class ChangeTextureOrder : EditorWindow
{

    //改变纹理顺序
    [MenuItem("地形/改变地形纹理顺序")]

    static void Open()
    {
        GetWindow<ChangeTextureOrder>();
    }
    private Terrain ToChange;
    private bool click = false;

    [SerializeField]//强制unity去序列化一个私有域, 在Inspector中显示
    //要改成的纹理
    protected List<UnityEngine.Texture2D> desTexture = new List<UnityEngine.Texture2D>();
    [SerializeField]
    //要被修改的terrains
    protected List<UnityEngine.Terrain> desTerrains = new List<UnityEngine.Terrain>();
    //存放被修改的textures
    protected List<SplatPrototype>ChangedTextures=new List<SplatPrototype>();
    //第几个被修改的
    int count = 0;


    //序列化对象
    protected SerializedObject _serializedObject0;
    protected SerializedObject _serializedObject;
    //序列化属性
    protected SerializedProperty _assetLstProperty0;
    protected SerializedProperty _assetLstProperty;

    protected void OnEnable()
    {
        //使用当前类初始化
        _serializedObject0 = new SerializedObject(this);
        //使用当前类初始化
        _serializedObject = new SerializedObject(this);
        //获取当前类中可序列话的属性
        _assetLstProperty0 = _serializedObject0.FindProperty("desTerrains");
        //获取当前类中可序列话的属性
        _assetLstProperty = _serializedObject.FindProperty("desTexture");

    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("该插件作用为解决：修改地形纹理顺序的问题");
        //ToChange = EditorGUILayout.ObjectField("修改哪个地形", ToChange, typeof(Terrain), true) as Terrain;//要修改的地形

        EditorGUILayout.LabelField("要修改的地图");
        //更新
        _serializedObject0.Update();
        //检查是否有修改
        EditorGUI.BeginChangeCheck();
        //显示要修改的纹理
        EditorGUILayout.PropertyField(_assetLstProperty0, true);
        //检查是否修改有结束
        if (EditorGUI.EndChangeCheck())
        {
            _serializedObject0.ApplyModifiedProperties();
        }

        EditorGUILayout.LabelField("改成以下纹理");
        //更新
        _serializedObject.Update();
        //检查是否有修改
        EditorGUI.BeginChangeCheck();
        //显示要修改的纹理
        EditorGUILayout.PropertyField(_assetLstProperty, true);
        //检查是否修改有结束
        if (EditorGUI.EndChangeCheck())
        {
            _serializedObject.ApplyModifiedProperties();
        }
        if (EditorGUILayout.Toggle("执行修改", false))
        {
            foreach( var t in desTerrains)
            {
                ChangedTextures.Clear();
                //ChangedTextures = new SplatPrototype[ToChange.terrainData.splatPrototypes.Length];
                foreach (var item in desTexture)
                {
                    ChangeTextures(item, count,t);
                    count++;
                }
                if (ChangedTextures.Count < t.terrainData.splatPrototypes.Length)
                    Debug.Log("地形texture数目减少");
                else if (ChangedTextures.Count > t.terrainData.splatPrototypes.Length)
                    Debug.Log("地形texture数目增加");
                t.terrainData.splatPrototypes = ChangedTextures.ToArray();
            }
            
        }

    }

    void ChangeTextures(UnityEngine.Texture2D sample, int index,Terrain t)
    {
        SplatPrototype temp= new SplatPrototype();
        if (index<t.terrainData.splatPrototypes.Length)
        {
            temp = t.terrainData.splatPrototypes[index];
            temp.texture = sample;
        }
        else
        {
            temp.texture = sample;
            temp.tileOffset = new Vector2(0, 0);
            temp.tileSize = new Vector2(15, 15);

        }
        ChangedTextures.Add(temp);
    }


}
