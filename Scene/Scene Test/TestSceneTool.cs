using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class TestSceneTool : MonoBehaviour
{
    private bool firstScan = true;
    public bool autoScan = false;
    public float stepx = 5;
    public float stepy = 5;
    public Vector2 scanSize=new Vector2(800.0f,800.0f);
    public Vector3 startPos = new Vector3(0.0f, 30.0f, 0.0f);
    public int internalFrame=10;
    public direction currentDirection = direction.x;

    private Vector3 beginPos;
    private int frames = 0;//累计间隔帧数,每到一下就清零
    private int pos=0;//现在扫描变换几次了//先x再y扫描
    private int timesX = 0;//XY方向各需要变换扫描几次
    private int timesY = 0;
    private const float PI = 3.14159265f;

    //rotate camera in four directions
    public enum direction {x,y,_x,_y };

    private void Awake()
    {
        //close other camera
        foreach (Camera c in Camera.allCameras)
        {
            c.gameObject.SetActive(false);
        }
        gameObject.SetActive(true);

        if (autoScan)
        {
            ChangeToStartPos();
            firstScan = false;
        }
        else
        {
            transform.position = startPos;
        }
    }

    private void ChangeToStartPos()
    {
        //从当前位置移动到角落
        Vector3 originalPos = startPos;
        beginPos = new Vector3(originalPos.x - 0.5f * scanSize.x, originalPos.y, originalPos.z - 0.5f * scanSize.y);
        transform.position = beginPos;
        frames = 0;
        timesX = (int)(scanSize.x / stepx);
        timesY = (int)(scanSize.y / stepy);
    }

    private void FixedUpdate()
    {

        //不自动扫描则退出
        if (!autoScan)
        {
            //根据手动改变的旋转位置判断当前应归属到哪一个旋转列
            //transform.eulerAngles = new Vector3(transform.eulerAngles.x % (int)360, transform.eulerAngles.y % (int)360, transform.eulerAngles.z % (int)360);
            if((transform.eulerAngles.y>315&& transform.eulerAngles.y<=360)||(transform.eulerAngles.y >=0 && transform.eulerAngles.y < 45))
                currentDirection = direction.x;
            else if(transform.eulerAngles.y >= 45 && transform.eulerAngles.y < 135)
                currentDirection = direction.y;
            else if (transform.eulerAngles.y >=135 && transform.eulerAngles.y < 225)
                currentDirection = direction._x;
            else if (transform.eulerAngles.y >= 225 && transform.eulerAngles.y < 315)
                currentDirection = direction._y;
            return;
        }
        if(firstScan)
        {
            ChangeToStartPos();
            firstScan = false;
            return;
        }
        //隔一定帧数然后更新位置
        if (pos < (timesX+1) * (timesY+1))
        {
            frames++;
            if (frames == internalFrame)
            {
                frames = 0;
                if (currentDirection == direction.x)
                {
                    transform.eulerAngles = new Vector3(0, 90, 0); ;
                    currentDirection = direction.y;
                }
                else if (currentDirection == direction.y)
                {
                    transform.eulerAngles = new Vector3(0, 180, 0); ;
                    currentDirection = direction._x;
                }
                else if (currentDirection == direction._x)
                {
                    transform.eulerAngles = new Vector3(0, 270, 0); ;
                    currentDirection = direction._y;
                }
                else if (currentDirection == direction._y)
                {
                    transform.eulerAngles = new Vector3(0, 0, 0); ;
                    currentDirection = direction.x;

                    int addX = (pos + 1) % (timesX+1);
                    int addY = (pos+1) /( timesX+1);
                    transform.position = new Vector3(beginPos.x + addX * stepx, transform.position.y, beginPos.z + addY * stepy);
                    pos++;
                }


            }
        }
    }



#if UNITY_EDITOR
    void OnGUI()
    {
        GUI.contentColor = Color.black;
        GUILayout.Label("Triangles: " + UnityStats.triangles, GUILayout.Width(500));
        GUILayout.Label("Vertices: " + UnityStats.vertices, GUILayout.Width(500));
        GUILayout.Space(10);
        GUILayout.Label("Batch: " + UnityStats.batches, GUILayout.Width(500));
        GUILayout.Label("Static Batch: " + UnityStats.staticBatches, GUILayout.Width(500));
        GUILayout.Label("DynamicBatch: " + UnityStats.dynamicBatches, GUILayout.Width(500));
        GUILayout.Space(10);
        GUILayout.Label("Total DrawCall: " + UnityStats.drawCalls, GUILayout.Width(500));
        GUILayout.Label("Static Batch DrawCall: " + UnityStats.staticBatchedDrawCalls, GUILayout.Width(500));
        GUILayout.Label("DynamicBatch DrawCall: " + UnityStats.dynamicBatchedDrawCalls, GUILayout.Width(500));
        GUILayout.Label("Instanced Batched DrawCalls: " + UnityStats.instancedBatchedDrawCalls, GUILayout.Width(500));
    }
#endif

}
