# 本地树木与居民房屋模型

来源：`/home/lx/模型/树/松树.glb` 和 `/home/lx/模型/居民房子/f8ddfc8490000b9f9f14860a3b78c366.glb`。原始 GLB 保留在原处，项目仅导入游戏运行副本。

- 松树：原1,499,934面，运行副本13,999面。
- 房屋：原1,499,898面，运行副本32,000面。
- 保留原模型UV与基础颜色贴图，贴图缩至2048；使用现有手绘模型材质以匹配场景光照。
- FBX位于 `Assets/StarfieldEra/Environment/LocalModels`，对应材质也在该目录。
- 归一化预制体位于 `Assets/StarfieldEra/Resources/Environment`；树木按原场景高度及固定随机朝向放置，房屋按各原建筑位置、高度、宽度限制放置，底部贴地。
- `LocalEnvironment` 统一实例化模型，自动共享网格和材质。地图树木与后续植树都使用新松树；原手绘房屋立面替换成新房屋，已建建筑与解锁建筑仍依附原交互根节点。
- 原有建筑占地、碰撞与交互继续使用既有逻辑；建造选址也会识别新树木的位置。采石场的石块外观保留。

重新转换：在项目根运行 `blender -b --python Tools/Blender/import_environment.py`（需要原始GLB存在于上面的本机路径）。停止Unity运行后，菜单 `StarfieldEra > Set up local environment models` 重建材质与预制体。

验证：Unity编译通过，实际运行确认113棵松树与当前4栋可见房屋使用新材质和模型，存档状态有效；仅检查导入及实际画面，未运行自动测试或完整发行构建。
