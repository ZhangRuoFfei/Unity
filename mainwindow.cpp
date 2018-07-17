#include "mainwindow.h"
#include "ui_mainwindow.h"
#include<QPalette>
//#include"painted_widget.h"
MainWindow::MainWindow(QWidget *parent) :
    QMainWindow(parent),
    ui(new Ui::MainWindow)
{

    //增加painter控件
    ui->setupUi(this);


    //使一开始的后三个按钮失效
    ui->pause->setDisabled (1);
    ui->delete_spline->setDisabled (0);
    ui->start->setDisabled (1);

}

MainWindow::~MainWindow()
{
    delete ui;
}


void MainWindow::on_pause_clicked()
{
    ui->finish_spline->setDisabled (1);
    ui->pause->setDisabled (1);
    ui->delete_spline->setDisabled (0);
    ui->start->setDisabled (0);

    ui->pw->change_button_state_pause (3);
}

void MainWindow::on_delete_spline_clicked()
{
    ui->finish_spline->setDisabled (0);
    ui->pause->setDisabled (1);
    ui->delete_spline->setDisabled (0);
    ui->start->setDisabled (1);

    ui->pw->delete_spline ();

}

void MainWindow::on_finish_spline_clicked()//曲线画完
{
    ui->finish_spline->setDisabled (0);
    ui->pause->setDisabled (1);
    ui->delete_spline->setDisabled (0);
    ui->start->setDisabled (0);

    int ggrain;
    float ttension;
    ggrain=ui->grain->text ().toInt ();
    ttension=ui->tension->text ().toFloat ();
    ui->pw->change_button_state_finish
            (ui->checkBox->checkState (),
             ui->checkBox_2->checkState (),
             1,ggrain,ttension);
}

void MainWindow::on_start_clicked()
{
    ui->finish_spline->setDisabled (1);
    ui->pause->setDisabled (0);
    ui->delete_spline->setDisabled (1);
    ui->start->setDisabled (1);

    int ggrain;
    float ttension;
    //把tension和grain和speed传给曲线,初始化小车
    ggrain=ui->grain->text ().toInt ();
    ttension=ui->tension->text ().toFloat ();
    float sspeed=ui->speed->text ().toFloat ();
    ui->pw->change_button_state_start
            (ui->checkBox->checkState (),
             ui->checkBox_2->checkState (),
             2,ggrain,ttension,sspeed);

}

void MainWindow::on_speed_valueChanged(double arg1)
{
    ui->pw->set_car_speed_w (ui->speed->text().toFloat ());
}



void MainWindow::on_checkBox_clicked()
{
    if(ui->checkBox->checkState ())
    {
        ui->checkBox_2->setChecked (false);
    }
    ui->pw->change_check1 (ui->checkBox->checkState ());
}

void MainWindow::on_checkBox_2_clicked()
{
    if(ui->checkBox_2->checkState ())
    {
        ui->checkBox->setChecked (false);
    }
    ui->pw->change_check2 (ui->checkBox_2->checkState ());


}
