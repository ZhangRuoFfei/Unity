#include "mainwindow.h"
#include "ui_mainwindow.h"
#include<QPalette>
#include"painted_widget.h"
#include<QString>
MainWindow::MainWindow(QWidget *parent) :
    QMainWindow(parent),
    ui(new Ui::MainWindow)
{


    ui->setupUi(this);
    //使一开始的后四个按钮失效
    ui->xianxing->setDisabled (1);
    ui->shiliang->setDisabled (1);
    ui->delete_img->setDisabled (0);
    ui->shiliang_2->setDisabled (1);
    finish_img=0;
}

MainWindow::~MainWindow()
{
    delete ui;
}


void MainWindow::on_finish_img_clicked()
{
    if(ui->pw->change_button_state_finish (0)) finish_img++;
    if(finish_img==2)
       {
        ui->xianxing->setDisabled (0);
        ui->shiliang->setDisabled (0);
        ui->delete_img->setDisabled (0);
        ui->finish_img->setDisabled (1);
        ui->shiliang_2->setDisabled (0);

    }

}

void MainWindow::on_delete_img_clicked()
{
    ui->xianxing->setDisabled (1);
    ui->shiliang->setDisabled (1);
    ui->delete_img->setDisabled (0);
    ui->finish_img->setDisabled (0);
    ui->shiliang_2->setDisabled (1);


    ui->pw->delete_img ();
    finish_img=0;
}

void MainWindow::on_xianxing_clicked()
{

    int iinner_frame=ui->inner_frame_2->text ().toInt ();
    QString sspeed_mode=ui->speed_2->currentText ();
    ui->pw->change_button_state_xianxing (1,iinner_frame,sspeed_mode);
}

void MainWindow::on_shiliang_clicked()
{

    int iinner_frame=ui->inner_frame_2->text ().toInt ();
    QString sspeed_mode=ui->speed_2->currentText ();
    ui->pw->change_button_state_shiliang (2,iinner_frame,sspeed_mode,0);
}

void MainWindow::on_shiliang_2_clicked()
{
    int iinner_frame=ui->inner_frame_2->text ().toInt ();
    QString sspeed_mode=ui->speed_2->currentText ();
    ui->pw->change_button_state_shiliang (2,iinner_frame,sspeed_mode,1);
}

void MainWindow::on_shiliang_3_clicked()
{
    ui->pw->pause();
}
