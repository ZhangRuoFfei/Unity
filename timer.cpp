#include"timer.h"
timer::timer(QObject * parent ):QNewObject( parent )
{

    m_nTimerId = startTimer(1000);

}

timer::~timer()
{

    if ( m_nTimerId != 0 )  killTimer(m_nTimerId);

}



void timer::timerEvent( QTimerEvent *event )
{


}
