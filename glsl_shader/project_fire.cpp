#include <stdio.h>  
#include <cstdlib>  
#include <string>  
#include<vector>
#include <GL/glew.h>
#include <iostream>  
#include<glut.h>
#pragma comment(lib,"glew32.lib")   

using namespace std;
GLuint tid;
float eyex = 0, eyey = 0, eyez = 50;
float centerx = 0, centery = 0, centerz = 0;
GLfloat thingposition[3] = { centerx, centery, centerz };
GLfloat eyeposition[3] = { eyex, eyey, eyez };
bool is_rotate = false;
float trotate = 0.0f;
GLuint programHandle;
GLuint vShader, fShader;
GLuint pointVAOId, pointVBOId;
bool first = true;

float mouse[2];
float resolution[2];
double _iMouseX, _iMouseY;
double startTime = 0.0;
double currentTime=0.0;
bool paused = false;
GLfloat vert_data[] = {
	-1.0, -1.0,
	1.0, -1.0,
	-1.0, 1.0,
	1.0, 1.0
};

//�����ַ���    
char *textFileRead(const char *fn)
{
	FILE *fp;
	char *content = NULL;
	int count = 0;
	if (fn != NULL)
	{
		fp = fopen(fn, "rt");
		if (fp != NULL)
		{
			fseek(fp, 0, SEEK_END);
			count = ftell(fp);
			rewind(fp);
			if (count > 0)
			{
				content = (char *)malloc(sizeof(char) * (count + 1));
				count = fread(content, sizeof(char), count, fp);
				content[count] = '\0';
			}
			fclose(fp);
		}
	}
	return content;
}

//��ʾ�汾
void show_info()
{
	//1���鿴�Կ���GLSL��OpenGL����Ϣ      
	const GLubyte *vendor = glGetString(GL_VENDOR);
	const GLubyte *renderer = glGetString(GL_RENDERER);
	const GLubyte *version = glGetString(GL_VERSION);
	const GLubyte *glslVersion = glGetString(GL_SHADING_LANGUAGE_VERSION);
	cout << "�Կ���Ӧ��   : " << vendor << endl;
	cout << "�Կ��ͺ�     : " << renderer << endl;
	cout << "OpenGL�汾   : " << version << endl;
	cout << "GLSL�汾     : " << glslVersion << endl;
}

void initShader(const char *VShaderFile, const char *FShaderFile)
{
	show_info();
	//2��������ɫ��      
	//������ɫ�����󣺶�����ɫ��      
	vShader = glCreateShader(GL_VERTEX_SHADER);
	//������      
	if (0 == vShader)
	{
		cerr << "ERROR : Create vertex shader failed" << endl;
		exit(1);
	}
	//����ɫ��Դ�������ɫ�����������      
	const GLchar *vShaderCode = textFileRead(VShaderFile);
	const GLchar *vCodeArray[1] = { vShaderCode };

	//���ַ�����󶨵���Ӧ����ɫ��������    
	glShaderSource(vShader, 1, vCodeArray, NULL);

	//������ɫ������      
	glCompileShader(vShader);

	//�������Ƿ�ɹ�      
	GLint compileResult;
	glGetShaderiv(vShader, GL_COMPILE_STATUS, &compileResult);
	if (GL_FALSE == compileResult)
	{
		GLint logLen;
		//�õ�������־����      
		glGetShaderiv(vShader, GL_INFO_LOG_LENGTH, &logLen);
		if (logLen > 0)
		{
			char *log = (char *)malloc(logLen);
			GLsizei written;
			//�õ���־��Ϣ�����      
			glGetShaderInfoLog(vShader, logLen, &written, log);
			cerr << "vertex shader compile log : " << endl;
			cerr << log << endl;
			free(log);//�ͷſռ�      
		}
	}

	//������ɫ������Ƭ����ɫ��      
	fShader = glCreateShader(GL_FRAGMENT_SHADER);
	//������      
	if (0 == fShader)
	{
		cerr << "ERROR : Create fragment shader failed" << endl;
		exit(1);
	}

	//����ɫ��Դ�������ɫ�����������      
	const GLchar *fShaderCode = textFileRead(FShaderFile);
	const GLchar *fCodeArray[1] = { fShaderCode };
	glShaderSource(fShader, 1, fCodeArray, NULL);

	//������ɫ������      
	glCompileShader(fShader);

	//�������Ƿ�ɹ�      
	glGetShaderiv(fShader, GL_COMPILE_STATUS, &compileResult);
	if (GL_FALSE == compileResult)
	{
		GLint logLen;
		//�õ�������־����      
		glGetShaderiv(fShader, GL_INFO_LOG_LENGTH, &logLen);
		if (logLen > 0)
		{
			char *log = (char *)malloc(logLen);
			GLsizei written;
			//�õ���־��Ϣ�����      
			glGetShaderInfoLog(fShader, logLen, &written, log);
			cerr << "fragment shader compile log : " << endl;
			cerr << log << endl;
			free(log);//�ͷſռ�      
		}
	}

	//3��������ɫ������*********************      
	//������ɫ������      
	programHandle = glCreateProgram();
	if (!programHandle)
	{
		cerr << "ERROR : create program failed" << endl;
		exit(1);
	}
	
	//����ɫ���������ӵ��������ĳ�����      
	glAttachShader(programHandle, vShader);
	glAttachShader(programHandle, fShader);

	//����Щ�������ӳ�һ����ִ�г���      
	glLinkProgram(programHandle);
	//��ѯ���ӵĽ��      
	GLint linkStatus;
	glGetProgramiv(programHandle, GL_LINK_STATUS, &linkStatus);
	if (GL_FALSE == linkStatus)
	{
		cerr << "ERROR : link shader program failed" << endl;
		GLint logLen;
		glGetProgramiv(programHandle, GL_INFO_LOG_LENGTH,
			&logLen);
		if (logLen > 0)
		{
			char *log = (char *)malloc(logLen);
			GLsizei written;
			glGetProgramInfoLog(programHandle, logLen,
				&written, log);
			cerr << "Program log : " << endl;
			cerr << log << endl;
		}
	}
}

//���glew��ʼ���ͼ��ض��㡢Ƭ����ɫ��  
void init()
{
	//��ʼ��glew��չ��      
	GLenum err = glewInit();
	if (GLEW_OK != err)
	{
		cout << "Error initializing GLEW: " << glewGetErrorString(err) << endl;
	}
	glEnable(GL_DEPTH_TEST);
	//���ض����Ƭ����ɫ���������ӵ�һ�����������    
	initShader("VertexShader.txt", "FragmentShader.txt");
	glGenVertexArrays(1, &pointVAOId);
	glGenBuffers(1, &pointVBOId);
	glBindVertexArray(pointVAOId);
	glBindBuffer(GL_ARRAY_BUFFER, pointVBOId);
	glBufferData(GL_ARRAY_BUFFER, sizeof(vert_data), vert_data, GL_STATIC_DRAW);
	// ����λ������
	glVertexAttribPointer(0, 2, GL_FLOAT, GL_FALSE, 2 * sizeof(GL_FLOAT), (GLvoid*)0);
	glEnableVertexAttribArray(0);


}

void Reshape(int w, int h)
{
	resolution[0] = w;
	resolution[1] = h;
	glViewport(0, 0, (GLsizei)w, (GLsizei)h);
	/*
	glMatrixMode(GL_PROJECTION);
	glLoadIdentity();
	gluPerspective(15, 1, 0.1, 1000.0);
	glMatrixMode(GL_MODELVIEW);
	*/
	glLoadIdentity();
	
}

void display()
{
	/*eyeposition[0] = eyex;
	eyeposition[1] = eyey;
	eyeposition[2] = eyez;
	thingposition[0] = centerx;
	thingposition[1] = centery;
	thingposition[2] = centerz;*/
	glClearColor(1.0f, 1.0f, 1.0f, 1.0f);
	glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
	glLoadIdentity();
	//gluLookAt(eyeposition[0], eyeposition[1], eyeposition[2], thingposition[0], thingposition[1], thingposition[2], 0.0, 1.0, 0.0);
	glEnable(GL_DEPTH_TEST);
	glRotatef(trotate, 0, 1, 0);

	glUseProgram(programHandle);
	GLint resolutionLocation = glGetUniformLocation(programHandle, "resolution");
	GLint timeLocation = glGetUniformLocation(programHandle, "time");
	GLint mouseLocation = glGetUniformLocation(programHandle, "mouse");
	if (!paused)
	{
		currentTime = (float)(glutGet(GLUT_ELAPSED_TIME) / 1000.0f - startTime);
		cout << currentTime << endl;
		glUniform1f(timeLocation, currentTime);
	}
	else
	{
		glUniform1f(timeLocation, currentTime);
		cout << currentTime << endl;
	}
	glUniform2fv(resolutionLocation, 1, resolution);
	glUniform2fv(mouseLocation, 1, mouse);
	glDrawArrays(GL_TRIANGLE_STRIP, 0, 4);
	//glutSolidTeapot(2.0);
	if (is_rotate)
	{
		trotate += 0.5f;
		if (trotate > 360) trotate = 0;
	}
	glutSwapBuffers();
}

void key(unsigned char key, int x, int y)
{
	if (key == 'q')
	{
		exit(0);
	}
	if (key == 's')
	{
		eyey += 0.1;
		centery -= 0.1;
	}
	if (key == 'd')
	{
		eyex -= 0.1;
		centerx += 0.1;
	}
	if (key == 'w')
	{
		eyey -= 0.1;
		centery += 0.1;
	}

	if (key == 'a')
	{
		eyex += 0.1;
		centerx -= 0.1;
	}
	if (key == 'e')
	{
		is_rotate = !is_rotate;
	}
	if (key == 'v')
	{
		eyez--;
	}
	if (key == 'b')
	{
		eyez++;
	}

}

void processMousePassiveMotion(int x, int y)
{
	mouse[0] = x*1.0 / resolution[0];
	mouse[1] = x*1.0 / resolution[1];
}

void processMouseEntry(int state)
{
	if (state == GLUT_LEFT)
		paused=true;
	else
		paused = false;
}

void idle()
{
	glutPostRedisplay();
}

int main(int argc, char** argv)
{
	glutInit(&argc, argv);
	glutInitDisplayMode(GLUT_DOUBLE | GLUT_RGB | GLUT_DEPTH);
	glutInitWindowSize(600, 600);
	resolution[0] = 600;
	resolution[1] = 600;
	glutInitWindowPosition(100, 100);
	glutCreateWindow("final project");
	init();
	glutReshapeFunc(Reshape);
	glutDisplayFunc(display);
	glutKeyboardFunc(key);
	glutPassiveMotionFunc(processMousePassiveMotion);
	glutEntryFunc(processMouseEntry);
	glutIdleFunc(idle);
	glutMainLoop();
	return 0;
}
