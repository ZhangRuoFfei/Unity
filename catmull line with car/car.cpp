#include"car.h"
car::car()
{
    first=true;
    max_length=0;
    speed=0;
    current_path_len=0;
    current_time=0;
    png1=new QPixmap;
    png1->load ("3.png");
}
car::car(float cspeed, float cpath_len,float ctime,float cmax,bool f):speed(cspeed),current_path_len(cpath_len),current_time(ctime),max_length(cmax),first(f)
{
    png1->load ("3.png");

}

car::~car ()
{
    //delete png1;
}

float car::cal_path_len (float time,bool change_time)
{

    if(change_time) current_time=current_time-1000;
    current_path_len+=(time-current_time)*speed;
    if(current_path_len>max_length)
    {
        current_path_len=current_path_len-max_length;
        first=false;
    }
    current_time=time;
    return current_path_len;
}

void car::change_car_speed (float cspeed)
{
    speed=cspeed;
}

void car::change_car_max_length (float max)
{
    max_length=max;
}

void car::change_car_current_path_len (float clen)
{
    current_path_len=clen;
    if(current_path_len>max_length)
    {
        current_path_len=current_path_len-max_length;
        first=false;
    }
}

void car::change_car_current_time (float ctime)
{
    current_time=ctime;
}

QPixmap car::get_car_pixmap ()
{
    return *png1;
}

bool car::car_first ()
{
    return first;
}
