# 星野纪元 · StarfieldEra

3D 低多边形单人游戏原型，融合农场经营、原创生物伙伴收集、城镇与科技发展、回合制战斗。

## 开发环境

- Unity 6.3 LTS / 6000.3.26f1
- Universal Render Pipeline（URP）
- 目标：Linux x86_64、Windows x86_64
- 暂不加入付费、广告或联机系统

在 Unity Hub 的 Projects 页面打开本目录。当前项目是已完成导入的开发基础，模板场景为 `Assets/Scenes/SampleScene.unity`；尚未实现游戏玩法。

## 目录

- `Assets/Scenes`：场景
- `Assets/Settings`：渲染与项目配置
- `Assets/Editor/ProjectBootstrap`：显式调用时才运行的初始化工具，不进入游戏构建
- `Docs/GameBrief.md`：游戏概念与首个原型范围

## 构建

打开 File → Build Profiles，选择 Linux 或 Windows 的 x86_64 平台。首个原型使用 Mono；编辑器已安装 Windows Mono、Linux IL2CPP 支持模块。

版本管理忽略 Library、Temp、Logs 和构建产物；Unity 资源应和对应 `.meta` 文件一起提交。
