### eyeBall shader 变量说明

#### 变量
1. `Lumainance`:眼球diffuse的亮度
2. `Iris Color`:虹膜部分的颜色，白色即为原始颜色
3. `Base`：基础纹理，一张眼球纹理即可
4. `Specular Tex`:会与巩膜虹膜normalMap产生的高光叠加，避免高光太突兀，不需要可以不设置。修改Iris&Sclera Gloss的值可以改变高光效果。
5. `Normal Detail Tex`:眼球细节的normal map（图片类型要设置成normal map），比如眼球的血管啊之类的，用来计算眼球细节的高光
6. `MaskTex`：遮罩，用来区分开虹膜角膜区域和巩膜区域，记得图片要设置成alpha from gray scale
7. `Cornea Gloss`：角膜高光的光泽度
8. `Cornea Specular`:角膜高光的亮度
9. `Cornea Specular Color`:角膜高光的颜色
10. `Iris&Sclera Gloss`:巩膜虹膜细节高光的光泽度
11.  `Iris&Sclera Specular`:巩膜虹膜细节高光的亮度
12.  `Detail Specular Color`:细节高光的颜色，alpha值可以修改细节高光的不透明度，使其不会太突兀
13.  `CUbe Map`:顾名思义，一张CubeMap，用来反射周围环境。如果有需要的话，在`Tool->renderCubeMap`中可以选择观察摄像头和目标cubeMap来自定义环境图案
14.  `ReflAmount`:反射周围环境的强度
15.  `Pupil Size`:眼球瞳孔大小
16. `Pupil Tex Mask`:瞳孔区域，用来去掉角膜的高光，alpha from gray scale，注意瞳孔边缘模糊一下不然过渡会比较突兀。如果想保留瞳孔区域的高光就不设置此图片。

-------------------------------------
#### 结果
漫反射+角膜高光+细节高光（如血管）+虹膜颜色叠加+cubemap环境反射

![result](https://github.com/AcccGO/Unity/blob/UnityShader/Human/Eye/demo.png)
