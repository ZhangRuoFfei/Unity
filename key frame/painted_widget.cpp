#include"painted_widget.h"
painted_widget::painted_widget(QWidget *parent):QFrame(parent),status(0),count(0),time_gap(100),time(0)
{
    //获取鼠标在画布中的坐标点
    setMouseTracking(true);
    mouse_pos=new QLabel(this);
    mouse_pos->setGeometry (QRect(220,10,160,20));
    mpos=" [X]:    [Y]: ";
    mouse_pos->setText (mpos);
    mouse_pos->show ();

    //获取关键帧的点数目
    frame_count=new QLabel(this);
    frame_count->setGeometry (QRect(430,10,250,20));
    QString s = QString::number(count, 10);
    frame_count->setText ("[key frame point(s)]: "+s);
    frame_count->show();

    //基本参数初始化
    frames=NULL;
    vframes=NULL;
    inner_frame=0;
    speed_mode=0;//0表示匀速，1表示加速，2表示减速
    img_num=0;
    draw_points.clear ();
    paint_point_for_img1=NULL;
    paint_point_for_img2=NULL;
    paint_vector_for_img1=NULL;
    paint_vector_for_img2=NULL;
    //设置时间触发器
    start_timer=new QTimer(this);
    start_timer->setSingleShot (false);
    start_timer->setInterval (time_gap);
    connect(start_timer,SIGNAL(timeout()),this,SLOT(time_up()));
}

painted_widget::~painted_widget()
{
    if(paint_point_for_img1!=NULL) delete paint_point_for_img1;
    delete start_timer;
}

void painted_widget::paintEvent (QPaintEvent*event)
{

    //paintEvent需要画笔工具QPainter，在头文件QPainter中
    QPainter p(this);
    //消锯齿，让画出的图形更加美观
    p.setRenderHint(QPainter::SmoothPixmapTransform);
    if(count == 0){}//排除什么都没画的情况
    else if(status==0)//当还在画没开始动时
    {
        if(img_num<2)
        {
            for(int i=0;i<draw_points.size();i++)
            {
                //画点
                //设置画笔属性
                p.setBrush (Qt::Dense4Pattern);
                p.setPen (QPen(Qt::gray,Qt::SolidPattern));
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

        if(img_num==1)
        {
            for(int i=0;i<count-1;i++)
            {      
                QPointF p1(paint_point_for_img1[i].get_x (),paint_point_for_img1[i].get_y ());
                QPointF p2(paint_point_for_img1[i+1].get_x (),paint_point_for_img1[i+1].get_y ());
                p.drawLine (p1,p2);
                p.drawEllipse (paint_point_for_img1[i].get_x ()-2,paint_point_for_img1[i].get_y ()-2,4,4);

            }
            p.drawEllipse (paint_point_for_img1[count-1].get_x ()-2,paint_point_for_img1[count-1].get_y ()-2,4,4);

        }
        else if(img_num==2)
        {
            for(int i=0;i<count-1;i++)
            {

                QPointF p1(paint_point_for_img1[i].get_x (),paint_point_for_img1[i].get_y ());
                QPointF p2(paint_point_for_img1[i+1].get_x (),paint_point_for_img1[i+1].get_y ());
                p.drawLine (p1,p2);
                p.drawEllipse (paint_point_for_img1[i].get_x ()-2,paint_point_for_img1[i].get_y ()-2,4,4);

                QPointF p3(paint_point_for_img2[i].get_x (),paint_point_for_img2[i].get_y ());
                QPointF p4(paint_point_for_img2[i+1].get_x (),paint_point_for_img2[i+1].get_y ());
                p.drawLine (p3,p4);
                p.drawEllipse (paint_point_for_img2[i].get_x ()-2,paint_point_for_img2[i].get_y ()-2,4,4);


            }
            p.drawEllipse (paint_point_for_img1[count-1].get_x ()-2,paint_point_for_img1[count-1].get_y ()-2,4,4);
            p.drawEllipse (paint_point_for_img2[count-1].get_x ()-2,paint_point_for_img2[count-1].get_y ()-2,4,4);

        }
    }
    else if(status==1||status==2)//线性插值
    {
        for(int i=0;i<count-1;i++)
        {
            QPointF p1(frames[time][i].get_x (),frames[time][i].get_y ());
            QPointF p2(frames[time][i+1].get_x (),frames[time][i+1].get_y ());
            p.drawLine (p1,p2);
            p.drawEllipse (frames[time][i].get_x ()-2,frames[time][i].get_y ()-2,4,4);

        }
        p.drawEllipse (frames[time][count-1].get_x ()-2,frames[time][count-1].get_y ()-2,4,4);


    }

}

void painted_widget::mousePressEvent (QMouseEvent *event)
{
    if(status==0&&img_num<2)
    {
        cpt temp((float)event->pos().x(),(float)event->pos().y ());
        draw_points.push_back (temp);
        if(img_num==0)
        {
            count++;
            QString s = QString::number(count, 10);
            frame_count->setText ("[key frame point(s)]: "+s);
            frame_count->show();
        }

    }
     setCursor(Qt::CrossCursor);
     update();

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
    mpos.append(" [X]: ");
    mpos.append(temp_x);
    mpos.append(" [Y]: ");
    mpos.append(temp_y);
    mouse_pos->setText (mpos);
    mouse_pos->show ();
    update();

}

int painted_widget::change_button_state_finish (int i )
{
    status=i;
    if(img_num==0)
    {
        count=draw_points.size ();
        paint_point_for_img1=new cpt[count];
        paint_vector_for_img1=new vec[count];
        for(int i=0;i<draw_points.size ();i++)
        {
            paint_point_for_img1[i].set_cpt (draw_points[i].get_x (),draw_points[i].get_y ());
            if(i>=1)
            {
                float deltx=draw_points[i].get_x ()-draw_points[i-1].get_x ();
                float delty=draw_points[i].get_y ()-draw_points[i-1].get_y ();
                paint_vector_for_img1[i].set_r (sqrt(deltx*deltx+delty*delty));
                paint_vector_for_img1[i].set_angle (atan2(delty,deltx));
            }

        }

        img_num++;
        draw_points.clear ();
    }
    else if(img_num==1)
    {
        int temp_count=draw_points.size ();
        if(temp_count!=count)//如果两个关键帧的点数目不一样
        {
            //认为这个画的不算数，清除
            draw_points.clear ();
            QMessageBox::critical(NULL, "Error", "number is wrong",QMessageBox::Yes);
            return 0;
        }
        paint_point_for_img2=new cpt[draw_points.size ()];
        paint_vector_for_img2=new vec[draw_points.size ()];
        for(int i=0;i<draw_points.size ();i++)
        {
            paint_point_for_img2[i].set_cpt (draw_points[i].get_x (),draw_points[i].get_y ());
            if(i>=1)
            {
                float deltx=draw_points[i].get_x ()-draw_points[i-1].get_x ();
                float delty=draw_points[i].get_y ()-draw_points[i-1].get_y ();
                paint_vector_for_img2[i].set_r (sqrt(deltx*deltx+delty*delty));
                paint_vector_for_img2[i].set_angle (atan2(delty,deltx));
            }
        }

        img_num++;
        draw_points.clear ();

    }
    update();
    return 1;//表示成功绘制了一副
}

void painted_widget::change_button_state_xianxing (int i,int iinner_frame,QString sspeed_mode)//线性插值
{
    time=0;
    status=i;
    change_speed_mode(sspeed_mode);
    inner_frame=iinner_frame;
    create_frames_xianxing();
}

void painted_widget::create_frames_xianxing ()
{

   if(frames!=NULL) delete frames;
   frames=new cpt*[2+inner_frame];
   for(int i=0;i<inner_frame+2;i++)
   {
       frames[i]=new cpt[count];
       float val=law(1.0f*i/(1+inner_frame));
       for(int j=0;j<count;j++)
       {
           frames[i][j].set_x ((1-val)*paint_point_for_img1[j].get_x ()+val*paint_point_for_img2[j].get_x ());
           frames[i][j].set_y ((1-val)*paint_point_for_img1[j].get_y ()+val*paint_point_for_img2[j].get_y ());
       }

   }
   start_timer->start (time_gap);//时间触发器开始
}

float painted_widget::law (float t)
{
    if(speed_mode==0) return t;
    else if(speed_mode==1) return (1-cos(PI*t/2));
    else  return(sin(PI*t/2));
}

void painted_widget::change_speed_mode (QString sm)
{
    if(sm=="Uniform speed") speed_mode=0;
    if(sm=="Accelerate") speed_mode=1;
    if(sm=="Decelerate") speed_mode=2;
}

void painted_widget::change_button_state_shiliang (int i, int iinner_frame, QString sspeed_mode,int direct)
{
    time=0;
    status=i;
    change_speed_mode(sspeed_mode);
    inner_frame=iinner_frame;
    create_frames_shiliang(direct);

}

void painted_widget::create_frames_shiliang (int direct)
{
    if(vframes!=NULL) delete vframes;
    vframes=new vec*[2+inner_frame];
    for(int i=0;i<inner_frame+2;i++)
    {
        vframes[i]=new vec[count];
        float val=1.0f*i/(1+inner_frame);
        for(int j=1;j<count;j++)
        {
            //r
            vframes[i][j].set_r ((1-val)*paint_vector_for_img1[j].get_r ()+val*paint_vector_for_img2[j].get_r ());
            //angle
            if(direct==0)
            {
                if(paint_vector_for_img1[j].get_angle()>paint_vector_for_img2[j].get_angle() )
                    vframes[i][j].set_angle ((1-val)*paint_vector_for_img1[j].get_angle ()+val*(paint_vector_for_img2[j].get_angle ()+2*PI));
                else
                    vframes[i][j].set_angle ((1-val)*paint_vector_for_img1[j].get_angle ()+val*(paint_vector_for_img2[j].get_angle ()));
            }
            else if(direct==1)
            {
                if(paint_vector_for_img1[j].get_angle()>paint_vector_for_img2[j].get_angle() )
                    vframes[i][j].set_angle ((1-val)*paint_vector_for_img1[j].get_angle ()+val*(paint_vector_for_img2[j].get_angle ()));
                else
                    vframes[i][j].set_angle ((1-val)*(paint_vector_for_img1[j].get_angle ()+2*PI)+val*(paint_vector_for_img2[j].get_angle ()));
            }

        }
    }
    if(frames!=NULL) delete frames;
    frames=new cpt*[2+inner_frame];
    for(int i=0;i<inner_frame+2;i++)
    {
        frames[i]=new cpt[count];
        float val=law(1.0f*i/(1+inner_frame));
        frames[i][0].set_x ((1-val)*paint_point_for_img1[0].get_x ()+val*paint_point_for_img2[0].get_x ());
        frames[i][0].set_y ((1-val)*paint_point_for_img1[0].get_y ()+val*paint_point_for_img2[0].get_y ());
        for(int j=1;j<count;j++)
        {
            frames[i][j].set_x (frames[i][j-1].get_x ()+vframes[i][j].get_r ()*cos(vframes[i][j].get_angle ()));
            frames[i][j].set_y (frames[i][j-1].get_y ()+vframes[i][j].get_r ()*sin(vframes[i][j].get_angle ()));
        }

    }
    start_timer->start (time_gap);//时间触发器开始
}

void painted_widget::delete_img ()
{
    status=0;//当前所处状态
    count=0;//关键点数目
    img_num=0;//关键帧数目
    inner_frame=0;
    speed_mode=0;
    if(paint_point_for_img1) delete paint_point_for_img1;
    if(paint_point_for_img2) delete paint_point_for_img2;
    if(paint_vector_for_img1) delete paint_vector_for_img1;
    if(paint_vector_for_img2) delete paint_vector_for_img2;

    if(frames) delete frames;
    if(vframes) delete(vframes);
    paint_point_for_img1=NULL;
    paint_point_for_img2=NULL;
    paint_vector_for_img1=NULL;
    paint_vector_for_img2=NULL;
    frames=NULL;
    vframes=NULL;


    start_timer->stop ();
    time=0;

    draw_points.clear ();

    frame_count->setText ("[key frame point(s)]: 0");
    frame_count->show();
    update();
}



void painted_widget::time_up ()
{
    time++;
    if(time==(inner_frame+2)) time=0;
    update();
}

void painted_widget::pause ()
{
    if(start_timer->isActive ())start_timer->stop ();
    else start_timer->start ();
}

