#ifndef CAR_H
#define CAR_H
#include<QPixmap>
class car
{
private:
    QPixmap *png1;
    float speed;
    float current_path_len;
    float current_time;
    float max_length;
    bool first;//是否第一遍走这条路

public:
    car();
    car(float cspeed,float cpath_len,float ctime,float cmax,bool f);
    ~car();
    float cal_path_len(float time,bool change_time);//计算当前路长，并返回
    void change_car_speed(float cspeed);
    void change_car_max_length(float max);
    void change_car_current_time(float ctime);
    void change_car_current_path_len(float clen);
    QPixmap get_car_pixmap();
    bool car_first();

};

#endif // CAR_H
