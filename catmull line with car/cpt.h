#ifndef cpt_H
#define cpt_H
class cpt
{private:
    float x;
    float y;
public:
    cpt();
    cpt(float xx,float yy);//构造函数
    void set_cpt(float xx,float yy);//赋值
    void set_x(float xx);
    void set_y(float yy);
    float multiple(char xy);
    float get_x();
    float get_y();

};
#endif // cpt_H
