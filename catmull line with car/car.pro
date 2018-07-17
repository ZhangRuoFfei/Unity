#-------------------------------------------------
#
# Project created by QtCreator 2017-10-05T15:23:27
#
#-------------------------------------------------

QT       += core gui

greaterThan(QT_MAJOR_VERSION, 4): QT += widgets

TARGET = car
TEMPLATE = app


SOURCES += main.cpp\
        mainwindow.cpp \
    cpt.cpp \
    catmull_spline.cpp \
    painted_widget.cpp \
    car.cpp

HEADERS  += mainwindow.h \
    catmull_spline.h \
    cpt.h \
    painted_widget.h \
    car.h

FORMS    += mainwindow.ui
