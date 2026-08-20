
#                                             UnityShapeZ
使用 Unity 引擎复刻自动化流水线游戏《异形工厂》（Shapez）的核心功能。

当前内容包括：  
1、传送带系统、传送带的动态调整，与其他建筑的交互  
2、GPUInstance渲染图形  
3、自定义Editor实现可视化调试  

直线及弯道传送带运输物体  
  <img width="266" height="166" alt="x6m65-jfnbk" src="https://github.com/user-attachments/assets/262d1993-1ec7-4505-9f36-4687992298cb" />  
使用双列表模拟传送带运输情况，实现流畅运输、局部堵塞等情形。  
不采用直接生成实体的方式而是根据每个物体的位置直接在每帧使用GPU实例化渲染图像。





自定义Editor界面  
<img width="1001" height="367" alt="屏幕截图 2026-08-09 213030" src="https://github.com/user-attachments/assets/93bc28f0-c25f-4820-ba1e-3f2a4d21f07b" />
原游戏已开源（原游戏语言为JavaScript）  
地址：https://github.com/tobspr-games/shapez.io
