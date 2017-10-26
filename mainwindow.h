#ifndef MAINWINDOW_H
#define MAINWINDOW_H

#include <QMainWindow>
#include"catmull_spline.h"
#include"painted_widget.h"
#include<qpainter.h>
#include<qpixmap.h>
#include<QVBoxLayout>

class QVBoxLayout;

namespace Ui {
class MainWindow;
}

class MainWindow : public QMainWindow
{
    Q_OBJECT

public:
    explicit MainWindow(QWidget *parent = 0);
    ~MainWindow();



private slots:
    void on_pause_clicked();

    void on_delete_spline_clicked();

    void on_finish_spline_clicked();

    void on_start_clicked();

    void on_speed_valueChanged(double arg1);


    void on_checkBox_clicked();

    void on_checkBox_2_clicked();

private:
    Ui::MainWindow *ui;
    //painted_widget *pw;
};

#endif // MAINWINDOW_H
