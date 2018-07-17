#ifndef PAINTED_WIDGET_H
#define PAINTED_WIDGET_H
#include<qwidget.h>
#include<QPainter>
#include<QPixmap>
#include"catmull_spline.h"
#include"car.h"
#include<qvector.h>
#include<QMouseEvent>
#include<QFrame>
#include<QTimer>
#include<QLabel>
#include<QString>
class QLabel;
class QTimer;
class painted_widget:public QFrame
{
    Q_OBJECT//只有继承了QObject类的类，才具有信号槽的能力
public:
    painted_widget(QWidget *parent = 0);
    void change_button_state_finish(bool ccheck1,bool ccheck2,int i,int ggrain,float ttension);
    void change_button_state_start(bool ccheck1,bool ccheck2,int i,int ggrain,float ttension,float sspeed);
    void change_button_state_pause (int i);
    void delete_spline();
    void set_car_speed_w(float wspeed);
    void change_check1(bool c);
    void change_check2(bool c);
protected:
    void paintEvent(QPaintEvent*event);//重载
    void mouseMoveEvent(QMouseEvent * event);//添加mouseMover事件响应
    void mousePressEvent (QMouseEvent *event);//每次鼠标点击都会增加一个点
    ~painted_widget();
private:
    QLabel *mouse_pos;
    QString mpos;
    QVector<cpt>draw_points;
    QVector<cpt>move_points;
    int count;//关键点数目
    int input_count;//插值之后的点的数目
    int status;//0表示在画还没确认 ，1表示画好还没运动 2表示正在运动，3表示正在暂停。不清除一直运动下去
    catmull_spline*paint_spline;
    car*paint_car;
    cpt*paint_point_for_spline;//将之前的容器转换成指针
    QTimer *start_timer;
    const float time_gap;
    float start_time;
    cpt car_pos;
    int beginning;
    float rotate_angle;//旋转角度
    float move_len;//小车行驶过的路径长度
    bool check1;//显示均匀采样点
    bool check2;//显示非均匀插值点
private slots:
    void time_up();

};

#endif // PAINTED_WIDGET_H
