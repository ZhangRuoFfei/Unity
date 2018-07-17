#ifndef VEC_H
#define VEC_H
class vec
{
private:
    float r;
    float angle;
public:
    vec();
    vec(float rr,float aa);//构造函数
    void set_vec(float rr,float aa);//赋值
    void set_r(float rr);
    void set_angle(float aa);
    float get_r();
    float get_angle();

};
#endif // VEC_H
