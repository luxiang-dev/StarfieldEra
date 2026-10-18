# 农场原型验证

验证日期：2026-10-13。编辑器：Unity 6000.3.26f1，Ubuntu 24.04。

- 编译通过。
- 3 个 EditMode 测试通过：资源不足与非法农田编号、两个浇水夜晚才成熟及防止重复收益、存档往返及损坏恢复。
- Play Mode 验证通过：角色键盘移动、键盘种植、uGUI 按钮种植与浇水、小屋距离限制、两晚成长、收获、买种子、实际存档。
- Linux x86_64 / Mono 构建成功；无图形模式启动 12 秒，未检测到运行时异常，测试结束后由超时工具正常终止。
- Windows x86_64 / Mono 跨平台构建成功。当前 Linux 环境未执行 Windows 玩家程序。

构建输出位于本机 `Builds/Linux/StarfieldEra` 与 `Builds/Windows/StarfieldEra.exe`，构建产物不提交到 Git。

Linux 构建有 1 条开发工具配置警告：Pipeline 不在玩家程序中启用。Windows 构建另外有 18 条可选 URP 光线追踪 shader 在 Linux 上无法编译的警告；此原型未使用光线追踪，常规 URP 构建成功，仍需要在 Windows 机器上验证实际渲染。
