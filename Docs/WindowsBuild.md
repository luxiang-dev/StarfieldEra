# Windows 窗口版打包步骤

工程：`/home/lx/UnityProjects/StarfieldEra`，Unity：`6000.3.26f1`。

1. 检查 Unity 安装包含 Windows Build Support。本机已安装，BuildPipeline 检查 StandaloneWindows64 支持结果为 true。
2. 停止编辑器 Play 模式，保存资源；使用现有场景 `Assets/Scenes/FarmPrototype.unity`。场景运行时从 Resources 加载 `BarbarianPlayer.prefab`，包含野蛮人的材质、骨骼及 Idle / Walk 动画。
3. 设置 Player Settings：Full Screen Mode = Windowed；Default Screen Width = 1280；Default Screen Height = 720；Resizable Window = true；Run In Background = true。
4. 设置目标平台 StandaloneWindows64、脚本后端 Mono，构建选项为 None（普通发布构建，不启用 Development Build）。
5. 通过已连接的编辑器执行 BuildPipeline.BuildPlayer，输出到 `Builds/Windows-Barbarian/StarfieldEra.exe`。构建日志在 `/home/lx/.config/unity3d/Editor.log`；编译资源和着色器可能耗时。
6. 成功后，保留完整输出目录。将 StarfieldEra.exe、UnityPlayer.dll、StarfieldEra_Data、MonoBleedingEdge 等文件一并复制到 Windows 电脑，双击 exe 启动。不能只复制 exe。

## 在 Unity 界面中重复操作

打开 File > Build Profiles，选择 Windows 平台，目标架构选择 Intel 64-bit / x86_64。通过 Player Settings 的 Resolution and Presentation 配置窗口设置；在 Other Settings 的 Configuration 中将 Scripting Backend 设为 Mono。确认 Scene List 中启用 FarmPrototype 场景，然后点击 Build，选择输出目录并将程序命名为 StarfieldEra.exe。

## 本次调用的核心 API

```csharp
PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
PlayerSettings.defaultScreenWidth = 1280;
PlayerSettings.defaultScreenHeight = 720;
PlayerSettings.resizableWindow = true;
PlayerSettings.runInBackground = true;
PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
AssetDatabase.SaveAssets();
var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
    scenes = new[] { "Assets/Scenes/FarmPrototype.unity" },
    locationPathName = "/home/lx/UnityProjects/StarfieldEra/Builds/Windows-Barbarian/StarfieldEra.exe",
    target = BuildTarget.StandaloneWindows64,
    options = BuildOptions.None
});
```

代码使用 UnityEditor、UnityEditor.Build 和 UnityEngine 命名空间，只能在编辑器侧执行。

## 本次验证结果

Unity BuildReport 结果为 Succeeded，耗时约 305 秒。报告中唯一的错误记录是 Pipeline 命令接口的 5 秒主线程等待超时；实际 BuildPipeline 继续执行并成功完成。生成程序经文件格式检查为 Windows x86-64 GUI PE 可执行文件。

已通过 Wine 使用 `-batchmode -nographics` 进行限时启动检查：Unity 引擎、Mono 程序集、Input System 和 PhysX 均完成初始化，检查期间日志未出现游戏脚本异常。未进行 Windows 真机图形测试。

发布压缩包：`Builds/StarfieldEra-Windows-Windowed.zip`，约 59.5 MiB；已检查 ZIP 完整性，打包时排除 Burst 的 DoNotShip 调试目录。
