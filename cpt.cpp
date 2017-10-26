#include"cpt.h"
cpt::cpt(){}
cpt::cpt(float xx,float yy):x(xx),y(yy){}
void cpt::set_cpt (float xx, float yy){x=xx;y=yy;}
void cpt::set_x(float xx){x=xx;}
void cpt::set_y(float yy){y=yy;}
float cpt::get_x (){return x;}
float cpt::get_y (){return y;}
float cpt::multiple (char xy)
{
    if(xy=='x')
        return x*x;
    else if(xy=='y')
        return y*y;
    return -1;
}
