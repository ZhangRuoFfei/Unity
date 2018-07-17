#ifndef CATMULL_SPLINE_H
#define CATMULL_SPLINE_H
#include"cpt.h"
#include"QVector"
class catmull_spline
{
private :
    int grain;//关键点之间的插值区间的数目
    int n;//关键点的数目
    //int input_n;//插值之后点的数目
    int all_points_num;//曲线上所有点的数目（包括插值点）
    float tension;//控制点位值的曲线平滑程度
    cpt*knots;//关键点们
    cpt*catmull_spline_points;//插值中间点以后的所有点
    float catmull_matrix[4][4];
    cpt*a;
    cpt*b;
    cpt*c;
    cpt*d;
    float*A;
    float*B;
    float*C;
    float*D;
    float*E;
    //int matrix_flag;//避免过多重复操作
    float total_length;//整体长度
    float *each_length;//每段长度

public:
    catmull_spline(cpt*points,int nn,int ggrain,float ttension);
    ~catmull_spline();
    void cubic_spline();
    void get_catmull_matrix();
    float matrix(int index,char xy,float a,float b,float c,float d,float u);
    int get_input_n();
    float get_total_length();
    float get_x(int i);
    float get_y(int i);
    float cal_spline_length_i(int i,float u1,float u2);//计算第i段u1到u2的长度
    float sfunction(int i,float u);
    cpt get_pos_by_u(int i,float u);
    cpt get_pos_by_length(float length);
    float get_u_by_length(int i,float length,float u1,float u2);
};

#endif // CATMULL_SPLINE_H
