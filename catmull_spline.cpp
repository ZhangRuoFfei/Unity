#include"catmull_spline.h"
#include<math.h>
catmull_spline::catmull_spline(cpt *points, int nn, int ggrain, float ttension):n(nn),grain(ggrain),tension(ttension),total_length(0)
{
    //input_n=1+(n-1)*grain;
    all_points_num=1+grain*(n-1);
    catmull_spline_points=new cpt[1+grain*(nn-1)];

    knots=new cpt[nn+2];
    knots[0].set_cpt (points[0].get_x (),points[0].get_y ());
    knots[nn+1].set_cpt (points[n-1].get_x (),points[n-1].get_y ());
    for(int i=1;i<=nn;i++)
    {
        knots[i].set_cpt (points[i-1].get_x (),points[i-1].get_y ());
    }

    a=new cpt[n-1];
    b=new cpt[n-1];
    c=new cpt[n-1];
    d=new cpt[n-1];
    A=new float[n-1];
    B=new float[n-1];
    C=new float[n-1];
    D=new float[n-1];
    E=new float[n-1];
    each_length=new float[n-1];
    get_catmull_matrix ();
    cubic_spline();

    //初始化ABCDE参数以及各段长度
    for(int i=0;i<n-1;i++)
    {
        A[i]=(a[i].multiple ('x')+a[i].multiple ('y'))*9;
        B[i]=(a[i].get_x ()*b[i].get_x ()+a[i].get_y ()*b[i].get_y ())*12;
        C[i]=(a[i].get_x ()*c[i].get_x ()+a[i].get_y ()*c[i].get_y ())*6+(b[i].multiple ('x')+b[i].multiple ('y'))*4;
        D[i]=(b[i].get_x ()*c[i].get_x ()+b[i].get_y ()*c[i].get_y ())*4;
        E[i]=c[i].multiple ('x')+c[i].multiple ('y');
        each_length[i]=cal_spline_length_i (i,0,1);
        total_length+=each_length[i];
    }


}

catmull_spline::~catmull_spline ()
{
    delete catmull_spline_points;
    delete knots;
    delete A;
    delete B;
    delete C;
    delete D;
    delete E;
    delete a;
    delete b;
    delete c;
    delete d;
    delete each_length;

}

void catmull_spline::cubic_spline ()
{
    int i,j;
    cpt*s,*k0,*kml,*k1,*k2;
    float*u=new float[grain];
    for(i=0;i<grain;i++)
    {
        u[i]=((float)i)/grain;//u<1 >=0
    }
    s=catmull_spline_points;
    kml=knots;
    k0=kml+1;
    k1=k0+1;
    k2=k1+1;//前后要各多加一个点是因为这时候之后遍历的话可能会越界
    for(i=0;i<n-1;i++)
    {
        for(j=0;j<grain;j++)
        {
            s->set_cpt (matrix(i,'x',kml->get_x (),k0->get_x (),k1->get_x (),k2->get_x (),u[j]),matrix(i,'y',kml->get_y (),k0->get_y (),k1->get_y (),k2->get_y (),u[j]));
            s++;
        }
        kml++;
        k0++;
        k1++;
        k2++;
    }
    s->set_cpt (knots[n].get_x (),knots[n].get_y ());

}

void catmull_spline::get_catmull_matrix ()
{
    catmull_matrix[0][0] = -tension;
    catmull_matrix[0][1] = 2 - tension;
    catmull_matrix[0][2] = tension - 2;
    catmull_matrix[0][3] = tension;
    catmull_matrix[1][0] = 2 * tension;
    catmull_matrix[1][1] = tension - 3;
    catmull_matrix[1][2] = 3 - 2 * tension;
    catmull_matrix[1][3] = -tension;
    catmull_matrix[2][0] = -tension;
    catmull_matrix[2][1] = 0;
    catmull_matrix[2][2] = tension;
    catmull_matrix[2][3] = 0;
    catmull_matrix[3][0] = 0;
    catmull_matrix[3][1] = 1;
    catmull_matrix[3][2] = 0;
    catmull_matrix[3][3] = 0;
}

float catmull_spline::matrix (int index,char xy,float aa, float bb, float cc, float dd, float uu)
{
    float p0,p1,p2,p3;
    p0=catmull_matrix[0][0]*aa+catmull_matrix[0][1]*bb+catmull_matrix[0][2]*cc+catmull_matrix[0][3]*dd;
    p1=catmull_matrix[1][0]*aa+catmull_matrix[1][1]*bb+catmull_matrix[1][2]*cc+catmull_matrix[1][3]*dd;
    p2=catmull_matrix[2][0]*aa+catmull_matrix[2][1]*bb+catmull_matrix[2][2]*cc+catmull_matrix[2][3]*dd;
    p3=catmull_matrix[3][0]*aa+catmull_matrix[3][1]*bb+catmull_matrix[3][2]*cc+catmull_matrix[3][3]*dd;


        if(xy=='x')
        {
        a[index].set_x (p0);
        b[index].set_x (p1);
        c[index].set_x (p2);
        d[index].set_x (p3);

        }
        else if(xy=='y')
        {
        a[index].set_y (p0);
        b[index].set_y (p1);
        c[index].set_y (p2);
        d[index].set_y (p3);
        }


    return(p3+uu*(p2+uu*(p1+uu*p0)));
}

int catmull_spline::get_input_n ()
{
    return all_points_num;
}

float catmull_spline::get_x (int i)
{
    return catmull_spline_points[i].get_x ();
}

float catmull_spline::get_y (int i)
{
    return catmull_spline_points[i].get_y ();
}

float catmull_spline::sfunction (int i, float u)
{
    float temp=A[i]*u*u*u*u+B[i]*u*u*u+C[i]*u*u+D[i]*u+E[i];
    if(temp==0) return 0;
    temp=sqrt(temp);
    return temp;
}

float catmull_spline::cal_spline_length_i (int i, float u1, float u2)
{
    int j;
    int n=40;//用扩展的simpson方法将该区间分成宽度为h的n个子区间
    float sum=0;
    float h=(u2-u1)/n;
    for( j=1;j<=n-1;j++)
    {
        if((j==0)||(j==n)) sum+=h/3*sfunction(i,u1+((float)j)/n*(u2-u1));
        if(j%2==1) sum+=4*h/3*sfunction(i,u1+((float)j)/n*(u2-u1));
        else if(j%2==0) sum+=2*h/3*sfunction(i,u1+((float)j)/n*(u2-u1));
    }

    return sum;
}

cpt catmull_spline::get_pos_by_u (int i, float u)
{
    cpt temp;
    float tempx;
    float tempy;
    tempx=a[i].get_x ()*u*u*u+b[i].get_x ()*u*u+c[i].get_x ()*u+d[i].get_x ();
    tempy=a[i].get_y ()*u*u*u+b[i].get_y ()*u*u+c[i].get_y ()*u+d[i].get_y ();
    temp.set_cpt (tempx,tempy);
    return temp;
}

float catmull_spline::get_u_by_length (int i, float length,float u1,float u2)
{
    float temp_length=cal_spline_length_i (i,u1,0.5*(u1+u2));
    if(temp_length==0) return u1;
    if(temp_length<(length-1))
    {
        return get_u_by_length (i,length-temp_length,(u1+u2)/2,u2);
    }
    else if(temp_length>(length+1))
    {
        return get_u_by_length (i,length,u1,(u1+u2)/2);

    }
    else return (u1+u2)/2;
}


cpt catmull_spline::get_pos_by_length (float length)
{
    cpt result;
    int k;
    float current_length=0;
    float current_spline_length=0;
    float current_i=0;
    float current_u=0;
    if(length==0) return catmull_spline_points[0];
    else if (length>=total_length) return catmull_spline_points[all_points_num-1];
    else//如果是处于中间位置
    {
        for( k=0;k<(n-1);k++)
        {
            current_length+=each_length[k];
            if(length==current_length) return catmull_spline_points[1+grain*(k+1)-1];
            else if(length>current_length) continue;
            else if(length<current_length)
            {
                current_i=k;
                current_length-=each_length[k];
                break;
            }
        }
        //找到第几段之后
        current_spline_length=length-current_length;//在当前spline上的长度
        //找到第i段上的u
        current_u=get_u_by_length (current_i,current_spline_length,0,1);
        result.set_cpt (get_pos_by_u (current_i,current_u).get_x (),get_pos_by_u(current_i,current_u).get_y ());
        return result;
    }

}

float catmull_spline::get_total_length ()
{
    return total_length;
}
