#include"vec.h"

vec::vec():r(0),angle(0){}
vec::vec(float rr,float aa):r(rr),angle(aa){}//构造函数
void vec::set_vec(float rr,float aa)//赋值
{
    r=rr;
    angle=aa;
}

void vec::set_r(float rr)
{
    r=rr;
}

void vec::set_angle(float aa)
{
    angle=aa;
}

float vec::get_r()
{
    return r;
}

float vec::get_angle()
{
    return angle;
}
