#-------------------------------------------------
#
# Project created by QtCreator 2017-11-03T09:57:48
#
#-------------------------------------------------

QT       += core gui

greaterThan(QT_MAJOR_VERSION, 4): QT += widgets

TARGET = P2
TEMPLATE = app


SOURCES += main.cpp\
        mainwindow.cpp \
    painted_widget.cpp \
    cpt.cpp \
    vec.cpp

HEADERS  += mainwindow.h \
    painted_widget.h \
    cpt.h \
    vec.h

FORMS    += mainwindow.ui
