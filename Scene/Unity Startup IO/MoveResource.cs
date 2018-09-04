using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System;

public class MoveResource : EditorWindow
{
    static List<string> sourceFolderVec = new List<string>();
    static List<string> sourceFileVec = new List<string>();
    static List<string> targetFileVec = new List<string>();
    static List<string> complexSourceFolderVec = new List<string>();
    static List<string> complexTargetFolderVec = new List<string>();

    [MenuItem("Tools/Move Resources")]
    private static void MoveResources()
    {
        //change Txt file path
        string[] src = File.ReadAllLines("D:/workPrograms/StartupProblem/moveResources/moveResources/sourceFileTxt.txt");
        string[] tar = File.ReadAllLines("D:/workPrograms/StartupProblem/moveResources/moveResources/targetFileTxt.txt");
        string[] srcFolder = File.ReadAllLines("D:/workPrograms/StartupProblem/moveResources/moveResources/complexSourceFolderTxt.txt");
        string[] tarFolder = File.ReadAllLines("D:/workPrograms/StartupProblem/moveResources/moveResources/complexTargetFolderTxt.txt");
        int src_num = int.Parse(src[0]);
        int tar_num = int.Parse(tar[0]);
        int suc = 0, fail = 0;
        if (src_num != tar_num)
        {
            Debug.Log("error num!");
            return;
        }
        for (int i = 1; i <= src_num; i++)
        {
            //FileUtil.MoveFileOrDirectory(src[i], tar[i]);
            if (!Directory.Exists(tarFolder[i]))
                Directory.CreateDirectory(tarFolder[i]);
            string tempSrc = @src[i];
            string tempTar = @tar[i];
            FileUtil.MoveFileOrDirectory(tempSrc, tempTar);
            //FileUtil.DeleteFileOrDirectory(tempSrc);
            Debug.Log("move: from " + tempSrc + " to " + tempTar);
            suc++;
        }
        Debug.Log("success " + suc + ", fail " + fail);
    }



    [MenuItem("Tools/Recover Resources")]
    private static void RecoverResource()
    {
        string[] src = File.ReadAllLines("D:/workPrograms/StartupProblem/moveResources/moveResources/sourceFileTxt.txt");
        string[] tar = File.ReadAllLines("D:/workPrograms/StartupProblem/moveResources/moveResources/targetFileTxt.txt");
        string[] srcFolder = File.ReadAllLines("D:/workPrograms/StartupProblem/moveResources/moveResources/complexSourceFolderTxt.txt");
        string[] tarFolder = File.ReadAllLines("D:/workPrograms/StartupProblem/moveResources/moveResources/complexTargetFolderTxt.txt");
        int src_num = int.Parse(src[0]);
        int tar_num = int.Parse(tar[0]);
        int suc = 0, fail = 0;
        if (src_num != tar_num)
        {
            Debug.Log("error num!");
            return;
        }
        for (int i = 1; i <= src_num; i++)
        {
            //FileUtil.MoveFileOrDirectory(src[i], tar[i]);
            if (!Directory.Exists(tarFolder[i]))
                Directory.CreateDirectory(tarFolder[i]);
            string tempSrc = @src[i];
            string tempTar = @tar[i];
            FileUtil.CopyFileOrDirectory(tempTar, tempSrc);
            Debug.Log("move: from " + tempTar + " to " + tempSrc);
            suc++;
        }
        Debug.Log("success " + suc + ", fail " + fail);
    }
}
