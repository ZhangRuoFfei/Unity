#ifndef PAINTED_WIDGET_H
#define PAINTED_WIDGET_H
#include<qwidget.h>
#include<QPainter>
#include<QPixmap>
#include<qvector.h>
#include<QMouseEvent>
#include<QFrame>
#include<QTimer>
#include<QLabel>
#include<QString>
#include<QMessageBox>
#include<math.h>
#include"cpt.h"
#include"vec.h"

class QLabel;
class QTimer;
class painted_widget:public QFrame
{
    Q_OBJECT//只有继承了QObject类的类，才具有信号槽的能力
public:
    painted_widget(QWidget *parent = 0);
    int change_button_state_finish(int i);
    void change_button_state_xianxing(int i,int inner_frame,QString speed_mode);
    void change_button_state_shiliang(int i,int inner_frame,QString speed_mode,int direct);
    void delete_img();
    void pause();


protected:
    void paintEvent(QPaintEvent*event);//重载
    void mouseMoveEvent(QMouseEvent * event);//添加mouseMover事件响应
    void mousePressEvent (QMouseEvent *event);//每次鼠标点击都会增加一个点
    ~painted_widget();

private:
    const float PI=3.14159;
    float law(float t);//插值速度模式
    void change_speed_mode(QString sm);
    void create_frames_xianxing();
    void create_frames_shiliang(int direct);
    cpt**frames;//所有帧
    vec**vframes;
    int inner_frame;
    int speed_mode;
    QLabel *mouse_pos;
    QLabel *frame_count;//显示出关键帧点数
    QString mpos;
    int img_num;//现在有几个关键图形
    QVector<cpt>draw_points;//关键帧图形

    int count;//关键帧点的数目

    int status;//0在画 1表示线性插值，2表示适量线性插值。不清除一直运动下去
    cpt*paint_point_for_img1;//将之前的容器转换成指针，关键帧1
    cpt*paint_point_for_img2;//将之前的容器转换成指针，关键帧2
    vec*paint_vector_for_img1;
    vec*paint_vector_for_img2;


    QTimer *start_timer;
    const float time_gap;
    int time;
    int beginning;


private slots:
    void time_up();

};

#endif // PAINTED_WIDGET_H
