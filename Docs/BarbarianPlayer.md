# 野蛮人玩家角色

玩家外观来自新上传的 `barbarian.glb`，原文件保存在 `Assets/StarfieldEra/Characters/Barbarian/Source/Barbarian.glb`。不依赖废弃目录中的 OBJ、FBX 或动画。

- 17 根骨骼，基础蒙皮，Idle 和 Walk 两个 0.8 秒循环动画。
- 通过 FBX 导入 Unity，使用 URP 材质与原模型贴图；角色高度为 1.85 米。
- `Resources/BarbarianPlayer.prefab` 在 FarmWorld 创建玩家时自动加载。
- `BarbarianLocomotion` 根据实际水平位移设置 Animator 的 Speed，移动时走路，停止时待机；区域传送不计为走路。
- 移动、转向和碰撞继续由现有 FarmGame / CharacterController 控制，动画不使用根运动。

## 修改与重建

Blender 绑定工程另存于 `/home/lx/模型/野蛮人/野蛮人_绑定走路.blend`。

可从项目根目录执行 `blender -b --python Tools/Blender/rig_barbarian.py` 重新生成模型和动画；会覆盖生成的 FBX、贴图，并在 Tools/Blender 保存绑定工程。Unity 停止运行后，使用菜单 **StarfieldEra > Set up barbarian player** 重建材质、Animator 和预制体。

当前为按模型形体制作的基础走路绑定；肩甲、裙摆采用蒙皮随动，没有单独的布料或盔甲物理，也未添加战斗动画。

## 验证

在 Unity 6000.3.26f1 中编译通过。实际运行并模拟 W 键：水平速度约 4 米/秒，Animator 进入 Walk；松开后回到 Idle。采样 Walk 确认循环启用、长度约 0.8 秒且蒙皮顶点确实发生位移，并检查了 Unity 中的贴图和行走姿态。
