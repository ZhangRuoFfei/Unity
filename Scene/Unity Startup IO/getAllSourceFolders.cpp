#include<string>
#include<vector>
#include<stdio.h>
#include<io.h>
#include<iostream>
#include <fstream>
#include <iostream>
#include <sstream>
#include<Windows.h>
#include<cstdlib>
using namespace std;

void getAllSourceFolders(string path, vector<string>& files)
{
	//文件句柄
	long   hFile = 0;
	//文件信息结构
	struct _finddata_t fileinfo;  
	string p; 
	if ((hFile = _findfirst(p.assign(path).append("\\*").c_str(), &fileinfo)) != -1)
	{
		do
		{
			//判断是否为文件夹
			if ((fileinfo.attrib &  _A_SUBDIR))  
			{
				if (strcmp(fileinfo.name, ".") != 0 && strcmp(fileinfo.name, "..") != 0)
				{
					if (strcmp(fileinfo.name ,"Resources")==0)
					{
						files.push_back(p.assign(path).append("/").append(fileinfo.name));
					}
					//递归当前文件夹
					getAllSourceFolders(p.assign(path).append("/").append(fileinfo.name), files);
				}
			}
		} while (_findnext(hFile, &fileinfo) == 0);//寻找下一个，成功返回0，否则-1
		_findclose(hFile);
	}
}

void getAllSourceFiles(string path, vector<string>& files,vector<string>&tfiles, vector<string>&sfolders, vector<string>&tfolders)
{
	long   hFile = 0;
	struct _finddata_t fileinfo;  
	string p; 
	if ((hFile = _findfirst(p.assign(path).append("\\*").c_str(), &fileinfo)) != -1)
	{
		do
		{
			if ((fileinfo.attrib &  _A_SUBDIR)) 
			{
				if (strcmp(fileinfo.name, ".") != 0 && strcmp(fileinfo.name, "..") != 0)
				{
					getAllSourceFiles(p.assign(path).append("/").append(fileinfo.name), files,tfiles, sfolders, tfolders);
				}
			}
			else//文件处理
			{
				files.push_back(p.assign(path).append("/").append(fileinfo.name));
				tfiles.push_back(p.assign("D:/Temp/").append(path.substr(3)).append("/").append(fileinfo.name));
				sfolders.push_back(p.assign(path));
				tfolders.push_back(p.assign("D:/Temp/").append(path.substr(3)));
			}
		} while (_findnext(hFile, &fileinfo) == 0); 
		_findclose(hFile);
	}
}

//test
void main()
{
	string sourceFolderDir = "D:/workPrograms/";
	vector<string> sourceFolderVec;

	//resources文件夹
	char * sourceFolderTxt = "sourceFolderTxt.txt";
	getAllSourceFolders(sourceFolderDir, sourceFolderVec);//找到所有source文件夹
	ofstream ofn(sourceFolderTxt);  //输出文件流,保存sourceFile文件夹
	int size = sourceFolderVec.size();
	int  FaiNum = 0;
	ofn << size << endl;
	for (int i = 0; i<size; i++)
	{
		ofn << sourceFolderVec[i] << endl;
	}
	ofn.close();

	//resources文件夹下的文件
	char * sourceFileTxt = "sourceFileTxt.txt";
	char * targetFileTxt = "targetFileTxt.txt";
	char * complexSourceFolderTxt = "complexSourceFolderTxt.txt";
	char * complexTargetFolderTxt = "complexTargetFolderTxt.txt";

	vector<string> sourceFileVec;
	vector<string> targetFileVec;
	vector<string>complexSourceFolderVec;
	vector<string>complexTargetFolderVec;

	for (int i = 0; i < sourceFolderVec.size(); i++)
	{
		getAllSourceFiles(sourceFolderVec[i], sourceFileVec, targetFileVec,complexSourceFolderVec, complexTargetFolderVec);
	}
	ofstream ofn2(sourceFileTxt);//输出文件流,保存sourceFile文件夹
	ofstream ofn3(targetFileTxt);
	ofstream ofn4(complexSourceFolderTxt);
	ofstream ofn5(complexTargetFolderTxt);


	size = sourceFileVec.size();
	ofn2 << size << endl;
	ofn3 << targetFileVec.size() << endl;
	ofn4 << complexSourceFolderVec.size() <<endl;
	ofn5 << complexTargetFolderVec.size() <<endl;

	for (int i = 0; i<size; i++)
	{
		ofn2 << sourceFileVec[i] << endl;
		ofn3 << targetFileVec[i] << endl;
		ofn4 << complexSourceFolderVec[i] << endl;
		ofn5 << complexTargetFolderVec[i] << endl;

	}
	ofn2.close();
	ofn3.close();
	ofn4.close();
	ofn5.close();

	return ;
}
