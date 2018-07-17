#ifndef TIMER_H
#define TIMER_H
#include<QObject>
class timer : public QObject
{

    Q_OBJECT

public:

    timer( QObject * parent = 0 );

    virtual ~timer();

protected:

    void timerEvent( QTimerEvent *event );

    int m_nTimerId;

};
#endif // TIMER_H
