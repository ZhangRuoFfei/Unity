#ifndef MAINWINDOW_H
#define MAINWINDOW_H

#include <QMainWindow>

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
    void on_finish_img_clicked();

    void on_delete_img_clicked();

    void on_xianxing_clicked();

    void on_shiliang_clicked();

    void on_shiliang_2_clicked();

    void on_shiliang_3_clicked();

private:
    Ui::MainWindow *ui;
    int finish_img;
};

#endif // MAINWINDOW_H
