#include"painted_widget.h"
painted_widget::painted_widget(QWidget *parent):QFrame(parent),status(0),count(0),time_gap(100),start_time(0)
{
    //获取鼠标在画布中的坐标点
    setMouseTracking(true);
    mouse_pos=new QLabel(this);
    mouse_pos->setGeometry (QRect(352,491,125,20));
    mpos=" X:    Y: ";
    mouse_pos->setText (mpos);
    mouse_pos->show ();

    //初始状态既不显示插值点也不显示均匀采样点
    check1=false;
    check2=false;

    //基本参数初始化
    move_len=0;
    rotate_angle=0;
    beginning=1;
    paint_car=NULL;
    paint_spline=NULL;
    paint_point_for_spline=NULL;

    //设置时间触发器
    start_timer=new QTimer(this);
    start_timer->setSingleShot (false);
    start_timer->setInterval (time_gap);
    connect(start_timer,SIGNAL(timeout()),this,SLOT(time_up()));
}

painted_widget::~painted_widget()
{
    if(paint_point_for_spline!=NULL) delete paint_point_for_spline;
    if(paint_spline!=NULL) delete paint_spline;
    if(paint_car!=NULL) delete paint_car;
    delete start_timer;
}

void painted_widget::paintEvent (QPaintEvent*event)
{

    //paintEvent需要画笔工具QPainter，在头文件QPainter中
    QPainter p(this);
    //消锯齿，让画出的图形更加美观
    p.setRenderHint(QPainter::SmoothPixmapTransform);
    //设置画笔属性
    p.setBrush (Qt::Dense4Pattern);
    p.setPen (QPen(Qt::gray,Qt::SolidPattern));



    if(status==0)//当还在画的时候，只画点
    {
        for(int i=0;i<draw_points.size ();i++)
        {
            //画点
            p.drawEllipse (draw_points[i].get_x ()-6,draw_points[i].get_y ()-6,12,12);
            //画点之间的连线
            if(i!=draw_points.size ()-1)
            {
                QPointF p1(draw_points[i].get_x (),draw_points[i].get_y ());
                QPointF p2(draw_points[i+1].get_x (),draw_points[i+1].get_y ());
                p.drawLine (p1,p2);
            }
        }

    }

    if(status==1)//当关键点全部画好以后，画出曲线
    {

        if(count>1)//不止一个关键点时，可以画线
        {
            for(int i=0;i<input_count-1;i++)
            {
                p.setPen (QPen(Qt::white, 4, Qt::SolidLine, Qt::RoundCap, Qt::RoundJoin));
                QPointF p1(paint_spline->get_x (i),paint_spline->get_y (i));
                QPointF p2(paint_spline->get_x (i+1),paint_spline->get_y (i+1));
                p.drawLine (p1,p2);


            }
        }
        for(int i=0;i<draw_points.size ();i++)
        {
            p.setBrush (Qt::Dense4Pattern);
            p.setPen (QPen(Qt::black, 1, Qt::DashDotLine, Qt::RoundCap, Qt::RoundJoin));
            p.drawEllipse (draw_points[i].get_x ()-6,draw_points[i].get_y ()-6,12,12);
            //画出关键点之间的连线
            if(i!=draw_points.size ()-1)
            {
                QPointF p1(draw_points[i].get_x (),draw_points[i].get_y ());
                QPointF p2(draw_points[i+1].get_x (),draw_points[i+1].get_y ());
                p.drawLine (p1,p2);
            }
        }

    }

    if(status==2||status==3)//此时要显示运动的小车
    {

        if(count==1)//当只画了一个点的时候，就只在原地弄一辆不动的小车
        {
            p.drawPixmap (draw_points[0].get_x ()-30,draw_points[0].get_y ()-45,60,45,paint_car->get_car_pixmap ());
        }
        else if(count>1)//不止一个关键点时，可以画线
        {

            for(int i=0;i<input_count-1;i++)
            {
                p.setPen (QPen(Qt::white, 4, Qt::SolidLine, Qt::RoundCap, Qt::RoundJoin));
                QPointF p1(paint_spline->get_x (i),paint_spline->get_y (i));
                QPointF p2(paint_spline->get_x (i+1),paint_spline->get_y (i+1));
                p.drawLine (p1,p2);

            }
            //还要画出运动的小车
            if(check1&&(!check2))
            {
                for(int m=0;m<move_points.size ();m++)
                {
                    p.setPen (QPen(Qt::blue, 1, Qt::SolidLine, Qt::RoundCap, Qt::RoundJoin));
                    p.setBrush (QBrush(Qt::blue, Qt::SolidPattern));
                    p.drawEllipse (move_points[m].get_x (),move_points[m].get_y (),2,2);
                }
            }
            p.translate (car_pos.get_x (),car_pos.get_y ());
            p.rotate (rotate_angle);
            p.drawPixmap (-30,-45,60,45,paint_car->get_car_pixmap ());
            p.rotate (-rotate_angle);
            p.translate (-car_pos.get_x (),-car_pos.get_y ());

        }
        //画关键点
        p.setPen (QPen(Qt::black, 1, Qt::DashDotLine, Qt::RoundCap, Qt::RoundJoin));
        for(int i=0;i<draw_points.size ();i++)
        {
            p.setBrush (Qt::Dense4Pattern);
            p.drawEllipse (draw_points[i].get_x ()-6,draw_points[i].get_y ()-6,12,12);

            //画出关键点之间的连线
            if(i!=draw_points.size ()-1)
            {
                QPointF p1(draw_points[i].get_x (),draw_points[i].get_y ());
                QPointF p2(draw_points[i+1].get_x (),draw_points[i+1].get_y ());
                p.drawLine (p1,p2);
            }
        }
    }

    //根据选择框画出插值点
    if(check2&&!check1)
    {
        for(int i=0;i<input_count-1;i++)
        {
        p.setPen (QPen(Qt::gray,1, Qt::SolidLine, Qt::RoundCap, Qt::RoundJoin));
        p.setBrush (QBrush(Qt::gray, Qt::SolidPattern));
        p.drawEllipse (paint_spline->get_x (i),paint_spline->get_y (i),2,2);
        }
    }
}

void painted_widget::mousePressEvent (QMouseEvent *event)
{
    if(status==0)
    {
        cpt temp((float)event->pos().x(),(float)event->pos().y ());
        draw_points.push_back (temp);
        count++;
        update();
    }
     setCursor(Qt::CrossCursor);

}

void painted_widget::mouseMoveEvent(QMouseEvent * event)//添加mouseMover事件响应
{
    QString temp_x;
    QString temp_y;
    QPoint coursePoint;
    coursePoint =event->pos ();//获取当前光标的位置
    temp_x.setNum(coursePoint.x());
    temp_y.setNum(coursePoint.y());
    mpos.clear ();
    mpos.append(" X: ");
    mpos.append(temp_x);
    mpos.append(" Y: ");
    mpos.append(temp_y);
    mouse_pos->setText (mpos);
    mouse_pos->show ();

}
void painted_widget::change_button_state_finish (bool ccheck1,bool ccheck2,int i,int ggrain,float ttension)
{
    status=i;
    if(draw_points.size ()>1)
    {
        check1=ccheck1;
        check2=ccheck2;
        paint_point_for_spline=new cpt[draw_points.size ()];
        for(int i=0;i<draw_points.size ();i++)
        {
            paint_point_for_spline[i].set_cpt (draw_points[i].get_x (),draw_points[i].get_y ());
        }
        paint_spline=new catmull_spline(paint_point_for_spline,count,ggrain,ttension);
        input_count=paint_spline->get_input_n ();
    }
    else paint_spline=NULL;
    update();
}

void painted_widget::change_button_state_start (bool ccheck1,bool ccheck2,int i,int ggrain, float ttension,float sspeed)
{
    paint_car=new car();
    paint_car->change_car_speed (sspeed);
    status=i;

    if(draw_points.size ()>1)
    {
        check1=ccheck1;
        check2=ccheck2;
        //初始化车的位置
        if(beginning)
        {
            car_pos.set_cpt (paint_point_for_spline[0].get_x (),paint_point_for_spline[0].get_y ());
        }

        start_timer->start (time_gap);//时间触发器开始
        paint_point_for_spline=new cpt[draw_points.size ()];
        for(int i=0;i<draw_points.size ();i++)
        {
            paint_point_for_spline[i].set_cpt (draw_points[i].get_x (),draw_points[i].get_y ());
        }
        delete paint_spline;
        paint_spline=new catmull_spline(paint_point_for_spline,count,ggrain,ttension);
        paint_car->change_car_max_length (paint_spline->get_total_length ());
        input_count=paint_spline->get_input_n ();
    }

    else paint_spline=NULL;

    update();
}

void painted_widget::change_button_state_pause (int i)
{
    status=i;
    beginning=0;
    start_timer->stop ();

}

void painted_widget::delete_spline ()
{
    status=0;//当前所处状态
    count=0;//关键点数目
    input_count=0;//插值之后点的数目
    delete paint_point_for_spline;
    delete paint_spline;
    delete paint_car;
    paint_point_for_spline=NULL;
    paint_spline=NULL;
    paint_car=NULL;
    start_timer->stop ();
    start_time=0;//行驶时间
    rotate_angle=0;//小车旋转角度
    draw_points.clear ();
    move_points.clear ();
    beginning=1;//是否刚刚开始运动
    move_len=0;//行驶距离

    update();
}

void painted_widget::set_car_speed_w (float wspeed)
{
    if(status==2)
    paint_car->change_car_speed (wspeed);
}

void painted_widget::time_up ()
{
    int pi=3.14159265;

    if(!beginning)
    {
        //如果不是刚开始，应该把之前的路长和时间告诉小车参数
        paint_car->change_car_current_path_len (move_len);
        paint_car->change_car_current_time (start_time);
    }


    start_time+=2.5;
    if(start_time>=1000)
    {
        start_time-=1000;
        move_len=paint_car->cal_path_len (start_time,true);
    }
    else
    {
        move_len=paint_car->cal_path_len (start_time,false);

    }
    car_pos=paint_spline->get_pos_by_length (move_len);

    if(paint_car->car_first ())
    {
        move_points.push_back (car_pos);
    }

    cpt temp_pos=paint_spline->get_pos_by_length (move_len+3);//下一个位置的旋转角度
    float temp_tan=(temp_pos.get_y ()-car_pos.get_y ())/(temp_pos.get_x ()-car_pos.get_x ());
    rotate_angle=atan(temp_tan)*360/(2*pi);
    if((temp_pos.get_y ()>car_pos.get_y ())&&(temp_pos.get_x ()<car_pos.get_x ())) rotate_angle-=180;
    if((temp_pos.get_y ()<car_pos.get_y ())&&(temp_pos.get_x ()<car_pos.get_x ())) rotate_angle+=180;

    qDebug( "timer event" );
    update();
}

void painted_widget::change_check1 (bool c)
{
    check1=c;
    if(check1) check2=false;
    update();
}

void painted_widget::change_check2 (bool c)
{
    check2=c;
    if(check2) check1=false;
    update();
}
